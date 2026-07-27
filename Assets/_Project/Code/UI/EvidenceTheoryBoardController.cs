using System;
using System.Collections.Generic;
using System.Linq;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using Wake.Core;
using Wake.Evidence;
using Wake.Puzzles;

namespace Wake.UI
{
    public enum EvidenceTheoryState
    {
        MissingEvidence,
        UnreliableEvidence,
        ReadyToUnlock,
        Unlocked
    }

    public readonly struct EvidenceTheoryView
    {
        public EvidenceTheoryView(
            string id,
            string title,
            string conclusion,
            EvidenceTheoryState state,
            IReadOnlyList<string> missingEvidenceIds,
            IReadOnlyList<string> unusableEvidenceIds)
        {
            Id = id ?? string.Empty;
            Title = title ?? string.Empty;
            Conclusion = conclusion ?? string.Empty;
            State = state;
            MissingEvidenceIds = missingEvidenceIds ?? Array.Empty<string>();
            UnusableEvidenceIds = unusableEvidenceIds ?? Array.Empty<string>();
        }

        public string Id { get; }
        public string Title { get; }
        public string Conclusion { get; }
        public EvidenceTheoryState State { get; }
        public IReadOnlyList<string> MissingEvidenceIds { get; }
        public IReadOnlyList<string> UnusableEvidenceIds { get; }
    }

    public static class EvidenceTheoryPresentation
    {
        public static EvidenceTheoryView Create(
            DeductionEvaluation evaluation,
            bool unlocked)
        {
            EvidenceTheoryState state = unlocked
                ? EvidenceTheoryState.Unlocked
                : evaluation == null
                    ? EvidenceTheoryState.MissingEvidence
                    : evaluation.UnusableEvidenceIds.Count > 0
                        ? EvidenceTheoryState.UnreliableEvidence
                        : evaluation.MissingEvidenceIds.Count == 0
                            ? EvidenceTheoryState.ReadyToUnlock
                            : EvidenceTheoryState.MissingEvidence;
            CanonicalDeductionDefinition definition = evaluation?.Definition;
            return new EvidenceTheoryView(
                definition?.Id,
                definition?.DisplayName,
                definition?.Conclusion,
                state,
                evaluation?.MissingEvidenceIds,
                evaluation?.UnusableEvidenceIds);
        }

        public static string StateLabel(EvidenceTheoryView view) => view.State switch
        {
            EvidenceTheoryState.Unlocked => "추론 완료",
            EvidenceTheoryState.ReadyToUnlock => "논증 가능",
            EvidenceTheoryState.UnreliableEvidence => "증거 훼손으로 사용 불가",
            _ => view.MissingEvidenceIds.Count == 0
                ? "증거 부족"
                : $"필요한 단서 {view.MissingEvidenceIds.Count}개를 더 찾아야 합니다."
        };

        public static string ButtonLabel(EvidenceTheoryView view) =>
            $"[{StateLabel(view)}]\n{view.Title}\n{view.Conclusion}";
    }

