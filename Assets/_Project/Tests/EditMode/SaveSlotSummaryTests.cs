using System.Text.RegularExpressions;
using NUnit.Framework;
using UnityEngine;
using Wake.Core;

namespace Wake.Tests
{
    public sealed class SaveSlotSummaryTests
    {
        private const string Primary =
            "UNDER_THE_HORIZON_GAME_STATE_V2";
        private const string Pending = Primary + "_PENDING";
        private const string Backup = Primary + "_BACKUP";
        private const string Legacy = "THE_WAKE_GAME_STATE_V1";

        private GameObject host;
        private GameStateManager state;

        [SetUp]
        public void SetUp()
        {
            DestroyManager();
            ClearAllSlots();
            GameStateManager.SetActiveSaveSlot(1);
        }

        [TearDown]
        public void TearDown()
        {
            DestroyManager();
            ClearAllSlots();
            GameStateManager.SetActiveSaveSlot(1);
        }

        [Test]
        public void EmptySlot_HasNoChapterAndDoesNotChangeActiveSlot()
        {
            GameStateManager.SetActiveSaveSlot(2);

            SaveSlotSummary summary =
                GameStateManager.GetSaveSlotSummary(1);
            bool hasSaveData =
                GameStateManager.HasSaveDataInSlot(1);

            Assert.That(summary.IsOccupied, Is.False);
            Assert.That(hasSaveData, Is.False);
            Assert.That(summary.ChapterLabel, Is.Empty);
            Assert.That(summary.IsCompleted, Is.False);
            Assert.That(GameStateManager.ActiveSaveSlot, Is.EqualTo(2));
        }

        [Test]
        public void FreshSave_IsShownAsPrologue()
        {
            CreateNewGame();

            SaveSlotSummary summary =
                GameStateManager.GetSaveSlotSummary(1);

            Assert.That(summary.IsOccupied, Is.True);
            Assert.That(summary.ChapterLabel, Is.EqualTo("\uD504\uB864\uB85C\uADF8"));
            Assert.That(summary.IsCompleted, Is.False);
        }

        [Test]
        public void UnenteredUnlockedScene_DoesNotAdvanceChapter()
        {
            CreateNewGame();
            state.UnlockProductionScene("D8-03");

            SaveSlotSummary summary =
                GameStateManager.GetSaveSlotSummary(1);

            Assert.That(summary.ChapterLabel, Is.EqualTo("\uD504\uB864\uB85C\uADF8"));
        }

        [Test]
        public void ActiveCheckpoint_IsTheAuthoritativeChapter()
        {
            CreateNewGame();
            state.RecordCompletedScene("D6-02");
            state.SaveDialogueCheckpoint("D4-02", 7, false, "");

            SaveSlotSummary summary =
                GameStateManager.GetSaveSlotSummary(1);

            Assert.That(summary.ChapterLabel, Is.EqualTo("DAY 4"));
        }

        [Test]
        public void AwaitingDayTransition_ShowsTheNextDay()
        {
            CreateNewGame();
            state.RecordCompletedScene("D3-05");
            state.SaveDialogueCheckpoint(
                "D3-05",
                8,
                false,
                "scene_complete");
            Assert.That(
                state.ClearDialogueCheckpoint(
                    "D3-05",
                    "scene_complete"),
                Is.True);

            SaveSlotSummary summary =
                GameStateManager.GetSaveSlotSummary(1);

            Assert.That(summary.ChapterLabel, Is.EqualTo("DAY 4"));
        }

        [Test]
        public void LastCompletedCatalogScene_DeterminesChapter()
        {
            CreateNewGame();
            state.RecordCompletedScene("D6-02");
            state.RecordCompletedScene("D2-01");

            SaveSlotSummary summary =
                GameStateManager.GetSaveSlotSummary(1);

            Assert.That(summary.ChapterLabel, Is.EqualTo("DAY 6"));
        }

        [Test]
        public void CompletedEpilogue_IsMarkedComplete()
        {
            CreateNewGame();
            state.RecordCompletedScene("D8-03");

            SaveSlotSummary summary =
                GameStateManager.GetSaveSlotSummary(1);

            Assert.That(summary.ChapterLabel, Is.EqualTo("\uC5D0\uD544\uB85C\uADF8"));
            Assert.That(summary.IsCompleted, Is.True);
        }

