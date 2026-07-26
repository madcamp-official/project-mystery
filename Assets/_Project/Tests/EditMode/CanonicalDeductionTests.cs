using System.Collections.Generic;
using System.Linq;
using NUnit.Framework;
using UnityEngine;
using Wake.Core;
using Wake.Puzzles;
using Wake.UI;

namespace Wake.Tests
{
    public class CanonicalDeductionTests
    {
        private const string SaveKey = "UNDER_THE_HORIZON_GAME_STATE_V2";
        private GameObject host;
        private GameStateManager state;
        private HashSet<string> evidence;
        private CanonicalDeductionService service;

        [SetUp]
        public void SetUp()
        {
            DestroyExistingManager();
            PlayerPrefs.DeleteKey(SaveKey);
            host = new GameObject("CanonicalDeductionTests");
            state = host.AddComponent<GameStateManager>();
            state.StartNewGame();
            evidence = new HashSet<string>();
            service = new CanonicalDeductionService(state, evidence.Contains);
        }

        [TearDown]
        public void TearDown()
        {
            if (host != null)
            {
                Object.DestroyImmediate(host);
            }

            DestroyExistingManager();
            PlayerPrefs.DeleteKey(SaveKey);
        }

        [Test]
        public void Catalog_ContainsTheSixSourceBackedChains()
        {
            Assert.That(
                CanonicalDeductionCatalog.All.Select(item => item.Id),
                Is.EqualTo(new[]
                {
                    "scene_denial",
                    "body_insertion",
                    "transport_route",
                    "actual_murder",
                    "culprit_link",
                    "past_event"
                }));
            Assert.That(
                CanonicalDeductionCatalog.All.Select(item => item.Id).Distinct().Count(),
                Is.EqualTo(6));
        }

        [TestCase("scene_denial", "C-03", "C-04", "C-05")]
        [TestCase("body_insertion", "C-07", "C-08")]
        [TestCase("transport_route", "C-09", "C-10")]
        [TestCase("actual_murder", "C-06", "C-12")]
        [TestCase("culprit_link", "C-01", "C-14", "C-16")]
        [TestCase("past_event", "C-17")]
        public void Catalog_UsesExactEvidenceRequirements(
            string deductionId,
            params string[] required)
        {
            Assert.That(
                CanonicalDeductionCatalog.TryGet(deductionId, out var definition),
                Is.True);
            Assert.That(definition.RequiredEvidenceIds, Is.EqualTo(required));
        }

        [Test]
        public void Evaluation_ReportsOnlyMissingEvidenceIds()
        {
            evidence.UnionWith(new[] { "C-03", "C-05" });

            DeductionEvaluation result =
                service.Evaluate(CanonicalDeductionCatalog.SceneDenial);

            Assert.That(result.HasAllEvidence, Is.False);
            Assert.That(result.MissingEvidenceIds, Is.EqualTo(new[] { "C-04" }));
            Assert.That(result.UnusableEvidenceIds, Is.Empty);
            Assert.That(result.CanUnlock, Is.False);
        }

        [Test]
        public void CompleteChain_UnlocksPermanentDeduction()
        {
            evidence.UnionWith(new[] { "C-07", "C-08" });

            Assert.That(service.TryUnlock("body_insertion"), Is.True);

            Assert.That(state.HasUnlockedDeduction("body_insertion"), Is.True);
            Assert.That(service.TryUnlock("body_insertion"), Is.False);
        }

        [Test]
        public void EveryCompletedChain_CanRemainUnlockedAtTheSameTime()
        {
            foreach (CanonicalDeductionDefinition definition in CanonicalDeductionCatalog.All)
            {
                evidence.UnionWith(definition.RequiredEvidenceIds);
                Assert.That(service.TryUnlock(definition.Id), Is.True);
            }

            Assert.That(
                state.UnlockedDeductionIds,
                Is.EquivalentTo(CanonicalDeductionCatalog.All.Select(item => item.Id)));
        }

