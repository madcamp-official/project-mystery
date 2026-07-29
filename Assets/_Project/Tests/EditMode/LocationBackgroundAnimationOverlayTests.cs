using System.Linq;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.UI;
using Wake.Exploration;

namespace Wake.Tests
{
    public sealed class LocationBackgroundAnimationOverlayTests
    {
        [Test]
        public void Evaluator_IsDeterministicAndKeepsElementsOnBackground()
        {
            foreach (LocationBackgroundAnimationProfile profile in
                     LocationBackgroundAnimationCatalog.All)
            {
                foreach (LocationBackgroundEffectSpec effect in
                         profile.Effects)
                {
                    if (IsMotion(effect.Type))
                        continue;

                    int samples = Mathf.Min(
                        effect.MaxElementCount,
                        4);
                    for (int index = 0; index < samples; index++)
                    {
                        LocationBackgroundElementState first =
                            LocationBackgroundAnimationEvaluator
                                .EvaluateElement(
                                    effect,
                                    index,
                                    12.75f);
                        LocationBackgroundElementState second =
                            LocationBackgroundAnimationEvaluator
                                .EvaluateElement(
                                    effect,
                                    index,
                                    12.75f);

                        Assert.That(
                            second.NormalizedPosition,
                            Is.EqualTo(first.NormalizedPosition),
                            $"{profile.Id}/{effect.Type}/{index}");
                        Assert.That(
                            second.AlphaMultiplier,
                            Is.EqualTo(first.AlphaMultiplier),
                            $"{profile.Id}/{effect.Type}/{index}");
                        Assert.That(
                            first.NormalizedPosition.x,
                            Is.InRange(0f, 1f),
                            $"{profile.Id}/{effect.Type}/{index}");
                        Assert.That(
                            first.NormalizedPosition.y,
                            Is.InRange(0f, 1f),
                            $"{profile.Id}/{effect.Type}/{index}");
                        Assert.That(
                            first.AlphaMultiplier,
                            Is.InRange(0f, 1f),
                            $"{profile.Id}/{effect.Type}/{index}");
                        Assert.That(
                            first.ScaleMultiplier,
                            Is.GreaterThan(0f),
                            $"{profile.Id}/{effect.Type}/{index}");
                    }
                }
            }
        }

        [Test]
        public void MotionEvaluator_ProducesFiniteOverscannedMotion()
        {
            LocationBackgroundEffectSpec[] motions =
                LocationBackgroundAnimationCatalog.All
                    .SelectMany(profile => profile.Effects)
                    .Where(effect => IsMotion(effect.Type))
                    .ToArray();

            Assert.That(motions, Is.Not.Empty);
            foreach (LocationBackgroundEffectSpec effect in motions)
            {
                for (int sample = 0; sample < 48; sample++)
                {
                    float time = sample * .371f;
                    LocationBackgroundMotionState state =
                        LocationBackgroundAnimationEvaluator.EvaluateMotion(
                            effect,
                            time);

                    Assert.That(
                        float.IsNaN(state.NormalizedOffset.x),
                        Is.False);
                    Assert.That(
                        float.IsNaN(state.NormalizedOffset.y),
                        Is.False);
                    Assert.That(
                        float.IsNaN(state.RotationDegrees),
                        Is.False);
                    Assert.That(
                        state.ScaleMultiplier,
                        Is.GreaterThanOrEqualTo(1f));

                    float overscanMargin =
                        (state.ScaleMultiplier - 1f) * .5f;
                    float requiredMargin = Mathf.Max(
                        Mathf.Abs(state.NormalizedOffset.x),
                        Mathf.Abs(state.NormalizedOffset.y));
                    Assert.That(
                        overscanMargin + .00001f,
                        Is.GreaterThanOrEqualTo(requiredMargin),
                        $"{effect.Type} at {time:F3}s");
                }
            }
        }

