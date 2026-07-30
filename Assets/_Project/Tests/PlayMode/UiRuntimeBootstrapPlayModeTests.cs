using System.Collections;
using System.Linq;
using NUnit.Framework;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.TestTools;
using UnityEngine.UI;
using Wake.Core;
using Wake.Evidence;
using Wake.Exploration;
using Wake.Puzzles;
using Wake.UI;

namespace Wake.Tests.PlayMode
{
    public sealed class UiRuntimeBootstrapPlayModeTests :
        UiBasicScenePlayModeFixture
    {
        [UnityTest]
        public IEnumerator Reinitialization_KeepsOneControllerAndListener()
        {
            Assert.That(Ui.IsInitialized, Is.True);
            Assert.That(Ui.ActivePanel, Is.EqualTo(UiPrimaryPanel.Start));
            Assert.That(
                RequireObject("Status HUD").activeSelf,
                Is.False,
                "시작 화면에서는 게임 상태 HUD가 숨겨져야 합니다.");
            Assert.That(Ui.RuntimeModalControllerCount, Is.EqualTo(11));

            Assert.That(Ui.EnsureInitialized(), Is.True);
            Assert.That(Ui.EnsureInitialized(), Is.True);
            Assert.That(Ui.RuntimeModalControllerCount, Is.EqualTo(11));

            GameObject ingame = RequireObject("Ingame");
            Assert.That(
                ingame.GetComponents<ExitInspectionUIController>(),
                Has.Length.EqualTo(1));
            Assert.That(
                ingame.GetComponents<ProductionPuzzleUIController>(),
                Has.Length.EqualTo(1));
            Assert.That(
                ingame.GetComponents<BloodDirectionPuzzleUIController>(),
                Has.Length.EqualTo(1));
            Assert.That(
                ingame.GetComponents<CameraBlindSpotUIController>(),
                Has.Length.EqualTo(1));
            Assert.That(
                ingame.GetComponents<FinalAccusationUIController>(),
                Has.Length.EqualTo(1));
            Assert.That(
                ingame.GetComponents<MarcusInterrogationUIController>(),
                Has.Length.EqualTo(1));
            Assert.That(
                ingame.GetComponents<TimelinePuzzleUIController>(),
                Has.Length.EqualTo(1));
            Assert.That(
                ingame.GetComponents<OrpheusAudioRestorationUIController>(),
                Has.Length.EqualTo(1));
            Assert.That(
                ingame.GetComponents<ProductionEndingUIController>(),
                Has.Length.EqualTo(1));
            Assert.That(
                ingame.GetComponents<ObjectiveMapHUDController>(),
                Has.Length.EqualTo(1));
            Assert.That(
                RequireObject("Evidence")
                    .GetComponents<EvidenceTheoryBoardController>(),
                Has.Length.EqualTo(1));

            int newGameFeedbackCount = 0;
            State.FeedbackRequested += _ => newGameFeedbackCount++;
            yield return StartNewGameFromVisibleButton();
            Assert.That(newGameFeedbackCount, Is.EqualTo(1));

            var duplicateHost = new GameObject("Duplicate UIManager");
            UIManager duplicate = duplicateHost.AddComponent<UIManager>();
            Assert.That(duplicate.enabled, Is.False);
            Assert.That(UIManager.Instance, Is.SameAs(Ui));
            Object.Destroy(duplicateHost);
            yield return null;
            AssertNoRuntimeErrors("UI 런타임 중복 초기화");
        }

        [UnityTest]
        public IEnumerator BloodPuzzle_DragSwapsPiecesAndClickStillRotates()
        {
            BloodDirectionPuzzleUIController controller =
                RequireObject("Ingame")
                    .GetComponent<BloodDirectionPuzzleUIController>();
            Assert.That(controller, Is.Not.Null);
            Assert.That(controller.Open(), Is.True);
            yield return null;
            UnityEngine.Canvas.ForceUpdateCanvases();
            Assert.That(
                LocationLoader.Instance.IsWorldInteractionSuppressed,
                Is.True);

            BloodPuzzlePieceView[] pieces =
                Object.FindObjectsByType<BloodPuzzlePieceView>(
                    FindObjectsInactive.Include,
                    FindObjectsSortMode.None);
            BloodPuzzlePieceView source = pieces.Single(
                item => item.name.EndsWith("Slot 1"));
            BloodPuzzlePieceView destination = pieces.Single(
                item => item.name.EndsWith("Slot 2"));
            Assert.That(source, Is.InstanceOf<IDragHandler>());

            int firstPiece = controller.Puzzle.Pieces[0];
            int secondPiece = controller.Puzzle.Pieces[1];
            Canvas canvas = source.GetComponentInParent<Canvas>().rootCanvas;
            Camera eventCamera = canvas.renderMode ==
                                 RenderMode.ScreenSpaceOverlay
                ? null
                : canvas.worldCamera;
            Vector2 sourcePosition =
                RectTransformUtility.WorldToScreenPoint(
                    eventCamera,
                    source.transform.position);
            Vector2 destinationPosition =
                RectTransformUtility.WorldToScreenPoint(
                    eventCamera,
                    destination.transform.position);
            var eventData = new PointerEventData(EventSystem.current)
            {
                button = PointerEventData.InputButton.Left,
                position = sourcePosition,
                pressPosition = sourcePosition,
                pointerDrag = source.gameObject
            };

            ExecuteEvents.Execute(
                source.gameObject,
                eventData,
                ExecuteEvents.pointerDownHandler);
            ExecuteEvents.Execute(
                source.gameObject,
                eventData,
                ExecuteEvents.beginDragHandler);
            eventData.position = destinationPosition;
            ExecuteEvents.Execute(
                source.gameObject,
                eventData,
                ExecuteEvents.dragHandler);
            ExecuteEvents.Execute(
                source.gameObject,
                eventData,
                ExecuteEvents.endDragHandler);
            yield return null;

            Assert.That(controller.Puzzle.Pieces[0], Is.EqualTo(secondPiece));
            Assert.That(controller.Puzzle.Pieces[1], Is.EqualTo(firstPiece));
            Assert.That(
                GameObject.Find("Blood Piece Drag Preview"),
                Is.Null);

            int rotationBeforeClick = controller.Puzzle.Rotations[0];
            var clickData = new PointerEventData(EventSystem.current)
            {
                button = PointerEventData.InputButton.Left,
                position = sourcePosition
            };
            ExecuteEvents.Execute(
                source.gameObject,
                clickData,
                ExecuteEvents.pointerDownHandler);
            ExecuteEvents.Execute(
                source.gameObject,
                clickData,
                ExecuteEvents.pointerClickHandler);
            Assert.That(
                controller.Puzzle.Rotations[0],
                Is.EqualTo((rotationBeforeClick + 1) % 4));

            controller.Puzzle.SetSolvedReconstruction();
            controller.RotatePiece(0);
            yield return null;
            UnityEngine.Canvas.ForceUpdateCanvases();

            BloodAnalysisToolDrag[] analysisTools =
                Object.FindObjectsByType<BloodAnalysisToolDrag>(
                    FindObjectsInactive.Include,
                    FindObjectsSortMode.None);
            BloodAnalysisToolDrag postureTool = analysisTools.First(
                item => item.Kind == BloodAnalysisToolKind.Posture &&
                        item.PostureIndex == 1);
            BloodAnalysisToolDrag woundTool = analysisTools.Single(
                item => item.Kind == BloodAnalysisToolKind.WoundMarker);
            BloodAnalysisToolDrag poolTool = analysisTools.Single(
                item => item.Kind == BloodAnalysisToolKind.PoolMarker);
            RectTransform board = Object
                .FindFirstObjectByType<BloodPuzzleBoardClick>(
                    FindObjectsInactive.Include)
                .GetComponent<RectTransform>();

            DragAnalysisTool(
                postureTool,
                board,
                new Vector2(0.68f, 0.38f));
            Assert.That(controller.Puzzle.SelectedPosture, Is.EqualTo(1));
            DragAnalysisTool(
                woundTool,
                board,
                new Vector2(0.2f, 0.2f));
            Assert.That(
                Vector2.Distance(
                    controller.Puzzle.WoundMarker.Value,
                    new Vector2(0.2f, 0.2f)),
                Is.LessThan(0.01f));
            DragAnalysisTool(
                poolTool,
                board,
                new Vector2(0.8f, 0.8f));
            yield return null;
            Assert.That(
                Vector2.Distance(
                    controller.Puzzle.PoolMarker.Value,
                    new Vector2(0.8f, 0.8f)),
                Is.LessThan(0.01f));
            Assert.That(
                controller.Puzzle.Stage,
                Is.EqualTo(BloodDirectionStage.ChooseConclusion));
            Assert.That(
                GameObject.Find("Analysis Tool Drag Preview"),
                Is.Null);

            controller.Close();
            Assert.That(
                LocationLoader.Instance.IsWorldInteractionSuppressed,
                Is.False);
            AssertNoRuntimeErrors("혈흔 퍼즐 드래그와 클릭 입력");
        }

        private static void DragAnalysisTool(
            BloodAnalysisToolDrag tool,
            RectTransform board,
            Vector2 normalizedBoardPosition)
        {
            Canvas canvas = tool.GetComponentInParent<Canvas>().rootCanvas;
            Camera eventCamera = canvas.renderMode ==
                                 RenderMode.ScreenSpaceOverlay
                ? null
                : canvas.worldCamera;
            Vector2 sourcePosition =
                RectTransformUtility.WorldToScreenPoint(
                    eventCamera,
                    tool.transform.position);
            Rect boardRect = board.rect;
            Vector3 localTarget = new(
                Mathf.Lerp(
                    boardRect.xMin,
                    boardRect.xMax,
                    normalizedBoardPosition.x),
                Mathf.Lerp(
                    boardRect.yMin,
                    boardRect.yMax,
                    normalizedBoardPosition.y));
            Vector2 targetPosition =
                RectTransformUtility.WorldToScreenPoint(
                    eventCamera,
                    board.TransformPoint(localTarget));
            var eventData = new PointerEventData(EventSystem.current)
            {
                button = PointerEventData.InputButton.Left,
                position = sourcePosition,
                pressPosition = sourcePosition,
                pointerDrag = tool.gameObject
            };

            ExecuteEvents.Execute(
                tool.gameObject,
                eventData,
                ExecuteEvents.beginDragHandler);
            eventData.position = targetPosition;
            ExecuteEvents.Execute(
                tool.gameObject,
                eventData,
                ExecuteEvents.dragHandler);
            ExecuteEvents.Execute(
                tool.gameObject,
                eventData,
                ExecuteEvents.endDragHandler);
        }

        [UnityTest]
        public IEnumerator EvidenceRecord_UsesAuthoredMetadataAndScrollViewport()
        {
            Assert.That(
                EvidenceInventory.Instance.TryAddById("C-01"),
                Is.True);

            Ui.ShowEvidence();
            yield return WaitForUiTransition();

            TMP_Text title =
                RequireComponent<TMP_Text>("Evidence/Text (TMP)");
            TMP_Text acquisition =
                RequireComponent<TMP_Text>("Evidence/Acquisition Place");
            TMP_Text relatedPeople =
                RequireComponent<TMP_Text>("Evidence/Related People");
            TMP_Text reliability =
                RequireComponent<TMP_Text>("Evidence/Reliability");
            TMP_Text description =
                RequireComponent<TMP_Text>(
                    "Evidence/Description Viewport/Description");
            ScrollRect scroll =
                RequireComponent<ScrollRect>(
                    "Evidence/Description Viewport");

            Assert.That(title.text, Is.Not.Empty);
            Assert.That(title.text, Does.Not.Contain("C-"));
            Assert.That(acquisition.text, Does.Contain("획득 장소"));
            Assert.That(relatedPeople.text, Does.Contain("관련 인물"));
            Assert.That(reliability.text, Is.Not.Empty);
            Assert.That(
                description.overflowMode,
                Is.EqualTo(TextOverflowModes.Overflow));
            Assert.That(scroll.vertical, Is.True);
            Assert.That(scroll.content, Is.SameAs(description.rectTransform));
            Assert.That(
                RequireObject("Evidence/Turn").activeSelf,
                Is.False,
                "Single-view evidence must not show a rotate control.");
            Assert.That(
                RequireObject("Evidence/Turn (1)").activeSelf,
                Is.False,
                "Single-view evidence must not show a rotate control.");
            Assert.That(
                RequireObject("Evidence").transform.Find("Turn (3)"),
                Is.Null,
                "The obsolete legacy Turn button must not be authored.");

            RectTransform next =
                RequireComponent<RectTransform>("Evidence/Next");
            RectTransform compare =
                RequireComponent<RectTransform>("Evidence/Turn (2)");
            Assert.That(next.rect.width, Is.GreaterThan(240f));
            Assert.That(compare.rect.width, Is.GreaterThan(240f));
            Bounds descriptionBounds =
                RectTransformUtility.CalculateRelativeRectTransformBounds(
                    Canvas.transform,
                    scroll.transform);
            Bounds compareBounds =
                RectTransformUtility.CalculateRelativeRectTransformBounds(
                    Canvas.transform,
                    compare);
            Assert.That(
                descriptionBounds.Intersects(compareBounds),
                Is.False,
                "Record comparison must not cover the description.");

            TMP_Text carouselLabel =
                RequireObject("Evidence/Evidences")
                    .GetComponentsInChildren<TMP_Text>(false)
                    .First();
            Assert.That(carouselLabel.maxVisibleLines, Is.EqualTo(2));
            Assert.That(
                carouselLabel.overflowMode,
                Is.EqualTo(TextOverflowModes.Ellipsis));
            AssertNoRuntimeErrors("조사 기록 상세 화면");
        }

        [UnityTest]
        public IEnumerator EvidenceAcquisitionNotices_QueueThreeOnRightAndReflow()
        {
            EvidenceAcquisitionNoticeController controller =
                Object.FindFirstObjectByType<
                    EvidenceAcquisitionNoticeController>();
            Assert.That(controller, Is.Not.Null);

            Assert.That(EvidenceInventory.Instance.TryAddById("C-01"), Is.True);
            Assert.That(EvidenceInventory.Instance.TryAddById("C-02"), Is.True);
            Assert.That(EvidenceInventory.Instance.TryAddById("C-03"), Is.True);
            Assert.That(EvidenceInventory.Instance.TryAddById("C-04"), Is.True);

            yield return new WaitForSecondsRealtime(0.35f);

            Assert.That(
                controller.VisibleNoticeCount,
                Is.EqualTo(
                    EvidenceAcquisitionNoticeController
                        .MaximumVisibleNotices));
            Assert.That(controller.PendingNoticeCount, Is.EqualTo(1));
            Assert.That(
                controller.VisibleMessages.Select(
                    message => message.Split('\n').Last()),
                Is.EqualTo(new[]
                {
                    "다니엘의 초대장",
                    "열린 출입문",
                    "외벽 발판"
                }));

            RectTransform first = RequireComponent<RectTransform>(
                "Evidence Acquisition Notice 1");
            Image background = first.GetComponent<Image>();
            Assert.That(first.anchorMin, Is.EqualTo(Vector2.one));
            Assert.That(first.anchorMax, Is.EqualTo(Vector2.one));
            Assert.That(first.anchoredPosition.x, Is.LessThan(0f));
            Assert.That(first.rect.width, Is.LessThan(340f));
            Assert.That(background.color.a, Is.EqualTo(1f));
            Assert.That(background.color.r, Is.Zero.Within(0.001f));
            Assert.That(background.color.g, Is.Zero.Within(0.001f));
            Assert.That(background.color.b, Is.Zero.Within(0.001f));
            Assert.That(first.GetComponent<Outline>(), Is.Null);

            yield return new WaitForSecondsRealtime(2.55f);

            Assert.That(controller.VisibleNoticeCount, Is.EqualTo(3));
            Assert.That(controller.PendingNoticeCount, Is.Zero);
            Assert.That(
                controller.VisibleMessages.Select(
                    message => message.Split('\n').Last()),
                Is.EqualTo(new[]
                {
                    "열린 출입문",
                    "외벽 발판",
                    "덕트 먼지"
                }));
            AssertNoRuntimeErrors("단서 획득 알림 큐");
        }

        [UnityTest]
        public IEnumerator LegacyTopToast_DoesNotCreateAVisualSurface()
        {
            Assert.That(ToastController.RuntimeSurfaceEnabled, Is.False);

            ToastController.Instance?.Show(
                "천장 조사 개방 · 단서 C-08 FireDetector");
            yield return null;

            Assert.That(
                Object.FindObjectsByType<RectTransform>(
                        FindObjectsInactive.Include,
                        FindObjectsSortMode.None)
                    .Any(rect => rect.name == "Toast"),
                Is.False);
            AssertNoRuntimeErrors("상단 Toast 제거");
        }

        [UnityTest]
        public IEnumerator NewGame_ShowsNaturalLanguageObjectiveHud()
        {
            yield return StartNewGameFromVisibleButton(
                startOpeningDialogue: false);
            yield return null;

            Assert.That(Ui.ActivePanel, Is.EqualTo(UiPrimaryPanel.Ingame));
            GameObject legacyObjective =
                RequireObject("Ingame/Objective HUD");
            Assert.That(
                legacyObjective.activeInHierarchy,
                Is.False,
                "The legacy objective overlay must not duplicate the authored top HUD.");
            GameObject context = RequireObject(
                "Exploration Global Navigation/Exploration Context");
            Assert.That(context.activeInHierarchy, Is.True);
            Assert.That(
                context.transform.Find("World Time")
                    ?.GetComponent<TMP_Text>()?.text,
                Does.Match(@"^DAY \d+ · .+ \d+:\d{2}$"));
            Assert.That(
                context.transform.Find("Current Location")
                    ?.GetComponent<TMP_Text>()?.text,
                Is.EqualTo("항구"));
            TMP_Text currentLocation = context.transform
                .Find("Current Location")
                ?.GetComponent<TMP_Text>();
            Assert.That(currentLocation, Is.Not.Null);
            Assert.That(currentLocation.gameObject.activeInHierarchy, Is.True);
            Assert.That(currentLocation.enableAutoSizing, Is.True);
            Assert.That(
                currentLocation.fontSizeMax,
                Is.EqualTo(42f).Within(0.01f));
            Assert.That(currentLocation.color.r, Is.EqualTo(0.89f).Within(0.01f));
            Assert.That(currentLocation.color.g, Is.EqualTo(0.72f).Within(0.01f));
            Assert.That(currentLocation.color.b, Is.EqualTo(0.35f).Within(0.01f));
            Assert.That(
                currentLocation.textWrappingMode,
                Is.EqualTo(TextWrappingModes.NoWrap));
            Assert.That(
                context.transform.Find("Current Objective")
                    ?.GetComponent<TMP_Text>()?.text,
                Is.EqualTo(
                    "<color=#E3B859>◆</color>  항구의 기자를 찾기"));
            Assert.That(
                context.transform.Find("Objective Eyebrow")
                    ?.GetComponent<TMP_Text>()?.text,
                Is.EqualTo("메인 목표"));
            Assert.That(
                context.transform.Find("Location Context"),
                Is.Null,
                "The top-left HUD must not show a redundant current-place caption.");
            Assert.That(
                RequireObject("Ingame").transform.Find(
                    "Narrative Location Context"),
                Is.Null,
                "The retired top-center location banner must not be created.");
            Assert.That(
                context.GetComponentsInChildren<TMP_Text>(true)
                    .Select(text => text.text),
                Has.None.Contains("P-01"));
            GameObject guidance = RequireObject(
                "Exploration Global Navigation/Objective Guidance");
            Assert.That(guidance.activeInHierarchy, Is.True);
            Assert.That(
                guidance.transform.Find("Guidance Eyebrow")
                    ?.GetComponent<TMP_Text>()?.text,
                Is.EqualTo("목표"));
            Assert.That(
                guidance.GetComponentsInChildren<TMP_Text>(true)
                    .Select(text => text.text),
                Has.None.Contains("서브 목표"));
            Assert.That(
                guidance.transform.Find("Current Guidance")
                    ?.GetComponent<TMP_Text>()?.text,
                Is.EqualTo("다니엘 머서 찾기"));
            Assert.That(
                guidance.transform.Find("Top Gold Line")
                    ?.GetComponent<Image>()?.color.a,
                Is.GreaterThan(0.75f));
            Assert.That(
                guidance.transform.Find("Bottom Gold Line")
                    ?.GetComponent<Image>()?.color.a,
                Is.GreaterThan(0.75f));
            AssertHudDim(
                context,
                "HUD Dim Left",
                sampleFromRight: false);
            AssertHudDim(
                guidance,
                "HUD Dim Center",
                sampleFromRight: false);
            AssertHudDim(
                RequireObject(
                    "Exploration Global Navigation/Global Navigation"),
                "HUD Dim Right",
                sampleFromRight: true);
            Assert.That(
                Dialogue.ActiveProductionSceneId,
                Is.Empty,
                "첫 장면 대사는 Daniel을 클릭하기 전까지 시작하면 안 됩니다.");
            Assert.That(
                RequireObject("Ingame/Line Panel").activeSelf,
                Is.False);

            Button daniel = Object.FindObjectsByType<Button>(
                    FindObjectsInactive.Include,
                    FindObjectsSortMode.None)
                .First(button =>
                    button.name.StartsWith("AmbientCharacter_DANIEL"));
            Transform talkMarker =
                daniel.transform.Find("Dialogue Speech Bubble");
            Assert.That(talkMarker, Is.Not.Null);
            Assert.That(talkMarker.gameObject.activeInHierarchy, Is.True);
            Assert.That(
                talkMarker.GetComponent<Image>()?.sprite,
                Is.Not.Null,
                "The dialogue prompt must render its speech-bubble icon.");
            AlphaContourRaycastFilter contour =
                daniel.GetComponent<AlphaContourRaycastFilter>();
            Assert.That(
                contour,
                Is.Not.Null,
                "Character interaction must follow the visible alpha contour.");
            Assert.That(
                contour.HasAlphaMask,
                Is.True,
                "The production character texture should provide an alpha mask.");
            Button[] worldCharacters = Object.FindObjectsByType<Button>(
                    FindObjectsInactive.Include,
                    FindObjectsSortMode.None)
                .Where(button =>
                    button.name.StartsWith("AmbientCharacter_"))
                .ToArray();
            Assert.That(
                worldCharacters
                    .Where(button => button != daniel)
                    .All(button =>
                        daniel.transform.GetSiblingIndex() >=
                        button.transform.GetSiblingIndex()),
                Is.True,
                "The story focus participant must stay above overlapping NPCs.");
            yield return InvokeAndSettle(daniel);
            Assert.That(
                Dialogue.ActiveProductionSceneId,
                Is.EqualTo("P-01"));
        }

        private static void AssertHudDim(
            GameObject region,
            string expectedSpriteName,
            bool sampleFromRight)
        {
            RectTransform regionRect =
                region.GetComponent<RectTransform>();
            RectTransform dimRect = region.transform
                .Find("Dim Background")
                ?.GetComponent<RectTransform>();
            Assert.That(dimRect, Is.Not.Null);
            Assert.That(dimRect.anchorMin, Is.EqualTo(Vector2.zero));
            Assert.That(dimRect.anchorMax, Is.EqualTo(Vector2.one));
            Assert.That(dimRect.offsetMin.y, Is.LessThan(0f));
            Assert.That(dimRect.offsetMax.y, Is.GreaterThan(0f));
            Assert.That(
                dimRect.rect.width,
                Is.GreaterThan(regionRect.rect.width));
            Assert.That(
                dimRect.GetComponent<LayoutElement>().ignoreLayout,
                Is.True);
            Image dim = dimRect.GetComponent<Image>();
            Assert.That(dim, Is.Not.Null);
            Assert.That(dim.sprite, Is.Not.Null);
            Assert.That(dim.sprite.name, Is.EqualTo(expectedSpriteName));
            Assert.That(dim.color.a, Is.GreaterThan(0.5f));
            Texture2D texture = dim.sprite.texture;
            int top = texture.height - 1;
            bool sampleFromCenter =
                expectedSpriteName.EndsWith("Center");
            int strongX = sampleFromCenter
                ? texture.width / 2
                : sampleFromRight ? texture.width - 1 : 0;
            int weakX = sampleFromRight ? 0 : texture.width - 1;
            if (sampleFromCenter)
                weakX = 0;
            Assert.That(
                texture.GetPixel(strongX, top).a,
                Is.GreaterThan(texture.GetPixel(weakX, top).a));
            Assert.That(
                texture.GetPixel(strongX, top).a,
                Is.GreaterThan(texture.GetPixel(strongX, 0).a));
        }

        [UnityTest]
        public IEnumerator Map_HidesWorldCharactersAndCompactsTopHud()
        {
            yield return StartNewGameFromVisibleButton(
                startOpeningDialogue: false);
            yield return null;

            Button daniel = Object.FindObjectsByType<Button>(
                    FindObjectsInactive.Include,
                    FindObjectsSortMode.None)
                .First(button =>
                    button.name.StartsWith("AmbientCharacter_DANIEL"));
            GameObject talkMarker =
                daniel.transform.Find("Dialogue Speech Bubble").gameObject;
            Assert.That(talkMarker.activeInHierarchy, Is.True);
            Assert.That(
                LocationLoader.Instance.IsPresentationVisible,
                Is.True);

            Ui.ShowMap();
            yield return WaitForUiTransition();

            Assert.That(Ui.ActivePanel, Is.EqualTo(UiPrimaryPanel.Map));
            Assert.That(
                LocationLoader.Instance.IsPresentationVisible,
                Is.False,
                "The exploration layer must be hidden as soon as the map opens.");
            Assert.That(daniel.gameObject.activeInHierarchy, Is.False);
            Assert.That(talkMarker.activeInHierarchy, Is.False);
            Assert.That(
                RequireObject(
                    "Exploration Global Navigation/Exploration Context")
                    .activeSelf,
                Is.False);
            Assert.That(
                RequireObject(
                    "Exploration Global Navigation/Global Navigation")
                    .activeInHierarchy,
                Is.True);

            Ui.ShowIngame();
            yield return WaitForUiTransition();
            Assert.That(
                LocationLoader.Instance.IsPresentationVisible,
                Is.True);
            Assert.That(daniel.gameObject.activeInHierarchy, Is.True);
        }

        [UnityTest]
        public IEnumerator SaveSlotTransition_KeepsObjectiveHudHidden()
        {
            Button startButton = RequireObject(
                    "StartScene/Title Presentation")
                .GetComponentsInChildren<Button>(true)
                .First(button => button.name == "시작하기");
            yield return InvokeAndSettle(startButton);
            Button slot = RequireObject("StartScene/Save Slot Selection")
                .GetComponentsInChildren<Button>(true)
                .First(button =>
                    button.name.StartsWith("Save Slot") &&
                    button.GetComponentInChildren<TMP_Text>(true).text.Contains(
                        "비어 있는 기록"));
            yield return InvokeAndSettle(slot);
            Button confirm = RequireObject(
                    "StartScene/Save Slot Selection/Start Confirmation/Confirm")
                .GetComponent<Button>();
            yield return InvokeAndSettle(confirm);

            GameObject objective = RequireObject("Ingame/Objective HUD");
            Assert.That(Ui.ActivePanel, Is.EqualTo(UiPrimaryPanel.Start));
            Assert.That(
                objective.activeInHierarchy,
                Is.False,
                "저장 슬롯 전환이 끝나기 전에는 목표 HUD가 보여서는 안 됩니다.");
        }

        [UnityTest]
        public IEnumerator ExplorationNavigation_UsesNaturalLanguageLabels()
        {
            TMP_Text startSettings = RequireObject("StartScene/Settings Btn")
                .GetComponentInChildren<TMP_Text>(true);
            Assert.That(startSettings, Is.Not.Null);
            Assert.That(
                startSettings.text,
                Is.Empty,
                "The start screen Settings button should render only its icon.");

            yield return StartNewGameFromVisibleButton();

            Assert.That(
                RequireObject("Status HUD").activeSelf,
                Is.False,
                "숫자 상태 HUD는 탐색 중에도 화면에 표시하지 않습니다.");
            string[] iconNavigationButtonPaths =
            {
                "Exploration Global Navigation/Global Navigation/지도 버튼",
                "Exploration Global Navigation/Global Navigation/조사 기록 버튼",
                "Exploration Global Navigation/Global Navigation/일시정지 버튼"
            };
            foreach (string path in iconNavigationButtonPaths)
            {
                Image icon = RequireComponent<Image>(path + "/Icon");
                Assert.That(icon.sprite, Is.Not.Null);
                Assert.That(
                    icon.sprite.name,
                    Does.Contain("outline"),
                    $"{path} must use the line-art HUD icon.");
                Assert.That(
                    RequireComponent<Image>(path).color.a,
                    Is.Zero.Within(0.001f),
                    $"{path} must not render a button background.");
            }
            GameObject context = RequireObject(
                "Exploration Global Navigation/Exploration Context");
            Assert.That(
                context.GetComponent<Image>().color.a,
                Is.Zero.Within(0.001f));
            Assert.That(
                RequireObject(
                    "Exploration Global Navigation/Global Navigation")
                    .GetComponent<Image>().color.a,
                Is.Zero.Within(0.001f));
            foreach (TMP_Text text in
                     context.GetComponentsInChildren<TMP_Text>(true))
            {
                Assert.That(
                    text.outlineWidth,
                    Is.GreaterThanOrEqualTo(0.1f),
                    $"{text.name} must use a readability outline.");
                Assert.That(text.outlineColor.a, Is.GreaterThan(0));
            }
            Assert.That(RequireObject("Ingame/Map Btn").activeSelf, Is.False);
            Assert.That(
                RequireObject("Ingame/Evidence Btn").activeSelf,
                Is.False);
            Assert.That(
                RequireObject("Ingame/Settings Btn").activeSelf,
                Is.False);
        }

        [UnityTest]
        public IEnumerator ExplorationNavigation_UsesAuthoredTopRegions()
        {
            yield return StartNewGameFromVisibleButton();

            RectTransform canvas = Canvas as RectTransform;
            RectTransform navigation =
                RequireObject(
                    "Exploration Global Navigation/Global Navigation")
                .GetComponent<RectTransform>();
            RectTransform context =
                RequireObject(
                    "Exploration Global Navigation/Exploration Context")
                .GetComponent<RectTransform>();
            Bounds navigationBounds =
                RectTransformUtility.CalculateRelativeRectTransformBounds(
                    canvas,
                    navigation);
            Bounds contextBounds =
                RectTransformUtility.CalculateRelativeRectTransformBounds(
                    canvas,
                    context);
            Assert.That(navigationBounds.max.x, Is.LessThanOrEqualTo(
                canvas.rect.xMax + 1f));
            Assert.That(navigationBounds.max.y, Is.LessThanOrEqualTo(
                canvas.rect.yMax + 1f));
            Assert.That(navigationBounds.center.x, Is.GreaterThan(0f));
            Assert.That(navigationBounds.center.y, Is.GreaterThan(0f));
            Assert.That(
                contextBounds.max.x,
                Is.LessThanOrEqualTo(navigationBounds.min.x + 1f),
                "The consolidated top-left HUD must not overlap navigation.");

            TMP_Text objective = RequireObject(
                    "Exploration Global Navigation/Exploration Context/" +
                    "Current Objective")
                .GetComponent<TMP_Text>();
            Assert.That(objective.text, Is.Not.Empty);
            Assert.That(objective.text, Does.Not.Contain("/41"));
        }

        [UnityTest]
        public IEnumerator LongAmbientDialogue_StaysInsideItsTextRect()
        {
            yield return StartNewGameFromVisibleButton();

            Wake.Narrative.DialogueController dialogue =
                Object.FindFirstObjectByType<
                    Wake.Narrative.DialogueController>();
            dialogue.CancelActiveDialogue();
            yield return null;
            const string original =
                "머서 씨 짐에는 기자 장비 표식이 붙어 있습니다. " +
                "초대장 확인 전에는 선적하지 않겠습니다. " +
                "긴 문장도 대화창의 안전 영역을 벗어나면 안 됩니다.";
            Assert.That(
                dialogue.StartAmbientLine(
                    "DOCK_PORTER",
                    original),
                Is.True);
            yield return null;

            TMP_Text line =
                RequireComponent<TMP_Text>("Ingame/Line Panel/Panel/line");
            Button next =
                RequireComponent<Button>("Ingame/Line Panel/Panel/Next");
            RawImage portrait =
                RequireComponent<RawImage>(
                    "Ingame/Speaker Portrait");
            GameObject legacyMapNavigation =
                RequireObject("Ingame/Map Btn");
            GameObject globalNavigation =
                RequireObject("Exploration Global Navigation");
            Assert.That(portrait.texture, Is.Not.Null);
            Assert.That(portrait.uvRect.y, Is.Zero.Within(0.001f));
            Assert.That(portrait.uvRect.height, Is.EqualTo(1f).Within(0.001f));
            Assert.That(
                globalNavigation.GetComponent<CanvasGroup>().alpha,
                Is.Zero,
                "Navigation controls must hide while dialogue is active.");
            Assert.That(legacyMapNavigation.activeSelf, Is.False);

            string presented = line.text;
            int guard = 0;
            while (presented.Length < original.Length && guard++ < 10)
            {
                line.maxVisibleCharacters = int.MaxValue;
                next.onClick.Invoke();
                yield return null;
                next.onClick.Invoke();
                yield return null;
                presented += line.text;
            }

            Assert.That(
                presented,
                Is.EqualTo(original),
                "Pagination must preserve every character of the dialogue.");
            line.maxVisibleCharacters = int.MaxValue;
            line.ForceMeshUpdate();

            Assert.That(line.enableAutoSizing, Is.True);
            Assert.That(
                line.overflowMode,
                Is.EqualTo(TextOverflowModes.Truncate));
            Assert.That(line.isTextOverflowing, Is.False);

            dialogue.CancelActiveDialogue();
            yield return null;
            Assert.That(
                globalNavigation.activeSelf,
                Is.True,
                "Navigation controls must return after dialogue ends.");
            Assert.That(
                legacyMapNavigation.activeSelf,
                Is.False,
                "Legacy navigation must stay hidden after dialogue ends.");
        }

        [UnityTest]
        public IEnumerator TitleLogo_UsesEntireSourceImage()
        {
            yield return null;

            Image logo = RequireComponent<Image>(
                "StartScene/Title Presentation/Under the Horizon Logo");
            Assert.That(logo.sprite, Is.Not.Null);
            Assert.That(logo.preserveAspect, Is.True);
            Assert.That(logo.sprite.rect.x, Is.Zero);
            Assert.That(logo.sprite.rect.y, Is.Zero);
            Assert.That(
                logo.sprite.rect.width,
                Is.EqualTo(logo.sprite.texture.width));
            Assert.That(
                logo.sprite.rect.height,
                Is.EqualTo(logo.sprite.texture.height));
            RectTransform logoRect = logo.rectTransform;
            Assert.That(
                logoRect.anchorMax.x - logoRect.anchorMin.x,
                Is.EqualTo(0.5f).Within(0.001f));
            Assert.That(
                logoRect.anchorMin.y,
                Is.GreaterThanOrEqualTo(0.7f));
            AssertNoRuntimeErrors("타이틀 로고 전체 영역");
        }

        [UnityTest]
        public IEnumerator SettingsSliders_ChangeLiveAudioSourceVolumes()
        {
            AudioManager audio = AudioManager.Instance;
            Assert.That(audio, Is.Not.Null);
            float originalMusic = audio.MusicVolume;
            float originalSfx = audio.SfxVolume;

            Ui.OpenSettings();
            yield return WaitForUiTransition();
            Slider music = RequireComponent<Slider>(
                "Settings Popup/Settings/Sound");
            Slider sfx = RequireComponent<Slider>(
                "Settings Popup/Settings/Sound (1)");

            Assert.That(
                music.value,
                Is.EqualTo(audio.MusicVolume).Within(0.001f));
            Assert.That(
                sfx.value,
                Is.EqualTo(audio.SfxVolume).Within(0.001f));

            music.value = 0.31f;
            sfx.value = 0.47f;
            yield return null;

            Assert.That(audio.MusicVolume, Is.EqualTo(0.31f).Within(0.001f));
            Assert.That(audio.SfxVolume, Is.EqualTo(0.47f).Within(0.001f));
            float activeMusicVolume = new[]
                {
                    GameObject.Find("MusicSource")
                        ?.GetComponent<AudioSource>(),
                    GameObject.Find("Music A")
                        ?.GetComponent<AudioSource>(),
                    GameObject.Find("Music B")
                        ?.GetComponent<AudioSource>()
                }
                .Where(source => source != null)
                .Max(source => source.volume);
            Assert.That(
                activeMusicVolume,
                Is.EqualTo(0.31f).Within(0.001f));
            Assert.That(
                GameObject.Find("SfxSource").GetComponent<AudioSource>().volume,
                Is.EqualTo(0.47f).Within(0.001f));

            audio.SetMusicVolume(originalMusic);
            audio.SetSfxVolume(originalSfx);
            AssertNoRuntimeErrors("설정 음량 실시간 반영");
        }

        [UnityTest]
        public IEnumerator TitleScreen_HidesDecorativeTaglines()
        {
            yield return null;

            TMP_Text[] labels = RequireObject("StartScene/Title Presentation")
                .GetComponentsInChildren<TMP_Text>(true);
            Assert.That(
                labels,
                Has.None.Matches<TMP_Text>(label =>
                    label.text.Contains("2D 내러티브 미스터리 어드벤처")));
            Assert.That(
                labels,
                Has.None.Matches<TMP_Text>(label =>
                    label.text.Contains("PRESS ANY KEY")));
            AssertNoRuntimeErrors("타이틀 장식 문구 제거");
        }

        [UnityTest]
        public IEnumerator FinalAccusation_AutoPreparesDeductionsAndAdvances()
        {
            yield return StartNewGameFromVisibleButton();
            string[] requiredEvidence =
            {
                "C-01", "C-03", "C-04", "C-05", "C-06", "C-07",
                "C-08", "C-09", "C-10", "C-12", "C-14", "C-16"
            };
            foreach (string evidenceId in requiredEvidence)
            {
                Assert.That(
                    EvidenceInventory.Instance.TryAddById(evidenceId),
                    Is.True,
                    evidenceId);
            }

            FinalAccusationUIController accusation =
                RequireObject("Ingame")
                    .GetComponent<FinalAccusationUIController>();
            accusation.Open();
            yield return WaitForUiTransition();

            Assert.That(
                RequireObject("Ingame/Final Accusation").activeSelf,
                Is.True);
            Assert.That(
                FinalAccusationSession.RequiredDeductionIds.All(
                    State.HasUnlockedDeduction),
                Is.True);
            Assert.That(
                RequireComponent<Button>(
                    "Ingame/Final Accusation/최종 논증 제출").interactable,
                Is.True);

            yield return InvokeAndSettle(
                RequireComponent<Button>(
                    "Ingame/Final Accusation/Culprit"));
            yield return InvokeAndSettle(
                RequireComponent<Button>(
                    "Ingame/Final Accusation/최종 논증 제출"));

            Assert.That(
                RequireObject(
                    "Ingame/Final Accusation/MurderLocation").activeSelf,
                Is.True);
            Assert.That(
                RequireObject("Ingame/Final Accusation")
                    .GetComponentsInChildren<TMP_Text>(true)
                    .Any(text => text.text.Contains("1단계 정답")),
                Is.True);
            AssertNoRuntimeErrors("최종 심문 자동 논증 준비");
        }

        [UnityTest]
        public IEnumerator FinalAccusation_MissingEvidenceReturnsFromTheoryBoard()
        {
            yield return StartNewGameFromVisibleButton();
            FinalAccusationUIController accusation =
                RequireObject("Ingame")
                    .GetComponent<FinalAccusationUIController>();
            accusation.Open();
            yield return WaitForUiTransition();

            Assert.That(
                RequireComponent<Button>(
                    "Ingame/Final Accusation/최종 논증 제출").interactable,
                Is.False);
            Button boardButton = RequireComponent<Button>(
                "Ingame/Final Accusation/증거 보드 열기");
            Assert.That(boardButton.gameObject.activeSelf, Is.True);

            yield return InvokeAndSettle(boardButton);
            Assert.That(Ui.ActivePanel, Is.EqualTo(UiPrimaryPanel.Evidence));
            Assert.That(
                RequireObject("Evidence Theory Board").activeSelf,
                Is.True);

            yield return InvokeAndSettle(
                RequireComponent<Button>("Evidence Theory Board/Close"));

            Assert.That(Ui.ActivePanel, Is.EqualTo(UiPrimaryPanel.Ingame));
            Assert.That(
                RequireObject("Ingame/Final Accusation").activeSelf,
                Is.True);
            Assert.That(
                RequireObject("Ingame/Final Accusation")
                    .GetComponentsInChildren<TMP_Text>(true)
                    .Any(text => text.text.Contains(
                        "최종 심문에 필요한 핵심 논증")),
                Is.True);
            AssertNoRuntimeErrors("최종 심문 증거 보드 복귀");
        }

        [UnityTest]
        public IEnumerator PrimaryButtons_RoundTripWithoutOrphanModal()
        {
            yield return StartNewGameFromVisibleButton();
            Dialogue.CancelActiveDialogue();
            yield return null;
            AssertOnlyPanel(UiPrimaryPanel.Ingame);

            yield return InvokeAndSettle(
                RequireComponent<Button>(
                    "Exploration Global Navigation/Global Navigation/" +
                    "지도 버튼"));
            AssertOnlyPanel(UiPrimaryPanel.Map);
            yield return InvokeAndSettle(
                AssertStyledMapBackButton());
            AssertOnlyPanel(UiPrimaryPanel.Ingame);

            yield return InvokeAndSettle(
                RequireComponent<Button>(
                    "Exploration Global Navigation/Global Navigation/" +
                    "조사 기록 버튼"));
            AssertOnlyPanel(UiPrimaryPanel.Evidence);
            Assert.That(Ui.OpenRuntimeModalCount, Is.Zero);
            Assert.That(
                RequireComponent<Image>("Evidence/Image").sprite,
                Is.Null);
            TMP_Text placeholder =
                RequireComponent<TMP_Text>(
                    "Evidence/Description Viewport/Description");
            Assert.That(placeholder.gameObject.activeSelf, Is.True);
            Assert.That(placeholder.text, Does.Contain("확보한 증거가 없습니다"));
            Assert.That(
                placeholder.font,
                Is.SameAs(
                    TypographyService.Resolve(
                        TypographyRole.BodyRegular)));
            TMP_Text title =
                RequireComponent<TMP_Text>("Evidence/Text (TMP)");
            Assert.That(title.text, Is.EqualTo("조사 기록"));
            Assert.That(title.text, Does.Not.Contain("C-"));
            Assert.That(
                title.font,
                Is.SameAs(
                    TypographyService.Resolve(
                        TypographyRole.Heading)));
            Assert.That(
                RequireComponent<Button>("Evidence/Next").interactable,
                Is.False);
            Assert.That(
                RequireComponent<Button>("Evidence/Next (1)").interactable,
                Is.False);
            yield return InvokeAndSettle(
                RequireComponent<Button>("Evidence/Turn (2)"));
            Assert.That(Ui.OpenRuntimeModalCount, Is.EqualTo(1));

            yield return InvokeAndSettle(
                RequireComponent<Button>("Evidence/Back Btn"));
            AssertOnlyPanel(UiPrimaryPanel.Ingame);
            Assert.That(Ui.OpenRuntimeModalCount, Is.Zero);
            Assert.That(Ui.IsSettingsOpen, Is.False);
            AssertNoRuntimeErrors("주 화면 왕복");
        }

        [UnityTest]
        public IEnumerator Settings_ClosesOtherModalAndOwnsInput()
        {
            yield return StartNewGameFromVisibleButton();
            Ui.ShowEvidence();
            yield return WaitForUiTransition();
            Assert.That(Ui.OpenRuntimeModalCount, Is.Zero);
            yield return InvokeAndSettle(
                RequireComponent<Button>("Evidence/Turn (2)"));
            Assert.That(Ui.OpenRuntimeModalCount, Is.EqualTo(1));

            Ui.OpenSettings();
            yield return WaitForUiTransition();
            Assert.That(Ui.IsSettingsOpen, Is.True);
            Assert.That(Ui.OpenRuntimeModalCount, Is.Zero);
            Assert.That(
                RequireObject("Settings Popup").transform.GetSiblingIndex(),
                Is.EqualTo(Canvas.childCount - 1));
            Assert.That(
                RequireObject("Settings Popup/Settings/Credit").activeSelf,
                Is.False);
            Assert.That(
                RequireObject("Settings Popup/Exit Btn").activeSelf,
                Is.False);
            AssertInsideSafeArea(
                RequireComponent<RectTransform>(
                    "Settings Popup/Settings"),
                "설정 패널");
            AssertInsideSafeArea(
                RequireComponent<RectTransform>(
                    "Settings Popup/Close"),
                "설정 닫기");

            CanvasGroup evidenceInput =
                RequireObject("Evidence").GetComponent<CanvasGroup>();
            CanvasGroup hudInput =
                RequireObject("Status HUD").GetComponent<CanvasGroup>();
            Assert.That(evidenceInput.interactable, Is.False);
            Assert.That(evidenceInput.blocksRaycasts, Is.False);
            Assert.That(hudInput.interactable, Is.False);
            Assert.That(hudInput.blocksRaycasts, Is.False);

            yield return InvokeAndSettle(
                RequireComponent<Button>("Settings Popup/Close"));
            Assert.That(Ui.IsSettingsOpen, Is.False);
            Assert.That(evidenceInput.interactable, Is.True);
            Assert.That(evidenceInput.blocksRaycasts, Is.True);
            Assert.That(hudInput.interactable, Is.False);
            Assert.That(hudInput.blocksRaycasts, Is.False);
            Assert.That(
                RequireObject("Status HUD").activeSelf,
                Is.False);
            AssertNoRuntimeErrors("설정 모달 입력 복구");
        }

        [UnityTest]
        public IEnumerator CharacterRelationshipCards_ShowDetailsAndKeepBackClickable()
        {
            yield return StartNewGameFromVisibleButton(false);
            Ui.ShowEvidence();
            yield return WaitForUiTransition();

            Button peopleTab = RequireComponent<Button>(
                "Evidence/Notebook Tabs/Characters Tab");
            yield return InvokeAndSettle(peopleTab);

            RectTransform peoplePanel = RequireComponent<RectTransform>(
                "Evidence/Characters And Relationships");
            RectTransform backRect = RequireComponent<RectTransform>(
                "Evidence/Back Btn");
            Assert.That(
                ScreenRect(peoplePanel).Overlaps(ScreenRect(backRect)),
                Is.False,
                "인물 관계도와 돌아가기 버튼 영역이 겹치면 안 됩니다.");
            Assert.That(
                backRect.GetSiblingIndex(),
                Is.EqualTo(backRect.parent.childCount - 1),
                "돌아가기 버튼은 관계도보다 위에서 입력을 받아야 합니다.");

            Button adrian = RequireComponent<Button>(
                "Evidence/Characters And Relationships/Viewport/Content/ADRIAN");
            Assert.That(adrian.interactable, Is.True);
            yield return InvokeAndSettle(adrian);

            GameObject detail = RequireObject(
                "Evidence/Characters And Relationships/Character Detail");
            Assert.That(detail.activeInHierarchy, Is.True);
            Assert.That(
                RequireText(
                    "Evidence/Characters And Relationships/" +
                    "Character Detail/Name").text,
                Is.EqualTo("아드리안 베일"));
            Assert.That(
                RequireText(
                    "Evidence/Characters And Relationships/" +
                    "Character Detail/Role").text,
                Does.Contain("사립 탐정"));
            Assert.That(
                RequireText(
                    "Evidence/Characters And Relationships/" +
                    "Character Detail/Summary").text,
                Does.Contain("MV Elysium"));

            yield return InvokeAndSettle(
                RequireComponent<Button>(
                    "Evidence/Characters And Relationships/" +
                    "Character Detail/Back To Character List"));
            Assert.That(detail.activeSelf, Is.False);
            Assert.That(adrian.gameObject.activeInHierarchy, Is.True);

            yield return InvokeAndSettle(
                RequireComponent<Button>("Evidence/Back Btn"));
            AssertOnlyPanel(UiPrimaryPanel.Ingame);
            AssertNoRuntimeErrors("인물 관계도 상세 정보와 돌아가기");
        }

        private Button AssertStyledMapBackButton()
        {
            Button button =
                RequireComponent<Button>("Map/Back Btn");
            Image image = button.GetComponent<Image>();
            RectTransform rect =
                button.GetComponent<RectTransform>();
            TMP_Text label =
                button.GetComponentInChildren<TMP_Text>(true);

            Assert.That(image.sprite, Is.Null);
            Assert.That(
                button.transition,
                Is.EqualTo(Selectable.Transition.ColorTint));
            Assert.That(label.text, Is.EqualTo("← 돌아가기"));
            Assert.That(rect.anchorMin.x, Is.GreaterThan(.8f));
            Assert.That(rect.anchorMin.y, Is.GreaterThan(.8f));
            Assert.That(
                button.transform.GetSiblingIndex(),
                Is.EqualTo(button.transform.parent.childCount - 1));
            return button;
        }

        private static Rect ScreenRect(RectTransform rect)
        {
            var corners = new Vector3[4];
            rect.GetWorldCorners(corners);
            return Rect.MinMaxRect(
                corners[0].x,
                corners[0].y,
                corners[2].x,
                corners[2].y);
        }

        private void AssertOnlyPanel(UiPrimaryPanel expected)
        {
            Assert.That(Ui.ActivePanel, Is.EqualTo(expected));
            Assert.That(
                RequireObject("StartScene").activeSelf,
                Is.EqualTo(expected == UiPrimaryPanel.Start));
            Assert.That(
                RequireObject("Ingame").activeSelf,
                Is.EqualTo(expected == UiPrimaryPanel.Ingame));
            Assert.That(
                RequireObject("Map").activeSelf,
                Is.EqualTo(expected == UiPrimaryPanel.Map));
            Assert.That(
                RequireObject("Evidence").activeSelf,
                Is.EqualTo(expected == UiPrimaryPanel.Evidence));
        }
    }
}
