using TMPro;
using UnityEngine;

namespace Wake.UI
{
    /// <summary>
    /// Maps status HUD fields to semantic font roles. The legacy
    /// RuntimeKoreanFont property remains a Body-role compatibility path for
    /// controllers that have not yet migrated.
    /// </summary>
    public static class StatusHUDTypography
    {
        public static int Apply(
            TMP_Text time,
            TMP_Text anxiety,
            TMP_Text integrity,
            TMP_Text progress,
            TMP_Text trust,
            Transform trustRoot)
        {
            int applied = 0;
            ApplyOne(time, TypographyRole.Technical, ref applied);
            ApplyOne(anxiety, TypographyRole.Body, ref applied);
            ApplyOne(integrity, TypographyRole.Body, ref applied);
            ApplyOne(progress, TypographyRole.Body, ref applied);

            bool trustCountedByRoot = trust != null &&
                trustRoot != null &&
                (trust.transform == trustRoot ||
                 trust.transform.IsChildOf(trustRoot));
            applied += TypographyService.ApplyRecursively(
                trustRoot,
                TypographyRole.Body);
            if (!trustCountedByRoot)
            {
                ApplyOne(trust, TypographyRole.Body, ref applied);
            }

            return applied;
        }

        private static void ApplyOne(
            TMP_Text label,
            TypographyRole role,
            ref int applied)
        {
            if (TypographyService.Apply(label, role))
            {
                applied++;
            }
        }
    }
}
