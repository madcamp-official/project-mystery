using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using Wake.Evidence;

namespace Wake.Exploration
{
    public enum InvestigationCompletionRule
    {
        AllRequiredPoints,
        AnyRequiredPoint,
        ManualConclusion
    }

    [Serializable]
    public sealed class InspectionPointDefinition
    {
        public InspectionPointDefinition(
            string pointId,
            string displayName,
            Rect normalizedRect,
            string observation,
            bool required = true)
        {
            PointId = pointId?.Trim().ToUpperInvariant() ?? string.Empty;
            DisplayName = displayName ?? string.Empty;
            NormalizedRect = normalizedRect;
            Observation = observation ?? string.Empty;
            Required = required;
        }

        public string PointId { get; }
        public string DisplayName { get; }
        public Rect NormalizedRect { get; }
        public string Observation { get; }
        public bool Required { get; }
    }

    public sealed class InvestigationTargetDefinition
    {
        public InvestigationTargetDefinition(
            string evidenceId,
            string locationCode,
            string sceneId,
            IEnumerable<InspectionPointDefinition> points,
            InvestigationCompletionRule completionRule =
                InvestigationCompletionRule.AllRequiredPoints)
        {
            EvidenceId = CanonicalEvidenceCatalog.NormalizeId(evidenceId);
            TargetId = $"TARGET_{EvidenceId.Replace('-', '_')}";
            LocationCode = locationCode ?? string.Empty;
            SceneId = sceneId ?? string.Empty;
            Points = (points ?? Array.Empty<InspectionPointDefinition>())
                .ToArray();
            CompletionRule = completionRule;
        }

        public string TargetId { get; }
        public string EvidenceId { get; }
        public string LocationCode { get; }
        public string SceneId { get; }
        public IReadOnlyList<InspectionPointDefinition> Points { get; }
        public InvestigationCompletionRule CompletionRule { get; }

        public bool IsComplete(Func<string, bool> inspected)
        {
            InspectionPointDefinition[] required =
                Points.Where(point => point.Required).ToArray();
            if (required.Length == 0)
                return false;
            return CompletionRule == InvestigationCompletionRule.AnyRequiredPoint
                ? required.Any(point => inspected(point.PointId))
                : required.All(point => inspected(point.PointId));
        }
    }

    public static class InvestigationTargetCatalog
    {
        private static readonly InvestigationTargetDefinition[] PilotTargets =
        {
            Target(
                "C-01", "PORT", "P-01",
                Point("FOLDED_CORNER", "접힌 모서리", .17f, .70f,
                    "모서리가 여러 번 접혔다. 우연히 구겨진 종이가 아니라 누군가 급히 숨겼던 흔적이다."),
                Point("SIGNATURE", "전자서명", .50f, .49f,
                    "리처드의 전자서명 자체는 진짜다. 문서 위조보다 발송 경로를 확인해야 한다."),
                Point("DISPATCH", "발송 정보", .76f, .28f,
                    "발송 서버가 리처드의 개인 단말이 아니라 비서실로 기록되어 있다.")),
            Target(
                "C-02", "HORIZON", "D1-06",
                Point("LOCK", "잠금 장치", .23f, .58f,
                    "잠금 장치에는 강제로 해제한 흔적이 없다."),
                Point("THRESHOLD", "문턱", .50f, .27f,
                    "젖은 신발이나 장비가 지나갔다면 남아야 할 자국이 보이지 않는다."),
                Point("HINGE", "경첩과 문틀", .76f, .59f,
                    "문은 열려 있지만 문틀의 먼지와 경첩 상태는 최근의 거친 출입을 부정한다.")),
            Target(
                "C-03", "HORIZON", "D2-01",
                Point("SALT_FILM", "염분막", .28f, .58f,
                    "발판 표면의 염분막이 끊기지 않았다. 누군가 밟았다면 생길 균열이 없다."),
                Point("PRESSURE_SENSOR", "압력 센서", .70f, .40f,
                    "센서 기록에도 사람의 하중에 해당하는 변화가 남아 있지 않다.")),
            Target(
                "C-04", "HORIZON", "D2-01",
                Point("DUCT_DUST", "덕트 먼지", .34f, .52f,
                    "먼지가 가장자리까지 고르게 쌓여 있다."),
                Point("INNER_WALL", "내부 벽면", .70f, .47f,
                    "좁은 덕트를 통과할 때 생길 긁힘이나 섬유 흔적이 없다.")),
            Target(
                "C-05", "HORIZON", "D2-01",
                Point("SCREWS", "점검구 나사", .30f, .38f,
                    "나사 홈에 새 공구 자국이 없다."),
                Point("DUST_SEAL", "먼지와 봉인", .69f, .60f,
                    "덮개 가장자리의 먼지가 균일하다. 최근 열린 적이 없는 상태다.")),
            Target(
                "C-07", "HORIZON", "D1-06",
                Point("CENTER", "혈흔 중심", .46f, .50f,
                    "혈흔이 가장 짙은 지점이 시신의 상처 위치와 어긋나 있다."),
                Point("DIRECTION", "비산 방향", .70f, .34f,
                    "흔적은 이 자리에서 상처가 생긴 것이 아니라 다른 곳에서 옮겨졌음을 가리킨다."))
        };

        private static readonly IReadOnlyDictionary<string, InvestigationTargetDefinition>
            ByEvidence = PilotTargets.ToDictionary(
                target => target.EvidenceId,
                StringComparer.Ordinal);

        public static IReadOnlyList<InvestigationTargetDefinition> Pilot =>
            PilotTargets;

        public static bool TryGet(
            string evidenceId,
            out InvestigationTargetDefinition target) =>
            ByEvidence.TryGetValue(
                CanonicalEvidenceCatalog.NormalizeId(evidenceId),
                out target);

        public static bool RequiresInspection(string evidenceId) =>
            TryGet(evidenceId, out _);

        public static string PointFlag(
            InvestigationTargetDefinition target,
            string pointId) =>
            $"inspection.{target.TargetId}.{pointId}".ToLowerInvariant();

        public static string CompletionFlag(
            InvestigationTargetDefinition target) =>
            $"inspection.{target.TargetId}.completed".ToLowerInvariant();

        private static InvestigationTargetDefinition Target(
            string evidenceId,
            string location,
            string scene,
            params InspectionPointDefinition[] points) =>
            new(evidenceId, location, scene, points);

        private static InspectionPointDefinition Point(
            string id,
            string name,
            float x,
            float y,
            string observation)
        {
            const float size = .15f;
            return new InspectionPointDefinition(
                id,
                name,
                new Rect(x - size * .5f, y - size * .5f, size, size),
                observation);
        }
    }
}
