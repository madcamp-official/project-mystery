using System.Collections;
using System.Linq;
using NUnit.Framework;
using TMPro;
using UnityEngine;
using UnityEngine.TestTools;
using UnityEngine.UI;
using Wake.Core;
using Wake.Narrative;
using Wake.UI;

namespace Wake.Tests.PlayMode
{
    public sealed class SystemScreenFlowPlayModeTests :
        UiBasicScenePlayModeFixture
    {
        [UnityTest]
        public IEnumerator Title_UsesFourActionsAndHidesGameplayChrome()
        {
            yield return null;

            Transform menu = Canvas.Find(
                "StartScene/Title Presentation/Title Menu");
            Assert.That(menu, Is.Not.Null);
            TMP_Text[] labels = menu
                .GetComponentsInChildren<TMP_Text>(false);

            Assert.That(labels.Select(label => label.text), Is.EquivalentTo(
                new[] { "시작", "설정", "크레딧", "종료" }));
            RectTransform menuRect = menu as RectTransform;
            RectTransform logoRect = Canvas.Find(
                "StartScene/Title Presentation/Under the Horizon Logo")
                as RectTransform;
            RectTransform footer = Canvas.Find(
                "StartScene/Title Presentation/Title Footer")
                as RectTransform;
            TMP_Text version = footer.Find("Version")
                .GetComponent<TMP_Text>();
            TMP_Text copyright = footer.Find("Copyright")
                .GetComponent<TMP_Text>();
            AssertResponsiveLayout(menuRect);
            AssertResponsiveLayout(logoRect);
            AssertResponsiveLayout(footer);
            AssertInsideCanvas(menuRect);
            AssertInsideCanvas(logoRect);
            AssertInsideCanvas(footer);
            Assert.That(version.alignment, Is.EqualTo(
                TextAlignmentOptions.Right));
            Assert.That(copyright.alignment, Is.EqualTo(
                TextAlignmentOptions.Right));
            Assert.That(
                version.transform.GetSiblingIndex(),
                Is.LessThan(copyright.transform.GetSiblingIndex()));
            Assert.That(Ui.ActivePanel, Is.EqualTo(UiPrimaryPanel.Start));
            Assert.That(
                Ui.ActiveSystemScreen,
                Is.EqualTo(SystemScreenState.Title));
            Assert.That(RequireObject("Status HUD").activeSelf, Is.False);
            Assert.That(RequireObject("Ingame").activeSelf, Is.False);
            AssertNoRuntimeErrors("타이틀 시스템 화면");
        }

        [UnityTest]
        public IEnumerator Start_OpensExactlyThreeAuthoredSlotCards()
        {
            Button start = Canvas.Find(
                    "StartScene/Title Presentation/Title Menu/시작하기")
                .GetComponent<Button>();
            yield return InvokeAndSettle(start);

            GameObject selection =
                RequireObject("StartScene/Save Slot Selection");
            Button[] slots = selection
                .GetComponentsInChildren<Button>(true)
                .Where(button => button.name.StartsWith("Save Slot"))
                .ToArray();

            Assert.That(selection.activeInHierarchy, Is.True);
            Assert.That(slots, Has.Length.EqualTo(3));
            Assert.That(
                Ui.ActiveSystemScreen,
                Is.EqualTo(SystemScreenState.SaveSlots));
            Assert.That(RequireObject("Status HUD").activeSelf, Is.False);
            AssertNoRuntimeErrors("저장 슬롯 시스템 화면");
        }

        [UnityTest]
        public IEnumerator OccupiedSlot_RequiresConfirmationBeforeDeletion()
        {
            yield return StartNewGameFromVisibleButton();
            Ui.ShowStartScene();
            yield return null;

            Button start = Canvas.Find(
                    "StartScene/Title Presentation/Title Menu/시작하기")
                .GetComponent<Button>();
            yield return InvokeAndSettle(start);

            Button delete = RequireComponent<Button>(
                "StartScene/Save Slot Selection/Slot Frame/" +
                "Slot Card 1/Delete Slot 1");
            Assert.That(delete.gameObject.activeInHierarchy, Is.True);
            Assert.That(GameStateManager.HasSaveDataInSlot(1), Is.True);

            yield return InvokeAndSettle(delete);
            GameObject confirmation = RequireObject(
                "StartScene/Save Slot Selection/Start Confirmation");
            Assert.That(confirmation.activeInHierarchy, Is.True);
            Assert.That(GameStateManager.HasSaveDataInSlot(1), Is.True);

            Button confirm = RequireComponent<Button>(
                "StartScene/Save Slot Selection/" +
                "Start Confirmation/Confirm");
            yield return InvokeAndSettle(confirm);

            Assert.That(GameStateManager.HasSaveDataInSlot(1), Is.False);
            Assert.That(confirmation.activeSelf, Is.False);
            Assert.That(delete.gameObject.activeSelf, Is.False);
            Assert.That(
                RequireText(
                    "StartScene/Save Slot Selection/Slot Frame/" +
                    "Slot Card 1/Save Slot 1/Label").text,
                Does.Contain("비어 있는 기록"));
            AssertNoRuntimeErrors("저장 슬롯 삭제 확인");
        }

        [UnityTest]
        public IEnumerator Credits_BlocksUnderlyingInputAndReturnsToTitle()
        {
            Button credits = Canvas.Find(
                    "StartScene/Title Presentation/Title Menu/크레딧")
                .GetComponent<Button>();
            yield return InvokeAndSettle(credits);

            GameObject creditsScreen =
                RequireObject("System Screen Flow/Credits");
            CanvasGroup titleInput =
                RequireObject("StartScene").GetComponent<CanvasGroup>();
            Assert.That(creditsScreen.activeInHierarchy, Is.True);
            Assert.That(
                Ui.ActiveSystemScreen,
                Is.EqualTo(SystemScreenState.Credits));
            Assert.That(titleInput.interactable, Is.False);
            Assert.That(titleInput.blocksRaycasts, Is.False);

            Button back = RequireComponent<Button>(
                "System Screen Flow/Credits/타이틀로");
            yield return InvokeAndSettle(back);

            Assert.That(creditsScreen.activeSelf, Is.False);
            Assert.That(
                Ui.ActiveSystemScreen,
                Is.EqualTo(SystemScreenState.Title));
            Assert.That(titleInput.interactable, Is.True);
            Assert.That(titleInput.blocksRaycasts, Is.True);
            AssertNoRuntimeErrors("크레딧 왕복");
        }

        [UnityTest]
        public IEnumerator PauseAndConfirmation_OwnInputAndReturnToGameplay()
        {
            yield return StartNewGameFromVisibleButton();
            Dialogue.CancelActiveDialogue();
            yield return null;

            Ui.OpenPause();
            yield return WaitForUiTransition();

            GameObject pause =
                RequireObject("System Screen Flow/Pause");
            CanvasGroup ingameInput =
                RequireObject("Ingame").GetComponent<CanvasGroup>();
            Assert.That(pause.activeInHierarchy, Is.True);
            Assert.That(
                Ui.ActiveSystemScreen,
                Is.EqualTo(SystemScreenState.Pause));
            Assert.That(ingameInput.interactable, Is.False);

            Button resume =
                RequireComponent<Button>(
                    "System Screen Flow/Pause/Pause Menu/계속");
            yield return InvokeAndSettle(resume);

            Assert.That(pause.activeSelf, Is.False);
            Assert.That(
                Ui.ActiveSystemScreen,
                Is.EqualTo(SystemScreenState.None));
            Assert.That(ingameInput.interactable, Is.True);

            bool confirmed = false;
            Ui.RequestConfirmation(
                "확인",
                "현재 행동을 계속하시겠습니까?",
                () => confirmed = true);
            yield return WaitForUiTransition();

            GameObject confirmation =
                RequireObject("System Screen Flow/Confirmation");
            Assert.That(confirmation.activeInHierarchy, Is.True);
            Assert.That(ingameInput.interactable, Is.False);
            Button confirm = RequireComponent<Button>(
                "System Screen Flow/Confirmation/확인");
            yield return InvokeAndSettle(confirm);

            Assert.That(confirmed, Is.True);
            Assert.That(confirmation.activeSelf, Is.False);
            Assert.That(
                Ui.ActiveSystemScreen,
                Is.EqualTo(SystemScreenState.None));
            Assert.That(ingameInput.interactable, Is.True);
            AssertNoRuntimeErrors("일시정지와 확인 모달");
        }

        [UnityTest]
        public IEnumerator Settings_AnimatesAndReturnsToTitle()
        {
            Ui.OpenSettings();
            yield return WaitForUiTransition();

            GameObject settings = RequireObject("Settings Popup");
            CanvasGroup titleInput =
                RequireObject("StartScene").GetComponent<CanvasGroup>();
            Assert.That(settings.activeInHierarchy, Is.True);
            Assert.That(
                Ui.ActiveSystemScreen,
                Is.EqualTo(SystemScreenState.Settings));
            Assert.That(titleInput.interactable, Is.False);

            Button close = RequireComponent<Button>(
                "Settings Popup/Close");
            yield return InvokeAndSettle(close);

            Assert.That(settings.activeSelf, Is.False);
            Assert.That(
                Ui.ActiveSystemScreen,
                Is.EqualTo(SystemScreenState.Title));
            Assert.That(titleInput.interactable, Is.True);
            AssertNoRuntimeErrors("설정 화면 왕복");
        }

        [UnityTest]
        public IEnumerator DayBoundary_ShowsChapterTransitionBeforeNextScene()
        {
            yield return StartNewGameFromVisibleButton(false);
            GameObject transition = RequireObject(
                "System Screen Flow/ChapterTransition");
            Button continueButton = RequireComponent<Button>(
                "System Screen Flow/ChapterTransition/계속");

            foreach (ProductionDayBoundary boundary in
                     ProductionDayBoundaryCatalog.All)
            {
                Assert.That(
                    ProductionSceneCatalog.TryGet(
                        boundary.NextSceneId,
                        out ProductionSceneDefinition next),
                    Is.True);
                Assert.That(
                    ProductionChapterTransitionCatalog.TryGet(
                        boundary.CompletedSceneId,
                        out ChapterTransitionRequest chapter),
                    Is.True);
                InvestigationEventHub.Publish(
                    InvestigationEventKind.SceneCompleted,
                    boundary.CompletedSceneId,
                    boundary.NextSceneId);
                yield return WaitForUiTransition();

                Assert.That(
                    transition.activeInHierarchy,
                    Is.True,
                    boundary.CompletedSceneId);
                Assert.That(
                    Ui.ActiveSystemScreen,
                    Is.EqualTo(SystemScreenState.ChapterTransition),
                    boundary.CompletedSceneId);
                Assert.That(
                    RequireText(
                        "System Screen Flow/ChapterTransition/" +
                        "Chapter Context").text,
                    Does.Contain($"DAY {next.Day}"));
                Assert.That(
                    RequireText(
                        "System Screen Flow/ChapterTransition/" +
                        "Chapter Title").text,
                    Is.EqualTo($"{next.Day}일 차"));
                Assert.That(
                    RequireText(
                        "System Screen Flow/ChapterTransition/" +
                        "Chapter Summary").text,
                    Is.Not.Empty);

                Assert.That(continueButton.interactable, Is.False);
                continueButton.onClick.Invoke();
                yield return null;
                Assert.That(transition.activeInHierarchy, Is.True);

                yield return new WaitForSecondsRealtime(
                    chapter.MinimumDisplayTime + .1f);
                Assert.That(continueButton.interactable, Is.True);
                continueButton.onClick.Invoke();
                continueButton.onClick.Invoke();
                yield return new WaitForSecondsRealtime(0.6f);
                yield return WaitForUiTransition();

                Assert.That(transition.activeSelf, Is.False);
                Assert.That(
                    Ui.ActiveSystemScreen,
                    Is.Not.EqualTo(SystemScreenState.ChapterTransition));
            }

            AssertNoRuntimeErrors("DAY 경계 챕터 전환");
        }

        private void AssertResponsiveLayout(RectTransform rect)
        {
            Assert.That(rect, Is.Not.Null);
            Assert.That(rect.anchorMin.x, Is.LessThan(rect.anchorMax.x));
            Assert.That(rect.anchorMin.y, Is.LessThan(rect.anchorMax.y));
            Assert.That(rect.anchoredPosition, Is.EqualTo(Vector2.zero));
            Assert.That(rect.sizeDelta, Is.EqualTo(Vector2.zero));
        }

        private void AssertInsideCanvas(RectTransform rect)
        {
            RectTransform canvasRect = Canvas as RectTransform;
            Bounds bounds =
                RectTransformUtility.CalculateRelativeRectTransformBounds(
                    canvasRect,
                    rect);
            Rect visible = canvasRect.rect;
            const float tolerance = 0.5f;

            Assert.That(bounds.min.x, Is.GreaterThanOrEqualTo(
                visible.xMin - tolerance));
            Assert.That(bounds.max.x, Is.LessThanOrEqualTo(
                visible.xMax + tolerance));
            Assert.That(bounds.min.y, Is.GreaterThanOrEqualTo(
                visible.yMin - tolerance));
            Assert.That(bounds.max.y, Is.LessThanOrEqualTo(
                visible.yMax + tolerance));
        }
    }
}
