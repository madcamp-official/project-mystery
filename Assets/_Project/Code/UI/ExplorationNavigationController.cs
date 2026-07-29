using System.Collections;
using System.Linq;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using Wake.Core;
using Wake.Exploration;
using Wake.Narrative;

namespace Wake.UI
{
    [DisallowMultipleComponent]
    public sealed class ExplorationNavigationController : MonoBehaviour
    {
        private static readonly Color HudGold =
            new(0.89f, 0.72f, 0.35f, 1f);
        private static readonly Color HudIvory =
            new(0.95f, 0.92f, 0.84f, 1f);
        private static readonly Color HudOutline =
            new(0.015f, 0.035f, 0.06f, 0.96f);
        private static readonly Color HudDim =
            new(0.008f, 0.02f, 0.04f, 0.86f);
        private const string ObjectiveMarker =
            "<color=#E3B859>◆</color>  ";

        private UIManager owner;
        private GameObject root;
        private CanvasGroup canvasGroup;
        private CanvasGroup pauseGroup;
        private TMP_Text locationLabel;
        private TMP_Text timeLabel;
        private TMP_Text objectiveEyebrow;
        private TMP_Text objectiveLabel;
        private TMP_Text guidanceEyebrow;
        private TMP_Text guidanceLabel;
        private Button mapButton;
        private Button evidenceButton;
        private Button pauseButton;
        private RectTransform contextRegion;
        private RectTransform objectiveRegion;
        private RectTransform globalRegion;
        private UiPrimaryPanel renderedPanel = UiPrimaryPanel.None;
        private string renderedLocationCode = string.Empty;
        private string renderedObjectiveKey = string.Empty;
        private string renderedTemporalKey = string.Empty;
        private bool interactionEnabled = true;
        private bool presentationSuppressed;
        private bool wasPresented;
        private Coroutine presentationRoutine;
        private const float SlideInDuration = 0.35f;
        private const float SlideInDistance = 70f;

        public GameObject Root => root;
        public TMP_Text LocationLabel => locationLabel;
        public TMP_Text ObjectiveLabel => objectiveLabel;
        public TMP_Text GuidanceLabel => guidanceLabel;
        public Button MapButton => mapButton;
        public Button EvidenceButton => evidenceButton;
        public Button PauseButton => pauseButton;

        public void Configure(UIManager uiManager)
        {
            owner = uiManager;
            BuildUi();
            BindNavigationActions();
            UnbindRuntime();
            BindRuntime();
            Refresh(true);
        }

        private void OnEnable()
        {
            BuildUi();
            BindRuntime();
            Refresh(true);
        }

        private void OnDisable()
        {
            UnbindRuntime();
            // Coroutines are killed by Unity when this GameObject
            // disables, so this reference would otherwise dangle and
            // the next OnEnable would think the HUD was already showing.
            presentationRoutine = null;
            wasPresented = false;
        }

        private void LateUpdate()
        {
            if (owner != null &&
                (renderedPanel != owner.ActivePanel ||
                 renderedLocationCode != ResolveLocationCode() ||
                 renderedObjectiveKey != ResolveObjectiveKey() ||
                 renderedTemporalKey != ResolveTemporalKey()))
            {
                Refresh();
            }
        }

        public void Refresh(bool force = false)
        {
            if (owner == null || root == null)
                return;

            UiPrimaryPanel panel = owner.ActivePanel;
            bool visible =
                panel == UiPrimaryPanel.Ingame ||
                panel == UiPrimaryPanel.Map ||
                panel == UiPrimaryPanel.Evidence;
            bool presented = visible && !presentationSuppressed;
            if (!root.activeSelf)
                root.SetActive(true);
            ApplyCanvasGroupState(presented);
            renderedPanel = panel;
            renderedLocationCode = ResolveLocationCode();
            renderedObjectiveKey = ResolveObjectiveKey();
            renderedTemporalKey = ResolveTemporalKey();
            if (!presented ||
                !root.activeInHierarchy ||
                root.transform.parent == null ||
                !root.transform.parent.gameObject.activeInHierarchy)
                return;

            root.transform.SetAsLastSibling();
            bool showExplorationHud = panel == UiPrimaryPanel.Ingame;
            contextRegion?.gameObject.SetActive(showExplorationHud);
            objectiveRegion?.gameObject.SetActive(showExplorationHud);
            globalRegion?.gameObject.SetActive(true);
            if (showExplorationHud)
            {
                RefreshContext();
                RefreshObjective();
                RefreshGuidance();
            }
            if (force)
                Canvas.ForceUpdateCanvases();
        }

