using System;
using System.Security.Cryptography;
using System.Text;
using NUnit.Framework;
using UnityEngine;
using Wake.Core;

namespace Wake.Tests
{
    public sealed class GameStateV2MigrationContractTests
    {
        private const string Primary = "UNDER_THE_HORIZON_GAME_STATE_V2";
        private const string Backup = Primary + "_BACKUP";
        private const string Pending = Primary + "_PENDING";
        private const string Legacy = "THE_WAKE_GAME_STATE_V1";
        private const string LegacyBackup = Legacy + "_BACKUP";
        private const string LegacyPending = Legacy + "_PENDING";
        private GameObject host;

        [SetUp]
        public void SetUp()
        {
            DestroyManager();
            ClearAllSlots();
        }

        [TearDown]
        public void TearDown()
        {
            DestroyManager();
            ClearAllSlots();
        }

        [Test]
        public void SaveKeys_UseFinalGameNameAndV2Suffix()
        {
            Assert.That(
                Primary,
                Is.EqualTo("UNDER_THE_HORIZON_GAME_STATE_V2"));
            Assert.That(
                Backup,
                Is.EqualTo("UNDER_THE_HORIZON_GAME_STATE_V2_BACKUP"));
            Assert.That(
                Pending,
                Is.EqualTo("UNDER_THE_HORIZON_GAME_STATE_V2_PENDING"));
            Assert.That(
                Legacy,
                Is.EqualTo("THE_WAKE_GAME_STATE_V1"));
        }

        [Test]
        public void NewGame_WritesOnlyUnderHorizonEnvelope()
        {
            GameStateManager state = CreateManager();
            state.StartNewGame();

            Assert.That(
                PlayerPrefs.HasKey(Primary),
                Is.True);
            Assert.That(
                PlayerPrefs.GetString(Primary),
                Does.Contain("\"format\":\"UNDER_THE_HORIZON_GAME_STATE\"")
                    .And.Contain("\"schemaVersion\":2")
                    .And.Contain("\"checksum\":"));
            Assert.That(
                PlayerPrefs.HasKey(Legacy),
                Is.False);
        }

        [Test]
        public void LegacyPlainJson_IsDetectedBeforeManagerCreation()
        {
            PlayerPrefs.SetString(
                Legacy,
                "{\"day\":3,\"currentLocationCode\":\"BRIDGE\"}");

            Assert.That(GameStateManager.HasSaveData, Is.True);

            GameStateManager state = CreateManager();
            Assert.That(state.Day, Is.EqualTo(3));
            Assert.That(state.CurrentLocationCode, Is.EqualTo("BRIDGE"));
            AssertMigrated();
        }

        [Test]
        public void LegacyEnvelope_PreservesGenerationAndPayload()
        {
            const string payload =
                "{\"day\":6,\"publicAnxiety\":42," +
                "\"evidenceIntegrity\":67," +
                "\"currentLocationCode\":\"ENGINE_CTRL\"}";
            PlayerPrefs.SetString(
                Legacy,
                CreateLegacyEnvelope(payload, 12));

            GameStateManager state = CreateManager();

            Assert.That(state.Day, Is.EqualTo(6));
            Assert.That(state.PublicAnxiety, Is.EqualTo(42));
            Assert.That(state.EvidenceIntegrity, Is.EqualTo(67));
            Assert.That(
                state.CurrentLocationCode,
                Is.EqualTo("ENGINE_CTRL"));
            Assert.That(
                PlayerPrefs.GetString(Primary),
                Does.Contain("\"generation\":13"));
            AssertMigrated();
        }

        [Test]
        public void CurrentV2Save_TakesPriorityOverLegacySave()
        {
            GameStateManager state = CreateManager();
            state.StartNewGame();
            state.RecordLocation("CURRENT");
            DestroyManager();
            PlayerPrefs.SetString(
                Legacy,
                "{\"day\":9,\"currentLocationCode\":\"LEGACY\"}");

            state = CreateManager();

            Assert.That(state.Day, Is.EqualTo(1));
            Assert.That(state.CurrentLocationCode, Is.EqualTo("CURRENT"));
            Assert.That(
                PlayerPrefs.HasKey(Legacy),
                Is.True,
                "An unused legacy slot is not deleted until the next successful save.");
            state.ChangePublicAnxiety(1);
            Assert.That(
                PlayerPrefs.HasKey(Legacy),
                Is.False);
        }

        [Test]
        public void CorruptLegacyEnvelope_IsNotContinueData()
        {
            string envelope = CreateLegacyEnvelope(
                "{\"day\":7}",
                3);
            PlayerPrefs.SetString(
                Legacy,
                envelope.Replace(
                    "\"checksum\":\"",
                    "\"checksum\":\"corrupt"));

            Assert.That(GameStateManager.HasSaveData, Is.False);
        }

        private GameStateManager CreateManager()
        {
            host = new GameObject(nameof(GameStateV2MigrationContractTests));
            GameStateManager state = host.AddComponent<GameStateManager>();
            state.ReloadSavedState();
            return state;
        }

        private static string CreateLegacyEnvelope(
            string payloadJson,
            long generation)
        {
            const string format = "THE_WAKE_GAME_STATE";
            const int schemaVersion = 2;
            Type payloadType = typeof(GameStateManager).Assembly.GetType(
                "Wake.Core.GameStateSaveData",
                throwOnError: true);
            object payload = JsonUtility.FromJson(payloadJson, payloadType);
            payloadJson = JsonUtility.ToJson(payload);
            string material =
                $"{format}\n{schemaVersion}\n" +
                $"{generation}\n{payloadJson}";
            using SHA256 sha = SHA256.Create();
            string checksum = BitConverter.ToString(
                    sha.ComputeHash(Encoding.UTF8.GetBytes(material)))
                .Replace("-", "")
                .ToLowerInvariant();
            return
                $"{{\"format\":\"{format}\"," +
                $"\"schemaVersion\":{schemaVersion}," +
                $"\"generation\":{generation}," +
                $"\"payload\":{payloadJson}," +
                $"\"checksum\":\"{checksum}\"}}";
        }

        private static void AssertMigrated()
        {
            Assert.That(
                PlayerPrefs.HasKey(Primary),
                Is.True);
            Assert.That(
                PlayerPrefs.HasKey(Legacy),
                Is.False);
        }

        private void DestroyManager()
        {
            if (host != null)
            {
                UnityEngine.Object.DestroyImmediate(host);
                host = null;
            }
            if (GameStateManager.Instance != null)
                UnityEngine.Object.DestroyImmediate(
                    GameStateManager.Instance.gameObject);
        }

        private static void ClearAllSlots()
        {
            foreach (string key in new[]
            {
                Primary,
                Backup,
                Pending,
                Legacy,
                LegacyBackup,
                LegacyPending
            })
                PlayerPrefs.DeleteKey(key);
            PlayerPrefs.Save();
        }
    }
}
