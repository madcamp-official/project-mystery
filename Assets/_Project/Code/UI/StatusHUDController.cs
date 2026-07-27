using System.Linq;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using Wake.Core;
using Wake.Narrative;

namespace Wake.UI
{
    [ExecuteAlways]
    public class StatusHUDController : MonoBehaviour
    {
        private const float HudHeight = 168f;
        private const float TimeFontSize = 46f;
        private const float IndicatorFontSize = 34f;
        private const float ProgressFontSize = 28f;
        private const float TrustHeight = 88f;
        private const float TrustWidth = 520f;
        private const float TrustFontSize = 30f;
        private const float IconSize = 44f;
        private const float MarkerSize = 18f;
        private const float PipSize = 22f;

        private static readonly Color Navy = new(0.035f, 0.075f, 0.12f, 0.96f);
        private static readonly Color Panel = new(0.075f, 0.13f, 0.18f, 0.96f);
        private static readonly Color Paper = new(0.91f, 0.87f, 0.76f, 1f);

        [Header("Global Meter Art")]
        [SerializeField] private Sprite anxietyIconSprite;
        [SerializeField] private Sprite integrityIconSprite;
        [SerializeField] private Sprite anxietyMeterFillSprite;
        [SerializeField] private Sprite integrityMeterFillSprite;
        [SerializeField] private Sprite anxietyMarker70Sprite;
        [SerializeField] private Sprite anxietyPanicOverlaySprite;
        [SerializeField] private Sprite integrityDamageOverlaySprite;
        [SerializeField] private Sprite integrityCriticalOverlaySprite;

        [Header("Trust Art")]
        [SerializeField] private Sprite trustPipEmptySprite;
        [SerializeField] private Sprite trustPipFilledSprite;

        private TMP_Text timeText;
        private TMP_Text anxietyText;
        private TMP_Text integrityText;
        private TMP_Text progressText;
        private TMP_Text trustText;
        private Image anxietyFill;
        private Image integrityFill;
        private GameObject anxietyPanicOverlay;
        private GameObject integrityDamageOverlay;
        private GameObject integrityCriticalOverlay;
        private Image[] trustPips;
        private GameObject trustRoot;
        private GameStateManager state;
        private string contextCharacter;

        public static TMP_FontAsset RuntimeKoreanFont =>
            TypographyService.Resolve(TypographyRole.Body);

        private void OnEnable()
        {
            BuildWireframe();
            ApplyTypography();
            if (Application.isPlaying)
            {
                TryBindState();
            }
            else
            {
                RenderDefaults();
            }
        }

        private void Start()
        {
            if (Application.isPlaying)
            {
                ApplyTypography();
                TryBindState();
            }
        }

        private void OnDisable()
        {
            UnbindState();
        }