        [Test]
        public void Show_CreatesNonInteractiveEffectsBelowExistingOverlays()
        {
            GameObject contentObject = CreateContent();
            GameObject existingOverlay = new(
                "Existing Overlay",
                typeof(RectTransform),
                typeof(CanvasRenderer),
                typeof(Image));
            existingOverlay.transform.SetParent(
                contentObject.transform,
                false);
            GameObject overlayObject = new(
                "Animation Owner",
                typeof(LocationBackgroundAnimationOverlay));
            try
            {
                LocationBackgroundAnimationOverlay overlay =
                    overlayObject.GetComponent<
                        LocationBackgroundAnimationOverlay>();
                overlay.Initialize(
                    contentObject.GetComponent<RectTransform>());
                overlay.Show("PORT");

                Assert.That(
                    overlay.ActiveProfileId,
                    Is.EqualTo("PORT"));
                Transform root =
                    contentObject.transform.GetChild(0);
                Assert.That(
                    root.name,
                    Is.EqualTo("Location Background Animation"));

                Image[] images =
                    root.GetComponentsInChildren<Image>(true);
                Assert.That(images, Is.Not.Empty);
                Assert.That(
                    images.All(image => !image.raycastTarget),
                    Is.True);
                Assert.That(
                    images.Any(image =>
                        image.name.StartsWith("DriftingMotes_")),
                    Is.False,
                    "Bloom-backed room particles own the single mote pool.");

                LocationBackgroundAnimationCatalog.TryGet(
                    "PORT",
                    out LocationBackgroundAnimationProfile profile);
                int expected = profile.Effects
                    .Where(effect =>
                        !IsMotion(effect.Type) &&
                        effect.Type !=
                        LocationBackgroundEffectType.DriftingMotes)
                    .Sum(effect => effect.MaxElementCount);
                Assert.That(
                    overlay.ActiveElementCount,
                    Is.EqualTo(expected));
            }
            finally
            {
                Object.DestroyImmediate(overlayObject);
                Object.DestroyImmediate(contentObject);
            }
        }

        [Test]
        public void SharedVisualRefresh_PreservesPlaybackPhase()
        {
            GameObject contentObject = CreateContent();
            GameObject overlayObject = new(
                "Animation Owner",
                typeof(LocationBackgroundAnimationOverlay));
            try
            {
                LocationBackgroundAnimationOverlay overlay =
                    overlayObject.GetComponent<
                        LocationBackgroundAnimationOverlay>();
                overlay.Initialize(
                    contentObject.GetComponent<RectTransform>());
                overlay.Show("SECURITY");
                overlay.Advance(3.5f);

                overlay.Show("INTERVIEW");

                Assert.That(
                    overlay.ActiveProfileId,
                    Is.EqualTo("SECURITY_INTERVIEW"));
                Assert.That(
                    overlay.PlaybackTime,
                    Is.EqualTo(3.5f).Within(.001f));
            }
            finally
            {
                Object.DestroyImmediate(overlayObject);
                Object.DestroyImmediate(contentObject);
            }
        }

        [Test]
        public void ProfileMoteColor_TintsExistingParticleSystem()
        {
            GameObject contentObject = CreateContent();
            GameObject overlayObject = new(
                "Animation Owner",
                typeof(LocationBackgroundAnimationOverlay));
            try
            {
                LocationBackgroundAnimationOverlay overlay =
                    overlayObject.GetComponent<
                        LocationBackgroundAnimationOverlay>();
                overlay.Initialize(
                    contentObject.GetComponent<RectTransform>());
                overlay.Show("PORT");
                Color fallback =
                    new(.1f, .2f, .3f, .47f);

                Color tint =
                    overlay.ResolveAmbientParticleTint(fallback);

                Assert.That(tint, Is.Not.EqualTo(fallback));
                Assert.That(
                    tint.a,
                    Is.EqualTo(fallback.a).Within(.001f));
            }
            finally
            {
                Object.DestroyImmediate(overlayObject);
                Object.DestroyImmediate(contentObject);
            }
        }

