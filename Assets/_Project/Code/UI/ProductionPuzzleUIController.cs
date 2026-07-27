using System;
using System.Collections.Generic;
using System.Linq;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using Wake.Core;
using Wake.Evidence;
using Wake.Narrative;
using Wake.Puzzles;

namespace Wake.UI
{
    public readonly struct PuzzleSelectionView
    {
        public PuzzleSelectionView(
            string id,
            string label,
            bool isSelected,
            bool isRequired,
            bool isAvailable)
        {
            Id = id;
            Label = label;
            IsSelected = isSelected;
            IsRequired = isRequired;
            IsAvailable = isAvailable;
        }

        public string Id { get; }
        public string Label { get; }
        public bool IsSelected { get; }
        public bool IsRequired { get; }
        public bool IsAvailable { get; }
        public string AccessibleLabel =>
            $"{(IsSelected ? "선택됨" : "선택 안 됨")}: {Label}";
    }

    public static class ProductionPuzzlePresentation
    {
        private static readonly IReadOnlyDictionary<string, string> Labels =
            new Dictionary<string, string>(StringComparer.Ordinal)
            {
                ["no_spatter"] = "비산혈 없음",
                ["center_mismatch"] = "혈흔 중심 불일치",
                ["vertical_drop"] = "수직 낙하 흔적",
                ["horizon_branch_22_18"] = "22:18 Horizon 분기",
                ["weight_86kg"] = "약 86kg 하중",
                ["ballast_horizon_route"] = "Ballast-Horizon 연결 경로"
            };

        public static string GetTitle(string puzzleId) =>
            ProductionPuzzleDefinition.Normalize(puzzleId) switch
            {
                ProductionPuzzleCatalog.BloodPattern => "혈흔 배열",
                ProductionPuzzleCatalog.CargoRailBranch => "화물 레일 분기",
                _ => "조사 퍼즐"
            };

        public static string GetObjective(string puzzleId) =>
            ProductionPuzzleDefinition.Normalize(puzzleId) switch
            {
                ProductionPuzzleCatalog.BloodPattern =>
                    "혈흔의 위치와 낙하 방향으로 시신 반입 방식을 확인하세요.",
                ProductionPuzzleCatalog.CargoRailBranch =>
                    "22:18 하중 이동이 지나간 레일 분기를 확인하세요.",
                _ => "필수 단서와 올바른 선택을 확인하세요."
            };

        public static string GetHint(
            ProductionPuzzleDefinition definition,
            int hintLevel)
        {
            if (hintLevel <= 0)
            {
                return "힌트를 사용하지 않았습니다.";
            }

            if (hintLevel == 1)
            {
                return GetObjective(definition.Id);
            }

            if (hintLevel == 2)
            {
                return "관련 증거: " +
                       string.Join(", ", definition.RequiredEvidenceIds);
            }

            return "정답과 관계없는 선택은 비활성화됩니다.";
        }

        public static IReadOnlyList<PuzzleSelectionView> CreateSelections(
            ProductionPuzzleDefinition definition,
            IReadOnlyCollection<string> selectedIds,
            int hintLevel)
        {
            var selected = new HashSet<string>(
                selectedIds ?? Array.Empty<string>(),
                StringComparer.Ordinal);
            return definition.AllowedSelectionIds
                .Select(id => new PuzzleSelectionView(
                    id,
                    Labels.TryGetValue(id, out string label) ? label : id,
                    selected.Contains(id),
                    definition.RequiredSelectionIds.Contains(id),
                    hintLevel < 3 || definition.RequiredSelectionIds.Contains(id)))
                .ToArray();
        }
    }

