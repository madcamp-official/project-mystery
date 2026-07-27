using System.Collections.Generic;
using System.Reflection;
using NUnit.Framework;
using TMPro;
using UnityEditor;
using UnityEngine;
using Wake.Evidence;
using Wake.Narrative;
using Wake.UI;

namespace Wake.Tests
{
    public sealed class SpecialTypographyPolicyTests
    {
        private readonly List<Object> created = new();
        private TypographyCatalog catalog;
        private TMP_FontAsset body;
        private TMP_FontAsset bodyRegular;
        private TMP_FontAsset choice;
        private TMP_FontAsset handwritten;
        private TMP_FontAsset specialAlert;
        private TMP_FontAsset specialComic;

        [SetUp]
        public void SetUp()
        {
            catalog = Track(
                ScriptableObject.CreateInstance<TypographyCatalog>());
            body = CreateFont();
            bodyRegular = CreateFont();
            choice = CreateFont();
            handwritten = CreateFont();
            specialAlert = CreateFont();
            specialComic = CreateFont();
            SetFont("body", body);
            SetFont("bodyRegular", bodyRegular);
            SetFont("choice", choice);
            SetFont("handwritten", handwritten);
            SetFont("specialAlert", specialAlert);
            SetFont("specialComic", specialComic);
            TypographyService.SetCatalogForTests(catalog);
        }

        [TearDown]
        public void TearDown()
        {
            TypographyService.SetCatalogForTests(null);
            foreach (Object item in created)
            {
                if (item != null)
                {
                    Object.DestroyImmediate(item);
                }
            }
            created.Clear();
        }

        [TestCase("invitation")]
        [TestCase("INVITATION")]
        public void InvitationEvidence_UsesHandwrittenRole(
            string category)
        {
            Assert.That(
                EvidenceTypography.ResolveDetailRole(category),
                Is.EqualTo(TypographyRole.Handwritten));
        }

        [TestCase(null)]
        [TestCase("")]
        [TestCase("forensic")]
        [TestCase("communication")]
        public void OtherEvidence_UsesRegularBodyRole(
            string category)
        {
            Assert.That(
                EvidenceTypography.ResolveDetailRole(category),
                Is.EqualTo(TypographyRole.BodyRegular));
        }

        [Test]
        public void InvitationEvidence_AppliesHandwrittenFont()
        {
            TMP_Text label = CreateLabel("Invitation");

            bool applied = EvidenceTypography.ApplyDetail(
                label,
                "invitation");

            Assert.That(applied, Is.True);
            Assert.That(label.font, Is.SameAs(handwritten));
        }

        [Test]
        public void NormalEvidence_AppliesRegularBodyFont()
        {
            TMP_Text label = CreateLabel("Forensic");

            EvidenceTypography.ApplyDetail(label, "forensic");

            Assert.That(label.font, Is.SameAs(bodyRegular));
        }

        [TestCase("농담으로 넘기기")]
        [TestCase("그건 농담이었어요")]
        public void ComicChoice_UsesSpecialComicRole(string content)
        {
            Assert.That(
                DialogueTypography.ResolveChoiceRole(content),
                Is.EqualTo(TypographyRole.SpecialComic));
        }

        [TestCase(null)]
        [TestCase("")]
        [TestCase("경고를 진지하게 듣기")]
        public void NormalChoice_UsesChoiceRole(string content)
        {
            Assert.That(
                DialogueTypography.ResolveChoiceRole(content),
                Is.EqualTo(TypographyRole.Choice));
        }

        [Test]
        public void ComicChoice_AppliesJuaFont()
        {
            TMP_Text label = CreateLabel("Comic");

            DialogueTypography.ApplyChoice(
                label,
                "농담으로 넘기기");

            Assert.That(label.font, Is.SameAs(specialComic));
        }

        [Test]
        public void ReusedChoice_ReturnsToNormalFont()
        {
            TMP_Text label = CreateLabel("Reused");
            DialogueTypography.ApplyChoice(
                label,
                "농담으로 넘기기");

            DialogueTypography.ApplyChoice(
                label,
                "경고를 진지하게 듣기");

            Assert.That(label.font, Is.SameAs(choice));
        }

        [Test]
        public void ComicChoice_AppliesSpecialComicFont()
        {
            TMP_Text label = CreateLabel("ComicChoice");

            bool applied = DialogueTypography.ApplyChoice(
                label,
                "농담으로 받아치기");

            Assert.That(applied, Is.True);
            Assert.That(label.font, Is.SameAs(specialComic));
        }

        [Test]
        public void EmptyChoice_AppliesDefaultChoiceFont()
        {
            TMP_Text label = CreateLabel("EmptyChoice");

            bool applied = DialogueTypography.ApplyChoice(
                label,
                string.Empty);

            Assert.That(applied, Is.True);
            Assert.That(label.font, Is.SameAs(choice));
        }

        [Test]
        public void InvitationComparison_IgnoresCaseOnly()
        {
            Assert.That(
                EvidenceTypography.ResolveDetailRole(" invitation "),
                Is.EqualTo(TypographyRole.BodyRegular));
        }

        [Test]
        public void AlertToast_UsesSpecialAlertRole()
        {
            Assert.That(
                ToastController.ResolveRole(
                    ToastTypographyStyle.Alert),
                Is.EqualTo(TypographyRole.SpecialAlert));
        }

        [Test]
        public void NormalToast_UsesBodyRole()
        {
            Assert.That(
                ToastController.ResolveRole(
                    ToastTypographyStyle.Normal),
                Is.EqualTo(TypographyRole.Body));
        }

        [Test]
        public void SpecialRolesRemainMutuallyDistinct()
        {
            Assert.That(handwritten, Is.Not.SameAs(specialAlert));
            Assert.That(handwritten, Is.Not.SameAs(specialComic));
            Assert.That(specialAlert, Is.Not.SameAs(specialComic));
            Assert.That(body, Is.Not.SameAs(handwritten));
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

        private TMP_FontAsset CreateFont()
        {
            Font source = AssetDatabase.LoadAssetAtPath<Font>(
                "Assets/TextMesh Pro/Fonts/LiberationSans.ttf");
            Assert.That(source, Is.Not.Null);
            return Track(TMP_FontAsset.CreateFontAsset(source));
        }

        private void SetFont(string fieldName, TMP_FontAsset value)
        {
            FieldInfo field = typeof(TypographyCatalog).GetField(
                fieldName,
                BindingFlags.Instance | BindingFlags.NonPublic);
            Assert.That(field, Is.Not.Null);
            field.SetValue(catalog, value);
        }

        private T Track<T>(T item)
            where T : Object
        {
            created.Add(item);
            return item;
        }
    }
}
