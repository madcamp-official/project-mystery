using System;
using System.Linq;
using TMPro;

namespace Wake.UI
{
    public readonly struct TypographyWhitespaceResult
    {
        public TypographyWhitespaceResult(
            string missingBefore,
            string missingAfter)
        {
            MissingBefore = missingBefore ?? string.Empty;
            MissingAfter = missingAfter ?? string.Empty;
        }

        public string MissingBefore { get; }
        public string MissingAfter { get; }
        public int RequestedCount => MissingBefore.Length;
        public int AddedCount =>
            Math.Max(0, MissingBefore.Length - MissingAfter.Length);
        public bool IsReady => MissingAfter.Length == 0;
    }

    public static class TypographyWhitespacePolicy
    {
        public const string RequiredCharacters = " ";

        public static TypographyWhitespaceResult Ensure(
            TMP_FontAsset font)
        {
            if (font == null)
            {
                return new TypographyWhitespaceResult(
                    RequiredCharacters,
                    RequiredCharacters);
            }

            string missingBefore = FindMissing(
                character => font.HasCharacter(character));
            if (missingBefore.Length == 0)
            {
                return new TypographyWhitespaceResult(
                    string.Empty,
                    string.Empty);
            }

            AtlasPopulationMode originalMode =
                font.atlasPopulationMode;
            if (originalMode == AtlasPopulationMode.Static)
            {
                font.atlasPopulationMode =
                    AtlasPopulationMode.Dynamic;
            }

            string missingAfter;
            try
            {
                font.TryAddCharacters(
                    missingBefore,
                    out missingAfter,
                    includeFontFeatures: false);
            }
            finally
            {
                font.atlasPopulationMode = originalMode;
            }
            return new TypographyWhitespaceResult(
                missingBefore,
                missingAfter);
        }

        public static string FindMissing(
            Func<int, bool> hasCharacter)
        {
            if (hasCharacter == null)
            {
                return RequiredCharacters;
            }

            return new string(RequiredCharacters
                .Where(character => !hasCharacter(character))
                .Distinct()
                .ToArray());
        }
    }
}