        public void BuildWireframe()
        {
            RectTransform root = transform as RectTransform;
            if (root == null)
            {
                return;
            }

            root.anchorMin = new Vector2(0f, 1f);
            root.anchorMax = new Vector2(1f, 1f);
            root.pivot = new Vector2(0.5f, 1f);
            root.anchoredPosition = Vector2.zero;
            root.sizeDelta = new Vector2(0f, HudHeight);

            Image background = GetOrAdd<Image>(gameObject);
            background.color = Navy;
            background.raycastTarget = false;
            Transform legacyTheoryPanel = root.Find("Theory Slots");
            if (legacyTheoryPanel != null)
            {
                legacyTheoryPanel.gameObject.SetActive(false);
            }

            Transform timePanel = EnsurePanel(root, "Time Badge", 0.01f, 0.17f);
            timeText = EnsureText(
                timePanel,
                "Value",
                TextAlignmentOptions.Center,
                TimeFontSize);

            Transform anxietyPanel = EnsurePanel(root, "Anxiety Indicator", 0.18f, 0.42f);
            bool hasAnxietyIcon = EnsureIcon(anxietyPanel, "Icon", anxietyIconSprite) != null;
            anxietyText = EnsureText(
                anxietyPanel,
                "Label",
                TextAlignmentOptions.TopLeft,
                IndicatorFontSize);
            OffsetLabelForIcon(anxietyText.rectTransform, hasAnxietyIcon);
            anxietyFill = EnsureMeterBar(anxietyPanel, "Bar", 0.16f, anxietyMeterFillSprite);
            EnsureMarker(anxietyPanel.Find("Bar"), "Marker70", anxietyMarker70Sprite, 0.7f);
            anxietyPanicOverlay = EnsureOverlay(anxietyPanel, "PanicOverlay", anxietyPanicOverlaySprite);

            Transform integrityPanel = EnsurePanel(root, "Integrity Indicator", 0.43f, 0.67f);
            bool hasIntegrityIcon = EnsureIcon(integrityPanel, "Icon", integrityIconSprite) != null;
            integrityText = EnsureText(
                integrityPanel,
                "Label",
                TextAlignmentOptions.TopLeft,
                IndicatorFontSize);
            OffsetLabelForIcon(integrityText.rectTransform, hasIntegrityIcon);
            integrityFill = EnsureMeterBar(integrityPanel, "Bar", 0.16f, integrityMeterFillSprite);
            integrityDamageOverlay = EnsureOverlay(integrityPanel, "DamageOverlay", integrityDamageOverlaySprite);
            integrityCriticalOverlay = EnsureOverlay(integrityPanel, "CriticalOverlay", integrityCriticalOverlaySprite);

            Transform progressPanel = EnsurePanel(
                root,
                "Investigation Progress",
                0.68f,
                0.99f);
            progressText = EnsureText(
                progressPanel,
                "Label",
                TextAlignmentOptions.Center,
                ProgressFontSize);

            Transform portraitFrame = transform.parent?.Find("Ingame/Line Panel/Image");
            if (portraitFrame != null)
            {
                trustRoot = EnsureChild(
                    portraitFrame,
                    "Context Trust",
                    typeof(CanvasRenderer),
                    typeof(Image));
                RectTransform trustRect = trustRoot.GetComponent<RectTransform>();
                trustRect.anchorMin = new Vector2(0.5f, 0f);
                trustRect.anchorMax = new Vector2(0.5f, 0f);
                trustRect.pivot = new Vector2(0.5f, 1f);
                trustRect.anchoredPosition = new Vector2(0f, -10f);
                trustRect.sizeDelta = new Vector2(TrustWidth, TrustHeight);
                trustRoot.GetComponent<Image>().color = Navy;
                trustText = EnsureText(
                    trustRect,
                    "Trust Label",
                    TextAlignmentOptions.Center,
                    TrustFontSize);
                trustPips = EnsureTrustPips(trustRect);
            }
        }

        public void SetContextCharacter(string characterName)
        {
            contextCharacter = IsPlayerCharacter(characterName) ? null : characterName;
            Refresh();
        }

        public void ClearContextCharacter()
        {
            contextCharacter = null;
            Refresh();
        }

        private void TryBindState()
        {
            if (state == GameStateManager.Instance && state != null)
            {
                Refresh();
                return;
            }

            UnbindState();
            state = GameStateManager.Instance;
            if (state == null)
            {
                RenderDefaults();
                return;
            }

            state.StateChanged += Refresh;
            state.FeedbackRequested += ShowFeedback;
            state.BadEndTriggered += ShowBadEnd;
            Refresh();
        }

        private void UnbindState()
        {
            if (state == null)
            {
                return;
            }

            state.StateChanged -= Refresh;
            state.FeedbackRequested -= ShowFeedback;
            state.BadEndTriggered -= ShowBadEnd;
            state = null;
        }

