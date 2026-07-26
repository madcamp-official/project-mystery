using System;
using System.Collections.Generic;
using UnityEngine;

namespace Wake.Core
{
    public enum TimeBlock
    {
        AM,
        PM,
        NIGHT
    }

    public enum StateThresholdKind
    {
        PublicAnxietyRestriction,
        PublicAnxietyBadEnd,
        EvidenceIntegrityBadEnd
    }

    [Serializable]
    public class CharacterTrustState
    {
        public string characterId;
        public int value = GameStateManager.DefaultTrust;
    }

    [Serializable]
    internal class GameStateSaveData
    {
        public int day = 1;
        public TimeBlock timeBlock = TimeBlock.AM;
        public int publicAnxiety = 15;
        public int evidenceIntegrity = 100;
        public int theorySlots = 3;
        public List<string> activeTheories = new();
        public List<CharacterTrustState> trust = new();
        public List<string> flags = new();
        public List<string> collectedEvidenceIds = new();
        public List<string> completedProductionSceneIds = new();
        public List<string> unlockedDeductionIds = new();
        public string currentLocationCode = string.Empty;
    }

    public class GameStateManager : MonoBehaviour
    {
        private const string SaveKey = "THE_WAKE_GAME_STATE_V1";

        public const int DefaultTrust = 2;
        public const int MaxTrust = 5;
        public const int MaxPercent = 100;
        public const int RestrictedAreaAnxiety = 70;

        public static GameStateManager Instance { get; private set; }
        public static bool HasSaveData => PlayerPrefs.HasKey(SaveKey);

        [SerializeField] private int startingTrust = DefaultTrust;
        [SerializeField] private int startingPublicAnxiety = 15;
        [SerializeField] private int startingEvidenceIntegrity = 100;
        [SerializeField] private int startingTheorySlots = 3;
        [SerializeField] private GameStateSaveData data = new();

        public int Day => data.day;
        public TimeBlock CurrentTimeBlock => data.timeBlock;
        public int PublicAnxiety => data.publicAnxiety;
        public int EvidenceIntegrity => data.evidenceIntegrity;
        public int TheorySlots => data.theorySlots;
        public int ActiveTheoryCount => data.activeTheories.Count;
        public IReadOnlyList<string> CollectedEvidenceIds => data.collectedEvidenceIds;
        public IReadOnlyList<string> CompletedProductionSceneIds =>
            data.completedProductionSceneIds;
        public IReadOnlyList<string> UnlockedDeductionIds => data.unlockedDeductionIds;
        public string CurrentLocationCode => data.currentLocationCode;

        public event Action StateChanged;
        public event Action<string> FeedbackRequested;
        public event Action<StateThresholdKind> StateThresholdReached;
        public event Action<string> BadEndTriggered;

        private void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Destroy(gameObject);
                return;
            }

