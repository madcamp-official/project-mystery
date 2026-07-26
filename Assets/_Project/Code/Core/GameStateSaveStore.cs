using System;
using System.Security.Cryptography;
using System.Text;
using UnityEngine;
namespace Wake.Core
{
    [Serializable]
    internal sealed class GameStateSaveEnvelope
    {
        public string format;
        public int schemaVersion;
        public long generation;
        public GameStateSaveData payload;
        public string checksum;
    }
    internal readonly struct GameStateSaveLoadResult
    {
        public GameStateSaveLoadResult(
            GameStateSaveData data,
            long generation,
            bool needsRewrite,
            string warning)
        {
            Data = data;
            Generation = generation;
            NeedsRewrite = needsRewrite;
            Warning = warning;
        }

        public GameStateSaveData Data { get; }
        public long Generation { get; }
        public bool NeedsRewrite { get; }
        public string Warning { get; }
    }
    internal static class GameStateSaveStore
    {
        public const string PrimaryKey = "THE_WAKE_GAME_STATE_V1";
        public const string BackupKey = PrimaryKey + "_BACKUP";
        public const string PendingKey = PrimaryKey + "_PENDING";
        private const string Format = "THE_WAKE_GAME_STATE";
        private const int SchemaVersion = 2;
        private const string InterruptedWarning = "중단된 저장 작업을 복구했습니다.";
        private const string BackupWarning =
            "저장 데이터가 손상되어 백업본으로 복구했습니다.";
        private const string CorruptWarning =
            "저장 데이터를 읽을 수 없어 안전한 초기 상태로 시작합니다. " +
            "새 게임을 시작하기 전까지 손상된 저장본은 유지됩니다.";
        private static readonly string[] LegacyFields =
        {
            "day", "timeBlock", "publicAnxiety", "evidenceIntegrity",
            "theorySlots", "activeTheories", "trust", "flags",
            "collectedEvidenceIds", "completedProductionSceneIds",
            "completedObjectiveIds", "puzzleSessions", "unlockedDeductionIds",
            "finalEndingId", "currentLocationCode", "dialogueCheckpoint"
        };
        private enum SaveSource { Primary, Pending, Backup }

        private sealed class Candidate
        {
            public GameStateSaveData Data;
            public long Generation;
            public bool Legacy;
            public string Raw;
            public SaveSource Source;
        }

        public static bool HasRecoverableData() =>
            SelectCandidate(out _, out _) != null;

        public static GameStateSaveLoadResult Load()
        {
            Candidate candidate =
                SelectCandidate(out bool corruption, out bool pendingPresent);
            if (candidate == null)
            {
                return new GameStateSaveLoadResult(
                    null, 0, false, corruption ? CorruptWarning : string.Empty);
            }

            bool rewrite =
                candidate.Legacy ||
                candidate.Source != SaveSource.Primary ||
                pendingPresent;
            string warning = candidate.Source switch
            {
                SaveSource.Backup => BackupWarning,
                SaveSource.Pending => InterruptedWarning,
                _ when pendingPresent => InterruptedWarning,
                _ => string.Empty
            };
            return new GameStateSaveLoadResult(
                candidate.Data, candidate.Generation, rewrite, warning);
        }

        public static long Save(GameStateSaveData data, long currentGeneration)
        {
            long generation = Math.Max(0, currentGeneration) + 1;
            string pending = Serialize(data, generation);
            Write(PendingKey, pending);
            Candidate primary = Read(PrimaryKey, SaveSource.Primary);
            if (primary != null)
            {
                Write(BackupKey, primary.Raw);
            }
            else if (!PlayerPrefs.HasKey(PrimaryKey) && currentGeneration == 0)
            {
                PlayerPrefs.DeleteKey(BackupKey);
                PlayerPrefs.Save();
            }
            Write(PrimaryKey, pending);
            PlayerPrefs.DeleteKey(PendingKey);
            PlayerPrefs.Save();
            return generation;
        }

