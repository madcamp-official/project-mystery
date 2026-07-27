using System.Collections.Generic;
using System.Reflection;
using NUnit.Framework;
using TMPro;
using UnityEditor;
using UnityEngine;
using Wake.Narrative;
using Wake.UI;

namespace Wake.Tests
{
    public sealed class DialogueTypographyTests
    {
        private readonly List<Object> createdObjects = new();
        private TypographyCatalog catalog;
        private TMP_FontAsset body;
        private TMP_FontAsset choice;
        private TMP_FontAsset speaker;

        [SetUp]
        public void SetUp()
        {
            catalog = Track(
                ScriptableObject.CreateInstance<TypographyCatalog>());
            body = CreateFontAsset();
            choice = CreateFontAsset();
            speaker = CreateFontAsset();

            SetFont("body", body);
            SetFont("choice", choice);
            SetFont("speakerName", speaker);
            TypographyService.SetCatalogForTests(catalog);
        }

        [TearDown]
        public void TearDown()
        {
            TypographyService.SetCatalogForTests(null);
            foreach (Object created in createdObjects)
            {
                if (created != null)
                {
                    Object.DestroyImmediate(created);
                }
            }
            createdObjects.Clear();
        }

        [Test]
        public void ApplyLine_UsesBodyFont()
        {
            TMP_Text label = CreateLabel("Line");

            bool applied = DialogueTypography.ApplyLine(label);

            Assert.That(applied, Is.True);
            Assert.That(label.font, Is.SameAs(body));
        }

        [Test]
        public void ApplySpeaker_UsesSpeakerNameFont()
        {
            TMP_Text label = CreateLabel("Speaker");

            bool applied = DialogueTypography.ApplySpeaker(label);

            Assert.That(applied, Is.True);
            Assert.That(label.font, Is.SameAs(speaker));
        }

        [Test]
        public void ApplyChoices_UsesChoiceFontForEveryLabel()
        {
            TMP_Text first = CreateLabel("Choice A");
            TMP_Text second = CreateLabel("Choice B");
            TMP_Text third = CreateLabel("Choice C");

            int applied = DialogueTypography.ApplyChoices(
                new[] { first, second, third });

            Assert.That(applied, Is.EqualTo(3));
            Assert.That(first.font, Is.SameAs(choice));
            Assert.That(second.font, Is.SameAs(choice));
            Assert.That(third.font, Is.SameAs(choice));
        }

        [Test]
        public void ApplyChoices_IgnoresNullEntries()
        {
            TMP_Text first = CreateLabel("Choice A");
            TMP_Text second = CreateLabel("Choice B");

            int applied = DialogueTypography.ApplyChoices(
                new TMP_Text[] { first, null, second });

            Assert.That(applied, Is.EqualTo(2));
            Assert.That(first.font, Is.SameAs(choice));
            Assert.That(second.font, Is.SameAs(choice));
        }

        [Test]
        public void ApplyChoices_ReturnsZeroForNullCollection()
        {
            int applied = DialogueTypography.ApplyChoices(null);

            Assert.That(applied, Is.Zero);
        }

        [Test]
        public void ApplyLine_ReturnsFalseForNullLabel()
        {
            Assert.That(
                DialogueTypography.ApplyLine(null),
                Is.False);
        }

        [Test]
        public void ApplySpeaker_ReturnsFalseForNullLabel()
        {
            Assert.That(
                DialogueTypography.ApplySpeaker(null),
                Is.False);
        }

        [Test]
        public void ApplyLine_ReplacesAuthoredFont()
        {
            TMP_Text label = CreateLabel("Line");
            TMP_FontAsset authored = CreateFontAsset();
            label.font = authored;

            DialogueTypography.ApplyLine(label);

            Assert.That(label.font, Is.SameAs(body));
            Assert.That(label.font, Is.Not.SameAs(authored));
        }

        [Test]
        public void ApplySpeaker_ReplacesAuthoredFont()
        {
            TMP_Text label = CreateLabel("Speaker");
            TMP_FontAsset authored = CreateFontAsset();
            label.font = authored;

            DialogueTypography.ApplySpeaker(label);

            Assert.That(label.font, Is.SameAs(speaker));
            Assert.That(label.font, Is.Not.SameAs(authored));
        }

