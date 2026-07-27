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

        private static readonly Color CharacterNormal =
            new Color32(24, 31, 46, 238);
        private static readonly Color CharacterHover =
            new Color32(42, 70, 83, 248);
        private static readonly Color CharacterPressed =
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

        public static ColorBlock CharacterColors()
        {
            ColorBlock colors = ColorBlock.defaultColorBlock;
            colors.normalColor = CharacterNormal;
            colors.highlightedColor = CharacterHover;
            colors.selectedColor = CharacterHover;
            colors.pressedColor = CharacterPressed;
            colors.disabledColor = new Color32(24, 31, 46, 120);
            colors.colorMultiplier = 1f;
            colors.fadeDuration = 0.08f;
            return colors;
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
