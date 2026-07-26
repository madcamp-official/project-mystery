using System.Collections.Generic;
using System.Linq;
using NUnit.Framework;
using UnityEditor;
using UnityEngine;
using Wake.Core;
using Wake.Narrative;
using Wake.Puzzles;

namespace Wake.Tests
{
    public class ProductionSceneCompletionTests
    {
        private const string SaveKey = "THE_WAKE_GAME_STATE_V1";
        private const string DialoguePath =
            "Assets/_Project/Content/Dialogue/The_Wake_Without_Footprints_Dialogue_KR.csv";

        private IReadOnlyList<DialogueRecord> records;
        private GameObject host;
        private GameStateManager state;

        [OneTimeSetUp]
        public void LoadProductionDialogue()
        {
            TextAsset csv = AssetDatabase.LoadAssetAtPath<TextAsset>(DialoguePath);
            Assert.That(csv, Is.Not.Null);
            records = DialogueCsvParser.Parse(csv.text).Records;
        }

        [SetUp]
        public void SetUp()
        {
            DestroyState();
            PlayerPrefs.DeleteKey(SaveKey);
            host = new GameObject("ProductionSceneCompletionTests");
            state = host.AddComponent<GameStateManager>();
        }

        [TearDown]
        public void TearDown()
        {
            DestroyState();
            PlayerPrefs.DeleteKey(SaveKey);
        }

        [Test]
        public void Catalog_RegistersEveryImplementedProductionInteraction()
        {
            Assert.That(
                ProductionSceneCompletionCatalog.All.Select(item => item.SceneId),
                Is.EquivalentTo(new[]
                {
                    "D2-01",
                    "D2-02",
                    "D4-04",
                    "D6-02",
                    "D6-05",
                    "D7-03",
                    "D8-01"
                }));
            Assert.That(
                ProductionSceneCompletionCatalog.All
                    .Select(item => item.InteractionId)
                    .Distinct()
                    .Count(),
                Is.EqualTo(7));
        }

        [Test]
        public void Catalog_ReportsPuzzleScenesWithoutRuntimeHandlers()
        {
            IReadOnlyList<ProductionSceneCompletionDiagnostic> diagnostics =
                ProductionSceneCompletionCatalog.Validate(
                    ProductionSceneCatalog.All);

            Assert.That(
                diagnostics.Select(item => item.SceneId),
                Is.EquivalentTo(new[] { "D4-03" }));
            Assert.That(
                diagnostics.All(item =>
                    item.Message.Contains("대사 완료로 진행")),
                Is.True);
        }

        [Test]
        public void ExitInspectionScene_WaitsForInteractionAfterDialogue()
        {
            var flow = CreateFlowWithPrerequisitesThrough("D2-01");

            FinishDialogue(flow, "D2-01");

            Assert.That(
                flow.Phase,
                Is.EqualTo(ProductionScenePhase.InteractionPending));
            Assert.That(
                flow.PendingInteractionId,
                Is.EqualTo(ProductionSceneCompletionCatalog.ExitInspectionInteraction));
            Assert.That(state.HasCompletedScene("D2-01"), Is.False);
            Assert.That(flow.CanStartScene("D2-02"), Is.False);
            Assert.That(flow.CanStartScene("D2-04"), Is.False);
            state.RecordCompletedScene("D2-01");
            Assert.That(flow.CanStartScene("D2-02"), Is.True);
            Assert.That(flow.CanStartScene("D2-04"), Is.True);
        }

        [Test]
        public void RegisteredPuzzle_DoesNotUnlockNextSceneAfterDialogue()
        {
            var flow = CreateFlowWithPrerequisitesThrough("D2-02");

            FinishDialogue(flow, "D2-02");

            Assert.That(
                flow.Phase,
                Is.EqualTo(ProductionScenePhase.InteractionPending));
            Assert.That(
                flow.PendingInteractionId,
                Is.EqualTo(ProductionSceneCompletionCatalog.BloodPatternInteraction));
            Assert.That(state.HasCompletedScene("D2-02"), Is.False);
            Assert.That(flow.CanStartScene("D2-03"), Is.False);
            Assert.That(
                flow.GetMissingPrerequisites("D2-03"),
                Is.EqualTo(new[] { "D2-02" }));
        }

        [Test]
        public void WrongInteraction_CannotCompletePendingScene()
        {
            var flow = CreateFlowWithPrerequisitesThrough("D2-02");
            FinishDialogue(flow, "D2-02");

            Assert.That(flow.CompletePendingInteraction("cargo_rail_branch"), Is.False);
            Assert.That(state.HasCompletedScene("D2-02"), Is.False);
            Assert.That(
                flow.Phase,
                Is.EqualTo(ProductionScenePhase.InteractionPending));
        }

