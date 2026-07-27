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
        private static readonly Color ResolvedColor =
            new(0.08f, 0.16f, 0.21f, 0.94f);
        private static readonly Color WarningColor =
            new(0.32f, 0.18f, 0.08f, 0.96f);

        private GameObject root;
        private TMP_Text label;

        public NarrativeLocationContext CurrentContext { get; private set; }
        public bool IsWarningVisible =>
            root != null &&
            root.activeSelf &&
            CurrentContext.IsDialogueOnly;

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
            if (root == null)
            {
                BuildUi();
            }

            bool hasContext =
                CurrentContext.Kind != NarrativeLocationKind.Undocumented;
            root.SetActive(hasContext);
            if (!hasContext)
            {
                return;
            }

            label.text = CurrentContext.IsDialogueOnly
                ? $"⚠ {CurrentContext.DisplayName}\n" +
                  CurrentContext.WarningMessage
                : $"장소 · {CurrentContext.DisplayName}";
            root.GetComponent<Image>().color =
                CurrentContext.IsDialogueOnly
                    ? WarningColor
                    : ResolvedColor;
        }

        public void Clear()
        {
            CurrentContext = default;
            root?.SetActive(false);
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
            rect.anchorMin = new Vector2(0f, 1f);
            rect.anchorMax = new Vector2(0f, 1f);
            rect.pivot = new Vector2(0f, 1f);
            rect.anchoredPosition = new Vector2(20f, -184f);
            rect.sizeDelta = new Vector2(430f, 76f);

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
            label.fontSize = 21f;
            label.alignment = TextAlignmentOptions.Center;
            label.textWrappingMode = TextWrappingModes.Normal;
            label.raycastTarget = false;
            root.SetActive(false);
        }
    }
}
