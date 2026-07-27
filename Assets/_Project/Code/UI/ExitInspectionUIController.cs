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
    [DisallowMultipleComponent]
    public sealed class ExitInspectionUIController : MonoBehaviour, IRuntimeModalController
    {
        private static readonly Color Panel = new(0.035f, 0.075f, 0.12f, 0.98f);
        private static readonly Color Available = new(0.16f, 0.20f, 0.26f, 1f);
        private static readonly Color Inspected = new(0.12f, 0.34f, 0.28f, 1f);
        private static readonly Color Warning = new(0.46f, 0.25f, 0.10f, 1f);

        private readonly List<Button> inspectionButtons = new();
        private GameObject root;
        private Button reopenButton;
        private Button hintButton;
        private Button submitButton;
        private TMP_Text progressText;
        private TMP_Text hintText;
        private TMP_Text statusText;
        private ExitInspectionSession session;

        public bool IsOpen => root != null && root.activeSelf;
        public ExitInspectionSession Session => session;
        public string StatusMessage => statusText?.text ?? string.Empty;

        private void Awake() => BuildUi();
        private void OnEnable() => SetReopenVisibility();
        private void OnDisable()
        {
            root?.SetActive(false);
            reopenButton?.gameObject.SetActive(false);
        }
        private void Update()
        {
            if (!IsOpen)
            {
                SetReopenVisibility();
            }
        }

        public bool Open()
        {
            GameStateManager state = GameStateManager.Instance;
            EvidenceInventory inventory = EvidenceInventory.Instance;
            if (root == null || inventory == null ||
                !ProductionSceneCompletionGate.CanStartInteraction(
                    state, ExitInspectionCatalog.SceneId, ExitInspectionCatalog.SessionId))
            {
                return false;
            }

            session = new ExitInspectionSession(
                state, inventory.Contains, inventory.TryAddById);
            statusText.text = session.Step == 0
                ? "세 출구를 자유로운 순서로 검사하세요."
                : $"저장된 검사 {session.Step}/3과 힌트 {session.HintLevel}/3을 복원했습니다.";
            reopenButton.gameObject.SetActive(false);
            root.transform.SetAsLastSibling();
            root.SetActive(true);
            Refresh();
            return true;
        }

        public void Close()
        {
            root?.SetActive(false);
            SetReopenVisibility();
        }

        public ExitInspectionResult Inspect(string inspectionId)
        {
            if (session == null)
            {
                return ExitInspectionResult.UnknownInspection;
            }

            ExitInspectionCatalog.TryGet(
                inspectionId, out ExitInspectionDefinition definition);
            ExitInspectionResult result = session.Inspect(inspectionId);
            statusText.text = result switch
            {
                ExitInspectionResult.Recorded =>
                    $"✓ {definition.Title} 검사 완료 · {definition.EvidenceId}\n{definition.Finding}",
                ExitInspectionResult.AlreadyInspected => "이미 검사하고 기록한 출구입니다.",
                ExitInspectionResult.EvidenceUnavailable => "검사 결과를 증거 상태에 기록하지 못했습니다.",
                ExitInspectionResult.SessionCompleted => "이미 완료된 출구 검증입니다.",
                _ => "등록되지 않은 검사 지점입니다."
            };
            Refresh();
            return result;
        }

        public bool UseHint()
        {
            bool changed = session != null && session.UseHint();
            statusText.text = changed
                ? $"힌트 {session.HintLevel}/3을 확인했습니다."
                : "사용할 수 있는 힌트를 모두 확인했습니다.";
            Refresh();
            return changed;
        }

        public ExitInspectionCompletion Submit()
        {
            if (session == null)
            {
                return new ExitInspectionCompletion(
                    false, Array.Empty<string>(), Array.Empty<string>());
            }

            ExitInspectionCompletion result = session.TryComplete();
            if (!result.Completed)
            {
                statusText.text = FailureMessage(result);
                ToastController.Instance?.Show(statusText.text);
                Refresh();
                return result;
            }

            statusText.text = "✓ 흔적 없는 출구 검증 완료 · 다음 조사로 이동합니다.";
            ToastController.Instance?.Show(statusText.text);
            Refresh();
            if (!ContinueToNextScene())
            {
                statusText.text = "검증은 완료됐지만 D2-02를 열지 못했습니다. 맵에서 선택하세요.";
            }
            return result;
        }

        private bool ContinueToNextScene()
        {
            if (!ProductionSceneCompletionCatalog.TryGet(
                    ExitInspectionCatalog.SceneId,
                    out ProductionSceneCompletionRequirement requirement))
            {
                return false;
            }

            MapController map = FindFirstObjectByType<MapController>();
            bool allowed = map != null &&
                           map.TryTravelToScene(requirement.NextSceneId).IsAllowed;
            if (allowed)
            {
                Close();
            }
            return allowed;
        }

        private void Refresh()
        {
            if (session == null)
            {
                return;
            }

            int total = ExitInspectionCatalog.All.Count;
            progressText.text =
                $"검사 진행 {session.Step}/{total} · 남은 지점 {total - session.Step}";
            hintText.text = Hint(session.HintLevel);
            hintButton.interactable = !session.IsCompleted && session.HintLevel < 3;
            Label(hintButton).text = $"? [힌트 {session.HintLevel}/3] 단서 보기";
            for (int index = 0; index < inspectionButtons.Count; index++)
            {
                ExitInspectionDefinition definition = ExitInspectionCatalog.All[index];
                Button button = inspectionButtons[index];
                bool done = session.HasInspected(definition.Id);
                button.interactable = !done && !session.IsCompleted;
                button.image.color = done ? Inspected : Available;
                Label(button).text = done
                    ? $"✓ [선택됨 · 검사 완료] {definition.Title} · {definition.EvidenceId}"
                    : $"○ [검사 가능] {definition.Title} · 확보 예정 {definition.EvidenceId}";
            }

            bool ready = session.Step == total;
            submitButton.image.color = ready ? Inspected : Warning;
            submitButton.interactable = !session.IsCompleted;
            Label(submitButton).text = ready
                ? "✓ [완료 가능] 흔적 없는 출구 확정"
                : $"△ [미완료 {total - session.Step}] 출구 검증 확정";
        }

        private void SetReopenVisibility()
        {
            if (reopenButton == null)
            {
                return;
            }

            UIManager ui = UIManager.Instance;
            GameStateManager state = GameStateManager.Instance;
            ProductionDialogueCheckpoint checkpoint = state?.DialogueCheckpoint;
            bool pending = checkpoint != null &&
                           checkpoint.pendingInteractionId == ExitInspectionCatalog.SessionId &&
                           !state.HasCompletedScene(ExitInspectionCatalog.SceneId);
            bool visible = pending && ui?.ActivePanel == UiPrimaryPanel.Ingame &&
                           !ui.IsSettingsOpen && ui.OpenRuntimeModalCount == 0 &&
                           DialogueController.Instance?.IsBusy != true;
            reopenButton.gameObject.SetActive(visible);
            if (visible)
            {
                int count = session?.Step ?? (
                    state.TryGetPuzzleSession(
                        ExitInspectionCatalog.SessionId, out PuzzleSessionState saved)
                        ? saved.selectedIds?.Count ?? 0
                        : 0);
                Label(reopenButton).text =
                    $"○ [진행 저장됨 {count}/3] 출구 검증 재개";
            }
        }

        private static string FailureMessage(ExitInspectionCompletion result)
        {
            string inspections = string.Join(", ", result.MissingInspectionIds.Select(id =>
                ExitInspectionCatalog.TryGet(id, out var item) ? item.Title : id));
            string evidence = string.Join(", ", result.MissingEvidenceIds);
            return "△ 출구 검증을 아직 완료할 수 없습니다." +
                   (inspections.Length > 0 ? $"\n남은 검사: {inspections}" : string.Empty) +
                   (evidence.Length > 0 ? $"\n누락 증거: {evidence}" : string.Empty);
        }

        private static string Hint(int level) => level switch
        {
            <= 0 => "힌트를 사용하지 않았습니다.",
            1 => "외벽과 내부 통로를 자유 순서로 하나씩 확인하세요.",
            2 => "외벽은 염분막과 센서, 내부 통로는 먼지의 연속성이 핵심입니다.",
            _ => "C-03, C-04, C-05를 모두 확보하면 사용된 출구가 없음을 확정할 수 있습니다."
        };

        private void BuildUi()
        {
            Transform canvas = GameObject.Find("Canvas")?.transform;
            if (canvas == null)
            {
                return;
            }

            root = Object("Exit Inspection", canvas, typeof(Image));
            Place(root, 0.5f, 0.5f, 0.5f, 0.5f, new Vector2(920f, 680f));
            root.GetComponent<Image>().color = Panel;
            Text(root.transform, "흔적 없는 출구 검증", .90f, .98f, 34f);
            Text(root.transform,
                "외벽 발판·공조 덕트·설비 점검구를 검사해 이동 흔적을 확인하세요.",
                .83f, .90f, 20f);
            progressText = Text(root.transform, string.Empty, .76f, .83f, 22f);
            for (int index = 0; index < ExitInspectionCatalog.All.Count; index++)
            {
                int captured = index;
                Button button = Button(root.transform,
                    $"Inspection {ExitInspectionCatalog.All[index].Id}",
                    .62f - index * .105f, .71f - index * .105f, .08f, .92f);
                button.onClick.AddListener(() =>
                    Inspect(ExitInspectionCatalog.All[captured].Id));
                inspectionButtons.Add(button);
            }

            hintText = Text(root.transform, string.Empty, .29f, .37f, 19f);
            statusText = Text(root.transform, string.Empty, .18f, .29f, 20f);
            hintButton = Button(root.transform, "Hint", .07f, .15f, .07f, .30f);
            hintButton.onClick.AddListener(() => UseHint());
            submitButton = Button(root.transform, "Submit", .07f, .15f, .32f, .70f);
            submitButton.onClick.AddListener(() => Submit());
            Button close = Button(root.transform, "Close", .07f, .15f, .72f, .93f);
            Label(close).text = "닫기 · 진행 저장됨";
            close.onClick.AddListener(Close);
            reopenButton = Button(canvas, "Exit Inspection Resume",
                .035f, .095f, .38f, .62f);
            reopenButton.onClick.AddListener(() => Open());
            InteractionTypography.Apply(
                root.transform,
                progressText,
                hintText,
                statusText);
            root.SetActive(false);
            reopenButton.gameObject.SetActive(false);
        }

        private static GameObject Object(
            string name, Transform parent, params Type[] components)
        {
            Type[] all = { typeof(RectTransform), typeof(CanvasRenderer) };
            var target = new GameObject(name, all.Concat(components).ToArray());
            target.transform.SetParent(parent, false);
            return target;
        }

        private static void Place(
            GameObject target, float minX, float minY, float maxX, float maxY,
            Vector2 size = default)
        {
            RectTransform rect = target.GetComponent<RectTransform>();
            rect.anchorMin = new Vector2(minX, minY);
            rect.anchorMax = new Vector2(maxX, maxY);
            rect.offsetMin = Vector2.zero;
            rect.offsetMax = Vector2.zero;
            rect.sizeDelta = size;
        }

        private static TMP_Text Text(
            Transform parent, string value, float minY, float maxY, float size)
        {
            GameObject target = Object("Label", parent, typeof(TextMeshProUGUI));
            Place(target, .07f, minY, .93f, maxY);
            TMP_Text text = target.GetComponent<TMP_Text>();
            TypographyService.Apply(text, TypographyRole.Body);
            text.fontSize = size;
            text.enableAutoSizing = true;
            text.fontSizeMin = Mathf.Max(14f, size * .72f);
            text.fontSizeMax = size;
            text.textWrappingMode = TextWrappingModes.Normal;
            text.alignment = TextAlignmentOptions.Center;
            text.color = Color.white;
            text.text = value;
            return text;
        }

        private static Button Button(
            Transform parent, string name, float minY, float maxY,
            float minX, float maxX)
        {
            GameObject target = Object(name, parent, typeof(Image), typeof(Button));
            Place(target, minX, minY, maxX, maxY);
            target.GetComponent<Image>().color = Available;
            TMP_Text text = Text(target.transform, string.Empty, 0f, 1f, 19f);
            text.raycastTarget = false;
            return target.GetComponent<Button>();
        }

        private static TMP_Text Label(Button button) =>
            button.GetComponentInChildren<TMP_Text>();
    }
}
