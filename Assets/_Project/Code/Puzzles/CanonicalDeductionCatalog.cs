using System;
using System.Collections.Generic;
using System.Linq;
using Wake.Core;
using Wake.Evidence;

namespace Wake.Puzzles
{
    public sealed class CanonicalDeductionDefinition
    {
        public string Id { get; }
        public string DisplayName { get; }
        public string Conclusion { get; }
        public IReadOnlyList<string> RequiredEvidenceIds { get; }

        public CanonicalDeductionDefinition(
            string id,
            string displayName,
            string conclusion,
            params string[] requiredEvidenceIds)
        {
            Id = id;
            DisplayName = displayName;
            Conclusion = conclusion;
            RequiredEvidenceIds = requiredEvidenceIds
                .Select(CanonicalEvidenceCatalog.NormalizeId)
                .ToArray();
        }
    }

    public sealed class DeductionEvaluation
    {
        public CanonicalDeductionDefinition Definition { get; }
        public IReadOnlyList<string> MissingEvidenceIds { get; }
        public IReadOnlyList<string> UnusableEvidenceIds { get; }
        public bool HasAllEvidence => MissingEvidenceIds.Count == 0;
        public bool IsReliable => UnusableEvidenceIds.Count == 0;
        public bool CanUnlock => HasAllEvidence && IsReliable;

        public DeductionEvaluation(
            CanonicalDeductionDefinition definition,
            IReadOnlyList<string> missingEvidenceIds,
            IReadOnlyList<string> unusableEvidenceIds)
        {
            Definition = definition;
            MissingEvidenceIds = missingEvidenceIds;
            UnusableEvidenceIds = unusableEvidenceIds;
        }
    }

    public static class CanonicalDeductionCatalog
    {
        public const string SceneDenial = "scene_denial";
        public const string BodyInsertion = "body_insertion";
        public const string TransportRoute = "transport_route";
        public const string ActualMurder = "actual_murder";
        public const string CulpritLink = "culprit_link";
        public const string PastEvent = "past_event";

        private static readonly CanonicalDeductionDefinition[] Entries =
        {
            new(
                SceneDenial,
                "현장 출입 부정",
                "통상 출입구와 점검구로는 시신을 반입하지 않았다.",
                "C-03", "C-04", "C-05"),
            new(
                BodyInsertion,
                "천장 시신 반입",
                "시신은 사망 뒤 천장 레일을 통해 호라이즌 룸에 반입됐다.",
                "C-07", "C-08"),
            new(
                TransportRoute,
                "서비스 운송 경로",
                "86kg 하중은 서비스 레일을 따라 7층에서 8층으로 이동했다.",
                "C-09", "C-10"),
            new(
                ActualMurder,
                "실제 살해 장소",
                "다니엘은 밸러스트 구역에서 질소로 살해됐다.",
                "C-06", "C-12"),
            new(
                CulpritLink,
                "에블린 연결",
                "초대장, 문장 습관, DNA가 에블린을 범행에 연결한다.",
                "C-01", "C-14", "C-16"),
            new(
                PastEvent,
                "오르페우스 과거 사건",
                "과거 보험 사기 설계와 현재 살인은 구분해야 한다.",
                "C-17")
        };

        private static readonly IReadOnlyDictionary<string, CanonicalDeductionDefinition> ById =
            Entries.ToDictionary(entry => entry.Id, StringComparer.Ordinal);

        public static IReadOnlyList<CanonicalDeductionDefinition> All => Entries;

        public static bool TryGet(
            string deductionId,
            out CanonicalDeductionDefinition definition)
        {
            return ById.TryGetValue(NormalizeId(deductionId), out definition);
        }

        public static string NormalizeId(string deductionId)
        {
            return string.IsNullOrWhiteSpace(deductionId)
                ? string.Empty
                : deductionId.Trim().ToLowerInvariant().Replace(' ', '_');
        }
    }

    public sealed class CanonicalDeductionService
    {
        private readonly GameStateManager state;
        private readonly Func<string, bool> hasEvidence;

        public CanonicalDeductionService(
            GameStateManager state,
            Func<string, bool> hasEvidence)
        {
            this.state = state ?? throw new ArgumentNullException(nameof(state));
            this.hasEvidence =
                hasEvidence ?? throw new ArgumentNullException(nameof(hasEvidence));
        }

        public DeductionEvaluation Evaluate(string deductionId)
        {
            if (!CanonicalDeductionCatalog.TryGet(deductionId, out var definition))
            {
                return null;
            }

            var missing = new List<string>();
            var unusable = new List<string>();
            foreach (string evidenceId in definition.RequiredEvidenceIds)
            {
                if (!hasEvidence(evidenceId))
                {
                    missing.Add(evidenceId);
                    continue;
                }

                // 문서에는 중간 임계치가 없다. 보존도가 1 이상이면 직접 증거를
                // 사용할 수 있고, 0에서는 기존 Bad End와 함께 직접 증거가 파괴된다.
                if (state.EvidenceIntegrity == 0 &&
                    CanonicalEvidenceCatalog.TryGet(evidenceId, out var evidence) &&
                    evidence.IsDirect)
                {
                    unusable.Add(evidenceId);
                }
            }

            return new DeductionEvaluation(definition, missing, unusable);
        }

        public bool TryUnlock(string deductionId)
        {
            DeductionEvaluation evaluation = Evaluate(deductionId);
            if (evaluation == null ||
                !evaluation.CanUnlock ||
                !state.UnlockDeduction(evaluation.Definition.Id))
            {
                return false;
            }

            InvestigationEventHub.Publish(
                InvestigationEventKind.TheoryCompleted,
                evaluation.Definition.Id);
            return true;
        }

        public IReadOnlyList<string> EvaluateAndUnlockAll()
        {
            var unlocked = new List<string>();
            foreach (CanonicalDeductionDefinition definition in CanonicalDeductionCatalog.All)
            {
                if (TryUnlock(definition.Id))
                {
                    unlocked.Add(definition.Id);
                }
            }

            return unlocked;
        }

        public bool TryActivate(string deductionId)
        {
            string normalized = CanonicalDeductionCatalog.NormalizeId(deductionId);
            return state.HasUnlockedDeduction(normalized) &&
                   state.ActivateTheory(normalized);
        }
    }
}