    [DisallowMultipleComponent]
    public sealed class EvidenceTheoryBoardController :
        MonoBehaviour,
        IRuntimeModalController
    {
        private static readonly Color Panel = new(0.035f, 0.075f, 0.12f, 0.99f);
        private static readonly Color Locked = new(0.14f, 0.15f, 0.18f, 1f);
        private static readonly Color Ready = new(0.38f, 0.30f, 0.14f, 1f);
        private static readonly Color Unlocked = new(0.16f, 0.30f, 0.40f, 1f);
        private static readonly Color Unreliable = new(0.42f, 0.16f, 0.16f, 1f);

        private readonly List<Button> theoryButtons = new();
        private GameObject root;
        private TMP_Text progressText;
        private TMP_Text statusText;
        private CanonicalDeductionService service;

        public bool IsOpen => root != null && root.activeSelf;

        private void Awake()
        {
            BuildUi();
        }

        public bool Open()
        {
            if (root == null ||
                GameStateManager.Instance == null ||
                EvidenceInventory.Instance == null)
            {
                return false;
            }

            service = new CanonicalDeductionService(
                GameStateManager.Instance,
                EvidenceInventory.Instance.Contains);
            IReadOnlyList<string> unlocked = service.EvaluateAndUnlockAll();
            statusText.text = unlocked.Count == 0
                ? "증거 조합과 가설 상태를 확인하세요."
                : $"새 논증 해금: {string.Join(", ", unlocked)}";
            root.SetActive(true);
            Refresh();
            return true;
        }

        public void Close()
        {
            root?.SetActive(false);
        }

        public bool ResolveDeduction(string deductionId)
        {
            GameStateManager state = GameStateManager.Instance;
            if (service == null || state == null)
            {
                return false;
            }

            string normalized = CanonicalDeductionCatalog.NormalizeId(deductionId);
            bool changed;
            if (state.HasUnlockedDeduction(normalized))
            {
                changed = false;
                statusText.text = "이미 완료한 추론입니다.";
            }
            else
            {
                changed = service.TryUnlock(normalized);
                statusText.text = changed
                    ? "증거 연결로 논증을 해금했습니다."
                    : "필요한 증거 연결을 아직 충족하지 못했습니다.";
            }

            Refresh();
            return changed;
        }

        public void Refresh()
        {
            if (service == null || GameStateManager.Instance == null)
            {
                return;
            }

            GameStateManager state = GameStateManager.Instance;
            int unlockedCount = CanonicalDeductionCatalog.All.Count(
                definition => state.HasUnlockedDeduction(definition.Id));
            progressText.text =
                $"완료한 추론 {unlockedCount}/{CanonicalDeductionCatalog.All.Count}";
            for (int index = 0; index < theoryButtons.Count; index++)
            {
                CanonicalDeductionDefinition definition =
                    CanonicalDeductionCatalog.All[index];
                DeductionEvaluation evaluation = service.Evaluate(definition.Id);
                EvidenceTheoryView view = EvidenceTheoryPresentation.Create(
                    evaluation,
                    state.HasUnlockedDeduction(definition.Id));
                Button button = theoryButtons[index];
                button.image.color = ColorFor(view.State);
                button.interactable =
                    view.State != EvidenceTheoryState.UnreliableEvidence &&
                    (view.State != EvidenceTheoryState.MissingEvidence ||
                     view.MissingEvidenceIds.Count == 0);
                button.GetComponentInChildren<TMP_Text>().text =
                    EvidenceTheoryPresentation.ButtonLabel(view);
            }
        }

        private void BuildUi()
        {
            Transform canvas = GameObject.Find("Canvas")?.transform;
            if (canvas == null)
            {
                return;
            }

            root = MakeObject("Evidence Theory Board", canvas, typeof(Image));
            RectTransform rootRect = root.GetComponent<RectTransform>();
            rootRect.anchorMin = new Vector2(0.5f, 0.5f);
            rootRect.anchorMax = new Vector2(0.5f, 0.5f);
            rootRect.sizeDelta = new Vector2(920f, 650f);
            root.GetComponent<Image>().color = Panel;

            TMP_Text titleText = MakeText(
                root.transform,
                "증거 보드 · 핵심 논증",
                0.89f,
                0.98f,
                34f,
                0.07f,
                0.93f);
            progressText = MakeText(
                root.transform,
                string.Empty,
                0.82f,
                0.89f,
                22f,
                0.07f,
                0.93f);

            for (int index = 0; index < CanonicalDeductionCatalog.All.Count; index++)
            {
                int captured = index;
                int column = index % 2;
                int row = index / 2;
                float minX = column == 0 ? 0.06f : 0.52f;
                float maxX = column == 0 ? 0.48f : 0.94f;
                float maxY = 0.78f - row * 0.19f;
                Button button = MakeButton(
                    root.transform,
                    $"Deduction {index + 1}",
                    maxY - 0.16f,
                    maxY,
                    string.Empty,
                    minX,
                    maxX,
                    18f);
                button.onClick.AddListener(() =>
                    ResolveDeduction(CanonicalDeductionCatalog.All[captured].Id));
                theoryButtons.Add(button);
            }

            statusText = MakeText(
                root.transform,
                string.Empty,
                0.10f,
                0.20f,
                19f,
                0.07f,
                0.75f);
            Button close = MakeButton(
                root.transform,
                "Close",
                0.04f,
                0.12f,
                "닫기",
                0.78f,
                0.93f,
                20f);
            close.onClick.AddListener(Close);
            FeatureTypography.ApplyTheoryBoard(
                root.transform,
                titleText,
                progressText,
                statusText);
            root.SetActive(false);
        }

        private static Color ColorFor(EvidenceTheoryState state) => state switch
        {
            EvidenceTheoryState.Unlocked => Unlocked,
            EvidenceTheoryState.ReadyToUnlock => Ready,
            EvidenceTheoryState.UnreliableEvidence => Unreliable,
            _ => Locked
        };

        private static GameObject MakeObject(
            string name,
            Transform parent,
            params Type[] components)
        {
            Type[] all = new Type[components.Length + 2];
            all[0] = typeof(RectTransform);
            all[1] = typeof(CanvasRenderer);
            Array.Copy(components, 0, all, 2, components.Length);
            var target = new GameObject(name, all);
            target.transform.SetParent(parent, false);
            return target;
        }

        private static TMP_Text MakeText(
            Transform parent,
            string value,
            float minY,
            float maxY,
            float size,
            float minX,
            float maxX)
        {
            GameObject target = MakeObject("Label", parent, typeof(TextMeshProUGUI));
            RectTransform rect = target.GetComponent<RectTransform>();
            rect.anchorMin = new Vector2(minX, minY);
            rect.anchorMax = new Vector2(maxX, maxY);
            rect.offsetMin = Vector2.zero;
            rect.offsetMax = Vector2.zero;
            TMP_Text text = target.GetComponent<TMP_Text>();
            text.fontSize = size;
            text.alignment = TextAlignmentOptions.Center;
            text.color = Color.white;
            text.text = value;
            return text;
        }

        private static Button MakeButton(
            Transform parent,
            string name,
            float minY,
            float maxY,
            string label,
            float minX,
            float maxX,
            float size)
        {
            GameObject target = MakeObject(name, parent, typeof(Image), typeof(Button));
            RectTransform rect = target.GetComponent<RectTransform>();
            rect.anchorMin = new Vector2(minX, minY);
            rect.anchorMax = new Vector2(maxX, maxY);
            rect.offsetMin = Vector2.zero;
            rect.offsetMax = Vector2.zero;
            target.GetComponent<Image>().color = Locked;
            TMP_Text text = MakeText(target.transform, label, 0f, 1f, size, 0f, 1f);
            text.raycastTarget = false;
            return target.GetComponent<Button>();
        }
    }
}
