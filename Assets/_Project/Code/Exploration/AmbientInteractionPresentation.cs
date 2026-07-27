using System;
using UnityEngine;
using UnityEngine.UI;

namespace Wake.Exploration
{
    public readonly struct AmbientCharacterPlacement
    {
        public AmbientCharacterPlacement(float anchorX, Vector2 size)
        {
            AnchorX = Mathf.Clamp01(anchorX);
            Size = new Vector2(
                Mathf.Max(0f, size.x),
                Mathf.Max(0f, size.y));
        }

        public float AnchorX { get; }
        public Vector2 Size { get; }
    }

    public static class AmbientInteractionPresentation
    {
        public const float MinimumAnchorX = 0.12f;
        public const float MaximumAnchorX = 0.88f;
        public const float CharacterButtonWidth = 184f;
        public const float CharacterButtonHeight = 72f;
        public const float CharacterEdgePadding = 16f;
        public const float CharacterSpacing = 16f;

        private static readonly Color CharacterSpriteNormal = Color.white;
        private static readonly Color CharacterSpriteHover =
            new Color32(255, 235, 187, 255);
        private static readonly Color CharacterSpritePressed =
            new Color32(210, 224, 221, 255);
        private static readonly Color PanelButtonNormal =
            new Color32(24, 31, 46, 238);
        private static readonly Color PanelButtonHover =
            new Color32(42, 70, 83, 248);
        private static readonly Color PanelButtonPressed =
            new Color32(31, 93, 103, 255);
        private static readonly Color HotspotNormal =
            new Color(0.23f, 0.60f, 0.68f, 0.08f);
        private static readonly Color HotspotHover =
            new Color(0.30f, 0.78f, 0.84f, 0.28f);
        private static readonly Color HotspotPressed =
            new Color(0.88f, 0.65f, 0.30f, 0.36f);

        public static AmbientCharacterPlacement CharacterPlacement(
            int index,
            int count)
        {
            if (count <= 0)
            {
                return new AmbientCharacterPlacement(
                    0.5f,
                    new Vector2(
                        CharacterButtonWidth,
                        CharacterButtonHeight));
            }

            int safeIndex = Mathf.Clamp(index, 0, count - 1);
            float anchor = count == 1
                ? 0.5f
                : Mathf.Lerp(
                    MinimumAnchorX,
                    MaximumAnchorX,
                    safeIndex / (float)(count - 1));
            return new AmbientCharacterPlacement(
                anchor,
                new Vector2(
                    CharacterButtonWidth,
                    CharacterButtonHeight));
        }

        public static AmbientCharacterPlacement CharacterPlacement(
            int index,
            int count,
            float viewportWidth,
            float safeAreaX,
            float safeAreaWidth)
        {
            if (viewportWidth <= 0f || safeAreaWidth <= 0f)
                return CharacterPlacement(index, count);

            int safeCount = Mathf.Max(1, count);
            int safeIndex = Mathf.Clamp(index, 0, safeCount - 1);
            float clampedSafeX = Mathf.Clamp(
                safeAreaX,
                0f,
                viewportWidth);
            float clampedSafeWidth = Mathf.Clamp(
                safeAreaWidth,
                0f,
                viewportWidth - clampedSafeX);
            float totalSpacing = CharacterSpacing * (safeCount - 1);
            float availableForButtons = Mathf.Max(
                safeCount,
                clampedSafeWidth -
                CharacterEdgePadding * 2f -
                totalSpacing);
            float buttonWidth = Mathf.Min(
                CharacterButtonWidth,
                availableForButtons / safeCount);
            float groupWidth =
                buttonWidth * safeCount + totalSpacing;
            float firstCenter =
                clampedSafeX +
                (clampedSafeWidth - groupWidth) * 0.5f +
                buttonWidth * 0.5f;
            float center =
                firstCenter +
                safeIndex * (buttonWidth + CharacterSpacing);

            return new AmbientCharacterPlacement(
                center / viewportWidth,
                new Vector2(buttonWidth, CharacterButtonHeight));
        }

        public static string CharacterLabel(
            string displayName,
            bool isAvailable = true)
        {
            string safeName = string.IsNullOrWhiteSpace(displayName)
                ? "탑승객"
                : displayName.Trim();
            return isAvailable
                ? $"{safeName}\n대화 가능"
                : $"{safeName}\n대화 완료";
        }

        public static string HotspotLabel(string title)
        {
            return string.IsNullOrWhiteSpace(title)
                ? "조사"
                : $"조사 · {title.Trim()}";
        }

        public static ColorBlock CharacterSpriteColors()
        {
            return CharacterSpriteColors(CharacterSpriteNormal);
        }

        public static ColorBlock CharacterSpriteColors(Color normalColor)
        {
            ColorBlock colors = ColorBlock.defaultColorBlock;
            colors.normalColor = normalColor;
            colors.highlightedColor =
                Color.Lerp(normalColor, CharacterSpriteHover, 0.42f);
            colors.selectedColor = colors.highlightedColor;
            colors.pressedColor =
                Color.Lerp(normalColor, CharacterSpritePressed, 0.52f);
            colors.disabledColor = new Color(
                normalColor.r,
                normalColor.g,
                normalColor.b,
                normalColor.a * 0.48f);
            colors.colorMultiplier = 1f;
            colors.fadeDuration = 0.08f;
            return colors;
        }

        public static ColorBlock PanelButtonColors()
        {
            ColorBlock colors = ColorBlock.defaultColorBlock;
            colors.normalColor = PanelButtonNormal;
            colors.highlightedColor = PanelButtonHover;
            colors.selectedColor = PanelButtonHover;
            colors.pressedColor = PanelButtonPressed;
            colors.disabledColor = new Color32(24, 31, 46, 120);
            colors.colorMultiplier = 1f;
            colors.fadeDuration = 0.08f;
            return colors;
        }

        public static ColorBlock CharacterColors()
        {
            return PanelButtonColors();
        }

        public static ColorBlock HotspotColors()
        {
            ColorBlock colors = ColorBlock.defaultColorBlock;
            colors.normalColor = HotspotNormal;
            colors.highlightedColor = HotspotHover;
            colors.selectedColor = HotspotHover;
            colors.pressedColor = HotspotPressed;
            colors.disabledColor = Color.clear;
            colors.colorMultiplier = 1f;
            colors.fadeDuration = 0.08f;
            return colors;
        }

        public static Rect ClampHotspot(Rect hotspot)
        {
            float xMin = Mathf.Clamp01(Mathf.Min(hotspot.xMin, hotspot.xMax));
            float xMax = Mathf.Clamp01(Mathf.Max(hotspot.xMin, hotspot.xMax));
            float yMin = Mathf.Clamp01(Mathf.Min(hotspot.yMin, hotspot.yMax));
            float yMax = Mathf.Clamp01(Mathf.Max(hotspot.yMin, hotspot.yMax));
            return Rect.MinMaxRect(xMin, yMin, xMax, yMax);
        }

        public static Vector2 PopupSize(Vector2 referenceResolution)
        {
            float width = Mathf.Max(320f, referenceResolution.x - 128f);
            float height = Mathf.Max(320f, referenceResolution.y - 96f);
            return new Vector2(
                Mathf.Min(720f, width),
                Mathf.Min(780f, height));
        }
    }
}
