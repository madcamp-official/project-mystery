using System.Collections;
using System.Linq;
using NUnit.Framework;
using TMPro;
using UnityEngine;
using UnityEngine.TestTools;
using UnityEngine.UI;
using Wake.Core;
using Wake.Evidence;
using Wake.Narrative;
using Wake.Puzzles;
using Wake.UI;

namespace Wake.Tests.PlayMode
{
    public sealed class ExitInspectionUIPlayModeTests :
        UiBasicScenePlayModeFixture
    {
        [UnityTest]
        public IEnumerator D201_OpensRestoresAndContinuesThroughVisibleUi()
        {
            State.StartNewGame();
            State.RecordCompletedScene("D1-07");
            Ui.ShowIngame();
            Assert.That(Dialogue.StartProductionScene("D2-01"), Is.True);
            yield return CompleteActiveProductionDialogue();

            ExitInspectionUIController controller = RequireObject("Ingame")
                .GetComponent<ExitInspectionUIController>();
            Assert.That(controller, Is.Not.Null);
            Assert.That(controller.IsOpen, Is.True);
            Assert.That(State.DialogueCheckpoint.pendingInteractionId,
                Is.EqualTo(ExitInspectionCatalog.SessionId));
            GameObject panel = RequireObject("Exit Inspection");
            Button[] inspections = panel.GetComponentsInChildren<Button>(true)
                .Where(button => button.name.StartsWith("Inspection "))
                .ToArray();
            Assert.That(inspections, Has.Length.EqualTo(3));
            Assert.That(inspections.Select(Label),
                Has.All.Contains("○ [검사 가능]"));
            Assert.That(inspections.Select(Label),
                Has.Some.Contains("C-03"));
            TMP_FontAsset koreanFont = StatusHUDController.RuntimeKoreanFont;
            Assert.That(koreanFont, Is.Not.Null);
            Assert.That(panel.GetComponentsInChildren<TMP_Text>(true)
                .All(text => text.font == koreanFont), Is.True);

            ExitInspectionCompletion blocked = controller.Submit();
            Assert.That(blocked.Completed, Is.False);
            Assert.That(controller.StatusMessage, Does.Contain("남은 검사"));
            AssertKoreanTextIsIntact(controller.StatusMessage);
            Assert.That(
                controller.Inspect(ExitInspectionCatalog.ServiceHatch),
                Is.EqualTo(ExitInspectionResult.Recorded));
            Assert.That(controller.UseHint(), Is.True);
            Assert.That(controller.Session.InspectionOrder,
                Is.EqualTo(new[] { ExitInspectionCatalog.ServiceHatch }));
            controller.Close();
            yield return null;

            Button reopen = RequireComponent<Button>("Exit Inspection Resume");
            Assert.That(reopen.gameObject.activeInHierarchy, Is.True);
            Assert.That(Label(reopen), Does.Contain("진행 저장됨 1/3"));

            yield return ReloadScenePreservingSave();
            yield return ContinueFromVisibleButton();
            controller = RequireObject("Ingame")
                .GetComponent<ExitInspectionUIController>();
            panel = RequireObject("Exit Inspection");
            inspections = panel.GetComponentsInChildren<Button>(true)
                .Where(button => button.name.StartsWith("Inspection "))
                .ToArray();
            reopen = RequireComponent<Button>("Exit Inspection Resume");
            Assert.That(controller.IsOpen, Is.True);
            Assert.That(reopen.gameObject.activeSelf, Is.False);
            Assert.That(controller.StatusMessage, Does.Contain("복원했습니다"));
            Assert.That(controller.Session.HintLevel, Is.EqualTo(1));
            Assert.That(inspections.Select(Label),
                Has.Some.Contains("✓ [선택됨 · 검사 완료]"));

            Assert.That(
                controller.Inspect(ExitInspectionCatalog.ExteriorLedge),
                Is.EqualTo(ExitInspectionResult.Recorded));
            Assert.That(
                controller.Inspect(ExitInspectionCatalog.AirDuct),
                Is.EqualTo(ExitInspectionResult.Recorded));
            ExitInspectionCompletion completed = controller.Submit();
            yield return null;
            yield return null;

            Assert.That(completed.Completed, Is.True);
            Assert.That(controller.IsOpen, Is.False);
            Assert.That(reopen.gameObject.activeSelf, Is.False);
            Assert.That(State.HasCompletedScene("D2-01"), Is.True);
            Assert.That(State.HasUnlockedDeduction(
                CanonicalDeductionCatalog.SceneDenial), Is.True);
            Assert.That(new[] { "C-03", "C-04", "C-05" }
                .All(EvidenceInventory.Instance.Contains), Is.True);
            Assert.That(Dialogue.ActiveProductionSceneId, Is.EqualTo("D2-02"));
            Assert.That(Dialogue.IsBusy, Is.True);
            Assert.That(State.CurrentLocationCode, Is.EqualTo("HORIZON"));
            Assert.That(State.CurrentTimeBlock, Is.EqualTo(TimeBlock.AM));
            AssertNoRuntimeErrors("D2-01 출구 검증 UI와 다음 장면 연결");
        }

        private static string Label(Button button) =>
            button.GetComponentInChildren<TMP_Text>().text;
    }
}
