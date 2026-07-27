using System.Collections.Generic;
using TMPro;
using Wake.UI;

namespace Wake.Narrative
{
    /// <summary>
    /// Owns the semantic typography assignments for the dialogue surface.
    /// Keeping the mapping here prevents dynamically cloned choices from
    /// silently inheriting whichever font happened to be authored in-scene.
    /// </summary>
    public static class DialogueTypography
    {
        public static int ApplySurface(
            TMP_Text line,
            TMP_Text speaker,
            IReadOnlyList<TMP_Text> choiceLabels)
        {
            int applied = 0;
            if (ApplyLine(line))
            {
                applied++;
            }
            if (ApplySpeaker(speaker))
            {
                applied++;
            }

            return applied + ApplyChoices(choiceLabels);
        }

        public static bool ApplyLine(TMP_Text line)
        {
            return TypographyService.Apply(line, TypographyRole.Body);
        }

        public static bool ApplySpeaker(TMP_Text speaker)
        {
            return TypographyService.Apply(
                speaker,
                TypographyRole.SpeakerName);
        }

        public static int ApplyChoices(
            IReadOnlyList<TMP_Text> choiceLabels)
        {
            if (choiceLabels == null)
            {
                return 0;
            }

            int applied = 0;
            for (int i = 0; i < choiceLabels.Count; i++)
            {
                if (TypographyService.Apply(
                        choiceLabels[i],
                        TypographyRole.Choice))
                {
                    applied++;
                }
            }

            return applied;
        }
    }
}
