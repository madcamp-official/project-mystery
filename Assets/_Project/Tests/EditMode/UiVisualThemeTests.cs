using NUnit.Framework;
using TMPro;
using UnityEditor;
using UnityEngine;
using UnityEngine.UI;
using Wake.UI;

namespace Wake.Tests
{
    public sealed class UiVisualThemeTests
    {
        private UiVisualTheme theme;

        [SetUp]
        public void SetUp()
        {
            theme = ScriptableObject.CreateInstance<UiVisualTheme>();
            UiVisualThemeService.SetThemeForTests(theme);
        }

        [TearDown]
        public void TearDown()
        {
            UiVisualThemeService.SetThemeForTests(null);
            Object.DestroyImmediate(theme);
        }

        [Test]
        public void DefaultThemeAsset_IsAvailableFromResources()
        {
            UiVisualTheme asset = AssetDatabase.LoadAssetAtPath<UiVisualTheme>(
                "Assets/_Project/Resources/UI/UiVisualTheme.asset");

            Assert.That(asset, Is.Not.Null);
        }

        [Test]
        public void SpacingScale_IsStrictlyIncreasing()
        {
            float previous = 0f;
            foreach (UiSpacingToken token in System.Enum.GetValues(
                         typeof(UiSpacingToken)))
            {
                float current = theme.Resolve(token);
                Assert.That(current, Is.GreaterThan(previous));
                previous = current;
            }
        }

        [Test]
        public void SemanticStatusColors_AreDistinct()
        {
            Assert.That(
                theme.Resolve(UiColorToken.Success),
                Is.Not.EqualTo(theme.Resolve(UiColorToken.Danger)));
            Assert.That(
                theme.Resolve(UiColorToken.Warning),
                Is.Not.EqualTo(theme.Resolve(UiColorToken.Disabled)));
        }

        [Test]
        public void ApplyText_UsesSemanticSizeColorAndLineSpacing()
        {
            GameObject target = new(
                "Theme Text",
                typeof(RectTransform),
                typeof(CanvasRenderer),
                typeof(TextMeshProUGUI));
            try
            {
                TMP_Text label = target.GetComponent<TMP_Text>();

                bool applied = UiVisualThemeService.ApplyText(
                    label,
                    UiTextStyle.BodyLarge);
                UiTextToken token = theme.Resolve(UiTextStyle.BodyLarge);

                Assert.That(applied, Is.True);
                Assert.That(label.fontSize, Is.EqualTo(token.FontSize));
                Assert.That(label.lineSpacing, Is.EqualTo(token.LineSpacing));
                Assert.That(
                    label.color,
                    Is.EqualTo(theme.Resolve(token.Color)));
            }
            finally
            {
                Object.DestroyImmediate(target);
            }
        }

        [Test]
        public void ApplyButton_ConfiguresEveryInteractionState()
        {
            GameObject target = new(
                "Theme Button",
                typeof(RectTransform),
                typeof(CanvasRenderer),
                typeof(Image),
                typeof(Button));
            try
            {
                Button button = target.GetComponent<Button>();

                bool applied = UiVisualThemeService.ApplyButton(
                    button,
                    UiButtonStyle.Primary);
                UiButtonToken token =
                    theme.Resolve(UiButtonStyle.Primary);

                Assert.That(applied, Is.True);
                Assert.That(
                    button.colors.normalColor,
                    Is.EqualTo(theme.Resolve(token.Normal)));
                Assert.That(
                    button.colors.highlightedColor,
                    Is.EqualTo(theme.Resolve(token.Highlighted)));
                Assert.That(
                    button.colors.pressedColor,
                    Is.EqualTo(theme.Resolve(token.Pressed)));
                Assert.That(
                    button.colors.disabledColor,
                    Is.EqualTo(theme.Resolve(token.Disabled)));
            }
            finally
            {
                Object.DestroyImmediate(target);
            }
        }

        [Test]
        public void ApplyMethods_RejectMissingTargets()
        {
            Assert.That(
                UiVisualThemeService.ApplyText(null, UiTextStyle.Body),
                Is.False);
            Assert.That(
                UiVisualThemeService.ApplyButton(
                    null,
                    UiButtonStyle.Primary),
                Is.False);
            Assert.That(
                UiVisualThemeService.ApplySurface(
                    null,
                    UiSurfaceStyle.Panel),
                Is.False);
        }
    }
}
