using System.Text;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using Seat0A.Core;

namespace Seat0A.UI
{
    [ExecuteAlways]
    public class StatusHUDController : MonoBehaviour
    {
        private const string KoreanGlyphWarmup =
            "승객 불안 현장 보존도 활성 가설 신뢰 획득 경고 제한구역 폐쇄 " +
            "새 수사를 시작합니다 핵심 증거가 파괴되었습니다 슬롯이 가득 찼습니다 " +
            "월화수목금토일 오전오후밤 0123456789+-/·●○!";

        private static readonly Color Navy = new(0.035f, 0.075f, 0.12f, 0.96f);
        private static readonly Color Panel = new(0.075f, 0.13f, 0.18f, 0.96f);
        private static readonly Color Paper = new(0.91f, 0.87f, 0.76f, 1f);
        private static readonly Color Teal = new(0.24f, 0.67f, 0.64f, 1f);
        private static readonly Color Amber = new(0.9f, 0.61f, 0.24f, 1f);
        private static readonly Color Red = new(0.79f, 0.22f, 0.24f, 1f);
        private static TMP_FontAsset koreanFont;

        private TMP_Text timeText;
        private TMP_Text anxietyText;
        private TMP_Text integrityText;
        private TMP_Text theoryText;
        private TMP_Text trustText;
        private Image anxietyFill;
        private Image integrityFill;
        private GameObject trustRoot;
        private GameStateService state;
        private string contextCharacter;

        public static TMP_FontAsset RuntimeKoreanFont => GetKoreanFont();

