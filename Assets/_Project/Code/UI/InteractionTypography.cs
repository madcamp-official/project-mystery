using TMPro;
using UnityEngine;

namespace Wake.UI
{
    public static class InteractionTypography
    {
        public static int Apply(
            Transform root,
            TMP_Text technical,
            TMP_Text hint,
            TMP_Text status)
        {
            int count = TypographyService.ApplyRecursively(
                root,
                TypographyRole.Body);
            Override(
                root,
                technical,
                TypographyRole.TechnicalStrong,
                ref count);
            Override(
                root,
                hint,
                TypographyRole.BodyRegular,
                ref count);
            Override(
                root,
                status,
                TypographyRole.Heading,
                ref count);
            return count;
        }

        public static bool ApplyUrgentAlert(TMP_Text alert)
        {
            return TypographyService.Apply(
                alert,
                TypographyRole.SpecialAlert);
        }

        private static void Override(
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
