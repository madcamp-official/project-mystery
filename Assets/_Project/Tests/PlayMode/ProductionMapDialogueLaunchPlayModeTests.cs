using System.Collections;
using System.Linq;
using NUnit.Framework;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.TestTools;
using UnityEngine.UI;
using Wake.Exploration;
using Wake.Narrative;
using Wake.Puzzles;
using Wake.UI;

namespace Wake.Tests.PlayMode
{
    public sealed class ProductionMapDialogueLaunchPlayModeTests :
        UiBasicScenePlayModeFixture
    {
        [UnityTest]
        public IEnumerator AvailableEntry_WaitsForFocusCharacterClick()
        {
            yield return CompleteOpeningScene();
            Assert.That(State.HasCompletedScene("P-01"), Is.True);
            Assert.That(Dialogue.IsBusy, Is.False);

            yield return ShowOrRefreshMap();
            MapController map = RequireMap();
            Button gangway = RequireSceneButton("P-02");
            Assert.That(gangway.interactable, Is.True);
            Assert.That(
                gangway.GetComponentInChildren<TMP_Text>().text,
                Is.EqualTo("승선 통로"));
            Transform destinationArrow =
                gangway.transform.Find("Objective Destination Arrow");
            Assert.That(destinationArrow, Is.Not.Null);
            Assert.That(destinationArrow.gameObject.activeSelf, Is.True);
            Assert.That(
                destinationArrow.GetComponentsInChildren<Image>(true),
                Has.Length.EqualTo(3));

            Button layeredGangway =
                RequireLayeredLocationButton("GANGWAY");
            yield return InvokeAndSettle(layeredGangway);
            Assert.That(
                State.CurrentLocationCode,
                Is.EqualTo("PORT"),
                "목적지를 선택하는 것만으로 이동하면 안 됩니다.");
            yield return InvokeAndSettle(RequireLayeredTravelButton());
            yield return WaitForTravel(map, "GANGWAY");

            Assert.That(map.LastTravelResult.IsAllowed, Is.True);
            Assert.That(
                map.LastTravelResult.Location.LocationCode,
                Is.EqualTo("GANGWAY"));
            Assert.That(State.CurrentLocationCode, Is.EqualTo("GANGWAY"));
            Assert.That(Dialogue.IsBusy, Is.False);
            Assert.That(Dialogue.ActiveProductionSceneId, Is.Empty);
            Assert.That(Ui.ActivePanel, Is.EqualTo(UiPrimaryPanel.Ingame));
            Assert.That(State.DialogueCheckpoint, Is.Null);

            yield return StartPreparedProductionSceneFromFocusCharacter(
                "P-02");

            Assert.That(Dialogue.IsBusy, Is.True);
            Assert.That(
                Dialogue.ActiveProductionSceneId,
                Is.EqualTo("P-02"));
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
            Assert.That(rendered.text, Is.Not.Empty);
            Assert.That(
                expected.TextKo,
                Does.StartWith(rendered.text),
                "첫 대사 페이지는 CSV 원문 앞부분과 일치해야 합니다.");
            AssertKoreanTextIsIntact(rendered.text);
            AssertNoRuntimeErrors("맵에서 P-02 대화 시작");
        }

        [UnityTest]
        public IEnumerator LockedEntry_RejectsTravelBeforeLoadingOrDialogue()
        {
            yield return ShowOrRefreshMap();
            MapController map = RequireMap();
            Button gangway = RequireSceneButton("P-02");

            Assert.That(gangway.gameObject.activeSelf, Is.False);
            Assert.That(gangway.interactable, Is.False);
            Assert.That(HasLayeredGangwayNode(), Is.False);

            SceneTravelResult result = map.TryTravelToScene("P-02");
            yield return null;

            Assert.That(result.IsAllowed, Is.False);
            Assert.That(
                result.DenialReason,
                Is.EqualTo(SceneAccessDenialReason.SceneNotUnlocked));
            Assert.That(State.CurrentLocationCode, Is.Empty);
            Assert.That(Dialogue.IsBusy, Is.False);
            Assert.That(Dialogue.ActiveProductionSceneId, Is.Empty);
            Assert.That(Ui.ActivePanel, Is.EqualTo(UiPrimaryPanel.Map));
            AssertNoRuntimeErrors("잠긴 맵 장면 거부");
        }

        [UnityTest]
        public IEnumerator D202FocusCharacterAndBloodPuzzle_RequireHorizon()
        {
            yield return StartNewGameFromVisibleButton(
                startOpeningDialogue: false);
            foreach (ProductionSceneDefinition scene in
                     ProductionSceneCatalog.All.TakeWhile(
                         scene => scene.SceneId != "D2-02"))
            {
                State.RecordCompletedScene(scene.SceneId);
            }
            State.UnlockProductionScene("D2-02");

            MapController map = RequireMap();
            map.RefreshMap();
            ProductionMapEntry vipLounge =
                map.CurrentViewModel.Entries.Single(
                    entry => entry.Spec.Code == "VIP_LOUNGE");
            LocationLoader.Instance.PrepareNarrativeScene(string.Empty);
            Assert.That(
                LocationLoader.Instance.TryLoadLocation(
                    vipLounge.Location,
                    out _),
                Is.True);
            Ui.ShowIngame();
            yield return WaitForUiTransition();
            yield return null;

            Button[] vipCharacters = Object.FindObjectsByType<Button>(
                    FindObjectsInactive.Include,
                    FindObjectsSortMode.None)
                .Where(button =>
                    button.name.StartsWith("AmbientCharacter_"))
                .ToArray();
            string vipCharacterNames = string.Join(
                ", ",
                vipCharacters.Select(button => button.name));
            Assert.That(
                vipCharacters.Any(button =>
                    button.name.StartsWith("AmbientCharacter_HELENA")),
                Is.False,
                vipCharacterNames);
            Assert.That(
                vipCharacters.Any(button =>
                    button.name.StartsWith("AmbientCharacter_EVELYN")),
                Is.True,
                vipCharacterNames);
            Assert.That(
                vipCharacters.Any(button =>
                    button.name.StartsWith("AmbientCharacter_CLAIRE")),
                Is.True,
                vipCharacterNames);
            Assert.That(
                Dialogue.TalkToWorldCharacter("D2-02", "HELENA"),
                Is.False);

            BloodDirectionPuzzleUIController bloodPuzzle =
                RequireObject("Ingame")
                    .GetComponent<BloodDirectionPuzzleUIController>();
            Assert.That(bloodPuzzle, Is.Not.Null);
            Assert.That(bloodPuzzle.Open(), Is.False);
            Assert.That(bloodPuzzle.IsOpen, Is.False);

            ProductionMapEntry horizon =
                map.CurrentViewModel.Entries.Single(
                    entry => entry.Spec.Code == "HORIZON");
            LocationLoader.Instance.PrepareNarrativeScene("D2-02");
            Assert.That(
                LocationLoader.Instance.TryLoadLocation(
                    horizon.Location,
                    out _),
                Is.True);
            yield return null;
            yield return null;
            Assert.That(
                bloodPuzzle.Open(),
                Is.False,
                "The puzzle must wait for the D2-02 dialogue checkpoint.");

            Button helena = Object.FindObjectsByType<Button>(
                    FindObjectsInactive.Exclude,
                    FindObjectsSortMode.None)
                .Single(button =>
                    button.name.StartsWith("AmbientCharacter_HELENA"));
            yield return InvokeAndSettle(helena);
            Assert.That(
                Dialogue.ActiveProductionSceneId,
                Is.EqualTo("D2-02"));
            yield return CompleteActiveProductionDialogue();

            Assert.That(bloodPuzzle.IsOpen, Is.True);
            Assert.That(State.DialogueCheckpoint, Is.Not.Null);
            Assert.That(
                State.DialogueCheckpoint.activeSceneId,
                Is.EqualTo("D2-02"));
            Assert.That(
                State.DialogueCheckpoint.pendingInteractionId,
                Is.EqualTo(ProductionPuzzleCatalog.BloodPattern));
            AssertNoRuntimeErrors(
                "D2-02 focus character and blood puzzle location gate");
        }

        [UnityTest]
        public IEnumerator CompletedEntry_HidesGangwayAndPreventsRevisit()
        {
            yield return CompleteOpeningScene();
            Assert.That(Dialogue.IsBusy, Is.False);
            State.RecordCompletedScene("P-02");

            yield return ShowOrRefreshMap();
            Button gangway = RequireSceneButton("P-02");
            Assert.That(gangway.gameObject.activeSelf, Is.False);
            Assert.That(HasLayeredGangwayNode(), Is.False);
            Assert.That(State.CurrentLocationCode, Is.EqualTo("PORT"));
            Assert.That(Dialogue.IsBusy, Is.False);
            Assert.That(Dialogue.ActiveProductionSceneId, Is.Empty);
            Assert.That(State.DialogueCheckpoint, Is.Null);
            Assert.That(Ui.ActivePanel, Is.EqualTo(UiPrimaryPanel.Map));
            AssertNoRuntimeErrors("완료 장소 재방문");
        }

        [UnityTest]
        public IEnumerator BusyDialogue_DoesNotMoveToAnotherAvailableScene()
        {
            yield return StartNewGameFromVisibleButton();
            Assert.That(Dialogue.ActiveProductionSceneId, Is.EqualTo("P-01"));
            State.RecordCompletedScene("P-01");
            State.UnlockProductionScene("P-02");
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

        [UnityTest]
        public IEnumerator PrologueMap_UnlocksGangwayThenSuiteBeforeFreeTravel()
        {
            yield return CompleteOpeningScene();
            yield return ShowOrRefreshMap();

            Assert.That(RequireSceneButton("P-02").interactable, Is.True);
            Assert.That(HasLayeredGangwayNode(), Is.True);

            State.RecordCompletedScene("P-02");
            State.UnlockProductionScene("P-03");
            yield return ShowOrRefreshMap();

            Button suite = RequireLocationButton("RICHARD_SUITE");
            Assert.That(suite.interactable, Is.True);
            Assert.That(
                RequireSceneButton("P-02").gameObject.activeSelf,
                Is.False);
            Assert.That(HasLayeredGangwayNode(), Is.False);
            Assert.That(
                suite.GetComponentInChildren<TMP_Text>().text,
                Is.EqualTo("리처드 스위트룸"));
            State.RecordCompletedScene("P-03");
            yield return ShowOrRefreshMap();

            Assert.That(
                RequireSceneButton("P-02").gameObject.activeSelf,
                Is.False);
            ProductionMapEntry revisitableSuite =
                RequireMap().CurrentViewModel.Entries.Single(
                    entry => entry.Spec.Code == "RICHARD_SUITE");
            Assert.That(
                revisitableSuite.Status,
                Is.EqualTo(ProductionMapEntryStatus.LocationOnly));
            Assert.That(revisitableSuite.SceneId, Is.Empty);
            Assert.That(
                RequireLocationButton("RICHARD_SUITE").interactable,
                Is.True);
            AssertNoRuntimeErrors("프롤로그 순차 이동");
        }

        [UnityTest]
        public IEnumerator PassengerMap_UsesPolygonRoomsAndHidesTechnicalData()
        {
            yield return CompleteOpeningScene();
            yield return ShowOrRefreshMap();

            Assert.That(
                RequireObject(
                    "Map/Rooms/Layered Map Surface/Deck Map/" +
                    "Map Room Hit Areas/Room Hit Area PORT"),
                Is.Not.Null);
            Button deckNine = Object.FindObjectsByType<Button>(
                    FindObjectsInactive.Include,
                    FindObjectsSortMode.None)
                .Single(button => button.name == "Deck 9 Tab");
            yield return InvokeAndSettle(deckNine);
            Assert.That(
                RequireComponent<TMP_Text>(
                    "Map/Rooms/Layered Map Surface/Deck Map/" +
                    "Structural Map Annotations/Atrium Connection Label")
                    .text,
                Is.EqualTo("아트리움"));

            MapRoomHitAreaGraphic[] deckNineRooms =
                Object.FindObjectsByType<MapRoomHitAreaGraphic>(
                        FindObjectsInactive.Exclude,
                        FindObjectsSortMode.None)
                    .Where(graphic =>
                        graphic.transform.parent?.name ==
                        "Map Room Hit Areas")
                    .ToArray();
            Assert.That(deckNineRooms, Has.Length.EqualTo(4));
            MapRoomHitAreaPointerHandler ballroom =
                Object.FindObjectsByType<MapRoomHitAreaPointerHandler>(
                        FindObjectsInactive.Exclude,
                        FindObjectsSortMode.None)
                    .Single(handler =>
                        handler.name == "Room Hit Area BALLROOM");
            ballroom.OnPointerClick(
                new PointerEventData(EventSystem.current));
            yield return null;
            Assert.That(
                RequireObject(
                        "Map/Rooms/Layered Map Surface/Location Detail/" +
                        "Location Name")
                    .GetComponent<TMP_Text>().text,
                Is.EqualTo("잠긴 장소"));
            Assert.That(
                State.CurrentLocationCode,
                Is.EqualTo("PORT"),
                "방 영역 선택만으로는 이동하면 안 됩니다.");
            Assert.That(
                RequireObject(
                    "Map/Rooms/Layered Map Surface/Deck Map/" +
                    "Passenger Spoiler Redactions/" +
                    "Passenger Redaction D9_BALLROOM_SERVICE"),
                Is.Not.Null);

            Button technical = Object.FindObjectsByType<Button>(
                    FindObjectsInactive.Include,
                    FindObjectsSortMode.None)
                .Single(button => button.name == "Technical Layer Tab");
            Assert.That(technical.interactable, Is.False);
            Assert.That(
                Object.FindObjectsByType<MapRoomHitAreaGraphic>(
                        FindObjectsInactive.Include,
                        FindObjectsSortMode.None)
                    .Any(graphic =>
                        graphic.name == "Room Hit Area SERVICE_RAIL"),
                Is.False);
        }

        [UnityTest]
        public IEnumerator LockedPublicRoom_UsesPadlockAndRedactsDetails()
        {
            yield return ShowOrRefreshMap();

            Button deckTen = Object.FindObjectsByType<Button>(
                    FindObjectsInactive.Include,
                    FindObjectsSortMode.None)
                .Single(button => button.name == "Deck 10 Tab");
            yield return InvokeAndSettle(deckTen);

            TMP_Text atriumLabel = RequireComponent<TMP_Text>(
                "Map/Rooms/Layered Map Surface/Deck Map/" +
                "Structural Map Annotations/Atrium Connection Label");
            Assert.That(atriumLabel.text, Is.EqualTo("아트리움"));
            Assert.That(atriumLabel.text, Does.Not.Contain("DECK"));
            Assert.That(atriumLabel.text, Does.Not.Contain("개방"));

            GameObject node = RequireObject(
                "Map/Rooms/Layered Map Surface/Deck Map/" +
                "Map Location Nodes/Layered Map Node RICHARD_SUITE");
            Assert.That(
                node.GetComponentsInChildren<MapPadlockGraphic>(true),
                Has.Length.EqualTo(1));
            Assert.That(
                node.GetComponentsInChildren<TMP_Text>(true),
                Is.Empty,
                "잠긴 지도 노드에는 장소 이름을 노출하지 않아야 합니다.");
            Assert.That(
                node.GetComponent<Image>().raycastTarget,
                Is.False,
                "방 polygon만 클릭을 처리해야 합니다.");

            string roomPath =
                "Map/Rooms/Layered Map Surface/Deck Map/" +
                "Map Room Hit Areas/Room Hit Area RICHARD_SUITE";
            MapRoomHitAreaGraphic room =
                RequireComponent<MapRoomHitAreaGraphic>(roomPath);
            Assert.That(room.IsLocked, Is.True);
            RequireComponent<MapRoomHitAreaPointerHandler>(roomPath)
                .OnPointerClick(new PointerEventData(EventSystem.current));
            yield return null;

            const string detailPath =
                "Map/Rooms/Layered Map Surface/Location Detail/";
            TMP_Text locationName =
                RequireComponent<TMP_Text>(detailPath + "Location Name");
            TMP_Text locationDescription =
                RequireComponent<TMP_Text>(
                    detailPath + "Location Description");
            TMP_Text knownPeople =
                RequireComponent<TMP_Text>(detailPath + "Known People");
            TMP_Text accessDescription =
                RequireComponent<TMP_Text>(
                    detailPath + "Access Description");
            Button travel = RequireLayeredTravelButton();

            Assert.That(locationName.text, Is.EqualTo("잠긴 장소"));
            Assert.That(locationDescription.text, Is.Empty);
            Assert.That(knownPeople.text, Is.Empty);
            Assert.That(accessDescription.text, Is.Empty);
            Assert.That(
                locationDescription.gameObject.activeSelf,
                Is.False);
            Assert.That(knownPeople.gameObject.activeSelf, Is.False);
            Assert.That(
                accessDescription.gameObject.activeSelf,
                Is.False);
            Assert.That(travel.interactable, Is.False);

            TMP_Text[] detailTexts = RequireObject(
                    "Map/Rooms/Layered Map Surface/Location Detail")
                .GetComponentsInChildren<TMP_Text>(true);
            string renderedDetail = string.Join(
                "\n",
                detailTexts.Select(text => text.text));
            Assert.That(
                renderedDetail,
                Does.Not.Contain(
                    CanonicalLocationCatalog
                        .FindSpec("RICHARD_SUITE")
                        .DisplayName));
            Assert.That(
                renderedDetail,
                Does.Not.Contain("선행 장면 필요"));
        }

        [UnityTest]
        public IEnumerator CompletingD602_RevealsTechnicalLayerOnlyOnSelection()
        {
            yield return CompleteOpeningScene();
            State.RecordCompletedScene(
                MapDeckCatalog.TechnicalUnlockSceneId);
            yield return ShowOrRefreshMap();

            Button deckSeven = Object.FindObjectsByType<Button>(
                    FindObjectsInactive.Include,
                    FindObjectsSortMode.None)
                .Single(button => button.name == "Deck 7 Tab");
            yield return InvokeAndSettle(deckSeven);
            Button technical = Object.FindObjectsByType<Button>(
                    FindObjectsInactive.Include,
                    FindObjectsSortMode.None)
                .Single(button => button.name == "Technical Layer Tab");
            Assert.That(technical.interactable, Is.True);
            Assert.That(
                RequireObject(
                        "Map/Rooms/Layered Map Surface/Deck Map/" +
                        "Technical Overlay")
                    .GetComponent<Image>().enabled,
                Is.False);

            yield return InvokeAndSettle(technical);

            Assert.That(
                RequireObject(
                        "Map/Rooms/Layered Map Surface/Deck Map/" +
                        "Technical Overlay")
                    .GetComponent<Image>().enabled,
                Is.True);
            Assert.That(
                Object.FindObjectsByType<MapPassengerRedactionGraphic>(
                        FindObjectsInactive.Exclude,
                        FindObjectsSortMode.None)
                    .Any(graphic =>
                        graphic.name ==
                        "Passenger Redaction D7_NORTH_TECHNICAL"),
                Is.False);
        }

        private static bool HasLayeredGangwayNode() =>
            Object.FindObjectsByType<Button>(
                    FindObjectsInactive.Include,
                    FindObjectsSortMode.None)
                .Any(button =>
                    button.name == "Layered Map Node GANGWAY" &&
                    button.gameObject.activeInHierarchy);

        private IEnumerator ShowOrRefreshMap()
        {
            if (Ui.ActivePanel == UiPrimaryPanel.Map)
            {
                RequireMap().RefreshMap();
            }
            else
            {
                Ui.ShowMap();
            }

            yield return WaitForMap();
        }

        private IEnumerator WaitForMap()
        {
            yield return WaitForUiTransition();
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
            Assert.That(
                ProductionSceneCatalog.TryGet(
                    sceneId,
                    out ProductionSceneDefinition scene),
                Is.True,
                $"{sceneId} 장면 정의가 필요합니다.");
            CanonicalLocationSpec location =
                CanonicalLocationCatalog.FindSpec(
                    scene.NarrativeLocationCode);
            Assert.That(
                location,
                Is.Not.Null,
                $"{sceneId} 장면의 실제 장소가 필요합니다.");
            return RequireLocationButton(location.Code);
        }

        private Button RequireLocationButton(string locationCode)
        {
            return RequireComponent<Button>(
                "Map/Rooms/Dynamic Location Viewport/" +
                $"Dynamic Location Content/Map Node {locationCode}");
        }

        private Button RequireLayeredLocationButton(string locationCode)
        {
            return RequireComponent<Button>(
                "Map/Rooms/Layered Map Surface/Deck Map/" +
                "Map Location Nodes/" +
                $"Layered Map Node {locationCode}");
        }

        private Button RequireLayeredTravelButton()
        {
            GameObject detail = RequireObject(
                "Map/Rooms/Layered Map Surface/Location Detail");
            Button[] buttons =
                detail.GetComponentsInChildren<Button>(true);
            Assert.That(
                buttons,
                Has.Length.EqualTo(1),
                "장소 상세 패널에는 이동 확인 버튼이 하나여야 합니다.");
            return buttons[0];
        }

        private static IEnumerator WaitForTravel(
            MapController map,
            string locationCode)
        {
            float deadline = Time.realtimeSinceStartup + 3f;
            while ((!map.LastTravelResult.IsAllowed ||
                    !string.Equals(
                        map.LastTravelResult.Location?.LocationCode,
                        locationCode,
                        System.StringComparison.Ordinal)) &&
                   Time.realtimeSinceStartup < deadline)
            {
                yield return null;
            }
        }
    }
}