        private void Refresh()
        {
            if (timeText == null)
            {
                BuildWireframe();
            }

            ApplyTypography();

            if (state == null)
            {
                state = GameStateManager.Instance;
            }

            if (state == null)
            {
                RenderDefaults();
                return;
            }

            timeText.text = $"DAY {state.Day}  ·  {state.CurrentTimeBlock}";

            anxietyText.text = state.PublicAnxiety >= GameStateManager.RestrictedAreaAnxiety
                ? $"! 승객 불안  {state.PublicAnxiety}/100"
                : $"승객 불안  {state.PublicAnxiety}/100";
            SetFill(anxietyFill, state.PublicAnxiety);
            SetActiveSafe(anxietyPanicOverlay, state.PublicAnxiety >= GameStateManager.MaxPercent);

            integrityText.text = state.EvidenceIntegrity == 0
                ? "! 현장 보존도  0/100"
                : $"현장 보존도  {state.EvidenceIntegrity}/100";
            SetFill(integrityFill, state.EvidenceIntegrity);
            SetActiveSafe(integrityDamageOverlay, state.EvidenceIntegrity > 0 && state.EvidenceIntegrity <= 50);
            SetActiveSafe(integrityCriticalOverlay, state.EvidenceIntegrity <= 25);

            progressText.text = InvestigationProgressPresentation.Create(
                state.CompletedProductionSceneIds,
                ProductionSceneCatalog.All.Select(scene => scene.SceneId)).Label;

            if (trustRoot != null)
            {
                bool showTrust = !string.IsNullOrWhiteSpace(contextCharacter);
                trustRoot.SetActive(showTrust);
                if (showTrust)
                {
                    int trust = state.GetTrust(contextCharacter);
                    trustText.text = $"{contextCharacter}  신뢰  {trust}/5";
                    RefreshTrustPips(trust);
                }
            }
        }

        private void RenderDefaults()
        {
            if (timeText != null)
            {
                timeText.text = "DAY 1  ·  AM";
                anxietyText.text = "승객 불안  15/100";
                integrityText.text = "현장 보존도  100/100";
                progressText.text = "수사 진행  0/41";
                SetFill(anxietyFill, 15);
                SetFill(integrityFill, 100);
                SetActiveSafe(anxietyPanicOverlay, false);
                SetActiveSafe(integrityDamageOverlay, false);
                SetActiveSafe(integrityCriticalOverlay, false);
            }

            if (trustRoot != null)
            {
                trustRoot.SetActive(false);
            }
        }

        private static void SetFill(Image fill, int value)
        {
            if (fill == null)
            {
                return;
            }

            fill.fillAmount = Mathf.Clamp01(value / 100f);
        }

        private static void SetActiveSafe(GameObject go, bool active)
        {
            if (go != null && go.activeSelf != active)
            {
                go.SetActive(active);
            }
        }

        private void RefreshTrustPips(int trust)
        {
            if (trustPips == null)
            {
                return;
            }

            for (int i = 0; i < trustPips.Length; i++)
            {
                if (trustPips[i] == null)
                {
                    continue;
                }

                Sprite sprite = i < trust ? trustPipFilledSprite : trustPipEmptySprite;
                if (sprite != null)
                {
                    trustPips[i].sprite = sprite;
                    trustPips[i].preserveAspect = true;
                }
            }
        }

        private static bool IsPlayerCharacter(string characterName)
        {
            if (string.IsNullOrWhiteSpace(characterName))
            {
                return true;
            }

            string normalized = characterName.Trim().ToUpperInvariant();
            return normalized is "ADRIAN" or "ADRIAN VALE" or "CLAIRE";
        }

        private static Transform EnsurePanel(
            RectTransform parent,
            string name,
            float anchorMinX,
            float anchorMaxX)
        {
            GameObject panel = EnsureChild(parent, name, typeof(CanvasRenderer), typeof(Image));
            RectTransform rect = panel.GetComponent<RectTransform>();
            rect.anchorMin = new Vector2(anchorMinX, 0.08f);
            rect.anchorMax = new Vector2(anchorMaxX, 0.92f);
            rect.offsetMin = Vector2.zero;
            rect.offsetMax = Vector2.zero;
            panel.GetComponent<Image>().color = Panel;
            panel.GetComponent<Image>().raycastTarget = false;
            return rect;
        }

