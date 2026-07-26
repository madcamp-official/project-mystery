using System.Collections;
using System.Linq;
using NUnit.Framework;
using TMPro;
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
        public IEnumerator UnresolvedScene_PreservesBackgroundAndShowsWarning()
        {
            yield return CompleteOpeningScene();
            State.RecordCompletedScene("D1-03");
            Assert.That(State.CurrentLocationCode, Is.EqualTo("PORT"));

            Ui.ShowMap();
            yield return null;
            yield return null;
            MapController map = Object.FindObjectsByType<MapController>(
                FindObjectsInactive.Include,
                FindObjectsSortMode.None).Single();
            Assert.That(map.CurrentViewModel.DialogueOnlyEntries.Count,
                Is.EqualTo(10));
            Assert.That(map.CurrentViewModel.DialogueOnlyEntries
                .Select(entry => entry.Scene.NarrativeLocationCode).Distinct().Count(),
                Is.EqualTo(8));

            GameObject content = RequireObject(
                "Map/Rooms/Dynamic Location Viewport/" +
                "Dynamic Location Content");
            Button entryButton = content
                .GetComponentsInChildren<Button>(true)
                .Single(button =>
                    button.GetComponentInChildren<TMP_Text>()?.text
                        .Contains("D1-04") == true);
            Assert.That(entryButton.interactable, Is.True);
            Assert.That(
                entryButton.GetComponentInChildren<TMP_Text>().text,
                Does.Contain("배경 유지"));
            Assert.That(RequireText(
                    "Map/Rooms/Unresolved Scene Notice").text,
                Does.Contain("D1-04(SERVICE7)"));

            yield return InvokeAndSettle(entryButton);

            Assert.That(State.CurrentLocationCode, Is.EqualTo("PORT"));
            Assert.That(LocationLoader.Instance.CurrentLocation.LocationCode,
                Is.EqualTo("PORT"));
            Assert.That(Dialogue.ActiveProductionSceneId, Is.EqualTo("D1-04"));
            Assert.That(State.DialogueCheckpoint, Is.Not.Null);
            Assert.That(State.DialogueCheckpoint.activeSceneId, Is.EqualTo("D1-04"));
            NarrativeLocationHUDController contextHud = RequireObject("Ingame")
                .GetComponent<NarrativeLocationHUDController>();
            Assert.That(contextHud, Is.Not.Null);
            Assert.That(contextHud.IsWarningVisible, Is.True);
            Assert.That(contextHud.CurrentContext.NarrativeCode, Is.EqualTo("SERVICE7"));
            string contextLabel = RequireText("Ingame/Narrative Location Context/Label").text;
            Assert.That(contextLabel, Does.Contain("배경 미확정"));
            Assert.That(contextLabel, Does.Contain("현재 배경 유지"));
            Assert.That(NarrativeLocationContextResolver.Resolve("UNKNOWN").Kind,
                Is.EqualTo(NarrativeLocationKind.Undocumented));
            Assert.That(DialogueOnlySceneAccess.Evaluate(
                    "D8-02",
                    new[] { "D8-01" },
                    FinalAccusationResolver.CompleteEndingId).IsAllowed,
                Is.True);
            AssertNoRuntimeErrors("대화 전용 장소 컨텍스트");
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
    }
}
