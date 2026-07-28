using NUnit.Framework;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using Wake.UI;

namespace Wake.Tests.EditMode
{
    public sealed class DialogueAdvanceControlTests
    {
        private GameObject root;
        private Button button;
        private Image image;
        private TMP_Text label;
        private DialogueAdvanceControl control;

        [SetUp]
        public void SetUp()
        {
            root = new GameObject(
                "Advance",
                typeof(RectTransform),
                typeof(CanvasRenderer),
                typeof(Image),
                typeof(Button));
            image = root.GetComponent<Image>();
            button = root.GetComponent<Button>();
            button.targetGraphic = image;

            GameObject labelObject = new(
                "Label",
                typeof(RectTransform),
                typeof(TextMeshProUGUI));
            labelObject.transform.SetParent(root.transform, false);
            label = labelObject.GetComponent<TMP_Text>();
            label.text = "Next";

            control = root.AddComponent<DialogueAdvanceControl>();
            control.Initialize(button);
        }

        [TearDown]
        public void TearDown()
        {
            Object.DestroyImmediate(root);
        }

        [Test]
        public void Initialize_HidesLegacyTextLabel()
        {
            Assert.That(label.text, Is.Empty);
            Assert.That(label.gameObject.activeSelf, Is.False);
        }

        [Test]
        public void RevealLine_ShowsMutedSkipState()
        {
            control.SetState(DialogueAdvanceState.RevealLine);

            Assert.That(root.activeSelf, Is.True);
            Assert.That(control.AccessibleHint, Is.EqualTo("문장 전체 보기"));
            Assert.That(root.GetComponent<CanvasGroup>().alpha,
                Is.EqualTo(0.72f).Within(0.001f));
        }

        [Test]
        public void AdvanceLine_ShowsFullyOpaqueState()
        {
            control.SetState(DialogueAdvanceState.AdvanceLine);

            Assert.That(root.activeSelf, Is.True);
            Assert.That(control.AccessibleHint, Is.EqualTo("다음 대사"));
            Assert.That(root.GetComponent<CanvasGroup>().alpha,
                Is.EqualTo(1f));
        }

        [Test]
        public void Hidden_DisablesButtonObject()
        {
            control.SetState(DialogueAdvanceState.Hidden);

            Assert.That(root.activeSelf, Is.False);
        }

        [Test]
        public void SetSprites_ConfiguresNormalAndPressedVisuals()
        {
            Texture2D texture = new(4, 4);
            Sprite normal = Sprite.Create(
                texture,
                new Rect(0, 0, 2, 2),
                Vector2.one * 0.5f);
            Sprite pressed = Sprite.Create(
                texture,
                new Rect(2, 2, 2, 2),
                Vector2.one * 0.5f);

            control.SetSprites(normal, pressed);

            Assert.That(image.sprite, Is.SameAs(normal));
            Assert.That(button.transition,
                Is.EqualTo(Selectable.Transition.SpriteSwap));
            Assert.That(button.spriteState.pressedSprite,
                Is.SameAs(pressed));

            Object.DestroyImmediate(normal);
            Object.DestroyImmediate(pressed);
            Object.DestroyImmediate(texture);
        }
    }
}
