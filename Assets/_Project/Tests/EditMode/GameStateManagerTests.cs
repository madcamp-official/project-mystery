using System.Collections.Generic;
using NUnit.Framework;
using UnityEditor;
using UnityEngine;
using Wake.Core;
using Wake.Evidence;

namespace Wake.Tests
{
    public class GameStateManagerTests
    {
        private const string SaveKey = "UNDER_THE_HORIZON_GAME_STATE_V2";

        private GameObject host;
        private GameStateManager state;

        [SetUp]
        public void SetUp()
        {
            DestroyExistingManager();
            PlayerPrefs.DeleteKey(SaveKey);
            PlayerPrefs.DeleteKey("THE_WAKE_GAME_STATE_V1");
            state = CreateManager();
            state.StartNewGame();
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
            PlayerPrefs.DeleteKey("THE_WAKE_GAME_STATE_V1");
        }

        [Test]
        public void NewGame_UsesDocumentedDefaults()
        {
            Assert.That(state.Day, Is.EqualTo(1));
            Assert.That(state.CurrentTimeBlock, Is.EqualTo(TimeBlock.AM));
            Assert.That(state.GetTrust("DANIEL"), Is.EqualTo(2));
            Assert.That(state.PublicAnxiety, Is.EqualTo(15));
            Assert.That(state.EvidenceIntegrity, Is.EqualTo(100));
            Assert.That(state.UnlockedDeductionIds, Is.Empty);
        }

        [Test]
        public void Trust_ClampsToZeroAndFive()
        {
            state.ChangeTrust("DANIEL", 20);
            Assert.That(state.GetTrust("DANIEL"), Is.EqualTo(5));

            state.ChangeTrust("DANIEL", -20);
            Assert.That(state.GetTrust("DANIEL"), Is.Zero);
        }

        [Test]
        public void Trust_NormalizesCharacterId()
        {
            state.ChangeTrust("  DANIEL  ", 1);

            Assert.That(state.GetTrust("DANIEL"), Is.EqualTo(3));
            Assert.That(state.GetTrust("  DANIEL "), Is.EqualTo(3));
        }

        [Test]
        public void Trust_ReportsActualClampedDeltaInKorean()
        {
            string feedback = null;
            state.FeedbackRequested += message => feedback = message;

            state.ChangeTrust("DANIEL", 10);

            Assert.That(feedback, Is.EqualTo("DANIEL \uC2E0\uB8B0 +3"));
            AssertValidKorean(feedback);
        }

        [Test]
        public void Anxiety_ClampsToZeroAndOneHundred()
        {
            state.ChangePublicAnxiety(-1000);
            Assert.That(state.PublicAnxiety, Is.Zero);

            state.ChangePublicAnxiety(1000);
            Assert.That(state.PublicAnxiety, Is.EqualTo(100));
        }

        [Test]
        public void Anxiety_AtSeventy_ClosesRestrictedAreasOnce()
        {
            var thresholds = new List<StateThresholdKind>();
            state.StateThresholdReached += thresholds.Add;

            state.ChangePublicAnxiety(55);
            state.ChangePublicAnxiety(1);

            Assert.That(state.PublicAnxiety, Is.EqualTo(71));
            Assert.That(state.HasFlag("restricted_areas_closed"), Is.True);
            Assert.That(
                thresholds,
                Is.EqualTo(new[] { StateThresholdKind.PublicAnxietyRestriction }));
        }

        [Test]
        public void Anxiety_BelowSeventy_ReopensRestrictedAreas()
        {
            state.ChangePublicAnxiety(55);
            Assert.That(state.HasFlag("restricted_areas_closed"), Is.True);

            state.ChangePublicAnxiety(-1);

            Assert.That(state.PublicAnxiety, Is.EqualTo(69));
            Assert.That(state.HasFlag("restricted_areas_closed"), Is.False);
        }

        [Test]
        public void Anxiety_AtOneHundred_TriggersPanicBadEnd()
        {
            string badEnd = null;
            StateThresholdKind? threshold = null;
            state.BadEndTriggered += message => badEnd = message;
            state.StateThresholdReached += kind => threshold = kind;

            state.ChangePublicAnxiety(85);

            Assert.That(state.HasFlag("bad_end_panic"), Is.True);
            Assert.That(threshold, Is.EqualTo(StateThresholdKind.PublicAnxietyBadEnd));
            Assert.That(badEnd, Is.EqualTo("승객 불안이 100에 도달했습니다."));
            AssertValidKorean(badEnd);
        }

