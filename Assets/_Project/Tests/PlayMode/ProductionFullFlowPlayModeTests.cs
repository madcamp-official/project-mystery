using System.Collections;
using System.Collections.Generic;
using System.Linq;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;
using Wake.Core;
using Wake.Evidence;
using Wake.Narrative;
using Wake.Puzzles;
using Wake.UI;

namespace Wake.Tests.PlayMode
{
    public sealed class ProductionFullFlowPlayModeTests
    {
        private const string SaveKey = "THE_WAKE_GAME_STATE_V1";

        private GameObject host;
        private GameStateManager state;
        private EvidenceInventory inventory;

        [UnitySetUp]
        public IEnumerator SetUp()
        {
            yield return DestroyRuntime();
            PlayerPrefs.DeleteKey(SaveKey);
            PlayerPrefs.Save();
            CreateRuntime("ProductionFullFlow");
            yield return null;
        }

        [UnityTearDown]
        public IEnumerator TearDown()
        {
            yield return DestroyRuntime();
            PlayerPrefs.DeleteKey(SaveKey);
            PlayerPrefs.Save();
        }

        [UnityTest]
        public IEnumerator NewGameAndDialogueCheckpoint_RestoreAcrossFrames()
        {
            state.StartNewGame();
            var firstPlayer = new RecordingScenePlayer();
            var firstDirector = new ProductionSceneDirector(state, firstPlayer);
            ProductionSceneEntry firstEntry = default;
            firstDirector.SceneEntered += entry => firstEntry = entry;

            Assert.That(firstDirector.StartNewGame(), Is.True);
            Assert.That(firstPlayer.StartedScenes, Is.EqualTo(new[] { "P-01" }));
            Assert.That(firstEntry.SceneId, Is.EqualTo("P-01"));
            Assert.That(firstEntry.Restored, Is.False);
            Assert.That(firstEntry.Objective, Is.Not.Empty);

            Assert.That(
                state.SaveDialogueCheckpoint(
                    "d2-02",
                    3,
                    false,
                    ProductionPuzzleCatalog.BloodPattern),
                Is.True);
            yield return RecreateRuntime("RestoredCheckpoint");

            var restoredPlayer = new RecordingScenePlayer();
            var restoredDirector =
                new ProductionSceneDirector(state, restoredPlayer);
            ProductionSceneEntry restoredEntry = default;
            restoredDirector.SceneEntered += entry => restoredEntry = entry;

            Assert.That(restoredDirector.ResumeGame(), Is.True);
            Assert.That(restoredPlayer.RestoredCheckpoint, Is.Not.Null);
            Assert.That(
                restoredPlayer.RestoredCheckpoint.activeSceneId,
                Is.EqualTo("D2-02"));
            Assert.That(restoredPlayer.RestoredCheckpoint.lineIndex, Is.EqualTo(3));
            Assert.That(restoredEntry.SceneId, Is.EqualTo("D2-02"));
            Assert.That(restoredEntry.Restored, Is.True);
            Assert.That(state.Day, Is.EqualTo(2));
        }

