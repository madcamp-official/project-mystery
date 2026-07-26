using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace Wake.Evidence
{
    public enum CanonicalEvidenceGrantMode
    {
        DialogueLine,
        Interaction
    }

    public sealed class CanonicalEvidenceEntry
    {
        public string Id { get; }
        public string DisplayName { get; }
        public string Description { get; }
        public string Category { get; }
        public bool IsDirect { get; }
        public CanonicalEvidenceGrantMode GrantMode { get; }
        public IReadOnlyList<string> GrantLineIds { get; }

        public CanonicalEvidenceEntry(
            string id,
            string displayName,
            string description,
            string category,
            bool isDirect,
            params string[] grantLineIds)
            : this(
                id,
                displayName,
                description,
                category,
                isDirect,
                CanonicalEvidenceGrantMode.DialogueLine,
                grantLineIds)
        {
        }

        public CanonicalEvidenceEntry(
            string id,
            string displayName,
            string description,
            string category,
            bool isDirect,
            CanonicalEvidenceGrantMode grantMode,
            params string[] grantLineIds)
        {
            Id = id;
            DisplayName = displayName;
            Description = description;
            Category = category;
            IsDirect = isDirect;
            GrantMode = grantMode;
            GrantLineIds = grantLineIds ?? Array.Empty<string>();
        }
    }

    public static class CanonicalEvidenceCatalog
    {
        private static readonly CanonicalEvidenceEntry[] Entries =
        {
            new("C-01", "Daniel의 초대장",
                "Richard 전자서명은 진짜지만 발송 서버는 비서실이다.",
                "invitation", false, "p_02_04"),
            new("C-02", "열린 출입문",
                "잠금 트릭이 아니라 출입 흔적 부재가 문제다.",
                "exit", true, "d1_06_02"),
            new("C-03", "외벽 발판",
                "염분막과 센서 기록이 온전하다.",
                "exit", true, CanonicalEvidenceGrantMode.Interaction, "d2_01_05"),
            new("C-04", "덕트 먼지",
                "통과 흔적이 없다.",
                "exit", true, CanonicalEvidenceGrantMode.Interaction, "d2_01_05"),
            new("C-05", "점검구 먼지",
                "먼지가 균일하게 유지되어 있다.",
                "exit", true, CanonicalEvidenceGrantMode.Interaction, "d2_01_05"),
            new("C-06", "구두 밑창",
                "Horizon 카펫이 아니라 Ballast 바닥 고무가 묻어 있다.",
                "forensic", true, "d6_03_04"),
            new("C-07", "혈흔 중심",
                "상처 위치와 혈흔 중심이 일치하지 않는다.",
                "forensic", true, "d2_02_04"),
            new("C-08", "화재감지기 오류",
                "22:18 천장 레일 통과 시각에 오류가 기록됐다.",
                "timeline", false, "d2_04_04"),
            new("C-09", "안정화 로그",
                "86kg이 7층에서 8층으로 이동했다.",
                "transport", false, "d6_01_04"),
            new("C-10", "운반백 자국",
                "어깨와 허리에 압박 흔적이 남아 있다.",
                "transport", true, "d6_02_04"),
            new("C-11", "안정제",
                "사망 시각 오판에 기여했다.",
                "medical", true, "d2_03_04"),
            new("C-12", "질소 로그",
                "Daniel의 실제 직접 사인을 입증한다.",
                "medical", false, "d6_03_04"),
            new("C-13", "익명 채팅",
                "Richard 유죄 가설을 강화하는 선택적 진실이다.",
                "communication", false, "d5_03_04"),
            new("C-14", "문장 습관",
                "Evelyn이 반복해서 사용하는 표현이다.",
                "communication", false, "d3_05_04"),
            new("C-15", "Marcus 인증",
                "금고 접근을 지원한 인증 기록이다.",
                "authentication", false, "d4_04_04"),
            new("C-16", "보호면 DNA",
                "Daniel과 Evelyn의 직접 접촉을 입증한다.",
                "identity", true, "d7_02_04"),
            new("C-17", "Orpheus 음성",
                "Richard의 무지와 Evelyn의 계획을 분리한다.",
                "history", false, "d7_03_04"),
            new("C-18", "수정 기사",
                "피해자의 오판을 사후에 바로잡는다.",
                "resolution", false, "d8_03_04")
        };

        private static readonly IReadOnlyDictionary<string, CanonicalEvidenceEntry> ById =
            Entries.ToDictionary(entry => entry.Id, StringComparer.Ordinal);

        private static readonly IReadOnlyDictionary<string, IReadOnlyList<string>> ByGrantLine =
            Entries
                .Where(entry =>
                    entry.GrantMode == CanonicalEvidenceGrantMode.DialogueLine)
                .SelectMany(entry => entry.GrantLineIds.Select(lineId => (lineId, entry.Id)))
                .GroupBy(pair => pair.lineId, StringComparer.Ordinal)
                .ToDictionary(
                    group => group.Key,
                    group => (IReadOnlyList<string>)group.Select(pair => pair.Id).ToArray(),
                    StringComparer.Ordinal);

        public static IReadOnlyList<CanonicalEvidenceEntry> All => Entries;

        public static bool TryGet(string evidenceId, out CanonicalEvidenceEntry entry)
        {
            return ById.TryGetValue(NormalizeId(evidenceId), out entry);
        }

        public static IReadOnlyList<string> GetGrantedEvidenceIds(string stableLineId)
        {
            string normalized = string.IsNullOrWhiteSpace(stableLineId)
                ? string.Empty
                : stableLineId.Trim().ToLowerInvariant();
            return ByGrantLine.TryGetValue(normalized, out IReadOnlyList<string> ids)
                ? ids
                : Array.Empty<string>();
        }

        public static EvidenceDefinition CreateRuntimeDefinition(string evidenceId)
        {
            if (!TryGet(evidenceId, out CanonicalEvidenceEntry entry))
            {
                return null;
            }

            EvidenceDefinition definition =
                ScriptableObject.CreateInstance<EvidenceDefinition>();
            definition.name = $"EvidenceDefinition_{entry.Id.Replace("-", string.Empty)}";
            definition.Initialize(entry);
            return definition;
        }

        public static string NormalizeId(string evidenceId)
        {
            string value = string.IsNullOrWhiteSpace(evidenceId)
                ? string.Empty
                : evidenceId.Trim().ToUpperInvariant().Replace('_', '-');
            if (value.Length >= 2 && value[0] == 'C')
            {
                string digits = value.Substring(1).TrimStart('-');
                if (int.TryParse(digits, out int number) && number > 0)
                {
                    return $"C-{number:00}";
                }
            }

            return value;
        }
    }
}
