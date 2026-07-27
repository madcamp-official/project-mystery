using System;
using NUnit.Framework;
using UnityEngine;
using Wake.UI;

namespace Wake.Tests
{
    public sealed class DialogueTypographyMetricsTests
    {
        private const float Tolerance = 0.15f;

        [Test]
        public void ReferenceResolution_HasUnitScale()
        {
            float scale = DialogueTypographyMetrics.CalculateCanvasScale(
                new Vector2(2880f, 1800f));

            Assert.That(scale, Is.EqualTo(1f).Within(Tolerance));
        }

        [Test]
        public void FullHd_LineMaximumMatchesFortyPixelTarget()
        {
            Vector2 range = DialogueTypographyMetrics.GetLineScreenRange(
                new Vector2(1920f, 1080f));

            Assert.That(range.x, Is.EqualTo(32.9f).Within(Tolerance));
            Assert.That(range.y, Is.EqualTo(40.5f).Within(Tolerance));
        }

        [Test]
        public void FullHd_ChoiceMaximumMatchesThirtySixPixelTarget()
        {
            Vector2 range = DialogueTypographyMetrics.GetChoiceScreenRange(
                new Vector2(1920f, 1080f));

            Assert.That(range.x, Is.EqualTo(30.4f).Within(Tolerance));
            Assert.That(range.y, Is.EqualTo(36.7f).Within(Tolerance));
        }

        [Test]
        public void FullHd_SpeakerRangeMatchesNameTarget()
        {
            Vector2 range = DialogueTypographyMetrics.GetSpeakerScreenRange(
                new Vector2(1920f, 1080f));

            Assert.That(range.x, Is.EqualTo(27.8f).Within(Tolerance));
            Assert.That(range.y, Is.EqualTo(32.9f).Within(Tolerance));
        }

        [TestCase(1280f, 720f, 0.4216f)]
        [TestCase(1920f, 1080f, 0.6325f)]
        [TestCase(2560f, 1440f, 0.8433f)]
        [TestCase(2880f, 1800f, 1f)]
        public void CommonResolutions_UseExpectedCanvasScale(
            float width,
            float height,
            float expected)
        {
            float scale = DialogueTypographyMetrics.CalculateCanvasScale(
                new Vector2(width, height));

            Assert.That(scale, Is.EqualTo(expected).Within(0.001f));
        }

        [Test]
        public void SixteenByTen_UsesGeometricMatchScale()
        {
            float scale = DialogueTypographyMetrics.CalculateCanvasScale(
                new Vector2(1920f, 1200f));

            Assert.That(scale, Is.EqualTo(0.6667f).Within(0.001f));
        }

        [Test]
        public void Ultrawide_DoesNotScaleOnlyFromWidth()
        {
            Vector2 screen = new(3440f, 1440f);

            float scale =
                DialogueTypographyMetrics.CalculateCanvasScale(screen);
            float widthOnly =
                screen.x /
                ResponsiveDialogueLayout.ReferenceResolution.x;

            Assert.That(scale, Is.LessThan(widthOnly));
            Assert.That(scale, Is.EqualTo(0.9775f).Within(0.002f));
        }

        [Test]
        public void HigherResolutionProducesLargerScreenPixels()
        {
            float at720 = DialogueTypographyMetrics.ToScreenPixels(
                DialogueTypographyMetrics.LineMaximum,
                new Vector2(1280f, 720f));
            float at1080 = DialogueTypographyMetrics.ToScreenPixels(
                DialogueTypographyMetrics.LineMaximum,
                new Vector2(1920f, 1080f));
            float at1440 = DialogueTypographyMetrics.ToScreenPixels(
                DialogueTypographyMetrics.LineMaximum,
                new Vector2(2560f, 1440f));

            Assert.That(at720, Is.LessThan(at1080));
            Assert.That(at1080, Is.LessThan(at1440));
        }

        [Test]
        public void RoleMaximumsPreserveVisualHierarchy()
        {
            Assert.That(
                DialogueTypographyMetrics.LineMaximum,
                Is.GreaterThan(
                    DialogueTypographyMetrics.ChoiceMaximum));
            Assert.That(
                DialogueTypographyMetrics.ChoiceMaximum,
                Is.GreaterThan(
                    DialogueTypographyMetrics.SpeakerMaximum));
        }

        [Test]
        public void RoleMinimumsRemainPositive()
        {
            Assert.That(DialogueTypographyMetrics.LineMinimum, Is.Positive);
            Assert.That(DialogueTypographyMetrics.ChoiceMinimum, Is.Positive);
            Assert.That(DialogueTypographyMetrics.SpeakerMinimum, Is.Positive);
        }

        [Test]
        public void AutoSizeRangesRemainOrdered()
        {
            Assert.That(
                DialogueTypographyMetrics.LineMinimum,
                Is.LessThan(DialogueTypographyMetrics.LineMaximum));
            Assert.That(
                DialogueTypographyMetrics.ChoiceMinimum,
                Is.LessThan(DialogueTypographyMetrics.ChoiceMaximum));
            Assert.That(
                DialogueTypographyMetrics.SpeakerMinimum,
                Is.LessThan(DialogueTypographyMetrics.SpeakerMaximum));
        }

        [Test]
        public void BodyLeadingIsLargest()
        {
            Assert.That(
                DialogueTypographyMetrics.BodyLineSpacing,
                Is.GreaterThan(
                    DialogueTypographyMetrics.ChoiceLineSpacing));
            Assert.That(
                DialogueTypographyMetrics.ChoiceLineSpacing,
                Is.GreaterThan(
                    DialogueTypographyMetrics.HeadingLineSpacing));
        }

        [TestCase(0f, 1080f)]
        [TestCase(1920f, 0f)]
        [TestCase(-1f, 1080f)]
        public void InvalidScreenSize_IsRejected(
            float width,
            float height)
        {
            Assert.That(
                () => DialogueTypographyMetrics.CalculateCanvasScale(
                    new Vector2(width, height)),
                Throws.TypeOf<ArgumentOutOfRangeException>());
        }

        [Test]
        public void NegativeReferenceFontSize_IsRejected()
        {
            Assert.That(
                () => DialogueTypographyMetrics.ToScreenPixels(
                    -1f,
                    new Vector2(1920f, 1080f)),
                Throws.TypeOf<ArgumentOutOfRangeException>());
        }
    }
}