        [UnityTest]
        public IEnumerator EvidenceAndDeductions_RestoreWithoutDuplicateEvents()
        {
            var collectedEvents = new List<string>();
            void OnEvent(InvestigationEvent value)
            {
                if (value.Kind == InvestigationEventKind.EvidenceCollected)
                {
                    collectedEvents.Add(value.SubjectId);
                }
            }

            InvestigationEventHub.Published += OnEvent;
            try
            {
                foreach (CanonicalEvidenceEntry entry in
                         CanonicalEvidenceCatalog.All)
                {
                    Assert.That(inventory.TryAddById(entry.Id), Is.True);
                }

                Assert.That(inventory.TryAddById("C-01"), Is.False);
                Assert.That(collectedEvents, Has.Count.EqualTo(18));
                var deductions = new CanonicalDeductionService(
                    state,
                    inventory.Contains);

                Assert.That(
                    deductions.EvaluateAndUnlockAll(),
                    Has.Count.EqualTo(CanonicalDeductionCatalog.All.Count));
                Assert.That(deductions.TryActivate(
                    CanonicalDeductionCatalog.SceneDenial), Is.True);
                yield return null;

                yield return RecreateRuntime("RestoredEvidence");
                inventory.RestoreFromIds(state.CollectedEvidenceIds);
                var restoredDeductions = new CanonicalDeductionService(
                    state,
                    inventory.Contains);

                Assert.That(
                    inventory.Collected,
                    Has.Count.EqualTo(CanonicalEvidenceCatalog.All.Count));
                Assert.That(
                    CanonicalDeductionCatalog.All.All(
                        item => state.HasUnlockedDeduction(item.Id)),
                    Is.True);
                Assert.That(
                    restoredDeductions.EvaluateAndUnlockAll(),
                    Is.Empty);
                Assert.That(
                    state.IsTheoryActive(CanonicalDeductionCatalog.SceneDenial),
                    Is.True);
                Assert.That(collectedEvents, Has.Count.EqualTo(18));
            }
            finally
            {
                InvestigationEventHub.Published -= OnEvent;
            }
        }

        [UnityTest]
        public IEnumerator ProductionPuzzles_PersistSelectionsAndCompletion()
        {
            CollectEvidence("C-07", "C-08", "C-09", "C-10");
            ProductionPuzzleDefinition blood =
                ProductionPuzzleCatalog.All.Single(
                    item => item.Id == ProductionPuzzleCatalog.BloodPattern);
            var partial = new ProductionPuzzleSession(
                blood,
                state,
                inventory.Contains);

            Assert.That(partial.Select("no_spatter"), Is.True);
            Assert.That(partial.Select("center_mismatch"), Is.True);
            partial.SetStep(2);
            Assert.That(partial.UseHint(), Is.True);
            yield return RecreateRuntime("RestoredPuzzles");
            inventory.RestoreFromIds(state.CollectedEvidenceIds);

            var restored = new ProductionPuzzleSession(
                blood,
                state,
                inventory.Contains);
            Assert.That(
                restored.SelectedIds,
                Is.EquivalentTo(new[] { "no_spatter", "center_mismatch" }));
            Assert.That(restored.Step, Is.EqualTo(2));
            Assert.That(restored.HintLevel, Is.EqualTo(1));
            Assert.That(restored.Select("vertical_drop"), Is.True);
            Assert.That(restored.TryComplete().Completed, Is.True);
            Assert.That(state.HasCompletedScene("D2-02"), Is.True);

            ProductionPuzzleDefinition rail =
                ProductionPuzzleCatalog.All.Single(
                    item => item.Id == ProductionPuzzleCatalog.CargoRailBranch);
            var railSession = new ProductionPuzzleSession(
                rail,
                state,
                inventory.Contains);
            foreach (string selection in rail.RequiredSelectionIds)
            {
                Assert.That(railSession.Select(selection), Is.True);
            }

            Assert.That(railSession.TryComplete().Completed, Is.True);
            Assert.That(state.HasCompletedScene("D6-02"), Is.True);
            yield return null;
        }

        [UnityTest]
        public IEnumerator InterrogationAndOrpheus_CompleteAndRestore()
        {
            var marcus = new MarcusInterrogationSession(
                state,
                tryGrantEvidence: inventory.TryAddById);
            Assert.That(
                marcus.Ask(
                    MarcusInterrogationCatalog.AuthenticationQuestion,
                    MarcusAnswer.Yes),
                Is.EqualTo(MarcusQuestionResult.Recorded));
            Assert.That(marcus.Complete().Completed, Is.True);
            Assert.That(inventory.Contains("C-15"), Is.True);
            Assert.That(
                state.HasFlag(
                    MarcusInterrogationCatalog.AuthenticationFlag),
                Is.True);

            var orpheus = new OrpheusAudioRestorationSession(state);
            foreach (OrpheusRecordSegment segment in OrpheusRecordCatalog.All)
            {
                Assert.That(
                    orpheus.Move(
                        segment.LineId,
                        orpheus.OrderedLineIds.Count),
                    Is.True);
            }

            Assert.That(orpheus.TryComplete().Completed, Is.True);
            Assert.That(state.HasCompletedScene("D7-03"), Is.True);
            yield return RecreateRuntime("RestoredSessions");
            inventory.RestoreFromIds(state.CollectedEvidenceIds);

            var restoredMarcus =
                new MarcusInterrogationSession(state);
            var restoredOrpheus =
                new OrpheusAudioRestorationSession(state);
            Assert.That(restoredMarcus.IsCompleted, Is.True);
            Assert.That(restoredOrpheus.IsCompleted, Is.True);
            Assert.That(inventory.Contains("C-15"), Is.True);
            Assert.That(inventory.Contains("C-17"), Is.True);
        }

