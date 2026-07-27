using System.Collections.Generic;
using System.Reflection;
using NUnit.Framework;
using TMPro;
using UnityEditor;
using UnityEditor.Build;
using UnityEngine;
using Wake.Editor;
using Wake.UI;

namespace Wake.Tests
{
    public sealed class TypographyReleaseBuildGateTests
    {
        private readonly List<Object> created = new();
        private TypographyCatalog catalog;
        private TMP_FontAsset font;

        [SetUp]
        public void SetUp()
        {
            catalog = Track(
                ScriptableObject.CreateInstance<TypographyCatalog>());
            font = CreateFont();
            AssignEveryRole(font);
        }

        [TearDown]
        public void TearDown()
        {
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
        public void CompleteCatalogAndMatchingDefault_HasNoErrors()
        {
            IReadOnlyList<string> errors =
                TypographyReleasePreflight.Validate(
                    catalog,
                    font,
                    string.Empty);

            Assert.That(errors, Is.Empty);
        }

        [Test]
        public void MissingCatalog_StopsFurtherValidation()
        {
            IReadOnlyList<string> errors =
                TypographyReleasePreflight.Validate(
                    null,
                    font,
                    "한");

            Assert.That(errors, Has.Count.EqualTo(1));
            Assert.That(
                errors[0],
                Does.StartWith(
                    TypographyReleasePreflight.MissingCatalogCode));
        }

        [Test]
        public void MissingCoreRole_IsReported()
        {
            SetFont("choice", null);

            IReadOnlyList<string> errors =
                TypographyReleasePreflight.Validate(
                    catalog,
                    font,
                    string.Empty);

            Assert.That(
                errors,
                Has.Some.StartsWith(
                    TypographyReleasePreflight.MissingRoleCode));
            Assert.That(
                string.Join("\n", errors),
                Does.Contain(nameof(TypographyRole.Choice)));
        }

        [TestCase("handwritten", TypographyRole.Handwritten)]
        [TestCase("specialAlert", TypographyRole.SpecialAlert)]
        [TestCase("specialComic", TypographyRole.SpecialComic)]
        public void MissingSpecialRole_IsReported(
            string fieldName,
            TypographyRole role)
        {
            SetFont(fieldName, null);

            IReadOnlyList<string> errors =
                TypographyReleasePreflight.Validate(
                    catalog,
                    font,
                    string.Empty);

            Assert.That(
                string.Join("\n", errors),
                Does.Contain(role.ToString()));
        }

        [Test]
        public void DifferentTmpDefault_IsReported()
        {
            TMP_FontAsset other = CreateFont();

            IReadOnlyList<string> errors =
                TypographyReleasePreflight.Validate(
                    catalog,
                    other,
                    string.Empty);

            Assert.That(
                errors,
                Has.Some.StartsWith(
                    TypographyReleasePreflight.DefaultFontCode));
        }

        [Test]
        public void NullTmpDefault_IsReported()
        {
            IReadOnlyList<string> errors =
                TypographyReleasePreflight.Validate(
                    catalog,
                    null,
                    string.Empty);

            Assert.That(
                errors,
                Has.Some.StartsWith(
                    TypographyReleasePreflight.DefaultFontCode));
        }

        [Test]
        public void MissingBody_ReportsRoleAndDefaultErrors()
        {
            SetFont("body", null);

            IReadOnlyList<string> errors =
                TypographyReleasePreflight.Validate(
                    catalog,
                    font,
                    string.Empty);

            Assert.That(
                errors,
                Has.Some.StartsWith(
                    TypographyReleasePreflight.MissingRoleCode));
            Assert.That(
                errors,
                Has.Some.StartsWith(
                    TypographyReleasePreflight.DefaultFontCode));
        }

        [Test]
        public void MissingGlyph_IsReportedWithoutDynamicAddition()
        {
            const string unsupported = "\u0378";
            Assert.That(
                font.HasCharacter(unsupported[0]),
                Is.False,
                "테스트 폰트가 예약 코드 포인트를 포함합니다.");

            IReadOnlyList<string> errors =
                TypographyReleasePreflight.Validate(
                    catalog,
                    font,
                    unsupported);

            Assert.That(
                errors,
                Has.Some.StartsWith(
                    TypographyReleasePreflight.MissingGlyphCode));
            Assert.That(
                string.Join("\n", errors),
                Does.Contain("U+0378"));
        }

        [Test]
        public void EmptyCorpus_DoesNotCreateGlyphError()
        {
            IReadOnlyList<string> errors =
                TypographyReleasePreflight.Validate(
                    catalog,
                    font,
                    string.Empty);

            Assert.That(
                errors,
                Has.None.StartsWith(
                    TypographyReleasePreflight.MissingGlyphCode));
        }

        [Test]
        public void ThrowIfInvalid_AllowsEmptyErrors()
        {
            Assert.DoesNotThrow(() =>
                TypographyReleasePreflight.ThrowIfInvalid(
                    System.Array.Empty<string>()));
        }

        [Test]
        public void ThrowIfInvalid_AllowsNullErrors()
        {
            Assert.DoesNotThrow(() =>
                TypographyReleasePreflight.ThrowIfInvalid(null));
        }

        [Test]
        public void ThrowIfInvalid_UsesBuildFailedException()
        {
            BuildFailedException exception = Assert.Throws<
                BuildFailedException>(() =>
                TypographyReleasePreflight.ThrowIfInvalid(new[]
                {
                    "TYPOGRAPHY_ROLE_MISSING: Choice",
                    "TMP_DEFAULT_FONT_MISMATCH: Body"
                }));

            Assert.That(
                exception.Message,
                Does.Contain("TYPOGRAPHY_ROLE_MISSING"));
            Assert.That(
                exception.Message,
                Does.Contain("TMP_DEFAULT_FONT_MISMATCH"));
        }

        [Test]
        public void RequiredGlyphRoles_IncludeAllSpecialRoles()
        {
            Assert.That(
                TypographyGlyphPreflight.RequiredRoles,
                Does.Contain(TypographyRole.Handwritten));
            Assert.That(
                TypographyGlyphPreflight.RequiredRoles,
                Does.Contain(TypographyRole.SpecialAlert));
            Assert.That(
                TypographyGlyphPreflight.RequiredRoles,
                Does.Contain(TypographyRole.SpecialComic));
        }

        [Test]
        public void BuildGate_RunsAfterProductionContentGate()
        {
            var gate = new TypographyReleaseBuildGate();

            Assert.That(gate.callbackOrder, Is.GreaterThan(0));
        }

        private TMP_FontAsset CreateFont()
        {
            Font source = AssetDatabase.LoadAssetAtPath<Font>(
                "Assets/TextMesh Pro/Fonts/LiberationSans.ttf");
            Assert.That(source, Is.Not.Null);
            return Track(TMP_FontAsset.CreateFontAsset(source));
        }

        private void AssignEveryRole(TMP_FontAsset value)
        {
            SetFont("body", value);
            SetFont("bodyRegular", value);
            SetFont("choice", value);
            SetFont("speakerName", value);
            SetFont("heading", value);
            SetFont("headingStrong", value);
            SetFont("technical", value);
            SetFont("technicalStrong", value);
            SetFont("handwritten", value);
            SetFont("specialAlert", value);
            SetFont("specialComic", value);
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
