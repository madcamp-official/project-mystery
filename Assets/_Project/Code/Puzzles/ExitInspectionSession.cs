using System;
using System.Collections.Generic;
using System.Linq;
using Wake.Core;
using Wake.Evidence;
using Wake.Narrative;

namespace Wake.Puzzles
{
    public enum ExitInspectionResult
    {
        Recorded,
        AlreadyInspected,
        UnknownInspection,
        EvidenceUnavailable,
        SessionCompleted
    }
    public sealed class ExitInspectionDefinition
    {
        public ExitInspectionDefinition(
            string id, string title, string finding, string evidenceId)
        {
            Id = Normalize(id);
            Title = title?.Trim() ?? string.Empty;
            Finding = finding?.Trim() ?? string.Empty;
            EvidenceId = CanonicalEvidenceCatalog.NormalizeId(evidenceId);
        }
        public string Id { get; }
        public string Title { get; }
        public string Finding { get; }
        public string EvidenceId { get; }
        public static string Normalize(string value) =>
            string.IsNullOrWhiteSpace(value)
                ? string.Empty
                : value.Trim().ToLowerInvariant().Replace(' ', '_');
    }
    public static class ExitInspectionCatalog
    {
        public const string SessionId = "exit_inspection";
        public const string SceneId = "D2-01";
        public const string ExteriorLedge = "exterior_ledge";
        public const string AirDuct = "air_duct";
        public const string ServiceHatch = "service_hatch";
        public const string CompletionFlag = "pz_exit_solved";
        private static readonly ExitInspectionDefinition[] Definitions =
        {
            new(ExteriorLedge, "외벽 발판",
                "염분막과 발판 센서 기록이 온전해 외부 이동 흔적이 없다.",
                "C-03"),
            new(AirDuct, "공조 덕트",
                "덕트 내부 먼지가 끊기지 않아 사람이 통과하지 않았다.",
                "C-04"),
            new(ServiceHatch, "설비 점검구",
                "점검구 먼지가 균일해 최근 개방된 흔적이 없다.",
                "C-05")
        };
        private static readonly IReadOnlyDictionary<string, ExitInspectionDefinition>
            ById = Definitions.ToDictionary(item => item.Id, StringComparer.Ordinal);

        public static IReadOnlyList<ExitInspectionDefinition> All => Definitions;
        public static bool TryGet(string inspectionId,
            out ExitInspectionDefinition definition) =>
            ById.TryGetValue(
                ExitInspectionDefinition.Normalize(inspectionId),
                out definition);
    }
    public readonly struct ExitInspectionCompletion
    {
        public ExitInspectionCompletion(
            bool completed,
            IReadOnlyList<string> missingInspectionIds,
            IReadOnlyList<string> missingEvidenceIds)
        {
            Completed = completed;
            MissingInspectionIds = missingInspectionIds ?? Array.Empty<string>();
            MissingEvidenceIds = missingEvidenceIds ?? Array.Empty<string>();
        }
        public bool Completed { get; }
        public IReadOnlyList<string> MissingInspectionIds { get; }
        public IReadOnlyList<string> MissingEvidenceIds { get; }
    }

    public sealed class ExitInspectionSession
    {
        private readonly GameStateManager state;
        private readonly Func<string, bool> hasEvidence;
        private readonly Func<string, bool> tryGrantEvidence;
        private readonly List<string> inspectionOrder = new();
        private readonly HashSet<string> inspected = new(StringComparer.Ordinal);

        public ExitInspectionSession(
            GameStateManager state,
            Func<string, bool> hasEvidence,
            Func<string, bool> tryGrantEvidence)
        {
            this.state = state ?? throw new ArgumentNullException(nameof(state));
            this.hasEvidence = hasEvidence ??
                throw new ArgumentNullException(nameof(hasEvidence));
            this.tryGrantEvidence = tryGrantEvidence ??
                throw new ArgumentNullException(nameof(tryGrantEvidence));

            if (state.TryGetPuzzleSession(
                    ExitInspectionCatalog.SessionId,
                    out PuzzleSessionState saved))
            {
                Restore(saved);
            }
        }

        public IReadOnlyList<string> InspectionOrder => inspectionOrder;
        public int Step => inspectionOrder.Count;
        public int HintLevel { get; private set; }
        public bool IsCompleted { get; private set; }

