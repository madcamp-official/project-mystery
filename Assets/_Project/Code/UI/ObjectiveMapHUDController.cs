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
        private InvestigationObjectiveTracker tracker;
        private GameStateManager state;

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
            root.SetActive(true);
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
            RuntimeUiLayoutRegistry.CopyLayout(rect, "hud.objective");

            Image background = root.GetComponent<Image>();
            background.color = PanelColor;
            background.raycastTarget = false;

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
                new Vector2(0.76f, 1f),
                24f,
                TextAlignmentOptions.Left);
            progressText = EnsureText(
                root.transform,
                "Progress",
                new Vector2(0.76f, 0f),
                new Vector2(1f, 1f),
                20f,
                TextAlignmentOptions.Center);
            accessibilityText = EnsureText(
                root.transform,
                "Accessibility",
                new Vector2(0.12f, 0f),
                new Vector2(0.76f, 0.42f),
                16f,
                TextAlignmentOptions.Left);
            accessibilityText.color = new Color(0.82f, 0.84f, 0.86f, 1f);
            ApplyKoreanFont();
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

            stateIcon.text = "!";
            stateIcon.color = Gold;
            titleText.text = "현재 조사 목표가 없습니다.";
            progressText.text = "0/0";
            accessibilityText.text = "조사 목표 없음";
        }

        private void RenderProduction()
        {
            CurrentProductionViewModel =
                ProductionObjectiveViewModel.Resolve(state);
            ProductionObjectiveItem? selected =
                CurrentProductionViewModel.Current ??
                CurrentProductionViewModel.Next;
            ProductionObjectiveItem item = selected ??
                CurrentProductionViewModel.Items[
                    CurrentProductionViewModel.Items.Count - 1];
            bool pending =
                item.Status == ProductionObjectiveStatus.InteractionPending;
            stateIcon.text = item.StateIcon;
            stateIcon.color = pending ? Gold :
                item.Status == ProductionObjectiveStatus.Completed
                    ? Complete
                    : Color.white;
            titleText.text = $"{item.StateLabel} · {item.Definition.Title}";
            progressText.text =
                $"{item.Definition.SceneId} · {CurrentProductionViewModel.Summary}";
            string nextLabel = CurrentProductionViewModel.Next.HasValue
                ? $" · 다음: {CurrentProductionViewModel.Next.Value.Definition.SceneId} " +
                  CurrentProductionViewModel.Next.Value.Definition.Title
                : string.Empty;
            accessibilityText.text =
                $"{item.Definition.Description} · {item.Definition.ScheduleLabel}{nextLabel}";
            root.SetActive(true);
        }
    }
}
