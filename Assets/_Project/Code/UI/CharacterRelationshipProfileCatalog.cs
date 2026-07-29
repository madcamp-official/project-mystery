using System;
using System.Collections.Generic;
using System.Linq;
using Wake.Core;

namespace Wake.UI
{
    public sealed class CharacterRelationshipProfile
    {
        public CharacterRelationshipProfile(
            string characterId,
            string role,
            string affiliation,
            string summary,
            string knownNote,
            string discoveryFlag = "",
            string discoverySceneId = "")
        {
            CharacterId = characterId ?? string.Empty;
            Role = role ?? string.Empty;
            Affiliation = affiliation ?? string.Empty;
            Summary = summary ?? string.Empty;
            KnownNote = knownNote ?? string.Empty;
            DiscoveryFlag = discoveryFlag ?? string.Empty;
            DiscoverySceneId = discoverySceneId ?? string.Empty;
        }

        public string CharacterId { get; }
        public string Role { get; }
        public string Affiliation { get; }
        public string Summary { get; }
        public string KnownNote { get; }
        public string DiscoveryFlag { get; }
        public string DiscoverySceneId { get; }

        public bool IsDiscovered(GameStateManager state)
        {
            if (string.Equals(
                    CharacterId,
                    "ADRIAN",
                    StringComparison.OrdinalIgnoreCase))
            {
                return true;
            }

            if (state == null)
            {
                return false;
            }

            return (!string.IsNullOrWhiteSpace(DiscoveryFlag) &&
                    state.HasFlag(DiscoveryFlag)) ||
                   (!string.IsNullOrWhiteSpace(DiscoverySceneId) &&
                    state.CompletedProductionSceneIds.Contains(
                        DiscoverySceneId,
                        StringComparer.OrdinalIgnoreCase));
        }
    }

    public static class CharacterRelationshipProfileCatalog
    {
        private static readonly CharacterRelationshipProfile[] Entries =
        {
            P(
                "ADRIAN",
                "사립 탐정",
                "독립 조사자",
                "MV Elysium에서 벌어진 협박과 불가능 사건을 조사하는 탐정.",
                "현재 수사를 이끌고 있으며 모든 인물의 진술과 단서를 교차 검증한다."),
            P(
                "CLAIRE",
                "호손 그룹 후계자",
                "호손 가문",
                "회사 내부 사정과 가족 신탁 문제를 잘 알고 있는 예정된 후계자.",
                "회사 비자금과 가족 문제에 예민하며 다니엘과 공개적으로 충돌했다.",
                "met_claire"),
            P(
                "DANIEL",
                "탐사 기자",
                "독립 언론인",
                "오르페우스 사고와 호손 그룹의 기록을 추적해 온 기자.",
                "탐정에게 선내의 위험을 경고했으며 협박 사건의 핵심 피해자가 되었다.",
                discoverySceneId: "P-01"),
            P(
                "RICHARD",
                "회장",
                "호손 그룹",
                "MV Elysium과 호손 그룹을 이끄는 경영자이자 이번 조사의 의뢰인.",
                "익명 협박장을 받은 뒤 아드리안에게 비공개 조사를 요청했다.",
                discoverySceneId: "P-03"),
            P(
                "EVELYN",
                "비서실 책임자",
                "호손 그룹 비서실",
                "회장의 일정과 공식 문서, 행사 운영 권한을 관리하는 실무 책임자.",
                "회장 명의 문서가 비서실 시스템을 거쳐 발송된 사실을 확인했다.",
                discoverySceneId: "P-02"),
            P(
                "THOMAS",
                "선장",
                "MV Elysium",
                "선박 운항과 승객 안전에 대한 최종 권한을 가진 선장.",
                "선장 권한으로 비공개 수사를 승인하고 선내 기록 접근에 협조했다.",
                discoverySceneId: "D1-07"),
            P(
                "MARCUS",
                "보안 책임자",
                "MV Elysium 보안부",
                "보안 카메라와 출입 기록, 제한구역 통제를 담당한다.",
                "사건 시간대에는 보안실에서 카메라와 승무원 통로를 감시했다고 진술했다.",
                "met_marcus"),
            P(
                "HELENA",
                "선내 의사",
                "MV Elysium 의무실",
                "의료 기록과 사망 시각, 혈흔 및 약물 관련 판단을 담당한다.",
                "다니엘에게 안정제를 처방했으며 의학적 판단의 독립성을 요구했다.",
                "met_helena"),
            P(
                "OWEN",
                "기관 기술 책임자",
                "MV Elysium 기관부",
                "안정화 장치와 기관 제어, 행사 설비 레일의 구조를 잘 아는 기술자.",
                "무게 이동과 설비 작동은 선박 기록에 흔적을 남긴다고 설명했다.",
                "met_owen")
        };

        private static readonly IReadOnlyDictionary<string, CharacterRelationshipProfile>
            ById = Entries.ToDictionary(
                entry => entry.CharacterId,
                StringComparer.OrdinalIgnoreCase);

        public static IReadOnlyList<CharacterRelationshipProfile> All =>
            Entries;

        public static bool TryGet(
            string characterId,
            out CharacterRelationshipProfile profile)
        {
            string normalized = characterId?.Trim() ?? string.Empty;
            return ById.TryGetValue(normalized, out profile);
        }

        private static CharacterRelationshipProfile P(
            string characterId,
            string role,
            string affiliation,
            string summary,
            string knownNote,
            string discoveryFlag = "",
            string discoverySceneId = "") =>
            new(
                characterId,
                role,
                affiliation,
                summary,
                knownNote,
                discoveryFlag,
                discoverySceneId);
    }
}
