using System.Linq;
using NUnit.Framework;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using Wake.UI;

namespace Wake.Tests
{
    public sealed class DialogueChoiceButtonPoolTests
    {
        private static readonly string[] AuthoredNames =
        {
            "Choice", "Choice (1)", "Choice (2)", "Choice (3)"
        };

        [Test]
        public void EnsureCapacity_ExpandsFourAuthoredButtonsToEight()
        {
            GameObject container = CreateContainer();
            try
            {
                DialogueChoiceButtonSet result =
                    DialogueChoiceButtonPool.EnsureCapacity(
                        container.transform,
                        AuthoredNames,
                        8);

                Assert.That(result.Buttons, Has.Length.EqualTo(8));
                Assert.That(result.Labels, Has.Length.EqualTo(8));
                Assert.That(container.transform.childCount, Is.EqualTo(8));
                Assert.That(
                    result.Buttons.Select(button => button.name),
                    Is.EqualTo(new[]
                    {
                        "Choice", "Choice (1)", "Choice (2)", "Choice (3)",
                        "Choice (4)", "Choice (5)", "Choice (6)", "Choice (7)"
                    }));
            }
            finally
            {
                Object.DestroyImmediate(container);
            }
        }

        [Test]
        public void GeneratedButtons_AreInactiveAndHaveNoPersistentClick()
        {
            GameObject container = CreateContainer();
            try
            {
                DialogueChoiceButtonSet result =
                    DialogueChoiceButtonPool.EnsureCapacity(
                        container.transform,
                        AuthoredNames,
                        8);

                foreach (Button button in result.Buttons.Skip(4))
                {
                    Assert.That(button.gameObject.activeSelf, Is.False);
                    Assert.That(
                        button.onClick.GetPersistentEventCount(),
                        Is.Zero);
                }
            }
            finally
            {
                Object.DestroyImmediate(container);
            }
        }

        [Test]
        public void EnsureCapacity_ReusesExistingAuthoredButtons()
        {
            GameObject container = CreateContainer();
            Button original =
                container.transform.Find("Choice").GetComponent<Button>();
            try
            {
                DialogueChoiceButtonSet result =
                    DialogueChoiceButtonPool.EnsureCapacity(
                        container.transform,
                        AuthoredNames,
                        4);

                Assert.That(result.Buttons[0], Is.SameAs(original));
                Assert.That(container.transform.childCount, Is.EqualTo(4));
            }
            finally
            {
                Object.DestroyImmediate(container);
            }
        }

        [Test]
        public void EnsureCapacity_RejectsMissingAuthoredButton()
        {
            GameObject container = CreateContainer();
            Object.DestroyImmediate(
                container.transform.Find("Choice (2)").gameObject);
            try
            {
                Assert.That(
                    () => DialogueChoiceButtonPool.EnsureCapacity(
                        container.transform,
                        AuthoredNames,
                        8),
                    Throws.TypeOf<System.InvalidOperationException>());
            }
            finally
            {
                Object.DestroyImmediate(container);
            }
        }

        [Test]
        public void EnsureCapacity_RejectsCapacityBelowAuthoredCount()
        {
            GameObject container = CreateContainer();
            try
            {
                Assert.That(
                    () => DialogueChoiceButtonPool.EnsureCapacity(
                        container.transform,
                        AuthoredNames,
                        3),
                    Throws.TypeOf<System.ArgumentOutOfRangeException>());
            }
            finally
            {
                Object.DestroyImmediate(container);
            }
        }

        private static GameObject CreateContainer()
        {
            GameObject container = new("Choices", typeof(RectTransform));
            foreach (string name in AuthoredNames)
            {
                GameObject buttonObject = new(
                    name,
                    typeof(RectTransform),
                    typeof(CanvasRenderer),
                    typeof(Image),
                    typeof(Button));
                buttonObject.transform.SetParent(container.transform, false);
                GameObject labelObject = new(
                    "Label",
                    typeof(RectTransform),
                    typeof(CanvasRenderer),
                    typeof(TextMeshProUGUI));
                labelObject.transform.SetParent(buttonObject.transform, false);
            }
            return container;
        }
    }
}