        public void SetInteractionEnabled(bool enabled)
        {
            interactionEnabled = enabled;
            ApplyCanvasGroupState(IsPresentedForCurrentPanel());
        }

        public void SetPresentationSuppressed(bool suppressed)
        {
            presentationSuppressed = suppressed;
            Refresh(true);
        }

        private bool IsPresentedForCurrentPanel()
        {
            if (owner == null || presentationSuppressed)
                return false;

            return owner.ActivePanel == UiPrimaryPanel.Ingame ||
                   owner.ActivePanel == UiPrimaryPanel.Map ||
                   owner.ActivePanel == UiPrimaryPanel.Evidence;
        }

        private void ApplyCanvasGroupState(bool presented)
        {
            if (canvasGroup == null)
                return;

            canvasGroup.interactable = presented && interactionEnabled;
            canvasGroup.blocksRaycasts = presented && interactionEnabled;
            // The pause button ignores this group (see CreateGlobalNavigation)
            // so it can stay clickable while interactionEnabled is false
            // during its own pause overlay - but it must still track
            // presented alone, or it stays visible/clickable outside
            // Ingame/Map/Evidence too (e.g. the title/lobby screen).
            if (pauseGroup != null)
            {
                pauseGroup.interactable = presented;
                pauseGroup.blocksRaycasts = presented;
            }

            bool risingEdge = presented && !wasPresented;
            wasPresented = presented;

            if (!presented)
            {
                if (presentationRoutine != null)
                {
                    StopCoroutine(presentationRoutine);
                    presentationRoutine = null;
                }
                SetAlpha(0f);
                RectTransform rootRect = root.transform as RectTransform;
                if (rootRect != null)
                {
                    rootRect.anchoredPosition = Vector2.zero;
                }
                return;
            }

            if (!risingEdge)
            {
                SetAlpha(1f);
                return;
            }

            // Slides down from above rather than snapping in, most
            // noticeably when dialogue ends and this HUD reappears.
            if (presentationRoutine != null)
            {
                StopCoroutine(presentationRoutine);
            }
            presentationRoutine = StartCoroutine(SlideIn());
        }

        private void SetAlpha(float value)
        {
            canvasGroup.alpha = value;
            if (pauseGroup != null)
            {
                pauseGroup.alpha = value;
            }
        }

        private IEnumerator SlideIn()
        {
            RectTransform rootRect = root.transform as RectTransform;
            float elapsed = 0f;
            while (elapsed < SlideInDuration)
            {
                elapsed += Time.unscaledDeltaTime;
                float t = Mathf.SmoothStep(
                    0f, 1f, Mathf.Clamp01(elapsed / SlideInDuration));
                SetAlpha(t);
                if (rootRect != null)
                {
                    rootRect.anchoredPosition =
                        new Vector2(0f, SlideInDistance * (1f - t));
                }
                yield return null;
            }
            SetAlpha(1f);
            if (rootRect != null)
            {
                rootRect.anchoredPosition = Vector2.zero;
            }
            presentationRoutine = null;
        }

