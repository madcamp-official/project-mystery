using System.Collections;
using System.Collections.Generic;
using System.Linq;
using NUnit.Framework;
using TMPro;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;
using UnityEngine.UI;
using Wake.Core;
using Wake.Exploration;
using Wake.Narrative;
using Wake.UI;

namespace Wake.Tests.PlayMode
{
    public abstract class UiBasicScenePlayModeFixture
    {
        protected const string UiScenePath =
            "Assets/_Project/Scenes/UI/UI Basic Scene.unity";
        protected const string SaveKey = "UNDER_THE_HORIZON_GAME_STATE_V2";

        private readonly List<string> runtimeErrors = new();
        private readonly Dictionary<string, string> savedPlayerPrefs = new();
        private int savedActiveSlot;
        private Scene loadedScene;

        protected Transform Canvas { get; private set; }
        protected DialogueDatabase Database { get; private set; }
        protected DialogueController Dialogue { get; private set; }
        protected GameFlow Flow { get; private set; }
        protected GameStateManager State { get; private set; }
        protected UIManager Ui { get; private set; }

        [UnitySetUp]
        public IEnumerator LoadUiBasicScene()
        {
            runtimeErrors.Clear();
            Application.logMessageReceived += CaptureRuntimeError;
            ExplorationHotspotFeedback.SetAccessibilityIndicators(false);
            PreserveSavedGame();
            ClearSavedGame();

            yield return LoadScene();

            AssertRuntimeReady();
            AssertNoRuntimeErrors("씬 초기화");
        }

        [UnityTearDown]
        public IEnumerator UnloadUiBasicScene()
        {
            if (loadedScene.IsValid() && loadedScene.isLoaded)
            {
                Scene scratch = SceneManager.CreateScene(
                    $"PlayModeCleanup_{Time.frameCount}");
                Assert.That(
                    SceneManager.SetActiveScene(scratch),
                    Is.True,
                    "테스트 정리용 씬을 활성화하지 못했습니다.");

                AsyncOperation unload =
                    SceneManager.UnloadSceneAsync(loadedScene);
                if (unload != null)
                {
                    while (!unload.isDone)
                    {
                        yield return null;
                    }
                }
            }

            yield return null;
            ClearSavedGame();
            RestoreSavedGame();
            ExplorationHotspotFeedback.SetAccessibilityIndicators(false);
            AssertNoRuntimeErrors("씬 정리");
            Application.logMessageReceived -= CaptureRuntimeError;
        }

        protected IEnumerator ReloadScenePreservingSave()
        {
            yield return LoadScene();
            AssertRuntimeReady();
            AssertNoRuntimeErrors("저장 데이터 유지 후 씬 재로드");
        }

        protected IEnumerator StartNewGameFromVisibleButton(
            bool startOpeningDialogue = true)
        {
            Button startButton = RequireObject(
                    "StartScene/Title Presentation")
                .GetComponentsInChildren<Button>(true)
                .First(button => button.name == "시작하기");
            Assert.That(
                startButton.gameObject.activeInHierarchy,
                Is.True,
                "새 게임 버튼은 시작 화면에서 보여야 합니다.");

            yield return InvokeAndSettle(startButton);
            Button slot = RequireSaveSlotButton(1);
            yield return InvokeAndSettle(slot);
            Button confirm = RequireObject(
                    "StartScene/Save Slot Selection/Start Confirmation/Confirm")
                .GetComponent<Button>();
            yield return InvokeAndSettle(confirm);

            float deadline = Time.realtimeSinceStartup + 8f;
            while (Ui.ActivePanel != UiPrimaryPanel.Ingame &&
                   Time.realtimeSinceStartup < deadline)
            {
                yield return null;
            }
            Assert.That(
                Ui.ActivePanel,
                Is.EqualTo(UiPrimaryPanel.Ingame),
                "저장 슬롯 전환 제한 시간 안에 탐색 화면이 열려야 합니다.");

            if (!startOpeningDialogue)
            {
                Dialogue.CancelActiveDialogue();
                yield return null;
                yield break;
            }

            deadline = Time.realtimeSinceStartup + 2f;
            while (Dialogue.ActiveProductionSceneId !=
                       ProductionSceneDirector.OpeningSceneId &&
                   Time.realtimeSinceStartup < deadline)
            {
                yield return null;
            }
            Assert.That(
                Dialogue.ActiveProductionSceneId,
                Is.EqualTo(ProductionSceneDirector.OpeningSceneId),
                "새 게임을 시작하면 항구 도입 나레이션이 즉시 재생되어야 합니다.");
        }

