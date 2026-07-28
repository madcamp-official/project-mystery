using UnityEngine;
using Wake.Exploration;

namespace Wake.UI
{
    public readonly struct NarrativeLocationHUDLayout
    {
        public NarrativeLocationHUDLayout(
            float width,
            float height,
            float topOffset)
        {
            Width = width;
            Height = height;
            TopOffset = topOffset;
        }

        public float Width { get; }
        public float Height { get; }
        public float TopOffset { get; }
    }

    public readonly struct NarrativeLocationHUDViewModel
    {
        public NarrativeLocationHUDViewModel(
            bool isVisible,
            bool isWarning,
            string eyebrow,
            string title,
            string supportingText,
            Color backgroundColor)
        {
            IsVisible = isVisible;
            IsWarning = isWarning;
            Eyebrow = eyebrow ?? string.Empty;
            Title = title ?? string.Empty;
            SupportingText = supportingText ?? string.Empty;
            BackgroundColor = backgroundColor;
        }

        public bool IsVisible { get; }
        public bool IsWarning { get; }
        public string Eyebrow { get; }
        public string Title { get; }
        public string SupportingText { get; }
        public Color BackgroundColor { get; }
        public string DisplayText =>
            !IsVisible
                ? string.Empty
                : $"<size=14><color=#D9B56D>{Eyebrow}</color></size>\n" +
                  $"<size=22>{Title}</size>" +
                  (string.IsNullOrEmpty(SupportingText)
                      ? string.Empty
                      : $"\n<size=13>{SupportingText}</size>");
    }

    public static class NarrativeLocationHUDPresentation
    {
        public const float MaximumWidth = 560f;
        public const float MinimumWidth = 280f;
        public const float PreferredHeight = 94f;
        public const float TopOffset = 184f;
        public const float HorizontalMargin = 16f;

        private static readonly Color ResolvedColor =
            new(0.08f, 0.16f, 0.21f, 0.94f);
        private static readonly Color WarningColor =
            new(0.32f, 0.18f, 0.08f, 0.96f);

        public static NarrativeLocationHUDViewModel Create(
            NarrativeLocationContext context)
        {
            if (context.Kind == NarrativeLocationKind.Undocumented)
            {
                return new NarrativeLocationHUDViewModel(
                    false,
                    false,
                    string.Empty,
                    string.Empty,
                    string.Empty,
                    Color.clear);
            }

            bool warning = context.IsDialogueOnly;
            return new NarrativeLocationHUDViewModel(
                true,
                warning,
                warning
                    ? "위치 확인 필요"
                    : "현재 위치",
                warning
                    ? $"⚠ {context.DisplayName}"
                    : context.DisplayName,
                warning
                    ? context.WarningMessage
                    : "탐색 가능한 현장",
                warning ? WarningColor : ResolvedColor);
        }

        public static NarrativeLocationHUDLayout CalculateLayout(
            float viewportWidth,
            float safeAreaWidth)
        {
            float availableWidth = Mathf.Max(
                MinimumWidth,
                Mathf.Min(viewportWidth, safeAreaWidth) -
                HorizontalMargin * 2f);
            return new NarrativeLocationHUDLayout(
                Mathf.Clamp(
                    availableWidth,
                    MinimumWidth,
                    MaximumWidth),
                PreferredHeight,
                TopOffset);
        }
    }
}