        private void BuildUi()
        {
            if (root != null)
                return;

            RectTransform canvas = GameObject.Find("Canvas")
                ?.GetComponent<RectTransform>();
            if (canvas == null)
                return;

            root = new GameObject(
                "Exploration Global Navigation",
                typeof(RectTransform),
                typeof(CanvasGroup));
            root.transform.SetParent(canvas, false);
            RectTransform rootRect = root.GetComponent<RectTransform>();
            Stretch(rootRect);
            canvasGroup = root.GetComponent<CanvasGroup>();

            contextRegion = CreateRegion(
                root.transform,
                "Exploration Context",
                ScreenRegionIds.ContextTopLeft);
            ApplyDim(contextRegion, DimDirection.Left);
            CreateContext(contextRegion);

            objectiveRegion = CreateRegion(
                root.transform,
                "Objective Guidance",
                ScreenRegionIds.ObjectiveTop);
            ApplyDim(objectiveRegion, DimDirection.Center);
            CreateGuidance(objectiveRegion);

            globalRegion = CreateRegion(
                root.transform,
                "Global Navigation",
                ScreenRegionIds.GlobalTopRight);
            ApplyDim(globalRegion, DimDirection.Right);
            CreateGlobalNavigation(globalRegion);
        }

        private void CreateContext(RectTransform parent)
        {
            VerticalLayoutGroup layout =
                parent.gameObject.AddComponent<VerticalLayoutGroup>();
            layout.padding = new RectOffset(20, 20, 12, 12);
            layout.spacing = 2f;
            layout.childAlignment = TextAnchor.MiddleLeft;
            layout.childControlWidth = true;
            layout.childControlHeight = true;
            layout.childForceExpandWidth = true;
            layout.childForceExpandHeight = false;

            timeLabel = CreateText(
                parent,
                "World Time",
                UiTextStyle.Technical,
                28f);
            locationLabel = CreateText(
                parent,
                "Current Location",
                UiTextStyle.Heading,
                40f);
            locationLabel.color = HudGold;
            locationLabel.textWrappingMode = TextWrappingModes.NoWrap;
            locationLabel.enableAutoSizing = true;
            locationLabel.fontSizeMin = 24f;
            locationLabel.fontSizeMax = 42f;
            objectiveEyebrow = CreateText(
                parent,
                "Objective Eyebrow",
                UiTextStyle.Caption,
                20f);
            objectiveEyebrow.text = "메인 목표";
            objectiveEyebrow.color = HudGold;
            objectiveLabel = CreateText(
                parent,
                "Current Objective",
                UiTextStyle.Body,
                32f);
        }

        private void CreateGuidance(RectTransform parent)
        {
            VerticalLayoutGroup layout =
                parent.gameObject.AddComponent<VerticalLayoutGroup>();
            layout.padding = new RectOffset(96, 96, 16, 14);
            layout.spacing = 3f;
            layout.childAlignment = TextAnchor.UpperCenter;
            layout.childControlWidth = true;
            layout.childControlHeight = true;
            layout.childForceExpandWidth = true;
            layout.childForceExpandHeight = false;

            CreateGoldLine(parent, "Top Gold Line");
            guidanceEyebrow = CreateText(
                parent,
                "Guidance Eyebrow",
                UiTextStyle.Caption,
                22f);
            guidanceEyebrow.text = "목표";
            guidanceEyebrow.color = HudGold;
            guidanceEyebrow.alignment = TextAlignmentOptions.Center;
            guidanceLabel = CreateText(
                parent,
                "Current Guidance",
                UiTextStyle.Body,
                38f);
            guidanceLabel.alignment = TextAlignmentOptions.Top;
            guidanceLabel.textWrappingMode = TextWrappingModes.NoWrap;
            guidanceLabel.enableAutoSizing = true;
            guidanceLabel.fontSizeMin = 19f;
            guidanceLabel.fontSizeMax = 30f;
            CreateGoldLine(parent, "Bottom Gold Line");
        }

