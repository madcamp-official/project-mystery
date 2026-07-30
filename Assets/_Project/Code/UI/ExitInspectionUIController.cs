using System;
using System.Collections.Generic;
using System.Linq;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;
using Wake.Core;
using Wake.Evidence;
using Wake.Exploration;
using Wake.Narrative;
using Wake.Puzzles;

namespace Wake.UI
{
    [DisallowMultipleComponent]
    public sealed class ExitInspectionUIController :
        MonoBehaviour,
        IRuntimeModalController
    {
        private enum ViewStage
        {
            Observe,
            Compare,
            Theory,
            Completed
        }

        private readonly List<Button> routeButtons = new();
        private readonly Dictionary<string, TMP_Text> routeFindings =
            new(StringComparer.Ordinal);
        private readonly Dictionary<string, Button> verdictButtons =
            new(StringComparer.Ordinal);
        private readonly Dictionary<ExitInspectionTheory, Button> theoryButtons =
            new();

        private GameObject root;
        private GameObject observePanel;
        private GameObject comparePanel;
        private GameObject theoryPanel;
        private CanvasGroup canvasGroup;
        private Button reopenButton;
        private Button hintButton;
        private Button primaryButton;
        private TMP_Text stageText;
        private TMP_Text progressText;
        private TMP_Text hintText;
        private TMP_Text statusText;
        private ExitInspectionSession session;
        private AmbientCharacterHotspotOverlay ambientCharacters;
        private ViewStage stage;
        private bool forceCompare;
        private bool routeVerdictsValidated;

        public bool IsOpen => root != null && root.activeSelf;
        public ExitInspectionSession Session => session;
        public string StatusMessage => statusText?.text ?? string.Empty;

        private void Awake() => BuildUi();
        private void OnEnable() => SetReopenVisibility();

        private void OnDisable()
        {
            root?.SetActive(false);
            reopenButton?.gameObject.SetActive(false);
            SetExplorationPresentationSuppressed(false);
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
                    state,
                    ExitInspectionCatalog.SceneId,
                    ExitInspectionCatalog.SessionId))
            {
                return false;
            }

