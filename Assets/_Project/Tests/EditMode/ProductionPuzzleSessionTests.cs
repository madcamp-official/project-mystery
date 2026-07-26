using System.Collections.Generic;
using NUnit.Framework;
using UnityEngine;
using Wake.Core;
using Wake.Puzzles;
namespace Wake.Tests
{
    public class ProductionPuzzleSessionTests
    {
        private const string SaveKey = "THE_WAKE_GAME_STATE_V1";

        private GameObject host;
        private GameStateManager state;

        [SetUp]
        public void SetUp()
        {
            DestroyExistingManager();
            PlayerPrefs.DeleteKey(SaveKey);
            state = CreateManager();
            state.StartNewGame();
        }

        [TearDown]
        public void TearDown()
        {
            Object.DestroyImmediate(host);
            DestroyExistingManager();
            PlayerPrefs.DeleteKey(SaveKey);
        }

        [Test]
        public void Catalog_RegistersOnlyTwoPuzzlesWithDocumentedAnswerIds()
        {
            Assert.That(ProductionPuzzleCatalog.All.Count, Is.EqualTo(2));
            Assert.That(ProductionPuzzleCatalog.TryGet("timeline_12_cards", out _), Is.False);
            Assert.That(
                ProductionPuzzleCatalog.TryGet(
                    ProductionPuzzleCatalog.BloodPattern,
                    out ProductionPuzzleDefinition blood),
                Is.True);
            Assert.That(blood.RequiredSelectionIds,
                Is.EqualTo(new[] { "no_spatter", "center_mismatch", "vertical_drop" }));
            Assert.That(blood.RequiredEvidenceIds, Is.EqualTo(new[] { "C-07" }));
        }

        [Test]
        public void BloodPattern_PersistsProgressHintAndCompletion()
        {
            HashSet<string> evidence = new() { "C-07" };
            ProductionPuzzleSession session =
                CreateSession(ProductionPuzzleCatalog.BloodPattern, evidence);

            Assert.That(session.Select("no_spatter"), Is.True);
            Assert.That(session.Select("center_mismatch"), Is.True);
            Assert.That(session.Select("vertical_drop"), Is.True);
            Assert.That(session.Select("invented_fragment"), Is.False);
            session.SetStep(3);
            Assert.That(session.UseHint(), Is.True);
            Assert.That(session.TryComplete().Completed, Is.True);

            ProductionPuzzleSession restored =
                CreateSession(ProductionPuzzleCatalog.BloodPattern, evidence);
            Assert.That(restored.SelectedIds, Has.Count.EqualTo(3));
            Assert.That(restored.Step, Is.EqualTo(3));
            Assert.That(restored.HintLevel, Is.EqualTo(1));
            Assert.That(restored.IsCompleted, Is.True);
            Assert.That(state.HasCompletedScene("D2-02"), Is.True);
        }

        [Test]
        public void CargoRail_RequiresExactRouteFactsAndThreeEvidenceIds()
        {
            HashSet<string> evidence = new() { "C-08", "C-09" };
            ProductionPuzzleSession session =
                CreateSession(ProductionPuzzleCatalog.CargoRailBranch, evidence);
            session.Select("horizon_branch_22_18");
            session.Select("weight_86kg");
            session.Select("ballast_horizon_route");

            PuzzleCompletionResult missing = session.TryComplete();
            evidence.Add("C-10");
            PuzzleCompletionResult completed = session.TryComplete();

            Assert.That(missing.Completed, Is.False);
            Assert.That(missing.MissingEvidenceIds, Is.EqualTo(new[] { "C-10" }));
            Assert.That(completed.Completed, Is.True);
            Assert.That(state.HasFlag("puzzle_cargo_rail_branch_completed"), Is.True);
            Assert.That(state.HasCompletedScene("D6-02"), Is.True);
        }

        [Test]
        public void Completion_IsIdempotentAndPublishesTheoryOnce()
        {
            int theoryEvents = 0;
            void Count(InvestigationEvent item)
            {
                if (item.Kind == InvestigationEventKind.TheoryCompleted)
                {
                    theoryEvents++;
                }
            }

            InvestigationEventHub.Published += Count;
            try
            {
                ProductionPuzzleSession session = CreateSession(
                    ProductionPuzzleCatalog.BloodPattern, new HashSet<string> { "C-07" });
                session.Select("no_spatter");
                session.Select("center_mismatch");
                session.Select("vertical_drop");

                Assert.That(session.TryComplete().Completed, Is.True);
                Assert.That(session.TryComplete().Completed, Is.False);
            }
            finally
            {
                InvestigationEventHub.Published -= Count;
            }

            Assert.That(theoryEvents, Is.EqualTo(1));
        }

        [Test]
        public void IncompleteSession_RestoresAfterManagerRecreation()
        {
            ProductionPuzzleSession session = CreateSession(
                ProductionPuzzleCatalog.CargoRailBranch, new HashSet<string>());
            session.Select("weight_86kg");
            session.SetStep(2);
            session.UseHint();
            Object.DestroyImmediate(host);
            state = CreateManager();
            state.ReloadSavedState();

            ProductionPuzzleSession restored = CreateSession(
                ProductionPuzzleCatalog.CargoRailBranch,
                new HashSet<string>());
            Assert.That(restored.SelectedIds, Contains.Item("weight_86kg"));
            Assert.That(restored.Step, Is.EqualTo(2));
            Assert.That(restored.HintLevel, Is.EqualTo(1));
            Assert.That(restored.IsCompleted, Is.False);
        }

        private ProductionPuzzleSession CreateSession(
            string puzzleId,
            HashSet<string> evidence)
        {
            Assert.That(ProductionPuzzleCatalog.TryGet(puzzleId, out var definition), Is.True);
            return new ProductionPuzzleSession(definition, state, evidence.Contains);
        }

        private GameStateManager CreateManager()
        {
            host = new GameObject("ProductionPuzzleSessionState");
            return host.AddComponent<GameStateManager>();
        }

        private static void DestroyExistingManager()
        {
            if (GameStateManager.Instance != null)
            {
                Object.DestroyImmediate(GameStateManager.Instance.gameObject);
            }
        }
    }
}
