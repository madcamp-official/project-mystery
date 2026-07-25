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
    }

    public class GameStateManager : MonoBehaviour
    {
        private const string SaveKey = "THE_WAKE_GAME_STATE_V1";

        public const int DefaultTrust = 2;
        public const int MaxTrust = 5;
        public const int MaxPercent = 100;
        public const int RestrictedAreaAnxiety = 70;

        public static GameStateManager Instance { get; private set; }

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
            Load();
            Normalize();
        }

        public void StartNewGame()
        {
            data = CreateDefaultData();
            SaveAndNotify();
            FeedbackRequested?.Invoke("새 수사를 시작합니다.");
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
            if (string.IsNullOrWhiteSpace(theoryId) || data.activeTheories.Contains(theoryId))
            {
                return false;
            }

            if (data.activeTheories.Count >= data.theorySlots)
            {
                FeedbackRequested?.Invoke("활성 가설 슬롯이 가득 찼습니다.");
                return false;
            }

            data.activeTheories.Add(theoryId);
            SaveAndNotify();
            FeedbackRequested?.Invoke($"가설 활성화 · {theoryId}");
            return true;
        }

        public bool RemoveTheory(string theoryId)
        {
            if (!data.activeTheories.Remove(theoryId))
            {
                return false;
            }

            SaveAndNotify();
            return true;
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
            return !string.IsNullOrWhiteSpace(flag) && data.flags.Contains(flag);
        }

        public void AddFlag(string flag, string displayName = null)
        {
            if (!AddFlagInternal(flag))
            {
                return;
            }

            SaveAndNotify();
            FeedbackRequested?.Invoke(
                $"{(string.IsNullOrWhiteSpace(displayName) ? flag : displayName)} 획득");
        }

        public void RemoveFlag(string flag)
        {
            if (string.IsNullOrWhiteSpace(flag) || !data.flags.Remove(flag))
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
                    $"{characterId} 신뢰 {(entry.value - previous > 0 ? "+" : string.Empty)}{entry.value - previous}");
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
                    $"승객 불안 {(data.publicAnxiety - previous > 0 ? "+" : string.Empty)}{data.publicAnxiety - previous}");
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
                BadEndTriggered?.Invoke("현장 보존도가 0이 되어 핵심 증거가 파괴되었습니다.");
            }

            if (showFeedback)
            {
                FeedbackRequested?.Invoke(
                    $"현장 보존도 {(data.evidenceIntegrity - previous > 0 ? "+" : string.Empty)}{data.evidenceIntegrity - previous}");
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
                    FeedbackRequested?.Invoke("경고 · 승객 불안으로 제한구역이 폐쇄됩니다.");
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
                BadEndTriggered?.Invoke("승객 불안이 100에 도달했습니다.");
            }
        }

        private bool AddFlagInternal(string flag)
        {
            if (string.IsNullOrWhiteSpace(flag) || data.flags.Contains(flag))
            {
                return false;
            }

            data.flags.Add(flag);
            return true;
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
