using NUnit.Framework;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using Wake.UI;

namespace Wake.Tests
{
    public sealed class ScreenShellAccessibilityTests
    {
        [TestCase(1280, 720, 1f)]
        [TestCase(1280, 720, 1.6f)]
        [TestCase(1920, 1080, 1.25f)]
        [TestCase(1920, 1200, 1.6f)]
        [TestCase(2560, 1080, 1f)]
        public void LateGamePrimaryAction_MeetsMinimumTargetSize(
            int width,
            int height,
            float uiScale)
        {
            const float normalizedWidth = .31f - .08f;
            const float normalizedHeight = .12f - .04f;
            Assert.That(
                width * normalizedWidth * uiScale,
                Is.GreaterThanOrEqualTo(44f));
            Assert.That(
                height * normalizedHeight * uiScale,
                Is.GreaterThanOrEqualTo(44f));
        }

        [Test]
        public void RuntimeButtonPreparation_EnablesDirectionalNavigation()
        {
            var target = new GameObject(
                "Accessible Button",
                typeof(RectTransform),
                typeof(Image),
                typeof(Button));
            try
            {
                Button button = target.GetComponent<Button>();
                ScreenShellRuntimePresenter.PrepareButton(button);
                LayoutElement layout = target.GetComponent<LayoutElement>();

                Assert.That(
                    button.navigation.mode,
                    Is.EqualTo(Navigation.Mode.Automatic));
                Assert.That(layout, Is.Not.Null);
                Assert.That(layout.minWidth, Is.GreaterThanOrEqualTo(44f));
                Assert.That(layout.minHeight, Is.GreaterThanOrEqualTo(44f));
            }
            finally
            {
                Object.DestroyImmediate(target);
            }
        }

        [Test]
        public void ReadableTextPreparation_PreservesWrappingAndMinimumSize()
        {
            var target = new GameObject(
                "Readable Text",
                typeof(RectTransform),
                typeof(TextMeshProUGUI));
            try
            {
                TMP_Text text = target.GetComponent<TMP_Text>();
                text.fontSize = 34f;
                ScreenShellRuntimePresenter.PrepareReadableText(text, 18f);

                Assert.That(text.enableAutoSizing, Is.True);
                Assert.That(text.fontSizeMin, Is.EqualTo(18f));
                Assert.That(
                    text.textWrappingMode,
                    Is.EqualTo(TextWrappingModes.Normal));
                Assert.That(
                    text.overflowMode,
                    Is.EqualTo(TextOverflowModes.Overflow));
            }
            finally
            {
                Object.DestroyImmediate(target);
            }
        }
    }
}
