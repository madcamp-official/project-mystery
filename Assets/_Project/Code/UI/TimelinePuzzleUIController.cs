using System;
using System.Collections.Generic;
using System.Linq;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using Wake.Core;
using Wake.Narrative;
using Wake.Puzzles;

namespace Wake.UI
{
    public readonly struct TimelineSlotView
    {
        public TimelineSlotView(int slot, string cardId, string label)
        {
            Slot = slot;
            CardId = cardId ?? string.Empty;
            Label = label ?? string.Empty;
        }

        public int Slot { get; }
        public string CardId { get; }
        public string Label { get; }
        public bool IsEmpty => string.IsNullOrEmpty(CardId);
    }

    public static class TimelinePuzzlePresentation
    {
        public static IReadOnlyList<TimelineSlotView> CreateSlots(
            IReadOnlyList<TimelineCardDefinition> definitions,
            IReadOnlyDictionary<int, string> placements)
        {
            var labels = (definitions ?? Array.Empty<TimelineCardDefinition>())
                .ToDictionary(card => card.Id, card => CardLabel(card));
            var result = new List<TimelineSlotView>();
            for (int slot = 0; slot < TimelinePuzzleCatalog.RequiredCardCount; slot++)
            {
                string cardId = placements != null &&
                                placements.TryGetValue(slot, out string placed)
                    ? placed
                    : string.Empty;
                string label = !string.IsNullOrEmpty(cardId) &&
                               labels.TryGetValue(cardId, out string known)
                    ? known
                    : "비어 있음";
                result.Add(new TimelineSlotView(slot, cardId, label));
            }
            return result;
        }

        public static string CardLabel(TimelineCardDefinition definition)
        {
            if (definition == null)
            {
                return "알 수 없는 카드";
            }

            return string.IsNullOrEmpty(definition.ConfirmedTime)
                ? definition.Label
                : $"{definition.ConfirmedTime} · {definition.Label}";
        }

        public static string Diagnostics(TimelineCompletionResult result)
        {
            if (result.Completed)
            {
                return "사건 타임라인을 완성했습니다.";
            }

            if (result.Diagnostics == null || result.Diagnostics.Count == 0)
            {
                return "배치 상태를 확인하세요.";
            }

            int sourceMissing = result.Diagnostics.Count(message =>
                message.StartsWith(
                    "source_missing:",
                    StringComparison.Ordinal));
            var summary = result.Diagnostics
                .Where(message => !message.StartsWith(
                    "source_missing:",
                    StringComparison.Ordinal))
                .Take(sourceMissing > 0 ? 2 : 3)
                .ToList();
            if (sourceMissing > 0)
            {
                summary.Add(
                    $"source_missing: 근거 자료 미확정 카드 {sourceMissing}장");
            }
            return string.Join("\n", summary.Take(3));
        }

        public static string SourceStatus(
            IEnumerable<TimelineCardDefinition> definitions)
        {
            TimelineSourceCoverage coverage =
                TimelinePuzzleValidator.InspectSources(definitions);
            return coverage.IsComplete
                ? $"근거 확인 {coverage.AuthoritativeCount}/{coverage.RequiredCount}"
                : $"근거 확인 {coverage.AuthoritativeCount}/{coverage.RequiredCount} · " +
                  $"source_missing {coverage.MissingSourceCount}장";
        }
    }

