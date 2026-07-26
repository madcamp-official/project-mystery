using System.Collections;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;
using UnityEngine.UI;
using Wake.UI;

namespace Wake.Tests.PlayMode
{
    public sealed class UiRuntimeBootstrapPlayModeTests :
        UiBasicScenePlayModeFixture
    {
        [UnityTest]
        public IEnumerator Reinitialization_KeepsOneControllerAndListener()
        {
            Assert.That(Ui.IsInitialized, Is.True);
            Assert.That(Ui.ActivePanel, Is.EqualTo(UiPrimaryPanel.Start));
            Assert.That(Ui.RuntimeModalControllerCount, Is.EqualTo(7));

            Assert.That(Ui.EnsureInitialized(), Is.True);
            Assert.That(Ui.EnsureInitialized(), Is.True);
            Assert.That(Ui.RuntimeModalControllerCount, Is.EqualTo(7));

            GameObject ingame = RequireObject("Ingame");
            Assert.That(
                ingame.GetComponents<ProductionPuzzleUIController>(),
                Has.Length.EqualTo(1));
            Assert.That(
                ingame.GetComponents<FinalAccusationUIController>(),
                Has.Length.EqualTo(1));
            Assert.That(
                ingame.GetComponents<MarcusInterrogationUIController>(),
                Has.Length.EqualTo(1));
            Assert.That(
                ingame.GetComponents<TimelinePuzzleUIController>(),
                Has.Length.EqualTo(1));
            Assert.That(
                ingame.GetComponents<OrpheusAudioRestorationUIController>(),
                Has.Length.EqualTo(1));
            Assert.That(
                ingame.GetComponents<ProductionEndingUIController>(),
                Has.Length.EqualTo(1));
            Assert.That(
                RequireObject("Evidence")
                    .GetComponents<EvidenceTheoryBoardController>(),
                Has.Length.EqualTo(1));

            int newGameFeedbackCount = 0;
            State.FeedbackRequested += _ => newGameFeedbackCount++;
            yield return StartNewGameFromVisibleButton();
            Assert.That(newGameFeedbackCount, Is.EqualTo(1));

            var duplicateHost = new GameObject("Duplicate UIManager");
            UIManager duplicate = duplicateHost.AddComponent<UIManager>();
            Assert.That(duplicate.enabled, Is.False);
            Assert.That(UIManager.Instance, Is.SameAs(Ui));
            Object.Destroy(duplicateHost);
            yield return null;
            AssertNoRuntimeErrors("UI 런타임 중복 초기화");
        }

        [UnityTest]
        public IEnumerator PrimaryButtons_RoundTripWithoutOrphanModal()
        {
            yield return StartNewGameFromVisibleButton();
            AssertOnlyPanel(UiPrimaryPanel.Ingame);

            yield return InvokeAndSettle(
                RequireComponent<Button>("Ingame/Map Btn"));
            AssertOnlyPanel(UiPrimaryPanel.Map);
            yield return InvokeAndSettle(
                RequireComponent<Button>("Map/Back Btn"));
            AssertOnlyPanel(UiPrimaryPanel.Ingame);

            yield return InvokeAndSettle(
                RequireComponent<Button>("Ingame/Evidence Btn"));
            AssertOnlyPanel(UiPrimaryPanel.Evidence);
            Assert.That(Ui.OpenRuntimeModalCount, Is.EqualTo(1));

            yield return InvokeAndSettle(
                RequireComponent<Button>("Evidence/Back Btn"));
            AssertOnlyPanel(UiPrimaryPanel.Ingame);
            Assert.That(Ui.OpenRuntimeModalCount, Is.Zero);
            Assert.That(Ui.IsSettingsOpen, Is.False);
            AssertNoRuntimeErrors("주 화면 왕복");
        }

        [UnityTest]
        public IEnumerator Settings_ClosesOtherModalAndOwnsInput()
        {
            yield return StartNewGameFromVisibleButton();
            Ui.ShowEvidence();
            Assert.That(Ui.OpenRuntimeModalCount, Is.EqualTo(1));

            Ui.OpenSettings();
            yield return null;
            Assert.That(Ui.IsSettingsOpen, Is.True);
            Assert.That(Ui.OpenRuntimeModalCount, Is.Zero);
            Assert.That(
                RequireObject("Settings Popup").transform.GetSiblingIndex(),
                Is.EqualTo(Canvas.childCount - 1));

            CanvasGroup evidenceInput =
                RequireObject("Evidence").GetComponent<CanvasGroup>();
            CanvasGroup hudInput =
                RequireObject("Status HUD").GetComponent<CanvasGroup>();
            Assert.That(evidenceInput.interactable, Is.False);
            Assert.That(evidenceInput.blocksRaycasts, Is.False);
            Assert.That(hudInput.interactable, Is.False);
            Assert.That(hudInput.blocksRaycasts, Is.False);

            yield return InvokeAndSettle(
                RequireComponent<Button>("Settings Popup/Close"));
            Assert.That(Ui.IsSettingsOpen, Is.False);
            Assert.That(evidenceInput.interactable, Is.True);
            Assert.That(evidenceInput.blocksRaycasts, Is.True);
            Assert.That(hudInput.interactable, Is.True);
            Assert.That(hudInput.blocksRaycasts, Is.True);
            AssertNoRuntimeErrors("설정 모달 입력 복구");
        }

        private void AssertOnlyPanel(UiPrimaryPanel expected)
        {
            Assert.That(Ui.ActivePanel, Is.EqualTo(expected));
            Assert.That(
                RequireObject("StartScene").activeSelf,
                Is.EqualTo(expected == UiPrimaryPanel.Start));
            Assert.That(
                RequireObject("Ingame").activeSelf,
                Is.EqualTo(expected == UiPrimaryPanel.Ingame));
            Assert.That(
                RequireObject("Map").activeSelf,
                Is.EqualTo(expected == UiPrimaryPanel.Map));
            Assert.That(
                RequireObject("Evidence").activeSelf,
                Is.EqualTo(expected == UiPrimaryPanel.Evidence));
        }
    }
}
