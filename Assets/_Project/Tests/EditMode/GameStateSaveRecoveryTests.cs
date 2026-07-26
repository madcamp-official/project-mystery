using System.Collections.Generic;
using System.Text.RegularExpressions;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;
using Wake.Core;
using Wake.Evidence;
using Wake.Narrative;
namespace Wake.Tests
{
    public class GameStateSaveRecoveryTests
    {
        private const string Primary = "UNDER_THE_HORIZON_GAME_STATE_V2";
        private const string Backup = Primary + "_BACKUP";
        private const string Pending = Primary + "_PENDING";
        private const string Legacy = "THE_WAKE_GAME_STATE_V1";
        private GameObject host;
        private GameStateManager state;
        [SetUp]
        public void SetUp()
        {
            DestroyManager();
            ClearSlots();
            CreateManager();
            state.StartNewGame();
        }
        [TearDown]
        public void TearDown()
        {
            DestroyManager();
            ClearSlots();
        }
        [Test]
        public void V2RoundTrip_PreservesWholeProductionState()
        {
            state.SetTime(8, TimeBlock.NIGHT);
            state.ChangeTrust("CLAIRE", 2);
            state.ChangePublicAnxiety(40);
            state.ChangeEvidenceIntegrity(-30);
            foreach (string theory in new[] { "route", "motive", "identity" })
            {
                state.ActivateTheory(theory);
            }
            state.AddFlag("service_rail_access");
            foreach (CanonicalEvidenceEntry item in CanonicalEvidenceCatalog.All)
            {
                state.RecordEvidenceCollected(item.Id);
            }
            foreach (ProductionSceneDefinition item in ProductionSceneCatalog.All)
            {
                state.RecordCompletedScene(item.SceneId);
            }
            foreach (ProductionSceneCompletionRequirement item in
                     ProductionSceneCompletionCatalog.All)
            {
                state.SavePuzzleSession(new PuzzleSessionState
                {
                    puzzleId = item.InteractionId,
                    selectedIds = new List<string> { "alpha", "beta" },
                    step = 2,
                    hintLevel = 1,
                    completed = true
                });
            }
            state.RecordCompletedObjective("inspect_horizon");
            state.UnlockDeduction("transport_route");
            state.TryRecordFinalEnding("complete");
            state.RecordLocation("HORIZON");
            state.SaveDialogueCheckpoint("D8-02", 3, true, "final_choice");
            RecreateManager();
            Assert.That(state.Day, Is.EqualTo(8));
            Assert.That(state.CurrentTimeBlock, Is.EqualTo(TimeBlock.NIGHT));
            Assert.That(state.GetTrust("CLAIRE"), Is.EqualTo(4));
            Assert.That(state.PublicAnxiety, Is.EqualTo(55));
            Assert.That(state.EvidenceIntegrity, Is.EqualTo(70));
            Assert.That(state.ActiveTheoryCount, Is.EqualTo(3));
            Assert.That(state.CollectedEvidenceIds, Has.Count.EqualTo(18));
            Assert.That(state.CompletedProductionSceneIds, Has.Count.EqualTo(41));
            Assert.That(state.HasCompletedObjective("inspect_horizon"), Is.True);
            Assert.That(state.HasUnlockedDeduction("transport_route"), Is.True);
            Assert.That(state.FinalEndingId, Is.EqualTo("complete"));
            Assert.That(state.CurrentLocationCode, Is.EqualTo("HORIZON"));
            Assert.That(state.DialogueCheckpoint.activeSceneId, Is.EqualTo("D8-02"));
            Assert.That(state.TryGetPuzzleSession(
                ProductionSceneCompletionCatalog.All[0].InteractionId,
                out PuzzleSessionState puzzle), Is.True);
            Assert.That(puzzle.completed, Is.True);
        }
        [Test]
        public void LegacyPrimary_MigratesAndKeepsRawBackup()
        {
            const string legacy = "{\"day\":2,\"publicAnxiety\":25," +
                "\"evidenceIntegrity\":80,\"currentLocationCode\":\"HORIZON\"}";
            DestroyManager();
            ClearSlots();
            PlayerPrefs.SetString(Primary, legacy);
            CreateManager();
            Assert.That(state.Day, Is.EqualTo(2));
            Assert.That(state.PublicAnxiety, Is.EqualTo(25));
            Assert.That(PlayerPrefs.GetString(Backup), Is.EqualTo(legacy));
            Assert.That(PlayerPrefs.GetString(Primary),
                Does.Contain("\"schemaVersion\":2").And.Contain("\"checksum\":"));
        }

        [Test]
        public void WakeV1Key_MigratesToUnderHorizonV2Key()
        {
            const string legacy = "{\"day\":4,\"publicAnxiety\":35," +
                "\"evidenceIntegrity\":75,\"flags\":[\"legacy_flag\"]," +
                "\"currentLocationCode\":\"PROMENADE\"}";
            DestroyManager();
            ClearSlots();
            PlayerPrefs.SetString(Legacy, legacy);

            CreateManager();

            Assert.That(state.Day, Is.EqualTo(4));
            Assert.That(state.PublicAnxiety, Is.EqualTo(35));
            Assert.That(state.EvidenceIntegrity, Is.EqualTo(75));
            Assert.That(state.HasFlag("legacy_flag"), Is.True);
            Assert.That(state.CurrentLocationCode, Is.EqualTo("PROMENADE"));
            Assert.That(PlayerPrefs.HasKey(Primary), Is.True);
            Assert.That(
                PlayerPrefs.GetString(Primary),
                Does.Contain("\"format\":\"UNDER_THE_HORIZON_GAME_STATE\""));
            Assert.That(PlayerPrefs.HasKey(Legacy), Is.False);
        }

