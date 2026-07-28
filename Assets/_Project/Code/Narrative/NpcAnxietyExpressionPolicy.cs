using System;
using Wake.Core;

namespace Wake.Narrative
{
    public static class NpcAnxietyExpressionPolicy
    {
        public const int ConcernThreshold = 40;

        public static PortraitEmotion Resolve(
            string characterId,
            PortraitEmotion source,
            int publicAnxiety)
        {
            if (string.Equals(
                    characterId?.Trim(),
                    "ADRIAN",
                    StringComparison.OrdinalIgnoreCase))
            {
                return source;
            }

            int anxiety = Math.Clamp(
                publicAnxiety,
                0,
                GameStateManager.MaxPercent);
            if (anxiety < ConcernThreshold)
            {
                return source;
            }

            if (anxiety < GameStateManager.RestrictedAreaAnxiety)
            {
                if (source == PortraitEmotion.Angry ||
                    source == PortraitEmotion.Concerned)
                {
                    return source;
                }

                return UsesAngryVariation(characterId)
                    ? PortraitEmotion.Concerned
                    : PortraitEmotion.Neutral;
            }

            return UsesAngryVariation(characterId)
                ? PortraitEmotion.Angry
                : PortraitEmotion.Concerned;
        }

        private static bool UsesAngryVariation(string characterId)
        {
            string value = characterId?.Trim().ToUpperInvariant() ??
                           string.Empty;
            unchecked
            {
                int hash = 17;
                foreach (char character in value)
                {
                    hash = hash * 31 + character;
                }
                return (hash & 1) == 0;
            }
        }
    }
}
