using System.Collections.Generic;
using System.Reflection;
using NUnit.Framework;
using TMPro;
using UnityEditor;
using UnityEngine;
using Wake.UI;

namespace Wake.Tests
{
    public sealed class StatusHUDTypographyTests
    {
        private readonly List<Object> created = new();
        private TypographyCatalog catalog;
        private TMP_FontAsset body;
        private TMP_FontAsset technical;

        [SetUp]
        public void SetUp()
        {
            catalog = Track(
                ScriptableObject.CreateInstance<TypographyCatalog>());
            body = CreateFont();
            technical = CreateFont();
            SetFont("body", body);
            SetFont("technical", technical);
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
        public void Apply_UsesTechnicalFontForTime()
        {
            TMP_Text time = CreateLabel("Time");

            StatusHUDTypography.Apply(
                time, null, null, null, null, null);

            Assert.That(time.font, Is.SameAs(technical));
        }

        [Test]
        public void Apply_UsesBodyFontForStatusLabels()
        {
            TMP_Text anxiety = CreateLabel("Anxiety");
            TMP_Text integrity = CreateLabel("Integrity");
            TMP_Text progress = CreateLabel("Progress");

            StatusHUDTypography.Apply(
                null, anxiety, integrity, progress, null, null);

            Assert.That(anxiety.font, Is.SameAs(body));
            Assert.That(integrity.font, Is.SameAs(body));
            Assert.That(progress.font, Is.SameAs(body));
        }

        [Test]
        public void Apply_UsesBodyFontForTrustLabel()
        {
            TMP_Text trust = CreateLabel("Trust");

            StatusHUDTypography.Apply(
                null, null, null, null, trust, null);

            Assert.That(trust.font, Is.SameAs(body));
        }

        [Test]
        public void Apply_CoversTrustRootChildren()
        {
            GameObject root = CreateRoot("Trust Root");
            TMP_Text label = CreateLabel("Trust", root.transform);
            TMP_Text detail = CreateLabel("Detail", root.transform);

            int applied = StatusHUDTypography.Apply(
                null, null, null, null, label, root.transform);

            Assert.That(applied, Is.EqualTo(2));
            Assert.That(label.font, Is.SameAs(body));
            Assert.That(detail.font, Is.SameAs(body));
        }

        [Test]
        public void Apply_IncludesInactiveTrustChildren()
        {
            GameObject root = CreateRoot("Trust Root");
            TMP_Text label = CreateLabel("Trust", root.transform);
            label.gameObject.SetActive(false);

            int applied = StatusHUDTypography.Apply(
                null, null, null, null, label, root.transform);

            Assert.That(applied, Is.EqualTo(1));
            Assert.That(label.font, Is.SameAs(body));
        }

        [Test]
        public void Apply_CountsTrustOutsideRoot()
        {
            GameObject root = CreateRoot("Trust Root");
            TMP_Text trust = CreateLabel("Trust");

            int applied = StatusHUDTypography.Apply(
                null, null, null, null, trust, root.transform);

            Assert.That(applied, Is.EqualTo(1));
            Assert.That(trust.font, Is.SameAs(body));
        }

        [Test]
        public void Apply_ReturnsNumberOfAssignedLabels()
        {
            TMP_Text time = CreateLabel("Time");
            TMP_Text anxiety = CreateLabel("Anxiety");
            TMP_Text integrity = CreateLabel("Integrity");
            TMP_Text progress = CreateLabel("Progress");
            TMP_Text trust = CreateLabel("Trust");

            int applied = StatusHUDTypography.Apply(
                time, anxiety, integrity, progress, trust, null);

            Assert.That(applied, Is.EqualTo(5));
        }

        [Test]
        public void Apply_ReturnsZeroForMissingLabels()
        {
            int applied = StatusHUDTypography.Apply(
                null, null, null, null, null, null);

            Assert.That(applied, Is.Zero);
        }

        [Test]
        public void Apply_ReplacesAuthoredFonts()
        {
            TMP_Text time = CreateLabel("Time");
            TMP_Text progress = CreateLabel("Progress");
            TMP_FontAsset authored = CreateFont();
            time.font = authored;
            progress.font = authored;

            StatusHUDTypography.Apply(
                time, null, null, progress, null, null);

            Assert.That(time.font, Is.SameAs(technical));
            Assert.That(progress.font, Is.SameAs(body));
            Assert.That(time.font, Is.Not.SameAs(authored));
            Assert.That(progress.font, Is.Not.SameAs(authored));
        }

        [Test]
        public void RuntimeKoreanFont_ResolvesBodyCompatibilityRole()
        {
            Assert.That(
                StatusHUDController.RuntimeKoreanFont,
                Is.SameAs(body));
        }

        [Test]
        public void TimeAndStatusRoles_RemainDistinct()
        {
            TMP_Text time = CreateLabel("Time");
            TMP_Text anxiety = CreateLabel("Anxiety");

            StatusHUDTypography.Apply(
                time, anxiety, null, null, null, null);

            Assert.That(time.font, Is.SameAs(technical));
            Assert.That(anxiety.font, Is.SameAs(body));
            Assert.That(time.font, Is.Not.SameAs(anxiety.font));
        }

        private GameObject CreateRoot(string name)
        {
            return Track(new GameObject(name, typeof(RectTransform)));
        }

        private TMP_Text CreateLabel(
            string name,
            Transform parent = null)
        {
            GameObject target = Track(
                new GameObject(
                    name,
                    typeof(RectTransform),
                    typeof(CanvasRenderer),
                    typeof(TextMeshProUGUI)));
            if (parent != null)
            {
                target.transform.SetParent(parent, false);
            }
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
