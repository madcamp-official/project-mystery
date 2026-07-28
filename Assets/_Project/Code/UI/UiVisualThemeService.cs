using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace Wake.UI
{
    public static class UiVisualThemeService
    {
        public const string ResourcesPath = "UI/UiVisualTheme";

        private static UiVisualTheme cachedTheme;
        private static UiVisualTheme themeOverride;
        private static UiVisualTheme fallbackTheme;

        public static UiVisualTheme Theme
        {
            get
            {
                if (themeOverride != null)
                {
                    return themeOverride;
                }

                if (cachedTheme == null)
                {
                    cachedTheme =
                        Resources.Load<UiVisualTheme>(ResourcesPath);
                }

                if (cachedTheme != null)
                {
                    return cachedTheme;
                }

                if (fallbackTheme == null)
                {
                    fallbackTheme =
                        ScriptableObject.CreateInstance<UiVisualTheme>();
                    fallbackTheme.hideFlags = HideFlags.HideAndDontSave;
                }

                return fallbackTheme;
            }
        }

        public static Color Resolve(UiColorToken token)
        {
            return Theme.Resolve(token);
        }

        public static float Resolve(UiSpacingToken token)
        {
            return Theme.Resolve(token);
        }

        public static UiInteractionToken Interaction => Theme.Interaction;

        public static bool ApplyText(TMP_Text text, UiTextStyle style)
        {
            if (text == null)
            {
                return false;
            }

            UiTextToken token = Theme.Resolve(style);
            TypographyService.Apply(text, token.TypographyRole);
            text.color = Resolve(token.Color);
            text.fontSize = token.FontSize;
            text.lineSpacing = token.LineSpacing;
            text.fontStyle = token.FontStyle;
            return true;
        }

        public static bool ApplyButton(Button button, UiButtonStyle style)
        {
            if (button == null)
            {
                return false;
            }

            UiButtonToken token = Theme.Resolve(style);
            ColorBlock colors = button.colors;
            colors.normalColor = Resolve(token.Normal);
            colors.highlightedColor = Resolve(token.Highlighted);
            colors.pressedColor = Resolve(token.Pressed);
            colors.selectedColor = Resolve(token.Selected);
            colors.disabledColor = Resolve(token.Disabled);
            colors.colorMultiplier = token.ColorMultiplier;
            colors.fadeDuration = token.FadeDuration;
            button.colors = colors;

            TMP_Text label = button.GetComponentInChildren<TMP_Text>(true);
            ApplyText(label, token.LabelStyle);
            return true;
        }

        public static bool ApplySurface(Image image, UiSurfaceStyle style)
        {
            if (image == null)
            {
                return false;
            }

            image.color = Resolve(style switch
            {
                UiSurfaceStyle.Canvas => UiColorToken.Canvas,
                UiSurfaceStyle.Panel => UiColorToken.Surface,
                UiSurfaceStyle.RaisedPanel => UiColorToken.SurfaceRaised,
                UiSurfaceStyle.Overlay => UiColorToken.SurfaceOverlay,
                UiSurfaceStyle.Toast => UiColorToken.SurfaceOverlay,
                _ => UiColorToken.Surface
            });
            return true;
        }

        public static void ClearCache()
        {
            cachedTheme = null;
        }

#if UNITY_EDITOR
        public static void SetThemeForTests(UiVisualTheme theme)
        {
            themeOverride = theme;
            cachedTheme = null;
        }
#endif
    }
}
