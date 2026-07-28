using System.Collections;
using System.Linq;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;
using UnityEngine.UI;
using Wake.Evidence;
using Wake.Puzzles;
using Wake.UI;

namespace Wake.Tests.PlayMode
{
    public sealed class CameraBlindSpotUIPlayModeTests :
        UiBasicScenePlayModeFixture
    {
        [UnityTest]
        public IEnumerator D204_RequiresVideoAndFacilityLogsBeforeCeilingUnlock()
        {
            State.StartNewGame();
            State.RecordCompletedScene("D2-01");
            State.UnlockProductionScene("D2-04");
            Ui.ShowIngame();

            Assert.That(Dialogue.StartProductionScene("D2-04"), Is.True);
            yield return CompleteActiveProductionDialogue();

            CameraBlindSpotUIController controller = RequireObject("Ingame")
                .GetComponent<CameraBlindSpotUIController>();
            Assert.That(controller, Is.Not.Null);
            Assert.That(controller.IsOpen, Is.True);
            Assert.That(
                RequireObject("Camera Blind Spot Puzzle")
                    .transform
                    .Find("CCTV 4 Channels")
                    .childCount,
                Is.EqualTo(4));
            Assert.That(
                State.DialogueCheckpoint.pendingInteractionId,
                Is.EqualTo(CameraBlindSpotSession.SessionId));

            controller.TogglePlayback();
            controller.AttemptVideoOnlyConclusion();
            Assert.That(
                controller.StatusMessage,
                Does.Contain("핵심 단서는 해금되지 않았습니다"));
            Assert.That(
                EvidenceInventory.Instance.Contains(
                    CameraBlindSpotSession.EvidenceId),
                Is.False);
            Assert.That(
                State.HasFlag(CameraBlindSpotSession.CeilingInvestigationFlag),
                Is.False);
            Assert.That(State.HasCompletedScene("D2-04"), Is.False);

            Assert.That(controller.OpenFacilityLogs(), Is.True);
            controller.SelectLog((int)FacilityLogKind.Door);
            controller.SelectLog((int)FacilityLogKind.Detector);
            Assert.That(controller.SelectDetectorError(), Is.True);
            CameraBlindSpotCompletion completed =
                controller.ConfirmErrorLocation();
            yield return null;

            Assert.That(completed.Completed, Is.True);
            Assert.That(
                EvidenceInventory.Instance.Contains(
                    CameraBlindSpotSession.EvidenceId),
                Is.True);
            Assert.That(
                State.HasFlag(CameraBlindSpotSession.CeilingInvestigationFlag),
                Is.True);
            Assert.That(State.HasCompletedScene("D2-04"), Is.True);
            Assert.That(State.DialogueCheckpoint, Is.Null);
            Assert.That(
                RequireObject("Camera Blind Spot Puzzle")
                    .GetComponentsInChildren<Button>(true)
                    .Single(button => button.name == "Continue To Ceiling")
                    .gameObject.activeSelf,
                Is.True);
            AssertNoRuntimeErrors("D2-04 CCTV와 설비 로그 대조 퍼즐");
        }
    }
}