        private static Image EnsureIcon(Transform parent, string name, Sprite sprite)
        {
            if (sprite == null)
            {
                Transform existingNone = parent.Find(name);
                if (existingNone != null)
                {
                    existingNone.gameObject.SetActive(false);
                }
                return null;
            }

            GameObject iconObject = EnsureChild(parent, name, typeof(CanvasRenderer), typeof(Image));
            iconObject.SetActive(true);
            RectTransform rect = iconObject.GetComponent<RectTransform>();
            rect.anchorMin = new Vector2(0f, 1f);
            rect.anchorMax = new Vector2(0f, 1f);
            rect.pivot = new Vector2(0f, 1f);
            rect.anchoredPosition = new Vector2(14f, -8f);
            rect.sizeDelta = new Vector2(IconSize, IconSize);

            Image image = iconObject.GetComponent<Image>();
            image.sprite = sprite;
            image.type = Image.Type.Simple;
            image.preserveAspect = true;
            image.raycastTarget = false;
            return image;
        }

        private static void OffsetLabelForIcon(RectTransform labelRect, bool hasIcon)
        {
            float leftMargin = hasIcon ? IconSize + 30f : 22f;
            labelRect.offsetMin = new Vector2(leftMargin, 12f);
            labelRect.offsetMax = new Vector2(-22f, -12f);
        }

        private static TMP_Text EnsureText(
            Transform parent,
            string name,
            TextAlignmentOptions alignment,
            float fontSize)
        {
            GameObject textObject = EnsureChild(
                parent,
                name,
                typeof(CanvasRenderer),
                typeof(TextMeshProUGUI));
            RectTransform rect = textObject.GetComponent<RectTransform>();
            rect.anchorMin = Vector2.zero;
            rect.anchorMax = Vector2.one;
            rect.offsetMin = new Vector2(22f, 12f);
            rect.offsetMax = new Vector2(-22f, -12f);

            TextMeshProUGUI text = textObject.GetComponent<TextMeshProUGUI>();
            text.alignment = alignment;
            text.fontSize = fontSize;
            text.color = Paper;
            text.textWrappingMode = TextWrappingModes.NoWrap;
            text.raycastTarget = false;
            TypographyService.Apply(text, TypographyRole.Body);
            return text;
        }

        private void ApplyTypography()
        {
            StatusHUDTypography.Apply(
                timeText,
                anxietyText,
                integrityText,
                progressText,
                trustText,
                trustRoot != null ? trustRoot.transform : null);
        }

        private static Image EnsureMeterBar(Transform parent, string name, float height, Sprite fillSprite)
        {
            GameObject trackObject = EnsureChild(
                parent,
                name,
                typeof(CanvasRenderer),
                typeof(Image));
            RectTransform track = trackObject.GetComponent<RectTransform>();
            track.anchorMin = new Vector2(0.05f, 0.1f);
            track.anchorMax = new Vector2(0.95f, 0.1f + height);
            track.offsetMin = Vector2.zero;
            track.offsetMax = Vector2.zero;
            Image trackImage = trackObject.GetComponent<Image>();
            trackImage.color = new Color(0f, 0f, 0f, 0.48f);
            trackImage.raycastTarget = false;

            GameObject fillObject = EnsureChild(
                track,
                "Fill",
                typeof(CanvasRenderer),
                typeof(Image));
            RectTransform fill = fillObject.GetComponent<RectTransform>();
            fill.anchorMin = Vector2.zero;
            fill.anchorMax = Vector2.one;
            fill.offsetMin = Vector2.zero;
            fill.offsetMax = Vector2.zero;
            Image fillImage = fillObject.GetComponent<Image>();
            fillImage.raycastTarget = false;

            if (fillSprite != null)
            {
                fillImage.sprite = fillSprite;
                fillImage.type = Image.Type.Filled;
                fillImage.fillMethod = Image.FillMethod.Horizontal;
                fillImage.fillOrigin = (int)Image.OriginHorizontal.Left;
                fillImage.color = Color.white;
            }
            else
            {
                fillImage.color = new Color(0.24f, 0.67f, 0.64f, 1f);
                fillImage.type = Image.Type.Filled;
                fillImage.fillMethod = Image.FillMethod.Horizontal;
                fillImage.fillOrigin = (int)Image.OriginHorizontal.Left;
            }
            fillImage.fillAmount = 1f;

            return fillImage;
        }

