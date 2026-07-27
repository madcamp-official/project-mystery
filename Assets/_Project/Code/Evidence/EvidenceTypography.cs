using TMPro;
using UnityEngine;
using Wake.UI;

namespace Wake.Evidence
{
    /// <summary>
    /// Defines semantic font roles for the evidence browser. The broad body
    /// assignment also covers authored navigation labels, while explicit
    /// overrides preserve the visual hierarchy of evidence content.
    /// </summary>
    public static class EvidenceTypography
    {
        public static int ApplySurface(
            Transform root,
            TMP_Text title,
            TMP_Text detail,
            TMP_Text theoryBoard)
        {
            int applied = TypographyService.ApplyRecursively(
                root,
                TypographyRole.BodyRegular);

            ApplyOverride(
                root,
                title,
                TypographyRole.Heading,
                ref applied);
            ApplyOverride(
                root,
                detail,
                TypographyRole.BodyRegular,
                ref applied);
            ApplyOverride(
                root,
                theoryBoard,
                TypographyRole.Heading,
                ref applied);
            return applied;
        }

        public static bool ApplyCarouselLabel(TMP_Text label)
        {
            return TypographyService.Apply(
                label,
                TypographyRole.TechnicalStrong);
        }

        public static bool ApplyDetail(TMP_Text label, string category)
        {
            return TypographyService.Apply(
                label,
                ResolveDetailRole(category));
        }

        public static TypographyRole ResolveDetailRole(string category)
        {
            return string.Equals(
                    category,
                    "invitation",
                    System.StringComparison.OrdinalIgnoreCase)
                ? TypographyRole.Handwritten
                : TypographyRole.BodyRegular;
        }

        private static void ApplyOverride(
            Transform root,
            TMP_Text label,
            TypographyRole role,
            ref int applied)
        {
            if (label == null)
            {
                return;
            }

            bool alreadyCounted = root != null &&
                (label.transform == root ||
                 label.transform.IsChildOf(root));
            if (TypographyService.Apply(label, role) && !alreadyCounted)
            {
                applied++;
            }
        }
    }
}
