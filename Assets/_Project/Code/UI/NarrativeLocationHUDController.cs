using TMPro;
using UnityEngine;
using UnityEngine.UI;
using Wake.Core;
using Wake.Exploration;
using Wake.Narrative;

namespace Wake.UI
{
    [DisallowMultipleComponent]
    public sealed class NarrativeLocationHUDController : MonoBehaviour
    {
        private GameObject root;
        private TMP_Text label;

        public NarrativeLocationContext CurrentContext { get; private set; }
        public NarrativeLocationHUDViewModel CurrentPresentation
        {
            get;
            private set;
        }
        public NarrativeLocationHUDLayout CurrentLayout { get; private set; }
        public bool IsWarningVisible =>
            root != null &&
            root.activeSelf &&
            CurrentPresentation.IsWarning;

        private void OnEnable()
        {
            BuildUi();
            InvestigationEventHub.Published += HandleInvestigationEvent;
            RefreshFromRuntime();
        }

        private void OnDisable()
        {
            InvestigationEventHub.Published -= HandleInvestigationEvent;
        }

        public void ShowScene(string sceneId)
        {
            CurrentContext =
                NarrativeLocationContextResolver.Resolve(sceneId);
            CurrentPresentation =
                NarrativeLocationHUDPresentation.Create(CurrentContext);
            if (root == null)
            {
                BuildUi();
            }

            root.SetActive(CurrentPresentation.IsVisible);
            if (!CurrentPresentation.IsVisible)
            {
                return;
            }

            label.text = CurrentPresentation.DisplayText;
            root.GetComponent<Image>().color =
                CurrentPresentation.BackgroundColor;
        }

        public void Clear()
        {
            CurrentContext = default;
            CurrentPresentation = default;
            root?.SetActive(false);
        }

        private void OnRectTransformDimensionsChange()
        {
            UpdateLayout();
        }

        private void HandleInvestigationEvent(
            InvestigationEvent investigationEvent)
        {
            if (investigationEvent.Kind == InvestigationEventKind.SceneEntered)
            {
                ShowScene(investigationEvent.SubjectId);
            }
        }

        private void RefreshFromRuntime()
        {
            string sceneId = DialogueController.Instance?
                .ActiveProductionSceneId;
            if (string.IsNullOrEmpty(sceneId))
            {
                sceneId = GameStateManager.Instance?
                    .DialogueCheckpoint?
                    .activeSceneId;
            }

            ShowScene(sceneId);
        }

        private void BuildUi()
        {
            if (root != null)
            {
                return;
            }

            root = new GameObject(
                "Narrative Location Context",
                typeof(RectTransform),
                typeof(CanvasRenderer),
                typeof(Image));
            root.transform.SetParent(transform, false);

            RectTransform rect = root.GetComponent<RectTransform>();
            rect.anchorMin = new Vector2(0.5f, 1f);
            rect.anchorMax = new Vector2(0.5f, 1f);
            rect.pivot = new Vector2(0.5f, 1f);
            UpdateLayout();

            root.GetComponent<Image>().raycastTarget = false;
            GameObject labelObject = new(
                "Label",
                typeof(RectTransform),
                typeof(CanvasRenderer),
                typeof(TextMeshProUGUI));
            labelObject.transform.SetParent(root.transform, false);
            RectTransform labelRect =
                labelObject.GetComponent<RectTransform>();
            labelRect.anchorMin = Vector2.zero;
            labelRect.anchorMax = Vector2.one;
            labelRect.offsetMin = new Vector2(12f, 6f);
            labelRect.offsetMax = new Vector2(-12f, -6f);
            label = labelObject.GetComponent<TMP_Text>();
            MapTypography.ApplyLocation(label);
            label.fontSize = 18f;
            label.alignment = TextAlignmentOptions.Center;
            label.textWrappingMode = TextWrappingModes.Normal;
            label.raycastTarget = false;
            root.SetActive(false);
        }

        private void UpdateLayout()
        {
            if (root == null ||
                transform is not RectTransform parentRect)
            {
                return;
            }

            if (RuntimeUiLayoutRegistry.CopyLayout(
                    root.GetComponent<RectTransform>(),
                    "hud.location"))
            {
                return;
            }

            float viewportWidth = parentRect.rect.width;
            float safeRatio = Screen.width > 0
                ? Screen.safeArea.width / Screen.width
                : 1f;
            CurrentLayout =
                NarrativeLocationHUDPresentation.CalculateLayout(
                    viewportWidth,
                    viewportWidth * safeRatio);
            RectTransform rect = root.GetComponent<RectTransform>();
            rect.anchoredPosition =
                new Vector2(0f, -CurrentLayout.TopOffset);
            rect.sizeDelta =
                new Vector2(CurrentLayout.Width, CurrentLayout.Height);
        }
    }
}
