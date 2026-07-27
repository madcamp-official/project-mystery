using NUnit.Framework;
using TMPro;
using UnityEditor;
using UnityEngine;
using Wake.UI;

namespace Wake.Tests
{
    public sealed class TypographyCatalogTests
    {
        private TypographyCatalog catalog;
        private TMP_FontAsset body;
        private TMP_FontAsset choice;
        private TMP_FontAsset heading;

        [SetUp]
        public void SetUp()
        {
            catalog = ScriptableObject.CreateInstance<TypographyCatalog>();
            Font sourceFont = AssetDatabase.LoadAssetAtPath<Font>(
                "Assets/TextMesh Pro/Fonts/LiberationSans.ttf");
            Assert.That(sourceFont, Is.Not.Null);
            body = TMP_FontAsset.CreateFontAsset(sourceFont);
            choice = TMP_FontAsset.CreateFontAsset(sourceFont);
            heading = TMP_FontAsset.CreateFontAsset(sourceFont);

            SerializedObject serialized = new(catalog);
            serialized.FindProperty("body").objectReferenceValue = body;
            serialized.FindProperty("choice").objectReferenceValue = choice;
            serialized.FindProperty("heading").objectReferenceValue = heading;
            serialized.ApplyModifiedPropertiesWithoutUndo();

            TypographyService.SetCatalogForTests(catalog);
        }

        [TearDown]
        public void TearDown()
        {
            TypographyService.SetCatalogForTests(null);
            Object.DestroyImmediate(catalog);
            Object.DestroyImmediate(body);
            Object.DestroyImmediate(choice);
            Object.DestroyImmediate(heading);
        }

        [Test]
        public void Resolve_ReturnsConfiguredRoleFont()
        {
            Assert.That(
                catalog.Resolve(TypographyRole.Body),
                Is.SameAs(body));
            Assert.That(
                catalog.Resolve(TypographyRole.Choice),
                Is.SameAs(choice));
            Assert.That(
                catalog.Resolve(TypographyRole.Heading),
                Is.SameAs(heading));
        }

        [Test]
        public void Resolve_MissingRoleFallsBackToBody()
        {
            Assert.That(
                catalog.Resolve(TypographyRole.Technical),
                Is.SameAs(body));
            Assert.That(
                catalog.Resolve(TypographyRole.SpecialComic),
                Is.SameAs(body));
        }

        [Test]
        public void MissingRoles_ExcludesOptionalSpecialFontsByDefault()
        {
            var missing = catalog.GetMissingRoles();

            Assert.That(missing, Does.Contain(TypographyRole.BodyRegular));
            Assert.That(missing, Does.Contain(TypographyRole.SpeakerName));
            Assert.That(missing, Does.Contain(TypographyRole.Technical));
            Assert.That(missing, Has.No.Member(TypographyRole.Handwritten));
            Assert.That(missing, Has.No.Member(TypographyRole.SpecialAlert));
            Assert.That(missing, Has.No.Member(TypographyRole.SpecialComic));
        }

        [Test]
        public void MissingRoles_IncludesSpecialFontsWhenRequested()
        {
            var missing = catalog.GetMissingRoles(includeSpecialRoles: true);

            Assert.That(missing, Does.Contain(TypographyRole.Handwritten));
            Assert.That(missing, Does.Contain(TypographyRole.SpecialAlert));
            Assert.That(missing, Does.Contain(TypographyRole.SpecialComic));
        }

        [Test]
        public void Service_ResolvesThroughConfiguredCatalog()
        {
            Assert.That(
                TypographyService.Resolve(TypographyRole.Choice),
                Is.SameAs(choice));
        }

        [Test]
        public void Apply_AssignsResolvedFont()
        {
            GameObject target = new(
                "Typography Test",
                typeof(RectTransform),
                typeof(CanvasRenderer),
                typeof(TextMeshProUGUI));
            try
            {
                TMP_Text label = target.GetComponent<TMP_Text>();

                bool applied = TypographyService.Apply(
                    label,
                    TypographyRole.Heading);

                Assert.That(applied, Is.True);
                Assert.That(label.font, Is.SameAs(heading));
            }
            finally
            {
                Object.DestroyImmediate(target);
            }
        }

        [Test]
        public void Apply_NullTextIsRejected()
        {
            Assert.That(
                TypographyService.Apply(null, TypographyRole.Body),
                Is.False);
        }

        [Test]
        public void ApplyRecursively_IncludesInactiveChildren()
        {
            GameObject root = new("Typography Root", typeof(RectTransform));
            GameObject active = CreateLabel("Active", root.transform);
            GameObject inactive = CreateLabel("Inactive", root.transform);
            inactive.SetActive(false);

            try
            {
                int count = TypographyService.ApplyRecursively(
                    root.transform,
                    TypographyRole.Choice);

                Assert.That(count, Is.EqualTo(2));
                Assert.That(
                    active.GetComponent<TMP_Text>().font,
                    Is.SameAs(choice));
                Assert.That(
                    inactive.GetComponent<TMP_Text>().font,
                    Is.SameAs(choice));
            }
            finally
            {
                Object.DestroyImmediate(root);
            }
        }

        private static GameObject CreateLabel(
            string name,
            Transform parent)
        {
            GameObject target = new(
                name,
                typeof(RectTransform),
                typeof(CanvasRenderer),
                typeof(TextMeshProUGUI));
            target.transform.SetParent(parent, false);
            return target;
        }
    }
}