        private void CreateGlobalNavigation(RectTransform parent)
        {
            HorizontalLayoutGroup layout =
                parent.gameObject.AddComponent<HorizontalLayoutGroup>();
            layout.padding = new RectOffset(8, 8, 8, 8);
            layout.spacing = 12f;
            layout.childAlignment = TextAnchor.MiddleRight;
            layout.childControlWidth = false;
            layout.childControlHeight = true;
            layout.childForceExpandWidth = false;
            layout.childForceExpandHeight = true;

            mapButton = CreateButton(
                parent,
                "지도",
                owner != null ? owner.ShowMap : null,
                LoadNavigationIcon(
                    "UI/Icons/Navigation/ui_icon_nav_map_outline",
                    "UI/Icons/Navigation/ui_icon_nav_map"));
            evidenceButton =
                CreateButton(
                    parent,
                    "조사 기록",
                    owner != null ? owner.ShowEvidence : null,
                    LoadNavigationIcon(
                        "UI/Icons/Navigation/ui_icon_nav_evidence_outline",
                        "UI/Icons/Navigation/ui_icon_nav_evidence"),
                    iconPadding: 10f);
            pauseButton =
                CreateButton(
                    parent,
                    "일시정지",
                    owner != null ? owner.OpenPause : null,
                    LoadNavigationIcon(
                        "UI/Icons/Navigation/ui_icon_nav_pause_outline",
                        "UI/Icons/Badges/ui_badge_settings"),
                    iconPadding: 12f);
            // Unlike map/evidence, the pause button must stay clickable
            // while paused so it can toggle pause back off - a nested
            // CanvasGroup ignoring the shared nav bar group (which
            // SetInteractionEnabled(false) disables while any system
            // screen is open) keeps it interactable regardless.
            // ApplyCanvasGroupState still drives its alpha/presented state
            // directly (see SetAlpha), so it stays hidden outside
            // Ingame/Map/Evidence rather than showing everywhere.
            pauseGroup = pauseButton.gameObject.AddComponent<CanvasGroup>();
            pauseGroup.ignoreParentGroups = true;
            pauseGroup.interactable = true;
            pauseGroup.blocksRaycasts = true;
        }

        private void BindNavigationActions()
        {
            if (owner == null)
                return;

            mapButton?.onClick.RemoveAllListeners();
            mapButton?.onClick.AddListener(owner.ShowMap);
            evidenceButton?.onClick.RemoveAllListeners();
            evidenceButton?.onClick.AddListener(owner.ShowEvidence);
            pauseButton?.onClick.RemoveAllListeners();
            pauseButton?.onClick.AddListener(owner.OpenPause);
        }

        private void RefreshContext()
        {
            GameStateManager state = GameStateManager.Instance;
            timeLabel.text = state != null
                ? ResolveWorldTimeLabel(state)
                : "DAY 1 · 오전";
            locationLabel.text = ResolveLocationDisplayName();
        }

        private void RefreshObjective()
        {
            ProductionObjectiveItem? current =
                ProductionObjectiveViewModel
                    .Resolve(GameStateManager.Instance)
                    .Current;
            if (current.HasValue)
            {
                objectiveLabel.text =
                    ObjectiveMarker + current.Value.Definition.Title;
            }
            else
            {
                objectiveLabel.text = ObjectiveMarker + "자유 조사";
            }
        }

        private void RefreshGuidance()
        {
            ProductionObjectiveViewModel view =
                ProductionObjectiveViewModel.Resolve(
                    GameStateManager.Instance);
            ProductionObjectivePresentation? presentation =
                view.Presentation;
            if (!presentation.HasValue)
            {
                guidanceLabel.text = "주변을 조사하세요";
                return;
            }

            ProductionObjectiveDefinition definition =
                presentation.Value.Definition;
            guidanceLabel.text = presentation.Value.IsTravel
                ? ToGuidanceSentence(presentation.Value.DisplayText)
                : definition.Steps.FirstOrDefault() ??
                  presentation.Value.DisplayText;
        }

        private string ResolveLocationDisplayName()
        {
            LocationDefinition loaded =
                LocationLoader.Instance?.CurrentLocation;
            if (!string.IsNullOrWhiteSpace(loaded?.DisplayName))
                return loaded.DisplayName.Trim();

            string locationCode = ResolveLocationCode();
            CanonicalLocationSpec canonical =
                CanonicalLocationCatalog.FindSpec(locationCode);
            if (!string.IsNullOrWhiteSpace(canonical?.DisplayName))
                return canonical.DisplayName.Trim();

            string sceneId =
                DialogueController.Instance?.ActiveProductionSceneId;
            if (string.IsNullOrWhiteSpace(sceneId))
            {
                sceneId = ProductionObjectiveViewModel
                    .Resolve(GameStateManager.Instance)
                    .Current?
                    .Definition
                    .SceneId;
            }
            if (!string.IsNullOrWhiteSpace(sceneId) &&
                ProductionSceneCatalog.TryGet(
                    sceneId,
                    out ProductionSceneDefinition scene))
            {
                canonical = CanonicalLocationCatalog.FindSpec(
                    scene.NarrativeLocationCode);
                if (!string.IsNullOrWhiteSpace(canonical?.DisplayName))
                    return canonical.DisplayName.Trim();
            }

            return "위치 정보 없음";
        }

