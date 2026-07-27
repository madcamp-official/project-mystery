using System.Collections.Generic;
using System.Reflection;
using NUnit.Framework;
using TMPro;
using UnityEditor;
using UnityEngine;
using Wake.UI;

namespace Wake.Tests
{
    public sealed class MapTypographyTests
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
        public void ApplyLocation_UsesHeading()
        {
            TMP_Text label = CreateLabel("Location");

            bool applied = MapTypography.ApplyLocation(label);

            Assert.That(applied, Is.True);
            Assert.That(label.font, Is.SameAs(heading));
        }

        [Test]
        public void ApplyCode_UsesTechnicalStrong()
        {
            TMP_Text label = CreateLabel("D7-01");

            bool applied = MapTypography.ApplyCode(label);

            Assert.That(applied, Is.True);
            Assert.That(label.font, Is.SameAs(technicalStrong));
        }

        [Test]
        public void ApplyNotice_UsesRegularBody()
        {
            TMP_Text label = CreateLabel("Notice");

            bool applied = MapTypography.ApplyNotice(label);

            Assert.That(applied, Is.True);
            Assert.That(label.font, Is.SameAs(bodyRegular));
        }

        [Test]
        public void RolesRemainDistinct()
        {
            TMP_Text location = CreateLabel("Location");
            TMP_Text code = CreateLabel("Code");
            TMP_Text notice = CreateLabel("Notice");

            MapTypography.ApplyLocation(location);
            MapTypography.ApplyCode(code);
            MapTypography.ApplyNotice(notice);

            Assert.That(location.font, Is.SameAs(heading));
            Assert.That(code.font, Is.SameAs(technicalStrong));
            Assert.That(notice.font, Is.SameAs(bodyRegular));
            Assert.That(location.font, Is.Not.SameAs(code.font));
            Assert.That(location.font, Is.Not.SameAs(notice.font));
            Assert.That(code.font, Is.Not.SameAs(notice.font));
        }

        [TestCase("location")]
        [TestCase("code")]
        [TestCase("notice")]
        public void ApplyMethods_RejectNull(string role)
        {
            bool applied = role switch
            {
                "location" => MapTypography.ApplyLocation(null),
                "code" => MapTypography.ApplyCode(null),
                _ => MapTypography.ApplyNotice(null)
            };

            Assert.That(applied, Is.False);
        }

        [TestCase("location")]
        [TestCase("code")]
        [TestCase("notice")]
        public void ApplyMethods_ReplaceAuthoredFont(string role)
        {
            TMP_Text label = CreateLabel(role);
            TMP_FontAsset authored = CreateFont();
            label.font = authored;

            switch (role)
            {
                case "location":
                    MapTypography.ApplyLocation(label);
                    Assert.That(label.font, Is.SameAs(heading));
                    break;
                case "code":
                    MapTypography.ApplyCode(label);
                    Assert.That(label.font, Is.SameAs(technicalStrong));
                    break;
                default:
                    MapTypography.ApplyNotice(label);
                    Assert.That(label.font, Is.SameAs(bodyRegular));
                    break;
            }
            Assert.That(label.font, Is.Not.SameAs(authored));
        }

        [Test]
        public void ReapplyingRole_IsStable()
        {
            TMP_Text label = CreateLabel("Location");

            MapTypography.ApplyLocation(label);
            TMP_FontAsset first = label.font;
            bool appliedAgain = MapTypography.ApplyLocation(label);

            Assert.That(appliedAgain, Is.True);
            Assert.That(label.font, Is.SameAs(first));
        }

        [Test]
        public void ApplyObjective_AssignsHeadingProgressAndAccessibility()
        {
            GameObject root = Track(
                new GameObject("Objective", typeof(RectTransform)));
            TMP_Text title = CreateLabel("Title");
            TMP_Text progress = CreateLabel("Progress");
            TMP_Text accessibility = CreateLabel("Accessibility");
            title.transform.SetParent(root.transform, false);
            progress.transform.SetParent(root.transform, false);
            accessibility.transform.SetParent(root.transform, false);

            int applied = MapTypography.ApplyObjective(
                root.transform,
                title,
                progress,
                accessibility);

            Assert.That(applied, Is.EqualTo(3));
            Assert.That(title.font, Is.SameAs(heading));
            Assert.That(progress.font, Is.SameAs(technicalStrong));
            Assert.That(accessibility.font, Is.SameAs(bodyRegular));
        }

        [Test]
        public void ApplyObjective_CoversOtherBodyLabels()
        {
            GameObject root = Track(
                new GameObject("Objective", typeof(RectTransform)));
            TMP_Text secondary = CreateLabel("Secondary");
            secondary.transform.SetParent(root.transform, false);

            int applied = MapTypography.ApplyObjective(
                root.transform,
                null,
                null,
                null);

            Assert.That(applied, Is.EqualTo(1));
            Assert.That(
                secondary.font,
                Is.SameAs(catalog.Resolve(TypographyRole.Body)));
        }

        [Test]
        public void ApplyObjective_CountsLabelsOutsideRoot()
        {
            GameObject root = Track(
                new GameObject("Objective", typeof(RectTransform)));
            TMP_Text title = CreateLabel("Title");
            TMP_Text progress = CreateLabel("Progress");

            int applied = MapTypography.ApplyObjective(
                root.transform,
                title,
                progress,
                null);

            Assert.That(applied, Is.EqualTo(2));
            Assert.That(title.font, Is.SameAs(heading));
            Assert.That(progress.font, Is.SameAs(technicalStrong));
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