        [Test]
        public void Integrity_ClampsToZeroAndOneHundred()
        {
            state.ChangeEvidenceIntegrity(-1000);
            Assert.That(state.EvidenceIntegrity, Is.Zero);

            state.ChangeEvidenceIntegrity(1000);
            Assert.That(state.EvidenceIntegrity, Is.EqualTo(100));
        }

        [Test]
        public void Integrity_AtZero_TriggersEvidenceBadEnd()
        {
            string badEnd = null;
            StateThresholdKind? threshold = null;
            state.BadEndTriggered += message => badEnd = message;
            state.StateThresholdReached += kind => threshold = kind;

            state.ChangeEvidenceIntegrity(-100);

            Assert.That(state.HasFlag("bad_end_integrity"), Is.True);
            Assert.That(threshold, Is.EqualTo(StateThresholdKind.EvidenceIntegrityBadEnd));
            Assert.That(
                badEnd,
                Is.EqualTo("현장 보존도가 0이 되어 핵심 증거가 파괴되었습니다."));
            AssertValidKorean(badEnd);
        }

        [Test]
        public void ChoiceEffects_ApplyAllDeltasAndNotifyOnce()
        {
            int stateChanges = 0;
            state.StateChanged += () => stateChanges++;

            state.ApplyChoiceEffects("EVELYN", 2, 10, -25);

            Assert.That(state.GetTrust("EVELYN"), Is.EqualTo(4));
            Assert.That(state.PublicAnxiety, Is.EqualTo(25));
            Assert.That(state.EvidenceIntegrity, Is.EqualTo(75));
            Assert.That(stateChanges, Is.EqualTo(1));
        }

        [Test]
        public void Deductions_AllowEveryUniqueUnlockedEntry()
        {
            for (int index = 0; index < 8; index++)
            {
                Assert.That(state.UnlockDeduction($"deduction_{index}"), Is.True);
            }

            Assert.That(state.UnlockedDeductionIds, Has.Count.EqualTo(8));
            Assert.That(state.UnlockDeduction("deduction_0"), Is.False);
            Assert.That(state.UnlockDeduction("  "), Is.False);
        }

        [Test]
        public void DeductionIds_AreTrimmedAndCannotBeRemoved()
        {
            Assert.That(state.UnlockDeduction(" deduction_a "), Is.True);

            Assert.That(state.HasUnlockedDeduction("deduction_a"), Is.True);
            Assert.That(state.HasUnlockedDeduction(" deduction_a "), Is.True);
            Assert.That(state.UnlockedDeductionIds, Is.EqualTo(new[] { "deduction_a" }));
        }

        [Test]
        public void Flags_AreTrimmedAndDoNotDuplicate()
        {
            int stateChanges = 0;
            state.StateChanged += () => stateChanges++;

            state.AddFlag(" secretary_access ");
            state.AddFlag("secretary_access");

            Assert.That(state.HasFlag("secretary_access"), Is.True);
            Assert.That(state.HasFlag(" secretary_access "), Is.True);
            Assert.That(stateChanges, Is.EqualTo(1));
        }

        [Test]
        public void EvidenceAndLocationIds_AreTrimmedBeforeSaving()
        {
            state.RecordEvidenceCollected(" C-02 ");
            state.RecordEvidenceCollected("C-02");
            state.RecordLocation(" HORIZON ");

            Assert.That(state.CollectedEvidenceIds, Is.EqualTo(new[] { "C-02" }));
            Assert.That(state.CurrentLocationCode, Is.EqualTo("HORIZON"));
        }

        [Test]
        public void CompletedScenes_AreCanonicalAndUnique()
        {
            int stateChanges = 0;
            state.StateChanged += () => stateChanges++;

            Assert.That(state.RecordCompletedScene(" p-01 "), Is.True);
            Assert.That(state.RecordCompletedScene("P-01"), Is.False);
            Assert.That(state.RecordCompletedScene("  "), Is.False);

            Assert.That(state.HasCompletedScene("P-01"), Is.True);
            Assert.That(state.HasCompletedScene(" p-01 "), Is.True);
            Assert.That(state.HasCompletedScene(null), Is.False);
            Assert.That(state.CompletedProductionSceneIds, Is.EqualTo(new[] { "P-01" }));
            Assert.That(stateChanges, Is.EqualTo(1));
        }

        [Test]
        public void CompletedScenes_RestoreAfterManagerRecreation()
        {
            state.RecordCompletedScene("P-01");
            state.RecordCompletedScene("D1-01");

            Object.DestroyImmediate(host);
            state = CreateManager();
            state.ReloadSavedState();

            Assert.That(state.HasCompletedScene("P-01"), Is.True);
            Assert.That(state.HasCompletedScene("D1-01"), Is.True);
            Assert.That(
                state.CompletedProductionSceneIds,
                Is.EquivalentTo(new[] { "P-01", "D1-01" }));
        }