    [DisallowMultipleComponent]
    public sealed class TimelinePuzzleUIController :
        MonoBehaviour,
        IRuntimeModalController
    {
        private static readonly Color Panel = new(0.035f, 0.075f, 0.12f, 0.98f);
        private static readonly Color Available = new(0.16f, 0.20f, 0.26f, 1f);
        private static readonly Color Selected = new(0.24f, 0.48f, 0.56f, 1f);
        private static readonly Color Placed = new(0.12f, 0.30f, 0.28f, 1f);
        private static readonly Color Empty = new(0.10f, 0.12f, 0.16f, 1f);

        private readonly List<Button> cardButtons = new();
        private readonly List<Button> slotButtons = new();
        private GameObject root;
        private TMP_Text hintText;
        private TMP_Text statusText;
        private TimelinePuzzleSession session;
        private string selectedCardId;

        public bool IsOpen => root != null && root.activeSelf;
        public TimelinePuzzleSession Session => session;
        public string SelectedCardId => selectedCardId;

        private void Awake()
        {
            BuildUi();
        }

        public bool Open()
        {
            GameStateManager state = GameStateManager.Instance;
            if (root == null ||
                !ProductionSceneCompletionGate.CanStartInteraction(
                    state,
                    TimelinePuzzleCatalog.SceneId,
                    TimelinePuzzleCatalog.PuzzleId))
            {
                return false;
            }

            session = new TimelinePuzzleSession(
                state,
                TimelinePuzzleCatalog.SourceBackedCards);
            selectedCardId = null;
            statusText.text =
                "사건 카드를 선택한 뒤 배치할 시간 슬롯을 누르세요.\n" +
                TimelinePuzzlePresentation.SourceStatus(session.Definitions);
            root.SetActive(true);
            Refresh();
            return true;
        }

        public void Close()
        {
            root?.SetActive(false);
        }

        public bool SelectCard(string cardId)
        {
            if (session == null ||
                session.IsCompleted ||
                !session.Definitions.Any(card => card.Id == cardId))
            {
                return false;
            }

            selectedCardId = cardId;
            statusText.text = "배치할 시간 슬롯을 선택하세요.";
            Refresh();
            return true;
        }

        public TimelinePlacementResult PlaceSelected(int slot)
        {
            if (session == null || string.IsNullOrEmpty(selectedCardId))
            {
                statusText.text = "먼저 사건 카드를 선택하세요.";
                return TimelinePlacementResult.UnknownCard;
            }

            TimelinePlacementResult result = session.Place(selectedCardId, slot);
            statusText.text = result switch
            {
                TimelinePlacementResult.Placed => $"{slot + 1}번 슬롯에 배치했습니다.",
                TimelinePlacementResult.InvalidSlot => "유효하지 않은 시간 슬롯입니다.",
                TimelinePlacementResult.Completed => "이미 완성된 타임라인입니다.",
                _ => "등록되지 않은 사건 카드입니다."
            };
            if (result == TimelinePlacementResult.Placed)
            {
                selectedCardId = null;
            }
            Refresh();
            return result;
        }

        public bool UseHint()
        {
            bool changed = session != null && session.UseHint();
            if (changed)
            {
                statusText.text = "힌트를 갱신했습니다.";
                Refresh();
            }
            return changed;
        }

        public TimelineCompletionResult Submit()
        {
            if (session == null)
            {
                return new TimelineCompletionResult(
                    false,
                    TimelinePuzzleCatalog.RequiredCardCount,
                    new[] { "타임라인 세션을 시작하지 못했습니다." });
            }

            TimelineCompletionResult result = session.TryComplete();
            statusText.text = TimelinePuzzlePresentation.Diagnostics(result);
            ToastController.Instance?.Show(statusText.text);
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

            hintText.text = session.GetHint();
            IReadOnlyList<TimelineSlotView> slots =
                TimelinePuzzlePresentation.CreateSlots(
                    session.Definitions,
                    session.Placements);
            for (int index = 0; index < slotButtons.Count; index++)
            {
                TimelineSlotView view = slots[index];
                Button button = slotButtons[index];
                button.interactable = !session.IsCompleted;
                button.image.color = view.IsEmpty ? Empty : Placed;
                button.GetComponentInChildren<TMP_Text>().text =
                    $"{view.Slot + 1:00}. [{(view.IsEmpty ? "빈 슬롯" : "배치됨")}] {view.Label}";
            }

            for (int index = 0; index < cardButtons.Count; index++)
            {
                TimelineCardDefinition definition = session.Definitions[index];
                int slot = session.Placements
                    .Where(pair => pair.Value == definition.Id)
                    .Select(pair => pair.Key)
                    .DefaultIfEmpty(-1)
                    .First();
                bool selected = selectedCardId == definition.Id;
                Button button = cardButtons[index];
                button.interactable = !session.IsCompleted;
                button.image.color = slot >= 0 ? Placed : selected ? Selected : Available;
                string state = slot >= 0
                    ? $"배치: {slot + 1:00}"
                    : selected ? "선택됨" : "선택 가능";
                button.GetComponentInChildren<TMP_Text>().text =
                    $"[{state}] {TimelinePuzzlePresentation.CardLabel(definition)}";
            }
        }

        private void BuildUi()
        {
            Transform canvas = GameObject.Find("Canvas")?.transform;
            if (canvas == null)
            {
                return;
            }

            root = MakeObject("Timeline Puzzle", canvas, typeof(Image));
            RectTransform rootRect = root.GetComponent<RectTransform>();
            rootRect.anchorMin = new Vector2(0.5f, 0.5f);
            rootRect.anchorMax = new Vector2(0.5f, 0.5f);
            rootRect.sizeDelta = new Vector2(1040f, 690f);
            root.GetComponent<Image>().color = Panel;

            MakeText(
                root.transform,
                "사건 타임라인 · 21:22–22:45",
                0.90f,
                0.98f,
                32f,
                0.06f,
                0.94f);
            MakeText(
                root.transform,
                "왼쪽 사건 카드를 선택하고 오른쪽의 12개 시간 슬롯에 순서대로 배치하세요.",
                0.84f,
                0.90f,
                19f,
                0.06f,
                0.94f);

            for (int index = 0; index < TimelinePuzzleCatalog.SourceBackedCards.Count; index++)
            {
                int captured = index;
                Button card = MakeButton(
                    root.transform,
                    $"Card {index + 1}",
                    0.52f + (4 - index) * 0.058f,
                    0.572f + (4 - index) * 0.058f,
                    string.Empty,
                    0.05f,
                    0.47f,
                    17f);
                card.onClick.AddListener(() =>
                    SelectCard(TimelinePuzzleCatalog.SourceBackedCards[captured].Id));
                cardButtons.Add(card);
            }

            for (int index = 0; index < TimelinePuzzleCatalog.RequiredCardCount; index++)
            {
                int captured = index;
                int column = index / 6;
                int row = index % 6;
                float minX = column == 0 ? 0.52f : 0.75f;
                float maxX = column == 0 ? 0.73f : 0.96f;
                float maxY = 0.82f - row * 0.09f;
                Button slot = MakeButton(
                    root.transform,
                    $"Slot {index + 1}",
                    maxY - 0.072f,
                    maxY,
                    string.Empty,
                    minX,
                    maxX,
                    15f);
                slot.onClick.AddListener(() => PlaceSelected(captured));
                slotButtons.Add(slot);
            }

            hintText = MakeText(
                root.transform,
                string.Empty,
                0.31f,
                0.43f,
                18f,
                0.05f,
                0.47f);
            statusText = MakeText(
                root.transform,
                string.Empty,
                0.16f,
                0.30f,
                18f,
                0.05f,
                0.47f);
            Button hint = MakeButton(
                root.transform, "Hint", 0.05f, 0.13f, "힌트", 0.05f, 0.18f, 19f);
            hint.onClick.AddListener(() => UseHint());
            Button submit = MakeButton(
                root.transform, "Submit", 0.05f, 0.13f, "완성 확인", 0.20f, 0.34f, 19f);
            submit.onClick.AddListener(() => Submit());
            Button close = MakeButton(
                root.transform, "Close", 0.05f, 0.13f, "닫기", 0.36f, 0.47f, 19f);
            close.onClick.AddListener(Close);
            root.SetActive(false);
        }

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
            text.font = StatusHUDController.RuntimeKoreanFont;
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
            target.GetComponent<Image>().color = Available;
            TMP_Text text = MakeText(target.transform, label, 0f, 1f, size, 0f, 1f);
            text.raycastTarget = false;
            return target.GetComponent<Button>();
        }
    }
}
