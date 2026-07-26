using System;
using System.Collections.Generic;
using System.Linq;
using NUnit.Framework;
using UnityEditor;
using UnityEngine;
using Wake.Core;
using Wake.Evidence;
using Wake.Exploration;
using Wake.Narrative;
using Wake.Puzzles;
using Wake.UI;

namespace Wake.Tests
{
    public class ProductionInvestigationFlowSmokeTests
    {
        private const string SaveKey = "UNDER_THE_HORIZON_GAME_STATE_V2";
        private const string DialoguePath =
            "Assets/_Project/Content/Dialogue/Under_the_Horizon_Dialogue_KR.csv";
        private const string LocationFolder =
            "Assets/_Project/Content/Locations";

        private IReadOnlyList<DialogueRecord> records;
        private GameObject host;
        private GameStateManager state;
        private EvidenceInventory inventory;

        [OneTimeSetUp]
        public void LoadProductionContent()
        {
            TextAsset csv = AssetDatabase.LoadAssetAtPath<TextAsset>(DialoguePath);
            Assert.That(csv, Is.Not.Null, DialoguePath);
            DialogueCsvParseResult parsed = DialogueCsvParser.Parse(csv.text);
            Assert.That(parsed.Success, Is.True, string.Join("\n", parsed.Errors));
            records = parsed.Records;
        }

        [SetUp]
        public void SetUp()
        {
            DestroyManager();
            PlayerPrefs.DeleteKey(SaveKey);
            host = new GameObject("ProductionInvestigationFlowSmokeTests");
            state = host.AddComponent<GameStateManager>();
            inventory = host.AddComponent<EvidenceInventory>();
            inventory.BindState(state);
        }

        [TearDown]
        public void TearDown()
        {
            DestroyManager();
            PlayerPrefs.DeleteKey(SaveKey);
        }

        [Test]
        public void ProductionCsv_PreservesRowsScenesStableIdsAndKorean()
        {
            Assert.That(records, Has.Count.EqualTo(1063));
            Assert.That(
                records.Select(item => item.SceneId).Distinct().Count(),
                Is.EqualTo(41));
            Assert.That(
                records.Select(item => item.StableLineId).Distinct().Count(),
                Is.EqualTo(1063));
            Assert.That(
                records.Any(item => item.StableLineId == "p_01_01"),
                Is.True);
            Assert.That(
                records.Any(item => item.StableLineId == "d7_03_04"),
                Is.True);
            Assert.That(
                records.All(item =>
                    !item.TextKo.Contains('\uFFFD') &&
                    !item.TextKo.Contains("占쏙옙")),
                Is.True);
        }

        [Test]
        public void DialogueAndScheduleValidators_AcceptCanonicalContent()
        {
            TextAsset csv = AssetDatabase.LoadAssetAtPath<TextAsset>(DialoguePath);
            DialogueValidationReport dialogue =
                DialogueContentValidator.Validate(csv.text);
            IReadOnlyList<SceneScheduleDiagnostic> schedule =
                ProductionSceneScheduleValidator.Validate(records);

            Assert.That(dialogue.ErrorCount, Is.Zero,
                string.Join("\n", dialogue.Diagnostics));
            Assert.That(schedule, Is.Empty,
                string.Join("\n", schedule.Select(item => item.Message)));
            Assert.That(
                ProductionSceneCatalog.All.Select(item => item.SceneId),
                Is.EquivalentTo(records.Select(item => item.SceneId).Distinct()));
        }

        [Test]
        public void PrologueFlow_AdvancesInOrderAndPersistsNextSceneGate()
        {
            var flow = new ProductionDialogueFlow(records, null, state);
            Assert.That(flow.StartScene("P-02"), Is.False);
            Assert.That(
                flow.GetMissingPrerequisites("P-02"),
                Is.EqualTo(new[] { "P-01" }));

            CompleteScene(flow, "P-01");
            Assert.That(state.HasCompletedScene("P-01"), Is.True);

            state.ReloadSavedState();
            var restored = new ProductionDialogueFlow(records, null, state);
            Assert.That(restored.CanStartScene("P-02"), Is.True);
            Assert.That(restored.StartScene("P-02"), Is.True);
        }

        [Test]
        public void DuplicateEvidenceAndSceneProgress_AreIdempotent()
        {
            state.RecordEvidenceCollected("C-07");
            state.RecordEvidenceCollected("c_07");
            state.RecordEvidenceCollected(" C-07 ");
            state.RecordCompletedScene("D2-02");
            state.RecordCompletedScene(" d2-02 ");

            Assert.That(
                state.CollectedEvidenceIds.Count(id => id == "C-07"),
                Is.EqualTo(1));
            Assert.That(
                state.CompletedProductionSceneIds.Count(id => id == "D2-02"),
                Is.EqualTo(1));
        }