        private static void EnsureMarker(Transform track, string name, Sprite sprite, float xFraction)
        {
            if (track == null || sprite == null)
            {
                return;
            }

            GameObject markerObject = EnsureChild(track, name, typeof(CanvasRenderer), typeof(Image));
            RectTransform rect = markerObject.GetComponent<RectTransform>();
            rect.anchorMin = new Vector2(xFraction, 0.5f);
            rect.anchorMax = new Vector2(xFraction, 0.5f);
            rect.pivot = new Vector2(0.5f, 0.5f);
            rect.anchoredPosition = Vector2.zero;
            rect.sizeDelta = new Vector2(MarkerSize, MarkerSize);

            Image image = markerObject.GetComponent<Image>();
            image.sprite = sprite;
            image.type = Image.Type.Simple;
            image.preserveAspect = true;
            image.raycastTarget = false;
        }

        private static GameObject EnsureOverlay(Transform parent, string name, Sprite sprite)
        {
            if (parent == null || sprite == null)
            {
                return null;
            }

            GameObject overlayObject = EnsureChild(parent, name, typeof(CanvasRenderer), typeof(Image));
            RectTransform rect = overlayObject.GetComponent<RectTransform>();
            rect.anchorMin = Vector2.zero;
            rect.anchorMax = Vector2.one;
            rect.offsetMin = Vector2.zero;
            rect.offsetMax = Vector2.zero;

            Image image = overlayObject.GetComponent<Image>();
            image.sprite = sprite;
            image.type = Image.Type.Simple;
            image.preserveAspect = false;
            image.raycastTarget = false;
            overlayObject.transform.SetAsLastSibling();
            overlayObject.SetActive(false);
            return overlayObject;
        }

        private Image[] EnsureTrustPips(Transform parent)
        {
            GameObject container = EnsureChild(parent, "Pips", typeof(RectTransform));
            RectTransform containerRect = container.GetComponent<RectTransform>();
            containerRect.anchorMin = new Vector2(0.5f, 0f);
            containerRect.anchorMax = new Vector2(0.5f, 0f);
            containerRect.pivot = new Vector2(0.5f, 0f);
            containerRect.anchoredPosition = new Vector2(0f, 4f);
            containerRect.sizeDelta = new Vector2(PipSize * 5f + 24f, PipSize);

            Image[] pips = new Image[5];
            for (int i = 0; i < pips.Length; i++)
            {
                GameObject pipObject = EnsureChild(
                    container.transform,
                    "Pip " + i,
                    typeof(CanvasRenderer),
                    typeof(Image));
                RectTransform rect = pipObject.GetComponent<RectTransform>();
                rect.anchorMin = new Vector2(0.5f, 0.5f);
                rect.anchorMax = new Vector2(0.5f, 0.5f);
                rect.pivot = new Vector2(0.5f, 0.5f);
                rect.sizeDelta = new Vector2(PipSize, PipSize);
                float spacing = PipSize + 6f;
                float startX = -spacing * 2f;
                rect.anchoredPosition = new Vector2(startX + spacing * i, 0f);

                Image image = pipObject.GetComponent<Image>();
                image.type = Image.Type.Simple;
                image.preserveAspect = true;
                image.raycastTarget = false;
                if (trustPipEmptySprite != null)
                {
                    image.sprite = trustPipEmptySprite;
                }
                pips[i] = image;
            }

            return pips;
        }

        private static GameObject EnsureChild(
            Transform parent,
            string name,
            params System.Type[] components)
        {
            Transform existing = parent.Find(name);
            GameObject child = existing != null
                ? existing.gameObject
                : new GameObject(name, typeof(RectTransform));
            if (existing == null)
            {
                child.transform.SetParent(parent, false);
                child.layer = parent.gameObject.layer;
            }

            foreach (System.Type component in components)
            {
                if (child.GetComponent(component) == null)
                {
                    child.AddComponent(component);
                }
            }
            return child;
        }

        private static T GetOrAdd<T>(GameObject target) where T : Component
        {
            T component = target.GetComponent<T>();
            return component != null ? component : target.AddComponent<T>();
        }

        private void ShowFeedback(string message)
        {
            ToastController.Instance?.Show(message);
        }

        private void ShowBadEnd(string message)
        {
            ToastController.Instance?.Show($"BAD END · {message}");
        }
    }
}