        public bool HasInspected(string inspectionId) =>
            inspected.Contains(ExitInspectionDefinition.Normalize(inspectionId));
        public ExitInspectionResult Inspect(string inspectionId)
        {
            if (IsCompleted)
            {
                return ExitInspectionResult.SessionCompleted;
            }

            if (!ExitInspectionCatalog.TryGet(
                    inspectionId,
                    out ExitInspectionDefinition definition))
            {
                return ExitInspectionResult.UnknownInspection;
            }

            if (inspected.Contains(definition.Id))
            {
                return hasEvidence(definition.EvidenceId) ||
                       tryGrantEvidence(definition.EvidenceId)
                    ? ExitInspectionResult.AlreadyInspected
                    : ExitInspectionResult.EvidenceUnavailable;
            }

            using IDisposable batch = state.BeginStateBatch();
            if (!hasEvidence(definition.EvidenceId) &&
                !tryGrantEvidence(definition.EvidenceId))
            {
                return ExitInspectionResult.EvidenceUnavailable;
            }

            inspected.Add(definition.Id);
            inspectionOrder.Add(definition.Id);
            Save();
            return ExitInspectionResult.Recorded;
        }
        public bool UseHint()
        {
            if (IsCompleted || HintLevel >= 3)
            {
                return false;
            }

            HintLevel++;
            Save();
            return true;
        }

        public ExitInspectionCompletion TryComplete()
        {
            string[] missingInspections = ExitInspectionCatalog.All
                .Where(item => !inspected.Contains(item.Id))
                .Select(item => item.Id)
                .ToArray();
            string[] missingEvidence = ExitInspectionCatalog.All
                .Where(item => !hasEvidence(item.EvidenceId))
                .Select(item => item.EvidenceId)
                .ToArray();
            if (IsCompleted ||
                missingInspections.Length > 0 ||
                missingEvidence.Length > 0 ||
                !ProductionSceneCompletionGate.CanStartInteraction(
                    state,
                    ExitInspectionCatalog.SceneId,
                    ExitInspectionCatalog.SessionId))
            {
                return new ExitInspectionCompletion(
                    false, missingInspections, missingEvidence);
            }

            var deduction = new CanonicalDeductionService(state, hasEvidence);
            using IDisposable batch = state.BeginStateBatch();
            if (!state.HasUnlockedDeduction(CanonicalDeductionCatalog.SceneDenial) &&
                !deduction.TryUnlock(CanonicalDeductionCatalog.SceneDenial))
            {
                return new ExitInspectionCompletion(
                    false, Array.Empty<string>(), missingEvidence);
            }

            IsCompleted = true;
            Save();
            state.AddFlag(
                ExitInspectionCatalog.CompletionFlag,
                "흔적 없는 출구 검증 완료");
            if (!ProductionSceneCompletionGate.TryComplete(
                    state,
                    ExitInspectionCatalog.SceneId,
                    ExitInspectionCatalog.SessionId))
            {
                return new ExitInspectionCompletion(
                    false, Array.Empty<string>(), Array.Empty<string>());
            }

            return new ExitInspectionCompletion(
                true, Array.Empty<string>(), Array.Empty<string>());
        }

        private void Restore(PuzzleSessionState saved)
        {
            foreach (string sourceId in
                     saved.selectedIds ?? new List<string>())
            {
                string inspectionId = ExitInspectionDefinition.Normalize(sourceId);
                if (ExitInspectionCatalog.TryGet(inspectionId, out _) &&
                    inspected.Add(inspectionId))
                {
                    inspectionOrder.Add(inspectionId);
                }
            }

            HintLevel = Math.Clamp(saved.hintLevel, 0, 3);
            IsCompleted = saved.completed &&
                          inspected.Count == ExitInspectionCatalog.All.Count;
        }

        private void Save()
        {
            state.SavePuzzleSession(new PuzzleSessionState
            {
                puzzleId = ExitInspectionCatalog.SessionId,
                selectedIds = new List<string>(inspectionOrder),
                step = inspectionOrder.Count,
                hintLevel = HintLevel,
                completed = IsCompleted
            });
        }
    }
}