        [UnityTest]
        public IEnumerator SourceBackedTimeline_RemainsBlockedAndRestoresWork()
        {
            var timeline = new TimelinePuzzleSession(
                state,
                TimelinePuzzleCatalog.SourceBackedCards);
            for (int index = 0;
                 index < TimelinePuzzleCatalog.RequiredSequence.Count;
                 index++)
            {
                Assert.That(
                    timeline.Place(
                        TimelinePuzzleCatalog.RequiredSequence[index],
                        index),
                    Is.EqualTo(TimelinePlacementResult.Placed));
            }

            Assert.That(timeline.UseHint(), Is.True);
            TimelineCompletionResult blocked = timeline.TryComplete();
            Assert.That(blocked.Completed, Is.False);
            Assert.That(blocked.MissingCardCount, Is.EqualTo(7));
            Assert.That(
                blocked.Diagnostics,
                Has.Some.Contains("정확히 12장"));
            yield return RecreateRuntime("RestoredTimeline");

            var restored = new TimelinePuzzleSession(
                state,
                TimelinePuzzleCatalog.SourceBackedCards);
            Assert.That(restored.Placements, Has.Count.EqualTo(5));
            Assert.That(restored.HintLevel, Is.EqualTo(1));
            Assert.That(restored.IsCompleted, Is.False);
            Assert.That(state.HasCompletedScene("D6-05"), Is.False);
        }

        [UnityTest]
        public IEnumerator CompleteInvestigation_PersistsEndingAndEpilogue()
        {
            state.StartNewGame();
            foreach (CanonicalEvidenceEntry entry in CanonicalEvidenceCatalog.All)
            {
                Assert.That(inventory.TryAddById(entry.Id), Is.True);
            }

            var deductions = new CanonicalDeductionService(
                state,
                inventory.Contains);
            Assert.That(
                deductions.EvaluateAndUnlockAll(),
                Has.Count.EqualTo(CanonicalDeductionCatalog.All.Count));

            var accusation = CreateCorrectAccusation(discloseCoverup: true);
            FinalAccusationSubmission result = accusation.Submit();
            Assert.That(result.Submitted, Is.True);
            Assert.That(result.Result.Ending, Is.EqualTo(FinalEnding.Complete));
            Assert.That(
                ProductionEndingCatalog.GetNextDialogueScene(
                    result.Result.EndingId,
                    false,
                    false),
                Is.EqualTo("D8-02"));
            Assert.That(state.HasCompletedScene("D8-01"), Is.True);

            state.RecordCompletedScene(ProductionEndingCatalog.ConfessionSceneId);
            state.RecordCompletedScene(ProductionEndingCatalog.EpilogueSceneId);
            yield return RecreateRuntime("RestoredEnding");

            Assert.That(
                state.FinalEndingId,
                Is.EqualTo(FinalAccusationResolver.CompleteEndingId));
            Assert.That(state.HasCompletedScene("D8-01"), Is.True);
            Assert.That(state.HasCompletedScene("D8-02"), Is.True);
            Assert.That(state.HasCompletedScene("D8-03"), Is.True);
            Assert.That(
                ProductionEndingCatalog.GetNextDialogueScene(
                    state.FinalEndingId,
                    state.HasCompletedScene("D8-02"),
                    state.HasCompletedScene("D8-03")),
                Is.Empty);

            var restored = new FinalAccusationSession(state);
            Assert.That(restored.Submit().Result.Ending, Is.EqualTo(
                FinalEnding.Complete));
        }

