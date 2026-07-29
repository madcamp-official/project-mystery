using System.Collections.Generic;
using TMPro;
using UnityEngine;

namespace Wake.UI
{
    public static class TypographyService
    {
        public const string ResourcesPath = "Typography/TypographyCatalog";

        private static TypographyCatalog cachedCatalog;
        private static TypographyCatalog catalogOverride;

        public static TypographyCatalog Catalog
        {
            get
            {
                if (catalogOverride != null)
                {
                    return catalogOverride;
                }

                if (cachedCatalog == null)
                {
                    cachedCatalog =
                        Resources.Load<TypographyCatalog>(ResourcesPath);
                }

                return cachedCatalog;
            }
        }

        public static TMP_FontAsset Resolve(TypographyRole role)
        {
            TypographyCatalog catalog = Catalog;
            TMP_FontAsset font = catalog != null
                ? catalog.Resolve(role)
                : TMP_Settings.defaultFontAsset;
            TMP_FontAsset body = catalog?.Body;
            if (font != null &&
                body != null &&
                font != body)
            {
                List<TMP_FontAsset> fallbacks =
                    font.fallbackFontAssetTable;
                if (fallbacks == null)
                {
                    fallbacks = new List<TMP_FontAsset>();
                    font.fallbackFontAssetTable = fallbacks;
                }

                if (!fallbacks.Contains(body))
                {
                    fallbacks.Add(body);
                }
            }
            TypographyWhitespacePolicy.Ensure(font);
            return font;
        }

        public static bool Apply(TMP_Text text, TypographyRole role)
        {
            if (text == null)
            {
                return false;
            }

            TMP_FontAsset font = Resolve(role);
            if (font == null)
            {
                return false;
            }

            if (text.font != font)
            {
                text.font = font;
                text.SetAllDirty();
            }

            return true;
        }

        public static int ApplyRecursively(
            Transform root,
            TypographyRole role,
            bool includeInactive = true)
        {
            if (root == null)
            {
                return 0;
            }

            int applied = 0;
            TMP_Text[] labels =
                root.GetComponentsInChildren<TMP_Text>(includeInactive);
            foreach (TMP_Text label in labels)
            {
                if (Apply(label, role))
                {
                    applied++;
                }
            }

            return applied;
        }

        public static void ClearCache()
        {
            cachedCatalog = null;
        }

#if UNITY_EDITOR
        public static void SetCatalogForTests(TypographyCatalog catalog)
        {
            catalogOverride = catalog;
            cachedCatalog = null;
        }
#endif
    }
}