        [Test]
        public void ReducedMotion_FreezesPlaybackAndRestoresBackgroundPose()
        {
            GameObject viewportObject = new(
                "Viewport",
                typeof(RectTransform));
            RectTransform viewport =
                viewportObject.GetComponent<RectTransform>();
            viewport.sizeDelta = new Vector2(1920f, 1080f);
            GameObject motionObject = new(
                "Motion",
                typeof(RectTransform));
            motionObject.transform.SetParent(
                viewportObject.transform,
                false);
            RectTransform motion =
                motionObject.GetComponent<RectTransform>();
            motion.sizeDelta = viewport.sizeDelta;
            GameObject contentObject = CreateContent();
            contentObject.transform.SetParent(
                motionObject.transform,
                false);
            GameObject overlayObject = new(
                "Animation Owner",
                typeof(LocationBackgroundAnimationOverlay));
            try
            {
                LocationBackgroundAnimationOverlay overlay =
                    overlayObject.GetComponent<
                        LocationBackgroundAnimationOverlay>();
                overlay.Initialize(
                    contentObject.GetComponent<RectTransform>(),
                    motion);
                overlay.Show("PORT");
                overlay.Advance(2f);

                overlay.SetReducedMotion(true);
                float frozenTime = overlay.PlaybackTime;
                overlay.Advance(5f);
                overlay.Show("PROMENADE");
                overlay.SetReducedMotion(true);

                Assert.That(overlay.IsPaused, Is.True);
                Assert.That(
                    overlay.PlaybackTime,
                    Is.EqualTo(frozenTime));
                Assert.That(
                    motion.anchoredPosition,
                    Is.EqualTo(Vector2.zero));
                Assert.That(
                    motion.localScale,
                    Is.EqualTo(Vector3.one));
                Assert.That(
                    motion.localRotation,
                    Is.EqualTo(Quaternion.identity));
            }
            finally
            {
                Object.DestroyImmediate(overlayObject);
                Object.DestroyImmediate(viewportObject);
            }
        }

        [Test]
        public void Hide_RestoresMotionRootAndClearsProfile()
        {
            GameObject viewportObject = new(
                "Viewport",
                typeof(RectTransform));
            RectTransform viewport =
                viewportObject.GetComponent<RectTransform>();
            viewport.sizeDelta = new Vector2(1920f, 1080f);
            GameObject motionObject = new(
                "Motion",
                typeof(RectTransform));
            motionObject.transform.SetParent(
                viewportObject.transform,
                false);
            RectTransform motion =
                motionObject.GetComponent<RectTransform>();
            motion.sizeDelta = viewport.sizeDelta;
            Vector2 authoredPosition = new(5f, -7f);
            Vector3 authoredScale = new(1.02f, .99f, 1f);
            Quaternion authoredRotation =
                Quaternion.Euler(0f, 0f, 1.2f);
            motion.anchoredPosition = authoredPosition;
            motion.localScale = authoredScale;
            motion.localRotation = authoredRotation;
            GameObject contentObject = CreateContent();
            contentObject.transform.SetParent(
                motionObject.transform,
                false);
            GameObject overlayObject = new(
                "Animation Owner",
                typeof(LocationBackgroundAnimationOverlay));
            try
            {
                LocationBackgroundAnimationOverlay overlay =
                    overlayObject.GetComponent<
                        LocationBackgroundAnimationOverlay>();
                overlay.Initialize(
                    contentObject.GetComponent<RectTransform>(),
                    motion);
                overlay.Show("PORT");
                overlay.ApplyAtTime(4f);

                Assert.That(
                    motion.localScale,
                    Is.Not.EqualTo(authoredScale));

                overlay.Hide();

                Assert.That(overlay.ActiveProfileId, Is.Empty);
                Assert.That(motion.anchoredPosition, Is.EqualTo(authoredPosition));
                Assert.That(motion.localScale, Is.EqualTo(authoredScale));
                Assert.That(
                    Quaternion.Angle(
                        motion.localRotation,
                        authoredRotation),
                    Is.LessThan(.001f));
            }
            finally
            {
                Object.DestroyImmediate(overlayObject);
                Object.DestroyImmediate(viewportObject);
            }
        }

        private static GameObject CreateContent()
        {
            GameObject content = new(
                "Cover Image",
                typeof(RectTransform));
            content.GetComponent<RectTransform>().sizeDelta =
                new Vector2(1920f, 1080f);
            return content;
        }

        private static bool IsMotion(
            LocationBackgroundEffectType type)
        {
            return type ==
                    LocationBackgroundEffectType.FullBackgroundDrift ||
                   type ==
                    LocationBackgroundEffectType.FullBackgroundShake;
        }
    }
}
