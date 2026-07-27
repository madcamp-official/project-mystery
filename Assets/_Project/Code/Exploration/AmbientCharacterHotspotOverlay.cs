using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using Wake.Narrative;

namespace Wake.Exploration
{
    [DisallowMultipleComponent]
    public sealed class AmbientCharacterHotspotOverlay : MonoBehaviour
    {
        private static readonly Dictionary<string, Texture2D> TextureCache =
            new();

        private readonly List<GameObject> spawned = new();
        private RectTransform contentRect;
        private Vector2 lastContentSize;
        private string currentLocationCode = string.Empty;

        public void Initialize(RectTransform backgroundContentRect)
        {
            contentRect = backgroundContentRect;
        }

        public void Show(string locationCode)
        {
            Clear();
            if (contentRect == null)
                return;

            currentLocationCode =
                locationCode?.Trim().ToUpperInvariant() ?? string.Empty;
            IReadOnlyList<AmbientBarkRecord> barks =
                AmbientBarkCatalog.GetAvailable(
                    currentLocationCode,
                    Wake.Core.GameStateManager.Instance);
            for (int index = 0; index < barks.Count; index++)
            {
                CreateWorldCharacter(barks[index], index, barks.Count);
            }

            RefreshLayout();
        }

        private void CreateWorldCharacter(
            AmbientBarkRecord bark,
            int index,
            int count)
        {
            if (!AmbientWorldCharacterCatalog.TryGetAsset(
                    bark.Speaker,
                    out AmbientWorldCharacterAsset asset))
            {
                return;
            }

            Texture2D texture = LoadTexture(asset.ResourcePath);
            if (texture == null)
                return;

            GameObject target = new(
                $"AmbientCharacter_{bark.Speaker}",
                typeof(RectTransform),
                typeof(CanvasRenderer),
                typeof(RawImage),
                typeof(Shadow),
                typeof(Button));
            target.transform.SetParent(contentRect, false);
            target.transform.SetAsLastSibling();

            RawImage image = target.GetComponent<RawImage>();
            image.texture = texture;
            image.uvRect = asset.UvRect;
            image.color = Color.white;
            image.raycastTarget = true;

            Shadow shadow = target.GetComponent<Shadow>();
            shadow.effectColor = new Color(0f, 0f, 0f, 0.42f);
            shadow.effectDistance = new Vector2(10f, -8f);
            shadow.useGraphicAlpha = true;

            Button button = target.GetComponent<Button>();
            button.targetGraphic = image;
            button.transition = Selectable.Transition.ColorTint;
            button.colors =
                AmbientInteractionPresentation.CharacterSpriteColors();
            button.onClick.AddListener(() =>
                DialogueController.Instance?.StartAmbientLine(
                    bark.Speaker,
                    bark.Text,
                    bark.Emotion));

            AmbientWorldPlacement placement =
                AmbientWorldCharacterCatalog.GetPlacement(
                    currentLocationCode,
                    index,
                    count);
            target.name += $"_{index}_{bark.Id}";
            ApplyPlacement(
                target.GetComponent<RectTransform>(),
                image,
                placement,
                asset.CellAspectRatio);
            spawned.Add(target);
        }

        private void LateUpdate()
        {
            if (contentRect != null &&
                spawned.Count > 0 &&
                contentRect.rect.size != lastContentSize)
            {
                RefreshLayout();
            }
        }

        private void RefreshLayout()
        {
            if (contentRect == null)
                return;

            Vector2 contentSize = contentRect.rect.size;
            if (contentSize.x <= 0f || contentSize.y <= 0f)
                return;

            for (int index = 0; index < spawned.Count; index++)
            {
                GameObject target = spawned[index];
                if (target == null)
                    continue;

                RawImage image = target.GetComponent<RawImage>();
                AmbientWorldPlacement placement =
                    AmbientWorldCharacterCatalog.GetPlacement(
                        currentLocationCode,
                        index,
                        spawned.Count);
                float cellAspect =
                    Mathf.Abs(image.uvRect.width) *
                    image.texture.width /
                    (image.uvRect.height * image.texture.height);
                ApplyPlacement(
                    target.GetComponent<RectTransform>(),
                    image,
                    placement,
                    cellAspect);
            }

            lastContentSize = contentSize;
        }

        private void ApplyPlacement(
            RectTransform rect,
            RawImage image,
            AmbientWorldPlacement placement,
            float aspectRatio)
        {
            Vector2 anchor = placement.Anchor;
            rect.anchorMin = anchor;
            rect.anchorMax = anchor;
            rect.pivot = new Vector2(0.5f, 0f);
            rect.anchoredPosition = Vector2.zero;

            float height = contentRect.rect.height *
                           placement.NormalizedHeight;
            rect.sizeDelta = new Vector2(height * aspectRatio, height);

            Rect uv = image.uvRect;
            float width = Mathf.Abs(uv.width);
            float baseX = uv.width < 0f ? uv.x + uv.width : uv.x;
            image.uvRect = placement.Mirror
                ? new Rect(baseX + width, uv.y, -width, uv.height)
                : new Rect(
                    baseX, uv.y, width, uv.height);
        }

        private static Texture2D LoadTexture(string resourcePath)
        {
            if (TextureCache.TryGetValue(
                    resourcePath,
                    out Texture2D cached))
            {
                return cached;
            }

            Texture2D texture = Resources.Load<Texture2D>(resourcePath);
            TextureCache[resourcePath] = texture;
            return texture;
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
