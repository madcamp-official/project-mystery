using System;
using TMPro;
using UnityEngine;

namespace Wake.UI
{
    public enum UiColorToken
    {
        Canvas,
        Surface,
        SurfaceRaised,
        SurfaceOverlay,
        Brass,
        Cream,
        TextPrimary,
        TextSecondary,
        Disabled,
        Success,
        Warning,
        Danger,
        Focus
    }

    public enum UiSpacingToken
    {
        ExtraSmall,
        Small,
        Medium,
        Large,
        ExtraLarge,
        Huge
    }

    public enum UiTextStyle
    {
        Caption,
        Body,
        BodyLarge,
        Choice,
        SpeakerName,
        Heading,
        Display,
        Technical,
        Handwritten,
        Alert
    }

    public enum UiButtonStyle
    {
        Primary,
        Secondary,
        Quiet,
        Danger
    }

    public enum UiSurfaceStyle
    {
        Canvas,
        Panel,
        RaisedPanel,
        Overlay,
        Toast
    }

    [Serializable]
    public struct UiTextToken
    {
        [SerializeField] private TypographyRole typographyRole;
        [SerializeField] private UiColorToken color;
        [SerializeField] private float fontSize;
        [SerializeField] private float lineSpacing;
        [SerializeField] private FontStyles fontStyle;

        public TypographyRole TypographyRole => typographyRole;
        public UiColorToken Color => color;
        public float FontSize => fontSize;
        public float LineSpacing => lineSpacing;
        public FontStyles FontStyle => fontStyle;

        public UiTextToken(
            TypographyRole typographyRole,
            UiColorToken color,
            float fontSize,
            float lineSpacing = 0f,
            FontStyles fontStyle = FontStyles.Normal)
        {
            this.typographyRole = typographyRole;
            this.color = color;
            this.fontSize = fontSize;
            this.lineSpacing = lineSpacing;
            this.fontStyle = fontStyle;
        }
    }

    [Serializable]
    public struct UiButtonToken
    {
        [SerializeField] private UiColorToken normal;
        [SerializeField] private UiColorToken highlighted;
        [SerializeField] private UiColorToken pressed;
        [SerializeField] private UiColorToken selected;
        [SerializeField] private UiColorToken disabled;
        [SerializeField] private UiTextStyle labelStyle;
        [SerializeField] private float colorMultiplier;
        [SerializeField] private float fadeDuration;

        public UiColorToken Normal => normal;
        public UiColorToken Highlighted => highlighted;
        public UiColorToken Pressed => pressed;
        public UiColorToken Selected => selected;
        public UiColorToken Disabled => disabled;
        public UiTextStyle LabelStyle => labelStyle;
        public float ColorMultiplier => colorMultiplier;
        public float FadeDuration => fadeDuration;

        public UiButtonToken(
            UiColorToken normal,
            UiColorToken highlighted,
            UiColorToken pressed,
            UiColorToken selected,
            UiColorToken disabled,
            UiTextStyle labelStyle,
            float colorMultiplier = 1f,
            float fadeDuration = 0.08f)
        {
            this.normal = normal;
            this.highlighted = highlighted;
            this.pressed = pressed;
            this.selected = selected;
            this.disabled = disabled;
            this.labelStyle = labelStyle;
            this.colorMultiplier = colorMultiplier;
            this.fadeDuration = fadeDuration;
        }
    }

    [Serializable]
    public struct UiInteractionToken
    {
        [SerializeField] private float hoverScale;
        [SerializeField] private float pressedScale;
        [SerializeField] private float hoverBrightness;
        [SerializeField] private float pressedBrightness;

        public float HoverScale => hoverScale;
        public float PressedScale => pressedScale;
        public float HoverBrightness => hoverBrightness;
        public float PressedBrightness => pressedBrightness;

        public UiInteractionToken(
            float hoverScale,
            float pressedScale,
            float hoverBrightness,
            float pressedBrightness)
        {
            this.hoverScale = hoverScale;
            this.pressedScale = pressedScale;
            this.hoverBrightness = hoverBrightness;
            this.pressedBrightness = pressedBrightness;
        }
    }

    [CreateAssetMenu(
        fileName = "UiVisualTheme",
        menuName = "Wake/UI/Visual Theme")]
    public sealed class UiVisualTheme : ScriptableObject
    {
        [Header("Palette")]
        [SerializeField] private Color canvas =
            new(0.025f, 0.043f, 0.071f, 1f);
        [SerializeField] private Color surface =
            new(0.055f, 0.094f, 0.133f, 0.94f);
        [SerializeField] private Color surfaceRaised =
            new(0.086f, 0.129f, 0.173f, 0.97f);
        [SerializeField] private Color surfaceOverlay =
            new(0.018f, 0.027f, 0.043f, 0.82f);
        [SerializeField] private Color brass =
            new(0.718f, 0.522f, 0.212f, 1f);
        [SerializeField] private Color cream =
            new(0.965f, 0.827f, 0.529f, 1f);
        [SerializeField] private Color textPrimary =
            new(0.965f, 0.941f, 0.867f, 1f);
        [SerializeField] private Color textSecondary =
            new(0.702f, 0.722f, 0.733f, 1f);
        [SerializeField] private Color disabled =
            new(0.286f, 0.314f, 0.357f, 0.78f);
        [SerializeField] private Color success =
            new(0.216f, 0.412f, 0.412f, 1f);
        [SerializeField] private Color warning =
            new(0.718f, 0.522f, 0.212f, 1f);
        [SerializeField] private Color danger =
            new(0.455f, 0.169f, 0.169f, 1f);
        [SerializeField] private Color focus =
            new(0.965f, 0.827f, 0.529f, 1f);

