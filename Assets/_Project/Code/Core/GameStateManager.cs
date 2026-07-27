using System;
using System.Collections.Generic;
using System.Linq;
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
    public class RuntimeCounterState
    {
        public string key = string.Empty;
        public int value;
    }

    [Serializable]
    public class PuzzleSessionState
    {
        public string puzzleId = string.Empty;
        public List<string> selectedIds = new();
        public int step;
        public int hintLevel;
        public bool completed;

        public PuzzleSessionState Copy()
        {
            return new PuzzleSessionState
            {
                puzzleId = puzzleId,
                selectedIds = new List<string>(selectedIds ?? new List<string>()),
                step = step,
                hintLevel = hintLevel,
                completed = completed
            };
        }
    }

    [Serializable]
    public class ProductionDialogueCheckpoint
    {
        public string activeSceneId = string.Empty;
        public int lineIndex;
        public bool awaitingChoice;
        public string pendingInteractionId = string.Empty;

        public ProductionDialogueCheckpoint Copy()
        {
            return new ProductionDialogueCheckpoint
            {
                activeSceneId = activeSceneId,
                lineIndex = lineIndex,
                awaitingChoice = awaitingChoice,
                pendingInteractionId = pendingInteractionId
            };
        }
    }

    [Serializable]
    internal class GameStateSaveData
    {
        public int day = 1;
        public TimeBlock timeBlock = TimeBlock.AM;
        public int publicAnxiety = 15;
        public int evidenceIntegrity = 100;
        public List<CharacterTrustState> trust = new();
        public List<string> flags = new();
        public List<string> collectedEvidenceIds = new();
        public List<string> completedProductionSceneIds = new();
        public List<string> completedObjectiveIds = new();
        public List<PuzzleSessionState> puzzleSessions = new();
        public List<string> unlockedDeductionIds = new();
        public List<string> unlockedProductionSceneIds = new();
        public List<RuntimeCounterState> runtimeCounters = new();
        public string finalEndingId = string.Empty;
        public string currentLocationCode = string.Empty;
        public ProductionDialogueCheckpoint dialogueCheckpoint;
    }

    public class GameStateManager : MonoBehaviour
    {
        public const int DefaultTrust = 2;
        public const int MaxTrust = 5;
        public const int MaxPercent = 100;
        public const int RestrictedAreaAnxiety = 70;

        public static GameStateManager Instance { get; private set; }
        public static bool HasSaveData => GameStateSaveStore.HasRecoverableData();
        public static int ActiveSaveSlot => GameStateSaveStore.ActiveSlot;
        public static bool HasSaveDataInSlot(int slot) =>
            GameStateSaveStore.HasRecoverableData(slot);
        public static void SetActiveSaveSlot(int slot) =>
            GameStateSaveStore.SelectSlot(slot);

        [SerializeField] private int startingTrust = DefaultTrust;
        [SerializeField] private int startingPublicAnxiety = 15;
        [SerializeField] private int startingEvidenceIntegrity = 100;
        [SerializeField] private GameStateSaveData data = new();

        private int stateBatchDepth;
        private bool stateBatchDirty;
        private long saveGeneration;

        public int Day => data.day;
        public TimeBlock CurrentTimeBlock => data.timeBlock;
        public int PublicAnxiety => data.publicAnxiety;
        public int EvidenceIntegrity => data.evidenceIntegrity;
        public IReadOnlyList<string> CollectedEvidenceIds => data.collectedEvidenceIds;
        public IReadOnlyList<string> CompletedProductionSceneIds =>
            data.completedProductionSceneIds;
        public IReadOnlyList<string> CompletedObjectiveIds => data.completedObjectiveIds;
        public IReadOnlyList<string> UnlockedDeductionIds => data.unlockedDeductionIds;
        public IReadOnlyList<string> UnlockedProductionSceneIds =>
            data.unlockedProductionSceneIds;
        public int WrongStrikeCount => GetRuntimeCounter("wrong_strike");
        public string FinalEndingId => data.finalEndingId;
        public string CurrentLocationCode => data.currentLocationCode;
        public ProductionDialogueCheckpoint DialogueCheckpoint =>
            data.dialogueCheckpoint?.Copy();

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
            GameStateSaveLoadResult loaded = GameStateSaveStore.Load();
            data = loaded.Data ?? CreateDefaultData();
            saveGeneration = loaded.Generation;
            Normalize();
            if (loaded.NeedsRewrite)
            {
                saveGeneration = GameStateSaveStore.Save(data, saveGeneration);
            }
            if (!string.IsNullOrEmpty(loaded.Warning))
            {
                Debug.LogWarning(loaded.Warning);
            }
        }

        public void SelectSaveSlot(int slot)
        {
            GameStateSaveStore.SelectSlot(slot);
            ReloadSavedState();
        }

        public void StartNewGame()
        {
            GameStateSaveStore.ClearAll();
            saveGeneration = 0;
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

        public void RecordEvidenceCollected(string evidenceId)
        {
            string normalized = NormalizeCanonicalId(evidenceId);
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

        public bool UnlockProductionScene(string sceneId)
        {
            string normalized = NormalizeSceneId(sceneId);
            if (string.IsNullOrEmpty(normalized) ||
                data.unlockedProductionSceneIds.Contains(normalized))
            {
                return false;
            }

            data.unlockedProductionSceneIds.Add(normalized);
            SaveAndNotify();
            return true;
        }

        public bool IsProductionSceneUnlocked(string sceneId)
        {
            string normalized = NormalizeSceneId(sceneId);
            return !string.IsNullOrEmpty(normalized) &&
                   data.unlockedProductionSceneIds.Contains(normalized);
        }

        public bool SaveDialogueCheckpoint(
            string sceneId,
            int lineIndex,
            bool awaitingChoice,
            string pendingInteractionId)
        {
            string normalizedSceneId = NormalizeSceneId(sceneId);
            if (string.IsNullOrEmpty(normalizedSceneId) || lineIndex < 0)
            {
                return false;
            }

            data.dialogueCheckpoint = new ProductionDialogueCheckpoint
            {
                activeSceneId = normalizedSceneId,
                lineIndex = lineIndex,
                awaitingChoice = awaitingChoice,
                pendingInteractionId = NormalizeObjectiveId(pendingInteractionId)
            };
            SaveAndNotify();
            return true;
        }

        public void ClearDialogueCheckpoint()
        {
            if (data.dialogueCheckpoint == null)
            {
                return;
            }

            data.dialogueCheckpoint = null;
            SaveAndNotify();
        }

        public bool ClearDialogueCheckpoint(
            string sceneId,
            string pendingInteractionId = "")
        {
            string normalizedSceneId = NormalizeSceneId(sceneId);
            string normalizedInteractionId =
                NormalizeObjectiveId(pendingInteractionId);
            if (!DialogueCheckpointMatches(
                    normalizedSceneId,
                    normalizedInteractionId))
            {
                return false;
            }

            data.dialogueCheckpoint = null;
            SaveAndNotify();
            return true;
        }

        internal IDisposable BeginStateBatch()
        {
            stateBatchDepth++;
            return new StateBatchScope(this);
        }

        public bool TrySynchronizeProductionSceneCompletion(
            string sceneId,
            string interactionId,
            out bool newlyCompleted,
            out bool checkpointCleared)
        {
            string normalizedSceneId = NormalizeSceneId(sceneId);
            string normalizedInteractionId =
                NormalizeObjectiveId(interactionId);
            newlyCompleted = false;
            checkpointCleared = false;
            if (string.IsNullOrEmpty(normalizedSceneId) ||
                string.IsNullOrEmpty(normalizedInteractionId))
            {
                return false;
            }

            newlyCompleted =
                !data.completedProductionSceneIds.Contains(normalizedSceneId);
            checkpointCleared = DialogueCheckpointMatches(
                normalizedSceneId,
                normalizedInteractionId);
            if (!newlyCompleted && !checkpointCleared)
            {
                return true;
            }

            if (newlyCompleted)
            {
                data.completedProductionSceneIds.Add(normalizedSceneId);
            }
            if (checkpointCleared)
            {
                data.dialogueCheckpoint = null;
            }

            SaveAndNotify();
            return true;
        }

        public bool HasCompletedObjective(string objectiveId)
        {
            string normalized = NormalizeObjectiveId(objectiveId);
            return !string.IsNullOrEmpty(normalized) &&
                   data.completedObjectiveIds.Contains(normalized);
        }

        public bool RecordCompletedObjective(string objectiveId)
        {
            string normalized = NormalizeObjectiveId(objectiveId);
            if (string.IsNullOrEmpty(normalized) ||
                data.completedObjectiveIds.Contains(normalized))
            {
                return false;
            }

            data.completedObjectiveIds.Add(normalized);
            SaveAndNotify();
            return true;
        }

        public bool TryGetPuzzleSession(
            string puzzleId,
            out PuzzleSessionState session)
        {
            string normalized = NormalizeObjectiveId(puzzleId);
            PuzzleSessionState stored = data.puzzleSessions.Find(item =>
                item != null && item.puzzleId == normalized);
            session = stored?.Copy();
            return session != null;
        }

        public bool SavePuzzleSession(PuzzleSessionState session)
        {
            if (session == null)
            {
                return false;
            }

            string puzzleId = NormalizeObjectiveId(session.puzzleId);
            if (string.IsNullOrEmpty(puzzleId))
            {
                return false;
            }

            PuzzleSessionState stored = data.puzzleSessions.Find(item =>
                item != null && item.puzzleId == puzzleId);
            if (stored == null)
            {
                stored = new PuzzleSessionState { puzzleId = puzzleId };
                data.puzzleSessions.Add(stored);
            }

            stored.selectedIds = (session.selectedIds ?? new List<string>())
                .Select(NormalizeObjectiveId)
                .Where(value => !string.IsNullOrEmpty(value))
                .Distinct()
                .ToList();
            stored.step = Mathf.Max(0, session.step);
            stored.hintLevel = Mathf.Clamp(session.hintLevel, 0, 3);
            stored.completed |= session.completed;
            SaveAndNotify();
            return true;
        }

        public bool HasUnlockedDeduction(string deductionId)
        {
            string normalized = NormalizeDeductionId(deductionId);
            return !string.IsNullOrEmpty(normalized) &&
                   data.unlockedDeductionIds.Contains(normalized);
        }

        public bool UnlockDeduction(string deductionId)
        {
            string normalized = NormalizeDeductionId(deductionId);
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

        public bool TryRecordFinalEnding(string endingId)
        {
            string normalized = FinalAccusationResolver.NormalizeEndingId(endingId);
            if (string.IsNullOrEmpty(normalized) ||
                !string.IsNullOrEmpty(data.finalEndingId))
            {
                return false;
            }

            data.finalEndingId = normalized;
            SaveAndNotify();
            return true;
        }

        public void RecordLocation(string locationCode)
        {
            string normalized = NormalizeCanonicalId(locationCode);
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

        public void ConsumeTimeBlocks(int count)
        {
            int remaining = Mathf.Max(0, count);
            if (remaining == 0)
            {
                return;
            }

            int day = data.day;
            int block = (int)data.timeBlock;
            while (remaining-- > 0)
            {
                block++;
                if (block > (int)TimeBlock.NIGHT)
                {
                    block = (int)TimeBlock.AM;
                    day++;
                }
            }

            data.day = day;
            data.timeBlock = (TimeBlock)block;
            SaveAndNotify();
        }

        public int GetRuntimeCounter(string key)
        {
            string normalized = NormalizeObjectiveId(key);
            RuntimeCounterState entry = data.runtimeCounters.Find(item =>
                item != null && item.key == normalized);
            return entry?.value ?? 0;
        }

        public int ChangeRuntimeCounter(string key, int delta, int minimum = 0)
        {
            string normalized = NormalizeObjectiveId(key);
            if (string.IsNullOrEmpty(normalized) || delta == 0)
            {
                return GetRuntimeCounter(normalized);
            }

            RuntimeCounterState entry = data.runtimeCounters.Find(item =>
                item != null && item.key == normalized);
            if (entry == null)
            {
                entry = new RuntimeCounterState { key = normalized };
                data.runtimeCounters.Add(entry);
            }

            int next = Mathf.Max(minimum, entry.value + delta);
            if (entry.value != next)
            {
                entry.value = next;
                SaveAndNotify();
            }
            return entry.value;
        }

        public bool HasFlag(string flag)
        {
            string normalized = NormalizeObjectiveId(flag);
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
            if (!data.flags.Remove(NormalizeObjectiveId(flag)))
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
                PublishThreshold(StateThresholdKind.EvidenceIntegrityBadEnd);
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
                    PublishThreshold(StateThresholdKind.PublicAnxietyRestriction);
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
                PublishThreshold(StateThresholdKind.PublicAnxietyBadEnd);
                BadEndTriggered?.Invoke(
                    "\uC2B9\uAC1D \uBD88\uC548\uC774 100\uC5D0 \uB3C4\uB2EC\uD588\uC2B5\uB2C8\uB2E4.");
            }
        }

        private bool AddFlagInternal(string flag)
        {
            string normalized = NormalizeObjectiveId(flag);
            if (string.IsNullOrEmpty(normalized) || data.flags.Contains(normalized))
            {
                return false;
            }

            data.flags.Add(normalized);
            return true;
        }

        private bool DialogueCheckpointMatches(
            string normalizedSceneId,
            string normalizedInteractionId)
        {
            return data.dialogueCheckpoint != null &&
                   NormalizeSceneId(data.dialogueCheckpoint.activeSceneId) ==
                   normalizedSceneId &&
                   NormalizeObjectiveId(
                       data.dialogueCheckpoint.pendingInteractionId) ==
                   normalizedInteractionId;
        }

        private void EndStateBatch()
        {
            if (stateBatchDepth <= 0)
            {
                return;
            }

            stateBatchDepth--;
            if (stateBatchDepth == 0 && stateBatchDirty)
            {
                stateBatchDirty = false;
                PersistAndNotify();
            }
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
                evidenceIntegrity = Mathf.Clamp(startingEvidenceIntegrity, 0, MaxPercent)
            };
        }

        private static string NormalizeId(string value)
        {
            return string.IsNullOrWhiteSpace(value) ? string.Empty : value.Trim();
        }

        private static string NormalizeSceneId(string value)
        {
            return NormalizeCanonicalId(value);
        }

        private static string NormalizeCanonicalId(string value)
        {
            return NormalizeId(value).ToUpperInvariant();
        }

        private static string NormalizeObjectiveId(string value)
        {
            return NormalizeId(value).ToLowerInvariant();
        }

        private static string NormalizeDeductionId(string value)
        {
            return NormalizeObjectiveId(value).Replace(' ', '_');
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
            data.trust ??= new List<CharacterTrustState>();
            data.flags = (data.flags ?? new List<string>())
                .Select(NormalizeObjectiveId)
                .Where(value => !string.IsNullOrEmpty(value))
                .Distinct()
                .ToList();
            data.collectedEvidenceIds ??= new List<string>();
            data.completedProductionSceneIds =
                NormalizeSceneIds(data.completedProductionSceneIds);
            data.completedObjectiveIds = (data.completedObjectiveIds ?? new List<string>())
                .Select(NormalizeObjectiveId)
                .Where(value => !string.IsNullOrEmpty(value))
                .Distinct()
                .ToList();
            data.puzzleSessions ??= new List<PuzzleSessionState>();
            foreach (PuzzleSessionState session in data.puzzleSessions.Where(item => item != null))
            {
                session.puzzleId = NormalizeObjectiveId(session.puzzleId);
                session.selectedIds = (session.selectedIds ?? new List<string>())
                    .Select(NormalizeObjectiveId)
                    .Where(value => !string.IsNullOrEmpty(value))
                    .Distinct()
                    .ToList();
                session.step = Mathf.Max(0, session.step);
                session.hintLevel = Mathf.Clamp(session.hintLevel, 0, 3);
            }
            data.puzzleSessions = data.puzzleSessions
                .Where(item => item != null && !string.IsNullOrEmpty(item.puzzleId))
                .GroupBy(item => item.puzzleId)
                .Select(group => group.First())
                .ToList();
            data.unlockedDeductionIds = NormalizeIds(data.unlockedDeductionIds);
            data.unlockedProductionSceneIds =
                NormalizeSceneIds(data.unlockedProductionSceneIds);
            data.runtimeCounters = (data.runtimeCounters ?? new List<RuntimeCounterState>())
                .Where(item => item != null)
                .Select(item => new RuntimeCounterState
                {
                    key = NormalizeObjectiveId(item.key),
                    value = Mathf.Max(0, item.value)
                })
                .Where(item => !string.IsNullOrEmpty(item.key))
                .GroupBy(item => item.key)
                .Select(group => new RuntimeCounterState
                {
                    key = group.Key,
                    value = group.Sum(item => item.value)
                })
                .ToList();
            data.finalEndingId =
                FinalAccusationResolver.NormalizeEndingId(data.finalEndingId);
            data.currentLocationCode ??= string.Empty;
            if (data.dialogueCheckpoint != null)
            {
                data.dialogueCheckpoint.activeSceneId =
                    NormalizeSceneId(data.dialogueCheckpoint.activeSceneId);
                data.dialogueCheckpoint.lineIndex =
                    Mathf.Max(0, data.dialogueCheckpoint.lineIndex);
                data.dialogueCheckpoint.pendingInteractionId =
                    NormalizeObjectiveId(
                        data.dialogueCheckpoint.pendingInteractionId);
                if (string.IsNullOrEmpty(data.dialogueCheckpoint.activeSceneId))
                {
                    data.dialogueCheckpoint = null;
                }
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
            if (stateBatchDepth > 0)
            {
                stateBatchDirty = true;
                return;
            }

            PersistAndNotify();
        }

        private void PersistAndNotify()
        {
            Normalize();
            saveGeneration = GameStateSaveStore.Save(data, saveGeneration);
            StateChanged?.Invoke();
        }

        private sealed class StateBatchScope : IDisposable
        {
            private GameStateManager owner;

            public StateBatchScope(GameStateManager owner)
            {
                this.owner = owner;
            }

            public void Dispose()
            {
                GameStateManager current = owner;
                owner = null;
                current?.EndStateBatch();
            }
        }

        private static void PublishThreshold(StateThresholdKind kind)
        {
            InvestigationEventHub.Publish(
                InvestigationEventKind.StateThresholdReached,
                kind.ToString());
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
                string id = NormalizeDeductionId(value);
                if (!string.IsNullOrEmpty(id) && !normalized.Contains(id))
                {
                    normalized.Add(id);
                }
            }

            return normalized;
        }

    }
}