        private static string ToGuidanceSentence(string value)
        {
            string text = value?.Trim() ?? string.Empty;
            if (string.IsNullOrEmpty(text))
                return "주변을 조사하세요";

            if (text.EndsWith("향하기"))
                return text.Substring(0, text.Length - 3) + "향하세요";
            return text;
        }

        private void BindRuntime()
        {
            if (GameStateManager.Instance != null)
                GameStateManager.Instance.StateChanged += HandleStateChanged;
            if (LocationLoader.Instance != null)
                LocationLoader.Instance.LocationChanged += HandleLocationChanged;
        }

        private void UnbindRuntime()
        {
            if (GameStateManager.Instance != null)
                GameStateManager.Instance.StateChanged -= HandleStateChanged;
            if (LocationLoader.Instance != null)
                LocationLoader.Instance.LocationChanged -= HandleLocationChanged;
        }

        private void HandleStateChanged() => Refresh();

        private void HandleLocationChanged(LocationDefinition location) =>
            Refresh();

        private string ResolveLocationCode()
        {
            return LocationLoader.Instance?.CurrentLocation?.LocationCode ??
                   GameStateManager.Instance?.CurrentLocationCode ??
                   string.Empty;
        }

        private static string ResolveObjectiveKey()
        {
            ProductionObjectiveItem? current =
                ProductionObjectiveViewModel
                    .Resolve(GameStateManager.Instance)
                    .Current;
            return current?.Definition.SceneId ?? string.Empty;
        }

        private static string ResolveTemporalKey()
        {
            GameStateManager state = GameStateManager.Instance;
            return state == null
                ? string.Empty
                : ResolveWorldTimeLabel(state);
        }

        private static string ResolveWorldTimeLabel(
            GameStateManager state)
        {
            string sceneId =
                DialogueController.Instance?.ActiveProductionSceneId;
            if (string.IsNullOrWhiteSpace(sceneId))
            {
                sceneId = ProductionObjectiveViewModel
                    .Resolve(state)
                    .Current?
                    .Definition
                    .SceneId;
            }

            if (!string.IsNullOrWhiteSpace(sceneId) &&
                ProductionSceneCatalog.TryGet(
                    sceneId,
                    out ProductionSceneDefinition scene))
            {
                int hour = scene.MinuteOfDay / 60;
                int minute = scene.MinuteOfDay % 60;
                int displayHour = hour > 12 ? hour - 12 : hour;
                if (displayHour == 0)
                    displayHour = 12;
                return
                    $"DAY {scene.Day} · {TimeBlockLabel(scene.TimeBlock)} " +
                    $"{displayHour}:{minute:00}";
            }

            return
                $"DAY {state.Day} · {TimeBlockLabel(state.CurrentTimeBlock)}";
        }

        private static string TimeBlockLabel(TimeBlock block) =>
            block switch
            {
                TimeBlock.AM => "오전",
                TimeBlock.PM => "오후",
                TimeBlock.NIGHT => "야간",
                _ => string.Empty
            };

        private static RectTransform CreateRegion(
            Transform parent,
            string name,
            string slotId)
        {
            GameObject target = new(
                name,
                typeof(RectTransform),
                typeof(CanvasRenderer),
                typeof(Image));
            target.transform.SetParent(parent, false);
            RectTransform rect = target.GetComponent<RectTransform>();
            if (!RuntimeUiLayoutRegistry.CopyWorldLayout(rect, slotId))
                Stretch(rect);
            Image hitArea = target.GetComponent<Image>();
            hitArea.color = Color.clear;
            hitArea.raycastTarget = false;
            return rect;
        }

