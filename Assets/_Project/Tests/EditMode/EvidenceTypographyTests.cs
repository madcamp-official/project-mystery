using System.Collections.Generic;
using System.Reflection;
using NUnit.Framework;
using TMPro;
using UnityEditor;
using UnityEngine;
using Wake.Evidence;
using Wake.UI;

namespace Wake.Tests
{
    public sealed class EvidenceTypographyTests
    {
        private readonly List<Object> created = new();
        private TypographyCatalog catalog;
        private TMP_FontAsset bodyRegular;
        private TMP_FontAsset heading;
        private TMP_FontAsset technicalStrong;

        [SetUp]
        public void SetUp()
        {
            catalog = Track(
                ScriptableObject.CreateInstance<TypographyCatalog>());
            bodyRegular = CreateFont();
            heading = CreateFont();
            technicalStrong = CreateFont();
            SetFont("body", bodyRegular);
            SetFont("bodyRegular", bodyRegular);
            SetFont("heading", heading);
            SetFont("technicalStrong", technicalStrong);
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

        [Test]
        public void ApplySurface_UsesHeadingForEvidenceTitle()
        {
            GameObject root = CreateRoot();
            TMP_Text title = CreateLabel("Title", root.transform);
            TMP_Text detail = CreateLabel("Detail", root.transform);

            EvidenceTypography.ApplySurface(root.transform, title, detail, null);

            Assert.That(title.font, Is.SameAs(heading));
        }

        [Test]
        public void ApplySurface_UsesRegularBodyForDescription()
        {
            GameObject root = CreateRoot();
            TMP_Text title = CreateLabel("Title", root.transform);
            TMP_Text detail = CreateLabel("Detail", root.transform);

            EvidenceTypography.ApplySurface(root.transform, title, detail, null);

            Assert.That(detail.font, Is.SameAs(bodyRegular));
        }

        [Test]
        public void ApplySurface_UsesHeadingForTheoryBoardAction()
        {
            GameObject root = CreateRoot();
            TMP_Text theory = CreateLabel("Theory", root.transform);

            EvidenceTypography.ApplySurface(
                root.transform,
                null,
                null,
                theory);

            Assert.That(theory.font, Is.SameAs(heading));
        }

        [Test]
        public void ApplySurface_CoversAuthoredNavigationLabels()
        {
            GameObject root = CreateRoot();
            TMP_Text next = CreateLabel("Next", root.transform);
            TMP_Text previous = CreateLabel("Previous", root.transform);
            TMP_Text back = CreateLabel("Back", root.transform);

            int applied = EvidenceTypography.ApplySurface(
                root.transform,
                null,
                null,
                null);

            Assert.That(applied, Is.EqualTo(3));
            Assert.That(next.font, Is.SameAs(bodyRegular));
            Assert.That(previous.font, Is.SameAs(bodyRegular));
            Assert.That(back.font, Is.SameAs(bodyRegular));
        }

        [Test]
        public void ApplySurface_IncludesInactiveLabels()
        {
            GameObject root = CreateRoot();
            TMP_Text inactive = CreateLabel("Inactive", root.transform);
            inactive.gameObject.SetActive(false);

            int applied = EvidenceTypography.ApplySurface(
                root.transform,
                null,
                null,
                null);

            Assert.That(applied, Is.EqualTo(1));
            Assert.That(inactive.font, Is.SameAs(bodyRegular));
        }

        [Test]
        public void ApplySurface_ReturnsZeroForMissingRoot()
        {
            int applied = EvidenceTypography.ApplySurface(
                null,
                null,
                null,
                null);

            Assert.That(applied, Is.Zero);
        }

        [Test]
        public void ApplySurface_CountsExplicitLabelsOutsideRoot()
        {
            GameObject root = CreateRoot();
            TMP_Text title = CreateLabel("Title", null);
            TMP_Text detail = CreateLabel("Detail", null);

            int applied = EvidenceTypography.ApplySurface(
                root.transform,
                title,
                detail,
                null);

            Assert.That(applied, Is.EqualTo(2));
            Assert.That(title.font, Is.SameAs(heading));
            Assert.That(detail.font, Is.SameAs(bodyRegular));
        }

        [Test]
        public void ApplyCarouselLabel_UsesTechnicalStrongFont()
        {
            GameObject root = CreateRoot();
            TMP_Text label = CreateLabel("C-07", root.transform);

            bool applied = EvidenceTypography.ApplyCarouselLabel(label);

            Assert.That(applied, Is.True);
            Assert.That(label.font, Is.SameAs(technicalStrong));
        }

        [Test]
        public void ApplyCarouselLabel_RejectsNull()
        {
            Assert.That(
                EvidenceTypography.ApplyCarouselLabel(null),
                Is.False);
        }

        [Test]
        public void ApplySurface_ReplacesSceneAuthoredFonts()
        {
            GameObject root = CreateRoot();
            TMP_Text title = CreateLabel("Title", root.transform);
            TMP_Text detail = CreateLabel("Detail", root.transform);
            TMP_FontAsset authored = CreateFont();
            title.font = authored;
            detail.font = authored;

            EvidenceTypography.ApplySurface(
                root.transform,
                title,
                detail,
                null);

            Assert.That(title.font, Is.SameAs(heading));
            Assert.That(detail.font, Is.SameAs(bodyRegular));
            Assert.That(title.font, Is.Not.SameAs(authored));
            Assert.That(detail.font, Is.Not.SameAs(authored));
        }

        [Test]
        public void EvidenceRoles_RemainDistinct()
        {
            GameObject root = CreateRoot();
            TMP_Text title = CreateLabel("Title", root.transform);
            TMP_Text detail = CreateLabel("Detail", root.transform);
            TMP_Text code = CreateLabel("C-07", root.transform);

            EvidenceTypography.ApplySurface(
                root.transform,
                title,
                detail,
                null);
            EvidenceTypography.ApplyCarouselLabel(code);

            Assert.That(title.font, Is.SameAs(heading));
            Assert.That(detail.font, Is.SameAs(bodyRegular));
            Assert.That(code.font, Is.SameAs(technicalStrong));
            Assert.That(title.font, Is.Not.SameAs(detail.font));
            Assert.That(code.font, Is.Not.SameAs(detail.font));
            Assert.That(code.font, Is.Not.SameAs(title.font));
        }

        private GameObject CreateRoot()
        {
            return Track(new GameObject("Evidence", typeof(RectTransform)));
        }

        private TMP_Text CreateLabel(string name, Transform parent)
        {
            GameObject target = Track(
                new GameObject(
                    name,
                    typeof(RectTransform),
                    typeof(CanvasRenderer),
                    typeof(TextMeshProUGUI)));
            target.transform.SetParent(parent, false);
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
