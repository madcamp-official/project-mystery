using System;
using System.Collections.Generic;
using System.Linq;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using Wake.Core;
using Wake.Narrative;

namespace Wake.UI
{
    public readonly struct ObjectiveHudItem
    {
        public ObjectiveHudItem(
            string id,
            string title,
            int completedCount,
            int requiredCount,
            bool isCompleted)
        {
            Id = id ?? string.Empty;
            Title = title ?? string.Empty;
            CompletedCount = completedCount;
            RequiredCount = requiredCount;
            IsCompleted = isCompleted;
        }

        public string Id { get; }
        public string Title { get; }
        public int CompletedCount { get; }
        public int RequiredCount { get; }
        public bool IsCompleted { get; }
        public string StateIcon => IsCompleted ? "✓" : "●";
        public string ProgressLabel => IsCompleted
            ? "완료"
            : $"{CompletedCount}/{RequiredCount}";
        public string AccessibilityLabel => IsCompleted
            ? $"완료: {Title}"
            : $"진행 중: {Title}, {ProgressLabel}";
    }

    public sealed class ObjectiveHudViewModel
    {
        private ObjectiveHudViewModel(
            IReadOnlyList<ObjectiveHudItem> items,
            ObjectiveHudItem? current)
        {
            Items = items;
            Current = current;
        }

        public IReadOnlyList<ObjectiveHudItem> Items { get; }
        public ObjectiveHudItem? Current { get; }
        public int CompletedCount => Items.Count(item => item.IsCompleted);
        public int TotalCount => Items.Count;
        public string Summary => TotalCount == 0
            ? "조사 목표 없음"
            : $"전체 목표 {CompletedCount}/{TotalCount}";

        public static ObjectiveHudViewModel Create(
            IEnumerable<ObjectiveProgress> progressItems)
        {
            var progressById = (progressItems ?? Array.Empty<ObjectiveProgress>())
                .Where(item => item != null)
                .ToDictionary(
                    item => item.Definition.Id,
                    item => item,
                    StringComparer.Ordinal);
            ObjectiveHudItem[] items = InvestigationObjectiveCatalog.All
                .Where(definition => progressById.ContainsKey(definition.Id))
                .Select(definition => ToItem(progressById[definition.Id]))
                .ToArray();
            ObjectiveHudItem? current = items
                .Where(item => !item.IsCompleted)
                .Select(item => (ObjectiveHudItem?)item)
                .FirstOrDefault();
            if (!current.HasValue && items.Length > 0)
            {
                current = items[items.Length - 1];
            }

            return new ObjectiveHudViewModel(items, current);
        }

        private static ObjectiveHudItem ToItem(ObjectiveProgress progress)
        {
            return new ObjectiveHudItem(
                progress.Definition.Id,
                progress.Definition.Title,
                progress.CompletedRequirementCount,
                progress.RequirementCount,
                progress.IsCompleted);
        }
    }

    [DisallowMultipleComponent]
    public sealed class ObjectiveMapHUDController : MonoBehaviour
    {
        private static readonly Color PanelColor = new(0.035f, 0.075f, 0.12f, 0.94f);
        private static readonly Color Gold = new(0.88f, 0.68f, 0.32f, 1f);
        private static readonly Color Complete = new(0.30f, 0.74f, 0.56f, 1f);

        private GameObject root;
        private TMP_Text stateIcon;
        private TMP_Text titleText;
        private TMP_Text progressText;
        private TMP_Text accessibilityText;
        private GameObject detailPanel;
        private TMP_Text detailListText;
        private InvestigationObjectiveTracker tracker;
        private GameStateManager state;
        private bool hasRenderableObjective;
        private bool detailsExpanded;
        private string renderedObjectiveId = string.Empty;

        public ObjectiveHudViewModel CurrentViewModel { get; private set; }
        public ProductionObjectiveViewModel CurrentProductionViewModel
        {
            get;
            private set;
        }

        private void OnEnable()
        {
            BuildUi();
            BindState();
        }

        private void OnDisable()
        {
            UnbindState();
        }

        private void LateUpdate()
        {
            if (state != GameStateManager.Instance)
            {
                BindState();
            }
            UpdateHudVisibility();
        }

        public void Refresh()
        {
            if (state != null)
            {
                RenderProduction();
                return;
            }

            if (tracker == null)
            {
                RenderEmpty();
                return;
            }

            CurrentViewModel = ObjectiveHudViewModel.Create(tracker.Progress);
            if (!CurrentViewModel.Current.HasValue)
            {
                RenderEmpty();
                return;
            }

            ObjectiveHudItem current = CurrentViewModel.Current.Value;
            stateIcon.text = current.StateIcon;
            stateIcon.color = current.IsCompleted ? Complete : Gold;
            titleText.text = current.Title;
            progressText.text =
                $"{current.ProgressLabel} · {CurrentViewModel.Summary}";
            accessibilityText.text = current.AccessibilityLabel;
            hasRenderableObjective = true;
            UpdateHudVisibility();
        }