        [Test]
        public void BloodPuzzle_ToMarcus_ToOrpheus_ProducesTypedState()
        {
            state.RecordEvidenceCollected("C-07");
            ProductionPuzzleCatalog.TryGet(
                ProductionPuzzleCatalog.BloodPattern,
                out ProductionPuzzleDefinition bloodDefinition);
            var blood = new ProductionPuzzleSession(
                bloodDefinition,
                state,
                id => state.CollectedEvidenceIds.Contains(id));
            foreach (string selection in bloodDefinition.RequiredSelectionIds)
            {
                blood.Select(selection);
            }

            Assert.That(blood.TryComplete().Completed, Is.True);

            var marcus = new MarcusInterrogationSession(
                state,
                tryGrantEvidence: id => inventory.TryAddById(id));
            marcus.Ask(
                MarcusInterrogationCatalog.AuthenticationQuestion,
                MarcusAnswer.Yes);
            Assert.That(marcus.Complete().Completed, Is.True);

            var orpheus = new OrpheusAudioRestorationSession(state);
            foreach (OrpheusRecordSegment segment in OrpheusRecordCatalog.All)
            {
                orpheus.Move(segment.LineId, orpheus.OrderedLineIds.Count);
            }
            Assert.That(orpheus.TryComplete().Completed, Is.True);

            Assert.That(state.CollectedEvidenceIds,
                Does.Contain(MarcusInterrogationCatalog.AuthenticationEvidence));
            Assert.That(state.CollectedEvidenceIds,
                Does.Contain(OrpheusRecordCatalog.EvidenceId));
            Assert.That(state.HasFlag("past_culprit_confirmed"), Is.True);
        }

        [Test]
        public void IncompleteTimelineAndMissingAudio_RemainVisibleDiagnostics()
        {
            var timeline = new TimelinePuzzleSession(
                state,
                TimelinePuzzleCatalog.SourceBackedCards);
            TimelineCompletionResult timelineResult = timeline.TryComplete();
            IReadOnlyList<string> audioDiagnostics =
                OrpheusRecordValidator.Validate(OrpheusRecordCatalog.All);

            Assert.That(timelineResult.Completed, Is.False);
            Assert.That(timelineResult.MissingCardCount, Is.EqualTo(12));
            Assert.That(
                timelineResult.Diagnostics,
                Has.Some.Contains("정확히 12장"));
            Assert.That(
                audioDiagnostics.Count(message => message.Contains("AudioClip 없음")),
                Is.EqualTo(4));
        }

        [Test]
        public void FinalAccusation_PersistsCompleteEndingAfterTypedFlow()
        {
            foreach (CanonicalDeductionDefinition definition in
                     CanonicalDeductionCatalog.All)
            {
                state.UnlockDeduction(definition.Id);
            }

            var final = new FinalAccusationSession(state);
            final.Update(
                AccusedPerson.Evelyn,
                MurderLocation.BallastControlAnnex,
                MurderMethod.NitrogenSuffocation,
                BodyTransport.CeilingServiceRail,
                DanielTargetBelief.Misconception,
                OrpheusEventDesign.Evelyn,
                true);
            FinalAccusationSubmission submission = default;
            for (int stage = 0;
                 stage <= FinalAccusationStageCatalog.All.Count &&
                 !submission.Submitted;
                 stage++)
            {
                submission = final.Submit();
            }

            Assert.That(submission.Submitted, Is.True);
            Assert.That(submission.Result.Ending, Is.EqualTo(FinalEnding.Complete));
            Assert.That(
                state.FinalEndingId,
                Is.EqualTo(FinalAccusationResolver.CompleteEndingId));

            RecreateManager();
            Assert.That(
                state.FinalEndingId,
                Is.EqualTo(FinalAccusationResolver.CompleteEndingId));
            Assert.That(
                FinalAccusationResolver.OpensD8Confession(state.FinalEndingId),
                Is.True);
        }

