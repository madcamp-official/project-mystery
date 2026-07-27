using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using Wake.Narrative;
using Wake.UI;

namespace Wake.Exploration
{
    [DisallowMultipleComponent]
    public sealed class AmbientCharacterHotspotOverlay : MonoBehaviour
    {
        private readonly List<GameObject> spawned = new();
        private RectTransform contentRect;
        private Vector2 lastViewportSize;
        private Rect lastSafeArea;

        public void Initialize(RectTransform backgroundContentRect)
        {
            contentRect = backgroundContentRect;
        }

        public void Show(string locationCode)
        {
            Clear();
            if (contentRect == null)
                return;

            IReadOnlyList<AmbientBarkRecord> barks =
                AmbientBarkCatalog.GetAvailable(
                    locationCode,
                    Wake.Core.GameStateManager.Instance);
            for (int index = 0; index < barks.Count; index++)
            {
                CreateCharacterButton(barks[index]);
            }
            RefreshLayout();
        }

        private void CreateCharacterButton(AmbientBarkRecord bark)
        {
            GameObject target = new(
                $"AmbientCharacter_{bark.Speaker}",
                typeof(RectTransform),
                typeof(CanvasRenderer),
                typeof(Image),
                typeof(Button),
                typeof(Outline));
            target.transform.SetParent(contentRect, false);
            RectTransform rect = target.GetComponent<RectTransform>();
            rect.anchorMin = Vector2.zero;
            rect.anchorMax = Vector2.zero;
            rect.pivot = new Vector2(.5f, 0f);

            Image image = target.GetComponent<Image>();
            image.color = Color.white;
            Outline outline = target.GetComponent<Outline>();
            outline.effectColor = new Color32(210, 164, 83, 230);
            outline.effectDistance = new Vector2(2f, -2f);

            GameObject labelObject = new(
                "Label",
                typeof(RectTransform),
                typeof(TextMeshProUGUI));
            labelObject.transform.SetParent(target.transform, false);
            RectTransform labelRect = labelObject.GetComponent<RectTransform>();
            labelRect.anchorMin = Vector2.zero;
            labelRect.anchorMax = Vector2.one;
            labelRect.offsetMin = new Vector2(8f, 4f);
            labelRect.offsetMax = new Vector2(-8f, -4f);
            TMP_Text label = labelObject.GetComponent<TMP_Text>();
            TypographyService.Apply(label, TypographyRole.Body);
            label.text = AmbientInteractionPresentation.CharacterLabel(
                DialoguePortraitCatalog.GetDisplayName(bark.Speaker));
            label.fontSize = 18f;
            label.alignment = TextAlignmentOptions.Center;
            label.color = Color.white;
            label.raycastTarget = false;

            Button button = target.GetComponent<Button>();
            button.colors = AmbientInteractionPresentation.CharacterColors();
            button.onClick.AddListener(() =>
                DialogueController.Instance?.StartAmbientLine(
                    bark.Speaker,
                    bark.Text,
                    bark.Emotion));
            spawned.Add(target);
        }

        private void LateUpdate()
        {
            if (contentRect != null &&
                spawned.Count > 0 &&
                (contentRect.rect.size != lastViewportSize ||
                 Screen.safeArea != lastSafeArea))
            {
                RefreshLayout();
            }
        }

        private void RefreshLayout()
        {
            if (contentRect == null)
                return;

            Vector2 viewportSize = contentRect.rect.size;
            if (viewportSize.x <= 0f || viewportSize.y <= 0f)
                return;

            float screenWidth = Mathf.Max(1f, Screen.width);
            float screenHeight = Mathf.Max(1f, Screen.height);
            float scaleX = viewportSize.x / screenWidth;
            float scaleY = viewportSize.y / screenHeight;
            Rect safeArea = Screen.safeArea;
            float safeAreaX = safeArea.xMin * scaleX;
            float safeAreaWidth = safeArea.width * scaleX;
            float bottom =
                safeArea.yMin * scaleY +
                AmbientInteractionPresentation.CharacterEdgePadding;

            for (int index = 0; index < spawned.Count; index++)
            {
                GameObject target = spawned[index];
                if (target == null)
                    continue;

                AmbientCharacterPlacement placement =
                    AmbientInteractionPresentation.CharacterPlacement(
                        index,
                        spawned.Count,
                        viewportSize.x,
                        safeAreaX,
                        safeAreaWidth);
                RectTransform rect = target.GetComponent<RectTransform>();
                rect.anchorMin = Vector2.zero;
                rect.anchorMax = Vector2.zero;
                rect.anchoredPosition = new Vector2(
                    placement.AnchorX * viewportSize.x,
                    bottom);
                rect.sizeDelta = placement.Size;
            }

            lastViewportSize = viewportSize;
            lastSafeArea = safeArea;
        }

        private void Clear()
        {
            foreach (GameObject target in spawned)
            {
                if (target != null)
                    Destroy(target);
            }
            spawned.Clear();
        }
    }
}
