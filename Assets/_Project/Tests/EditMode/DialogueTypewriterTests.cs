using System;
using NUnit.Framework;
using TMPro;
using UnityEditor;
using UnityEngine;
using Wake.UI;

namespace Wake.Tests
{
    public sealed class DialogueTypewriterTests
    {
        private const string FontPath =
            "Assets/_Project/Resources/Typography/" +
            "Pretendard Medium SDF.asset";

        [Test]
        public void Begin_StartsWithNoVisibleCharacters()
        {
            var progress = new DialogueTypewriterProgress(50f);

            progress.Begin(12);

            Assert.That(progress.TotalCharacters, Is.EqualTo(12));
            Assert.That(progress.VisibleCharacters, Is.Zero);
            Assert.That(progress.IsRevealing, Is.True);
        }

        [Test]
        public void Advance_RevealsCharactersAtConfiguredRate()
        {
            var progress = new DialogueTypewriterProgress(50f);
            progress.Begin(20);

            progress.Advance(0.1f);

            Assert.That(progress.VisibleCharacters, Is.EqualTo(5));
        }

        [Test]
        public void Advance_PreservesFractionalCharacterTime()
        {
            var progress = new DialogueTypewriterProgress(50f);
            progress.Begin(20);

            progress.Advance(0.01f);
            Assert.That(progress.VisibleCharacters, Is.Zero);
            progress.Advance(0.01f);

            Assert.That(progress.VisibleCharacters, Is.EqualTo(1));
        }

        [Test]
        public void Advance_StopsAtTotalCharacterCount()
        {
            var progress = new DialogueTypewriterProgress(50f);
            progress.Begin(3);

            progress.Advance(10f);

            Assert.That(progress.VisibleCharacters, Is.EqualTo(3));
            Assert.That(progress.IsRevealing, Is.False);
        }

        [Test]
        public void Complete_ReturnsTrueOnlyWhileRevealWasActive()
        {
            var progress = new DialogueTypewriterProgress(50f);
            progress.Begin(10);

            Assert.That(progress.Complete(), Is.True);
            Assert.That(progress.VisibleCharacters, Is.EqualTo(10));
            Assert.That(progress.Complete(), Is.False);
        }

        [Test]
        public void Begin_ReplacesPreviousRevealState()
        {
            var progress = new DialogueTypewriterProgress(50f);
            progress.Begin(20);
            progress.Advance(0.2f);

            progress.Begin(4);

            Assert.That(progress.TotalCharacters, Is.EqualTo(4));
            Assert.That(progress.VisibleCharacters, Is.Zero);
        }

        [Test]
        public void EmptyAndNegativeTotals_AreImmediatelyComplete()
        {
            var progress = new DialogueTypewriterProgress(50f);

            progress.Begin(0);
            Assert.That(progress.IsRevealing, Is.False);
            progress.Begin(-10);

            Assert.That(progress.TotalCharacters, Is.Zero);
            Assert.That(progress.IsRevealing, Is.False);
        }

        [TestCase(0f, DialogueTypewriter.MinimumCharactersPerSecond)]
        [TestCase(10f, DialogueTypewriter.MinimumCharactersPerSecond)]
        [TestCase(50f, 50f)]
        [TestCase(200f, DialogueTypewriter.MaximumCharactersPerSecond)]
        public void Speed_IsClampedToSupportedInspectorRange(
            float requested,
            float expected)
        {
            var progress =
                new DialogueTypewriterProgress(requested);

            Assert.That(
                progress.CharactersPerSecond,
                Is.EqualTo(expected));
        }

        [Test]
        public void RichTextTags_AreNotCountedAsVisibleCharacters()
        {
            using TypewriterRig rig = new();

            rig.Typewriter.Play("<b>안녕</b>");

            Assert.That(rig.Label.text, Is.EqualTo("<b>안녕</b>"));
            Assert.That(rig.Typewriter.TotalCharacters, Is.EqualTo(2));
            Assert.That(rig.Label.maxVisibleCharacters, Is.Zero);
        }

        [Test]
        public void CompleteImmediately_ShowsAllAndConsumesOneClick()
        {
            using TypewriterRig rig = new();
            rig.Typewriter.Play("빠르게 표시되는 문장");

            bool consumed = rig.Typewriter.CompleteImmediately();

            Assert.That(consumed, Is.True);
            Assert.That(rig.Typewriter.IsRevealing, Is.False);
            Assert.That(
                rig.Label.maxVisibleCharacters,
                Is.EqualTo(int.MaxValue));
            Assert.That(
                rig.Typewriter.CompleteImmediately(),
                Is.False);
        }

        [Test]
        public void NewLine_ReplacesPreviousTextAndProgress()
        {
            using TypewriterRig rig = new();
            rig.Typewriter.Play("첫 번째로 긴 문장");

            rig.Typewriter.Play("다음");

            Assert.That(rig.Label.text, Is.EqualTo("다음"));
            Assert.That(rig.Typewriter.VisibleCharacters, Is.Zero);
            Assert.That(rig.Typewriter.TotalCharacters, Is.EqualTo(2));
        }

        [Test]
        public void NullText_IsHandledAsEmptyAndShownImmediately()
        {
            using TypewriterRig rig = new();

            rig.Typewriter.Play(null);

            Assert.That(rig.Label.text, Is.Empty);
            Assert.That(rig.Typewriter.IsRevealing, Is.False);
            Assert.That(
                rig.Label.maxVisibleCharacters,
                Is.EqualTo(int.MaxValue));
        }

        [Test]
        public void CancelAndShowAll_StopsRevealWithoutChangingText()
        {
            using TypewriterRig rig = new();
            rig.Typewriter.Play("취소할 대사");

            rig.Typewriter.CancelAndShowAll();

            Assert.That(rig.Label.text, Is.EqualTo("취소할 대사"));
            Assert.That(rig.Typewriter.IsRevealing, Is.False);
            Assert.That(
                rig.Label.maxVisibleCharacters,
                Is.EqualTo(int.MaxValue));
        }

        private sealed class TypewriterRig : IDisposable
        {
            private readonly GameObject canvasObject;

            public TypewriterRig()
            {
                canvasObject = new GameObject(
                    "Canvas",
                    typeof(RectTransform),
                    typeof(Canvas));
                var labelObject = new GameObject(
                    "Line",
                    typeof(RectTransform),
                    typeof(CanvasRenderer),
                    typeof(TextMeshProUGUI),
                    typeof(DialogueTypewriter));
                labelObject.transform.SetParent(
                    canvasObject.transform,
                    false);
                Label = labelObject.GetComponent<TMP_Text>();
                Label.font =
                    AssetDatabase.LoadAssetAtPath<TMP_FontAsset>(FontPath);
                Assert.That(Label.font, Is.Not.Null, FontPath);
                Typewriter =
                    labelObject.GetComponent<DialogueTypewriter>();
                Typewriter.Initialize(
                    Label,
                    DialogueTypewriter.DefaultCharactersPerSecond);
            }

            public TMP_Text Label { get; }
            public DialogueTypewriter Typewriter { get; }

            public void Dispose()
            {
                UnityEngine.Object.DestroyImmediate(canvasObject);
            }
        }
    }
}