        private static void ApplyDim(
            RectTransform region,
            DimDirection direction)
        {
            if (region == null)
                return;

            GameObject background = new(
                "Dim Background",
                typeof(RectTransform),
                typeof(CanvasRenderer),
                typeof(Image),
                typeof(LayoutElement));
            background.transform.SetParent(region, false);
            background.transform.SetAsFirstSibling();
            RectTransform rect =
                background.GetComponent<RectTransform>();
            rect.anchorMin = Vector2.zero;
            rect.anchorMax = Vector2.one;
            rect.offsetMin = direction switch
            {
                DimDirection.Left => new Vector2(-48f, -72f),
                DimDirection.Right => new Vector2(-150f, -72f),
                _ => new Vector2(-90f, -72f)
            };
            rect.offsetMax = direction switch
            {
                DimDirection.Left => new Vector2(150f, 28f),
                DimDirection.Right => new Vector2(48f, 28f),
                _ => new Vector2(90f, 28f)
            };
            LayoutElement layout =
                background.GetComponent<LayoutElement>();
            layout.ignoreLayout = true;
            Image image = background.GetComponent<Image>();
            image.sprite = HudDimSpriteCache.Get(direction);
            image.type = Image.Type.Simple;
            image.color = HudDim;
            image.raycastTarget = false;
        }

        private static void CreateGoldLine(Transform parent, string name)
        {
            GameObject target = new(
                name,
                typeof(RectTransform),
                typeof(CanvasRenderer),
                typeof(Image),
                typeof(LayoutElement));
            target.transform.SetParent(parent, false);
            Image image = target.GetComponent<Image>();
            image.color = new Color(
                HudGold.r,
                HudGold.g,
                HudGold.b,
                0.82f);
            image.raycastTarget = false;
            LayoutElement layout = target.GetComponent<LayoutElement>();
            layout.preferredHeight = 1.5f;
            layout.minHeight = 1.5f;
        }

        private static TMP_Text CreateText(
            Transform parent,
            string name,
            UiTextStyle style,
            float preferredHeight)
        {
            GameObject target = new(
                name,
                typeof(RectTransform),
                typeof(TextMeshProUGUI),
                typeof(LayoutElement));
            target.transform.SetParent(parent, false);
            TMP_Text text = target.GetComponent<TMP_Text>();
            UiVisualThemeService.ApplyText(text, style);
            text.color = HudIvory;
            text.outlineColor = HudOutline;
            text.outlineWidth = 0.16f;
            text.extraPadding = true;
            text.textWrappingMode = TextWrappingModes.Normal;
            text.overflowMode = TextOverflowModes.Ellipsis;
            text.alignment = TextAlignmentOptions.MidlineLeft;
            text.raycastTarget = false;
            target.GetComponent<LayoutElement>().preferredHeight =
                preferredHeight;
            return text;
        }

        private static Button CreateButton(
            Transform parent,
            string label,
            UnityEngine.Events.UnityAction action,
            Sprite icon = null,
            float iconPadding = 8f)
        {
            GameObject target = new(
                $"{label} 버튼",
                typeof(RectTransform),
                typeof(CanvasRenderer),
                typeof(Image),
                typeof(Button),
                typeof(LayoutElement));
            target.transform.SetParent(parent, false);
            LayoutElement element = target.GetComponent<LayoutElement>();
            if (icon != null)
            {
                element.preferredWidth = 64f;
                element.minWidth = 64f;
            }
            else
            {
                element.preferredWidth = label.Length > 3 ? 190f : 150f;
                element.minWidth = 120f;
            }

            Image iconGraphic = null;
            if (icon != null)
            {
                GameObject iconObject = new(
                    "Icon",
                    typeof(RectTransform),
                    typeof(Image));
                iconObject.transform.SetParent(target.transform, false);
                Stretch(iconObject.GetComponent<RectTransform>(), iconPadding);
                iconGraphic = iconObject.GetComponent<Image>();
                iconGraphic.sprite = icon;
                iconGraphic.preserveAspect = true;
                iconGraphic.raycastTarget = false;
            }
            else
            {
                GameObject textObject = new(
                    "Label",
                    typeof(RectTransform),
                    typeof(TextMeshProUGUI));
                textObject.transform.SetParent(target.transform, false);
                Stretch(textObject.GetComponent<RectTransform>(), 12f);
                TMP_Text text = textObject.GetComponent<TMP_Text>();
                text.text = label;
                text.alignment = TextAlignmentOptions.Center;
                text.raycastTarget = false;
            }

            Button button = target.GetComponent<Button>();
            Image hitArea = target.GetComponent<Image>();
            hitArea.color = Color.clear;
            hitArea.raycastTarget = true;
            button.targetGraphic =
                iconGraphic != null ? iconGraphic : hitArea;
            button.transition = Selectable.Transition.ColorTint;
            ColorBlock colors = button.colors;
            colors.normalColor = Color.white;
            colors.highlightedColor = new Color(1f, 0.96f, 0.82f, 1f);
            colors.pressedColor = new Color(0.78f, 0.61f, 0.28f, 1f);
            colors.selectedColor = colors.highlightedColor;
            colors.disabledColor = new Color(0.72f, 0.62f, 0.42f, 0.48f);
            colors.colorMultiplier = 1f;
            colors.fadeDuration = 0.08f;
            button.colors = colors;
            if (action != null)
                button.onClick.AddListener(action);
            target.AddComponent<UiHoverFeedback>();
            return button;
        }

