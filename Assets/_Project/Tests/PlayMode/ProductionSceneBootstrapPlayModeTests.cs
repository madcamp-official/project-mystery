using System.Collections;
using System.Linq;
using NUnit.Framework;
using TMPro;
using UnityEngine;
using UnityEngine.TestTools;
using UnityEngine.UI;
using Wake.Core;
using Wake.Exploration;
using Wake.Narrative;

namespace Wake.Tests.PlayMode
{
    public sealed class ProductionSceneBootstrapPlayModeTests :
        UiBasicScenePlayModeFixture
    {
        private const string ProductionAssetName =
            "Under_the_Horizon_Dialogue_KR";
        private const string OpeningSceneId = "P-01";
        private const string OpeningLineId = "p_01_01";
        private const string OpeningText =
            "MV Elysium은 항구의 유리 지붕 너머에서 지나치게 새것처럼 빛나고 있었다.";

        [UnityTest]
        public IEnumerator SceneDatabase_UsesCompleteProductionCsv()
        {
            Assert.That(
                Database.SourceAsset,
                Is.Not.Null,
                "DialogueDatabase에 CSV TextAsset이 연결되어야 합니다.");
            Assert.That(
                Database.SourceAssetName,
                Is.EqualTo(ProductionAssetName),
                "샘플 CSV가 아니라 원본 프로덕션 CSV를 사용해야 합니다.");
            Assert.That(Database.LoadErrors, Is.Empty);
            Assert.That(Database.RecordCount, Is.EqualTo(1063));
            Assert.That(Database.SceneCount, Is.EqualTo(41));

            DialogueRecord[] records = Database.Records.Values.ToArray();
            Assert.That(
                records
                    .Select(record => record.StableLineId)
                    .Distinct()
                    .Count(),
                Is.EqualTo(1063),
                "stable line ID 200개가 모두 고유해야 합니다.");
            Assert.That(
                records.Count(record => record.Speaker == "PLAYER_CHOICE"),
                Is.EqualTo(90),
                "원본 CSV의 선택지 행 30개가 보존되어야 합니다.");
            Assert.That(
                records
                    .Where(record => record.Speaker == "PLAYER_CHOICE")
                    .Select(record => record.ChoiceId)
                    .Distinct()
                    .Count(),
                Is.EqualTo(90),
                "선택지 ID 30개가 모두 보존되어야 합니다.");

            foreach (IGrouping<string, DialogueRecord> scene in
                     records.GroupBy(record => record.SceneId))
            {
                int[] actualOrders = scene
                    .OrderBy(record => record.Order)
                    .Select(record => record.Order)
                    .ToArray();
                int[] expectedOrders =
                    Enumerable.Range(1, actualOrders.Length).ToArray();

                Assert.That(
                    actualOrders,
                    Is.EqualTo(expectedOrders),
                    $"{scene.Key} 장면의 order는 1부터 연속이어야 합니다.");
            }

            Assert.That(
                Database.TryGetRecord(
                    OpeningLineId,
                    out DialogueRecord opening),
                Is.True);
            Assert.That(opening.SceneId, Is.EqualTo(OpeningSceneId));
            Assert.That(opening.Order, Is.EqualTo(1));
            Assert.That(opening.Speaker, Is.EqualTo("NARRATION"));
            Assert.That(opening.TextKo, Is.EqualTo(OpeningText));
            AssertKoreanTextIsIntact(opening.TextKo);
            AssertNoRuntimeErrors("프로덕션 CSV 계약 확인");
            yield return null;
        }

        [UnityTest]
        public IEnumerator StartButton_EntersPortAndRendersOpeningLine()
        {
            Assert.That(
                RequireObject("StartScene").activeSelf,
                Is.True,
                "초기 화면은 StartScene이어야 합니다.");
            Assert.That(
                RequireObject("Ingame").activeSelf,
                Is.False,
                "게임 시작 전에는 Ingame 패널이 숨겨져야 합니다.");

            yield return StartNewGameFromVisibleButton();

            Assert.That(RequireObject("StartScene").activeSelf, Is.False);
            Assert.That(RequireObject("Ingame").activeSelf, Is.True);
            Assert.That(Dialogue.IsBusy, Is.True);
            Assert.That(
                Dialogue.ActiveProductionSceneId,
                Is.EqualTo(OpeningSceneId));
            Assert.That(State.CurrentLocationCode, Is.EqualTo("PORT"));
            Assert.That(State.Day, Is.EqualTo(1));
            Assert.That(State.CurrentTimeBlock, Is.EqualTo(TimeBlock.AM));
            Assert.That(LocationLoader.Instance, Is.Not.Null);
            Assert.That(
                LocationLoader.Instance.CurrentLocation.LocationCode,
                Is.EqualTo("PORT"));
            Assert.That(
                LocationLoader.Instance.CurrentLocation.BackgroundSprite,
                Is.Not.Null);
            BackgroundCoverPresenter background =
                Object.FindFirstObjectByType<BackgroundCoverPresenter>(
                    FindObjectsInactive.Include);
            Assert.That(background, Is.Not.Null);
            Assert.That(background.gameObject.activeInHierarchy, Is.True);
            Assert.That(
                background.Sprite,
                Is.SameAs(
                    LocationLoader.Instance.CurrentLocation.BackgroundSprite));

            ProductionDialogueCheckpoint checkpoint =
                State.DialogueCheckpoint;
            Assert.That(checkpoint, Is.Not.Null);
            Assert.That(
                checkpoint.activeSceneId,
                Is.EqualTo(OpeningSceneId));
            Assert.That(checkpoint.lineIndex, Is.EqualTo(0));
            Assert.That(checkpoint.awaitingChoice, Is.False);

            GameObject linePanel =
                RequireObject("Ingame/Line Panel");
            Assert.That(linePanel.activeInHierarchy, Is.True);

            TMP_Text line =
                RequireText("Ingame/Line Panel/Panel/line");
            Assert.That(line.text, Is.EqualTo(OpeningText));
            AssertKoreanTextIsIntact(line.text);
            AssertNoRuntimeErrors("새 게임 P-01 첫 대사 표시");
        }

        [UnityTest]
        public IEnumerator OpeningDialogue_ShowsTwoChoicesWithoutGuessingEffect()
        {
            yield return StartNewGameFromVisibleButton();

            yield return AdvanceToVisibleChoices();

            GameObject choices =
                RequireObject("Ingame/Line Panel/Select Btn");
            Button next =
                RequireComponent<Button>("Ingame/Line Panel/Panel/Next");
            Assert.That(choices.activeInHierarchy, Is.True);
            Assert.That(next.gameObject.activeSelf, Is.False);
            Assert.That(State.DialogueCheckpoint, Is.Not.Null);
            Assert.That(
                State.DialogueCheckpoint.awaitingChoice,
                Is.True);
            Assert.That(
                State.DialogueCheckpoint.lineIndex,
                Is.EqualTo(19));

            Button serious = RequireComponent<Button>(
                "Ingame/Line Panel/Select Btn/Choice");
            Button joke = RequireComponent<Button>(
                "Ingame/Line Panel/Select Btn/Choice (1)");
            Button unusedThird = RequireComponent<Button>(
                "Ingame/Line Panel/Select Btn/Choice (2)");
            Button unusedFourth = RequireComponent<Button>(
                "Ingame/Line Panel/Select Btn/Choice (3)");

            Assert.That(serious.gameObject.activeSelf, Is.True);
            Assert.That(joke.gameObject.activeSelf, Is.True);
            Assert.That(unusedThird.gameObject.activeSelf, Is.False);
            Assert.That(unusedFourth.gameObject.activeSelf, Is.False);
            UnityEngine.Canvas.ForceUpdateCanvases();
            AssertInsideSafeArea(
                choices.GetComponent<RectTransform>(),
                "P-01 choice container");
            AssertInsideSafeArea(
                serious.GetComponent<RectTransform>(),
                "P-01_C1");
            AssertInsideSafeArea(
                joke.GetComponent<RectTransform>(),
                "P-01_C2");
            Assert.That(
                serious.GetComponentInChildren<TMP_Text>().text,
                Is.EqualTo("그의 경고를 진지하게 듣기"));
            Assert.That(
                joke.GetComponentInChildren<TMP_Text>().text,
                Is.EqualTo("농담으로 넘기기"));

            int trustBefore = State.GetTrust("DANIEL");
            yield return InvokeAndSettle(serious);

            Assert.That(
                State.GetTrust("DANIEL"),
                Is.EqualTo(trustBefore + 1),
                "공식 P-01_C1의 Daniel 신뢰 +1 효과가 적용되어야 합니다.");
            RawImage portrait = RequireComponent<RawImage>(
                "Ingame/Line Panel/Speaker Portrait");
            Assert.That(portrait.gameObject.activeInHierarchy, Is.True);
            Assert.That(portrait.texture, Is.Not.Null);
            Assert.That(State.HasCompletedScene(OpeningSceneId), Is.False);
            Assert.That(Dialogue.IsBusy, Is.True);
            Assert.That(
                RequireObject("Ingame/Line Panel").activeSelf,
                Is.True);
            AssertNoRuntimeErrors("P-01 선택지 표시 및 선택");
        }

        [UnityTest]
        public IEnumerator ContinueButton_RestoresProductionLineFromCheckpoint()
        {
            yield return StartNewGameFromVisibleButton();

            Button next =
                RequireComponent<Button>("Ingame/Line Panel/Panel/Next");
            yield return InvokeAndSettle(next);
            yield return InvokeAndSettle(next);

            const string restoredText = "[조사: 구겨진 초대장]";
            Assert.That(
                RequireText("Ingame/Line Panel/Panel/line").text,
                Is.EqualTo(restoredText));
            Assert.That(State.DialogueCheckpoint, Is.Not.Null);
            Assert.That(State.DialogueCheckpoint.lineIndex, Is.EqualTo(2));

            yield return ReloadScenePreservingSave();

            Assert.That(GameStateManager.HasSaveData, Is.True);
            Assert.That(
                RequireObject("StartScene/Continue Btn").activeSelf,
                Is.False);
            yield return ContinueFromVisibleButton();

            Assert.That(Dialogue.IsBusy, Is.True);
            Assert.That(
                Dialogue.ActiveProductionSceneId,
                Is.EqualTo(OpeningSceneId));
            Assert.That(State.CurrentLocationCode, Is.EqualTo("PORT"));
            Assert.That(State.DialogueCheckpoint, Is.Not.Null);
            Assert.That(State.DialogueCheckpoint.lineIndex, Is.EqualTo(2));

            string actual =
                RequireText("Ingame/Line Panel/Panel/line").text;
            Assert.That(actual, Is.EqualTo(restoredText));
            AssertKoreanTextIsIntact(actual);
            AssertNoRuntimeErrors("체크포인트 이어하기");
        }
    }
}