        public static void ClearAll()
        {
            PlayerPrefs.DeleteKey(PrimaryKey);
            PlayerPrefs.DeleteKey(BackupKey);
            PlayerPrefs.DeleteKey(PendingKey);
            PlayerPrefs.Save();
        }
        private static Candidate SelectCandidate(
            out bool activeCorruption,
            out bool pendingPresent)
        {
            bool primaryPresent = PlayerPrefs.HasKey(PrimaryKey);
            pendingPresent = PlayerPrefs.HasKey(PendingKey);
            bool backupPresent = PlayerPrefs.HasKey(BackupKey);
            Candidate primary = Read(PrimaryKey, SaveSource.Primary);
            Candidate pending = Read(PendingKey, SaveSource.Pending);
            bool backupEligible = primaryPresent || pending != null;
            Candidate backup = backupEligible
                ? Read(BackupKey, SaveSource.Backup)
                : null;
            activeCorruption =
                (primaryPresent && primary == null) ||
                (pendingPresent && pending == null) ||
                (backupEligible && backupPresent && backup == null);
            return Newer(Newer(primary, pending), backup);
        }

        private static Candidate Newer(Candidate current, Candidate candidate)
        {
            return candidate != null &&
                   (current == null || candidate.Generation > current.Generation)
                ? candidate
                : current;
        }

        private static Candidate Read(string key, SaveSource source)
        {
            if (!PlayerPrefs.HasKey(key))
            {
                return null;
            }

            string raw = PlayerPrefs.GetString(key);
            try
            {
                if (LooksLikeEnvelope(raw))
                {
                    GameStateSaveEnvelope envelope =
                        JsonUtility.FromJson<GameStateSaveEnvelope>(raw);
                    if (envelope == null ||
                        envelope.format != Format ||
                        envelope.schemaVersion != SchemaVersion ||
                        envelope.generation < 1 ||
                        envelope.payload == null ||
                        !string.Equals(
                            envelope.checksum,
                            ComputeChecksum(envelope),
                            StringComparison.OrdinalIgnoreCase))
                    {
                        return null;
                    }
                    return new Candidate
                    {
                        Data = envelope.payload,
                        Generation = envelope.generation,
                        Raw = raw,
                        Source = source
                    };
                }

                if (!LooksLikeLegacy(raw))
                {
                    return null;
                }
                GameStateSaveData legacy =
                    JsonUtility.FromJson<GameStateSaveData>(raw);
                return legacy == null ? null : new Candidate
                {
                    Data = legacy,
                    Legacy = true,
                    Raw = raw,
                    Source = source
                };
            }
            catch (Exception)
            {
                return null;
            }
        }

        private static string Serialize(GameStateSaveData data, long generation)
        {
            var envelope = new GameStateSaveEnvelope
            {
                format = Format,
                schemaVersion = SchemaVersion,
                generation = generation,
                payload = data
            };
            envelope.checksum = ComputeChecksum(envelope);
            return JsonUtility.ToJson(envelope);
        }

        private static string ComputeChecksum(GameStateSaveEnvelope envelope)
        {
            string material =
                $"{envelope.format}\n{envelope.schemaVersion}\n" +
                $"{envelope.generation}\n{JsonUtility.ToJson(envelope.payload)}";
            using SHA256 sha256 = SHA256.Create();
            byte[] hash = sha256.ComputeHash(Encoding.UTF8.GetBytes(material));
            return BitConverter.ToString(hash).Replace("-", "").ToLowerInvariant();
        }

        private static void Write(string key, string value)
        {
            PlayerPrefs.SetString(key, value);
            PlayerPrefs.Save();
        }

        private static bool LooksLikeEnvelope(string raw) =>
            raw?.Contains("\"format\"") == true ||
            raw?.Contains("\"schemaVersion\"") == true ||
            raw?.Contains("\"payload\"") == true ||
            raw?.Contains("\"checksum\"") == true;

        private static bool LooksLikeLegacy(string raw) =>
            !string.IsNullOrWhiteSpace(raw) &&
            Array.Exists(
                LegacyFields,
                field => raw.Contains($"\"{field}\""));
    }
}
