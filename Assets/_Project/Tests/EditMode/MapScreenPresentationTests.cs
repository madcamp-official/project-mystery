using NUnit.Framework;
using UnityEngine;
using UnityEngine.UI;
using Wake.Exploration;
using Wake.UI;

namespace Wake.Tests
{
    public sealed class MapScreenPresentationTests
    {
        private GameObject mapRoot;

        [TearDown]
        public void TearDown()
        {
            if (mapRoot != null)
            {
                UnityEngine.Object.DestroyImmediate(mapRoot);
            }
        }

        [TestCase(10, "DECK 10 · 귀빈 및 지휘 갑판")]
        [TestCase(9, "DECK 9 · 대형 공용 갑판")]
        [TestCase(8, "DECK 8 · 아트리움 및 산책 갑판")]
        [TestCase(7, "DECK 7 · 서비스 및 기관 구역")]
        [TestCase(0, "항구 · 승선 구역")]
        [TestCase(6, "DECK 6 · 층별 설계도")]
        public void DeckDisplayTitle_ReturnsUnifiedLocalizedTitle(
            int deck,
            string expected)
        {
            Assert.That(
                MapDeckCatalog.DeckDisplayTitle(deck),
                Is.EqualTo(expected));
        }

        [TestCase("RICHARD_SUITE", "리처드 스위트룸")]
        [TestCase("ATRIUM", "아트리움")]
        public void InteractiveMapLabels_UseOneCanonicalPlaceName(
            string locationCode,
            string expected)
        {
            CanonicalLocationSpec spec =
                CanonicalLocationCatalog.FindSpec(locationCode);

            Assert.That(spec, Is.Not.Null);
            Assert.That(spec.DisplayName, Is.EqualTo(expected));
            Assert.That(spec.DisplayName, Does.Not.Contain("DECK"));
            Assert.That(spec.DisplayName, Does.Not.Contain("개방"));
        }

        [Test]
        public void EnsureBackdrop_LoadsAndStretchesOneFirstSibling()
        {
            mapRoot = new GameObject("Map Root", typeof(RectTransform));
            var existingChild = new GameObject(
                "Existing Child",
                typeof(RectTransform));
            existingChild.transform.SetParent(mapRoot.transform, false);

            Sprite expected = Resources.Load<Sprite>(
                MapScreenBackdropPresenter.ResourcePath);
            Assert.That(
                expected,
                Is.Not.Null,
                MapScreenBackdropPresenter.ResourcePath);

            Image first = MapScreenBackdropPresenter.Ensure(
                mapRoot.transform);

            Assert.That(first, Is.Not.Null);
            Assert.That(first.sprite, Is.SameAs(expected));
            Assert.That(first.transform.parent, Is.SameAs(mapRoot.transform));
            Assert.That(first.transform.GetSiblingIndex(), Is.Zero);
            Assert.That(first.raycastTarget, Is.False);
            Assert.That(first.preserveAspect, Is.False);
            Assert.That(first.rectTransform.anchorMin, Is.EqualTo(Vector2.zero));
            Assert.That(first.rectTransform.anchorMax, Is.EqualTo(Vector2.one));
            Assert.That(first.rectTransform.offsetMin, Is.EqualTo(Vector2.zero));
            Assert.That(first.rectTransform.offsetMax, Is.EqualTo(Vector2.zero));
            Assert.That(first.rectTransform.localScale, Is.EqualTo(Vector3.one));
            Assert.That(mapRoot.transform.childCount, Is.EqualTo(2));

            Image second = MapScreenBackdropPresenter.Ensure(
                mapRoot.transform);

            Assert.That(second, Is.SameAs(first));
            Assert.That(mapRoot.transform.childCount, Is.EqualTo(2));
            Assert.That(second.transform.GetSiblingIndex(), Is.Zero);
        }
    }
}
