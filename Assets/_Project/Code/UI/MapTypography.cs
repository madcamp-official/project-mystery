using TMPro;
using UnityEngine;

namespace Wake.UI
{
    public static class MapTypography
    {
        public static bool ApplyLocation(TMP_Text label)
        {
            return TypographyService.Apply(label, TypographyRole.Heading);
        }

        public static bool ApplyCode(TMP_Text label)
        {
            return TypographyService.Apply(
                label,
                TypographyRole.TechnicalStrong);
        }

        public static bool ApplyNotice(TMP_Text label)
        {
            return TypographyService.Apply(
                label,
                TypographyRole.BodyRegular);
        }

        public static int ApplyObjective(
            Transform root,
            TMP_Text title,
            TMP_Text progress,
            TMP_Text accessibility)
        {
            int applied = TypographyService.ApplyRecursively(
                root,
                TypographyRole.Body);
            ApplyOverride(root, title, TypographyRole.Heading, ref applied);
            ApplyOverride(
                root,
                progress,
                TypographyRole.TechnicalStrong,
                ref applied);
            ApplyOverride(
                root,
                accessibility,
                TypographyRole.BodyRegular,
                ref applied);
            return applied;
        }

        private static void ApplyOverride(
            Transform root,
            TMP_Text label,
            TypographyRole role,
            ref int count)
        {
            if (label == null)
            {
                return;
            }
            bool counted = root != null &&
                (label.transform == root ||
                 label.transform.IsChildOf(root));
            if (TypographyService.Apply(label, role) && !counted)
            {
                count++;
            }
        }
    }
}
