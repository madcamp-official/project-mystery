using System;
using System.Collections.Generic;

namespace Wake.Narrative
{
    public enum ProductionUiEventChannel
    {
        General,
        Evidence,
        Theory,
        Status,
        Objective,
        Interaction,
        Ending
    }

    public readonly struct ProductionUiEventPresentation
    {
        public ProductionUiEventPresentation(
            ProductionUiEventChannel channel,
            string message,
            bool showToast)
        {
            Channel = channel;
            Message = message ?? string.Empty;
            ShowToast = showToast && Message.Length > 0;
        }

        public ProductionUiEventChannel Channel { get; }
        public string Message { get; }
        public bool ShowToast { get; }
    }

    public static class ProductionPresentationRouting
    {
        public static bool IsSystemEvent(DialogueRecord record)
        {
            return record != null &&
                   (EqualsToken(record.Speaker, "SYSTEM") ||
                    EqualsToken(record.LineType, "system"));
        }

        public static ProductionUiEventPresentation ClassifySystemEvent(
            DialogueRecord record)
        {
            string effect = record?.NextOrEffect ?? string.Empty;
            string stage = record?.StageDirection ?? string.Empty;
            string message = record?.TextKo ?? string.Empty;

            if (Contains(effect, "ending:") ||
                EqualsToken(record?.LineType, "ending_trigger"))
            {
                return new ProductionUiEventPresentation(
                    ProductionUiEventChannel.Ending,
                    message,
                    showToast: false);
            }

            if (Contains(effect, "evidence:") ||
                StartsWith(message, "단서 획득") ||
                StartsWith(message, "단서 등록") ||
                StartsWith(message, "메타데이터 단서"))
            {
                return new ProductionUiEventPresentation(
                    ProductionUiEventChannel.Evidence,
                    message,
                    showToast: true);
            }

            if (Contains(effect, "theory:") ||
                StartsWith(message, "가설 해금"))
            {
                return new ProductionUiEventPresentation(
                    ProductionUiEventChannel.Theory,
                    message,
                    showToast: true);
            }

            if (Contains(effect, "publicAnxiety:") ||
                Contains(effect, "evidenceIntegrity:") ||
                Contains(effect, "trust_") ||
                Contains(effect, "timeBlock:"))
            {
                return new ProductionUiEventPresentation(
                    ProductionUiEventChannel.Status,
                    message,
                    showToast: false);
            }

            if (Contains(stage, "Puzzle") ||
                Contains(stage, "Interrogation") ||
                Contains(stage, "Logic UI") ||
                Contains(stage, "Accusation"))
            {
                return new ProductionUiEventPresentation(
                    ProductionUiEventChannel.Interaction,
                    message,
                    showToast: true);
            }

            if (Contains(effect, "scene_unlock:"))
            {
                return new ProductionUiEventPresentation(
                    ProductionUiEventChannel.Objective,
                    message,
                    showToast: false);
            }

            return new ProductionUiEventPresentation(
                ProductionUiEventChannel.General,
                message,
                showToast: true);
        }

        private static bool EqualsToken(string value, string expected) =>
            string.Equals(
                value?.Trim(),
                expected,
                StringComparison.OrdinalIgnoreCase);

        private static bool Contains(string value, string token) =>
            (value ?? string.Empty).IndexOf(
                token,
                StringComparison.OrdinalIgnoreCase) >= 0;

        private static bool StartsWith(string value, string token) =>
            (value ?? string.Empty).StartsWith(
                token,
                StringComparison.OrdinalIgnoreCase);
    }

    public static class InvestigationPresentationPolicy
    {
        private static readonly HashSet<string> InvestigationMonologues =
            new(StringComparer.OrdinalIgnoreCase)
            {
                "D1-04_021",
                "D2-05_010",
                "D2-06_022",
                "D3-03_019",
                "D3-05_016",
                "D4-02_022",
                "D5-01_023",
                "D5-04_002",
                "D5-04_003",
                "D6-05_003",
                "D6-05_004",
                "D6-05_005",
                "D6-05_006",
                "D6-05_007",
                "D6-05_008"
            };

        private static readonly HashSet<string> ObservationMonologues =
            new(StringComparer.OrdinalIgnoreCase)
            {
                "D1-01_027",
                "D1-02_016"
            };

        public static bool IsMarker(DialogueRecord record)
        {
            string text = record?.TextKo?.Trim() ?? string.Empty;
            return (text.StartsWith("[조사:", StringComparison.Ordinal) ||
                    text.StartsWith(
                        "[분석 콘솔:",
                        StringComparison.Ordinal)) &&
                   text.EndsWith("]", StringComparison.Ordinal);
        }

        public static bool IsInvestigationResult(DialogueRecord record)
        {
            if (record == null)
                return false;

            return string.Equals(
                       record.LineType,
                       "inspection",
                       StringComparison.OrdinalIgnoreCase) ||
                   InvestigationMonologues.Contains(record.CanonicalLineId) ||
                   ObservationMonologues.Contains(record.CanonicalLineId);
        }

        public static bool IsObservation(DialogueRecord record) =>
            record != null &&
            ObservationMonologues.Contains(record.CanonicalLineId);

        public static string MarkerTitle(DialogueRecord record)
        {
            string text = record?.TextKo?.Trim() ?? string.Empty;
            if (!IsMarker(record))
                return "조사 대상";

            int separator = text.IndexOf(':');
            return text.Substring(
                    separator + 1,
                    text.Length - separator - 2)
                .Trim();
        }

        public static string ResultTitle(DialogueRecord record)
        {
            if (IsObservation(record))
                return "관찰 기록";

            string id = record?.CanonicalLineId ?? string.Empty;
            if (id.StartsWith("D6-05_", StringComparison.OrdinalIgnoreCase))
                return "사건 시간표 분석";
            if (id.StartsWith("D5-04_", StringComparison.OrdinalIgnoreCase))
                return "현장 재구성";
            return "조사 결과";
        }

        public static IReadOnlyCollection<string> RoutedMonologueIds =>
            InvestigationMonologues;
        public static IReadOnlyCollection<string> ObservationMonologueIds =>
            ObservationMonologues;
    }
}