        protected IEnumerator ContinueFromVisibleButton()
        {
            Button startButton = RequireObject(
                    "StartScene/Title Presentation")
                .GetComponentsInChildren<Button>(true)
                .First(button => button.name == "시작하기");
            yield return InvokeAndSettle(startButton);
            Button slot = RequireSaveSlotButton(1);
            yield return InvokeAndSettle(slot);
            Button confirm = RequireObject(
                    "StartScene/Save Slot Selection/Start Confirmation/Confirm")
                .GetComponent<Button>();
            yield return InvokeAndSettle(confirm);

            float deadline = Time.realtimeSinceStartup + 8f;
            while (Ui.ActivePanel != UiPrimaryPanel.Ingame &&
                   Time.realtimeSinceStartup < deadline)
            {
                yield return null;
            }
            Assert.That(
                Ui.ActivePanel,
                Is.EqualTo(UiPrimaryPanel.Ingame),
                "저장된 수사를 선택하면 제한 시간 안에 탐색 화면이 열려야 합니다.");
        }

        protected IEnumerator InvokeAndSettle(Button button)
        {
            Assert.That(button, Is.Not.Null);
            bool isAmbientCharacter =
                button.name.StartsWith("AmbientCharacter_");
            Assert.That(
                button.gameObject.activeInHierarchy,
                Is.True,
                $"{button.name} 버튼이 비활성 상태입니다.");
            Assert.That(
                button.interactable,
                Is.True,
                $"{button.name} 버튼을 누를 수 없습니다.");

            button.onClick.Invoke();
            yield return null;
            yield return null;
            yield return WaitForUiTransition();
            if (isAmbientCharacter)
                yield return new WaitForSecondsRealtime(0.75f);
        }

        protected IEnumerator WaitForUiTransition(float timeout = 2f)
        {
            // Coordinator state is set by its coroutine on the next frame.
            // Give direct Open/Show calls one frame to begin before polling it.
            yield return null;
            float transitionDeadline =
                Time.realtimeSinceStartup + timeout;
            while (UIManager.Instance != null &&
                   UIManager.Instance.IsTransitioning &&
                   Time.realtimeSinceStartup < transitionDeadline)
            {
                yield return null;
            }
            Assert.That(
                UIManager.Instance == null ||
                UIManager.Instance.IsTransitioning,
                Is.False,
                "UI 전환이 제한 시간 안에 완료되지 않았습니다.");
            yield return null;
            UnityEngine.Canvas.ForceUpdateCanvases();
        }

        protected IEnumerator StartPreparedProductionSceneFromFocusCharacter(
            string sceneId)
        {
            Assert.That(
                ScenePresenceCatalog.TryGet(sceneId, out ScenePresenceRecord scene),
                Is.True,
                $"{sceneId} 장면의 인물 배치 정보를 찾지 못했습니다.");
            SceneWorldCharacter focus =
                ScenePresencePresentationPolicy
                    .SelectVisible(scene, scene.FocusLocation, 5)
                    .FirstOrDefault(character =>
                        character.IsFocusParticipant);
            Assert.That(
                focus.CharacterId,
                Is.Not.Empty,
                $"{sceneId} 장면의 대화 시작 인물을 찾지 못했습니다.");

            Button target = null;
            float deadline = Time.realtimeSinceStartup + 2f;
            while (target == null && Time.realtimeSinceStartup < deadline)
            {
                target = Object.FindObjectsByType<Button>(
                        FindObjectsInactive.Exclude,
                        FindObjectsSortMode.None)
                    .FirstOrDefault(button =>
                        button.name.StartsWith(
                            $"AmbientCharacter_{focus.CharacterId}"));
                if (target == null)
                    yield return null;
            }

            Assert.That(
                target,
                Is.Not.Null,
                $"{sceneId} 장면의 {focus.CharacterId} 대화 대상을 찾지 못했습니다.");
            yield return InvokeAndSettle(target);

            deadline = Time.realtimeSinceStartup + 2f;
            while (Dialogue.ActiveProductionSceneId != sceneId &&
                   Time.realtimeSinceStartup < deadline)
            {
                yield return null;
            }

            Assert.That(
                Dialogue.ActiveProductionSceneId,
                Is.EqualTo(sceneId),
                $"{sceneId} 장면은 주요 인물을 클릭한 뒤 시작되어야 합니다.");
        }

