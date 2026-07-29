using NUnit.Framework;
using UnityEngine;
using UnityEngine.UI;
using Wake.Exploration;

namespace Wake.Tests
{
    public sealed class UiCharacterIdleMotionTests
    {
        [Test]
        public void Evaluate_TimeZero_StartsFromAuthoredState()
        {
            UiCharacterIdleMotionSample sample =
                UiCharacterIdleMotionEvaluator.Evaluate(17, 0f);

            Assert.That(
                sample.AnchoredPositionOffset,
                Is.EqualTo(Vector2.zero));
            Assert.That(
                sample.ScaleMultiplier,
                Is.EqualTo(Vector2.one));
            Assert.That(sample.RotationDegrees, Is.EqualTo(0f));
        }

        [Test]
        public void Evaluate_SameSeedAndTime_ReturnsIdenticalSample()
        {
            UiCharacterIdleMotionSample first =
                UiCharacterIdleMotionEvaluator.Evaluate(73, 9.25f);
            UiCharacterIdleMotionSample second =
                UiCharacterIdleMotionEvaluator.Evaluate(73, 9.25f);

            Assert.That(
                second.AnchoredPositionOffset,
                Is.EqualTo(first.AnchoredPositionOffset));
            Assert.That(
                second.ScaleMultiplier,
                Is.EqualTo(first.ScaleMultiplier));
            Assert.That(
                second.RotationDegrees,
                Is.EqualTo(first.RotationDegrees));
        }

        [Test]
        public void Evaluate_DifferentSeeds_ProduceDistinctMotion()
        {
            UiCharacterIdleMotionSample first =
                UiCharacterIdleMotionEvaluator.Evaluate(11, 5.75f);
            UiCharacterIdleMotionSample second =
                UiCharacterIdleMotionEvaluator.Evaluate(12, 5.75f);

            bool isDifferent =
                first.AnchoredPositionOffset !=
                    second.AnchoredPositionOffset ||
                first.RotationDegrees != second.RotationDegrees ||
                first.ScaleMultiplier != second.ScaleMultiplier;

            Assert.That(isDifferent, Is.True);
        }

        [Test]
        public void Evaluate_DefaultMotion_RemainsSubtleAndVisible()
        {
            for (int seed = 0; seed < 12; seed++)
            {
                for (float time = 0f;
                     time <= 30f;
                     time += 0.025f)
                {
                    UiCharacterIdleMotionSample sample =
                        UiCharacterIdleMotionEvaluator.Evaluate(
                            seed,
                            time);

                    Assert.That(
                        sample.AnchoredPositionOffset.x,
                        Is.EqualTo(0f));
                    Assert.That(
                        Mathf.Abs(
                            sample.AnchoredPositionOffset.y),
                        Is.LessThanOrEqualTo(1.5f));
                    Assert.That(
                        sample.ScaleMultiplier.x,
                        Is.EqualTo(1f));
                    Assert.That(
                        sample.ScaleMultiplier.y,
                        Is.InRange(
                            UiCharacterIdleMotionEvaluator
                                .MinimumScaleYMultiplier,
                            UiCharacterIdleMotionEvaluator
                                .MaximumScaleYMultiplier));
                    Assert.That(
                        Mathf.Abs(sample.RotationDegrees),
                        Is.LessThanOrEqualTo(0.65f));
                }
            }
        }

        [Test]
        public void Advance_UsesSelectedTimeSource()
        {
            GameObject target = CreateTarget(out Image image);
            try
            {
                UiCharacterIdleMotion motion =
                    target.AddComponent<UiCharacterIdleMotion>();
                motion.Configure(
                    deterministicSeed: 4,
                    graphic: image,
                    unscaledTime: true);

                motion.Advance(
                    scaledDeltaTime: 0.1f,
                    unscaledDeltaTime: 0.7f);
                Assert.That(
                    motion.ElapsedTime,
                    Is.EqualTo(0.7f).Within(0.0001f));

                motion.Restart();
                motion.UseUnscaledTime = false;
                motion.Advance(
                    scaledDeltaTime: 0.2f,
                    unscaledDeltaTime: 0.9f);
                Assert.That(
                    motion.ElapsedTime,
                    Is.EqualTo(0.2f).Within(0.0001f));
            }
            finally
            {
                Object.DestroyImmediate(target);
            }
        }

        [Test]
        public void StopAndRestore_RestoresAuthoredRectAndImageState()
        {
            GameObject target = CreateTarget(out Image image);
            RectTransform rect =
                target.GetComponent<RectTransform>();
            Vector2 authoredPosition = new(37f, -18f);
            Vector3 authoredScale = new(1.2f, 0.85f, 1f);
            Quaternion authoredRotation =
                Quaternion.Euler(0f, 0f, 7f);
            Color authoredColor =
                new(0.3f, 0.65f, 0.8f, 0.74f);
            rect.anchoredPosition = authoredPosition;
            rect.localScale = authoredScale;
            rect.localRotation = authoredRotation;
            image.color = authoredColor;

            try
            {
                UiCharacterIdleMotion motion =
                    target.AddComponent<UiCharacterIdleMotion>();
                motion.Configure(91, image);
                motion.ApplyAtTime(4.37f);

                Assert.That(
                    rect.anchoredPosition,
                    Is.Not.EqualTo(authoredPosition));
                Assert.That(image.color, Is.EqualTo(authoredColor));

                motion.StopAndRestore();

                Assert.That(
                    rect.anchoredPosition,
                    Is.EqualTo(authoredPosition));
                Assert.That(
                    rect.localScale,
                    Is.EqualTo(authoredScale));
                Assert.That(
                    Quaternion.Angle(
                        rect.localRotation,
                        authoredRotation),
                    Is.LessThan(0.0001f));
                Assert.That(image.color, Is.EqualTo(authoredColor));
            }
            finally
            {
                Object.DestroyImmediate(target);
            }
        }

