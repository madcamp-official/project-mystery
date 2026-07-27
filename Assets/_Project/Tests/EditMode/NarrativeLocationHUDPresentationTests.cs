using NUnit.Framework;
using UnityEngine;
using Wake.Exploration;
using Wake.UI;

namespace Wake.Tests
{
    public sealed class NarrativeLocationHUDPresentationTests
    {
        [Test]
        public void PhysicalLocation_ShowsSceneAndExplorationContext()
        {
            var context = new NarrativeLocationContext(
                "D1-04",
                "SERVICE7",
                "CREW_STAIRS",
                "승무원 계단",
                NarrativeLocationKind.Physical);

            NarrativeLocationHUDViewModel view =
                NarrativeLocationHUDPresentation.Create(context);

            Assert.That(view.IsVisible, Is.True);
            Assert.That(view.IsWarning, Is.False);
            Assert.That(view.Eyebrow, Is.EqualTo("D1-04 · 현재 위치"));
            Assert.That(view.Title, Is.EqualTo("승무원 계단"));
            Assert.That(view.SupportingText,
                Is.EqualTo("탐색 배경 · CREW_STAIRS"));
            Assert.That(view.DisplayText, Does.Contain("D1-04"));
            Assert.That(view.DisplayText, Does.Contain("승무원 계단"));
        }

        [Test]
        public void DialogueOnlyLocation_UsesTextWarningBeyondColor()
        {
            var context = new NarrativeLocationContext(
                "D8-02",
                "UNRESOLVED_ROOM",
                string.Empty,
                "UNRESOLVED ROOM",
                NarrativeLocationKind.DialogueOnly);

            NarrativeLocationHUDViewModel view =
                NarrativeLocationHUDPresentation.Create(context);

            Assert.That(view.IsVisible, Is.True);
            Assert.That(view.IsWarning, Is.True);
            Assert.That(view.Eyebrow, Does.Contain("위치 확인 필요"));
            Assert.That(view.Title, Does.StartWith("⚠"));
            Assert.That(view.SupportingText,
                Is.EqualTo("배경 미확정 · 현재 배경 유지"));
            Assert.That(view.DisplayText, Does.Contain("배경 미확정"));
        }

        [Test]
        public void UndocumentedScene_HidesLocationContext()
        {
            NarrativeLocationHUDViewModel view =
                NarrativeLocationHUDPresentation.Create(default);

            Assert.That(view.IsVisible, Is.False);
            Assert.That(view.IsWarning, Is.False);
            Assert.That(view.DisplayText, Is.Empty);
            Assert.That(view.BackgroundColor, Is.EqualTo(Color.clear));
        }

        [TestCase(1920f, 1920f, 560f)]
        [TestCase(1280f, 1100f, 560f)]
        [TestCase(720f, 720f, 560f)]
        [TestCase(390f, 390f, 358f)]
        [TestCase(320f, 280f, 280f)]
        public void Layout_ClampsWidthInsideSafeArea(
            float viewportWidth,
            float safeAreaWidth,
            float expectedWidth)
        {
            NarrativeLocationHUDLayout layout =
                NarrativeLocationHUDPresentation.CalculateLayout(
                    viewportWidth,
                    safeAreaWidth);

            Assert.That(layout.Width, Is.EqualTo(expectedWidth).Within(0.01f));
            Assert.That(layout.Height,
                Is.EqualTo(NarrativeLocationHUDPresentation.PreferredHeight));
            Assert.That(layout.TopOffset,
                Is.EqualTo(NarrativeLocationHUDPresentation.TopOffset));
        }

        [Test]
        public void Layout_LeavesSymmetricHorizontalMarginOnNarrowScreen()
        {
            const float safeWidth = 412f;

            NarrativeLocationHUDLayout layout =
                NarrativeLocationHUDPresentation.CalculateLayout(
                    safeWidth,
                    safeWidth);

            float remaining = safeWidth - layout.Width;
            Assert.That(
                remaining,
                Is.EqualTo(
                    NarrativeLocationHUDPresentation.HorizontalMargin * 2f)
                    .Within(0.01f));
        }

        [Test]
        public void Layout_UsesSafeAreaWhenItIsNarrowerThanViewport()
        {
            NarrativeLocationHUDLayout layout =
                NarrativeLocationHUDPresentation.CalculateLayout(
                    1400f,
                    520f);

            Assert.That(layout.Width, Is.EqualTo(488f).Within(0.01f));
            Assert.That(layout.Width, Is.LessThan(520f));
        }
    }
}
