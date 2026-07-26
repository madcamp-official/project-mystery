using System;
using System.Collections.Generic;
using System.Linq;
using NUnit.Framework;
using UnityEditor;
using UnityEngine;
using Wake.Core;
using Wake.Narrative;

namespace Wake.Tests
{
    public sealed class OfficialProductionFlowRegressionTests
    {
        private const string SaveKey = "UNDER_THE_HORIZON_GAME_STATE_V2";
        private const string DialoguePath =
            "Assets/_Project/Content/Dialogue/" +
            "Under_the_Horizon_Dialogue_KR.csv";

        private IReadOnlyList<DialogueRecord> records;
        private GameObject host;
        private GameStateManager state;

        [OneTimeSetUp]
        public void LoadOfficialDialogue()
        {
            TextAsset asset =
                AssetDatabase.LoadAssetAtPath<TextAsset>(DialoguePath);
            Assert.That(asset, Is.Not.Null, DialoguePath);
            DialogueCsvParseResult parsed =
                DialogueCsvParser.Parse(asset.text);
            Assert.That(
                parsed.Success,
                Is.True,
                string.Join("\n", parsed.Errors));
            records = parsed.Records;
        }

        [SetUp]
        public void SetUp()
        {
            DestroyManager();
            PlayerPrefs.DeleteKey(SaveKey);
            host = new GameObject("OfficialProductionFlowRegressionTests");
            state = host.AddComponent<GameStateManager>();
            state.StartNewGame();
            state.TryRecordFinalEnding(
                FinalAccusationResolver.CompleteEndingId);
        }

        [TearDown]
        public void TearDown()
        {
            DestroyManager();
            PlayerPrefs.DeleteKey(SaveKey);
        }

        [Test]
        public void OfficialSource_HasExpectedRuntimeShape()
        {
            Assert.That(
                records.Count,
                Is.EqualTo(
                    OfficialDialogueContractValidator.ExpectedDialogueCount));
            Assert.That(
                records.Select(record => record.SceneId)
                    .Distinct(StringComparer.Ordinal)
                    .Count(),
                Is.EqualTo(
                    OfficialDialogueContractValidator.ExpectedSceneCount));
            Assert.That(
                records.Count(record =>
                    !string.IsNullOrWhiteSpace(record.ChoiceId)),
                Is.EqualTo(
                    OfficialDialogueContractValidator.ExpectedChoiceCount));
        }

        [Test]
        public void EveryScheduledScene_CanReachAStableEndState()
        {
            var sceneIds = ProductionSceneCatalog.All
                .Select(scene => scene.SceneId)
                .ToHashSet(StringComparer.Ordinal);
            var reached = new List<string>();

            foreach (ProductionSceneDefinition scene in
                     ProductionSceneCatalog.All)
            {
                var prerequisitesSatisfied = sceneIds
                    .Where(sceneId => sceneId != scene.SceneId)
                    .ToHashSet(StringComparer.Ordinal);
                var flow = new ProductionDialogueFlow(
                    records,
                    prerequisitesSatisfied,
                    state,
                    _ => true);

                Assert.That(
                    flow.StartScene(scene.SceneId),
                    Is.True,
                    $"{scene.SceneId}: {string.Join("; ", flow.Warnings)}");
                RunToStableEnd(flow, scene.SceneId);
                Assert.That(
                    flow.Phase,
                    Is.EqualTo(ProductionScenePhase.Completed)
                        .Or.EqualTo(ProductionScenePhase.InteractionPending),
                    scene.SceneId);
                Assert.That(
                    flow.Warnings,
                    Is.Empty,
                    $"{scene.SceneId}: {string.Join("; ", flow.Warnings)}");
                reached.Add(scene.SceneId);
            }

            Assert.That(
                reached,
                Is.EqualTo(
                    ProductionSceneCatalog.All.Select(scene => scene.SceneId)));
        }

        [Test]
        public void EveryScene_KeepsChoicesWithinRuntimeCapacity()
        {
            foreach (IGrouping<string, DialogueRecord> scene in records
                         .GroupBy(record => record.SceneId))
            {
                int largestGroup = scene
                    .Select((record, index) => new { record, index })
                    .Where(item => item.record.Speaker == "PLAYER_CHOICE")
                    .GroupBy(item => ConsecutiveChoiceGroup(scene, item.index))
                    .Select(group => group.Count())
                    .DefaultIfEmpty(0)
                    .Max();

                Assert.That(
                    largestGroup,
                    Is.LessThanOrEqualTo(ProductionDialogueFlow.ChoiceCapacity),
                    scene.Key);
            }
        }

