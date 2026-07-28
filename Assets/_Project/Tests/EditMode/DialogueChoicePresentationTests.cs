using System.Collections.Generic;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.UI;
using Wake.UI;

namespace Wake.Tests.EditMode
{
    public sealed class DialogueChoicePresentationTests
    {
        private GameObject root;
        private RectTransform container;
        private readonly List<Button> buttons = new();
        private DialogueChoicePresentation presentation;

        [SetUp]
        public void SetUp()
        {
            root = new GameObject("Root", typeof(RectTransform));
            GameObject containerObject = new(
                "Choices",
                typeof(RectTransform),
                typeof(GridLayoutGroup));
            containerObject.transform.SetParent(root.transform, false);
            container = containerObject.GetComponent<RectTransform>();
            GridLayoutGroup grid =
                containerObject.GetComponent<GridLayoutGroup>();
            grid.constraint =
                GridLayoutGroup.Constraint.FixedColumnCount;
            grid.constraintCount = 2;

            for (int i = 0; i < 4; i++)
            {
                GameObject choice = new(
                    $"Choice {i}",
                    typeof(RectTransform),
                    typeof(Image),
                    typeof(Button));
                choice.transform.SetParent(container, false);
                buttons.Add(choice.GetComponent<Button>());
            }

            presentation =
                containerObject.AddComponent<DialogueChoicePresentation>();
            presentation.Initialize(container, buttons);
        }

        [TearDown]
        public void TearDown()
        {
            Object.DestroyImmediate(root);
            buttons.Clear();
        }

        [Test]
        public void Show_TracksActiveButtonsAndRestoresVisuals()
        {
            buttons[3].gameObject.SetActive(false);

            presentation.Show();

            Assert.That(container.gameObject.activeSelf, Is.True);
            Assert.That(presentation.ActiveCount, Is.EqualTo(3));
            for (int i = 0; i < 3; i++)
            {
                Assert.That(
                    buttons[i].targetGraphic.color.a,
                    Is.EqualTo(1f));
                Assert.That(
                    buttons[i].transform.localScale,
                    Is.EqualTo(Vector3.one));
            }
        }

        [Test]
        public void Initialize_AddsHoverFeedback()
        {
            foreach (Button button in buttons)
            {
                Assert.That(
                    button.GetComponent<UiHoverFeedback>(),
                    Is.Not.Null);
            }
        }

        [Test]
        public void Show_ConfiguresExplicitTwoColumnNavigation()
        {
            presentation.Show();

            Navigation first = buttons[0].navigation;
            Navigation last = buttons[3].navigation;
            Assert.That(first.mode, Is.EqualTo(Navigation.Mode.Explicit));
            Assert.That(first.selectOnRight, Is.SameAs(buttons[1]));
            Assert.That(first.selectOnDown, Is.SameAs(buttons[2]));
            Assert.That(last.selectOnLeft, Is.SameAs(buttons[2]));
            Assert.That(last.selectOnUp, Is.SameAs(buttons[1]));
        }

        [Test]
        public void Hide_DeactivatesContainerAndClearsCount()
        {
            presentation.Show();

            presentation.Hide();

            Assert.That(container.gameObject.activeSelf, Is.False);
            Assert.That(presentation.ActiveCount, Is.Zero);
        }

        [TestCase(0, 4, 2, DialogueChoiceDirection.Right, 1)]
        [TestCase(0, 4, 2, DialogueChoiceDirection.Down, 2)]
        [TestCase(3, 4, 2, DialogueChoiceDirection.Left, 2)]
        [TestCase(3, 4, 2, DialogueChoiceDirection.Up, 1)]
        [TestCase(1, 3, 2, DialogueChoiceDirection.Down, -1)]
        [TestCase(2, 3, 2, DialogueChoiceDirection.Right, -1)]
        [TestCase(0, 1, 1, DialogueChoiceDirection.Down, -1)]
        public void NavigationPolicy_ReturnsExpectedNeighbor(
            int index,
            int count,
            int columns,
            DialogueChoiceDirection direction,
            int expected)
        {
            Assert.That(
                DialogueChoiceNavigationPolicy.FindNeighbor(
                    index,
                    count,
                    columns,
                    direction),
                Is.EqualTo(expected));
        }

        [Test]
        public void NavigationPolicy_RejectsInvalidIndex()
        {
            Assert.That(
                DialogueChoiceNavigationPolicy.FindNeighbor(
                    -1,
                    4,
                    2,
                    DialogueChoiceDirection.Right),
                Is.EqualTo(-1));
            Assert.That(
                DialogueChoiceNavigationPolicy.FindNeighbor(
                    4,
                    4,
                    2,
                    DialogueChoiceDirection.Left),
                Is.EqualTo(-1));
        }
    }
}