        private void BindState()
        {
            GameStateManager candidate = GameStateManager.Instance;
            if (candidate == null)
            {
                RenderEmpty();
                return;
            }

            if (candidate == state && tracker != null)
            {
                Refresh();
                return;
            }

            UnbindState();
            state = candidate;
            tracker = new InvestigationObjectiveTracker(state);
            state.StateChanged += HandleStateChanged;
            tracker.ProgressChanged += HandleProgressChanged;
            tracker.ObjectiveCompleted += HandleObjectiveCompleted;
            Refresh();
        }

        private void UnbindState()
        {
            if (state != null)
            {
                state.StateChanged -= HandleStateChanged;
            }

            if (tracker != null)
            {
                tracker.ProgressChanged -= HandleProgressChanged;
                tracker.ObjectiveCompleted -= HandleObjectiveCompleted;
                tracker.Dispose();
            }

            tracker = null;
            state = null;
        }

        private void HandleStateChanged()
        {
            Refresh();
        }

        private void HandleProgressChanged(ObjectiveProgress progress)
        {
            Refresh();
            InvestigationFeedback feedback =
                InvestigationFeedbackCatalog.ForObjective(progress);
            ToastController.Instance?.Show(feedback.Message);
        }

        private void HandleObjectiveCompleted(
            InvestigationObjectiveDefinition definition)
        {
            Refresh();
            InvestigationFeedback feedback =
                InvestigationFeedbackCatalog.ForObjectiveCompleted(definition);
            ToastController.Instance?.Show(
                $"{feedback.Title}\n{feedback.Message}");
        }

        private void BuildUi()
        {
            if (root != null)
            {
                return;
            }

            Transform canvas = GameObject.Find("Canvas")?.transform;
            Transform parent = canvas?.Find("Ingame");
            if (parent == null)
            {
                return;
            }

            Transform existing = parent.Find("Objective HUD");
            root = existing != null
                ? existing.gameObject
                : new GameObject(
                    "Objective HUD",
                    typeof(RectTransform),
                    typeof(CanvasRenderer),
                    typeof(Image));
            if (existing == null)
            {
                root.transform.SetParent(parent, false);
            }

            RectTransform rect = root.GetComponent<RectTransform>();
            rect.anchorMin = new Vector2(0.5f, 1f);
            rect.anchorMax = new Vector2(0.5f, 1f);
            rect.pivot = new Vector2(0.5f, 1f);
            rect.anchoredPosition = new Vector2(0f, -180f);
            rect.sizeDelta = new Vector2(660f, 92f);
            if (!RuntimeUiLayoutRegistry.CopyWorldLayout(
                    rect,
                    ScreenRegionIds.ObjectiveTop))
            {
                rect.anchorMin = new Vector2(0f, 1f);
                rect.anchorMax = new Vector2(0f, 1f);
                rect.pivot = new Vector2(0f, 1f);
                rect.anchoredPosition = new Vector2(32f, -32f);
                rect.sizeDelta = new Vector2(660f, 92f);
            }

            Image background = root.GetComponent<Image>();
            background.color = PanelColor;
            background.raycastTarget = true;
            Button toggle = root.GetComponent<Button>() ??
                            root.AddComponent<Button>();
            toggle.targetGraphic = background;
            toggle.transition = Selectable.Transition.None;
            toggle.onClick.RemoveAllListeners();
            toggle.onClick.AddListener(ToggleDetails);

            stateIcon = EnsureText(
                root.transform,
                "State",
                new Vector2(0f, 0f),
                new Vector2(0.12f, 1f),
                34f,
                TextAlignmentOptions.Center);
            titleText = EnsureText(
                root.transform,
                "Title",
                new Vector2(0.12f, 0.42f),
                new Vector2(0.70f, 1f),
                24f,
                TextAlignmentOptions.Left);
            progressText = EnsureText(
                root.transform,
                "Progress",
                new Vector2(0.70f, 0f),
                new Vector2(1f, 1f),
                17f,
                TextAlignmentOptions.Center);
            accessibilityText = EnsureText(
                root.transform,
                "Accessibility",
                new Vector2(0.12f, 0f),
                new Vector2(0.70f, 0.42f),
                16f,
                TextAlignmentOptions.Left);
            accessibilityText.color = new Color(0.82f, 0.84f, 0.86f, 1f);
            BuildDetailPanel();
            ApplyKoreanFont();
        }