            Instance = this;
            ReloadSavedState();
        }

        public void ReloadSavedState()
        {
            Load();
            Normalize();
        }

        public void StartNewGame()
        {
            data = CreateDefaultData();
            SaveAndNotify();
            FeedbackRequested?.Invoke("\uC0C8 \uC218\uC0AC\uB97C \uC2DC\uC791\uD569\uB2C8\uB2E4.");
        }

        public int GetTrust(string characterId)
        {
            CharacterTrustState entry = FindTrust(characterId);
            return entry != null ? entry.value : Mathf.Clamp(startingTrust, 0, MaxTrust);
        }

        public void ApplyChoiceEffects(
            string targetCharacterId,
            int trustDelta,
            int anxietyDelta,
            int integrityDelta)
        {
            ChangeTrust(targetCharacterId, trustDelta, false);
            ChangePublicAnxiety(anxietyDelta, false);
            ChangeEvidenceIntegrity(integrityDelta, false);
            SaveAndNotify();
        }

        public void ChangeTrust(string characterId, int delta)
        {
            if (ChangeTrust(characterId, delta, true))
            {
                SaveAndNotify();
            }
        }

        public void ChangePublicAnxiety(int delta)
        {
            if (ChangePublicAnxiety(delta, true))
            {
                SaveAndNotify();
            }
        }

        public void ChangeEvidenceIntegrity(int delta)
        {
            if (ChangeEvidenceIntegrity(delta, true))
            {
                SaveAndNotify();
            }
        }

        public bool ActivateTheory(string theoryId)
        {
            string normalized = NormalizeId(theoryId);
            if (string.IsNullOrEmpty(normalized) || data.activeTheories.Contains(normalized))
            {
                return false;
            }

            if (data.activeTheories.Count >= data.theorySlots)
            {
                FeedbackRequested?.Invoke(
                    "\uD65C\uC131 \uAC00\uC124 \uC2AC\uB86F\uC774 \uAC00\uB4DD \uCC3C\uC2B5\uB2C8\uB2E4.");
                return false;
            }

            data.activeTheories.Add(normalized);
            SaveAndNotify();
            FeedbackRequested?.Invoke(
                $"\uAC00\uC124 \uD65C\uC131\uD654 \u00B7 {normalized}");
            return true;
        }

        public bool RemoveTheory(string theoryId)
        {
            if (!data.activeTheories.Remove(NormalizeId(theoryId)))
            {
                return false;
            }

            SaveAndNotify();
            return true;
        }

        public void RecordEvidenceCollected(string evidenceId)
        {
            string normalized = NormalizeId(evidenceId);
            if (string.IsNullOrEmpty(normalized) || data.collectedEvidenceIds.Contains(normalized))
            {
                return;
            }

            data.collectedEvidenceIds.Add(normalized);
            SaveAndNotify();
        }

        public bool HasCompletedScene(string sceneId)
        {
            string normalized = NormalizeSceneId(sceneId);
            return !string.IsNullOrEmpty(normalized) &&
                   data.completedProductionSceneIds.Contains(normalized);
        }

        public bool RecordCompletedScene(string sceneId)
        {
            string normalized = NormalizeSceneId(sceneId);
            if (string.IsNullOrEmpty(normalized) ||
                data.completedProductionSceneIds.Contains(normalized))
            {
                return false;
            }

            data.completedProductionSceneIds.Add(normalized);
            SaveAndNotify();
            return true;
        }

        public bool IsTheoryActive(string theoryId)
        {
            string normalized = NormalizeId(theoryId);
            return !string.IsNullOrEmpty(normalized) &&
                   data.activeTheories.Contains(normalized);
        }

        public bool HasUnlockedDeduction(string deductionId)
        {
            string normalized = NormalizeId(deductionId);
            return !string.IsNullOrEmpty(normalized) &&
                   data.unlockedDeductionIds.Contains(normalized);
        }

        public bool UnlockDeduction(string deductionId)
        {
            string normalized = NormalizeId(deductionId);
            if (string.IsNullOrEmpty(normalized) ||
                data.unlockedDeductionIds.Contains(normalized))
            {
                return false;
            }

            data.unlockedDeductionIds.Add(normalized);
            SaveAndNotify();
            FeedbackRequested?.Invoke($"추론 해금 · {normalized}");
            return true;
        }

        public void RecordLocation(string locationCode)
        {
            string normalized = NormalizeId(locationCode);
            if (string.IsNullOrEmpty(normalized) || data.currentLocationCode == normalized)
            {
                return;
            }

            data.currentLocationCode = normalized;
            SaveAndNotify();
        }

        public void SetTime(int day, TimeBlock timeBlock)
        {
            int normalizedDay = Mathf.Max(1, day);
            if (data.day == normalizedDay && data.timeBlock == timeBlock)
            {
                return;
            }

            data.day = normalizedDay;
            data.timeBlock = timeBlock;
            SaveAndNotify();
        }

        public bool HasFlag(string flag)
        {
            string normalized = NormalizeId(flag);
            return !string.IsNullOrEmpty(normalized) && data.flags.Contains(normalized);
        }

        public void AddFlag(string flag, string displayName = null)
        {
            if (!AddFlagInternal(flag))
            {
                return;
            }

            SaveAndNotify();
            FeedbackRequested?.Invoke(
                $"{(string.IsNullOrWhiteSpace(displayName) ? flag : displayName)} \uD68D\uB4DD");
        }

        public void RemoveFlag(string flag)
        {
            if (!data.flags.Remove(NormalizeId(flag)))
            {
                return;
            }

            SaveAndNotify();
        }

        private bool ChangeTrust(string characterId, int delta, bool showFeedback)
        {
            if (string.IsNullOrWhiteSpace(characterId) || delta == 0)
            {
                return false;
            }

            CharacterTrustState entry = FindTrust(characterId);
            if (entry == null)
            {
                entry = new CharacterTrustState
                {
                    characterId = NormalizeId(characterId),
                    value = Mathf.Clamp(startingTrust, 0, MaxTrust)
                };
                data.trust.Add(entry);
            }

            int previous = entry.value;
            entry.value = Mathf.Clamp(entry.value + delta, 0, MaxTrust);
            if (entry.value == previous)
            {
                return false;
            }

            if (showFeedback)
            {
                FeedbackRequested?.Invoke(
                    $"{characterId} \uC2E0\uB8B0 " +
                    $"{(entry.value - previous > 0 ? "+" : string.Empty)}{entry.value - previous}");
            }

            return true;
        }

        private bool ChangePublicAnxiety(int delta, bool showFeedback)
        {
            if (delta == 0)
            {
                return false;
            }

            int previous = data.publicAnxiety;
            data.publicAnxiety = Mathf.Clamp(data.publicAnxiety + delta, 0, MaxPercent);
            if (data.publicAnxiety == previous)
            {
                return false;
            }

            UpdateAnxietyThresholds(previous);
            if (showFeedback)
            {
                FeedbackRequested?.Invoke(
                    $"\uC2B9\uAC1D \uBD88\uC548 " +
                    $"{(data.publicAnxiety - previous > 0 ? "+" : string.Empty)}{data.publicAnxiety - previous}");
            }

            return true;
        }

        private bool ChangeEvidenceIntegrity(int delta, bool showFeedback)
        {
            if (delta == 0)
            {
                return false;
            }

            int previous = data.evidenceIntegrity;
            data.evidenceIntegrity = Mathf.Clamp(data.evidenceIntegrity + delta, 0, MaxPercent);
            if (data.evidenceIntegrity == previous)
            {
                return false;
            }

            if (data.evidenceIntegrity == 0)
            {
                AddFlagInternal("bad_end_integrity");
                StateThresholdReached?.Invoke(StateThresholdKind.EvidenceIntegrityBadEnd);
                BadEndTriggered?.Invoke(
                    "\uD604\uC7A5 \uBCF4\uC874\uB3C4\uAC00 0\uC774 \uB418\uC5B4 " +
                    "\uD575\uC2EC \uC99D\uAC70\uAC00 \uD30C\uAD34\uB418\uC5C8\uC2B5\uB2C8\uB2E4.");
            }

            if (showFeedback)
            {
                FeedbackRequested?.Invoke(
                    $"\uD604\uC7A5 \uBCF4\uC874\uB3C4 " +
                    $"{(data.evidenceIntegrity - previous > 0 ? "+" : string.Empty)}{data.evidenceIntegrity - previous}");
            }

            return true;
        }

        private void UpdateAnxietyThresholds(int previousAnxiety)
        {
            if (data.publicAnxiety >= RestrictedAreaAnxiety)
            {
                if (AddFlagInternal("restricted_areas_closed") &&
                    previousAnxiety < RestrictedAreaAnxiety)
                {
                    StateThresholdReached?.Invoke(StateThresholdKind.PublicAnxietyRestriction);
                    FeedbackRequested?.Invoke(
                        "\uACBD\uACE0 \u00B7 \uC2B9\uAC1D \uBD88\uC548\uC73C\uB85C " +
                        "\uC81C\uD55C\uAD6C\uC5ED\uC774 \uD3D0\uC1C4\uB429\uB2C8\uB2E4.");
                }
            }
            else
            {
                data.flags.Remove("restricted_areas_closed");
            }

            if (data.publicAnxiety == MaxPercent && previousAnxiety < MaxPercent)
            {
                AddFlagInternal("bad_end_panic");
                StateThresholdReached?.Invoke(StateThresholdKind.PublicAnxietyBadEnd);
                BadEndTriggered?.Invoke(
                    "\uC2B9\uAC1D \uBD88\uC548\uC774 100\uC5D0 \uB3C4\uB2EC\uD588\uC2B5\uB2C8\uB2E4.");
            }
        }

        private bool AddFlagInternal(string flag)
        {
            string normalized = NormalizeId(flag);
            if (string.IsNullOrEmpty(normalized) || data.flags.Contains(normalized))
            {
                return false;
            }

            data.flags.Add(normalized);
            return true;
        }

        private void OnDestroy()
        {
            if (Instance == this)
            {
                Instance = null;
            }
        }

        private CharacterTrustState FindTrust(string characterId)
        {
            string normalized = NormalizeId(characterId);
            return data.trust.Find(entry =>
                entry != null &&
                string.Equals(entry.characterId, normalized, StringComparison.OrdinalIgnoreCase));
        }

        private GameStateSaveData CreateDefaultData()
        {
            return new GameStateSaveData
            {
                publicAnxiety = Mathf.Clamp(startingPublicAnxiety, 0, MaxPercent),
                evidenceIntegrity = Mathf.Clamp(startingEvidenceIntegrity, 0, MaxPercent),
                theorySlots = Mathf.Max(1, startingTheorySlots)
            };
        }

        private static string NormalizeId(string value)
        {
            return string.IsNullOrWhiteSpace(value) ? string.Empty : value.Trim();
        }

        private static string NormalizeSceneId(string value)
        {
            return NormalizeId(value).ToUpperInvariant();
        }

        private static List<string> NormalizeSceneIds(IEnumerable<string> values)
        {
            var normalized = new List<string>();
            if (values == null)
            {
                return normalized;
            }

            foreach (string value in values)
            {
                string sceneId = NormalizeSceneId(value);
                if (!string.IsNullOrEmpty(sceneId) && !normalized.Contains(sceneId))
                {
                    normalized.Add(sceneId);
                }
            }

            return normalized;
        }

        private void Normalize()
        {
            data ??= CreateDefaultData();
            data.day = Mathf.Max(1, data.day);
            data.publicAnxiety = Mathf.Clamp(data.publicAnxiety, 0, MaxPercent);
            data.evidenceIntegrity = Mathf.Clamp(data.evidenceIntegrity, 0, MaxPercent);
            data.theorySlots = Mathf.Max(1, data.theorySlots);
            data.activeTheories ??= new List<string>();
            data.trust ??= new List<CharacterTrustState>();
            data.flags ??= new List<string>();
            data.collectedEvidenceIds ??= new List<string>();
            data.completedProductionSceneIds =
                NormalizeSceneIds(data.completedProductionSceneIds);
            data.unlockedDeductionIds = NormalizeIds(data.unlockedDeductionIds);
            data.currentLocationCode ??= string.Empty;

            if (data.activeTheories.Count > data.theorySlots)
            {
                data.activeTheories.RemoveRange(
                    data.theorySlots,
                    data.activeTheories.Count - data.theorySlots);
            }

            foreach (CharacterTrustState entry in data.trust)
            {
                if (entry != null)
                {
                    entry.value = Mathf.Clamp(entry.value, 0, MaxTrust);
                }
            }

            UpdateAnxietyThresholds(data.publicAnxiety);
        }

        private void SaveAndNotify()
        {
            Normalize();
            PlayerPrefs.SetString(SaveKey, JsonUtility.ToJson(data));
            PlayerPrefs.Save();
            StateChanged?.Invoke();
        }

        private static List<string> NormalizeIds(IEnumerable<string> values)
        {
            var normalized = new List<string>();
            if (values == null)
            {
                return normalized;
            }

            foreach (string value in values)
            {
                string id = NormalizeId(value);
                if (!string.IsNullOrEmpty(id) && !normalized.Contains(id))
                {
                    normalized.Add(id);
                }
            }

            return normalized;
        }

        private void Load()
        {
            if (!PlayerPrefs.HasKey(SaveKey))
            {
                data = CreateDefaultData();
                return;
            }

            try
            {
                data = JsonUtility.FromJson<GameStateSaveData>(PlayerPrefs.GetString(SaveKey));
            }
            catch (Exception exception)
            {
                Debug.LogWarning($"Game state could not be loaded: {exception.Message}");
                data = CreateDefaultData();
            }
        }
    }
}
