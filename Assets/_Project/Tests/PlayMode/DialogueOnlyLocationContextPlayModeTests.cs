using System.Collections;
using System.Linq;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;
using UnityEngine.UI;
using Wake.Core;
using Wake.Exploration;
using Wake.UI;

namespace Wake.Tests.PlayMode
{
    public sealed class DialogueOnlyLocationContextPlayModeTests :
        UiBasicScenePlayModeFixture
    {
        [UnityTest]
        public IEnumerator NarrativeAlias_LoadsMappedBackgroundWithoutWarning()
        {
            yield return CompleteOpeningScene();
            State.RecordCompletedScene("D1-03");
            State.UnlockProductionScene("D1-04");
            Assert.That(State.CurrentLocationCode, Is.EqualTo("PORT"));

            Ui.ShowMap();
            yield return WaitForUiTransition();
            MapController map = Object.FindObjectsByType<MapController>(
                FindObjectsInactive.Include,
                FindObjectsSortMode.None).Single();
            Assert.That(map.CurrentViewModel.DialogueOnlyEntries, Is.Empty);
            Assert.That(map.CurrentViewModel.UnresolvedScenes, Is.Empty);
            ProductionMapEntry serviceDeck = map.CurrentViewModel.Entries
                .Single(entry => entry.Spec.Code == "SERVICE7");
            Assert.That(serviceDeck.SceneId, Is.EqualTo("D1-04"));
            Assert.That(serviceDeck.Status,
                Is.EqualTo(ProductionMapEntryStatus.Available));
            Assert.That(serviceDeck.Location.BackgroundSprite, Is.Not.Null);

            SceneTravelResult travel = map.TryTravelToScene("D1-04");
            yield return WaitForUiTransition();

            Assert.That(travel.IsAllowed, Is.True);
            Assert.That(Ui.ActivePanel, Is.EqualTo(UiPrimaryPanel.Ingame));
            Assert.That(State.CurrentLocationCode, Is.EqualTo("SERVICE7"));
            Assert.That(LocationLoader.Instance.CurrentLocation.LocationCode,
                Is.EqualTo("SERVICE7"));
            Assert.That(
                LocationLoader.Instance.IsPresentationVisible,
                Is.True);
            Assert.That(LocationLoader.Instance.CurrentLocation.BackgroundSprite,
                Is.Not.Null);
            Assert.That(Dialogue.IsBusy, Is.False);
            Assert.That(
                LocationLoader.Instance.UsesApprovedSemanticCharacterPlacement,
                Is.True,
                "D1-04 must use the approved crew-stairs semantic profile.");
            Assert.That(
                LocationLoader.Instance.HasApprovedSemanticSceneLayout,
                Is.True);
            Assert.That(
                LocationLoader.Instance.ApprovedSemanticCastMatches,
                Is.True,
                "The approved D1-04 layout must match the runtime's sole " +
                "cabin-attendant witness.");
            Assert.That(
                LocationLoader.Instance.ActiveBackgroundVariantKey,
                Is.EqualTo(
                    "LocationBackgroundVariants/bg_crew_stairs_default"));
            Assert.That(
                ApprovedBackgroundSemanticResolver.TryResolve(
                    LocationLoader.Instance.CurrentLocation.LocationCode,
                    LocationLoader.Instance.ActiveBackgroundVariantKey,
                    LocationLoader.Instance.ActiveBackgroundSprite,
                    "D1-04",
                    out BackgroundSemanticRuntimeResolution resolution),
                Is.True);
            Assert.That(
                resolution.SceneLayout,
                Is.Not.Null,
                "The SERVICE7 scene must resolve its approved D1-04 fixed " +
                "layout.");

            Button[] worldCharacters = Object.FindObjectsByType<Button>(
                    FindObjectsInactive.Include,
                    FindObjectsSortMode.None)
                .Where(button =>
                    button.name.StartsWith("AmbientCharacter_"))
                .ToArray();
            AmbientCharacterHotspotOverlay semanticOverlay =
                Object.FindFirstObjectByType<
                    AmbientCharacterHotspotOverlay>(
                    FindObjectsInactive.Include);
            string activeStates = string.Join(
                ", ",
                worldCharacters.Select(button =>
                    $"{button.name}:{button.gameObject.activeSelf}"));
            string semanticDiagnostics = semanticOverlay != null
                ? string.Join(
                    " | ",
                    semanticOverlay.SemanticPlacementDiagnostics)
                : "<overlay missing>";
            Assert.That(
                worldCharacters.Count(button =>
                    button.gameObject.activeInHierarchy),
                Is.EqualTo(1),
                "Only the Deck 7 witness should be visible. " +
                $"activeSelf={activeStates}; " +
                $"semantic={semanticDiagnostics}");
            Button attendant = worldCharacters.Single(button =>
                button.name.StartsWith(
                    "AmbientCharacter_CREW_ATTENDANT_"));
            Assert.That(attendant.gameObject.activeInHierarchy, Is.True);
            Assert.That(
                worldCharacters.Any(button =>
                    button.name.StartsWith("AmbientCharacter_DANIEL_")),
                Is.False,
                "Daniel is only a tracked logical location in D1-04 and " +
                "must not be instantiated in the room.");
            Assert.That(
                worldCharacters.Any(button =>
                    button.name.StartsWith(
                        "AmbientCharacter_CREW_SECURITY_")),
                Is.False,
                "The physical CREW_STAIRS fallback bark must not replace " +
                "the scene-specific cabin attendant.");

            BackgroundSemanticCharacterSlotBinding assignment =
                resolution.SceneLayout.Assignments.Single(item =>
                    item.CharacterId == "CREW_ATTENDANT");
            Assert.That(assignment.SlotId, Is.EqualTo("landing_center"));
            BackgroundSemanticSlot slot =
                resolution.Profile.Slots.Single(item =>
                    item.Id == assignment.SlotId);
            RectTransform attendantRect =
                attendant.GetComponent<RectTransform>();
            Assert.That(
                attendantRect.anchorMin.x,
                Is.EqualTo(slot.Anchor.x).Within(0.0001f));
            Assert.That(
                attendantRect.anchorMin.y,
                Is.EqualTo(slot.Anchor.y).Within(0.0001f));

            yield return InvokeAndSettle(attendant);
            Assert.That(Dialogue.ActiveProductionSceneId, Is.EqualTo("D1-04"));
            Assert.That(State.DialogueCheckpoint, Is.Not.Null);
            Assert.That(State.DialogueCheckpoint.activeSceneId, Is.EqualTo("D1-04"));
            NarrativeLocationHUDController contextHud = RequireObject("Ingame")
                .GetComponent<NarrativeLocationHUDController>();
            Assert.That(contextHud, Is.Not.Null);
            Assert.That(contextHud.IsWarningVisible, Is.False);
            yield return new WaitForSecondsRealtime(0.4f);
            Assert.That(
                contextHud.transform.Find("Narrative Location Context"),
                Is.Null,
                "현재 위치는 좌상단 통합 HUD에만 표시해야 합니다.");
            Assert.That(contextHud.CurrentContext.NarrativeCode, Is.EqualTo("SERVICE7"));
            Assert.That(contextHud.CurrentContext.PhysicalLocationCode,
                Is.EqualTo("SERVICE7"));
            Assert.That(contextHud.CurrentContext.Kind,
                Is.EqualTo(NarrativeLocationKind.Physical));
            Assert.That(contextHud.CurrentContext.WarningMessage, Is.Empty);
            Assert.That(NarrativeLocationContextResolver.Resolve("UNKNOWN").Kind,
                Is.EqualTo(NarrativeLocationKind.Undocumented));
            Assert.That(DialogueOnlySceneAccess.Evaluate(
                    "D8-02",
                    new[] { "D8-01" },
                    FinalAccusationResolver.CompleteEndingId).IsAllowed,
                Is.False);
            AssertNoRuntimeErrors("공식 장면 배경 위치 매핑");
        }

    }
}
