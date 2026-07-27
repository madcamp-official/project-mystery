using System.Linq;
using NUnit.Framework;
using Wake.Editor;
using Wake.UI;

namespace Wake.Tests
{
    public sealed class TypographyGlyphPreflightTests
    {
        [Test]
        public void CollectCharacters_RemovesWhitespaceAndControls()
        {
            string result = TypographyGlyphPreflight.CollectCharacters(
                new[] { "가 나\n다\t라" });

            Assert.That(result, Is.EqualTo("가나다라"));
        }

        [Test]
        public void CollectCharacters_DeduplicatesAndSorts()
        {
            string result = TypographyGlyphPreflight.CollectCharacters(
                new[] { "다나가", "나다" });

            Assert.That(result, Is.EqualTo("가나다"));
        }

        [Test]
        public void CollectCharacters_CombinesMultipleSources()
        {
            string result = TypographyGlyphPreflight.CollectCharacters(
                new[] { "증거 C-07", "DAY 1 · AM" });

            Assert.That(result, Does.Contain("증"));
            Assert.That(result, Does.Contain("거"));
            Assert.That(result, Does.Contain("C"));
            Assert.That(result, Does.Contain("7"));
            Assert.That(result, Does.Contain("·"));
        }

        [Test]
        public void CollectCharacters_HandlesNullAndEmptySources()
        {
            Assert.That(
                TypographyGlyphPreflight.CollectCharacters(null),
                Is.Empty);
            Assert.That(
                TypographyGlyphPreflight.CollectCharacters(
                    new string[] { null, string.Empty }),
                Is.Empty);
        }

        [TestCase("Assets/_Project/Data/dialogue.csv", true)]
        [TestCase("Assets/_Project/Scenes/UI.unity", true)]
        [TestCase("Assets/_Project/Code/Label.cs", true)]
        [TestCase("Assets/_Project/Data/Evidence.asset", true)]
        [TestCase("Assets/_Project/Data/config.json", true)]
        [TestCase("Assets/_Project/Docs/note.txt", true)]
        [TestCase("Assets/_Project/Art/icon.png", false)]
        [TestCase("Assets/_Project/Editor/Builder.cs", false)]
        [TestCase("Assets/_Project/Fonts/license.txt", false)]
        [TestCase("Assets/_Project/Tests/Fixture.cs", false)]
        [TestCase("", false)]
        public void IsSupportedSource_UsesExpectedProjectFiles(
            string path,
            bool expected)
        {
            Assert.That(
                TypographyGlyphPreflight.IsSupportedSource(path),
                Is.EqualTo(expected));
        }

        [Test]
        public void RequiredRoles_IncludeAllReleaseUiRoles()
        {
            Assert.That(
                TypographyGlyphPreflight.RequiredRoles,
                Is.EquivalentTo(new[]
                {
                    TypographyRole.Body,
                    TypographyRole.BodyRegular,
                    TypographyRole.Choice,
                    TypographyRole.SpeakerName,
                    TypographyRole.Heading,
                    TypographyRole.HeadingStrong,
                    TypographyRole.Technical,
                    TypographyRole.TechnicalStrong,
                    TypographyRole.Handwritten,
                    TypographyRole.SpecialAlert,
                    TypographyRole.SpecialComic
                }));
        }

        [Test]
        public void RequiredRoles_ContainElevenDistinctRoles()
        {
            Assert.That(
                TypographyGlyphPreflight.RequiredRoles.Count,
                Is.EqualTo(11));
            Assert.That(
                TypographyGlyphPreflight.RequiredRoles,
                Is.Unique);
        }

        [Test]
        public void Validate_ReturnsEmptyForMissingInputs()
        {
            Assert.That(
                TypographyGlyphPreflight.Validate(
                    null,
                    "한글",
                    tryAddCharacters: false),
                Is.Empty);
            Assert.That(
                TypographyGlyphPreflight.Validate(
                    null,
                    string.Empty,
                    tryAddCharacters: false),
                Is.Empty);
        }

        [Test]
        public void Corpus_IsOrdinallySorted()
        {
            string corpus = TypographyGlyphPreflight.CollectCharacters(
                new[] { "Z가A1·" });

            char[] sorted = corpus.OrderBy(character => character).ToArray();
            Assert.That(corpus, Is.EqualTo(new string(sorted)));
        }

        [Test]
        public void CollectCharacters_RemovesCorruptionSentinels()
        {
            string corpus = TypographyGlyphPreflight.CollectCharacters(
                new[] { "정상占쏙옙紐⑺문구" });

            Assert.That(corpus, Does.Contain("정"));
            Assert.That(corpus, Does.Contain("문"));
            Assert.That(corpus, Does.Not.Contain("占"));
            Assert.That(corpus, Does.Not.Contain("紐"));
            Assert.That(corpus, Does.Not.Contain("⑺"));
        }
    }
}