        [Test]
        public void FullGameState_RoundTripsWithoutLosingInvestigationProgress()
        {
            state.SetTime(6, TimeBlock.NIGHT);
            state.ChangeTrust("CLAIRE", 2);
            state.ChangePublicAnxiety(40);
            state.ChangeEvidenceIntegrity(-25);
            state.AddFlag("service_rail_access");
            state.RecordEvidenceCollected("C-10");
            state.RecordCompletedScene("D6-02");
            state.RecordCompletedObjective("inspect_service_rail");
            state.UnlockDeduction(CanonicalDeductionCatalog.TransportRoute);
            state.RecordLocation("SERVICE_RAIL");
            state.SavePuzzleSession(new PuzzleSessionState
            {
                puzzleId = ProductionPuzzleCatalog.CargoRailBranch,
                selectedIds = new List<string> { "weight_86kg" },
                step = 2,
                hintLevel = 1
            });

            RecreateManager();

            Assert.That(state.Day, Is.EqualTo(6));
            Assert.That(state.CurrentTimeBlock, Is.EqualTo(TimeBlock.NIGHT));
            Assert.That(state.GetTrust("CLAIRE"), Is.EqualTo(4));
            Assert.That(state.PublicAnxiety, Is.EqualTo(55));
            Assert.That(state.EvidenceIntegrity, Is.EqualTo(75));
            Assert.That(state.HasFlag("service_rail_access"), Is.True);
            Assert.That(state.CollectedEvidenceIds, Does.Contain("C-10"));
            Assert.That(state.HasCompletedScene("D6-02"), Is.True);
            Assert.That(
                state.HasCompletedObjective("inspect_service_rail"),
                Is.True);
            Assert.That(
                state.HasUnlockedDeduction(CanonicalDeductionCatalog.TransportRoute),
                Is.True);
            Assert.That(state.CurrentLocationCode, Is.EqualTo("SERVICE_RAIL"));
            Assert.That(
                state.TryGetPuzzleSession(
                    ProductionPuzzleCatalog.CargoRailBranch,
                    out PuzzleSessionState puzzle),
                Is.True);
            Assert.That(puzzle.selectedIds, Does.Contain("weight_86kg"));
            Assert.That(puzzle.hintLevel, Is.EqualTo(1));
        }

        [Test]
        public void LocationDiagnostics_KeepEightUnresolvedCodesAsWarnings()
        {
            LocationDefinition[] locations = LoadLocations();
            IReadOnlyList<LocationCatalogDiagnostic> diagnostics =
                CanonicalLocationCatalog.Validate(
                    locations,
                    ProductionSceneCatalog.All);
            string[] warnings = diagnostics
                .Where(item =>
                    item.Severity == LocationCatalogDiagnosticSeverity.Warning)
                .Select(item => item.Code)
                .OrderBy(value => value, StringComparer.Ordinal)
                .ToArray();

            Assert.That(warnings, Is.EqualTo(new[]
            {
                "BRIDGE",
                "CABIN_CLAIRE",
                "CABIN_DANIEL",
                "EVIDENCE_BOARD",
                "FORENSIC",
                "INTERVIEW",
                "SERVICE7",
                "STERN"
            }));
            Assert.That(
                diagnostics.Where(item =>
                    item.Severity == LocationCatalogDiagnosticSeverity.Error),
                Is.Empty);
        }

        [Test]
        public void KoreanFeedbackAndSelectionState_DoNotDependOnColor()
        {
            ProductionPuzzleCatalog.TryGet(
                ProductionPuzzleCatalog.BloodPattern,
                out ProductionPuzzleDefinition definition);
            IReadOnlyList<PuzzleSelectionView> views =
                ProductionPuzzlePresentation.CreateSelections(
                    definition,
                    new[] { "center_mismatch" },
                    0);

            Assert.That(
                views.Single(item => item.IsSelected).AccessibleLabel,
                Does.StartWith("선택됨:"));
            Assert.That(
                views.Where(item => !item.IsSelected)
                    .All(item => item.AccessibleLabel.StartsWith("선택 안 됨:")),
                Is.True);
            Assert.That(
                views.All(item =>
                    !item.Label.Contains('\uFFFD') &&
                    !item.AccessibleLabel.Contains("占쏙옙")),
                Is.True);
        }

        private static void CompleteScene(
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
                    flow.Advance();
                }
            }
        }

        private static LocationDefinition[] LoadLocations() =>
            AssetDatabase.FindAssets(
                    "t:LocationDefinition",
                    new[] { LocationFolder })
                .Select(AssetDatabase.GUIDToAssetPath)
                .Select(AssetDatabase.LoadAssetAtPath<LocationDefinition>)
                .OrderBy(item => item.LocationCode, StringComparer.Ordinal)
                .ToArray();

        private void RecreateManager()
        {
            UnityEngine.Object.DestroyImmediate(host);
            host = new GameObject("RestoredProductionFlow");
            state = host.AddComponent<GameStateManager>();
            state.ReloadSavedState();
        }

        private void DestroyManager()
        {
            if (GameStateManager.Instance != null)
            {
                UnityEngine.Object.DestroyImmediate(
                    GameStateManager.Instance.gameObject);
            }
            else if (host != null)
            {
                UnityEngine.Object.DestroyImmediate(host);
            }

            host = null;
        }
    }
}