        protected IEnumerator AdvanceToVisibleChoices(int maximumSteps = 200)
        {
            Button next =
                RequireComponent<Button>("Ingame/Line Panel/Panel/Next");
            GameObject choices =
                RequireObject("Ingame/Line Panel/Select Btn");
            GameObject investigation =
                RequireObject("Investigation Dialogue UI");
            Button investigationAction =
                RequireComponent<Button>(
                    "Investigation Dialogue UI/Investigation Frame/Action");
            int steps = 0;
            while (!choices.activeInHierarchy && Dialogue.IsBusy)
            {
                Assert.That(
                    steps++,
                    Is.LessThan(maximumSteps),
                    "선택지가 나타나기 전에 대사 진행 상한을 초과했습니다.");
                yield return investigation.activeInHierarchy
                    ? InvokeAndSettle(investigationAction)
                    : InvokeAndSettle(next);
            }

            Assert.That(
                choices.activeInHierarchy,
                Is.True,
                "현재 프로덕션 대사에 선택지가 표시되지 않았습니다.");
        }

        protected IEnumerator CompleteActiveProductionDialogue(
            int maximumSteps = 500)
        {
            Button next =
                RequireComponent<Button>("Ingame/Line Panel/Panel/Next");
            GameObject choices =
                RequireObject("Ingame/Line Panel/Select Btn");
            GameObject investigation =
                RequireObject("Investigation Dialogue UI");
            Button investigationAction =
                RequireComponent<Button>(
                    "Investigation Dialogue UI/Investigation Frame/Action");
            int steps = 0;
            while (Dialogue.IsBusy)
            {
                Assert.That(
                    steps++,
                    Is.LessThan(maximumSteps),
                    "프로덕션 대사가 완료되기 전에 진행 상한을 초과했습니다.");

                InvestigationScreenController detailedController =
                    Object.FindFirstObjectByType<InvestigationScreenController>(
                        FindObjectsInactive.Include);
                if (detailedController != null &&
                    detailedController.IsOpen)
                {
                    GameObject detailedInvestigation =
                        Object.FindObjectsByType<Transform>(
                                FindObjectsInactive.Include,
                                FindObjectsSortMode.None)
                            .First(item =>
                                item.name == "Investigation Screen" &&
                                item.gameObject.activeInHierarchy)
                            .gameObject;
                    Button detailedInvestigationAction =
                        detailedInvestigation.transform
                            .Find("Primary Action")
                            .GetComponent<Button>();
                    if (!detailedInvestigationAction.interactable)
                    {
                        Button[] points = detailedInvestigation
                            .GetComponentsInChildren<Button>(false)
                            .Where(button =>
                                button.name.StartsWith("Inspection Point ") &&
                                button.gameObject.activeInHierarchy &&
                                button.interactable)
                            .ToArray();
                        Assert.That(
                            points,
                            Is.Not.Empty,
                            "세부 조사 화면에서 확인할 관찰 지점을 찾지 못했습니다.");
                        foreach (Button point in points)
                        {
                            point.onClick.Invoke();
                            yield return null;
                        }
                        UnityEngine.Canvas.ForceUpdateCanvases();
                    }

                    Assert.That(
                        detailedInvestigationAction.interactable,
                        Is.True,
                        "모든 관찰 지점을 확인한 뒤 세부 조사 완료 버튼이 활성화되어야 합니다.");
                    yield return InvokeAndSettle(detailedInvestigationAction);
                }
                else if (investigation.activeInHierarchy)
                {
                    yield return InvokeAndSettle(investigationAction);
                }
                else if (choices.activeInHierarchy)
                {
                    Button choice = choices
                        .GetComponentsInChildren<Button>(false)
                        .FirstOrDefault(button =>
                            button.gameObject.activeInHierarchy &&
                            button.interactable);
                    Assert.That(
                        choice,
                        Is.Not.Null,
                        "활성화된 선택지 버튼을 찾지 못했습니다.");
                    yield return InvokeAndSettle(choice);
                }
                else
                {
                    yield return InvokeAndSettle(next);
                }
            }
        }

