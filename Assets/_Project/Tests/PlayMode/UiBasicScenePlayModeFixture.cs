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
            AssertNoRuntimeErrors("씬 정리");
            Application.logMessageReceived -= CaptureRuntimeError;
        }

        protected IEnumerator ReloadScenePreservingSave()
        {
            yield return LoadScene();
            AssertRuntimeReady();
            AssertNoRuntimeErrors("저장 데이터 유지 후 씬 재로드");
        }

        protected IEnumerator StartNewGameFromVisibleButton()
        {
            Button startButton =
                RequireComponent<Button>("StartScene/Start Game Btn");
            Assert.That(
                startButton.gameObject.activeInHierarchy,
                Is.True,
                "새 게임 버튼은 시작 화면에서 보여야 합니다.");

            yield return InvokeAndSettle(startButton);
            Button slot = RequireObject("StartScene/Save Slot Selection")
                .GetComponentsInChildren<Button>(true)
                .First(button =>
                    button.name.StartsWith("Save Slot") &&
                    button.GetComponentInChildren<TMP_Text>(true).text.Contains("빈 슬롯"));
            yield return InvokeAndSettle(slot);
            Button confirm = RequireObject(
                    "StartScene/Save Slot Selection/Start Confirmation/Confirm")
                .GetComponent<Button>();
            yield return InvokeAndSettle(confirm);
        }

        protected IEnumerator ContinueFromVisibleButton()
        {
            Button startButton =
                RequireComponent<Button>("StartScene/Start Game Btn");
            yield return InvokeAndSettle(startButton);
            Button slot = RequireObject("StartScene/Save Slot Selection")
                .GetComponentsInChildren<Button>(true)
                .First(button =>
                    button.name.StartsWith("Save Slot") &&
                    button.GetComponentInChildren<TMP_Text>(true).text.Contains("저장된 수사"));
            yield return InvokeAndSettle(slot);
            Button confirm = RequireObject(
                    "StartScene/Save Slot Selection/Start Confirmation/Confirm")
                .GetComponent<Button>();
            yield return InvokeAndSettle(confirm);
        }

        protected IEnumerator InvokeAndSettle(Button button)
        {
            Assert.That(button, Is.Not.Null);
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
        }

        protected IEnumerator AdvanceToVisibleChoices(int maximumSteps = 200)
        {
            Button next =
                RequireComponent<Button>("Ingame/Line Panel/Panel/Next");
            GameObject choices =
                RequireObject("Ingame/Line Panel/Select Btn");
            int steps = 0;
            while (!choices.activeInHierarchy && Dialogue.IsBusy)
            {
                Assert.That(
                    steps++,
                    Is.LessThan(maximumSteps),
                    "선택지가 나타나기 전에 대사 진행 상한을 초과했습니다.");
                yield return InvokeAndSettle(next);
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
            int steps = 0;
            while (Dialogue.IsBusy)
            {
                Assert.That(
                    steps++,
                    Is.LessThan(maximumSteps),
                    "프로덕션 대사가 완료되기 전에 진행 상한을 초과했습니다.");

                if (choices.activeInHierarchy)
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
            Assert.That(State.HasCompletedScene("P-01"), Is.True);
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
            PlayerPrefs.DeleteKey(SaveKey);
            PlayerPrefs.DeleteKey(SaveKey + "_BACKUP");
            PlayerPrefs.DeleteKey(SaveKey + "_PENDING");
            for (int slot = 2; slot <= 3; slot++)
            {
                string slotKey = $"{SaveKey}_SLOT_{slot}";
                PlayerPrefs.DeleteKey(slotKey);
                PlayerPrefs.DeleteKey(slotKey + "_BACKUP");
                PlayerPrefs.DeleteKey(slotKey + "_PENDING");
            }
            PlayerPrefs.Save();
        }
    }
}