        [Test]
        public void ScheduleValidator_AcceptsRichBranchConditionsAndUiDirections()
        {
            IReadOnlyList<SceneScheduleDiagnostic> diagnostics =
                ProductionSceneScheduleValidator.Validate(records);

            Assert.That(
                diagnostics,
                Is.Empty,
                string.Join(
                    "\n",
                    diagnostics.Select(item =>
                        $"{item.SceneId}: {item.Message}")));
        }

        [Test]
        public void RegisteredInteractionScenes_StopAtTheirPendingHandler()
        {
            foreach (ProductionSceneCompletionRequirement requirement in
                     ProductionSceneCompletionCatalog.All)
            {
                ProductionDialogueFlow flow =
                    CreateFlowWithSatisfiedPrerequisites(
                        requirement.SceneId);

                Assert.That(
                    flow.StartScene(requirement.SceneId),
                    Is.True,
                    requirement.SceneId);
                RunToStableEnd(flow, requirement.SceneId);

                Assert.That(
                    flow.Phase,
                    Is.EqualTo(ProductionScenePhase.InteractionPending),
                    requirement.SceneId);
                Assert.That(
                    flow.PendingInteractionId,
                    Is.EqualTo(requirement.InteractionId),
                    requirement.SceneId);
            }
        }

        [Test]
        public void ScenesWithoutHandlers_CompleteThroughDialogue()
        {
            HashSet<string> interactionScenes =
                ProductionSceneCompletionCatalog.All
                    .Select(requirement => requirement.SceneId)
                    .ToHashSet(StringComparer.Ordinal);

            foreach (ProductionSceneDefinition scene in
                     ProductionSceneCatalog.All.Where(scene =>
                         !interactionScenes.Contains(scene.SceneId)))
            {
                ProductionDialogueFlow flow =
                    CreateFlowWithSatisfiedPrerequisites(scene.SceneId);

                Assert.That(
                    flow.StartScene(scene.SceneId),
                    Is.True,
                    scene.SceneId);
                RunToStableEnd(flow, scene.SceneId);

                Assert.That(
                    flow.Phase,
                    Is.EqualTo(ProductionScenePhase.Completed),
                    scene.SceneId);
                Assert.That(
                    flow.PendingInteractionId,
                    Is.Empty,
                    scene.SceneId);
            }
        }

        [Test]
        public void EveryScene_UsesContiguousOneBasedOrderValues()
        {
            foreach (IGrouping<string, DialogueRecord> scene in records
                         .GroupBy(record => record.SceneId))
            {
                int[] orders = scene
                    .OrderBy(record => record.Order)
                    .Select(record => record.Order)
                    .ToArray();

                Assert.That(
                    orders,
                    Is.EqualTo(Enumerable.Range(1, orders.Length)),
                    scene.Key);
            }
        }

        private static void RunToStableEnd(
            ProductionDialogueFlow flow,
            string sceneId)
        {
            int guard = 0;
            while (!flow.IsComplete && guard++ < 2000)
            {
                if (flow.IsAwaitingChoice)
                {
                    Assert.That(
                        flow.SelectChoice(0),
                        Is.True,
                        sceneId);
                }
                else
                {
                    flow.Advance();
                }
            }

            Assert.That(
                guard,
                Is.LessThan(2000),
                $"{sceneId} did not reach a stable end state.");
        }

        private static int ConsecutiveChoiceGroup(
            IEnumerable<DialogueRecord> scene,
            int targetIndex)
        {
            DialogueRecord[] rows = scene.ToArray();
            int start = targetIndex;
            while (start > 0 &&
                   rows[start - 1].Speaker == "PLAYER_CHOICE")
            {
                start--;
            }
            return start;
        }

        private ProductionDialogueFlow CreateFlowWithSatisfiedPrerequisites(
            string currentSceneId)
        {
            HashSet<string> completed = ProductionSceneCatalog.All
                .Select(scene => scene.SceneId)
                .Where(sceneId => sceneId != currentSceneId)
                .ToHashSet(StringComparer.Ordinal);
            return new ProductionDialogueFlow(
                records,
                completed,
                state,
                _ => true);
        }

        private void DestroyManager()
        {
            if (host != null)
            {
                UnityEngine.Object.DestroyImmediate(host);
            }
            if (GameStateManager.Instance != null)
            {
                UnityEngine.Object.DestroyImmediate(
                    GameStateManager.Instance.gameObject);
            }
            host = null;
            state = null;
        }
    }
}
