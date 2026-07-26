using NUnit.Framework;
using UnityEngine;
using Wake.Exploration;

namespace Wake.Tests
{
    public sealed class BackgroundCoverLayoutTests
    {
        private const float Tolerance = 0.01f;

        [Test]
        public void SixteenByNineSource_CoversSixteenByTenWithoutDistortion()
        {
            BackgroundCoverResult result = BackgroundCoverLayout.Calculate(
                new Vector2(1920f, 1200f),
                new Vector2(1920f, 1080f),
                new Vector2(0.5f, 0.5f));

            Assert.That(result.Size.x, Is.EqualTo(2133.33f).Within(Tolerance));
            Assert.That(result.Size.y, Is.EqualTo(1200f).Within(Tolerance));
            Assert.That(
                result.Size.x / result.Size.y,
                Is.EqualTo(16f / 9f).Within(Tolerance));
            Assert.That(result.Offset, Is.EqualTo(Vector2.zero));
        }

        [Test]
        public void SixteenByNineSource_FillsReferenceResolutionExactly()
        {
            BackgroundCoverResult result = BackgroundCoverLayout.Calculate(
                new Vector2(1920f, 1080f),
                new Vector2(3840f, 2160f),
                new Vector2(0.5f, 0.5f));

            Assert.That(result.Size.x, Is.EqualTo(1920f).Within(Tolerance));
            Assert.That(result.Size.y, Is.EqualTo(1080f).Within(Tolerance));
            Assert.That(result.Offset, Is.EqualTo(Vector2.zero));
        }

        [Test]
        public void WideViewport_CropsVerticalArea()
        {
            BackgroundCoverResult result = BackgroundCoverLayout.Calculate(
                new Vector2(2560f, 1080f),
                new Vector2(1920f, 1080f),
                new Vector2(0.5f, 0.5f));

            Assert.That(result.Size.x, Is.EqualTo(2560f).Within(Tolerance));
            Assert.That(result.Size.y, Is.EqualTo(1440f).Within(Tolerance));
            Assert.That(result.Offset, Is.EqualTo(Vector2.zero));
        }

        [Test]
        public void FocusMovesCropTowardRequestedSubject()
        {
            BackgroundCoverResult left = BackgroundCoverLayout.Calculate(
                new Vector2(1920f, 1200f),
                new Vector2(1920f, 1080f),
                new Vector2(0f, 0.5f));
            BackgroundCoverResult right = BackgroundCoverLayout.Calculate(
                new Vector2(1920f, 1200f),
                new Vector2(1920f, 1080f),
                new Vector2(1f, 0.5f));

            Assert.That(left.Offset.x, Is.GreaterThan(0f));
            Assert.That(right.Offset.x, Is.LessThan(0f));
            Assert.That(left.Offset.x, Is.EqualTo(-right.Offset.x).Within(Tolerance));
        }

        [Test]
        public void ZoomExpandsCoveredImageAroundFocus()
        {
            BackgroundCoverResult normal = BackgroundCoverLayout.Calculate(
                new Vector2(1920f, 1080f),
                new Vector2(1920f, 1080f),
                new Vector2(0.5f, 0.5f));
            BackgroundCoverResult zoomed = BackgroundCoverLayout.Calculate(
                new Vector2(1920f, 1080f),
                new Vector2(1920f, 1080f),
                new Vector2(0.5f, 0.5f),
                1.25f);

            Assert.That(zoomed.Size, Is.EqualTo(normal.Size * 1.25f));
            Assert.That(zoomed.Offset, Is.EqualTo(Vector2.zero));
        }

        [Test]
        public void InvalidDimensions_AreRejected()
        {
            Assert.That(
                () => BackgroundCoverLayout.Calculate(
                    Vector2.zero,
                    new Vector2(1920f, 1080f),
                    new Vector2(0.5f, 0.5f)),
                Throws.TypeOf<System.ArgumentOutOfRangeException>());
            Assert.That(
                () => BackgroundCoverLayout.Calculate(
                    new Vector2(1920f, 1080f),
                    Vector2.zero,
                    new Vector2(0.5f, 0.5f)),
                Throws.TypeOf<System.ArgumentOutOfRangeException>());
        }

        [Test]
        public void Presenter_CreatesMaskedNonInteractiveCoverImage()
        {
            GameObject canvasObject = new("Canvas", typeof(Canvas));
            GameObject presenterObject = new(
                "Presenter",
                typeof(RectTransform),
                typeof(BackgroundCoverPresenter));
            try
            {
                BackgroundCoverPresenter presenter =
                    presenterObject.GetComponent<BackgroundCoverPresenter>();
                presenter.Initialize(canvasObject.GetComponent<RectTransform>());

                Assert.That(
                    presenterObject.GetComponent<UnityEngine.UI.RectMask2D>(),
                    Is.Not.Null);
                Assert.That(presenterObject.transform.childCount, Is.EqualTo(1));
                UnityEngine.UI.Image image = presenterObject
                    .GetComponentInChildren<UnityEngine.UI.Image>(true);
                Assert.That(image, Is.Not.Null);
                Assert.That(image.raycastTarget, Is.False);
                Assert.That(image.preserveAspect, Is.False);
                Assert.That(presenterObject.activeSelf, Is.False);
            }
            finally
            {
                Object.DestroyImmediate(presenterObject);
                Object.DestroyImmediate(canvasObject);
            }
        }

        [Test]
        public void Presenter_ClampsFocusAndZoomWhenShowingSprite()
        {
            GameObject canvasObject = new("Canvas", typeof(Canvas));
            GameObject presenterObject = new(
                "Presenter",
                typeof(RectTransform),
                typeof(BackgroundCoverPresenter));
            Texture2D texture = new(16, 9);
            Sprite sprite = Sprite.Create(
                texture,
                new Rect(0f, 0f, 16f, 9f),
                new Vector2(0.5f, 0.5f));
            try
            {
                BackgroundCoverPresenter presenter =
                    presenterObject.GetComponent<BackgroundCoverPresenter>();
                presenter.Initialize(canvasObject.GetComponent<RectTransform>());
                presenter.Show(sprite, new Vector2(-2f, 3f), 0.25f);

                Assert.That(presenterObject.activeSelf, Is.True);
                Assert.That(presenter.Sprite, Is.SameAs(sprite));
                Assert.That(presenter.Focus, Is.EqualTo(new Vector2(0f, 1f)));
                Assert.That(presenter.Zoom, Is.EqualTo(1f));
            }
            finally
            {
                Object.DestroyImmediate(sprite);
                Object.DestroyImmediate(texture);
                Object.DestroyImmediate(presenterObject);
                Object.DestroyImmediate(canvasObject);
            }
        }
    }
}
