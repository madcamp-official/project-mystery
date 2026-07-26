using System.Collections;
using System.Linq;
using NUnit.Framework;
using TMPro;
using UnityEngine;
using UnityEngine.TestTools;
using UnityEngine.UI;
using Wake.Exploration;
using Wake.Narrative;
using Wake.UI;

namespace Wake.Tests.PlayMode
{
    public sealed class ProductionMapDialogueLaunchPlayModeTests :
        UiBasicScenePlayModeFixture
    {
        [UnityTest]
        public IEnumerator AvailableEntry_LoadsBackgroundAndStartsCsvScene()
        {
            yield return CompleteOpeningScene();
            Assert.That(State.HasCompletedScene("P-01"), Is.True);
            Assert.That(Dialogue.IsBusy, Is.False);

            Ui.ShowMap();
            yield return WaitForMap();
            MapController map = RequireMap();
            Button gangway = RequireSceneButton("P-02");
            Assert.That(gangway.interactable, Is.True);
            Assert.That(
                gangway.GetComponentInChildren<TMP_Text>().text,
                Does.Contain("이동 가능"));

            yield return InvokeAndSettle(gangway);

            Assert.That(map.LastTravelResult.IsAllowed, Is.True);
            Assert.That(
                map.LastTravelResult.Location.LocationCode,
                Is.EqualTo("GANGWAY"));
            Assert.That(State.CurrentLocationCode, Is.EqualTo("GANGWAY"));
            Assert.That(Dialogue.IsBusy, Is.True);
            Assert.That(
                Dialogue.ActiveProductionSceneId,
                Is.EqualTo("P-02"));
            Assert.That(Ui.ActivePanel, Is.EqualTo(UiPrimaryPanel.Ingame));
            Assert.That(State.DialogueCheckpoint, Is.Not.Null);
            Assert.That(
                State.DialogueCheckpoint.activeSceneId,
                Is.EqualTo("P-02"));
            Assert.That(State.DialogueCheckpoint.lineIndex, Is.Zero);

            DialogueRecord expected = Database.Records.Values
                .Where(record =>
                    record.SceneId == "P-02" &&
                    record.Speaker != "PLAYER_CHOICE")
                .OrderBy(record => record.Order)
                .First();
            TMP_Text rendered =
                RequireText("Ingame/Line Panel/Panel/line");
            Assert.That(rendered.text, Is.EqualTo(expected.TextKo));
            AssertKoreanTextIsIntact(rendered.text);
            AssertNoRuntimeErrors("맵에서 P-02 대화 시작");
        }

        [UnityTest]
        public IEnumerator LockedEntry_RejectsTravelBeforeLoadingOrDialogue()
        {
            Ui.ShowMap();
            yield return WaitForMap();
            MapController map = RequireMap();
            Button gangway = RequireSceneButton("P-02");

            Assert.That(gangway.interactable, Is.False);
            Assert.That(
                gangway.GetComponentInChildren<TMP_Text>().text,
                Does.Contain("선행 장면 필요"));

            SceneTravelResult result = map.TryTravelToScene("P-02");
            yield return null;

            Assert.That(result.IsAllowed, Is.False);
            Assert.That(
                result.DenialReason,
                Is.EqualTo(
                    SceneAccessDenialReason.PrerequisiteSceneIncomplete));
            Assert.That(State.CurrentLocationCode, Is.Empty);
            Assert.That(Dialogue.IsBusy, Is.False);
            Assert.That(Dialogue.ActiveProductionSceneId, Is.Empty);
            Assert.That(Ui.ActivePanel, Is.EqualTo(UiPrimaryPanel.Map));
            AssertNoRuntimeErrors("잠긴 맵 장면 거부");
        }

        [UnityTest]
        public IEnumerator CompletedEntry_RevisitsLocationWithoutReplay()
        {
            yield return CompleteOpeningScene();
            Assert.That(Dialogue.IsBusy, Is.False);
            State.RecordCompletedScene("P-02");

            Ui.ShowMap();
            yield return WaitForMap();
            Button gangway = RequireSceneButton("P-02");
            Assert.That(gangway.interactable, Is.True);
            Assert.That(
                gangway.GetComponentInChildren<TMP_Text>().text,
                Does.Contain("완료"));

            yield return InvokeAndSettle(gangway);

            Assert.That(State.CurrentLocationCode, Is.EqualTo("GANGWAY"));
            Assert.That(Dialogue.IsBusy, Is.False);
            Assert.That(Dialogue.ActiveProductionSceneId, Is.Empty);
            Assert.That(State.DialogueCheckpoint, Is.Null);
            Assert.That(Ui.ActivePanel, Is.EqualTo(UiPrimaryPanel.Ingame));
            AssertNoRuntimeErrors("완료 장소 재방문");
        }

        [UnityTest]
        public IEnumerator BusyDialogue_DoesNotMoveToAnotherAvailableScene()
        {
            yield return StartNewGameFromVisibleButton();
            Assert.That(Dialogue.ActiveProductionSceneId, Is.EqualTo("P-01"));
            State.RecordCompletedScene("P-01");
            MapController map = RequireMap();

            SceneTravelResult result = map.TryTravelToScene("P-02");
            yield return null;

            Assert.That(result.IsAllowed, Is.False);
            Assert.That(
                result.DenialReason,
                Is.EqualTo(SceneAccessDenialReason.DialogueUnavailable));
            Assert.That(State.CurrentLocationCode, Is.EqualTo("PORT"));
            Assert.That(Dialogue.ActiveProductionSceneId, Is.EqualTo("P-01"));
            Assert.That(
                State.DialogueCheckpoint.activeSceneId,
                Is.EqualTo("P-01"));
            AssertNoRuntimeErrors("진행 중 대화의 이중 시작 방지");
        }

        private IEnumerator CompleteOpeningScene()
        {
            yield return StartNewGameFromVisibleButton();
            Button next =
                RequireComponent<Button>("Ingame/Line Panel/Panel/Next");
            for (int index = 0; index < 5; index++)
            {
                yield return InvokeAndSettle(next);
            }

            yield return InvokeAndSettle(
                RequireComponent<Button>(
                    "Ingame/Line Panel/Select Btn/Choice"));
        }

        private IEnumerator WaitForMap()
        {
            yield return null;
            yield return null;
            Assert.That(Ui.ActivePanel, Is.EqualTo(UiPrimaryPanel.Map));
            Assert.That(
                RequireObject(
                    "Map/Rooms/Dynamic Location Viewport/" +
                    "Dynamic Location Content"),
                Is.Not.Null);
        }

        private MapController RequireMap()
        {
            MapController[] controllers =
                Object.FindObjectsByType<MapController>(
                    FindObjectsInactive.Include,
                    FindObjectsSortMode.None);
            Assert.That(controllers, Has.Length.EqualTo(1));
            return controllers[0];
        }

        private Button RequireSceneButton(string sceneId)
        {
            GameObject content = RequireObject(
                "Map/Rooms/Dynamic Location Viewport/" +
                "Dynamic Location Content");
            Button[] matches = content
                .GetComponentsInChildren<Button>(true)
                .Where(button =>
                    button.GetComponentInChildren<TMP_Text>()?.text
                        .Contains(sceneId) == true)
                .ToArray();
            Assert.That(
                matches,
                Has.Length.EqualTo(1),
                $"{sceneId} 맵 버튼은 정확히 하나여야 합니다.");
            return matches[0];
        }
    }
}
