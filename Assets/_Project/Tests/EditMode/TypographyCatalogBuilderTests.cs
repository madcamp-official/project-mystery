using System;
using System.Linq;
using NUnit.Framework;
using UnityEditor;
using UnityEngine;
using Wake.Editor;
using Wake.UI;

namespace Wake.Tests
{
    public sealed class TypographyCatalogBuilderTests
    {
        [Test]
        public void Specifications_CoverEveryTypographyRoleOnce()
        {
            TypographyRole[] expected =
                (TypographyRole[])Enum.GetValues(typeof(TypographyRole));
            TypographyRole[] actual = TypographyCatalogBuilder.Specifications
                .Select(specification => specification.Role)
                .ToArray();

            Assert.That(actual, Is.EquivalentTo(expected));
            Assert.That(actual.Distinct().Count(), Is.EqualTo(actual.Length));
        }

        [Test]
        public void Specifications_UseStaticTrueTypeSources()
        {
            foreach (FontBuildSpec specification in
                     TypographyCatalogBuilder.Specifications)
            {
                Assert.That(
                    specification.SourcePath,
                    Does.EndWith(".ttf"));
                Assert.That(
                    specification.SourcePath,
                    Does.StartWith("Assets/_Project/Fonts/Source/"));
            }
        }

        [Test]
        public void Specifications_ReferenceImportedFontAssets()
        {
            foreach (FontBuildSpec specification in
                     TypographyCatalogBuilder.Specifications)
            {
                Font source = AssetDatabase.LoadAssetAtPath<Font>(
                    specification.SourcePath);
                Assert.That(
                    source,
                    Is.Not.Null,
                    $"Missing source for {specification.Role}");
            }
        }

        [Test]
        public void Specifications_HaveUniqueOutputsAndCatalogProperties()
        {
            var specifications = TypographyCatalogBuilder.Specifications;

            Assert.That(
                specifications.Select(item => item.AssetName).Distinct().Count(),
                Is.EqualTo(specifications.Count));
            Assert.That(
                specifications
                    .Select(item => item.CatalogProperty)
                    .Distinct()
                    .Count(),
                Is.EqualTo(specifications.Count));
        }

        [Test]
        public void Specifications_MarkOnlySpecialRolesOptional()
        {
            TypographyRole[] optional = TypographyCatalogBuilder.Specifications
                .Where(specification => specification.Optional)
                .Select(specification => specification.Role)
                .ToArray();

            Assert.That(
                optional,
                Is.EquivalentTo(new[]
                {
                    TypographyRole.Handwritten,
                    TypographyRole.SpecialAlert,
                    TypographyRole.SpecialComic
                }));
        }

        [TestCase("Pretendard-OFL.txt")]
        [TestCase("SUITE-OFL.txt")]
        [TestCase("IBMPlexMono-OFL.txt")]
        [TestCase("GowunDodum-OFL.txt")]
        [TestCase("BlackHanSans-OFL.txt")]
        [TestCase("Jua-OFL.txt")]
        public void LicenseFile_IsIncluded(string fileName)
        {
            UnityEngine.Object license = AssetDatabase.LoadMainAssetAtPath(
                $"Assets/_Project/Fonts/Licenses/{fileName}");

            Assert.That(license, Is.Not.Null);
        }
    }
}
