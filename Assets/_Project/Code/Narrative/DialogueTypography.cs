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
        // Choice buttons use the same font as the dialogue line itself,
        // so the choice window doesn't read as a mix of fonts.
        private const TypographyRole ChoiceTextRole = TypographyRole.Body;

        // Unicode non-breaking space (U+00A0) - TMP will not wrap on this,
        // so gluing the last two words together with it prevents a single
        // trailing word/particle from being stranded alone on its own line.
        private const char NonBreakingSpace = (char)0x00A0;

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
                        ChoiceTextRole))
                {
                    applied++;
                }
            }

            return applied;
        }

        public static bool ApplyChoice(TMP_Text label)
        {
            return TypographyService.Apply(label, ChoiceTextRole);
        }

        public static string PreventOrphanWrap(string content)
        {
            if (string.IsNullOrEmpty(content))
            {
                return content;
            }

            int lastSpace = content.LastIndexOf(' ');
            if (lastSpace < 0 || lastSpace == content.Length - 1)
            {
                return content;
            }

            return content.Substring(0, lastSpace) + NonBreakingSpace +
                content.Substring(lastSpace + 1);
        }
    }
}
