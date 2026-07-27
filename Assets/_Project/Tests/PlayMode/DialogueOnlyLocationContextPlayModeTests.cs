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
            yield return null;
            yield return null;
            MapController map = Object.FindObjectsByType<MapController>(
                FindObjectsInactive.Include,
                FindObjectsSortMode.None).Single();
            Assert.That(map.CurrentViewModel.DialogueOnlyEntries, Is.Empty);
            Assert.That(map.CurrentViewModel.UnresolvedScenes, Is.Empty);
            ProductionMapEntry crewStairs = map.CurrentViewModel.Entries
                .Single(entry => entry.Spec.Code == "CREW_STAIRS");
            Assert.That(crewStairs.SceneId, Is.EqualTo("D1-04"));
            Assert.That(crewStairs.Status,
                Is.EqualTo(ProductionMapEntryStatus.Available));
            Assert.That(crewStairs.Location.BackgroundSprite, Is.Not.Null);

            SceneTravelResult travel = map.TryTravelToScene("D1-04");
            yield return null;
            yield return null;

            Assert.That(travel.IsAllowed, Is.True);
            Assert.That(State.CurrentLocationCode, Is.EqualTo("CREW_STAIRS"));
            Assert.That(LocationLoader.Instance.CurrentLocation.LocationCode,
                Is.EqualTo("CREW_STAIRS"));
            Assert.That(LocationLoader.Instance.CurrentLocation.BackgroundSprite,
                Is.Not.Null);
            Assert.That(Dialogue.ActiveProductionSceneId, Is.EqualTo("D1-04"));
            Assert.That(State.DialogueCheckpoint, Is.Not.Null);
            Assert.That(State.DialogueCheckpoint.activeSceneId, Is.EqualTo("D1-04"));
            NarrativeLocationHUDController contextHud = RequireObject("Ingame")
                .GetComponent<NarrativeLocationHUDController>();
            Assert.That(contextHud, Is.Not.Null);
            Assert.That(contextHud.IsWarningVisible, Is.False);
            Assert.That(contextHud.CurrentContext.NarrativeCode, Is.EqualTo("SERVICE7"));
            Assert.That(contextHud.CurrentContext.PhysicalLocationCode,
                Is.EqualTo("CREW_STAIRS"));
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