        [Header("Spacing")]
        [SerializeField] private float spacingExtraSmall = 4f;
        [SerializeField] private float spacingSmall = 8f;
        [SerializeField] private float spacingMedium = 16f;
        [SerializeField] private float spacingLarge = 24f;
        [SerializeField] private float spacingExtraLarge = 32f;
        [SerializeField] private float spacingHuge = 48f;

        [Header("Typography")]
        [SerializeField] private UiTextToken caption = new(
            TypographyRole.BodyRegular, UiColorToken.TextSecondary, 18f);
        [SerializeField] private UiTextToken body = new(
            TypographyRole.Body, UiColorToken.TextPrimary, 26f, 6f);
        [SerializeField] private UiTextToken bodyLarge = new(
            TypographyRole.Body, UiColorToken.TextPrimary, 34f, 8f);
        [SerializeField] private UiTextToken choice = new(
            TypographyRole.Choice, UiColorToken.TextPrimary, 28f);
        [SerializeField] private UiTextToken speakerName = new(
            TypographyRole.SpeakerName, UiColorToken.Canvas, 28f);
        [SerializeField] private UiTextToken heading = new(
            TypographyRole.Heading, UiColorToken.Cream, 38f);
        [SerializeField] private UiTextToken display = new(
            TypographyRole.HeadingStrong, UiColorToken.Cream, 52f);
        [SerializeField] private UiTextToken technical = new(
            TypographyRole.Technical, UiColorToken.Cream, 20f);
        [SerializeField] private UiTextToken handwritten = new(
            TypographyRole.Handwritten, UiColorToken.Canvas, 28f, 8f);
        [SerializeField] private UiTextToken alert = new(
            TypographyRole.SpecialAlert, UiColorToken.Cream, 40f);

        [Header("Buttons")]
        [SerializeField] private UiButtonToken primaryButton = new(
            UiColorToken.Brass,
            UiColorToken.Cream,
            UiColorToken.Warning,
            UiColorToken.Cream,
            UiColorToken.Disabled,
            UiTextStyle.Choice);
        [SerializeField] private UiButtonToken secondaryButton = new(
            UiColorToken.SurfaceRaised,
            UiColorToken.Brass,
            UiColorToken.Surface,
            UiColorToken.Brass,
            UiColorToken.Disabled,
            UiTextStyle.Choice);
        [SerializeField] private UiButtonToken quietButton = new(
            UiColorToken.SurfaceOverlay,
            UiColorToken.SurfaceRaised,
            UiColorToken.Surface,
            UiColorToken.SurfaceRaised,
            UiColorToken.Disabled,
            UiTextStyle.Body);
        [SerializeField] private UiButtonToken dangerButton = new(
            UiColorToken.Danger,
            UiColorToken.Warning,
            UiColorToken.Danger,
            UiColorToken.Warning,
            UiColorToken.Disabled,
            UiTextStyle.Choice);

        [Header("Interaction")]
        [SerializeField] private UiInteractionToken interaction = new(
            1.035f, 0.98f, 1.10f, 0.94f);

        public Color Resolve(UiColorToken token)
        {
            return token switch
            {
                UiColorToken.Canvas => canvas,
                UiColorToken.Surface => surface,
                UiColorToken.SurfaceRaised => surfaceRaised,
                UiColorToken.SurfaceOverlay => surfaceOverlay,
                UiColorToken.Brass => brass,
                UiColorToken.Cream => cream,
                UiColorToken.TextPrimary => textPrimary,
                UiColorToken.TextSecondary => textSecondary,
                UiColorToken.Disabled => disabled,
                UiColorToken.Success => success,
                UiColorToken.Warning => warning,
                UiColorToken.Danger => danger,
                UiColorToken.Focus => focus,
                _ => Color.magenta
            };
        }

        public float Resolve(UiSpacingToken token)
        {
            return token switch
            {
                UiSpacingToken.ExtraSmall => spacingExtraSmall,
                UiSpacingToken.Small => spacingSmall,
                UiSpacingToken.Medium => spacingMedium,
                UiSpacingToken.Large => spacingLarge,
                UiSpacingToken.ExtraLarge => spacingExtraLarge,
                UiSpacingToken.Huge => spacingHuge,
                _ => 0f
            };
        }

        public UiTextToken Resolve(UiTextStyle style)
        {
            return style switch
            {
                UiTextStyle.Caption => caption,
                UiTextStyle.Body => body,
                UiTextStyle.BodyLarge => bodyLarge,
                UiTextStyle.Choice => choice,
                UiTextStyle.SpeakerName => speakerName,
                UiTextStyle.Heading => heading,
                UiTextStyle.Display => display,
                UiTextStyle.Technical => technical,
                UiTextStyle.Handwritten => handwritten,
                UiTextStyle.Alert => alert,
                _ => body
            };
        }

        public UiButtonToken Resolve(UiButtonStyle style)
        {
            return style switch
            {
                UiButtonStyle.Primary => primaryButton,
                UiButtonStyle.Secondary => secondaryButton,
                UiButtonStyle.Quiet => quietButton,
                UiButtonStyle.Danger => dangerButton,
                _ => primaryButton
            };
        }

        public UiInteractionToken Interaction => interaction;
    }
}
