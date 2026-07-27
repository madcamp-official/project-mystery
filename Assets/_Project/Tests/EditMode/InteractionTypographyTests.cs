using System.Collections.Generic;
using System.Reflection;
using NUnit.Framework;
using TMPro;
using UnityEditor;
using UnityEngine;
using Wake.UI;

namespace Wake.Tests
{
    public sealed class InteractionTypographyTests
    {
        private readonly List<Object> created = new();
        private TypographyCatalog catalog;
        private TMP_FontAsset body;
        private TMP_FontAsset bodyRegular;
        private TMP_FontAsset heading;
        private TMP_FontAsset technicalStrong;
        private TMP_FontAsset specialAlert;

        [SetUp]
        public void SetUp()
        {
            catalog = Track(
                ScriptableObject.CreateInstance<TypographyCatalog>());
            body = CreateFont();
            bodyRegular = CreateFont();
            heading = CreateFont();
            technicalStrong = CreateFont();
            specialAlert = CreateFont();
            SetFont("body", body);
            SetFont("bodyRegular", bodyRegular);
            SetFont("heading", heading);
            SetFont("technicalStrong", technicalStrong);
            SetFont("specialAlert", specialAlert);
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
        public void Apply_AssignsNamedInteractionRoles()
        {
            GameObject root = CreateRoot();
            TMP_Text technical = CreateLabel("Progress", root.transform);
            TMP_Text hint = CreateLabel("Hint", root.transform);
            TMP_Text status = CreateLabel("Status", root.transform);

            int count = InteractionTypography.Apply(
                root.transform,
                technical,
                hint,
                status);

            Assert.That(count, Is.EqualTo(3));
            Assert.That(technical.font, Is.SameAs(technicalStrong));
            Assert.That(hint.font, Is.SameAs(bodyRegular));
            Assert.That(status.font, Is.SameAs(heading));
        }

        [Test]
        public void Apply_UsesBodyForOtherLabels()
        {
            GameObject root = CreateRoot();
            TMP_Text action = CreateLabel("Action", root.transform);
            TMP_Text description = CreateLabel(
                "Description",
                root.transform);

            int count = InteractionTypography.Apply(
                root.transform,
                null,
                null,
                null);

            Assert.That(count, Is.EqualTo(2));
            Assert.That(action.font, Is.SameAs(body));
            Assert.That(description.font, Is.SameAs(body));
        }

        [Test]
        public void Apply_IncludesInactiveLabels()
        {
            GameObject root = CreateRoot();
            TMP_Text label = CreateLabel("Inactive", root.transform);
            label.gameObject.SetActive(false);

            InteractionTypography.Apply(
                root.transform,
                null,
                null,
                null);

            Assert.That(label.font, Is.SameAs(body));
        }

        [Test]
        public void Apply_ReturnsZeroForMissingSurface()
        {
            Assert.That(
                InteractionTypography.Apply(null, null, null, null),
                Is.Zero);
        }

        [Test]
        public void Apply_CountsNamedLabelsOutsideRoot()
        {
            GameObject root = CreateRoot();
            TMP_Text technical = CreateLabel("Progress", null);
            TMP_Text status = CreateLabel("Status", null);

            int count = InteractionTypography.Apply(
                root.transform,
                technical,
                null,
                status);

            Assert.That(count, Is.EqualTo(2));
            Assert.That(technical.font, Is.SameAs(technicalStrong));
            Assert.That(status.font, Is.SameAs(heading));
        }

        [Test]
        public void NamedRolesRemainDistinct()
        {
            GameObject root = CreateRoot();
            TMP_Text technical = CreateLabel("Progress", root.transform);
            TMP_Text hint = CreateLabel("Hint", root.transform);
            TMP_Text status = CreateLabel("Status", root.transform);

            InteractionTypography.Apply(
                root.transform,
                technical,
                hint,
                status);

            Assert.That(technical.font, Is.Not.SameAs(hint.font));
            Assert.That(technical.font, Is.Not.SameAs(status.font));
            Assert.That(hint.font, Is.Not.SameAs(status.font));
        }

        [Test]
        public void ApplyUrgentAlert_UsesSpecialAlert()
        {
            TMP_Text alert = CreateLabel("Urgent Alert", null);

            bool applied =
                InteractionTypography.ApplyUrgentAlert(alert);

            Assert.That(applied, Is.True);
            Assert.That(alert.font, Is.SameAs(specialAlert));
        }

        [Test]
        public void ApplyUrgentAlert_RejectsNull()
        {
            Assert.That(
                InteractionTypography.ApplyUrgentAlert(null),
                Is.False);
        }

        [Test]
        public void Apply_ReplacesAuthoredFonts()
        {
            GameObject root = CreateRoot();
            TMP_Text status = CreateLabel("Status", root.transform);
            TMP_FontAsset authored = CreateFont();
            status.font = authored;

            InteractionTypography.Apply(
                root.transform,
                null,
                null,
                status);

            Assert.That(status.font, Is.SameAs(heading));
            Assert.That(status.font, Is.Not.SameAs(authored));
        }

        private GameObject CreateRoot()
        {
            return Track(
                new GameObject("Interaction", typeof(RectTransform)));
        }

        private TMP_Text CreateLabel(string name, Transform parent)
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
