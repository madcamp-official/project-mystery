using System;
using System.Collections.Generic;
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
    public sealed class CameraBlindSpotUIController :
        MonoBehaviour,
        IRuntimeModalController
    {
        private static readonly Color Backdrop = new(0.025f, 0.026f, 0.047f, 0.995f);
        private static readonly Color Surface = new(0.07f, 0.065f, 0.09f, 1f);
        private static readonly Color SurfaceRaised = new(0.105f, 0.095f, 0.12f, 1f);
        private static readonly Color Gold = new(0.78f, 0.62f, 0.34f, 1f);
        private static readonly Color Violet = new(0.55f, 0.30f, 0.78f, 1f);
        private static readonly Color Muted = new(0.55f, 0.56f, 0.64f, 1f);

        private readonly List<Image> feedImages = new();
        private readonly List<RectTransform> scanLines = new();
        private readonly List<TMP_Text> feedTimes = new();
        private readonly List<Button> logTabs = new();
        private readonly List<GameObject> timelineMarkers = new();

        private GameObject root;
        private RectTransform timelineTracks;
        private TMP_Text statusText;
        private TMP_Text logTitleText;
        private TMP_Text logBodyText;
        private TMP_Text currentTimeText;
        private TMP_Text eventDetailText;
        private Button playButton;
        private Button logsButton;
        private Button detectorEventButton;
        private Button locationButton;
        private Button videoOnlyButton;
        private Button continueButton;
        private Button reopenButton;
        private Slider timeSlider;
        private Texture2D feedAtlas;
        private CameraBlindSpotSession session;
        private bool isPlaying;
        private float playbackAccumulator;

        public bool IsOpen => root != null && root.activeSelf;
        public CameraBlindSpotSession Session => session;
        public string StatusMessage => statusText?.text ?? string.Empty;

        private void Awake()
        {
            BuildUi();
        }

        private void OnEnable()
        {
            SetReopenVisibility();
        }

        private void OnDisable()
        {
            session?.SetTime(session.CurrentSecond);
            root?.SetActive(false);
            reopenButton?.gameObject.SetActive(false);
        }

        private void Update()
        {
            if (!IsOpen)
            {
                SetReopenVisibility();
                return;
            }

            AnimateFeeds();
            if (!isPlaying || session == null)
            {
                return;
            }

            playbackAccumulator += Time.unscaledDeltaTime * 90f;
            if (playbackAccumulator < 1f)
            {
                return;
            }

            int advance = Mathf.FloorToInt(playbackAccumulator);
            playbackAccumulator -= advance;
            int next = session.CurrentSecond + advance;
            if (next >= timeSlider.maxValue)
            {
                next = 0;
            }
            session.SetTime(next, false);
            timeSlider.SetValueWithoutNotify(next);
            RefreshTime();
        }

        public bool Open()
        {
            GameStateManager state = GameStateManager.Instance;
            EvidenceInventory inventory = EvidenceInventory.Instance;
            if (root == null ||
                inventory == null ||
                !ProductionSceneCompletionGate.CanStartInteraction(
                    state,
                    CameraBlindSpotSession.SceneId,
                    CameraBlindSpotSession.SessionId))
            {
                return false;
            }

            session = new CameraBlindSpotSession(
                state,
                inventory.Contains,
                inventory.TryAddById);
            isPlaying = false;
            timeSlider.SetValueWithoutNotify(session.CurrentSecond);
            RuntimeModalTransition.Open(root);
            reopenButton.gameObject.SetActive(false);
            root.transform.SetAsLastSibling();
            statusText.text = session.HasReviewedCctv
                ? "저장된 분석 상태를 복원했습니다."
                : "먼저 CCTV를 재생해 21:50~22:30 사이 출입자를 확인하세요.";
            if (session.HasOverlaidLogs)
            {
                SelectLog((int)FacilityLogKind.Door);
            }
            Refresh();
            return true;
        }

        public void Close()
        {
            isPlaying = false;
            session?.SetTime(session.CurrentSecond);
            RuntimeModalTransition.Close(root, SetReopenVisibility);
        }

        public void TogglePlayback()
        {
            if (session == null || session.IsCompleted)
            {
                return;
            }

            isPlaying = !isPlaying;
            session.ReviewCctv();
            statusText.text = isPlaying
                ? "4개 채널 재생 중 · 복도와 출입문에 인물 없음"
                : "영상 검토 완료 · 해당 구간 출입자 없음";
            Refresh();
        }

        public bool OpenFacilityLogs()
        {
            if (session == null)
            {
                return false;
            }

            if (!session.OpenFacilityLogs())
            {
                statusText.text =
                    "영상 구간을 먼저 재생해야 설비 기록과 같은 시간축에 겹칠 수 있습니다.";
                return false;
            }

            SelectLog(0);
            statusText.text =
                "설비 로그가 CCTV 시간축에 중첩되었습니다. 출입문 기록부터 확인하세요.";
            Refresh();
            return true;
        }

        public void SelectLog(int index)
        {
            if (session == null || !session.HasOverlaidLogs ||
                index < 0 || index > 3)
            {
                return;
            }

            FacilityLogKind kind = (FacilityLogKind)index;
            session.SelectLog(kind);
            switch (kind)
            {
                case FacilityLogKind.Door:
                    logTitleText.text = "출입문 로그";
                    logBodyText.text =
                        "21:50–22:30  호라이즌 룸 출입문\n" +
                        "상태 확인: 닫힘\n개방·인증·통과 이벤트 없음";
                    eventDetailText.text =
                        "출입 기록 없음이 확인되었습니다. CCTV에서도 같은 구간에 인물이 없습니다.";
                    break;
                case FacilityLogKind.Detector:
                    logTitleText.text = "감지기 로그";
                    logBodyText.text =
                        "22:03  정상 신호\n22:12  정상 신호\n" +
                        "22:18:07  화재감지기 오류 · 4초\n22:24  정상 신호";
                    eventDetailText.text =
                        "시간축의 보라색 오류 이벤트를 선택하세요.";
                    break;
                case FacilityLogKind.Lighting:
                    logTitleText.text = "조명 로그";
                    logBodyText.text =
                        "21:50  야간 모드\n22:10  밝기 보정\n22:20  밝기 보정\n22:30  정상";
                    eventDetailText.text = "전원 저하나 비정상 점멸 기록 없음";
                    break;
                default:
                    logTitleText.text = "환기 로그";
                    logBodyText.text =
                        "21:50  순환 42%\n22:10  순환 41%\n22:20  순환 42%\n22:30  순환 42%";
                    eventDetailText.text = "압력·진동·유량 모두 정상 범위";
                    break;
            }

            detectorEventButton.gameObject.SetActive(
                kind == FacilityLogKind.Detector);
            for (int tab = 0; tab < logTabs.Count; tab++)
            {
                logTabs[tab].image.color =
                    tab == index ? new Color(0.30f, 0.23f, 0.16f) : SurfaceRaised;
            }
            Refresh();
        }

        public bool SelectDetectorError()
        {
            if (session == null || !session.SelectDetectorError())
            {
                statusText.text =
                    "출입문 로그의 ‘개방 이벤트 없음’을 먼저 CCTV와 대조하세요.";
                return false;
            }

            timeSlider.SetValueWithoutNotify(CameraBlindSpotSession.DetectorErrorSecond);
            eventDetailText.text =
                "22:18:07  화재감지기 오류\n지속 시간 00:04 · 신호 상태 ‘차폐’";
            statusText.text =
                "영상에는 변화가 없지만 같은 시각 감지기 신호가 4초 끊겼습니다.";
            RefreshTime();
            Refresh();
            return true;
        }

        public CameraBlindSpotCompletion ConfirmErrorLocation()
        {
            if (session == null || !session.ConfirmErrorLocation())
            {
                statusText.text = "먼저 22:18의 감지기 오류를 선택하세요.";
                return new CameraBlindSpotCompletion(
                    false,
                    new[] { "22:18 감지기 오류 선택" });
            }

            CameraBlindSpotCompletion result = session.TryComplete();
            if (!result.Completed)
            {
                statusText.text = result.MissingSteps.Count > 0
                    ? "대조가 더 필요합니다: " + string.Join(", ", result.MissingSteps)
                    : "분석 결과를 저장하지 못했습니다.";
                return result;
            }

            isPlaying = false;
            eventDetailText.text =
                "오류 위치\n호라이즌 룸 천장 감지기 · HR-SD-07\n" +
                "천장 중앙, 행사 레일 인접 구역";
            statusText.text =
                "사람이 문으로 들어온 흔적은 없습니다. 하지만 22:18 천장 부근에서 설비 이상이 발생했습니다.";
            continueButton.gameObject.SetActive(true);
            locationButton.gameObject.SetActive(false);
            videoOnlyButton.gameObject.SetActive(false);
            Refresh();
            return result;
        }

        public void AttemptVideoOnlyConclusion()
        {
            if (session == null)
            {
                return;
            }

            session.ReviewCctv();
            isPlaying = false;
            statusText.text =
                "영상 결론: 침입자 없음.\n하지만 영상 밖 설비 기록을 대조하지 않아 핵심 단서는 해금되지 않았습니다.";
            Refresh();
        }

        public void ContinueToCeiling()
        {
            if (session?.IsCompleted != true)
            {
                return;
            }

            MapController map = FindFirstObjectByType<MapController>();
            if (map != null &&
                map.TryTravelToScene("D2-05").IsAllowed)
            {
                Close();
            }
        }

        private void OnTimelineChanged(float value)
        {
            if (session == null)
            {
                return;
            }
            session.SetTime(Mathf.RoundToInt(value));
            RefreshTime();
        }

        private void Refresh()
        {
            if (session == null)
            {
                return;
            }

            Label(playButton).text = isPlaying ? "Ⅱ  일시정지" : "▶  CCTV 재생";
            logsButton.interactable = session.HasReviewedCctv;
            logsButton.image.color =
                session.HasReviewedCctv ? new Color(0.26f, 0.21f, 0.15f) : Surface;
            foreach (Button tab in logTabs)
            {
                tab.gameObject.SetActive(session.HasOverlaidLogs);
            }
            foreach (GameObject marker in timelineMarkers)
            {
                marker.SetActive(session.HasOverlaidLogs);
            }
            detectorEventButton.gameObject.SetActive(
                session.HasOverlaidLogs &&
                session.ActiveLog == FacilityLogKind.Detector);
            locationButton.gameObject.SetActive(
                session.HasSelectedDetectorError && !session.IsCompleted);
            locationButton.interactable = session.HasSelectedDetectorError;
            videoOnlyButton.gameObject.SetActive(!session.IsCompleted);
            continueButton.gameObject.SetActive(session.IsCompleted);
            RefreshTime();
        }

        private void RefreshTime()
        {
            int absoluteSecond =
                CameraBlindSpotSession.StartMinute * 60 +
                (session?.CurrentSecond ?? 0);
            int hour = absoluteSecond / 3600;
            int minute = absoluteSecond / 60 % 60;
            int second = absoluteSecond % 60;
            string value = $"{hour:00}:{minute:00}:{second:00}";
            currentTimeText.text = value;
            foreach (TMP_Text time in feedTimes)
            {
                time.text = value;
            }
        }

        private void AnimateFeeds()
        {
            float now = Time.unscaledTime;
            for (int index = 0; index < feedImages.Count; index++)
            {
                float noise = Mathf.Sin(now * (7.3f + index) + index * 1.7f);
                Image feed = feedImages[index];
                feed.color = new Color(
                    0.82f + noise * 0.015f,
                    0.88f + noise * 0.012f,
                    1f,
                    1f);
                feed.rectTransform.anchoredPosition =
                    new Vector2(noise * 0.45f, 0f);

                RectTransform line = scanLines[index];
                float y = 1f - Mathf.Repeat(now * (0.18f + index * 0.012f), 1f);
                line.anchorMin = new Vector2(0f, y);
                line.anchorMax = new Vector2(1f, y);
            }
        }

        private void BuildUi()
        {
            Transform canvas = GameObject.Find("Canvas")?.transform;
            if (canvas == null)
            {
                return;
            }

            feedAtlas = Resources.Load<Texture2D>(
                "Puzzles/CameraBlindSpot/cctv_feeds");
            root = Panel("Camera Blind Spot Puzzle", canvas, Backdrop);
            ScreenShellRuntimePresenter.Place(
                root.GetComponent<RectTransform>(),
                ScreenShellSlotIds.PuzzlePanel,
                new Vector2(.04f, .08f),
                new Vector2(.96f, .92f));
            root.AddComponent<ScreenShellRuntimePresenter>()
                .Configure(ScreenShellType.Puzzle);

            TMP_Text title = Text(root.transform,
                "카메라의 맹점 / 영상 및 설비 로그 대조", 30f,
                TextAlignmentOptions.Left);
            SetRect(title.rectTransform, .025f, .925f, .62f, .982f);
            title.color = Gold;
            TMP_Text objective = Text(root.transform,
                "좌측 CCTV 영상과 우측 설비 로그를 같은 시간축에서 비교하세요.", 17f,
                TextAlignmentOptions.Right);
            SetRect(objective.rectTransform, .55f, .938f, .93f, .978f);
            objective.color = Muted;
            Button close = Button(root.transform, "×", 28f);
            close.name = "Close";
            SetRect(close.GetComponent<RectTransform>(), .95f, .93f, .985f, .978f);
            close.onClick.AddListener(Close);

            BuildFeeds();
            BuildLogPanel();
            BuildTimeline();

            statusText = Text(root.transform, string.Empty, 17f,
                TextAlignmentOptions.Left);
            SetRect(statusText.rectTransform, .025f, .018f, .70f, .071f);
            statusText.color = new Color(0.92f, 0.83f, 0.68f);
            videoOnlyButton = Button(root.transform, "영상 분석 종료", 16f);
            videoOnlyButton.name = "Video Only Conclusion";
            SetRect(videoOnlyButton.GetComponent<RectTransform>(), .715f, .018f, .835f, .068f);
            videoOnlyButton.onClick.AddListener(AttemptVideoOnlyConclusion);
            continueButton = Button(root.transform, "천장 조사로 이동", 16f);
            continueButton.name = "Continue To Ceiling";
            SetRect(continueButton.GetComponent<RectTransform>(), .715f, .018f, .92f, .068f);
            continueButton.image.color = new Color(0.25f, 0.38f, 0.28f);
            continueButton.onClick.AddListener(ContinueToCeiling);
            Button saveClose = Button(root.transform, "닫기", 16f);
            SetRect(saveClose.GetComponent<RectTransform>(), .925f, .018f, .975f, .068f);
            saveClose.onClick.AddListener(Close);

            reopenButton = Button(canvas, "CCTV 분석 재개", 16f);
            reopenButton.name = "Camera Blind Spot Resume";
            SetRect(
                reopenButton.GetComponent<RectTransform>(),
                .38f,
                .035f,
                .62f,
                .095f);
            reopenButton.onClick.AddListener(() => Open());

            FeatureTypography.ApplyPuzzle(
                root.transform,
                title,
                objective,
                statusText);
            root.SetActive(false);
            reopenButton.gameObject.SetActive(false);
        }

        private void SetReopenVisibility()
        {
            if (reopenButton == null)
            {
                return;
            }

            GameStateManager state = GameStateManager.Instance;
            ProductionDialogueCheckpoint checkpoint = state?.DialogueCheckpoint;
            UIManager ui = UIManager.Instance;
            bool pending = checkpoint != null &&
                           checkpoint.pendingInteractionId ==
                           CameraBlindSpotSession.SessionId &&
                           !state.HasCompletedScene(CameraBlindSpotSession.SceneId);
            bool visible = pending &&
                           ui?.ActivePanel == UiPrimaryPanel.Ingame &&
                           !ui.IsSettingsOpen &&
                           ui.OpenRuntimeModalCount == 0 &&
                           DialogueController.Instance?.IsBusy != true;
            reopenButton.gameObject.SetActive(visible);
            if (visible)
            {
                Label(reopenButton).text = "CCTV·설비 로그 분석 재개";
            }
        }

        private void BuildFeeds()
        {
            RectTransform feedArea = Panel(
                    "CCTV 4 Channels",
                    root.transform,
                    new Color(0.025f, 0.038f, 0.062f, 1f))
                .GetComponent<RectTransform>();
            SetRect(feedArea, .02f, .265f, .48f, .91f);

            string[] captions =
            {
                "호라이즌 룸 외부 복도",
                "선실 복도 교차 지점",
                "서비스 복도",
                "호라이즌 룸 출입구"
            };
            for (int index = 0; index < 4; index++)
            {
                int column = index % 2;
                int row = index / 2;
                float minX = .012f + column * .494f;
                float maxX = minX + .482f;
                float maxY = .988f - row * .494f;
                float minY = maxY - .482f;
                GameObject frame = Panel(
                    $"CAM {index + 1:00}",
                    feedArea,
                    new Color(0.10f, 0.12f, 0.16f, 1f));
                SetRect(frame.GetComponent<RectTransform>(), minX, minY, maxX, maxY);

                GameObject picture = Panel("Footage", frame.transform, Color.white);
                RectTransform pictureRect = picture.GetComponent<RectTransform>();
                SetRect(pictureRect, .012f, .012f, .988f, .988f);
                Image image = picture.GetComponent<Image>();
                image.sprite = FeedSprite(index);
                image.preserveAspect = false;
                image.raycastTarget = false;
                feedImages.Add(image);

                TMP_Text cam = Text(frame.transform, $"CAM {index + 1:00}", 15f,
                    TextAlignmentOptions.Left);
                SetRect(cam.rectTransform, .04f, .84f, .35f, .96f);
                TMP_Text time = Text(frame.transform, "21:50:00", 15f,
                    TextAlignmentOptions.Right);
                SetRect(time.rectTransform, .56f, .84f, .96f, .96f);
                feedTimes.Add(time);
                TMP_Text caption = Text(frame.transform, captions[index], 13f,
                    TextAlignmentOptions.Left);
                SetRect(caption.rectTransform, .04f, .025f, .96f, .13f);

                GameObject scan = Panel(
                    "Scan Line",
                    frame.transform,
                    new Color(0.55f, 0.72f, 1f, .13f));
                RectTransform scanRect = scan.GetComponent<RectTransform>();
                scanRect.sizeDelta = new Vector2(0f, 2f);
                scan.GetComponent<Image>().raycastTarget = false;
                scanLines.Add(scanRect);
            }

            playButton = Button(root.transform, "▶  CCTV 재생", 17f);
            playButton.name = "CCTV Playback";
            SetRect(playButton.GetComponent<RectTransform>(), .025f, .205f, .16f, .25f);
            playButton.onClick.AddListener(TogglePlayback);
            logsButton = Button(root.transform, "설비 로그 겹치기", 17f);
            logsButton.name = "Overlay Facility Logs";
            SetRect(logsButton.GetComponent<RectTransform>(), .17f, .205f, .32f, .25f);
            logsButton.onClick.AddListener(() => OpenFacilityLogs());
        }

        private void BuildLogPanel()
        {
            RectTransform panel = Panel(
                    "Facility System Logs",
                    root.transform,
                    Surface)
                .GetComponent<RectTransform>();
            SetRect(panel, .495f, .265f, .98f, .91f);
            TMP_Text heading = Text(panel, "설비 시스템 로그", 24f,
                TextAlignmentOptions.Left);
            SetRect(heading.rectTransform, .035f, .90f, .96f, .98f);

            string[] tabs = { "출입문", "감지기", "조명", "환기" };
            for (int index = 0; index < tabs.Length; index++)
            {
                int captured = index;
                Button tab = Button(panel, tabs[index], 16f);
                tab.name = $"{tabs[index]} Log Tab";
                SetRect(
                    tab.GetComponent<RectTransform>(),
                    .03f + index * .24f,
                    .80f,
                    .26f + index * .24f,
                    .88f);
                tab.onClick.AddListener(() => SelectLog(captured));
                logTabs.Add(tab);
            }

            logTitleText = Text(panel, "CCTV를 먼저 검토하세요", 18f,
                TextAlignmentOptions.Left);
            SetRect(logTitleText.rectTransform, .05f, .70f, .95f, .78f);
            logTitleText.color = Gold;
            logBodyText = Text(panel,
                "설비 로그는 CCTV 재생 후 활성화됩니다.", 17f,
                TextAlignmentOptions.TopLeft);
            SetRect(logBodyText.rectTransform, .05f, .40f, .95f, .70f);
            eventDetailText = Text(panel, string.Empty, 17f,
                TextAlignmentOptions.TopLeft);
            SetRect(eventDetailText.rectTransform, .05f, .13f, .95f, .36f);
            eventDetailText.color = new Color(.78f, .67f, .94f);

            detectorEventButton = Button(panel,
                "22:18:07  화재감지기 오류 (4초)", 17f);
            detectorEventButton.name = "Detector Error 22:18";
            SetRect(
                detectorEventButton.GetComponent<RectTransform>(),
                .05f,
                .42f,
                .95f,
                .51f);
            detectorEventButton.image.color = new Color(.25f, .14f, .32f);
            detectorEventButton.onClick.AddListener(() => SelectDetectorError());
            locationButton = Button(panel, "오류 위치 확인", 17f);
            locationButton.name = "Inspect Error Location";
            SetRect(locationButton.GetComponent<RectTransform>(), .58f, .035f, .95f, .11f);
            locationButton.image.color = new Color(.28f, .18f, .36f);
            locationButton.onClick.AddListener(() => ConfirmErrorLocation());
        }

        private void BuildTimeline()
        {
            RectTransform timeline = Panel(
                    "21:50-22:30 Timeline",
                    root.transform,
                    new Color(.055f, .05f, .075f, 1f))
                .GetComponent<RectTransform>();
            SetRect(timeline, .02f, .085f, .98f, .19f);

            currentTimeText = Text(timeline, "21:50:00", 21f,
                TextAlignmentOptions.Left);
            SetRect(currentTimeText.rectTransform, .02f, .56f, .12f, .94f);
            currentTimeText.color = Gold;
            TMP_Text start = Text(timeline, "21:50", 14f, TextAlignmentOptions.Left);
            SetRect(start.rectTransform, .13f, .68f, .22f, .95f);
            TMP_Text middle = Text(timeline, "22:10", 14f, TextAlignmentOptions.Center);
            SetRect(middle.rectTransform, .47f, .68f, .56f, .95f);
            TMP_Text end = Text(timeline, "22:30", 14f, TextAlignmentOptions.Right);
            SetRect(end.rectTransform, .88f, .68f, .97f, .95f);

            GameObject sliderObject = new(
                "Time Slider",
                typeof(RectTransform),
                typeof(Slider));
            sliderObject.transform.SetParent(timeline, false);
            RectTransform sliderRect = sliderObject.GetComponent<RectTransform>();
            SetRect(sliderRect, .14f, .49f, .96f, .70f);
            GameObject background = Panel("Background", sliderObject.transform, SurfaceRaised);
            SetRect(background.GetComponent<RectTransform>(), 0f, .40f, 1f, .60f);
            GameObject fillArea = new("Fill Area", typeof(RectTransform));
            fillArea.transform.SetParent(sliderObject.transform, false);
            SetRect(fillArea.GetComponent<RectTransform>(), 0f, .35f, 1f, .65f);
            GameObject fill = Panel("Fill", fillArea.transform, Gold);
            SetRect(fill.GetComponent<RectTransform>(), 0f, 0f, 1f, 1f);
            GameObject handleArea = new("Handle Slide Area", typeof(RectTransform));
            handleArea.transform.SetParent(sliderObject.transform, false);
            SetRect(handleArea.GetComponent<RectTransform>(), 0f, 0f, 1f, 1f);
            GameObject handle = Panel("Handle", handleArea.transform, Gold);
            RectTransform handleRect = handle.GetComponent<RectTransform>();
            handleRect.sizeDelta = new Vector2(13f, 28f);

            timeSlider = sliderObject.GetComponent<Slider>();
            timeSlider.minValue = 0f;
            timeSlider.maxValue =
                (CameraBlindSpotSession.EndMinute -
                 CameraBlindSpotSession.StartMinute) * 60;
            timeSlider.wholeNumbers = true;
            timeSlider.fillRect = fill.GetComponent<RectTransform>();
            timeSlider.handleRect = handleRect;
            timeSlider.targetGraphic = handle.GetComponent<Image>();
            timeSlider.direction = Slider.Direction.LeftToRight;
            timeSlider.onValueChanged.AddListener(OnTimelineChanged);

            timelineTracks = Panel(
                    "Facility Event Tracks",
                    timeline,
                    Color.clear)
                .GetComponent<RectTransform>();
            SetRect(timelineTracks, .14f, .06f, .96f, .43f);
            timelineTracks.GetComponent<Image>().raycastTarget = false;
            AddTimelineMarker(.16f, .76f, Gold, "Door check");
            AddTimelineMarker(.42f, .76f, Gold, "Door check");
            AddTimelineMarker(.70f, .76f, Gold, "Door check");
            AddTimelineMarker(.7029f, .51f, Violet, "22:18 detector error");
            AddTimelineMarker(.31f, .26f, new Color(.35f, .55f, .35f), "Lighting");
            AddTimelineMarker(.79f, .26f, new Color(.35f, .55f, .35f), "Lighting");
            AddTimelineMarker(.10f, .04f, new Color(.35f, .48f, .72f), "Ventilation");
            AddTimelineMarker(.90f, .04f, new Color(.35f, .48f, .72f), "Ventilation");
        }

        private void AddTimelineMarker(
            float x,
            float y,
            Color color,
            string name)
        {
            GameObject marker = Panel(name, timelineTracks, color);
            RectTransform rect = marker.GetComponent<RectTransform>();
            rect.anchorMin = new Vector2(x, y);
            rect.anchorMax = new Vector2(x, y);
            rect.sizeDelta = new Vector2(9f, 9f);
            marker.GetComponent<Image>().raycastTarget = false;
            timelineMarkers.Add(marker);
        }

        private Sprite FeedSprite(int index)
        {
            if (feedAtlas == null)
            {
                return null;
            }
            float width = feedAtlas.width * .5f;
            float height = feedAtlas.height * .5f;
            int column = index % 2;
            int rowFromTop = index / 2;
            return Sprite.Create(
                feedAtlas,
                new Rect(
                    column * width,
                    rowFromTop == 0 ? height : 0f,
                    width,
                    height),
                new Vector2(.5f, .5f),
                100f);
        }

        private static GameObject Panel(
            string name,
            Transform parent,
            Color color)
        {
            var target = new GameObject(
                name,
                typeof(RectTransform),
                typeof(CanvasRenderer),
                typeof(Image));
            target.transform.SetParent(parent, false);
            target.GetComponent<Image>().color = color;
            return target;
        }

        private static TMP_Text Text(
            Transform parent,
            string value,
            float size,
            TextAlignmentOptions alignment)
        {
            var target = new GameObject(
                "Label",
                typeof(RectTransform),
                typeof(CanvasRenderer),
                typeof(TextMeshProUGUI));
            target.transform.SetParent(parent, false);
            TMP_Text text = target.GetComponent<TMP_Text>();
            TypographyService.Apply(text, TypographyRole.Body);
            text.text = value;
            text.fontSize = size;
            text.color = Color.white;
            text.alignment = alignment;
            text.textWrappingMode = TextWrappingModes.Normal;
            return text;
        }

        private static Button Button(
            Transform parent,
            string label,
            float size)
        {
            GameObject target = Panel(label, parent, SurfaceRaised);
            Button button = target.AddComponent<Button>();
            TMP_Text text = Text(target.transform, label, size,
                TextAlignmentOptions.Center);
            SetRect(text.rectTransform, .03f, .03f, .97f, .97f);
            text.raycastTarget = false;
            ScreenShellRuntimePresenter.PrepareButton(button);
            return button;
        }

        private static TMP_Text Label(Button button) =>
            button.GetComponentInChildren<TMP_Text>();

        private static void SetRect(
            RectTransform rect,
            float minX,
            float minY,
            float maxX,
            float maxY)
        {
            rect.anchorMin = new Vector2(minX, minY);
            rect.anchorMax = new Vector2(maxX, maxY);
            rect.offsetMin = Vector2.zero;
            rect.offsetMax = Vector2.zero;
        }
    }
}