        [Test]
        public void OneShotDialogueAndNpcInteractionIds_AreUniqueAndPersisted()
        {
            Assert.That(
                state.RecordAppliedDialogueEffect(" D1-02_014 "),
                Is.True);
            Assert.That(
                state.RecordAppliedDialogueEffect("d1-02_014"),
                Is.False);
            Assert.That(
                state.RecordCompletedNpcInteraction(
                    "NPC:BARK:D1-02:DINING:ANX_LOW_04"),
                Is.True);

            Object.DestroyImmediate(host);
            state = CreateManager();
            state.ReloadSavedState();

            Assert.That(
                state.HasAppliedDialogueEffect("D1-02_014"),
                Is.True);
            Assert.That(
                state.HasCompletedNpcInteraction(
                    "npc:bark:d1-02:dining:anx_low_04"),
                Is.True);
            Assert.That(state.AppliedDialogueEffectIds, Has.Count.EqualTo(1));
            Assert.That(state.CompletedNpcInteractionIds, Has.Count.EqualTo(1));
        }

        [Test]
        public void LegacyTheoryFields_AreDiscardedWhileOtherProgressMigrates()
        {
            const string legacyJson =
                "{\"day\":2,\"timeBlock\":1,\"publicAnxiety\":25," +
                "\"evidenceIntegrity\":80,\"theorySlots\":3," +
                "\"activeTheories\":[\"old_theory\"],\"trust\":[],\"flags\":[]," +
                "\"collectedEvidenceIds\":[\"C-02\"]," +
                "\"unlockedDeductionIds\":[\"transport_route\"]," +
                "\"currentLocationCode\":\"HORIZON\"}";

            PlayerPrefs.SetString("THE_WAKE_GAME_STATE_V1", legacyJson);
            PlayerPrefs.DeleteKey(SaveKey);
            PlayerPrefs.DeleteKey(SaveKey + "_BACKUP");
            PlayerPrefs.DeleteKey(SaveKey + "_PENDING");
            state.ReloadSavedState();

            Assert.That(state.CompletedProductionSceneIds, Is.Empty);
            Assert.That(state.HasCompletedScene("P-01"), Is.False);
            Assert.That(state.Day, Is.EqualTo(2));
            Assert.That(state.PublicAnxiety, Is.EqualTo(25));
            Assert.That(state.EvidenceIntegrity, Is.EqualTo(80));
            Assert.That(state.CollectedEvidenceIds, Contains.Item("C-02"));
            Assert.That(state.HasUnlockedDeduction("transport_route"), Is.True);
            Assert.That(state.CurrentLocationCode, Is.EqualTo("HORIZON"));
            string migratedJson = PlayerPrefs.GetString(SaveKey);
            Assert.That(migratedJson, Does.Not.Contain("\"theorySlots\""));
            Assert.That(migratedJson, Does.Not.Contain("\"activeTheories\""));
        }

        [Test]
        public void LoadedSceneProgress_DropsBlankAndDuplicateValues()
        {
            const string malformedJson =
                "{\"completedProductionSceneIds\":[\" p-01 \",\"P-01\",\"\",\"d1-01\"]}";

            PlayerPrefs.SetString("THE_WAKE_GAME_STATE_V1", malformedJson);
            PlayerPrefs.DeleteKey(SaveKey);
            PlayerPrefs.DeleteKey(SaveKey + "_BACKUP");
            PlayerPrefs.DeleteKey(SaveKey + "_PENDING");
            state.ReloadSavedState();

            Assert.That(
                state.CompletedProductionSceneIds,
                Is.EqualTo(new[] { "P-01", "D1-01" }));
        }

        [Test]
        public void Time_ClampsDayToOneAndPreservesBlock()
        {
            state.SetTime(-5, TimeBlock.NIGHT);

            Assert.That(state.Day, Is.EqualTo(1));
            Assert.That(state.CurrentTimeBlock, Is.EqualTo(TimeBlock.NIGHT));
        }