        private void BuildDetailPanel()
        {
            Transform existing = root.transform.Find("Objective Details");
            detailPanel = existing != null
                ? existing.gameObject
                : new GameObject(
                    "Objective Details",
                    typeof(RectTransform),
                    typeof(CanvasRenderer),
                    typeof(Image));
            if (existing == null)
            {
                detailPanel.transform.SetParent(root.transform, false);
            }

            RectTransform panelRect =
                detailPanel.GetComponent<RectTransform>();
            panelRect.anchorMin = new Vector2(0f, 0f);
            panelRect.anchorMax = new Vector2(1f, 0f);
            panelRect.pivot = new Vector2(0.5f, 1f);
            panelRect.anchoredPosition = new Vector2(0f, -8f);
            panelRect.sizeDelta = new Vector2(0f, 156f);
            Image panelBackground = detailPanel.GetComponent<Image>();
            panelBackground.color =
                new Color(PanelColor.r, PanelColor.g, PanelColor.b, 0.97f);
            panelBackground.raycastTarget = false;

            detailListText = EnsureText(
                detailPanel.transform,
                "List",
                Vector2.zero,
                Vector2.one,
                18f,
                TextAlignmentOptions.TopLeft);
            detailListText.textWrappingMode = TextWrappingModes.Normal;
            detailListText.lineSpacing = 8f;
            detailPanel.SetActive(false);
        }

        private static TMP_Text EnsureText(
            Transform parent,
            string name,
            Vector2 anchorMin,
            Vector2 anchorMax,
            float fontSize,
            TextAlignmentOptions alignment)
        {
            Transform existing = parent.Find(name);
            GameObject target = existing != null
                ? existing.gameObject
                : new GameObject(
                    name,
                    typeof(RectTransform),
                    typeof(CanvasRenderer),
                    typeof(TextMeshProUGUI));
            if (existing == null)
            {
                target.transform.SetParent(parent, false);
            }

            RectTransform rect = target.GetComponent<RectTransform>();
            rect.anchorMin = anchorMin;
            rect.anchorMax = anchorMax;
            rect.offsetMin = new Vector2(12f, 6f);
            rect.offsetMax = new Vector2(-12f, -6f);

            TMP_Text text = target.GetComponent<TMP_Text>();
            text.fontSize = fontSize;
            text.alignment = alignment;
            text.color = Color.white;
            text.textWrappingMode = TextWrappingModes.NoWrap;
            text.raycastTarget = false;
            return text;
        }

        private void ApplyKoreanFont()
        {
            MapTypography.ApplyObjective(
                root != null ? root.transform : null,
                titleText,
                progressText,
                accessibilityText);
        }

        private void RenderEmpty()
        {
            CurrentProductionViewModel = null;
            CurrentViewModel =
                ObjectiveHudViewModel.Create(Array.Empty<ObjectiveProgress>());
            if (root == null)
            {
                return;
            }

            hasRenderableObjective = false;
            root.SetActive(false);
        }

        private void RenderProduction()
        {
            CurrentProductionViewModel =
                ProductionObjectiveViewModel.Resolve(state);
            if (root == null ||
                !CurrentProductionViewModel.Presentation.HasValue)
            {
                RenderEmpty();
                return;
            }

            ProductionObjectivePresentation presentation =
                CurrentProductionViewModel.Presentation.Value;
            if (!string.Equals(
                    renderedObjectiveId,
                    presentation.Definition.ObjectiveId,
                    StringComparison.Ordinal))
            {
                renderedObjectiveId =
                    presentation.Definition.ObjectiveId;
                detailsExpanded = false;
            }
            stateIcon.text = presentation.StateIcon;
            stateIcon.color = Gold;
            titleText.text = presentation.DisplayText;
            accessibilityText.text = presentation.DetailText;
            detailListText.text = "세부 목표\n" +
                string.Join(
                    "\n",
                    presentation.Definition.Steps
                        .Take(4)
                        .Select(step => $"• {step}"));
            UpdateDetailsPresentation(presentation.ActionLabel);
            hasRenderableObjective = true;
            UpdateHudVisibility();
        }

        private void ToggleDetails()
        {
            if (!hasRenderableObjective)
            {
                return;
            }

            detailsExpanded = !detailsExpanded;
            string actionLabel =
                CurrentProductionViewModel?.Presentation?.ActionLabel ??
                "확인";
            UpdateDetailsPresentation(actionLabel);
        }

        private void UpdateDetailsPresentation(string actionLabel)
        {
            if (detailPanel != null)
            {
                detailPanel.SetActive(detailsExpanded);
            }
            if (progressText != null)
            {
                progressText.text = detailsExpanded
                    ? $"{actionLabel} · 접기 ▴"
                    : $"다음 목표 · {actionLabel}\n세부 목표 ▾";
            }
        }

        private void UpdateHudVisibility()
        {
            if (root == null)
            {
                return;
            }

            UIManager ui = UIManager.Instance;
            GameFlow flow = GameFlow.Instance;
            Wake.Exploration.LocationLoader locations =
                Wake.Exploration.LocationLoader.Instance;
            bool visible =
                hasRenderableObjective &&
                ui != null &&
                ui.ActivePanel == UiPrimaryPanel.Ingame &&
                ui.ActiveSystemScreen == SystemScreenState.None &&
                flow != null &&
                flow.HasActiveSession &&
                locations != null &&
                locations.CurrentLocation != null &&
                locations.IsPresentationVisible;
            if (root.activeSelf != visible)
            {
                root.SetActive(visible);
            }
        }
    }
}