        [Test]
        public void ApplyAtTime_NeverChangesGraphicColor()
        {
            GameObject target = CreateTarget(out Image image);
            Color authoredColor =
                new(0.32f, 0.61f, 0.83f, 0.57f);
            image.color = authoredColor;
            try
            {
                UiCharacterIdleMotion motion =
                    target.AddComponent<UiCharacterIdleMotion>();
                motion.Configure(8, image);

                for (float time = 0f;
                     time <= 30f;
                     time += 0.025f)
                {
                    motion.ApplyAtTime(time);
                    Assert.That(
                        image.color,
                        Is.EqualTo(authoredColor),
                        $"Graphic color changed at {time:F3}s.");
                }
            }
            finally
            {
                Object.DestroyImmediate(target);
            }
        }

        [Test]
        public void Rebase_DoesNotDoubleApplyCurrentSample()
        {
            GameObject target = CreateTarget(out Image image);
            RectTransform rect =
                target.GetComponent<RectTransform>();
            try
            {
                UiCharacterIdleMotion motion =
                    target.AddComponent<UiCharacterIdleMotion>();
                motion.Configure(31, image);
                motion.ApplyAtTime(3.8f);
                Vector2 positionBefore = rect.anchoredPosition;
                Vector3 scaleBefore = rect.localScale;
                Quaternion rotationBefore = rect.localRotation;
                Color colorBefore = image.color;

                motion.Rebase();

                Assert.That(
                    rect.anchoredPosition,
                    Is.EqualTo(positionBefore));
                Assert.That(
                    rect.localScale,
                    Is.EqualTo(scaleBefore));
                Assert.That(
                    Quaternion.Angle(
                        rect.localRotation,
                        rotationBefore),
                    Is.LessThan(0.0001f));
                Assert.That(image.color, Is.EqualTo(colorBefore));
            }
            finally
            {
                Object.DestroyImmediate(target);
            }
        }

        [Test]
        public void Rebase_CapturesLayoutWritesButNotInFlightScale()
        {
            GameObject target = CreateTarget(out Image image);
            RectTransform rect =
                target.GetComponent<RectTransform>();
            Vector3 authoredScale = new(1.15f, 0.9f, 1f);
            Quaternion authoredRotation =
                Quaternion.Euler(0f, 0f, -4f);
            rect.localScale = authoredScale;
            rect.localRotation = authoredRotation;

            try
            {
                UiCharacterIdleMotion motion =
                    target.AddComponent<UiCharacterIdleMotion>();
                motion.Configure(62, image);
                motion.ApplyAtTime(6.2f);

                Vector2 newLayoutPosition = new(-43f, 27f);
                Color newLayoutColor =
                    new(0.85f, 0.4f, 0.25f, 0.68f);
                rect.anchoredPosition = newLayoutPosition;
                image.color = newLayoutColor;
                motion.CaptureAuthoredLayout();
                motion.StopAndRestore();

                Assert.That(
                    rect.anchoredPosition,
                    Is.EqualTo(newLayoutPosition));
                Assert.That(
                    rect.localScale,
                    Is.EqualTo(authoredScale));
                Assert.That(
                    Quaternion.Angle(
                        rect.localRotation,
                        authoredRotation),
                    Is.LessThan(0.0001f));
                Assert.That(image.color, Is.EqualTo(newLayoutColor));
            }
            finally
            {
                Object.DestroyImmediate(target);
            }
        }

        [Test]
        public void SetAuthoredGraphicColor_DoesNotRebaseTransform()
        {
            GameObject target = CreateTarget(out Image image);
            RectTransform rect =
                target.GetComponent<RectTransform>();
            try
            {
                UiCharacterIdleMotion motion =
                    target.AddComponent<UiCharacterIdleMotion>();
                motion.Configure(8, image);
                motion.ApplyAtTime(4.25f);
                Vector2 positionBefore = rect.anchoredPosition;
                Vector3 scaleBefore = rect.localScale;
                Quaternion rotationBefore = rect.localRotation;
                Color newTint =
                    new(0.2f, 0.7f, 0.55f, 0.8f);

                motion.SetAuthoredGraphicColor(newTint);

                Assert.That(
                    rect.anchoredPosition,
                    Is.EqualTo(positionBefore));
                Assert.That(
                    rect.localScale,
                    Is.EqualTo(scaleBefore));
                Assert.That(
                    Quaternion.Angle(
                        rect.localRotation,
                        rotationBefore),
                    Is.LessThan(0.0001f));
                Assert.That(image.color, Is.EqualTo(newTint));

                motion.StopAndRestore();
                Assert.That(image.color, Is.EqualTo(newTint));
            }
            finally
            {
                Object.DestroyImmediate(target);
            }
        }

        private static GameObject CreateTarget(out Image image)
        {
            GameObject target = new(
                "UI Character",
                typeof(RectTransform),
                typeof(CanvasRenderer),
                typeof(Image));
            image = target.GetComponent<Image>();
            return target;
        }
    }
}