        private static Sprite LoadNavigationIcon(
            string preferredPath,
            string fallbackPath)
        {
            Sprite preferred = Resources.Load<Sprite>(preferredPath);
            return preferred != null
                ? preferred
                : Resources.Load<Sprite>(fallbackPath);
        }
        private static void Stretch(RectTransform rect, float inset = 0f)
        {
            rect.anchorMin = Vector2.zero;
            rect.anchorMax = Vector2.one;
            rect.offsetMin = new Vector2(inset, inset);
            rect.offsetMax = new Vector2(-inset, -inset);
        }

        private enum DimDirection
        {
            Left,
            Center,
            Right
        }

        private static class HudDimSpriteCache
        {
            private const int Width = 64;
            private const int Height = 32;
            private static readonly Sprite[] Sprites = new Sprite[3];

            public static Sprite Get(DimDirection direction)
            {
                int index = (int)direction;
                if (Sprites[index] == null)
                    Sprites[index] = Create(direction);
                return Sprites[index];
            }

            private static Sprite Create(DimDirection direction)
            {
                Texture2D texture = new(
                    Width,
                    Height,
                    TextureFormat.RGBA32,
                    false)
                {
                    name = $"HUD Dim {direction}",
                    filterMode = FilterMode.Bilinear,
                    wrapMode = TextureWrapMode.Clamp,
                    hideFlags = HideFlags.HideAndDontSave
                };
                Color32[] pixels = new Color32[Width * Height];
                for (int y = 0; y < Height; y++)
                {
                    float vertical = Mathf.SmoothStep(
                        0f,
                        1f,
                        y / (Height - 1f));
                    for (int x = 0; x < Width; x++)
                    {
                        float normalizedX = x / (Width - 1f);
                        float horizontal = direction switch
                        {
                            DimDirection.Left =>
                                1f - Mathf.SmoothStep(
                                    0.15f,
                                    1f,
                                    normalizedX),
                            DimDirection.Right =>
                                Mathf.SmoothStep(
                                    0f,
                                    0.85f,
                                    normalizedX),
                            _ => 1f - Mathf.SmoothStep(
                                0.1f,
                                0.5f,
                                Mathf.Abs(normalizedX - 0.5f))
                        };
                        byte alpha = (byte)Mathf.RoundToInt(
                            255f * horizontal * vertical);
                        pixels[y * Width + x] =
                            new Color32(255, 255, 255, alpha);
                    }
                }
                texture.SetPixels32(pixels);
                texture.Apply(false, false);
                Sprite sprite = Sprite.Create(
                    texture,
                    new Rect(0f, 0f, Width, Height),
                    new Vector2(0.5f, 0.5f),
                    100f);
                sprite.name = $"HUD Dim {direction}";
                sprite.hideFlags = HideFlags.HideAndDontSave;
                return sprite;
            }
        }
    }
}