        [Test]
        public void ApplyChoices_ReplacesMixedAuthoredFonts()
        {
            TMP_Text first = CreateLabel("Choice A");
            TMP_Text second = CreateLabel("Choice B");
            first.font = CreateFontAsset();
            second.font = CreateFontAsset();

            DialogueTypography.ApplyChoices(new[] { first, second });

            Assert.That(first.font, Is.SameAs(choice));
            Assert.That(second.font, Is.SameAs(choice));
        }

        [Test]
        public void DialogueRoles_RemainDistinct()
        {
            TMP_Text line = CreateLabel("Line");
            TMP_Text speakerLabel = CreateLabel("Speaker");
            TMP_Text choiceLabel = CreateLabel("Choice");

            DialogueTypography.ApplyLine(line);
            DialogueTypography.ApplySpeaker(speakerLabel);
            DialogueTypography.ApplyChoices(new[] { choiceLabel });

            Assert.That(line.font, Is.SameAs(body));
            Assert.That(speakerLabel.font, Is.SameAs(speaker));
            Assert.That(choiceLabel.font, Is.SameAs(choice));
            Assert.That(line.font, Is.Not.SameAs(speakerLabel.font));
            Assert.That(line.font, Is.Not.SameAs(choiceLabel.font));
            Assert.That(
                speakerLabel.font,
                Is.Not.SameAs(choiceLabel.font));
        }

        [Test]
        public void ApplyChoices_SupportsReadOnlyLists()
        {
            TMP_Text first = CreateLabel("Choice A");
            TMP_Text second = CreateLabel("Choice B");
            IReadOnlyList<TMP_Text> labels =
                new List<TMP_Text> { first, second }.AsReadOnly();

            int applied = DialogueTypography.ApplyChoices(labels);

            Assert.That(applied, Is.EqualTo(2));
            Assert.That(first.font, Is.SameAs(choice));
            Assert.That(second.font, Is.SameAs(choice));
        }

        [Test]
        public void ApplySurface_AssignsEveryDialogueRole()
        {
            TMP_Text line = CreateLabel("Line");
            TMP_Text speakerLabel = CreateLabel("Speaker");
            TMP_Text firstChoice = CreateLabel("Choice A");
            TMP_Text secondChoice = CreateLabel("Choice B");

            int applied = DialogueTypography.ApplySurface(
                line,
                speakerLabel,
                new[] { firstChoice, secondChoice });

            Assert.That(applied, Is.EqualTo(4));
            Assert.That(line.font, Is.SameAs(body));
            Assert.That(speakerLabel.font, Is.SameAs(speaker));
            Assert.That(firstChoice.font, Is.SameAs(choice));
            Assert.That(secondChoice.font, Is.SameAs(choice));
        }

        [Test]
        public void ApplySurface_CountsOnlyAvailableLabels()
        {
            TMP_Text line = CreateLabel("Line");

            int applied = DialogueTypography.ApplySurface(
                line,
                null,
                new TMP_Text[] { null });

            Assert.That(applied, Is.EqualTo(1));
            Assert.That(line.font, Is.SameAs(body));
        }

        private TMP_Text CreateLabel(string name)
        {
            GameObject target = Track(
                new GameObject(
                    name,
                    typeof(RectTransform),
                    typeof(CanvasRenderer),
                    typeof(TextMeshProUGUI)));
            return target.GetComponent<TMP_Text>();
        }

        private TMP_FontAsset CreateFontAsset()
        {
            Font sourceFont = AssetDatabase.LoadAssetAtPath<Font>(
                "Assets/TextMesh Pro/Fonts/LiberationSans.ttf");
            Assert.That(sourceFont, Is.Not.Null);
            return Track(TMP_FontAsset.CreateFontAsset(sourceFont));
        }

        private void SetFont(string fieldName, TMP_FontAsset value)
        {
            FieldInfo field = typeof(TypographyCatalog).GetField(
                fieldName,
                BindingFlags.Instance | BindingFlags.NonPublic);
            Assert.That(field, Is.Not.Null);
            field.SetValue(catalog, value);
        }

        private T Track<T>(T created)
            where T : Object
        {
            createdObjects.Add(created);
            return created;
        }
    }
}
