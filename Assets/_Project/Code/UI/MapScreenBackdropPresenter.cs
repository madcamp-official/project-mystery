using UnityEngine;
using UnityEngine.UI;

namespace Wake.UI
{
    public static class MapScreenBackdropPresenter
    {
        public const string ObjectName = "Map Screen Backdrop";
        public const string ResourcePath =
            "Maps/UI/ui_map_screen_backdrop";

        public static Image Ensure(Transform mapRoot)
        {
            if (mapRoot == null)
            {
                return null;
            }

            Transform existing = mapRoot.Find(ObjectName);
            Image image = existing != null
                ? existing.GetComponent<Image>()
                : Create(mapRoot);
            if (image == null)
            {
                return null;
            }

            Sprite backdrop = Resources.Load<Sprite>(ResourcePath);
            image.sprite = backdrop;
            image.type = backdrop != null &&
                         backdrop.border.sqrMagnitude > 0f
                ? Image.Type.Sliced
                : Image.Type.Simple;
            image.preserveAspect = false;
            image.fillCenter = true;
            image.color = backdrop != null
                ? Color.white
                : UiVisualThemeService.Resolve(UiColorToken.Canvas);
            image.raycastTarget = false;

            RectTransform rect = image.rectTransform;
            rect.anchorMin = Vector2.zero;
            rect.anchorMax = Vector2.one;
            rect.offsetMin = Vector2.zero;
            rect.offsetMax = Vector2.zero;
            rect.localScale = Vector3.one;
            image.transform.SetAsFirstSibling();
            return image;
        }

        private static Image Create(Transform mapRoot)
        {
            GameObject target = new(
                ObjectName,
                typeof(RectTransform),
                typeof(CanvasRenderer),
                typeof(Image));
            target.transform.SetParent(mapRoot, false);
            return target.GetComponent<Image>();
        }
    }
}
