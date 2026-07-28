using UnityEngine;
using UnityEngine.UI;

namespace Wake.UI
{
    // Fills the space behind "Title Presentation" so the lobby dive shows a
    // depth-tinted gradient with drifting bubbles and seafloor silhouettes
    // instead of the camera's flat clear color once the title art slides
    // out of view. SetDepth() is driven by the same t as the water dive.
    [DisallowMultipleComponent]
    public sealed class LobbyBackdropController : MonoBehaviour
    {
        private const int BubbleCount = 12;
        private const int SilhouetteCount = 4;

        private static readonly Color ShallowTop = new(0.20f, 0.50f, 0.58f, 1f);
        private static readonly Color ShallowBottom = new(0.03f, 0.10f, 0.17f, 1f);
        private static readonly Color DeepTop = new(0.02f, 0.06f, 0.11f, 1f);
        private static readonly Color DeepBottom = new(0.003f, 0.01f, 0.02f, 1f);

        private static Sprite softDotSprite;

        private Texture2D gradientTexture;
        private RectTransform bubbleLayer;
        private readonly RectTransform[] bubbles = new RectTransform[BubbleCount];
        private readonly float[] bubbleSpeed = new float[BubbleCount];
        private readonly float[] bubblePhase = new float[BubbleCount];
        private readonly float[] bubbleBaseX = new float[BubbleCount];
        private float depth;

        private void Awake()
        {
            Build();
        }

        public void SetDepth(float t)
        {
            depth = Mathf.Clamp01(t);
            ApplyGradient();
        }

        private void Build()
        {
            RectTransform root = transform as RectTransform;
            SaveSlotSelectionController.Stretch(root);

            BuildGradient(root);
            BuildSilhouettes(root);
            BuildBubbles(root);
        }

        private void BuildGradient(RectTransform root)
        {
            GameObject imageObject = new(
                "Depth Gradient", typeof(RectTransform), typeof(RawImage));
            imageObject.transform.SetParent(root, false);
            RectTransform rect = imageObject.GetComponent<RectTransform>();
            SaveSlotSelectionController.Stretch(rect);
            RawImage gradientImage = imageObject.GetComponent<RawImage>();
            gradientImage.raycastTarget = false;

            gradientTexture = new Texture2D(1, 8, TextureFormat.RGBA32, false)
            {
                wrapMode = TextureWrapMode.Clamp,
                filterMode = FilterMode.Bilinear,
                name = "LobbyDepthGradient"
            };
            gradientImage.texture = gradientTexture;
            ApplyGradient();
        }

        private void ApplyGradient()
        {
            if (gradientTexture == null)
            {
                return;
            }
            Color top = Color.Lerp(ShallowTop, DeepTop, depth);
            Color bottom = Color.Lerp(ShallowBottom, DeepBottom, depth);
            int height = gradientTexture.height;
            for (int y = 0; y < height; y++)
            {
                float t = y / (float)(height - 1);
                gradientTexture.SetPixel(0, y, Color.Lerp(bottom, top, t));
            }
            gradientTexture.Apply();
        }

        private void BuildSilhouettes(RectTransform root)
        {
            Sprite sprite = GetSoftDotSprite();
            for (int i = 0; i < SilhouetteCount; i++)
            {
                GameObject blobObject = new(
                    $"Silhouette {i}", typeof(RectTransform), typeof(Image));
                blobObject.transform.SetParent(root, false);
                RectTransform rect = blobObject.GetComponent<RectTransform>();
                rect.anchorMin = new Vector2(Random.Range(0.05f, 0.9f), 0f);
                rect.anchorMax = rect.anchorMin;
                rect.pivot = new Vector2(0.5f, 0f);
                float size = Random.Range(260f, 520f);
                rect.sizeDelta = new Vector2(size, size * Random.Range(0.5f, 0.75f));
                rect.anchoredPosition = new Vector2(0f, Random.Range(-40f, 10f));
                Image image = blobObject.GetComponent<Image>();
                image.sprite = sprite;
                image.color = new Color(0.01f, 0.03f, 0.05f, Random.Range(0.35f, 0.55f));
                image.raycastTarget = false;
            }
        }

        private void BuildBubbles(RectTransform root)
        {
            GameObject layerObject = new("Bubbles", typeof(RectTransform));
            bubbleLayer = layerObject.GetComponent<RectTransform>();
            bubbleLayer.SetParent(root, false);
            SaveSlotSelectionController.Stretch(bubbleLayer);

            Sprite sprite = GetSoftDotSprite();
            for (int i = 0; i < BubbleCount; i++)
            {
                GameObject bubbleObject = new(
                    $"Bubble {i}", typeof(RectTransform), typeof(Image));
                bubbleObject.transform.SetParent(bubbleLayer, false);
                RectTransform rect = bubbleObject.GetComponent<RectTransform>();
                float size = Random.Range(6f, 22f);
                rect.sizeDelta = new Vector2(size, size);
                rect.anchorMin = rect.anchorMax = new Vector2(0.5f, 0f);
                rect.pivot = new Vector2(0.5f, 0.5f);
                Image image = bubbleObject.GetComponent<Image>();
                image.sprite = sprite;
                image.color = new Color(0.8f, 0.95f, 1f, 0.28f);
                image.raycastTarget = false;
                bubbles[i] = rect;
                bubbleSpeed[i] = Random.Range(40f, 110f);
                bubblePhase[i] = Random.Range(0f, 1f);
                bubbleBaseX[i] = Random.Range(-1f, 1f);
            }
        }

        private void Update()
        {
            if (bubbleLayer == null)
            {
                return;
            }
            Rect layerRect = bubbleLayer.rect;
            float height = layerRect.height;
            if (height <= 0f)
            {
                return;
            }
            float width = layerRect.width;
            for (int i = 0; i < bubbles.Length; i++)
            {
                RectTransform rect = bubbles[i];
                if (rect == null)
                {
                    continue;
                }
                bubblePhase[i] +=
                    bubbleSpeed[i] * Time.unscaledDeltaTime / Mathf.Max(height, 1f);
                if (bubblePhase[i] > 1f)
                {
                    bubblePhase[i] -= 1f;
                    bubbleBaseX[i] = Random.Range(-1f, 1f);
                }
                float y = bubblePhase[i] * height;
                float drift = Mathf.Sin(bubblePhase[i] * 6f + i) * 14f;
                float x = bubbleBaseX[i] * width * 0.45f + drift;
                rect.anchoredPosition = new Vector2(x, y);
            }
        }

        private static Sprite GetSoftDotSprite()
        {
            if (softDotSprite != null)
            {
                return softDotSprite;
            }
            const int size = 64;
            Texture2D texture = new(size, size, TextureFormat.RGBA32, false)
            {
                wrapMode = TextureWrapMode.Clamp,
                filterMode = FilterMode.Bilinear,
                name = "LobbySoftDot"
            };
            Vector2 center = new(size / 2f, size / 2f);
            float radius = size / 2f;
            for (int y = 0; y < size; y++)
            {
                for (int x = 0; x < size; x++)
                {
                    float distance = Vector2.Distance(new Vector2(x, y), center);
                    float alpha = Mathf.Clamp01(1f - distance / radius);
                    alpha *= alpha;
                    texture.SetPixel(x, y, new Color(1f, 1f, 1f, alpha));
                }
            }
            texture.Apply();
            softDotSprite = Sprite.Create(
                texture,
                new Rect(0f, 0f, size, size),
                new Vector2(0.5f, 0.5f));
            return softDotSprite;
        }
    }
}
