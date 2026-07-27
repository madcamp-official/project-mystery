using NUnit.Framework;
using UnityEngine;
using Wake.UI;

namespace Wake.Tests
{
    public sealed class DialogueChoiceLayoutPolicyTests
    {
        [Test]
        public void NoActiveChoices_UsesNoLayoutHeight()
        {
            DialogueChoiceLayoutSpec spec =
                DialogueChoiceLayoutPolicy.Calculate(2420f, 0);

            Assert.That(spec.Columns, Is.EqualTo(1));
            Assert.That(spec.Rows, Is.Zero);
            Assert.That(spec.CellSize, Is.EqualTo(Vector2.zero));
            Assert.That(spec.RequiredHeight, Is.Zero);
        }

        [Test]
        public void OpeningChoices_UseTwoWideColumns()
        {
            DialogueChoiceLayoutSpec spec =
                DialogueChoiceLayoutPolicy.Calculate(2420f, 2);

            Assert.That(spec.Columns, Is.EqualTo(2));
            Assert.That(spec.Rows, Is.EqualTo(1));
            Assert.That(
                spec.CellSize.x,
                Is.EqualTo(1192f).Within(0.01f));
            Assert.That(
                spec.CellSize.y,
                Is.EqualTo(
                    DialogueChoiceLayoutPolicy.MinimumCellHeight));
        }

        [Test]
        public void NarrowContainer_FallsBackToOneColumn()
        {
            DialogueChoiceLayoutSpec spec =
                DialogueChoiceLayoutPolicy.Calculate(1000f, 2);

            Assert.That(spec.Columns, Is.EqualTo(1));
            Assert.That(spec.Rows, Is.EqualTo(2));
            Assert.That(spec.CellSize.x, Is.EqualTo(980f));
        }

        [TestCase(2, 1)]
        [TestCase(3, 2)]
        [TestCase(4, 2)]
        [TestCase(5, 3)]
        [TestCase(8, 4)]
        public void WideContainer_ComputesRequiredRows(
            int activeChoiceCount,
            int expectedRows)
        {
            DialogueChoiceLayoutSpec spec =
                DialogueChoiceLayoutPolicy.Calculate(
                    2420f,
                    activeChoiceCount);

            Assert.That(spec.Columns, Is.EqualTo(2));
            Assert.That(spec.Rows, Is.EqualTo(expectedRows));
            Assert.That(
                spec.RequiredHeight,
                Is.EqualTo(
                    DialogueChoiceLayoutPolicy.Padding * 2f +
                    expectedRows * spec.CellSize.y +
                    Mathf.Max(0, expectedRows - 1) *
                    DialogueChoiceLayoutPolicy.Spacing));
        }

        [Test]
        public void PreferredLabelHeight_ExpandsCellWithinLimits()
        {
            DialogueChoiceLayoutSpec expanded =
                DialogueChoiceLayoutPolicy.Calculate(
                    2420f,
                    2,
                    104f);
            DialogueChoiceLayoutSpec clamped =
                DialogueChoiceLayoutPolicy.Calculate(
                    2420f,
                    2,
                    1000f);

            Assert.That(
                expanded.CellSize.y,
                Is.EqualTo(
                    104f +
                    DialogueChoiceLayoutPolicy.LabelVerticalPadding));
            Assert.That(
                clamped.CellSize.y,
                Is.EqualTo(
                    DialogueChoiceLayoutPolicy.MaximumCellHeight));
        }

        [Test]
        public void MinimumCellHeight_RemainsClickableAtMinimumResolution()
        {
            float minimumCanvasScale =
                DialogueTypographyMetrics.CalculateCanvasScale(
                    new Vector2(1280f, 720f));

            float physicalHeight =
                DialogueChoiceLayoutPolicy.MinimumCellHeight *
                minimumCanvasScale;

            Assert.That(physicalHeight, Is.GreaterThanOrEqualTo(44f));
        }

        [Test]
        public void LabelWidth_ReservesHorizontalPadding()
        {
            DialogueChoiceLayoutSpec spec =
                DialogueChoiceLayoutPolicy.Calculate(2420f, 2);

            Assert.That(
                DialogueChoiceLayoutPolicy.GetLabelWidth(spec),
                Is.EqualTo(
                    spec.CellSize.x -
                    DialogueChoiceLayoutPolicy.LabelHorizontalPadding));
        }
    }
}