        private void OnEnable()
        {
            BuildWireframe();
            ApplyKoreanFont();
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
                ApplyKoreanFont();
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
            root.sizeDelta = new Vector2(0f, 58f);

            Image background = GetOrAdd<Image>(gameObject);
            background.color = Navy;
            background.raycastTarget = false;

            Transform timePanel = EnsurePanel(root, "Time Badge", 0.01f, 0.17f);
            timeText = EnsureText(timePanel, "Value", TextAlignmentOptions.Center, 18f);

            Transform anxietyPanel = EnsurePanel(root, "Anxiety Indicator", 0.18f, 0.42f);
            anxietyText = EnsureText(anxietyPanel, "Label", TextAlignmentOptions.TopLeft, 15f);
            anxietyFill = EnsureBar(anxietyPanel, "Bar", 0.15f);

            Transform integrityPanel = EnsurePanel(root, "Integrity Indicator", 0.43f, 0.67f);
            integrityText = EnsureText(integrityPanel, "Label", TextAlignmentOptions.TopLeft, 15f);
            integrityFill = EnsureBar(integrityPanel, "Bar", 0.15f);

            Transform theoryPanel = EnsurePanel(root, "Theory Slots", 0.68f, 0.99f);
            theoryText = EnsureText(theoryPanel, "Label", TextAlignmentOptions.Center, 16f);

            Transform portraitFrame = transform.parent?.Find("Ingame/Line Panel/Image");
            if (portraitFrame != null)
            {
                trustRoot = EnsureChild(
                    portraitFrame,
                    "Context Trust",
                    typeof(CanvasRenderer),
                    typeof(Image));
                RectTransform trustRect = trustRoot.GetComponent<RectTransform>();
                trustRect.anchorMin = new Vector2(0f, 0f);
                trustRect.anchorMax = new Vector2(1f, 0f);
                trustRect.pivot = new Vector2(0.5f, 1f);
                trustRect.anchoredPosition = new Vector2(0f, -4f);
                trustRect.sizeDelta = new Vector2(0f, 32f);
                trustRoot.GetComponent<Image>().color = Navy;
                trustText = EnsureText(
                    trustRect,
                    "Trust Label",
                    TextAlignmentOptions.Center,
                    14f);
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
            if (state == GameStateService.Instance && state != null)
            {
                Refresh();
                return;
            }

            UnbindState();
            state = GameStateService.Instance;
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

            ApplyKoreanFont();

            if (state == null)
            {
                state = GameStateService.Instance;
            }

            if (state == null)
            {
                RenderDefaults();
                return;
            }

            GameStateData data = state.Data;
            timeText.text = $"DAY {data.day}  ·  {data.timeBlock}";

            anxietyText.text = data.publicAnxiety >= GameStateService.RestrictedAreaAnxiety
                ? $"! 승객 불안  {data.publicAnxiety}/100"
                : $"승객 불안  {data.publicAnxiety}/100";
            SetBar(anxietyFill, data.publicAnxiety, true);

            integrityText.text = data.evidenceIntegrity == 0
                ? "! 현장 보존도  0/100"
                : $"현장 보존도  {data.evidenceIntegrity}/100";
            SetBar(integrityFill, data.evidenceIntegrity, false);

            theoryText.text =
                $"활성 가설  {data.activeTheories.Count}/{data.theorySlots}    {BuildSlots(data.activeTheories.Count, data.theorySlots)}";

            if (trustRoot != null)
            {
                bool showTrust = !string.IsNullOrWhiteSpace(contextCharacter);
                trustRoot.SetActive(showTrust);
                if (showTrust)
                {
                    int trust = state.GetTrust(contextCharacter);
                    trustText.text = $"{contextCharacter}  신뢰  {BuildSlots(trust, GameStateService.MaxTrust)}  {trust}/5";
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
                theoryText.text = "활성 가설  0/3    ○ ○ ○";
                SetBar(anxietyFill, 15, true);
                SetBar(integrityFill, 100, false);
            }

            if (trustRoot != null)
            {
                trustRoot.SetActive(false);
            }
        }

        private static void SetBar(Image fill, int value, bool dangerIncreases)
        {
            if (fill == null)
            {
                return;
            }

            float normalized = Mathf.Clamp01(value / 100f);
            RectTransform rect = fill.rectTransform;
            rect.anchorMax = new Vector2(normalized, 1f);
            rect.offsetMin = Vector2.zero;
            rect.offsetMax = Vector2.zero;

            if (dangerIncreases)
            {
                fill.color = value >= 100 ? Red : value >= 70 ? Amber : Teal;
            }
            else
            {
                fill.color = value <= 0 ? Red : value <= 25 ? Red : value <= 50 ? Amber : Teal;
            }
        }

        private static string BuildSlots(int filled, int total)
        {
            StringBuilder builder = new();
            for (int i = 0; i < total; i++)
            {
                if (i > 0)
                {
                    builder.Append(' ');
                }
                builder.Append(i < filled ? '●' : '○');
            }
            return builder.ToString();
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
            rect.anchorMin = new Vector2(anchorMinX, 0.13f);
            rect.anchorMax = new Vector2(anchorMaxX, 0.87f);
            rect.offsetMin = Vector2.zero;
            rect.offsetMax = Vector2.zero;
            panel.GetComponent<Image>().color = Panel;
            panel.GetComponent<Image>().raycastTarget = false;
            return rect;
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
            rect.offsetMin = new Vector2(8f, 4f);
            rect.offsetMax = new Vector2(-8f, -4f);

            TextMeshProUGUI text = textObject.GetComponent<TextMeshProUGUI>();
            text.alignment = alignment;
            text.fontSize = fontSize;
            text.color = Paper;
            text.textWrappingMode = TextWrappingModes.NoWrap;
            text.raycastTarget = false;
            TMP_FontAsset runtimeFont = GetKoreanFont();
            if (runtimeFont != null)
            {
                text.font = runtimeFont;
            }
            else if (text.font == null && TMP_Settings.defaultFontAsset != null)
            {
                text.font = TMP_Settings.defaultFontAsset;
            }
            return text;
        }

        private static TMP_FontAsset GetKoreanFont()
        {
            if (koreanFont != null)
            {
                return koreanFont;
            }

            string[] familyNames =
            {
                "Malgun Gothic",
                "Noto Sans CJK KR",
                "Apple SD Gothic Neo",
                "Arial Unicode MS"
            };

            foreach (string familyName in familyNames)
            {
                TMP_FontAsset candidate = TMP_FontAsset.CreateFontAsset(
                    familyName,
                    "Regular",
                    90);
                if (candidate == null)
                {
                    continue;
                }

                candidate.hideFlags = HideFlags.DontSave;
                if (candidate.material != null)
                {
                    candidate.material.hideFlags = HideFlags.DontSave;
                }
                foreach (Texture2D atlasTexture in candidate.atlasTextures)
                {
                    if (atlasTexture != null)
                    {
                        atlasTexture.hideFlags = HideFlags.DontSave;
                    }
                }
                candidate.atlasPopulationMode = AtlasPopulationMode.Dynamic;
                candidate.isMultiAtlasTexturesEnabled = true;
                bool added = candidate.TryAddCharacters(
                    KoreanGlyphWarmup,
                    out string missingCharacters);
                if (added && string.IsNullOrEmpty(missingCharacters))
                {
                    koreanFont = candidate;
                    koreanFont.name = $"Runtime Korean HUD Font ({familyName})";
                    return koreanFont;
                }

                if (Application.isPlaying)
                {
                    Destroy(candidate);
                }
                else
                {
                    DestroyImmediate(candidate);
                }
            }

            Debug.LogWarning("No Korean-capable OS font was found for the status HUD.");
            return TMP_Settings.defaultFontAsset;
        }

        private void ApplyKoreanFont()
        {
            TMP_FontAsset runtimeFont = GetKoreanFont();
            if (runtimeFont == null)
            {
                return;
            }

            TMP_Text[] labels = GetComponentsInChildren<TMP_Text>(true);
            foreach (TMP_Text label in labels)
            {
                label.font = runtimeFont;
                label.SetAllDirty();
            }

            if (trustRoot != null)
            {
                TMP_Text[] trustLabels = trustRoot.GetComponentsInChildren<TMP_Text>(true);
                foreach (TMP_Text label in trustLabels)
                {
                    label.font = runtimeFont;
                    label.SetAllDirty();
                }
            }
        }

        private static Image EnsureBar(Transform parent, string name, float height)
        {
            GameObject trackObject = EnsureChild(
                parent,
                name,
                typeof(CanvasRenderer),
                typeof(Image));
            RectTransform track = trackObject.GetComponent<RectTransform>();
            track.anchorMin = new Vector2(0.05f, 0.12f);
            track.anchorMax = new Vector2(0.95f, 0.12f + height);
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
            return fillImage;
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
            ToastController.Instance?.Show($"BAD END 위험 · {message}");
        }
    }
}
