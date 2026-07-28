using NUnit.Framework;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
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
        public void Feedback_HidesPromptUntilPointerOrFocusRequestsIt()
        {
            GameObject root = CreateFeedbackTarget(out ExplorationHotspotFeedback feedback, out TMP_Text label);
            try
            {
                feedback.Configure("Inspect ledger", label);

                Assert.That(feedback.IsIndicatorVisible, Is.False);
                Assert.That(label.gameObject.activeSelf, Is.False);

                feedback.OnPointerEnter(null);
                Assert.That(feedback.IsIndicatorVisible, Is.True);

                feedback.OnPointerExit(null);
                Assert.That(feedback.IsIndicatorVisible, Is.False);

                feedback.OnSelect(null);
                Assert.That(feedback.IsIndicatorVisible, Is.True);

                feedback.OnDeselect(null);
                Assert.That(feedback.IsIndicatorVisible, Is.False);
            }
            finally
            {
                Object.DestroyImmediate(root);
            }
        }

        [Test]
        public void Feedback_AccessibilityModeRevealsAndHidesAllPrompts()
        {
            GameObject first = CreateFeedbackTarget(out ExplorationHotspotFeedback firstFeedback, out TMP_Text firstLabel);
            GameObject second = CreateFeedbackTarget(out ExplorationHotspotFeedback secondFeedback, out TMP_Text secondLabel);
            try
            {
                firstFeedback.Configure("Talk", firstLabel);
                secondFeedback.Configure("Examine", secondLabel);

                ExplorationHotspotFeedback.SetAccessibilityIndicators(true);

                Assert.That(ExplorationHotspotFeedback.AccessibilityIndicatorsEnabled, Is.True);
                Assert.That(firstFeedback.IsIndicatorVisible, Is.True);
                Assert.That(secondFeedback.IsIndicatorVisible, Is.True);

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
        public void Feedback_UsesExistingLabelInsteadOfCreatingDuplicatePrompt()
        {
            GameObject root = CreateFeedbackTarget(out ExplorationHotspotFeedback feedback, out TMP_Text label);
            try
            {
                feedback.Configure("Board the ship", label);

                Assert.That(root.GetComponentsInChildren<TMP_Text>(true), Has.Length.EqualTo(1));
                Assert.That(label.text, Is.EqualTo("Board the ship"));
                Assert.That(label.raycastTarget, Is.False);
            }
            finally
            {
                Object.DestroyImmediate(root);
            }
        }

        [Test]
        public void Feedback_ResetTransientStateClearsClickFocusResidue()
        {
            GameObject root = CreateFeedbackTarget(
                out ExplorationHotspotFeedback feedback,
                out TMP_Text label);
            try
            {
                feedback.Configure("Talk", label);
                feedback.OnPointerEnter(null);
                feedback.OnSelect(null);
                Assert.That(feedback.IsIndicatorVisible, Is.True);

                feedback.ResetTransientState();

                Assert.That(feedback.IsIndicatorVisible, Is.False);
            }
            finally
            {
                Object.DestroyImmediate(root);
            }
        }

        private static GameObject CreateFeedbackTarget(
            out ExplorationHotspotFeedback feedback,
            out TMP_Text label)
        {
            GameObject root = new(
                "Feedback Target",
                typeof(RectTransform),
                typeof(CanvasRenderer),
                typeof(Image));
            feedback = root.AddComponent<ExplorationHotspotFeedback>();

            GameObject labelObject = new(
                "Prompt",
                typeof(RectTransform),
                typeof(TextMeshProUGUI));
            labelObject.transform.SetParent(root.transform, false);
            label = labelObject.GetComponent<TMP_Text>();
            return root;
        }
    }
}
