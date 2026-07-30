using System;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;
using Wake.Core;
using Wake.Evidence;
using Wake.Exploration;

namespace Wake.UI
{
    [DisallowMultipleComponent]
    public sealed class InvestigationScreenController :
        MonoBehaviour,
        IRuntimeModalController
    {
        private readonly List<Button> pointButtons = new();
        private GameObject root;
        private RectTransform imageTransform;
        private Image closeupImage;
        private TMP_Text locationLabel;
        private TMP_Text titleLabel;
        private TMP_Text objectiveLabel;
        private TMP_Text observationLabel;
        private TMP_Text actionLabel;
        private Button actionButton;
        private InvestigationTargetDefinition target;
        private EvidenceDefinition evidence;
        private Action exitAction;
        private float zoom = 1f;
        private float rotation;

        public static InvestigationScreenController Instance { get; private set; }
        public bool IsOpen => root != null && root.activeSelf;
        public string ActiveTargetId => target?.TargetId ?? string.Empty;

        private void Awake()
        {
            Instance = this;
            Transform canvas = GameObject.Find("Canvas")?.transform;
            if (canvas != null)
                Initialize(canvas);
        }

        private void OnDestroy()
        {
            if (Instance == this)
                Instance = null;
        }

        public void Initialize(Transform canvas)
        {
            Instance = this;
            if (root != null || canvas == null)
                return;

            root = Create(
                "Investigation Screen",
                canvas,
                typeof(Canvas),
                typeof(GraphicRaycaster),
                typeof(Image));
            RectTransform rootRect = root.GetComponent<RectTransform>();
            Stretch(rootRect);
            Canvas overlayCanvas = root.GetComponent<Canvas>();
            overlayCanvas.overrideSorting = true;
            overlayCanvas.sortingOrder = 620;
            Image dimmer = root.GetComponent<Image>();
            dimmer.color = new Color32(3, 9, 17, 248);
            dimmer.raycastTarget = true;

            locationLabel = Text(
                root.transform,
                "Location",
                new Rect(.03f, .91f, .30f, .06f),
                23f,
                TextAlignmentOptions.Left,
                new Color32(207, 169, 96, 255));
            titleLabel = Text(
                root.transform,
                "Target",
                new Rect(.03f, .84f, .42f, .07f),
                38f,
                TextAlignmentOptions.Left,
                new Color32(248, 235, 207, 255));
            objectiveLabel = Text(
                root.transform,
                "Objective",
                new Rect(.34f, .91f, .38f, .06f),
                24f,
                TextAlignmentOptions.Center,
                new Color32(226, 228, 221, 255));

            GameObject viewport = Create(
                "Closeup Viewport",
                root.transform,
                typeof(Image),
                typeof(RectMask2D));
            SetRect(
                viewport.GetComponent<RectTransform>(),
                new Rect(.16f, .23f, .68f, .59f));
            viewport.GetComponent<Image>().color =
                new Color32(10, 23, 37, 255);

            GameObject imageObject = Create(
                "Closeup Image",
                viewport.transform,
                typeof(Image));
            imageTransform = imageObject.GetComponent<RectTransform>();
            Stretch(imageTransform, 18f);
            closeupImage = imageObject.GetComponent<Image>();
            closeupImage.preserveAspect = true;
            closeupImage.raycastTarget = false;

            observationLabel = Text(
                root.transform,
                "Adrian Observation",
                new Rect(.22f, .06f, .52f, .14f),
                25f,
                TextAlignmentOptions.TopLeft,
                new Color32(235, 231, 215, 255));
            observationLabel.textWrappingMode = TextWrappingModes.Normal;
            observationLabel.enableAutoSizing = true;
            observationLabel.fontSizeMin = 18f;
            observationLabel.fontSizeMax = 25f;

            Button zoomOut = Button(
                root.transform,
                "Zoom Out",
                "－",
                new Rect(.03f, .08f, .055f, .07f),
                () => SetZoom(zoom - .2f));
            Button zoomIn = Button(
                root.transform,
                "Zoom In",
                "＋",
                new Rect(.09f, .08f, .055f, .07f),
                () => SetZoom(zoom + .2f));
            Button rotate = Button(
                root.transform,
                "Rotate",
                "회전",
                new Rect(.03f, .16f, .115f, .055f),
                () => SetRotation(rotation + 90f));
            Button reset = Button(
                root.transform,
                "Reset",
                "초기화",
                new Rect(.03f, .02f, .115f, .05f),
                ResetView);
            zoomOut.navigation = Navigation.defaultNavigation;
            zoomIn.navigation = Navigation.defaultNavigation;
            rotate.navigation = Navigation.defaultNavigation;
            reset.navigation = Navigation.defaultNavigation;

            Button(
                root.transform,
                "Evidence Notebook",
                "조사 기록",
                new Rect(.78f, .91f, .10f, .06f),
                OpenNotebook);
            Button(
                root.transform,
                "Close",
                "닫기",
                new Rect(.89f, .91f, .08f, .06f),
                Exit);
            actionButton = Button(
                root.transform,
                "Primary Action",
                "조사 기록에 남기기",
                new Rect(.78f, .06f, .19f, .09f),
                CompleteOrExit);
            actionLabel =
                actionButton.GetComponentInChildren<TMP_Text>(true);

            root.SetActive(false);
        }

        public bool Begin(
            string evidenceId,
            Action onExit = null)
        {
            if (root == null ||
                !InvestigationTargetCatalog.TryGet(evidenceId, out target))
            {
                return false;
            }

            EvidenceInventory inventory = EvidenceInventory.Instance;
            evidence = inventory?.FindDefinition(evidenceId);
            if (evidence == null)
                return false;

            exitAction = onExit;
            locationLabel.text = LocationName(target.LocationCode);
            titleLabel.text = evidence.DisplayName;
            objectiveLabel.text = "세부 흔적을 확인해 관찰을 완성하세요";
            closeupImage.sprite =
                evidence.Views != null && evidence.Views.Length > 0
                    ? evidence.Views[0]
                    : null;
            observationLabel.text =
                IsRewardGranted()
                    ? "이미 조사 기록에 정리한 대상이다. 필요한 흔적을 다시 확인할 수 있다."
                    : "화면의 세부 지점에 포인터를 가까이 대거나 포커스를 이동해 조사하세요.";
            ResetView();
            BuildPoints();
            RefreshCompletion();
            root.transform.SetAsLastSibling();
            root.SetActive(true);
            EventSystem.current?.SetSelectedGameObject(
                pointButtons.Count > 0
                    ? pointButtons[0].gameObject
                    : actionButton.gameObject);
            return true;
        }

        private void BuildPoints()
        {
            foreach (Button button in pointButtons)
            {
                if (button != null)
                    Destroy(button.gameObject);
            }
            pointButtons.Clear();

            foreach (InspectionPointDefinition point in target.Points)
            {
                GameObject pointObject = Create(
                    $"Inspection Point {point.PointId}",
                    imageTransform,
                    typeof(Image),
                    typeof(Button),
                    typeof(Outline));
                RectTransform rect = pointObject.GetComponent<RectTransform>();
                rect.anchorMin = point.NormalizedRect.min;
                rect.anchorMax = point.NormalizedRect.max;
                rect.offsetMin = rect.offsetMax = Vector2.zero;
                Image image = pointObject.GetComponent<Image>();
                bool inspected = IsInspected(point.PointId);
                image.color = inspected
                    ? new Color(83f / 255f, 139f / 255f, 145f / 255f, .10f)
                    : ExplorationHotspotFeedback.AccessibilityIndicatorsEnabled
                        ? new Color(1f, .79f, .31f, .10f)
                        : new Color(1f, .79f, .31f, .001f);
                Button button = pointObject.GetComponent<Button>();
                ColorBlock colors = button.colors;
                colors.normalColor = Color.white;
                colors.highlightedColor =
                    new Color(1f, .86f, .48f, inspected ? .28f : .18f);
                colors.selectedColor = colors.highlightedColor;
                colors.pressedColor = new Color(1f, .69f, .24f, .32f);
                colors.fadeDuration = .08f;
                button.colors = colors;
                button.onClick.AddListener(() => Inspect(point, image));
                Outline outline = pointObject.GetComponent<Outline>();
                outline.effectColor = new Color(1f, .78f, .30f, .65f);
                outline.effectDistance = Vector2.zero;
                pointObject.AddComponent<ExplorationHotspotFeedback>()
                    .Configure();
                pointButtons.Add(button);
            }
        }

        private void Inspect(
            InspectionPointDefinition point,
            Image pointImage)
        {
            bool repeated = IsInspected(point.PointId);
            if (!repeated)
            {
                GameStateManager.Instance?.AddFlagSilently(
                    InvestigationTargetCatalog.PointFlag(
                        target,
                        point.PointId));
            }
            pointImage.color =
                new Color(83f / 255f, 139f / 255f, 145f / 255f, .10f);
            observationLabel.text = repeated
                ? $"다시 확인했다. {point.Observation}"
                : point.Observation;
            RefreshCompletion();
        }

        private void RefreshCompletion()
        {
            bool complete = IsInvestigationComplete();
            bool rewarded = IsRewardGranted();
            actionButton.interactable = complete || rewarded;
            actionLabel.text = rewarded
                ? "현장으로 돌아가기"
                : complete
                    ? "조사 기록에 남기기"
                    : "관찰을 더 확인하세요";
        }

        private void CompleteOrExit()
        {
            if (IsRewardGranted())
            {
                Exit();
                return;
            }
            if (!IsInvestigationComplete())
                return;

            bool added = EvidenceInventory.Instance?.Add(evidence) == true;
            GameStateManager.Instance?.AddFlagSilently(
                InvestigationTargetCatalog.CompletionFlag(target));
            observationLabel.text = added
                ? "관찰한 사실과 해석을 분리해 조사 기록에 정리했다."
                : "이 기록은 이미 정리되어 있다.";
            CheckSealedExitConclusion();
            RefreshCompletion();
        }

        private bool IsInvestigationComplete() =>
            target != null &&
            target.IsComplete(IsInspected);

        private bool IsInspected(string pointId) =>
            GameStateManager.Instance?.HasFlag(
                InvestigationTargetCatalog.PointFlag(target, pointId)) == true;

        private bool IsRewardGranted() =>
            target != null &&
            (EvidenceInventory.Instance?.Contains(target.EvidenceId) == true ||
             GameStateManager.Instance?.HasFlag(
                 InvestigationTargetCatalog.CompletionFlag(target)) == true);

        private void CheckSealedExitConclusion()
        {
            EvidenceInventory inventory = EvidenceInventory.Instance;
            GameStateManager state = GameStateManager.Instance;
            if (inventory == null || state == null ||
                state.HasFlag("sealed_exits_observations_complete") ||
                !inventory.Contains("C-03") ||
                !inventory.Contains("C-04") ||
                !inventory.Contains("C-05"))
            {
                return;
            }

            state.AddFlagSilently("sealed_exits_observations_complete");
            ToastController.Instance?.Show(
                "관찰 완료 · 세 출구의 흔적을 비교할 수 있습니다");
        }

        private void OpenNotebook()
        {
            if (target == null)
                return;
            if (EvidenceInventory.Instance?.Contains(target.EvidenceId) == true)
            {
                string evidenceId = target.EvidenceId;
                Exit();
                UIManager.Instance?.ShowEvidence(evidenceId);
                return;
            }
            ToastController.Instance?.Show(
                "관찰을 완료해 기록으로 정리한 뒤 열람할 수 있습니다.");
        }

        private void Exit()
        {
            root.SetActive(false);
            target = null;
            evidence = null;
            Action callback = exitAction;
            exitAction = null;
            callback?.Invoke();
        }

        public void Close() => Exit();

        private void SetZoom(float value)
        {
            zoom = Mathf.Clamp(value, 1f, 2f);
            imageTransform.localScale = Vector3.one * zoom;
        }

        private void SetRotation(float value)
        {
            rotation = Mathf.Repeat(value, 360f);
            imageTransform.localRotation =
                Quaternion.Euler(0f, 0f, rotation);
        }

        private void ResetView()
        {
            zoom = 1f;
            rotation = 0f;
            if (imageTransform == null)
                return;
            imageTransform.localScale = Vector3.one;
            imageTransform.localRotation = Quaternion.identity;
            imageTransform.anchoredPosition = Vector2.zero;
        }

        private static string LocationName(string locationCode) =>
            CanonicalLocationCatalog.FindSpec(locationCode)
                ?.DisplayName ?? locationCode;

        private static GameObject Create(
            string name,
            Transform parent,
            params Type[] components)
        {
            Type[] types = new Type[components.Length + 2];
            types[0] = typeof(RectTransform);
            types[1] = typeof(CanvasRenderer);
            Array.Copy(components, 0, types, 2, components.Length);
            var result = new GameObject(name, types);
            result.transform.SetParent(parent, false);
            return result;
        }

        private static TMP_Text Text(
            Transform parent,
            string name,
            Rect anchors,
            float size,
            TextAlignmentOptions alignment,
            Color color)
        {
            GameObject item = Create(
                name,
                parent,
                typeof(TextMeshProUGUI));
            SetRect(item.GetComponent<RectTransform>(), anchors);
            TMP_Text text = item.GetComponent<TMP_Text>();
            text.fontSize = size;
            text.alignment = alignment;
            text.color = color;
            text.raycastTarget = false;
            TypographyService.Apply(text, TypographyRole.Body);
            return text;
        }

        private static Button Button(
            Transform parent,
            string name,
            string label,
            Rect anchors,
            Action action)
        {
            GameObject item = Create(
                name,
                parent,
                typeof(Image),
                typeof(Button));
            SetRect(item.GetComponent<RectTransform>(), anchors);
            Image image = item.GetComponent<Image>();
            image.color = new Color32(190, 151, 82, 245);
            Button button = item.GetComponent<Button>();
            button.targetGraphic = image;
            button.onClick.AddListener(() => action?.Invoke());
            TMP_Text text = Text(
                item.transform,
                "Label",
                new Rect(0f, 0f, 1f, 1f),
                22f,
                TextAlignmentOptions.Center,
                new Color32(10, 20, 31, 255));
            text.text = label;
            TypographyService.Apply(text, TypographyRole.Choice);
            return button;
        }

        private static void SetRect(RectTransform rect, Rect anchors)
        {
            rect.anchorMin = anchors.min;
            rect.anchorMax = anchors.max;
            rect.offsetMin = rect.offsetMax = Vector2.zero;
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