        [Test]
        public void UnlockedDeductions_RestoreAsPermanentProgress()
        {
            evidence.UnionWith(new[] { "C-06", "C-12", "C-17" });
            service.EvaluateAndUnlockAll();

            Object.DestroyImmediate(host);
            host = new GameObject("RestoredCanonicalDeductions");
            state = host.AddComponent<GameStateManager>();
            state.ReloadSavedState();

            Assert.That(state.HasUnlockedDeduction("actual_murder"), Is.True);
            Assert.That(state.HasUnlockedDeduction("past_event"), Is.True);
            Assert.That(state.UnlockedDeductionIds.Count, Is.EqualTo(2));
        }

        [Test]
        public void PositiveIntegrity_DoesNotInventAnIntermediateThreshold()
        {
            evidence.UnionWith(new[] { "C-03", "C-04", "C-05" });
            state.ChangeEvidenceIntegrity(-99);

            DeductionEvaluation result = service.Evaluate("scene_denial");

            Assert.That(state.EvidenceIntegrity, Is.EqualTo(1));
            Assert.That(result.CanUnlock, Is.True);
            Assert.That(result.UnusableEvidenceIds, Is.Empty);
        }

        [Test]
        public void ZeroIntegrity_BlocksChainsThatNeedDirectEvidence()
        {
            evidence.UnionWith(new[] { "C-03", "C-04", "C-05" });
            state.ChangeEvidenceIntegrity(-100);

            DeductionEvaluation result = service.Evaluate("scene_denial");

            Assert.That(result.HasAllEvidence, Is.True);
            Assert.That(result.IsReliable, Is.False);
            Assert.That(
                result.UnusableEvidenceIds,
                Is.EqualTo(new[] { "C-03", "C-04", "C-05" }));
            Assert.That(service.TryUnlock("scene_denial"), Is.False);
        }

        [Test]
        public void ZeroIntegrity_DoesNotRewriteIndirectEvidencePolicy()
        {
            evidence.Add("C-17");
            state.ChangeEvidenceIntegrity(-100);

            DeductionEvaluation result = service.Evaluate("past_event");

            Assert.That(result.CanUnlock, Is.True);
            Assert.That(result.UnusableEvidenceIds, Is.Empty);
            Assert.That(service.TryUnlock("past_event"), Is.True);
            Assert.That(state.HasFlag("bad_end_integrity"), Is.True);
        }

        [Test]
        public void EvaluateAndUnlockAll_ReturnsOnlyNewlyCompletedChains()
        {
            evidence.UnionWith(new[] { "C-07", "C-08", "C-17" });

            Assert.That(
                service.EvaluateAndUnlockAll(),
                Is.EquivalentTo(new[] { "body_insertion", "past_event" }));
            Assert.That(service.EvaluateAndUnlockAll(), Is.Empty);
        }

        [Test]
        public void Presentation_DistinguishesMissingAndCompletedStates()
        {
            DeductionEvaluation missing = service.Evaluate("scene_denial");
            EvidenceTheoryView missingView =
                EvidenceTheoryPresentation.Create(missing, false);
            EvidenceTheoryView unlockedView =
                EvidenceTheoryPresentation.Create(missing, true);

            Assert.That(
                missingView.State,
                Is.EqualTo(EvidenceTheoryState.MissingEvidence));
            Assert.That(
                EvidenceTheoryPresentation.StateLabel(missingView),
                Does.Contain("C-03"));
            Assert.That(
                unlockedView.State,
                Is.EqualTo(EvidenceTheoryState.Unlocked));
            Assert.That(
                EvidenceTheoryPresentation.StateLabel(unlockedView),
                Is.EqualTo("추론 완료"));
        }

        [Test]
        public void Presentation_ExposesIntegrityFailureWithoutColorOnlyMeaning()
        {
            evidence.UnionWith(new[] { "C-03", "C-04", "C-05" });
            state.ChangeEvidenceIntegrity(-100);

            EvidenceTheoryView view = EvidenceTheoryPresentation.Create(
                service.Evaluate("scene_denial"),
                false);

            Assert.That(
                view.State,
                Is.EqualTo(EvidenceTheoryState.UnreliableEvidence));
            Assert.That(
                EvidenceTheoryPresentation.ButtonLabel(view),
                Does.Contain("증거 훼손으로 사용 불가"));
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