        protected IEnumerator CompleteOpeningScene()
        {
            yield return StartNewGameFromVisibleButton();
            yield return CompleteActiveProductionDialogue();

            Button invitation = null;
            float deadline = Time.realtimeSinceStartup + 2f;
            while (invitation == null &&
                   Time.realtimeSinceStartup < deadline)
            {
                invitation = Object.FindObjectsByType<Button>(
                        FindObjectsInactive.Exclude,
                        FindObjectsSortMode.None)
                    .FirstOrDefault(button =>
                        button.name == "EvidenceHotspot_C-01");
                if (invitation == null)
                    yield return null;
            }
            Assert.That(
                invitation,
                Is.Not.Null,
                "도입 나레이션 뒤 항구 배경에서 구겨진 초대장을 선택할 수 있어야 합니다.");
            yield return InvokeAndSettle(invitation);

            InvestigationScreenController detailedController =
                Object.FindFirstObjectByType<InvestigationScreenController>(
                    FindObjectsInactive.Include);
            Assert.That(detailedController?.IsOpen, Is.True);
            GameObject detailedInvestigation =
                Object.FindObjectsByType<Transform>(
                        FindObjectsInactive.Include,
                        FindObjectsSortMode.None)
                    .First(item =>
                        item.name == "Investigation Screen" &&
                        item.gameObject.activeInHierarchy)
                    .gameObject;
            foreach (Button point in detailedInvestigation
                         .GetComponentsInChildren<Button>(false)
                         .Where(button =>
                             button.name.StartsWith("Inspection Point ")))
            {
                point.onClick.Invoke();
                yield return null;
            }
            Button action = detailedInvestigation.transform
                .Find("Primary Action")
                .GetComponent<Button>();
            Assert.That(action.interactable, Is.True);
            yield return InvokeAndSettle(action);
            yield return InvokeAndSettle(action);

            yield return StartPreparedProductionSceneFromFocusCharacter(
                ProductionSceneDirector.OpeningSceneId);
            yield return CompleteActiveProductionDialogue();
            Assert.That(
                State.HasCompletedScene("P-01"),
                Is.True,
                $"opening completion missing; busy={Dialogue.IsBusy}, " +
                $"active={Dialogue.ActiveProductionSceneId}, " +
                $"checkpoint={State.DialogueCheckpoint?.activeSceneId ?? "<none>"}");
        }

        protected Button RequireSaveSlotButton(int slot)
        {
            Assert.That(slot, Is.InRange(1, 3));
            return RequireComponent<Button>(
                "StartScene/Save Slot Selection/Slot Frame/" +
                $"Slot Card {slot}/Save Slot {slot}");
        }

        protected TMP_Text RequireSaveSlotText(int slot, string elementName)
        {
            Assert.That(slot, Is.InRange(1, 3));
            Assert.That(elementName, Is.Not.Null.And.Not.Empty);
            return RequireText(
                "StartScene/Save Slot Selection/Slot Frame/" +
                $"Slot Card {slot}/Save Slot {slot}/{elementName}");
        }

        protected GameObject RequireObject(string path)
        {
            Transform target = Canvas.Find(path);
            Assert.That(
                target,
                Is.Not.Null,
                $"Canvas/{path} 오브젝트를 찾지 못했습니다.");
            return target.gameObject;
        }

        protected T RequireComponent<T>(string path)
            where T : Component
        {
            GameObject target = RequireObject(path);
            T component = target.GetComponent<T>();
            Assert.That(
                component,
                Is.Not.Null,
                $"Canvas/{path}에 {typeof(T).Name} 컴포넌트가 없습니다.");
            return component;
        }

        protected TMP_Text RequireText(string path)
        {
            return RequireComponent<TMP_Text>(path);
        }

        protected void AssertNoRuntimeErrors(string phase)
        {
            Assert.That(
                runtimeErrors,
                Is.Empty,
                $"{phase} 중 Unity 오류가 발생했습니다:\n" +
                string.Join("\n\n", runtimeErrors));
        }

        protected static void AssertKoreanTextIsIntact(string text)
        {
            Assert.That(text, Is.Not.Null.And.Not.Empty);
            Assert.That(
                text.Contains('\uFFFD'),
                Is.False,
                "대사에 Unicode replacement character가 있습니다.");

            string[] commonMojibakeMarkers =
            {
                "???", "媛", "遺", "鍮", "吏", "寃"
            };
            Assert.That(
                commonMojibakeMarkers.Any(text.Contains),
                Is.False,
                $"대사에 깨진 한글 패턴이 있습니다: {text}");
        }

