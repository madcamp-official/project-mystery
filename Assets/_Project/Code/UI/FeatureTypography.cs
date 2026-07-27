using TMPro;
using UnityEngine;

namespace Wake.UI
{
    public static class FeatureTypography
    {
        public static int ApplyPuzzle(
            Transform root,
            TMP_Text title,
            TMP_Text objective,
            TMP_Text hint)
        {
            int count = TypographyService.ApplyRecursively(
                root,
                TypographyRole.Choice);
            Override(root, title, TypographyRole.HeadingStrong, ref count);
            Override(root, objective, TypographyRole.Body, ref count);
            Override(root, hint, TypographyRole.BodyRegular, ref count);
            return count;
        }

        public static int ApplyEnding(
            Transform root,
            TMP_Text route,
            TMP_Text title,
            TMP_Text epilogue,
            TMP_Text reason)
        {
            int count = TypographyService.ApplyRecursively(
                root,
                TypographyRole.Body);
            Override(root, route, TypographyRole.Technical, ref count);
            Override(root, title, TypographyRole.HeadingStrong, ref count);
            Override(root, epilogue, TypographyRole.BodyRegular, ref count);
            Override(root, reason, TypographyRole.BodyRegular, ref count);
            return count;
        }

        public static int ApplyTheoryBoard(
            Transform root,
            TMP_Text title,
            TMP_Text progress,
            TMP_Text status)
        {
            int count = TypographyService.ApplyRecursively(
                root,
                TypographyRole.Choice);
            Override(root, title, TypographyRole.HeadingStrong, ref count);
            Override(root, progress, TypographyRole.Technical, ref count);
            Override(root, status, TypographyRole.BodyRegular, ref count);
            return count;
        }

        public static bool ApplyMenuAction(Transform target)
        {
            TMP_Text label = target?.GetComponentInChildren<TMP_Text>(true);
            return TypographyService.Apply(label, TypographyRole.Heading);
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
