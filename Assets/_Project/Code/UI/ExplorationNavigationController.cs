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
        private UIManager owner;
        private GameObject root;
        private CanvasGroup canvasGroup;
        private TMP_Text locationLabel;
        private TMP_Text timeLabel;
        private TMP_Text contextLabel;
        private TMP_Text objectiveLabel;
        private TMP_Text objectiveDetail;
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

        public GameObject Root => root;
        public TMP_Text LocationLabel => locationLabel;
        public TMP_Text ObjectiveLabel => objectiveLabel;
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
                RefreshContext(panel);
                RefreshObjective(panel);
            }
            SetSelectedState(mapButton, panel == UiPrimaryPanel.Map);
            SetSelectedState(
                evidenceButton,
                panel == UiPrimaryPanel.Evidence);

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

            canvasGroup.alpha = presented ? 1f : 0f;
            canvasGroup.interactable = presented && interactionEnabled;
            canvasGroup.blocksRaycasts = presented && interactionEnabled;
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
            CreateContext(contextRegion);

            objectiveRegion = CreateRegion(
                root.transform,
                "Exploration Objective",
                ScreenRegionIds.ObjectiveTop);
            CreateObjective(objectiveRegion);

            globalRegion = CreateRegion(
                root.transform,
                "Global Navigation",
                ScreenRegionIds.GlobalTopRight);
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
                36f);
            contextLabel = CreateText(
                parent,
                "Location Context",
                UiTextStyle.Caption,
                26f);
        }

        private void CreateObjective(RectTransform parent)
        {
            VerticalLayoutGroup layout =
                parent.gameObject.AddComponent<VerticalLayoutGroup>();
            layout.padding = new RectOffset(22, 22, 10, 10);
            layout.spacing = 2f;
            layout.childAlignment = TextAnchor.MiddleLeft;
            layout.childControlWidth = true;
            layout.childControlHeight = true;
            layout.childForceExpandWidth = true;
            layout.childForceExpandHeight = false;

            objectiveLabel = CreateText(
                parent,
                "Current Objective",
                UiTextStyle.Body,
                34f);
            objectiveDetail = CreateText(
                parent,
                "Objective Detail",
                UiTextStyle.Caption,
                24f);
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
                Resources.Load<Sprite>("UI/Icons/Navigation/ui_icon_nav_map"));
            evidenceButton =
                CreateButton(
                    parent,
                    "조사 기록",
                    owner != null ? owner.ShowEvidence : null,
                    Resources.Load<Sprite>(
                        "UI/Icons/Navigation/ui_icon_nav_evidence"),
                    iconPadding: 20f);
            pauseButton =
                CreateButton(
                    parent,
                    "일시정지",
                    owner != null ? owner.OpenPause : null);
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

        private void RefreshContext(UiPrimaryPanel panel)
        {
            GameStateManager state = GameStateManager.Instance;
            timeLabel.text = state != null
                ? ResolveWorldTimeLabel(state)
                : "DAY 1 · 오전";
            switch (panel)
            {
                case UiPrimaryPanel.Map:
                    locationLabel.text = "선내 지도";
                    contextLabel.text = "이동할 장소를 선택하세요";
                    break;
                case UiPrimaryPanel.Evidence:
                    locationLabel.text = "조사 기록";
                    contextLabel.text = "확보한 기록을 검토하세요";
                    break;
                default:
                    LocationDefinition location =
                        LocationLoader.Instance?.CurrentLocation;
                    CanonicalLocationSpec fallback =
                        CanonicalLocationCatalog.FindSpec(
                            ResolveLocationCode());
                    locationLabel.text =
                        location != null &&
                        !string.IsNullOrWhiteSpace(location.DisplayName)
                            ? location.DisplayName
                            : fallback?.DisplayName ?? "현재 장소";
                    contextLabel.text = "주변을 살펴보고 단서를 찾으세요";
                    break;
            }
        }

        private void RefreshObjective(UiPrimaryPanel panel)
        {
            if (panel == UiPrimaryPanel.Map)
            {
                objectiveLabel.text = "다음 조사 장소 선택";
                objectiveDetail.text =
                    "잠긴 장소는 사건 진행 후 열립니다";
                return;
            }
            if (panel == UiPrimaryPanel.Evidence)
            {
                objectiveLabel.text = "기록과 인물의 연결 확인";
                objectiveDetail.text =
                    "코드와 수집률 대신 사건의 맥락을 확인하세요";
                return;
            }

            ProductionObjectiveItem? current =
                ProductionObjectiveViewModel
                    .Resolve(GameStateManager.Instance)
                    .Current;
            if (current.HasValue)
            {
                objectiveLabel.text = current.Value.Definition.Title;
                objectiveDetail.text =
                    current.Value.Definition.Description;
            }
            else
            {
                objectiveLabel.text = "자유 조사";
                objectiveDetail.text =
                    "인물과 사물을 직접 선택해 조사하세요";
            }
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
                string minuteLabel = minute > 0
                    ? $" {minute}분"
                    : string.Empty;
                return
                    $"DAY {scene.Day} · {TimeBlockLabel(scene.TimeBlock)} " +
                    $"{displayHour}시{minuteLabel}";
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
            UiVisualThemeService.ApplySurface(
                target.GetComponent<Image>(),
                UiSurfaceStyle.Overlay);
            return rect;
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

            if (icon != null)
            {
                GameObject iconObject = new(
                    "Icon",
                    typeof(RectTransform),
                    typeof(Image));
                iconObject.transform.SetParent(target.transform, false);
                Stretch(iconObject.GetComponent<RectTransform>(), iconPadding);
                Image iconImage = iconObject.GetComponent<Image>();
                iconImage.sprite = icon;
                iconImage.preserveAspect = true;
                iconImage.raycastTarget = false;
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
            UiVisualThemeService.ApplyButton(
                button,
                UiButtonStyle.Secondary);
            if (action != null)
                button.onClick.AddListener(action);
            target.AddComponent<UiHoverFeedback>();
            return button;
        }

        private static void SetSelectedState(Button button, bool selected)
        {
            if (button == null)
                return;
            button.interactable = !selected;
        }

        private static void Stretch(RectTransform rect, float inset = 0f)
        {
            rect.anchorMin = Vector2.zero;
            rect.anchorMax = Vector2.one;
            rect.offsetMin = new Vector2(inset, inset);
            rect.offsetMax = new Vector2(-inset, -inset);
        }
    }
}