        [Test]
        public void Preview_UsesNewestPendingWithoutRewritingStorage()
        {
            CreateNewGame();
            state.SetTime(2, TimeBlock.AM);
            string primaryDay2 = PlayerPrefs.GetString(Primary);
            state.SetTime(7, TimeBlock.NIGHT);
            string pendingDay7 = PlayerPrefs.GetString(Primary);
            PlayerPrefs.SetString(Primary, primaryDay2);
            PlayerPrefs.SetString(Pending, pendingDay7);
            PlayerPrefs.Save();
            GameStateManager.SetActiveSaveSlot(3);

            SaveSlotSummary summary =
                GameStateManager.GetSaveSlotSummary(1);

            Assert.That(summary.ChapterLabel, Is.EqualTo("DAY 7"));
            Assert.That(PlayerPrefs.GetString(Primary), Is.EqualTo(primaryDay2));
            Assert.That(PlayerPrefs.GetString(Pending), Is.EqualTo(pendingDay7));
            Assert.That(GameStateManager.ActiveSaveSlot, Is.EqualTo(3));
        }

        [Test]
        public void Preview_RecoversBackupWhenPrimaryIsCorrupt()
        {
            CreateNewGame();
            state.SetTime(4, TimeBlock.PM);
            string validBackup = PlayerPrefs.GetString(Primary);
            string corruptPrimary = CorruptChecksum(validBackup);
            PlayerPrefs.SetString(Primary, corruptPrimary);
            PlayerPrefs.SetString(Backup, validBackup);
            PlayerPrefs.Save();

            SaveSlotSummary summary =
                GameStateManager.GetSaveSlotSummary(1);

            Assert.That(summary.ChapterLabel, Is.EqualTo("DAY 4"));
            Assert.That(PlayerPrefs.GetString(Primary), Is.EqualTo(corruptPrimary));
            Assert.That(PlayerPrefs.GetString(Backup), Is.EqualTo(validBackup));
        }

        [Test]
        public void Preview_DoesNotTreatOrphanedBackupAsOccupied()
        {
            CreateNewGame();
            state.SetTime(5, TimeBlock.PM);
            string orphanedBackup = PlayerPrefs.GetString(Primary);
            PlayerPrefs.DeleteKey(Primary);
            PlayerPrefs.DeleteKey(Pending);
            PlayerPrefs.SetString(Backup, orphanedBackup);
            PlayerPrefs.Save();

            SaveSlotSummary summary =
                GameStateManager.GetSaveSlotSummary(1);

            Assert.That(summary.IsOccupied, Is.False);
            Assert.That(summary.ChapterLabel, Is.Empty);
        }

        [Test]
        public void LegacyFallback_UsesSavedDayWithoutMigrating()
        {
            const string legacyJson =
                "{\"day\":5,\"currentLocationCode\":\"HORIZON\"}";
            PlayerPrefs.SetString(Legacy, legacyJson);
            PlayerPrefs.Save();
            GameStateManager.SetActiveSaveSlot(2);

            SaveSlotSummary summary =
                GameStateManager.GetSaveSlotSummary(1);

            Assert.That(summary.IsOccupied, Is.True);
            Assert.That(summary.ChapterLabel, Is.EqualTo("DAY 5"));
            Assert.That(PlayerPrefs.GetString(Legacy), Is.EqualTo(legacyJson));
            Assert.That(PlayerPrefs.HasKey(Primary), Is.False);
            Assert.That(GameStateManager.ActiveSaveSlot, Is.EqualTo(2));
        }

        private void CreateNewGame()
        {
            host = new GameObject(nameof(SaveSlotSummaryTests));
            state = host.AddComponent<GameStateManager>();
            state.StartNewGame();
        }

        private void DestroyManager()
        {
            if (host != null)
            {
                Object.DestroyImmediate(host);
                host = null;
                state = null;
            }

            if (GameStateManager.Instance != null)
            {
                Object.DestroyImmediate(
                    GameStateManager.Instance.gameObject);
            }
        }

        private static string CorruptChecksum(string json)
        {
            return Regex.Replace(
                json,
                "\"checksum\":\"[^\"]+\"",
                "\"checksum\":\"invalid\"");
        }

        private static void ClearAllSlots()
        {
            foreach (string primary in new[]
            {
                Primary,
                Primary + "_SLOT_2",
                Primary + "_SLOT_3"
            })
            {
                PlayerPrefs.DeleteKey(primary);
                PlayerPrefs.DeleteKey(primary + "_BACKUP");
                PlayerPrefs.DeleteKey(primary + "_PENDING");
            }

            PlayerPrefs.DeleteKey(Legacy);
            PlayerPrefs.DeleteKey(Legacy + "_BACKUP");
            PlayerPrefs.DeleteKey(Legacy + "_PENDING");
            PlayerPrefs.Save();
        }
    }
}