            session = new ExitInspectionSession(
                state,
                inventory.Contains,
                inventory.TryAddById);
            forceCompare = false;
            SynchronizeAuthoredObservations();
            routeVerdictsValidated =
                session.SelectedTheory != ExitInspectionTheory.None;
            statusText.text = session.IsCompleted
                ? "이미 입증한 결론입니다."
                : session.Step == 0
                    ? "세 후보 출구를 모두 조사해 살아 있는 제3자가 빠져나갔는지 검증하세요."
                    : "저장된 관찰·판정·가설 상태를 복원했습니다.";
            reopenButton.gameObject.SetActive(false);
            SetExplorationPresentationSuppressed(true);
            RuntimeModalTransition.Open(
                root,
                () =>
                {
                    ApplyAuthoredLayout();
                    Refresh();
                    Canvas.ForceUpdateCanvases();
                    LayoutRebuilder.ForceRebuildLayoutImmediate(
                        root.GetComponent<RectTransform>());
                    FocusStageEntry();
                });
            return true;
        }

        public void Close()
        {
            RestoreInteraction();
            RuntimeModalTransition.Close(
                root,
                () =>
                {
                    SetExplorationPresentationSuppressed(false);
                    SetReopenVisibility();
                });
        }

        // Kept for old callers and save migration. It records authored
        // observations, but verdicts and the final theory are still required.
        public ExitInspectionResult Inspect(string inspectionId)
        {
            if (session == null)
            {
                return ExitInspectionResult.UnknownInspection;
            }

            ExitInspectionResult result = session.Inspect(inspectionId);
            statusText.text = result switch
            {
                ExitInspectionResult.Recorded =>
                    "관찰 두 항목을 기록했습니다. 이제 경로의 사용 여부를 판정하세요.",
                ExitInspectionResult.AlreadyInspected =>
                    "이미 관찰을 마친 경로입니다.",
                ExitInspectionResult.EvidenceUnavailable =>
                    "관찰 결과를 조사 기록에 보존하지 못했습니다.",
                ExitInspectionResult.SessionCompleted =>
                    "이미 완료된 출구 검증입니다.",
                _ => "등록되지 않은 검사 경로입니다."
            };
            Refresh();
            return result;
        }

        public bool UseHint()
        {
            bool changed = session != null && session.UseHint();
            statusText.text = changed
                ? $"힌트 {session.HintLevel}/3을 열었습니다. 이전 힌트도 아래 기록에서 확인할 수 있습니다."
                : "사용할 수 있는 힌트를 모두 확인했습니다.";
            Refresh();
            return changed;
        }

        public ExitInspectionAction SetVerdict(
            string inspectionId,
            ExitRouteVerdict verdict)
        {
            if (session == null)
            {
                return new ExitInspectionAction(
                    ExitInspectionActionCode.UnknownRoute,
                    false,
                    "출구 검증이 시작되지 않았습니다.");
            }

            ExitInspectionAction result =
                session.SetRouteVerdict(inspectionId, verdict);
            if (result.Accepted)
            {
                routeVerdictsValidated = false;
            }
            statusText.text = result.Message;
            Refresh();
            return result;
        }

        public ExitInspectionAction SelectTheory(ExitInspectionTheory theory)
        {
            if (session == null)
            {
                return new ExitInspectionAction(
                    ExitInspectionActionCode.InvalidTheory,
                    false,
                    "출구 검증이 시작되지 않았습니다.");
            }

            ExitInspectionAction result = session.SelectTheory(theory);
            statusText.text = result.Message;
            Refresh();
            return result;
        }

        public ExitInspectionCompletion Submit()
        {
            if (session == null)
            {
                return new ExitInspectionCompletion(
                    false,
                    Array.Empty<string>(),
                    Array.Empty<string>());
            }

            ExitInspectionCompletion result = session.TryComplete();
            statusText.text = result.Message;
            if (!result.Completed)
            {
                if (result.Failure ==
                    ExitInspectionCompletionFailure.IncorrectVerdicts)
                {
                    forceCompare = true;
                }
                ToastController.Instance?.Show(statusText.text);
                Refresh();
                return result;
            }

            ToastController.Instance?.ShowAlert(
                "논증 완료 · 사건 당시 살아 있는 제3자는 없었다");
            Refresh();
            if (!ContinueToNextScene())
            {
                Close();
                ToastController.Instance?.Show(
                    "검증을 완료했습니다. 지도에서 다음 조사 장소를 선택하세요.");
            }
            return result;
        }

        private void OpenDetailedInvestigation(
            ExitInspectionDefinition definition)
        {
            if (definition == null || session == null)
            {
                return;
            }

            InvestigationScreenController controller =
                InvestigationScreenController.Instance ??
                FindFirstObjectByType<InvestigationScreenController>();
            if (controller == null)
            {
                statusText.text = "세부 조사 화면을 열 수 없습니다.";
                return;
            }

            SuspendInteraction();
            bool opened = controller.Begin(
                definition.EvidenceId,
                () =>
                {
                    SynchronizeRouteObservation(definition);
                    RestoreInteraction();
                    Refresh();
                    statusText.text = session.HasInspected(definition.Id)
                        ? $"{definition.Title}의 관찰을 완료했습니다."
                        : $"{definition.Title}에 확인하지 않은 세부 흔적이 남아 있습니다.";
                    FocusStageEntry();
                });
            if (!opened)
            {
                RestoreInteraction();
                statusText.text =
                    $"{definition.Title} 세부 조사 화면을 열 수 없습니다.";
            }
        }

        private void SynchronizeAuthoredObservations()
        {
            foreach (ExitInspectionDefinition definition
                     in ExitInspectionCatalog.All)
            {
                SynchronizeRouteObservation(definition);
            }
        }

        private void SynchronizeRouteObservation(
            ExitInspectionDefinition definition)
        {
            if (session == null ||
                definition == null ||
                !InvestigationTargetCatalog.TryGet(
                    definition.EvidenceId,
                    out InvestigationTargetDefinition target))
            {
                return;
            }

            GameStateManager state = GameStateManager.Instance;
            foreach (InspectionPointDefinition point in target.Points)
            {
                if (state?.HasFlag(
                        InvestigationTargetCatalog.PointFlag(
                            target,
                            point.PointId)) == true)
                {
                    session.Observe(definition.Id, point.PointId);
                }
            }

            // Old saves could contain only the completion flag. Migrate those
            // as observed without inferring a verdict or theory.
            if (!session.HasInspected(definition.Id) &&
                state?.HasFlag(
                    InvestigationTargetCatalog.CompletionFlag(target)) == true)
            {
                session.Inspect(definition.Id);
            }
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
                           map.TryTravelToScene(
                               requirement.NextSceneId).IsAllowed;
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

            stage = ResolveStage();
            observePanel.SetActive(stage == ViewStage.Observe);
            comparePanel.SetActive(stage == ViewStage.Compare);
            theoryPanel.SetActive(stage == ViewStage.Theory);
            stageText.text = stage switch
            {
                ViewStage.Observe => "1 · 세 후보 출구의 판정 근거 수집",
                ViewStage.Compare => "2 · 사람의 통과 여부 판정",
                ViewStage.Theory => "3 · 가설 검증",
                _ => "검증 완료"
            };
            progressText.text = ProgressMessage();
            hintText.text = HintHistory(session.HintLevel);

            hintButton.interactable =
                !session.IsCompleted && session.HintLevel < 3;
            Label(hintButton).text = session.HintLevel < 3
                ? $"다음 힌트 보기 {session.HintLevel}/3"
                : "힌트 모두 확인";

            RefreshRoutes();
            RefreshVerdicts();
            RefreshTheories();
            RefreshPrimary();
            FitFeedbackHeight(statusText, 58f);
            FitFeedbackHeight(hintText, 30f);
            LayoutRebuilder.ForceRebuildLayoutImmediate(
                root.GetComponent<RectTransform>());
        }

        private void RefreshRoutes()
        {
            for (int index = 0; index < routeButtons.Count; index++)
            {
                ExitInspectionDefinition definition =
                    ExitInspectionCatalog.All[index];
                bool done = session.HasInspected(definition.Id);
                Button button = routeButtons[index];
                button.interactable = !session.IsCompleted;
                Label(button).text = done
                    ? $"{definition.Title} · 판정 근거 확보"
                    : $"{definition.Title} · 통과 흔적 조사하기";
                UiVisualThemeService.ApplyButton(
                    button,
                    done ? UiButtonStyle.Primary : UiButtonStyle.Secondary);

                if (routeFindings.TryGetValue(
                        definition.Id,
                        out TMP_Text finding))
                {
                    finding.text = done
                        ? definition.Finding
                        : "이 경로가 사건 당시 사람의 이동에 사용됐는지 판단할 두 가지 흔적을 확인하세요.";
                    finding.color = UiVisualThemeService.Resolve(
                        done
                            ? UiColorToken.TextPrimary
                            : UiColorToken.TextSecondary);
                }
            }
        }

        private void RefreshVerdicts()
        {
            foreach (ExitInspectionDefinition definition
                     in ExitInspectionCatalog.All)
            {
                ExitRouteVerdict selected =
                    session.GetVerdict(definition.Id);
                foreach (ExitRouteVerdict verdict in VerdictOptions())
                {
                    if (!verdictButtons.TryGetValue(
                            VerdictKey(definition.Id, verdict),
                            out Button button))
                    {
                        continue;
                    }

                    button.interactable =
                        !session.IsCompleted &&
                        session.HasInspected(definition.Id);
                    UiVisualThemeService.ApplyButton(
                        button,
                        selected == verdict
                            ? UiButtonStyle.Primary
                            : UiButtonStyle.Secondary);
                }
            }
        }

        private void RefreshTheories()
        {
            foreach ((ExitInspectionTheory theory, Button button)
                     in theoryButtons)
            {
                button.interactable = !session.IsCompleted;
                UiVisualThemeService.ApplyButton(
                    button,
                    session.SelectedTheory == theory
                        ? UiButtonStyle.Primary
                        : UiButtonStyle.Secondary);
            }
        }

        private void RefreshPrimary()
        {
            primaryButton.interactable = stage switch
            {
                ViewStage.Observe => false,
                ViewStage.Compare => ExitInspectionCatalog.All.All(
                    item =>
                        session.GetVerdict(item.Id) != ExitRouteVerdict.None),
                ViewStage.Theory =>
                    session.SelectedTheory != ExitInspectionTheory.None,
                _ => false
            };
            Label(primaryButton).text = stage switch
            {
                ViewStage.Observe => "모든 경로를 관찰하세요",
                ViewStage.Compare => primaryButton.interactable
                    ? "판정 비교 완료"
                    : "세 경로를 판정하세요",
                ViewStage.Theory => "선택한 가설 검증",
                _ => "검증 완료"
            };
            UiVisualThemeService.ApplyButton(
                primaryButton,
                UiButtonStyle.Primary);
        }

        private ViewStage ResolveStage()
        {
            if (session.IsCompleted)
            {
                return ViewStage.Completed;
            }
            if (ExitInspectionCatalog.All.Any(
                    item => !session.HasInspected(item.Id)))
            {
                return ViewStage.Observe;
            }
            if (forceCompare ||
                !routeVerdictsValidated ||
                ExitInspectionCatalog.All.Any(
                    item =>
                        session.GetVerdict(item.Id) ==
                        ExitRouteVerdict.None))
            {
                return ViewStage.Compare;
            }
            return ViewStage.Theory;
        }

        private string ProgressMessage()
        {
            int observed = session.Step;
            int verdicts = ExitInspectionCatalog.All.Count(
                item => session.GetVerdict(item.Id) != ExitRouteVerdict.None);
            return stage switch
            {
                ViewStage.Observe =>
                    $"근거 수집 {observed}/3 · 세 후보 출구마다 통과 여부를 판단할 흔적 2개를 확보하세요.",
                ViewStage.Compare =>
                    $"경로 판정 {verdicts}/3 · 사건 당시 살아 있는 제3자가 각 경로를 통과했는지 판단하세요.",
                ViewStage.Theory =>
                    "세 경로의 미사용 기록과 문턱 흔적을 함께 설명할 가설을 고르세요.",
                _ => "호라이즌 룸 출구 논증이 조사 기록에 보존됐습니다."
            };
        }

        private void HandlePrimaryAction()
        {
            if (stage == ViewStage.Compare &&
                ExitInspectionCatalog.All.All(
                    item =>
                        session.GetVerdict(item.Id) != ExitRouteVerdict.None))
            {
                ExitInspectionAction validation =
                    session.ValidateRouteVerdicts();
                statusText.text = validation.Message;
                if (!validation.Accepted)
                {
                    ToastController.Instance?.ShowAlert(validation.Message);
                    Refresh();
                    FocusStageEntry();
                    return;
                }

                ToastController.Instance?.ShowAlert(
                    "판정 정확 · 세 후보 출구 모두 미사용");
                forceCompare = false;
                routeVerdictsValidated = true;
                Refresh();
                FocusStageEntry();
                return;
            }

            if (stage == ViewStage.Theory)
            {
                Submit();
            }
        }

        private void SuspendInteraction()
        {
            if (canvasGroup == null)
            {
                return;
            }
            canvasGroup.interactable = false;
            canvasGroup.blocksRaycasts = false;
        }

        private void RestoreInteraction()
        {
            if (canvasGroup == null)
            {
                return;
            }
            canvasGroup.interactable = true;
            canvasGroup.blocksRaycasts = true;
        }

        private void ApplyAuthoredLayout()
        {
            if (root == null)
            {
                return;
            }

            ScreenShellRuntimePresenter.Place(
                root.GetComponent<RectTransform>(),
                ScreenShellSlotIds.PuzzlePanel,
                new Vector2(.04f, .06f),
                new Vector2(.96f, .94f));
        }

        private void SetExplorationPresentationSuppressed(bool suppressed)
        {
            ambientCharacters ??=
                FindFirstObjectByType<AmbientCharacterHotspotOverlay>(
                    FindObjectsInactive.Include);
            ambientCharacters?.SetModalPresentationSuppressed(suppressed);
        }

        private void FocusStageEntry()
        {
            Button target = stage switch
            {
                ViewStage.Observe =>
                    routeButtons.FirstOrDefault(button => button.interactable),
                ViewStage.Compare =>
                    verdictButtons.Values.FirstOrDefault(
                        button => button.interactable),
                ViewStage.Theory =>
                    theoryButtons.Values.FirstOrDefault(
                        button => button.interactable),
                _ => hintButton
            };
            EventSystem.current?.SetSelectedGameObject(target?.gameObject);
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
                           checkpoint.pendingInteractionId ==
                           ExitInspectionCatalog.SessionId &&
                           !state.HasCompletedScene(
                               ExitInspectionCatalog.SceneId);
            bool visible =
                pending &&
                ui?.ActivePanel == UiPrimaryPanel.Ingame &&
                !ui.IsSettingsOpen &&
                ui.OpenRuntimeModalCount == 0 &&
                DialogueController.Instance?.IsBusy != true;
            reopenButton.gameObject.SetActive(visible);
            if (!visible)
            {
                return;
            }

            int count = session?.Step ?? 0;
            if (session == null &&
                state.TryGetPuzzleSession(
                    ExitInspectionCatalog.SessionId,
                    out PuzzleSessionState saved))
            {
                count = saved.completed
                    ? ExitInspectionCatalog.All.Count
                    : Mathf.Clamp(saved.step, 0, ExitInspectionCatalog.All.Count);
            }
            Label(reopenButton).text =
                $"출구 검증 재개 · 관찰 {count}/3";
        }

        private static string HintHistory(int level) => level switch
        {
            <= 0 => "힌트 기록 없음 · 필요하면 ‘다음 힌트 보기’를 선택하세요.",
            1 => "힌트 1 · 경로가 사용됐다면 반드시 남아야 할 흔적부터 찾으세요.",
            2 => "힌트 1 · 경로가 사용됐다면 반드시 남아야 할 흔적부터 찾으세요.\n" +
                 "힌트 2 · 표면의 물리 흔적과 센서·구조 기록을 함께 비교하세요.",
            _ => "힌트 1 · 경로가 사용됐다면 반드시 남아야 할 흔적부터 찾으세요.\n" +
                 "힌트 2 · 표면의 물리 흔적과 센서·구조 기록을 함께 비교하세요.\n" +
                 "힌트 3 · 출구가 아니라 사건 당시 방 안에 누가 있었는지 전제를 의심하세요."
        };

        private void BuildUi()
        {
            Transform canvas = GameObject.Find("Canvas")?.transform;
            if (canvas == null)
            {
                return;
            }

            root = CreateSurface(
                "Exit Inspection",
                canvas,
                UiSurfaceStyle.Overlay);
            ScreenShellRuntimePresenter.Place(
                root.GetComponent<RectTransform>(),
                ScreenShellSlotIds.PuzzlePanel,
                new Vector2(.04f, .06f),
                new Vector2(.96f, .94f));
            root.AddComponent<ScreenShellRuntimePresenter>()
                .Configure(ScreenShellType.Puzzle);
            canvasGroup = root.AddComponent<CanvasGroup>();

            VerticalLayoutGroup layout =
                root.AddComponent<VerticalLayoutGroup>();
            layout.padding = new RectOffset(28, 28, 24, 22);
            layout.spacing = 8f;
            layout.childAlignment = TextAnchor.UpperCenter;
            layout.childControlWidth = true;
            layout.childControlHeight = true;
            layout.childForceExpandWidth = true;
            layout.childForceExpandHeight = false;

            TMP_Text title = CreateLayoutText(
                root.transform,
                "Title",
                "흔적 없는 출구 검증",
                UiTextStyle.Heading,
                46f,
                TextAlignmentOptions.Center);
            stageText = CreateLayoutText(
                root.transform,
                "Stage",
                string.Empty,
                UiTextStyle.Technical,
                30f,
                TextAlignmentOptions.Center);
            progressText = CreateLayoutText(
                root.transform,
                "Progress",
                string.Empty,
                UiTextStyle.Body,
                34f,
                TextAlignmentOptions.Center);

            observePanel = BuildObservePanel(root.transform);
            comparePanel = BuildComparePanel(root.transform);
            theoryPanel = BuildTheoryPanel(root.transform);

            statusText = CreateLayoutText(
                root.transform,
                "Feedback",
                string.Empty,
                UiTextStyle.BodyLarge,
                58f,
                TextAlignmentOptions.Center);
            hintText = CreateLayoutText(
                root.transform,
                "Hint Text",
                string.Empty,
                UiTextStyle.Caption,
                30f,
                TextAlignmentOptions.Center);

            GameObject footer = CreateHorizontalGroup(
                root.transform,
                "Footer",
                62f,
                12f);
            Button close = CreateLayoutButton(
                footer.transform,
                "Close",
                "나가기 · 진행 저장",
                UiButtonStyle.Quiet,
                190f);
            close.onClick.AddListener(Close);
            hintButton = CreateLayoutButton(
                footer.transform,
                "Hint",
                "다음 힌트 보기 0/3",
                UiButtonStyle.Quiet,
                210f);
            hintButton.onClick.AddListener(() => UseHint());
            AddFlexibleSpacer(footer.transform);
            primaryButton = CreateLayoutButton(
                footer.transform,
                "Primary Action",
                "가설 검증",
                UiButtonStyle.Primary,
                250f);
            primaryButton.onClick.AddListener(HandlePrimaryAction);

            reopenButton = CreateAnchoredButton(
                canvas,
                "Exit Inspection Resume",
                "출구 검증 재개",
                new Rect(.38f, .035f, .24f, .06f));
            reopenButton.onClick.AddListener(() => Open());

            root.SetActive(false);
            reopenButton.gameObject.SetActive(false);
        }

        private GameObject BuildObservePanel(Transform parent)
        {
            GameObject panel = CreateHorizontalGroup(
                parent,
                "Observe Stage",
                0f,
                12f);
            panel.GetComponent<LayoutElement>().flexibleHeight = 1f;

            foreach (ExitInspectionDefinition definition
                     in ExitInspectionCatalog.All)
            {
                ExitInspectionDefinition captured = definition;
                GameObject card = CreateVerticalCard(
                    panel.transform,
                    $"Exit Route {definition.Id}");
                TMP_Text routeTitle = CreateLayoutText(
                    card.transform,
                    "Route Title",
                    definition.Title,
                    UiTextStyle.SpeakerName,
                    42f,
                    TextAlignmentOptions.Center);
                Button button = CreateLayoutButton(
                    card.transform,
                    $"Inspection {definition.Id}",
                    $"{definition.Title} · 조사하기",
                    UiButtonStyle.Secondary,
                    0f,
                    62f);
                button.onClick.AddListener(
                    () => OpenDetailedInvestigation(captured));
                routeButtons.Add(button);
                TMP_Text finding = CreateLayoutText(
                    card.transform,
                    "Finding",
                    string.Empty,
                    UiTextStyle.Body,
                    0f,
                    TextAlignmentOptions.TopLeft);
                finding.GetComponent<LayoutElement>().flexibleHeight = 1f;
                routeFindings[definition.Id] = finding;
                _ = routeTitle;
            }
            return panel;
        }

        private GameObject BuildComparePanel(Transform parent)
        {
            GameObject panel = CreateVerticalGroup(
                parent,
                "Compare Stage",
                0f,
                10f);
            panel.GetComponent<LayoutElement>().flexibleHeight = 1f;

            CreateLayoutText(
                panel.transform,
                "Premise",
                "판정 질문 · 사건 당시 살아 있는 제3자가 이 경로를 통과했는가?\n" +
                "문턱 기록과 각 경로에서 확보한 물리 흔적을 함께 비교하세요.",
                UiTextStyle.Body,
                48f,
                TextAlignmentOptions.Center);
            foreach (ExitInspectionDefinition definition
                     in ExitInspectionCatalog.All)
            {
                GameObject row = CreateHorizontalGroup(
                    panel.transform,
                    $"Exit Verdict Row {definition.Id}",
                    70f,
                    8f);
                TMP_Text label = CreateLayoutText(
                    row.transform,
                    "Route",
                    definition.Title,
                    UiTextStyle.SpeakerName,
                    0f,
                    TextAlignmentOptions.MidlineLeft);
                LayoutElement labelLayout =
                    label.GetComponent<LayoutElement>();
                labelLayout.preferredWidth = 190f;
                labelLayout.flexibleWidth = .8f;

                foreach (ExitRouteVerdict verdict in VerdictOptions())
                {
                    string routeId = definition.Id;
                    ExitRouteVerdict captured = verdict;
                    Button button = CreateLayoutButton(
                        row.transform,
                        $"Exit Verdict {routeId} {verdict}",
                        VerdictLabel(verdict),
                        UiButtonStyle.Secondary,
                        175f,
                        56f);
                    button.onClick.AddListener(
                        () => SetVerdict(routeId, captured));
                    verdictButtons[VerdictKey(routeId, verdict)] = button;
                }
            }
            return panel;
        }

        private GameObject BuildTheoryPanel(Transform parent)
        {
            GameObject panel = CreateVerticalGroup(
                parent,
                "Theory Stage",
                0f,
                10f);
            panel.GetComponent<LayoutElement>().flexibleHeight = 1f;
            CreateLayoutText(
                panel.transform,
                "Question",
                "세 경로와 문턱이 모두 사용되지 않았다면, 사건 당시 무엇이 사실이었는가?",
                UiTextStyle.BodyLarge,
                54f,
                TextAlignmentOptions.Center);

            AddTheoryButton(
                panel.transform,
                ExitInspectionTheory.PerfectCleanup,
                "범인이 모든 흔적을 완벽히 지웠다");
            AddTheoryButton(
                panel.transform,
                ExitInspectionTheory.DoorExit,
                "피해자가 문으로 범인을 내보냈다");
            AddTheoryButton(
                panel.transform,
                ExitInspectionTheory.NoLiveThirdParty,
                "사건 당시 방 안에 살아 있는 제3자가 없었다");
            Button review = CreateLayoutButton(
                panel.transform,
                "Review Exit Verdicts",
                "경로 판정 다시 보기",
                UiButtonStyle.Secondary,
                0f,
                54f);
            review.onClick.AddListener(() =>
            {
                forceCompare = true;
                Refresh();
                FocusStageEntry();
            });
            return panel;
        }

        private void AddTheoryButton(
            Transform parent,
            ExitInspectionTheory theory,
            string label)
        {
            Button button = CreateLayoutButton(
                parent,
                $"Exit Theory {theory}",
                label,
                UiButtonStyle.Secondary,
                0f,
                66f);
            button.onClick.AddListener(() => SelectTheory(theory));
            theoryButtons[theory] = button;
        }

        private static GameObject CreateSurface(
            string name,
            Transform parent,
            UiSurfaceStyle style)
        {
            GameObject target = new(
                name,
                typeof(RectTransform),
                typeof(CanvasRenderer),
                typeof(Image));
            target.transform.SetParent(parent, false);
            UiVisualThemeService.ApplySurface(
                target.GetComponent<Image>(),
                style);
            return target;
        }

        private static GameObject CreateVerticalCard(
            Transform parent,
            string name)
        {
            GameObject card = CreateSurface(
                name,
                parent,
                UiSurfaceStyle.RaisedPanel);
            LayoutElement element = card.AddComponent<LayoutElement>();
            element.flexibleWidth = 1f;
            element.flexibleHeight = 1f;
            VerticalLayoutGroup layout =
                card.AddComponent<VerticalLayoutGroup>();
            layout.padding = new RectOffset(16, 16, 14, 14);
            layout.spacing = 10f;
            layout.childControlWidth = true;
            layout.childControlHeight = true;
            layout.childForceExpandWidth = true;
            layout.childForceExpandHeight = false;
            return card;
        }

        private static GameObject CreateVerticalGroup(
            Transform parent,
            string name,
            float preferredHeight,
            float spacing)
        {
            GameObject target = new(name, typeof(RectTransform));
            target.transform.SetParent(parent, false);
            LayoutElement element = target.AddComponent<LayoutElement>();
            element.preferredHeight = preferredHeight;
            VerticalLayoutGroup layout =
                target.AddComponent<VerticalLayoutGroup>();
            layout.spacing = spacing;
            layout.childControlWidth = true;
            layout.childControlHeight = true;
            layout.childForceExpandWidth = true;
            layout.childForceExpandHeight = false;
            return target;
        }

        private static GameObject CreateHorizontalGroup(
            Transform parent,
            string name,
            float preferredHeight,
            float spacing)
        {
            GameObject target = new(name, typeof(RectTransform));
            target.transform.SetParent(parent, false);
            LayoutElement element = target.AddComponent<LayoutElement>();
            element.preferredHeight = preferredHeight;
            HorizontalLayoutGroup layout =
                target.AddComponent<HorizontalLayoutGroup>();
            layout.spacing = spacing;
            layout.childControlWidth = true;
            layout.childControlHeight = true;
            layout.childForceExpandWidth = false;
            layout.childForceExpandHeight = true;
            return target;
        }

        private static TMP_Text CreateLayoutText(
            Transform parent,
            string name,
            string value,
            UiTextStyle style,
            float preferredHeight,
            TextAlignmentOptions alignment)
        {
            GameObject target = new(
                name,
                typeof(RectTransform),
                typeof(CanvasRenderer),
                typeof(TextMeshProUGUI),
                typeof(LayoutElement));
            target.transform.SetParent(parent, false);
            LayoutElement element = target.GetComponent<LayoutElement>();
            element.preferredHeight = preferredHeight;
            TMP_Text text = target.GetComponent<TMP_Text>();
            text.text = value;
            text.alignment = alignment;
            text.textWrappingMode = TextWrappingModes.Normal;
            text.overflowMode = TextOverflowModes.Overflow;
            UiVisualThemeService.ApplyText(text, style);
            ScreenShellRuntimePresenter.PrepareReadableText(text, 18f);
            return text;
        }

        private static void FitFeedbackHeight(
            TMP_Text text,
            float minimumHeight)
        {
            if (text == null)
            {
                return;
            }

            RectTransform rect = text.rectTransform;
            float availableWidth = rect.rect.width;
            if (availableWidth <= 1f &&
                rect.parent is RectTransform parent)
            {
                availableWidth = Mathf.Max(120f, parent.rect.width - 56f);
            }

            float preferredHeight = text.GetPreferredValues(
                text.text,
                Mathf.Max(120f, availableWidth),
                0f).y;
            LayoutElement element = text.GetComponent<LayoutElement>();
            element.preferredHeight = Mathf.Max(
                minimumHeight,
                preferredHeight + 10f);
        }

        private static Button CreateLayoutButton(
            Transform parent,
            string name,
            string label,
            UiButtonStyle style,
            float preferredWidth,
            float preferredHeight = 58f)
        {
            GameObject target = CreateSurface(
                name,
                parent,
                UiSurfaceStyle.RaisedPanel);
            Button button = target.AddComponent<Button>();
            button.targetGraphic = target.GetComponent<Image>();
            LayoutElement element = target.AddComponent<LayoutElement>();
            element.preferredWidth = preferredWidth;
            element.preferredHeight = preferredHeight;
            element.minHeight = 52f;
            if (preferredWidth <= 0f)
            {
                element.flexibleWidth = 1f;
            }
            TMP_Text text = CreateStretchedLabel(target.transform, label);
            UiVisualThemeService.ApplyButton(button, style);
            ScreenShellRuntimePresenter.PrepareButton(
                button,
                preferredWidth > 0f ? preferredWidth : 120f,
                52f);
            text.textWrappingMode = TextWrappingModes.Normal;
            return button;
        }

        private static Button CreateAnchoredButton(
            Transform parent,
            string name,
            string label,
            Rect normalizedRect)
        {
            GameObject target = CreateSurface(
                name,
                parent,
                UiSurfaceStyle.RaisedPanel);
            RectTransform rect = target.GetComponent<RectTransform>();
            rect.anchorMin = normalizedRect.min;
            rect.anchorMax = normalizedRect.max;
            rect.offsetMin = Vector2.zero;
            rect.offsetMax = Vector2.zero;
            Button button = target.AddComponent<Button>();
            button.targetGraphic = target.GetComponent<Image>();
            CreateStretchedLabel(target.transform, label);
            UiVisualThemeService.ApplyButton(
                button,
                UiButtonStyle.Secondary);
            ScreenShellRuntimePresenter.PrepareButton(button, 180f, 52f);
            return button;
        }

        private static TMP_Text CreateStretchedLabel(
            Transform parent,
            string value)
        {
            GameObject target = new(
                "Label",
                typeof(RectTransform),
                typeof(CanvasRenderer),
                typeof(TextMeshProUGUI));
            target.transform.SetParent(parent, false);
            RectTransform rect = target.GetComponent<RectTransform>();
            rect.anchorMin = Vector2.zero;
            rect.anchorMax = Vector2.one;
            rect.offsetMin = new Vector2(12f, 6f);
            rect.offsetMax = new Vector2(-12f, -6f);
            TMP_Text text = target.GetComponent<TMP_Text>();
            text.text = value;
            text.alignment = TextAlignmentOptions.Center;
            text.raycastTarget = false;
            text.textWrappingMode = TextWrappingModes.Normal;
            text.overflowMode = TextOverflowModes.Overflow;
            UiVisualThemeService.ApplyText(text, UiTextStyle.Choice);
            ScreenShellRuntimePresenter.PrepareReadableText(text, 18f);
            return text;
        }

        private static void AddFlexibleSpacer(Transform parent)
        {
            GameObject spacer = new("Spacer", typeof(RectTransform));
            spacer.transform.SetParent(parent, false);
            spacer.AddComponent<LayoutElement>().flexibleWidth = 1f;
        }

        private static IEnumerable<ExitRouteVerdict> VerdictOptions()
        {
            yield return ExitRouteVerdict.Used;
            yield return ExitRouteVerdict.Unused;
            yield return ExitRouteVerdict.Inconclusive;
        }

        private static string VerdictKey(
            string routeId,
            ExitRouteVerdict verdict) =>
            $"{ExitInspectionDefinition.Normalize(routeId)}:{verdict}";

        private static string VerdictLabel(ExitRouteVerdict verdict) =>
            verdict switch
            {
                ExitRouteVerdict.Used => "통과함\n(사용 흔적 있음)",
                ExitRouteVerdict.Unused => "통과하지 않음\n(사용 흔적 없음)",
                ExitRouteVerdict.Inconclusive => "증거 부족\n(판단 보류)",
                _ => "미선택"
            };

        private static TMP_Text Label(Button button) =>
            button?.GetComponentInChildren<TMP_Text>(true);
    }
}