        [Test]
        public void SaveData_RestoresStateAfterManagerRecreation()
        {
            state.ChangeTrust("DANIEL", 2);
            state.ChangePublicAnxiety(60);
            state.ChangeEvidenceIntegrity(-40);
            state.UnlockDeduction("ceiling_insertion");
            state.AddFlag("ceiling_access");
            state.RecordEvidenceCollected("C-03");
            state.RecordCompletedScene("P-01");
            state.RecordLocation("HORIZON");
            state.SetTime(2, TimeBlock.PM);

            string savedJson = PlayerPrefs.GetString(SaveKey);
            Assert.That(savedJson, Does.Contain("DANIEL"));
            Assert.That(savedJson, Does.Contain("\"value\":4"));

            Object.DestroyImmediate(host);
            state = CreateManager();
            state.ReloadSavedState();

            Assert.That(state.GetTrust("DANIEL"), Is.EqualTo(4));
            Assert.That(state.PublicAnxiety, Is.EqualTo(75));
            Assert.That(state.EvidenceIntegrity, Is.EqualTo(60));
            Assert.That(state.HasUnlockedDeduction("ceiling_insertion"), Is.True);
            Assert.That(state.HasFlag("ceiling_access"), Is.True);
            Assert.That(state.CollectedEvidenceIds, Contains.Item("C-03"));
            Assert.That(state.HasCompletedScene("P-01"), Is.True);
            Assert.That(state.CurrentLocationCode, Is.EqualTo("HORIZON"));
            Assert.That(state.Day, Is.EqualTo(2));
            Assert.That(state.CurrentTimeBlock, Is.EqualTo(TimeBlock.PM));
        }

        [Test]
        public void PublicApi_DoesNotExposeRemovedTheorySlotOperations()
        {
            var publicMembers = typeof(GameStateManager).GetMembers();
            string[] removedNames =
            {
                "TheorySlots",
                "ActiveTheoryCount",
                "ActivateTheory",
                "RemoveTheory",
                "IsTheoryActive"
            };

            foreach (string removedName in removedNames)
            {
                Assert.That(
                    publicMembers,
                    Has.None.Matches<System.Reflection.MemberInfo>(
                        member => member.Name == removedName),
                    $"{removedName} must not return to the production state contract.");
            }
        }

        [Test]
        public void V2Save_ContainsDeductionProgressWithoutRemovedTheoryFields()
        {
            state.UnlockDeduction("transport_route");

            string savedJson = PlayerPrefs.GetString(SaveKey);

            Assert.That(savedJson, Does.Contain("\"unlockedDeductionIds\""));
            Assert.That(savedJson, Does.Contain("transport_route"));
            Assert.That(savedJson, Does.Not.Contain("\"theorySlots\""));
            Assert.That(savedJson, Does.Not.Contain("\"activeTheories\""));
        }

        [TestCase(
            "Assets/_Project/Content/Evidence/EvidenceDefinition_C02.asset",
            "\uC5F4\uB9B0 \uCD9C\uC785\uBB38",
            "잠금 트릭이 아니라 흔적 부재가 문제")]
        [TestCase(
            "Assets/_Project/Content/Evidence/EvidenceDefinition_C03.asset",
            "\uC678\uBCBD \uBC1C\uD310",
            "염분막과 센서 기록이 온전")]
        [TestCase(
            "Assets/_Project/Content/Evidence/EvidenceDefinition_C04.asset",
            "\uB355\uD2B8 \uBA3C\uC9C0",
            "통과 흔적 없음")]
        [TestCase(
            "Assets/_Project/Content/Evidence/EvidenceDefinition_C05.asset",
            "\uC810\uAC80\uAD6C \uBA3C\uC9C0",
            "봉인과 먼지가 균일")]
        public void EvidenceDefinitions_UseSourceAccurateKorean(
            string assetPath,
            string expectedName,
            string expectedDescription)
        {
            EvidenceDefinition evidence =
                AssetDatabase.LoadAssetAtPath<EvidenceDefinition>(assetPath);

            Assert.That(evidence, Is.Not.Null);
            Assert.That(evidence.DisplayName, Is.EqualTo(expectedName));
            Assert.That(evidence.Description, Is.EqualTo(expectedDescription));
            AssertValidKorean(evidence.DisplayName);
            AssertValidKorean(evidence.Description);
        }

        private GameStateManager CreateManager()
        {
            host = new GameObject("GameStateManagerTests");
            return host.AddComponent<GameStateManager>();
        }

        private static void DestroyExistingManager()
        {
            if (GameStateManager.Instance != null)
            {
                Object.DestroyImmediate(GameStateManager.Instance.gameObject);
            }
        }

        private static void AssertValidKorean(string value)
        {
            Assert.That(value, Is.Not.Null.And.Not.Empty);
            Assert.That(value.IndexOf('\uFFFD'), Is.EqualTo(-1));
            Assert.That(value, Does.Not.Contain("???"));
        }
    }
}