        protected static void AssertInsideSafeArea(
            RectTransform rect,
            string context)
        {
            var corners = new Vector3[4];
            rect.GetWorldCorners(corners);
            Rect safeArea = Screen.safeArea;
            foreach (Vector3 corner in corners)
            {
                Assert.That(
                    corner.x,
                    Is.InRange(safeArea.xMin, safeArea.xMax),
                    $"{context} x={corner.x} is outside {safeArea}.");
                Assert.That(
                    corner.y,
                    Is.InRange(safeArea.yMin, safeArea.yMax),
                    $"{context} y={corner.y} is outside {safeArea}.");
            }
        }

        private IEnumerator LoadScene()
        {
            AsyncOperation load = SceneManager.LoadSceneAsync(
                UiScenePath,
                LoadSceneMode.Single);
            Assert.That(
                load,
                Is.Not.Null,
                $"{UiScenePath} 로드를 시작하지 못했습니다.");

            while (!load.isDone)
            {
                yield return null;
            }

            yield return null;
            loadedScene = SceneManager.GetSceneByPath(UiScenePath);
            if (!loadedScene.IsValid())
            {
                loadedScene =
                    SceneManager.GetSceneByName("UI Basic Scene");
            }

            ResolveRuntime();
        }

        private void ResolveRuntime()
        {
            GameObject canvasObject = GameObject.Find("Canvas");
            Canvas = canvasObject != null ? canvasObject.transform : null;
            Database = DialogueDatabase.Instance;
            Dialogue = DialogueController.Instance;
            Flow = GameFlow.Instance;
            State = GameStateManager.Instance;
            Ui = UIManager.Instance;
        }

        private void AssertRuntimeReady()
        {
            Assert.That(
                loadedScene.IsValid() && loadedScene.isLoaded,
                Is.True,
                "UI Basic Scene이 로드되지 않았습니다.");
            Assert.That(Canvas, Is.Not.Null, "Canvas 싱글턴 루트를 찾지 못했습니다.");
            Assert.That(Database, Is.Not.Null, "DialogueDatabase가 초기화되지 않았습니다.");
            Assert.That(Dialogue, Is.Not.Null, "DialogueController가 초기화되지 않았습니다.");
            Assert.That(Flow, Is.Not.Null, "GameFlow가 초기화되지 않았습니다.");
            Assert.That(State, Is.Not.Null, "GameStateManager가 초기화되지 않았습니다.");
            Assert.That(Ui, Is.Not.Null, "UIManager가 초기화되지 않았습니다.");
        }

        private void CaptureRuntimeError(
            string condition,
            string stackTrace,
            LogType type)
        {
            if (type != LogType.Error &&
                type != LogType.Assert &&
                type != LogType.Exception)
            {
                return;
            }

            runtimeErrors.Add(
                $"{type}: {condition}\n{stackTrace}".TrimEnd());
        }

        private static void ClearSavedGame()
        {
            GameStateManager.SetActiveSaveSlot(1);
            foreach (string key in EnumerateSaveKeys())
            {
                PlayerPrefs.DeleteKey(key);
            }

            PlayerPrefs.Save();
        }

        private void PreserveSavedGame()
        {
            savedActiveSlot = GameStateManager.ActiveSaveSlot;
            savedPlayerPrefs.Clear();
            foreach (string key in EnumerateSaveKeys())
            {
                if (PlayerPrefs.HasKey(key))
                {
                    savedPlayerPrefs[key] = PlayerPrefs.GetString(key);
                }
            }
        }

        private void RestoreSavedGame()
        {
            foreach (KeyValuePair<string, string> pair in savedPlayerPrefs)
            {
                PlayerPrefs.SetString(pair.Key, pair.Value);
            }

            PlayerPrefs.Save();
            GameStateManager.SetActiveSaveSlot(savedActiveSlot);
        }

        private static IEnumerable<string> EnumerateSaveKeys()
        {
            for (int slot = 1; slot <= 3; slot++)
            {
                string slotKey =
                    slot == 1 ? SaveKey : $"{SaveKey}_SLOT_{slot}";
                yield return slotKey;
                yield return slotKey + "_BACKUP";
                yield return slotKey + "_PENDING";
            }
        }
    }
}
