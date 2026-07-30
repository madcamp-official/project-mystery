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
            yield return WaitForUiTransition();
            Assert.That(Dialogue.StartProductionScene("D2-01"), Is.True);
            yield return CompleteActiveProductionDialogue();

            ExitInspectionUIController controller = RequireObject("Ingame")
                .GetComponent<ExitInspectionUIController>();
            Assert.That(controller, Is.Not.Null);
            Assert.That(
                controller.IsOpen,
                Is.True,
                $"restore failed; panel={Ui.ActivePanel}, " +
                $"checkpoint={State.DialogueCheckpoint?.activeSceneId ?? "<none>"}/" +
                $"{State.DialogueCheckpoint?.pendingInteractionId ?? "<none>"}, " +
                $"flow={Flow.HasActiveSession}, dialogue={Dialogue.IsBusy}");
            Assert.That(State.DialogueCheckpoint.pendingInteractionId,
                Is.EqualTo(ExitInspectionCatalog.SessionId));
            GameObject panel = RequireObject("Exit Inspection");
            Button[] inspections = panel.GetComponentsInChildren<Button>(true)
                .Where(button => button.name.StartsWith("Inspection "))
                .ToArray();
            Assert.That(inspections, Has.Length.EqualTo(3));
            Assert.That(inspections.Select(Label),
                Has.All.Contains("조사하기"));
            Assert.That(
                inspections.Select(Label).All(value => !value.Contains("C-")),
                Is.True);
            TMP_Text[] appliedTexts = panel
                .GetComponentsInChildren<TMP_Text>(true)
                .ToArray();
            Assert.That(appliedTexts, Is.Not.Empty);
            Assert.That(appliedTexts.Select(text => text.font),
                Has.All.Not.Null);
            Assert.That(appliedTexts.Min(text => text.fontSizeMin),
                Is.GreaterThanOrEqualTo(18f));

            foreach (ExitInspectionDefinition definition
                     in ExitInspectionCatalog.All)
            {
                Button route = inspections.Single(button =>
                    button.name == $"Inspection {definition.Id}");
                yield return CompleteVisibleRouteInvestigation(route);
                Assert.That(
                    controller.Session.HasInspected(definition.Id),
                    Is.True,
                    $"{definition.Title}의 두 관찰 지점이 세션에 반영되어야 합니다.");
            }

            ExitInspectionCompletion blocked = controller.Submit();
            Assert.That(blocked.Completed, Is.False);
            Assert.That(
                blocked.Failure,
                Is.EqualTo(ExitInspectionCompletionFailure.MissingVerdicts));
            Assert.That(controller.StatusMessage, Does.Contain("판정"));
            AssertKoreanTextIsIntact(controller.StatusMessage);

            ExitInspectionAction firstVerdict = controller.SetVerdict(
                ExitInspectionCatalog.ServiceHatch,
                ExitRouteVerdict.Unused);
            Assert.That(firstVerdict.Accepted, Is.True);
            Assert.That(controller.UseHint(), Is.True);
            Assert.That(
                controller.Session.GetVerdict(
                    ExitInspectionCatalog.ServiceHatch),
                Is.EqualTo(ExitRouteVerdict.Unused));
            controller.Close();
            yield return WaitForUiTransition();

            Button reopen = RequireComponent<Button>("Exit Inspection Resume");
            Assert.That(reopen.gameObject.activeInHierarchy, Is.True);
            Assert.That(Label(reopen), Does.Contain("관찰 3/3"));

            yield return ReloadScenePreservingSave();
            yield return ContinueFromVisibleButton();
            controller = RequireObject("Ingame")
                .GetComponent<ExitInspectionUIController>();
            panel = RequireObject("Exit Inspection");
            inspections = panel.GetComponentsInChildren<Button>(true)
                .Where(button => button.name.StartsWith("Inspection "))
                .ToArray();
            reopen = RequireComponent<Button>("Exit Inspection Resume");
            Assert.That(
                controller.IsOpen,
                Is.True,
                $"restore failed; panel={Ui.ActivePanel}, " +
                $"checkpoint={State.DialogueCheckpoint?.activeSceneId ?? "<none>"}/" +
                $"{State.DialogueCheckpoint?.pendingInteractionId ?? "<none>"}, " +
                $"flow={Flow.HasActiveSession}, dialogue={Dialogue.IsBusy}");
            Assert.That(reopen.gameObject.activeSelf, Is.False);
            Assert.That(controller.StatusMessage, Does.Contain("복원했습니다"));
            Assert.That(controller.Session.HintLevel, Is.EqualTo(1));
            Assert.That(
                controller.Session.GetVerdict(
                    ExitInspectionCatalog.ServiceHatch),
                Is.EqualTo(ExitRouteVerdict.Unused));

            Assert.That(controller.SetVerdict(
                ExitInspectionCatalog.ExteriorLedge,
                ExitRouteVerdict.Used).Accepted, Is.True);
            Assert.That(controller.SetVerdict(
                ExitInspectionCatalog.AirDuct,
                ExitRouteVerdict.Unused).Accepted, Is.True);

            Assert.That(controller.SelectTheory(
                ExitInspectionTheory.NoLiveThirdParty).Accepted, Is.True);
            ExitInspectionCompletion wrongRoute = controller.Submit();
            Assert.That(wrongRoute.Completed, Is.False);
            Assert.That(
                wrongRoute.Failure,
                Is.EqualTo(
                    ExitInspectionCompletionFailure.IncorrectVerdicts));
            Assert.That(
                RequireObject("Exit Inspection/Compare Stage")
                    .activeInHierarchy,
                Is.True,
                "잘못된 경로 판정은 비교 화면으로 돌아가 수정할 수 있어야 합니다.");
            Assert.That(controller.SetVerdict(
                ExitInspectionCatalog.ExteriorLedge,
                ExitRouteVerdict.Unused).Accepted, Is.True);

            Assert.That(controller.SelectTheory(
                ExitInspectionTheory.PerfectCleanup).Accepted, Is.True);
            ExitInspectionCompletion contradicted = controller.Submit();
            Assert.That(contradicted.Completed, Is.False);
            Assert.That(
                contradicted.Failure,
                Is.EqualTo(
                    ExitInspectionCompletionFailure.PerfectCleanupContradicted));
            Assert.That(controller.IsOpen, Is.True);
            Assert.That(State.HasCompletedScene("D2-01"), Is.False);
            Assert.That(State.IsProductionSceneUnlocked("D2-04"), Is.False);
            Assert.That(State.HasUnlockedDeduction(
                CanonicalDeductionCatalog.SceneDenial), Is.False);

            Assert.That(controller.SelectTheory(
                ExitInspectionTheory.NoLiveThirdParty).Accepted, Is.True);
            ExitInspectionCompletion completed = controller.Submit();
            yield return WaitForUiTransition();

            Assert.That(completed.Completed, Is.True);
            Assert.That(controller.IsOpen, Is.False);
            Assert.That(reopen.gameObject.activeSelf, Is.False);
            Assert.That(State.HasCompletedScene("D2-01"), Is.True);
            Assert.That(State.HasUnlockedDeduction(
                CanonicalDeductionCatalog.SceneDenial), Is.True);
            Assert.That(State.IsProductionSceneUnlocked("D2-04"), Is.True);
            Assert.That(new[] { "C-03", "C-04", "C-05" }
                .All(EvidenceInventory.Instance.Contains), Is.True);
            MapController map = Object.FindFirstObjectByType<MapController>();
            Assert.That(map, Is.Not.Null);
            Assert.That(
                map.LastTravelResult.IsAllowed,
                Is.True,
                $"D2-02 자동 이동 실패: " +
                $"{map.LastTravelResult.DenialReason} / " +
                $"{map.LastTravelResult.Detail}");
            yield return StartPreparedProductionSceneFromFocusCharacter("D2-02");
            Assert.That(Dialogue.ActiveProductionSceneId, Is.EqualTo("D2-02"));
            Assert.That(Dialogue.IsBusy, Is.True);
            Assert.That(State.CurrentLocationCode, Is.EqualTo("HORIZON"));
            Assert.That(State.CurrentTimeBlock, Is.EqualTo(TimeBlock.AM));
            AssertNoRuntimeErrors("D2-01 출구 검증 UI와 다음 장면 연결");
        }

        private static string Label(Button button) =>
            button.GetComponentInChildren<TMP_Text>().text;

        private IEnumerator CompleteVisibleRouteInvestigation(Button route)
        {
            yield return InvokeAndSettle(route);

            InvestigationScreenController investigation =
                Object.FindFirstObjectByType<InvestigationScreenController>(
                    FindObjectsInactive.Include);
            Assert.That(
                investigation,
                Is.Not.Null,
                "세부 조사 화면 컨트롤러가 존재해야 합니다.");
            Assert.That(
                investigation.IsOpen,
                Is.True,
                "출구 버튼을 누르면 세부 조사 화면이 열려야 합니다. " +
                $"상태: {RequireObject("Ingame").GetComponent<ExitInspectionUIController>().StatusMessage}");
            GameObject screen = Object.FindObjectsByType<Transform>(
                    FindObjectsInactive.Include,
                    FindObjectsSortMode.None)
                .First(item =>
                    item.name == "Investigation Screen" &&
                    item.gameObject.activeInHierarchy)
                .gameObject;

            Button[] points = screen.GetComponentsInChildren<Button>(false)
                .Where(button =>
                    button.name.StartsWith("Inspection Point ") &&
                    button.gameObject.activeInHierarchy &&
                    button.interactable)
                .ToArray();
            Assert.That(points, Has.Length.EqualTo(2));
            foreach (Button point in points)
            {
                yield return InvokeAndSettle(point);
            }

            Button action = screen.transform
                .Find("Primary Action")
                .GetComponent<Button>();
            Assert.That(action.interactable, Is.True);
            yield return InvokeAndSettle(action);
            if (screen.activeInHierarchy)
            {
                Assert.That(action.interactable, Is.True);
                yield return InvokeAndSettle(action);
            }

            Assert.That(
                screen.activeInHierarchy,
                Is.False,
                "조사 기록을 남긴 뒤 출구 비교 화면으로 돌아와야 합니다.");
        }
    }
}
