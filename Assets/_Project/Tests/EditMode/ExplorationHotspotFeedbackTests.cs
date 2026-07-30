using NUnit.Framework;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using Wake.Exploration;

namespace Wake.Tests
{
    public sealed class ExplorationHotspotFeedbackTests
    {
        [TearDown]
        public void TearDown()
        {
            ExplorationHotspotFeedback.SetAccessibilityIndicators(false);
        }

        [Test]
        public void Feedback_HoverAndSelectionUseOutlineWithoutTextPrompt()
        {
            GameObject root = CreateFeedbackTarget(
                out ExplorationHotspotFeedback feedback);
            try
            {
                feedback.Configure();

                Assert.That(feedback.IsIndicatorVisible, Is.False);
                AssertNoTextPrompt(root);

                feedback.OnPointerEnter(null);
                Assert.That(feedback.IsIndicatorVisible, Is.True);
                AssertNoTextPrompt(root);

                feedback.OnPointerExit(null);
                Assert.That(feedback.IsIndicatorVisible, Is.False);

                feedback.OnSelect(null);
                Assert.That(feedback.IsIndicatorVisible, Is.True);
                AssertNoTextPrompt(root);

                feedback.OnDeselect(null);
                Assert.That(feedback.IsIndicatorVisible, Is.False);
            }
            finally
            {
                Object.DestroyImmediate(root);
            }
        }

        [Test]
        public void Feedback_AccessibilityModeUsesOutlineWithoutTextPrompt()
        {
            GameObject first = CreateFeedbackTarget(
                out ExplorationHotspotFeedback firstFeedback);
            GameObject second = CreateFeedbackTarget(
                out ExplorationHotspotFeedback secondFeedback);
            try
            {
                firstFeedback.Configure();
                secondFeedback.Configure();

                ExplorationHotspotFeedback.SetAccessibilityIndicators(true);

                Assert.That(
                    ExplorationHotspotFeedback
                        .AccessibilityIndicatorsEnabled,
                    Is.True);
                Assert.That(firstFeedback.IsIndicatorVisible, Is.True);
                Assert.That(secondFeedback.IsIndicatorVisible, Is.True);
                AssertNoTextPrompt(first);
                AssertNoTextPrompt(second);

                ExplorationHotspotFeedback.SetAccessibilityIndicators(false);

                Assert.That(firstFeedback.IsIndicatorVisible, Is.False);
                Assert.That(secondFeedback.IsIndicatorVisible, Is.False);
            }
            finally
            {
                Object.DestroyImmediate(first);
                Object.DestroyImmediate(second);
            }
        }

        [Test]
        public void Feedback_ResetTransientStateClearsHighlightResidue()
        {
            GameObject root = CreateFeedbackTarget(
                out ExplorationHotspotFeedback feedback);
            try
            {
                feedback.Configure();
                feedback.OnPointerEnter(null);
                feedback.OnSelect(null);
                Assert.That(feedback.IsIndicatorVisible, Is.True);

                feedback.ResetTransientState();

                Assert.That(feedback.IsIndicatorVisible, Is.False);
                AssertNoTextPrompt(root);
            }
            finally
            {
                Object.DestroyImmediate(root);
            }
        }

        private static GameObject CreateFeedbackTarget(
            out ExplorationHotspotFeedback feedback)
        {
            GameObject root = new(
                "Feedback Target",
                typeof(RectTransform),
                typeof(CanvasRenderer),
                typeof(Image));
            feedback = root.AddComponent<ExplorationHotspotFeedback>();
            return root;
        }

        private static void AssertNoTextPrompt(GameObject root)
        {
            Assert.That(
                root.transform.Find("Interaction Label"),
                Is.Null);
            Assert.That(
                root.GetComponentsInChildren<TMP_Text>(true),
                Is.Empty);
        }
    }
}