        [Test]
        public void NewGame_ClearsBothV2AndLegacySlots()
        {
            PlayerPrefs.SetString(Legacy, "{\"day\":7}");
            PlayerPrefs.SetString(Legacy + "_BACKUP", "{\"day\":6}");
            PlayerPrefs.SetString(Legacy + "_PENDING", "{\"day\":8}");

            state.StartNewGame();

            Assert.That(PlayerPrefs.HasKey(Legacy), Is.False);
            Assert.That(PlayerPrefs.HasKey(Legacy + "_BACKUP"), Is.False);
            Assert.That(PlayerPrefs.HasKey(Legacy + "_PENDING"), Is.False);
            Assert.That(PlayerPrefs.HasKey(Primary), Is.True);
        }
        [Test]
        public void InterruptedJournal_UsesNewestGeneration()
        {
            state.RecordLocation("OLD");
            string older = PlayerPrefs.GetString(Primary);
            state.RecordLocation("NEW");
            string newer = PlayerPrefs.GetString(Primary);
            PlayerPrefs.SetString(Primary, older);
            PlayerPrefs.SetString(Backup, older);
            PlayerPrefs.SetString(Pending, newer);
            LogAssert.Expect(LogType.Warning,
                "중단된 저장 작업을 복구했습니다.");
            RecreateManager();
            Assert.That(state.CurrentLocationCode, Is.EqualTo("NEW"));
            Assert.That(PlayerPrefs.HasKey(Pending), Is.False);
            Assert.That(GameStateManager.HasSaveData, Is.True);
        }
        [Test]
        public void CorruptPrimary_RecoversValidBackup()
        {
            state.RecordLocation("BACKUP_STATE");
            string validBackup = PlayerPrefs.GetString(Primary);
            state.RecordLocation("LATEST_STATE");
            PlayerPrefs.SetString(Backup, validBackup);
            PlayerPrefs.SetString(Primary,
                CorruptChecksum(PlayerPrefs.GetString(Primary)));
            LogAssert.Expect(LogType.Warning,
                "저장 데이터가 손상되어 백업본으로 복구했습니다.");
            RecreateManager();
            Assert.That(state.CurrentLocationCode, Is.EqualTo("BACKUP_STATE"));
            Assert.That(PlayerPrefs.GetString(Backup), Is.EqualTo(validBackup));
            Assert.That(GameStateManager.HasSaveData, Is.True);
        }
        [Test]
        public void AllSlotsCorrupt_UsesDefaultsWithoutOverwrite()
        {
            const string corrupt = "{\"format\":\"THE_WAKE_GAME_STATE\"," +
                "\"schemaVersion\":2,\"generation\":9,\"payload\":{\"day\":8}," +
                "\"checksum\":\"bad\"}";
            DestroyManager();
            PlayerPrefs.SetString(Primary, corrupt);
            PlayerPrefs.SetString(Backup, corrupt);
            PlayerPrefs.SetString(Pending, corrupt);
            LogAssert.Expect(LogType.Warning,
                "저장 데이터를 읽을 수 없어 안전한 초기 상태로 시작합니다. " +
                "새 게임을 시작하기 전까지 손상된 저장본은 유지됩니다.");
            CreateManager();

            Assert.That(state.Day, Is.EqualTo(1));
            Assert.That(state.PublicAnxiety, Is.EqualTo(15));
            Assert.That(PlayerPrefs.GetString(Primary), Is.EqualTo(corrupt));
            Assert.That(GameStateManager.HasSaveData, Is.False);
            state.StartNewGame();
            Assert.That(GameStateManager.HasSaveData, Is.True);
            Assert.That(PlayerPrefs.HasKey(Backup), Is.False);
            Assert.That(PlayerPrefs.HasKey(Pending), Is.False);
        }

        [Test]
        public void BackupOnly_IsNotContinueData()
        {
            state.RecordLocation("STALE");
            string stale = PlayerPrefs.GetString(Primary);
            DestroyManager();
            PlayerPrefs.DeleteKey(Primary);
            PlayerPrefs.DeleteKey(Pending);
            PlayerPrefs.SetString(Backup, stale);
            CreateManager();

            Assert.That(GameStateManager.HasSaveData, Is.False);
            Assert.That(state.CurrentLocationCode, Is.Empty);
        }

        private void CreateManager()
        {
            host = new GameObject(nameof(GameStateSaveRecoveryTests));
            state = host.AddComponent<GameStateManager>();
            state.ReloadSavedState();
        }
        private void RecreateManager()
        {
            DestroyManager();
            CreateManager();
        }

        private void DestroyManager()
        {
            if (host != null)
            {
                Object.DestroyImmediate(host);
                host = null;
            }
            if (GameStateManager.Instance != null)
            {
                Object.DestroyImmediate(GameStateManager.Instance.gameObject);
            }
        }

        private static string CorruptChecksum(string json) =>
            Regex.Replace(json, "\"checksum\":\"[^\"]+\"",
                "\"checksum\":\"invalid\"");

        private static void ClearSlots()
        {
            PlayerPrefs.DeleteKey(Primary);
            PlayerPrefs.DeleteKey(Backup);
            PlayerPrefs.DeleteKey(Pending);
            PlayerPrefs.DeleteKey(Legacy);
            PlayerPrefs.DeleteKey(Legacy + "_BACKUP");
            PlayerPrefs.DeleteKey(Legacy + "_PENDING");
            PlayerPrefs.Save();
        }
    }
}
