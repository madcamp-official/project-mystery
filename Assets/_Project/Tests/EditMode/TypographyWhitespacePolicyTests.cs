using System.Collections.Generic;
using NUnit.Framework;
using TMPro;
using UnityEngine;
using Wake.UI;

namespace Wake.Tests
{
    public sealed class TypographyWhitespacePolicyTests
    {
        [Test]
        public void RequiredCharacters_ContainsStandardSpaceOnce()
        {
            Assert.That(
                TypographyWhitespacePolicy.RequiredCharacters,
                Is.EqualTo(" "));
            Assert.That(
                TypographyWhitespacePolicy.RequiredCharacters.Length,
                Is.EqualTo(1));
        }

        [Test]
        public void FindMissing_ReturnsSpaceWhenUnavailable()
        {
            int probedCharacter = -1;
            string missing = TypographyWhitespacePolicy.FindMissing(
                character =>
                {
                    probedCharacter = character;
                    return false;
                });

            Assert.That(missing, Is.EqualTo(" "));
            Assert.That(probedCharacter, Is.EqualTo(0x20));
        }

        [Test]
        public void FindMissing_ReturnsEmptyWhenSpaceIsAvailable()
        {
            string missing = TypographyWhitespacePolicy.FindMissing(
                character => character == ' ');

            Assert.That(missing, Is.Empty);
        }

        [Test]
        public void FindMissing_TreatsNullProbeAsAllMissing()
        {
            Assert.That(
                TypographyWhitespacePolicy.FindMissing(null),
                Is.EqualTo(" "));
        }

        [Test]
        public void Result_ReportsRequestedAddedAndReadyCounts()
        {
            var complete = new TypographyWhitespaceResult(" ", string.Empty);
            var blocked = new TypographyWhitespaceResult(" ", " ");

            Assert.That(complete.RequestedCount, Is.EqualTo(1));
            Assert.That(complete.AddedCount, Is.EqualTo(1));
            Assert.That(complete.IsReady, Is.True);
            Assert.That(blocked.RequestedCount, Is.EqualTo(1));
            Assert.That(blocked.AddedCount, Is.Zero);
            Assert.That(blocked.IsReady, Is.False);
        }

        [Test]
        public void Ensure_NullFontReturnsBlockedResult()
        {
            TypographyWhitespaceResult result =
                TypographyWhitespacePolicy.Ensure(null);

            Assert.That(result.RequestedCount, Is.EqualTo(1));
            Assert.That(result.AddedCount, Is.Zero);
            Assert.That(result.MissingAfter, Is.EqualTo(" "));
            Assert.That(result.IsReady, Is.False);
        }

        [Test]
        public void Resolve_PreparesStandardSpaceForEveryCatalogRole()
        {
            var seen = new HashSet<TMP_FontAsset>();
            foreach (TypographyRole role in
                     (TypographyRole[])System.Enum.GetValues(
                         typeof(TypographyRole)))
            {
                TMP_FontAsset font = TypographyService.Resolve(role);

                Assert.That(font, Is.Not.Null, role.ToString());
                Assert.That(
                    font.HasCharacter(' '),
                    Is.True,
                    $"{role} font must render U+0020.");
                seen.Add(font);
            }

            Assert.That(seen.Count, Is.GreaterThan(1));
        }

        [Test]
        public void Apply_PreparesSpaceBeforeAssigningFont()
        {
            var host = new GameObject(
                "TypographyWhitespacePolicyTests",
                typeof(RectTransform),
                typeof(TextMeshProUGUI));
            try
            {
                TMP_Text label = host.GetComponent<TMP_Text>();

                Assert.That(
                    TypographyService.Apply(
                        label,
                        TypographyRole.Body),
                    Is.True);
                Assert.That(label.font, Is.Not.Null);
                Assert.That(label.font.HasCharacter(' '), Is.True);
                label.text = "공백 포함 문장";
                label.ForceMeshUpdate();
                Assert.That(label.text, Does.Contain(" "));
            }
            finally
            {
                Object.DestroyImmediate(host);
            }
        }

        [Test]
        public void Ensure_IsIdempotentForResolvedBodyFont()
        {
            TMP_FontAsset font =
                TypographyService.Resolve(TypographyRole.Body);

            TypographyWhitespaceResult first =
                TypographyWhitespacePolicy.Ensure(font);
            TypographyWhitespaceResult second =
                TypographyWhitespacePolicy.Ensure(font);

            Assert.That(first.IsReady, Is.True);
            Assert.That(second.IsReady, Is.True);
            Assert.That(second.RequestedCount, Is.Zero);
            Assert.That(second.AddedCount, Is.Zero);
            Assert.That(font.HasCharacter(' '), Is.True);
        }
    }
}
