using System.Collections;
using System.Linq;
using NUnit.Framework;
using TMPro;
using UnityEngine;
using UnityEngine.TestTools;
using UnityEngine.UI;
using Wake.Core;
using Wake.Evidence;
using Wake.Exploration;
using Wake.Narrative;
using Wake.UI;

namespace Wake.Tests.PlayMode
{
    public sealed class UiBasicSceneEndToEndPlayModeTests :
        UiBasicScenePlayModeFixture
    {
        [UnityTest]
        public IEnumerator ProductionPath_ReturnsToTitleAndStartsFreshGame()
        {
            yield return CompleteOpeningScene();
            Assert.That(Flow.HasActiveSession, Is.True);
            Assert.That(State.HasCompletedScene("P-01"), Is.True);
            Assert.That(State.CurrentLocationCode, Is.EqualTo("PORT"));

            CompleteScenes("P-02", "P-03");
            yield return SelectSceneFromMap("D1-01");
            Assert.That(Dialogue.ActiveProductionSceneId, Is.EqualTo("D1-01"));
            Assert.That(State.CurrentLocationCode, Is.EqualTo("ATRIUM"));

            Dialogue.CancelActiveDialogue();
            CompleteScenes("D1-01", "D1-02", "D1-03");
            yield return SelectSceneFromMap("D1-04");
            Assert.That(Dialogue.ActiveProductionSceneId, Is.EqualTo("D1-04"));
            Assert.That(State.CurrentLocationCode, Is.EqualTo("CREW_STAIRS"),
                "대화 전용 장소는 현재 확정 배경을 유지해야 합니다.");
            NarrativeLocationHUDController locationHud =
                RequireObject("Ingame")
                    .GetComponent<NarrativeLocationHUDController>();
            Assert.That(locationHud.IsWarningVisible, Is.False);

            Dialogue.CancelActiveDialogue();
            CompleteScenes("D1-04", "D1-05", "D1-06", "D1-07");
            yield return SelectSceneFromMap("D2-01");
            Assert.That(Dialogue.ActiveProductionSceneId, Is.EqualTo("D2-01"));
            Assert.That(State.CurrentLocationCode, Is.EqualTo("HORIZON"));
            Assert.That(State.DialogueCheckpoint.activeSceneId,
                Is.EqualTo("D2-01"));

            Assert.That(EvidenceInventory.Instance.Contains("C-01"), Is.True);
            Assert.That(EvidenceInventory.Instance.TryAddById("C-01"), Is.False);
            Ui.ShowEvidence();
            yield return null;
            Assert.That(Ui.ActivePanel, Is.EqualTo(UiPrimaryPanel.Evidence));
            Assert.That(RequireObject("Evidence").activeInHierarchy, Is.True);
            Assert.That(State.CollectedEvidenceIds, Does.Contain("C-01"));

            State.RecordLocation("PORT");
            Assert.That(
                State.CurrentLocationCode,
                Is.EqualTo("PORT"),
                "회귀 조건: 저장 위치가 진행 중인 D2-01의 HORIZON과 달라야 합니다.");

            yield return ReloadScenePreservingSave();
            Assert.That(GameStateManager.HasSaveData, Is.True);
            yield return ContinueFromVisibleButton();
            Assert.That(Dialogue.ActiveProductionSceneId, Is.EqualTo("D2-01"));
            Assert.That(State.DialogueCheckpoint.activeSceneId,
                Is.EqualTo("D2-01"));
            Assert.That(State.CurrentLocationCode, Is.EqualTo("HORIZON"));
            Assert.That(
                LocationLoader.Instance.CurrentLocation.LocationCode,
                Is.EqualTo("HORIZON"));
            Assert.That(
                LocationLoader.Instance.CurrentLocation.BackgroundSprite,
                Is.Not.Null);
            Assert.That(EvidenceInventory.Instance.Contains("C-01"), Is.True);

            Dialogue.CancelActiveDialogue();
            State.ClearDialogueCheckpoint();
            Assert.That(State.TryRecordFinalEnding(
                FinalAccusationResolver.BadEndingId), Is.True);
            ProductionEndingUIController ending =
                Object.FindFirstObjectByType<ProductionEndingUIController>();
            ending.ShowStoredEnding();
            yield return null;

            GameObject endingRoot = RequireObject("Production Ending");
            Assert.That(endingRoot.activeInHierarchy, Is.True);
            TMP_Text[] endingLabels =
                endingRoot.GetComponentsInChildren<TMP_Text>(true);
            Assert.That(endingLabels, Is.Not.Empty);
            foreach (TMP_Text label in endingLabels.Where(
                         label => !string.IsNullOrWhiteSpace(label.text)))
            {
                AssertKoreanTextIsIntact(label.text);
            }

            Button titleButton = endingRoot.GetComponentsInChildren<Button>(true)
                .Single(button => button.name == "타이틀로");
            yield return InvokeAndSettle(titleButton);

            AssertTitleSessionIsClean(endingRoot);
            Assert.That(GameStateManager.HasSaveData, Is.True,
                "타이틀 복귀는 Continue 저장본을 삭제하면 안 됩니다.");
            Assert.That(State.FinalEndingId,
                Is.EqualTo(FinalAccusationResolver.BadEndingId));
            Assert.That(RequireObject("StartScene/Continue Btn").activeSelf,
                Is.False);

            yield return ContinueFromVisibleButton();
            Assert.That(LocationLoader.Instance.IsPresentationVisible, Is.True);
            Assert.That(State.FinalEndingId,
                Is.EqualTo(FinalAccusationResolver.BadEndingId));
            Assert.That(GameStateManager.HasSaveData, Is.True);

            Ui.ShowStartScene();
            yield return null;
            yield return StartNewGameFromVisibleButton();

            Assert.That(Ui.ActivePanel, Is.EqualTo(UiPrimaryPanel.Ingame));
            Assert.That(LocationLoader.Instance.IsPresentationVisible, Is.True);
            Assert.That(Flow.HasActiveSession, Is.True);
            Assert.That(Dialogue.IsBusy, Is.True);
            Assert.That(Dialogue.ActiveProductionSceneId, Is.EqualTo("P-01"));
            Assert.That(State.FinalEndingId, Is.Empty);
            Assert.That(State.CompletedProductionSceneIds, Is.Empty);
            Assert.That(State.CollectedEvidenceIds, Is.Empty);
            Assert.That(EvidenceInventory.Instance.Collected, Is.Empty);
            Assert.That(State.CurrentLocationCode, Is.EqualTo("PORT"));
            AssertSingleRuntimeControllers();
            AssertNoRuntimeErrors("전체 프로덕션 경로");
        }

        [UnityTest]
        public IEnumerator ShowStartScene_CancelsTransientSessionButKeepsSave()
        {
            yield return StartNewGameFromVisibleButton();
            Assert.That(Dialogue.IsBusy, Is.True);
            Assert.That(EvidenceInventory.Instance.TryAddById("C-02"), Is.True);
            string savedScene = State.DialogueCheckpoint.activeSceneId;

            Ui.ShowStartScene();
            yield return null;

            Assert.That(Ui.ActivePanel, Is.EqualTo(UiPrimaryPanel.Start));
            Assert.That(Flow.HasActiveSession, Is.False);
            Assert.That(Dialogue.IsBusy, Is.False);
            Assert.That(Dialogue.ActiveProductionSceneId, Is.Empty);
            Assert.That(RequireObject("Ingame/Line Panel").activeSelf, Is.False);
            Assert.That(EvidenceInventory.Instance.Collected, Is.Empty);
            Assert.That(State.DialogueCheckpoint.activeSceneId,
                Is.EqualTo(savedScene),
                "타이틀 복귀는 저장된 Continue 체크포인트를 보존해야 합니다.");
            Assert.That(State.CollectedEvidenceIds, Does.Contain("C-02"));
            Assert.That(GameStateManager.HasSaveData, Is.True);

            yield return ContinueFromVisibleButton();
            Assert.That(Dialogue.IsBusy, Is.True);
            Assert.That(Dialogue.ActiveProductionSceneId,
                Is.EqualTo(savedScene));
            Assert.That(EvidenceInventory.Instance.Contains("C-02"), Is.True);
            AssertSingleRuntimeControllers();
            AssertNoRuntimeErrors("타이틀 복귀 후 Continue");
        }

        [UnityTest]
        public IEnumerator ReturningToTitle_AllowsRepeatedNewGameInSameScene()
        {
            yield return StartNewGameFromVisibleButton();
            Assert.That(Dialogue.ActiveProductionSceneId, Is.EqualTo("P-01"));
            Assert.That(Flow.HasActiveSession, Is.True);
            ProductionDialogueCheckpoint firstCheckpoint =
                State.DialogueCheckpoint.Copy();

            Ui.ShowStartScene();
            yield return null;
            Assert.That(Dialogue.IsBusy, Is.False);
            Assert.That(Flow.HasActiveSession, Is.False);
            Assert.That(RequireObject("StartScene").activeSelf, Is.True);

            yield return StartNewGameFromVisibleButton();
            Assert.That(Dialogue.IsBusy, Is.True,
                "같은 Unity 씬에서도 새 수사를 다시 시작할 수 있어야 합니다.");
            Assert.That(Flow.HasActiveSession, Is.True);
            Assert.That(Dialogue.ActiveProductionSceneId, Is.EqualTo("P-01"));
            Assert.That(State.DialogueCheckpoint, Is.Not.SameAs(firstCheckpoint));
            Assert.That(State.DialogueCheckpoint.activeSceneId,
                Is.EqualTo("P-01"));
            Assert.That(State.DialogueCheckpoint.lineIndex, Is.Zero);
            Assert.That(State.FinalEndingId, Is.Empty);
            Assert.That(State.CollectedEvidenceIds, Is.Empty);
            AssertSingleRuntimeControllers();
            AssertNoRuntimeErrors("같은 씬에서 새 수사 재시작");
        }

        private IEnumerator SelectSceneFromMap(string sceneId)
        {
            State.UnlockProductionScene(sceneId);
            Ui.ShowMap();
            yield return null;
            yield return null;

            Transform content = Canvas.Find(
                "Map/Rooms/Dynamic Location Viewport/" +
                "Dynamic Location Content");
            Assert.That(content, Is.Not.Null);
            Button[] candidates = content.GetComponentsInChildren<Button>(true)
                .Where(button =>
                    button.GetComponentInChildren<TMP_Text>()?.text
                        .Contains(sceneId) == true)
                .ToArray();
            Assert.That(candidates, Has.Length.EqualTo(1),
                $"{sceneId} 맵 항목은 하나여야 합니다.");
            Assert.That(candidates[0].interactable, Is.True,
                $"{sceneId} 맵 항목이 잠겨 있습니다.");

            yield return InvokeAndSettle(candidates[0]);
            Assert.That(Ui.ActivePanel, Is.EqualTo(UiPrimaryPanel.Ingame));
            Assert.That(Dialogue.IsBusy, Is.True);
        }

        private void CompleteScenes(params string[] sceneIds)
        {
            foreach (string sceneId in sceneIds)
            {
                State.RecordCompletedScene(sceneId);
            }
        }

        private void AssertTitleSessionIsClean(GameObject endingRoot)
        {
            Assert.That(Ui.ActivePanel, Is.EqualTo(UiPrimaryPanel.Start));
            Assert.That(Flow.HasActiveSession, Is.False);
            Assert.That(RequireObject("StartScene").activeSelf, Is.True);
            Assert.That(RequireObject("Ingame").activeSelf, Is.False);
            Assert.That(endingRoot.activeSelf, Is.False);
            Assert.That(LocationLoader.Instance.IsPresentationVisible, Is.False,
                "타이틀 화면에서는 마지막 위치 배경과 핫스팟을 숨겨야 합니다.");
            Assert.That(Dialogue.IsBusy, Is.False);
            Assert.That(Dialogue.ActiveProductionSceneId, Is.Empty);
            Assert.That(EvidenceInventory.Instance.Collected, Is.Empty);
            Assert.That(Ui.OpenRuntimeModalCount, Is.Zero);
            AssertSingleRuntimeControllers();
        }

        private static void AssertSingleRuntimeControllers()
        {
            Assert.That(Object.FindObjectsByType<UIManager>(
                FindObjectsInactive.Include, FindObjectsSortMode.None),
                Has.Length.EqualTo(1));
            Assert.That(Object.FindObjectsByType<GameFlow>(
                FindObjectsInactive.Include, FindObjectsSortMode.None),
                Has.Length.EqualTo(1));
            Assert.That(Object.FindObjectsByType<DialogueController>(
                FindObjectsInactive.Include, FindObjectsSortMode.None),
                Has.Length.EqualTo(1));
            Assert.That(Object.FindObjectsByType<ProductionEndingUIController>(
                FindObjectsInactive.Include, FindObjectsSortMode.None),
                Has.Length.EqualTo(1));
        }
    }
}