        [UnityTest]
        public IEnumerator PanicAndIntegrityThresholds_OverrideCorrectAnswers()
        {
            UnlockAllDeductionsDirectly();
            state.ChangePublicAnxiety(85);
            FinalAccusationSubmission panic =
                CreateCorrectAccusation(discloseCoverup: false).Submit();

            Assert.That(panic.Submitted, Is.True);
            Assert.That(panic.Result.Ending, Is.EqualTo(FinalEnding.BadPanic));
            Assert.That(
                state.FinalEndingId,
                Is.EqualTo(FinalAccusationResolver.PanicEndingId));
            yield return RecreateRuntime("IntegrityThreshold");

            PlayerPrefs.DeleteKey(SaveKey);
            Object.Destroy(host);
            yield return null;
            CreateRuntime("IntegrityThresholdFresh");
            UnlockAllDeductionsDirectly();
            state.ChangeEvidenceIntegrity(-100);
            FinalAccusationSubmission integrity =
                CreateCorrectAccusation(discloseCoverup: false).Submit();

            Assert.That(integrity.Submitted, Is.True);
            Assert.That(
                integrity.Result.Ending,
                Is.EqualTo(FinalEnding.BadIntegrity));
            Assert.That(
                state.FinalEndingId,
                Is.EqualTo(FinalAccusationResolver.IntegrityEndingId));
        }

        private FinalAccusationSession CreateCorrectAccusation(
            bool discloseCoverup)
        {
            var session = new FinalAccusationSession(state);
            session.Update(
                AccusedPerson.Evelyn,
                MurderLocation.BallastControlAnnex,
                MurderMethod.NitrogenSuffocation,
                BodyTransport.CeilingServiceRail,
                DanielTargetBelief.Richard,
                OrpheusEventDesign.InsuranceFraud,
                discloseCoverup);
            return session;
        }

        private void CollectEvidence(params string[] evidenceIds)
        {
            foreach (string evidenceId in evidenceIds)
            {
                Assert.That(inventory.TryAddById(evidenceId), Is.True);
            }
        }

        private void UnlockAllDeductionsDirectly()
        {
            foreach (CanonicalDeductionDefinition definition in
                     CanonicalDeductionCatalog.All)
            {
                Assert.That(state.UnlockDeduction(definition.Id), Is.True);
            }
        }

        private void CreateRuntime(string name)
        {
            host = new GameObject(name);
            state = host.AddComponent<GameStateManager>();
            inventory = host.AddComponent<EvidenceInventory>();
            inventory.BindState(state);
        }

        private IEnumerator RecreateRuntime(string name)
        {
            Object.Destroy(host);
            yield return null;
            CreateRuntime(name);
            yield return null;
        }

        private static IEnumerator DestroyRuntime()
        {
            if (GameStateManager.Instance != null)
            {
                Object.Destroy(GameStateManager.Instance.gameObject);
                yield return null;
            }
        }

        private sealed class RecordingScenePlayer : IProductionScenePlayer
        {
            public string ActiveProductionSceneId { get; private set; } =
                string.Empty;

            public List<string> StartedScenes { get; } = new();
            public ProductionDialogueCheckpoint RestoredCheckpoint { get; private set; }

            public bool StartProductionScene(string sceneId)
            {
                ActiveProductionSceneId = sceneId;
                StartedScenes.Add(sceneId);
                return true;
            }

            public bool RestoreProductionScene(
                ProductionDialogueCheckpoint checkpoint)
            {
                RestoredCheckpoint = checkpoint?.Copy();
                ActiveProductionSceneId =
                    RestoredCheckpoint?.activeSceneId ?? string.Empty;
                return RestoredCheckpoint != null;
            }
        }
    }
}
