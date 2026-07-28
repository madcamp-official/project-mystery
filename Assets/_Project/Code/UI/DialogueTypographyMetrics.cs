using System;
using UnityEngine;

namespace Wake.UI
{
    public static class DialogueTypographyMetrics
    {
        public const float LineMinimum = 32f;
        public const float LineMaximum = 56f;
        public const float ChoiceMinimum = 34f;
        public const float ChoiceMaximum = 48f;
        public const float SpeakerMinimum = 36f;
        public const float SpeakerMaximum = 46f;

        // TMP adds these values to each face's native line metric. With the
        // imported Korean faces this produces approximately 140-150% leading.
        public const float BodyLineSpacing = 8f;
        public const float ChoiceLineSpacing = 7f;
        public const float HeadingLineSpacing = 4f;

        public static float CalculateCanvasScale(Vector2 screenSize)
        {
            if (screenSize.x <= 0f || screenSize.y <= 0f)
            {
                throw new ArgumentOutOfRangeException(
                    nameof(screenSize),
                    "Screen dimensions must be positive.");
            }

            float widthScale =
                screenSize.x / ResponsiveDialogueLayout.ReferenceResolution.x;
            float heightScale =
                screenSize.y / ResponsiveDialogueLayout.ReferenceResolution.y;
            return Mathf.Sqrt(widthScale * heightScale);
        }

        public static float ToScreenPixels(
            float referenceFontSize,
            Vector2 screenSize)
        {
            if (referenceFontSize < 0f)
            {
                throw new ArgumentOutOfRangeException(
                    nameof(referenceFontSize));
            }
            return referenceFontSize * CalculateCanvasScale(screenSize);
        }

        public static Vector2 GetLineScreenRange(Vector2 screenSize)
        {
            return new Vector2(
                ToScreenPixels(LineMinimum, screenSize),
                ToScreenPixels(LineMaximum, screenSize));
        }

        public static Vector2 GetChoiceScreenRange(Vector2 screenSize)
        {
            return new Vector2(
                ToScreenPixels(ChoiceMinimum, screenSize),
                ToScreenPixels(ChoiceMaximum, screenSize));
        }

        public static Vector2 GetSpeakerScreenRange(Vector2 screenSize)
        {
            return new Vector2(
                ToScreenPixels(SpeakerMinimum, screenSize),
                ToScreenPixels(SpeakerMaximum, screenSize));
        }
    }
}
