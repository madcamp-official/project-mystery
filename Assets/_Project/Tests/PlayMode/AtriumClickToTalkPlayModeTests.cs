using System.Collections;
using System.Linq;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;
using UnityEngine.UI;
using Wake.Core;
using Wake.Exploration;
using Wake.Narrative;
using Wake.UI;

namespace Wake.Tests.PlayMode
{
    public sealed class AtriumClickToTalkPlayModeTests :
        UiBasicScenePlayModeFixture
    {
        private Button foundCharacterButton;

        [UnityTest]
        public IEnumerator D101_ClickingTwoSuspectsInSequenceDoesNotThrow()
        {
            yield return CompleteOpeningScene();
            State.RecordCompletedScene("P-02");
            State.RecordCompletedScene("P-03");

            yield return EnterAtriumFromMap();
            Assert.That(Dialogue.ActiveProductionSceneId, Is.EqualTo("D1-01"));

            yield return DrainDialogueUntilSuspended();
            Assert.That(Dialogue.IsBusy, Is.False);

            yield return FindAmbientCharacterButton("OWEN");
            yield return InvokeAndSettle(foundCharacterButton);
            Assert.That(Dialogue.ActiveProductionSceneId, Is.EqualTo("D1-01"));
            Assert.That(Dialogue.IsBusy, Is.True);

            yield return DrainDialogueUntilSuspended();
            Assert.That(Dialogue.IsBusy, Is.False);

            yield return FindAmbientCharacterButton("HELENA");
            yield return InvokeAndSettle(foundCharacterButton);
            Assert.That(Dialogue.ActiveProductionSceneId, Is.EqualTo("D1-01"));
            Assert.That(Dialogue.IsBusy, Is.True);
        }

        private IEnumerator EnterAtriumFromMap()
        {
            State.UnlockProductionScene("D1-01");
            Ui.ShowMap();
            yield return null;
            yield return null;

            Transform content = Canvas.Find(
                "Map/Rooms/Dynamic Location Viewport/" +
                "Dynamic Location Content");
            Assert.That(content, Is.Not.Null);
            Transform node = content.Find("Map Node ATRIUM");
            Button[] candidates = node != null
                ? node.GetComponents<Button>()
                : System.Array.Empty<Button>();
            Assert.That(candidates, Has.Length.EqualTo(1));

            yield return InvokeAndSettle(candidates[0]);
            Assert.That(Ui.ActivePanel, Is.EqualTo(UiPrimaryPanel.Ingame));
            yield return StartPreparedProductionSceneFromFocusCharacter("D1-01");
        }

        private IEnumerator FindAmbientCharacterButton(string characterId)
        {
            foundCharacterButton = null;
            float deadline = Time.realtimeSinceStartup + 2f;
            while (foundCharacterButton == null &&
                   Time.realtimeSinceStartup < deadline)
            {
                foundCharacterButton = Object.FindObjectsByType<Button>(
                        FindObjectsInactive.Exclude,
                        FindObjectsSortMode.None)
                    .FirstOrDefault(button =>
                        button.name.StartsWith(
                            $"AmbientCharacter_{characterId}"));
                if (foundCharacterButton == null)
                    yield return null;
            }

            Assert.That(
                foundCharacterButton,
                Is.Not.Null,
                $"{characterId} 클릭 대상을 찾지 못했습니다.");
        }

        private IEnumerator DrainDialogueUntilSuspended(
            int maximumSteps = 200)
        {
            Button next =
                RequireComponent<Button>("Ingame/Line Panel/Panel/Next");
            GameObject choices =
                RequireObject("Ingame/Line Panel/Select Btn");
            int steps = 0;
            while (Dialogue.IsBusy && !choices.activeInHierarchy)
            {
                Assert.That(
                    steps++,
                    Is.LessThan(maximumSteps),
                    "대사가 탐사 화면으로 돌아오기 전에 진행 상한을 초과했습니다.");
                yield return InvokeAndSettle(next);
            }
        }
    }
}