        [Test]
        public void MatchingInteraction_CompletesAndUnlocksDependentScene()
        {
            var flow = CreateFlowWithPrerequisitesThrough("D2-02");
            FinishDialogue(flow, "D2-02");

            Assert.That(
                flow.CompletePendingInteraction(
                    ProductionSceneCompletionCatalog.BloodPatternInteraction),
                Is.True);

            Assert.That(flow.Phase, Is.EqualTo(ProductionScenePhase.Completed));
            Assert.That(state.HasCompletedScene("D2-02"), Is.True);
            Assert.That(flow.CanStartScene("D2-03"), Is.True);
        }

        [Test]
        public void RepeatedCompletion_IsIdempotent()
        {
            state.SaveDialogueCheckpoint(
                "D4-04",
                12,
                false,
                MarcusInterrogationCatalog.SessionId);
            int stateChanges = 0;
            int completionEvents = 0;
            string nextSceneId = string.Empty;
            state.StateChanged += () => stateChanges++;
            void Count(InvestigationEvent item)
            {
                if (item.Kind == InvestigationEventKind.SceneCompleted)
                {
                    completionEvents++;
                    nextSceneId = item.ContextId;
                }
            }

            InvestigationEventHub.Published += Count;
            try
            {
                Assert.That(
                    ProductionSceneCompletionGate.TryComplete(
                        state,
                        "D4-04",
                        MarcusInterrogationCatalog.SessionId),
                    Is.True);
                Assert.That(
                    ProductionSceneCompletionGate.TryComplete(
                        state,
                        " d4-04 ",
                        " MARCUS_INTERROGATION "),
                    Is.True);
            }
            finally
            {
                InvestigationEventHub.Published -= Count;
            }

            Assert.That(
                state.CompletedProductionSceneIds.Count(id => id == "D4-04"),
                Is.EqualTo(1));
            Assert.That(state.DialogueCheckpoint, Is.Null);
            Assert.That(stateChanges, Is.EqualTo(1));
            Assert.That(completionEvents, Is.EqualTo(1));
            Assert.That(nextSceneId, Is.EqualTo("D5-01"));
        }

        [Test]
        public void Completion_PreservesUnrelatedCheckpoint()
        {
            state.SaveDialogueCheckpoint("D3-05", 7, true, "other_interaction");

            Assert.That(
                ProductionSceneCompletionGate.TryComplete(
                    state,
                    "D2-02",
                    ProductionPuzzleCatalog.BloodPattern),
                Is.True);

            Assert.That(state.DialogueCheckpoint.activeSceneId, Is.EqualTo("D3-05"));
            Assert.That(state.DialogueCheckpoint.lineIndex, Is.EqualTo(7));
            Assert.That(state.DialogueCheckpoint.awaitingChoice, Is.True);
            Assert.That(
                state.DialogueCheckpoint.pendingInteractionId,
                Is.EqualTo("other_interaction"));
        }

        [Test]
        public void CompletionGate_RejectsUnknownSceneAndNullState()
        {
            Assert.That(
                ProductionSceneCompletionGate.TryComplete(
                    null,
                    "D2-02",
                    ProductionPuzzleCatalog.BloodPattern),
                Is.False);
            Assert.That(
                ProductionSceneCompletionGate.TryComplete(
                    state,
                    "D4-03",
                    "unregistered_reconstruction"),
                Is.False);
            Assert.That(state.CompletedProductionSceneIds, Is.Empty);
        }

        [Test]
        public void SavedInteractionCompletion_IsVisibleToNewFlow()
        {
            ProductionSceneCompletionGate.TryComplete(
                state,
                "D6-02",
                ProductionPuzzleCatalog.CargoRailBranch);
            state.ReloadSavedState();

            var restored = new ProductionDialogueFlow(records, null, state);

            Assert.That(restored.IsSceneCompleted("d6-02"), Is.True);
        }

        private ProductionDialogueFlow CreateFlowWithPrerequisitesThrough(
            string sceneId)
        {
            ProductionSceneDefinition target =
                ProductionSceneCatalog.All.Single(item => item.SceneId == sceneId);
            foreach (string prerequisite in target.Prerequisites)
            {
                state.RecordCompletedScene(prerequisite);
            }

            return new ProductionDialogueFlow(records, null, state);
        }

        private static void FinishDialogue(
            ProductionDialogueFlow flow,
            string sceneId)
        {
            Assert.That(flow.StartScene(sceneId), Is.True,
                string.Join("\n", flow.Warnings));
            while (!flow.IsComplete)
            {
                if (flow.IsAwaitingChoice)
                {
                    Assert.That(flow.SelectChoice(0), Is.True);
                }
                else
                {
                    Assert.That(flow.Current, Is.Not.Null);
                    flow.Advance();
                }
            }
        }

        private void DestroyState()
        {
            if (GameStateManager.Instance != null)
            {
                Object.DestroyImmediate(GameStateManager.Instance.gameObject);
            }
            else if (host != null)
            {
                Object.DestroyImmediate(host);
            }

            host = null;
            state = null;
        }
    }
}