    [DisallowMultipleComponent]
    public sealed class ProductionPuzzleUIController :
        MonoBehaviour,
        IRuntimeModalController
    {
        private static readonly Color Panel = new(0.035f, 0.075f, 0.12f, 0.98f);
        private static readonly Color Selected = new(0.20f, 0.58f, 0.48f, 1f);
        private static readonly Color Available = new(0.16f, 0.20f, 0.26f, 1f);

        private GameObject root;
        private TMP_Text titleText;
        private TMP_Text objectiveText;
        private TMP_Text hintText;
        private readonly List<Button> selectionButtons = new();
        private ProductionPuzzleSession session;

        public bool IsOpen => root != null && root.activeSelf;
        public ProductionPuzzleSession Session => session;

        private void Awake()
        {
            BuildUi();
        }

        public bool Open(string puzzleId)
        {
            GameStateManager state = GameStateManager.Instance;
            if (!ProductionPuzzleCatalog.TryGet(puzzleId, out var definition) ||
                !ProductionSceneCompletionGate.CanStartInteraction(
                    state,
                    definition.SceneId,
                    definition.Id))
            {
                return false;
            }

            session = new ProductionPuzzleSession(
                definition,
                state,
                evidenceId =>
                    EvidenceInventory.Instance != null &&
                    EvidenceInventory.Instance.Contains(evidenceId));
            root.SetActive(true);
            Refresh();
            return true;
        }

        public void Close()
        {
            root?.SetActive(false);
        }

        public bool Toggle(string selectionId)
        {
            if (session == null || session.IsCompleted)
            {
                return false;
            }

            bool changed = session.SelectedIds.Contains(selectionId)
                ? session.Deselect(selectionId)
                : session.Select(selectionId);
            if (changed)
            {
                Refresh();
            }
            return changed;
        }

        public bool UseHint()
        {
            bool changed = session != null && session.UseHint();
            if (changed)
            {
                Refresh();
            }
            return changed;
        }

        public PuzzleCompletionResult Submit()
        {
            if (session == null)
            {
                return new PuzzleCompletionResult(
                    false,
                    Array.Empty<string>(),
                    Array.Empty<string>());
            }

            PuzzleCompletionResult result = session.TryComplete();
            InvestigationFeedback feedback =
                InvestigationFeedbackCatalog.ForPuzzle(session.Definition, result);
            ToastController.Instance?.Show(
                $"{feedback.Title}\n{feedback.Message}");
            if (result.Completed)
            {
                Close();
            }
            else
            {
                Refresh();
            }
            return result;
        }

        private void Refresh()
        {
            if (session == null)
            {
                return;
            }

            titleText.text =
                ProductionPuzzlePresentation.GetTitle(session.Definition.Id);
            objectiveText.text =
                ProductionPuzzlePresentation.GetObjective(session.Definition.Id);
            hintText.text = ProductionPuzzlePresentation.GetHint(
                session.Definition,
                session.HintLevel);
            IReadOnlyList<PuzzleSelectionView> views =
                ProductionPuzzlePresentation.CreateSelections(
                    session.Definition,
                    session.SelectedIds,
                    session.HintLevel);
            for (int i = 0; i < selectionButtons.Count; i++)
            {
                bool active = i < views.Count;
                selectionButtons[i].gameObject.SetActive(active);
                if (!active)
                {
                    continue;
                }

                PuzzleSelectionView view = views[i];
                Button button = selectionButtons[i];
                button.interactable = view.IsAvailable;
                button.image.color = view.IsSelected ? Selected : Available;
                TMP_Text label = button.GetComponentInChildren<TMP_Text>();
                label.text = $"{(view.IsSelected ? "✓" : "○")} {view.Label}";
                button.onClick.RemoveAllListeners();
                button.onClick.AddListener(() => Toggle(view.Id));
            }
        }

        private void BuildUi()
        {
            Transform canvas = GameObject.Find("Canvas")?.transform;
            if (canvas == null)
            {
                return;
            }

            root = new GameObject(
                "Production Puzzle",
                typeof(RectTransform),
                typeof(CanvasRenderer),
                typeof(Image));
            root.transform.SetParent(canvas, false);
            RectTransform rect = root.GetComponent<RectTransform>();
            rect.anchorMin = new Vector2(0.5f, 0.5f);
            rect.anchorMax = new Vector2(0.5f, 0.5f);
            rect.sizeDelta = new Vector2(760f, 560f);
            root.GetComponent<Image>().color = Panel;

            titleText = MakeText("Title", 0.82f, 1f, 34f);
            objectiveText = MakeText("Objective", 0.66f, 0.82f, 22f);
            hintText = MakeText("Hint", 0.16f, 0.30f, 20f);
            for (int i = 0; i < 6; i++)
            {
                selectionButtons.Add(MakeButton(
                    $"Selection {i}",
                    0.31f + i * 0.055f,
                    0.36f + i * 0.055f,
                    $"선택 {i + 1}"));
            }

            Button hint = MakeButton("Hint Button", 0.04f, 0.13f, "힌트");
            hint.onClick.AddListener(() => UseHint());
            Button submit = MakeButton("Submit Button", 0.04f, 0.13f, "확인");
            submit.GetComponent<RectTransform>().anchoredPosition = new Vector2(180f, 0f);
            submit.onClick.AddListener(() => Submit());
            Button close = MakeButton("Close Button", 0.04f, 0.13f, "나가기");
            close.GetComponent<RectTransform>().anchoredPosition = new Vector2(-180f, 0f);
            close.onClick.AddListener(Close);
            FeatureTypography.ApplyPuzzle(
                root.transform,
                titleText,
                objectiveText,
                hintText);
            root.SetActive(false);
        }

        private TMP_Text MakeText(
            string name,
            float anchorMinY,
            float anchorMaxY,
            float fontSize)
        {
            var target = new GameObject(
                name,
                typeof(RectTransform),
                typeof(CanvasRenderer),
                typeof(TextMeshProUGUI));
            target.transform.SetParent(root.transform, false);
            RectTransform rect = target.GetComponent<RectTransform>();
            rect.anchorMin = new Vector2(0.08f, anchorMinY);
            rect.anchorMax = new Vector2(0.92f, anchorMaxY);
            rect.offsetMin = Vector2.zero;
            rect.offsetMax = Vector2.zero;
            TMP_Text text = target.GetComponent<TMP_Text>();
            TypographyService.Apply(text, TypographyRole.Body);
            text.fontSize = fontSize;
            text.color = Color.white;
            text.alignment = TextAlignmentOptions.Center;
            return text;
        }

        private Button MakeButton(
            string name,
            float anchorMinY,
            float anchorMaxY,
            string label)
        {
            var target = new GameObject(
                name,
                typeof(RectTransform),
                typeof(CanvasRenderer),
                typeof(Image),
                typeof(Button));
            target.transform.SetParent(root.transform, false);
            RectTransform rect = target.GetComponent<RectTransform>();
            rect.anchorMin = new Vector2(0.2f, anchorMinY);
            rect.anchorMax = new Vector2(0.8f, anchorMaxY);
            rect.offsetMin = Vector2.zero;
            rect.offsetMax = Vector2.zero;
            target.GetComponent<Image>().color = Available;
            TMP_Text text = MakeTextChild(target.transform, label);
            text.raycastTarget = false;
            return target.GetComponent<Button>();
        }

        private static TMP_Text MakeTextChild(Transform parent, string value)
        {
            var target = new GameObject(
                "Label",
                typeof(RectTransform),
                typeof(CanvasRenderer),
                typeof(TextMeshProUGUI));
            target.transform.SetParent(parent, false);
            RectTransform rect = target.GetComponent<RectTransform>();
            rect.anchorMin = Vector2.zero;
            rect.anchorMax = Vector2.one;
            rect.offsetMin = Vector2.zero;
            rect.offsetMax = Vector2.zero;
            TMP_Text text = target.GetComponent<TMP_Text>();
            TypographyService.Apply(text, TypographyRole.Choice);
            text.fontSize = 20f;
            text.alignment = TextAlignmentOptions.Center;
            text.color = Color.white;
            text.text = value;
            return text;
        }
    }
}
