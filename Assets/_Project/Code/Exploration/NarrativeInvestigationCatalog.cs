using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace Wake.Exploration
{
    public sealed class NarrativeInvestigationDefinition
    {
        public NarrativeInvestigationDefinition(
            string targetId,
            string locationCode,
            string sceneId,
            string displayName,
            string resourcePath,
            string imageText,
            string completionFlag,
            IEnumerable<InspectionPointDefinition> points)
        {
            TargetId = targetId?.Trim().ToUpperInvariant() ?? string.Empty;
            LocationCode = locationCode ?? string.Empty;
            SceneId = sceneId ?? string.Empty;
            DisplayName = displayName ?? string.Empty;
            ResourcePath = resourcePath ?? string.Empty;
            ImageText = imageText ?? string.Empty;
            CompletionFlag = completionFlag ?? string.Empty;
            Points = (points ?? Array.Empty<InspectionPointDefinition>())
                .ToArray();
        }

        public string TargetId { get; }
        public string LocationCode { get; }
        public string SceneId { get; }
        public string DisplayName { get; }
        public string ResourcePath { get; }
        public string ImageText { get; }
        public string CompletionFlag { get; }
        public IReadOnlyList<InspectionPointDefinition> Points { get; }

        public bool IsComplete(Func<string, bool> inspected) =>
            Points.Where(point => point.Required)
                .All(point => inspected(point.PointId));
    }

    public static class NarrativeInvestigationCatalog
    {
        public const string PortMessengerTargetId = "P01_ENCRYPTED_MESSAGE";
        public const string GangwayManifestTargetId =
            "P02_BOARDING_MANIFEST";
        public const string GangwaySignatureTargetId =
            "P02_ELECTRONIC_SIGNATURE";

        private static readonly NarrativeInvestigationDefinition[] Targets =
        {
            new(
                PortMessengerTargetId,
                "PORT",
                "P-01",
                "암호화 메신저 알림",
                "Investigation/evidence_p01_encrypted_message",
                "익명 발신자\n선미 · 21시 이후\n회장이 혼자일 때",
                "anonymous_tip_preview",
                new[]
                {
                    Point(
                        "ANONYMOUS_SENDER",
                        "발신자",
                        .50f,
                        .61f,
                        "발신자 이름 대신 익명 표식만 남아 있다."),
                    Point(
                        "MEETING_TIME",
                        "시간",
                        .50f,
                        .49f,
                        "약속 시각은 21시 이후로 지정되어 있다."),
                    Point(
                        "MEETING_CONDITION",
                        "메시지",
                        .50f,
                        .37f,
                        "선미에서 회장이 혼자일 때 만나자는 조건이 적혀 있다.")
                })
            ,
            new(
                GangwayManifestTargetId,
                "GANGWAY",
                "P-02",
                "승선 명단",
                "LocationBackgroundVariants/bg_gangway_default_luggage",
                string.Empty,
                "p02_boarding_manifest_inspected",
                new[]
                {
                    Point(
                        "DANIEL_ENTRY",
                        "다니엘의 이름",
                        .194f,
                        .607f,
                        "다니엘의 이름 옆 초대자가 리처드로 적혀 있다.",
                        .145f,
                        .075f),
                    Point(
                        "HANDWRITTEN_EDIT",
                        "수기 수정",
                        .210f,
                        .555f,
                        "인쇄된 명단 위에 누군가 급히 내용을 고쳐 썼다.",
                        .150f,
                        .070f),
                    Point(
                        "EDITOR_INITIALS",
                        "수정자 이니셜",
                        .226f,
                        .505f,
                        "수정자 표기에는 이름 대신 E.S라는 이니셜이 남아 있다.",
                        .135f,
                        .065f)
                }),
            new(
                GangwaySignatureTargetId,
                "GANGWAY",
                "P-02",
                "전자 서명 검증 단말",
                "LocationBackgroundVariants/bg_gangway_default_luggage",
                string.Empty,
                "p02_electronic_signature_inspected",
                new[]
                {
                    Point(
                        "SIGNATURE_KEY",
                        "서명 키",
                        .376f,
                        .585f,
                        "리처드의 전자 서명 키 자체는 유효하다.",
                        .090f,
                        .055f),
                    Point(
                        "SENDING_SERVER",
                        "발송 서버",
                        .405f,
                        .548f,
                        "발송 경로는 회장 개인 단말이 아니라 비서실 서버다.",
                        .085f,
                        .060f)
                })
        };

        private static readonly IReadOnlyDictionary<string,
            NarrativeInvestigationDefinition> ById =
            Targets.ToDictionary(
                target => target.TargetId,
                StringComparer.Ordinal);

        public static bool TryGet(
            string targetId,
            out NarrativeInvestigationDefinition definition) =>
            ById.TryGetValue(
                targetId?.Trim().ToUpperInvariant() ?? string.Empty,
                out definition);

        public static IReadOnlyList<NarrativeInvestigationDefinition> All =>
            Targets;

        public static IReadOnlyList<NarrativeInvestigationDefinition>
            GetForLocation(string locationCode, string sceneId) =>
            Targets.Where(target =>
                    string.Equals(
                        target.LocationCode,
                        locationCode?.Trim(),
                        StringComparison.OrdinalIgnoreCase) &&
                    string.Equals(
                        target.SceneId,
                        sceneId?.Trim(),
                        StringComparison.OrdinalIgnoreCase))
                .ToArray();

        public static string PointFlag(
            NarrativeInvestigationDefinition target,
            string pointId) =>
            $"inspection.{target.TargetId}.{pointId}".ToLowerInvariant();

        private static InspectionPointDefinition Point(
            string id,
            string name,
            float x,
            float y,
            string observation,
            float width = .23f,
            float height = .105f)
        {
            return new InspectionPointDefinition(
                id,
                name,
                new Rect(
                    x - width * .5f,
                    y - height * .5f,
                    width,
                    height),
                observation);
        }
    }
}
