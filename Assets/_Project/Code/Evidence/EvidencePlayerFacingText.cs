using System;
using System.Collections.Generic;
using System.Text.RegularExpressions;

namespace Wake.Evidence
{
    public static class EvidencePlayerFacingText
    {
        private static readonly Regex EvidenceCode = new(
            @"\bC[-_ ]?(\d{1,2})\b",
            RegexOptions.IgnoreCase | RegexOptions.CultureInvariant);

        private static readonly Regex InternalEffectToken = new(
            @"\b(?:evidence|flag|scene_unlock|publicAnxiety|" +
            @"evidenceIntegrity|timeBlock|trust_[A-Za-z0-9_]+)" +
            @"\s*:[^;\s]+",
            RegexOptions.IgnoreCase | RegexOptions.CultureInvariant);

        private static readonly Regex RepeatedSpaces = new(
            @"[ \t]{2,}",
            RegexOptions.CultureInvariant);

        public static IReadOnlyList<string> AllowedLatinTokens { get; } =
            new[] { "DNA", "COO", "VIP", "kg", "cm", "ES" };

        public static string AcquisitionMessage(EvidenceDefinition evidence)
        {
            string name = evidence?.DisplayName?.Trim();
            return string.IsNullOrEmpty(name)
                ? "새로운 단서를 발견했습니다"
                : $"새로운 단서를 발견했습니다\n{name}";
        }

        public static string SanitizeMessage(string message)
        {
            if (string.IsNullOrWhiteSpace(message))
            {
                return string.Empty;
            }

            string sanitized = InternalEffectToken.Replace(message, "정보");
            sanitized = EvidenceCode.Replace(sanitized, match =>
            {
                string id = $"C-{int.Parse(match.Groups[1].Value):00}";
                return CanonicalEvidenceCatalog.TryGet(
                    id,
                    out CanonicalEvidenceEntry entry)
                    ? entry.DisplayName
                    : "단서";
            });

            foreach (CanonicalEvidenceEntry entry in CanonicalEvidenceCatalog.All)
            {
                string duplicated =
                    $"{entry.DisplayName} {entry.DisplayName}";
                sanitized = sanitized.Replace(
                    duplicated,
                    entry.DisplayName,
                    StringComparison.Ordinal);
            }

            return RepeatedSpaces.Replace(sanitized, " ").Trim();
        }

        public static bool TryExtractAcquisitionName(
            string message,
            out string displayName)
        {
            const string prefix = "단서 획득:";
            string source = message?.Trim() ?? string.Empty;
            if (!source.StartsWith(prefix, StringComparison.Ordinal))
            {
                displayName = string.Empty;
                return false;
            }

            source = source.Substring(prefix.Length).Trim();
            const string auxiliaryPrefix = "보조 단서:";
            if (source.StartsWith(
                    auxiliaryPrefix,
                    StringComparison.Ordinal))
            {
                source = source.Substring(auxiliaryPrefix.Length).Trim();
            }

            displayName = SanitizeMessage(source);
            return displayName.Length > 0;
        }

        public static bool ContainsInternalCode(string message) =>
            !string.IsNullOrEmpty(message) &&
            (EvidenceCode.IsMatch(message) ||
             InternalEffectToken.IsMatch(message));
    }
}
