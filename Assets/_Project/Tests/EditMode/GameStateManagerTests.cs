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
            if (host != null)
            {
                Object.DestroyImmediate(host);
            }

            DestroyExistingManager();
            PlayerPrefs.DeleteKey(SaveKey);
        }

        [Test]
        public void NewGame_UsesDocumentedDefaults()
        {
            Assert.That(state.Day, Is.EqualTo(1));
            Assert.That(state.CurrentTimeBlock, Is.EqualTo(TimeBlock.AM));
            Assert.That(state.GetTrust("DANIEL"), Is.EqualTo(2));
            Assert.That(state.PublicAnxiety, Is.EqualTo(15));
            Assert.That(state.EvidenceIntegrity, Is.EqualTo(100));
            Assert.That(state.TheorySlots, Is.EqualTo(3));
            Assert.That(state.ActiveTheoryCount, Is.Zero);
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
        public void Theories_AllowThreeUniqueActiveEntries()
        {
            Assert.That(state.ActivateTheory("theory_a"), Is.True);
            Assert.That(state.ActivateTheory("theory_b"), Is.True);
            Assert.That(state.ActivateTheory("theory_c"), Is.True);

            Assert.That(state.ActiveTheoryCount, Is.EqualTo(3));
            Assert.That(state.ActivateTheory("theory_d"), Is.False);
            Assert.That(state.ActivateTheory("theory_a"), Is.False);
            Assert.That(state.ActivateTheory("  "), Is.False);
        }

        [Test]
        public void TheoryIds_AreTrimmedAndRemovingOneFreesSlot()
        {
            state.ActivateTheory(" theory_a ");
            state.ActivateTheory("theory_b");
            state.ActivateTheory("theory_c");

            Assert.That(state.ActivateTheory("theory_a"), Is.False);
            Assert.That(state.RemoveTheory(" theory_b "), Is.True);
            Assert.That(state.ActivateTheory("theory_d"), Is.True);
            Assert.That(state.ActiveTheoryCount, Is.EqualTo(3));
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
            state.ActivateTheory("ceiling_insertion");
            state.AddFlag("ceiling_access");
            state.RecordEvidenceCollected("C-03");
            state.RecordLocation("HORIZON");
            state.SetTime(2, TimeBlock.PM);

            string savedJson = PlayerPrefs.GetString("THE_WAKE_GAME_STATE_V1");
            Assert.That(savedJson, Does.Contain("DANIEL"));
            Assert.That(savedJson, Does.Contain("\"value\":4"));

            Object.DestroyImmediate(host);
            state = CreateManager();
            state.ReloadSavedState();

            Assert.That(state.GetTrust("DANIEL"), Is.EqualTo(4));
            Assert.That(state.PublicAnxiety, Is.EqualTo(75));
            Assert.That(state.EvidenceIntegrity, Is.EqualTo(60));
            Assert.That(state.ActiveTheoryCount, Is.EqualTo(1));
            Assert.That(state.HasFlag("ceiling_access"), Is.True);
            Assert.That(state.CollectedEvidenceIds, Contains.Item("C-03"));
            Assert.That(state.CurrentLocationCode, Is.EqualTo("HORIZON"));
            Assert.That(state.Day, Is.EqualTo(2));
            Assert.That(state.CurrentTimeBlock, Is.EqualTo(TimeBlock.PM));
        }

        [TestCase(
            "Assets/_Project/Content/Evidence/EvidenceDefinition_C02.asset",
            "\uC5F4\uB9B0 \uCD9C\uC785\uBB38",
            "\uC7A0\uAE08 \uD2B8\uB9AD\uC774 \uC544\uB2C8\uB77C \uCD9C\uC785 \uD754\uC801 \uBD80\uC7AC\uAC00 \uBB38\uC81C\uB2E4.")]
        [TestCase(
            "Assets/_Project/Content/Evidence/EvidenceDefinition_C03.asset",
            "\uC678\uBCBD \uBC1C\uD310",
            "\uC5FC\uBD84\uB9C9\uACFC \uC13C\uC11C \uAE30\uB85D\uC774 \uC628\uC804\uD558\uB2E4.")]
        [TestCase(
            "Assets/_Project/Content/Evidence/EvidenceDefinition_C04.asset",
            "\uB355\uD2B8 \uBA3C\uC9C0",
            "\uD1B5\uACFC \uD754\uC801\uC774 \uC5C6\uB2E4.")]
        [TestCase(
            "Assets/_Project/Content/Evidence/EvidenceDefinition_C05.asset",
            "\uC810\uAC80\uAD6C \uBA3C\uC9C0",
            "\uBA3C\uC9C0\uAC00 \uADE0\uC77C\uD558\uAC8C \uC720\uC9C0\uB418\uC5B4 \uC788\uB2E4.")]
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
