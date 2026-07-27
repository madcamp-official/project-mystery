using System.Collections;
using System.Linq;
using NUnit.Framework;
using TMPro;
using UnityEngine;
using UnityEngine.TestTools;
using UnityEngine.UI;
using Wake.Core;
using Wake.UI;

namespace Wake.Tests.PlayMode
{
    public sealed class DialogueTypewriterFlowPlayModeTests :
        UiBasicScenePlayModeFixture
    {
        private const string LinePath = "Ingame/Line Panel/Panel/line";
        private const string NextPath = "Ingame/Line Panel/Panel/Next";
        private const string ChoicesPath = "Ingame/Line Panel/Select Btn";
        private const string LongAmbientLine =
            "다니엘은 주변을 살핀 뒤 목소리를 낮췄다. " +
            "이 문장은 일시정지 상태에서도 충분히 오래 표시되어 " +
            "타이프라이터 진행을 안정적으로 확인할 수 있다.";

        [UnityTest]
        public IEnumerator ProductionLine_StartsHiddenAndKeepsFullText()
        {
            yield return StartNewGameFromVisibleButton();

            DialogueTypewriter typewriter = RequireTypewriter();
            TMP_Text line = RequireText(LinePath);

            Assert.That(line.text, Is.Not.Empty);
            Assert.That(typewriter.TotalCharacters, Is.GreaterThan(0));
            Assert.That(typewriter.VisibleCharacters,
                Is.LessThan(typewriter.TotalCharacters));
            Assert.That(typewriter.IsRevealing, Is.True);
            Assert.That(
                line.maxVisibleCharacters,
                Is.EqualTo(typewriter.VisibleCharacters));
            AssertNoRuntimeErrors("Production 타이프라이터 초기 상태");
        }

        [UnityTest]
        public IEnumerator UnscaledTime_AdvancesWhileGameIsPaused()
        {
            yield return StartNewGameFromVisibleButton();

            DialogueTypewriter typewriter = RequireTypewriter();
            int before = typewriter.VisibleCharacters;
            float previousScale = Time.timeScale;
            Time.timeScale = 0f;
            yield return new WaitForSecondsRealtime(0.12f);
            Time.timeScale = previousScale;

            Assert.That(
                typewriter.VisibleCharacters,
                Is.GreaterThan(before));
            Assert.That(Dialogue.IsBusy, Is.True);
            AssertNoRuntimeErrors("일시정지 중 unscaled 진행");
        }

        [UnityTest]
        public IEnumerator FirstNextClick_CompletesWithoutAdvancingCheckpoint()
        {
            yield return StartNewGameFromVisibleButton();

            Button next = RequireComponent<Button>(NextPath);
            DialogueTypewriter typewriter = RequireTypewriter();
            ProductionDialogueCheckpoint before =
                RequireCheckpoint().Copy();
            string fullText = RequireText(LinePath).text;

            yield return RawClick(next);

            ProductionDialogueCheckpoint after = RequireCheckpoint();
            Assert.That(typewriter.IsRevealing, Is.False);
            Assert.That(
                after.activeSceneId,
                Is.EqualTo(before.activeSceneId));
            Assert.That(after.lineIndex, Is.EqualTo(before.lineIndex));
            Assert.That(after.awaitingChoice, Is.EqualTo(before.awaitingChoice));
            Assert.That(RequireText(LinePath).text, Is.EqualTo(fullText));
            Assert.That(
                RequireText(LinePath).maxVisibleCharacters,
                Is.EqualTo(int.MaxValue));
        }

        [UnityTest]
        public IEnumerator SecondNextClick_AdvancesAndStartsNextReveal()
        {
            yield return StartNewGameFromVisibleButton();

            Button next = RequireComponent<Button>(NextPath);
            int initialIndex = RequireCheckpoint().lineIndex;
            string firstText = RequireText(LinePath).text;

            yield return RawClick(next);
            yield return RawClick(next);

            Assert.That(
                RequireCheckpoint().lineIndex,
                Is.EqualTo(initialIndex + 1));
            Assert.That(RequireText(LinePath).text, Is.Not.EqualTo(firstText));
            Assert.That(RequireTypewriter().IsRevealing, Is.True);
            AssertNoRuntimeErrors("두 번째 클릭 진행");
        }

        [UnityTest]
        public IEnumerator ThreeRapidClicks_AdvanceExactlyOneLine()
        {
            yield return StartNewGameFromVisibleButton();

            Button next = RequireComponent<Button>(NextPath);
            int initialIndex = RequireCheckpoint().lineIndex;

            next.onClick.Invoke();
            next.onClick.Invoke();
            next.onClick.Invoke();
            yield return null;
            yield return null;

            Assert.That(
                RequireCheckpoint().lineIndex,
                Is.EqualTo(initialIndex + 1));
            Assert.That(RequireTypewriter().IsRevealing, Is.False);
            Assert.That(Dialogue.IsBusy, Is.True);
            AssertNoRuntimeErrors("빠른 연속 클릭");
        }

        [UnityTest]
        public IEnumerator ChoiceScreen_StopsRevealAndShowsLabelsImmediately()
        {
            yield return StartNewGameFromVisibleButton();

            yield return AdvanceToVisibleChoices();

            GameObject choices = RequireObject(ChoicesPath);
            TMP_Text[] labels = choices
                .GetComponentsInChildren<TMP_Text>(false)
                .Where(label => label.gameObject.activeInHierarchy)
                .ToArray();
            Assert.That(choices.activeInHierarchy, Is.True);
            Assert.That(RequireTypewriter().IsRevealing, Is.False);
            Assert.That(labels, Has.Length.GreaterThanOrEqualTo(2));
            Assert.That(labels, Has.All.Matches<TMP_Text>(label =>
                !string.IsNullOrWhiteSpace(label.text)));
            Assert.That(
                RequireComponent<Button>(NextPath).gameObject.activeSelf,
                Is.False);
        }

        [UnityTest]
        public IEnumerator AmbientFirstClick_CompletesWithoutClosingLine()
        {
            yield return CompleteOpeningScene();

            Assert.That(
                Dialogue.StartAmbientLine(
                    "DANIEL",
                    LongAmbientLine,
                    "neutral"),
                Is.True);
            yield return null;

            Button next = RequireComponent<Button>(NextPath);
            DialogueTypewriter typewriter = RequireTypewriter();
            Assert.That(typewriter.IsRevealing, Is.True);

            yield return RawClick(next);

            Assert.That(Dialogue.IsBusy, Is.True);
            Assert.That(typewriter.IsRevealing, Is.False);
            Assert.That(
                RequireObject("Ingame/Line Panel").activeSelf,
                Is.True);
        }

        [UnityTest]
        public IEnumerator AmbientSecondClick_ClosesAfterRevealCompleted()
        {
            yield return CompleteOpeningScene();

            Assert.That(
                Dialogue.StartAmbientLine(
                    "DANIEL",
                    LongAmbientLine,
                    "neutral"),
                Is.True);
            yield return null;
            Button next = RequireComponent<Button>(NextPath);

            yield return RawClick(next);
            yield return RawClick(next);

            Assert.That(Dialogue.IsBusy, Is.False);
            Assert.That(
                RequireObject("Ingame/Line Panel").activeSelf,
                Is.False);
            Assert.That(RequireTypewriter().IsRevealing, Is.False);
            AssertNoRuntimeErrors("Ambient 두 번째 클릭 종료");
        }

        [UnityTest]
        public IEnumerator CancelActiveDialogue_StopsRevealAndHidesPanel()
        {
            yield return CompleteOpeningScene();

            Assert.That(
                Dialogue.StartAmbientLine(
                    "DANIEL",
                    LongAmbientLine,
                    "neutral"),
                Is.True);
            yield return null;
            DialogueTypewriter typewriter = RequireTypewriter();
            Assert.That(typewriter.IsRevealing, Is.True);

            Dialogue.CancelActiveDialogue();
            yield return null;

            Assert.That(Dialogue.IsBusy, Is.False);
            Assert.That(typewriter.IsRevealing, Is.False);
            Assert.That(
                RequireObject("Ingame/Line Panel").activeSelf,
                Is.False);
            Assert.That(
                RequireText(LinePath).maxVisibleCharacters,
                Is.EqualTo(int.MaxValue));
        }

        [UnityTest]
        public IEnumerator NewAmbientLine_IsNotChangedByCancelledCoroutine()
        {
            yield return CompleteOpeningScene();

            Assert.That(
                Dialogue.StartAmbientLine(
                    "DANIEL",
                    LongAmbientLine,
                    "neutral"),
                Is.True);
            yield return null;
            Dialogue.CancelActiveDialogue();

            const string replacement =
                "취소 뒤 시작한 새 대사는 이전 코루틴의 영향을 받지 않는다.";
            Assert.That(
                Dialogue.StartAmbientLine(
                    "EVELYN",
                    replacement,
                    "neutral"),
                Is.True);
            DialogueTypewriter typewriter = RequireTypewriter();
            int before = typewriter.VisibleCharacters;
            yield return new WaitForSecondsRealtime(0.08f);

            Assert.That(RequireText(LinePath).text, Is.EqualTo(replacement));
            Assert.That(typewriter.VisibleCharacters, Is.GreaterThan(before));
            Assert.That(typewriter.TotalCharacters,
                Is.EqualTo(RequireText(LinePath).textInfo.characterCount));
            AssertNoRuntimeErrors("취소 후 새 Ambient 대사");
        }

        [UnityTest]
        public IEnumerator ProductionCancel_ClearsRevealWithoutCheckpointAdvance()
        {
            yield return StartNewGameFromVisibleButton();

            ProductionDialogueCheckpoint before =
                RequireCheckpoint().Copy();
            DialogueTypewriter typewriter = RequireTypewriter();

            Dialogue.CancelActiveDialogue();
            yield return null;

            Assert.That(Dialogue.IsBusy, Is.False);
            Assert.That(typewriter.IsRevealing, Is.False);
            Assert.That(State.DialogueCheckpoint, Is.Not.Null);
            Assert.That(
                State.DialogueCheckpoint.lineIndex,
                Is.EqualTo(before.lineIndex));
            Assert.That(
                State.DialogueCheckpoint.activeSceneId,
                Is.EqualTo(before.activeSceneId));
        }

        [UnityTest]
        public IEnumerator DefaultSpeed_MatchesReadingTimeBudget()
        {
            yield return StartNewGameFromVisibleButton();

            DialogueTypewriter typewriter = RequireTypewriter();
            Assert.That(
                typewriter.CharactersPerSecond,
                Is.EqualTo(50f));

            float commonLineSeconds =
                74f / typewriter.CharactersPerSecond;
            float longestLineSeconds =
                190f / typewriter.CharactersPerSecond;
            Assert.That(commonLineSeconds, Is.EqualTo(1.48f).Within(0.001f));
            Assert.That(longestLineSeconds, Is.EqualTo(3.8f).Within(0.001f));
        }

        [UnityTest]
        public IEnumerator CompletedReveal_DoesNotAdvanceByItself()
        {
            yield return StartNewGameFromVisibleButton();

            int index = RequireCheckpoint().lineIndex;
            DialogueTypewriter typewriter = RequireTypewriter();
            typewriter.CharactersPerSecond =
                DialogueTypewriter.MaximumCharactersPerSecond;
            typewriter.CompleteImmediately();
            yield return new WaitForSecondsRealtime(0.1f);

            Assert.That(typewriter.IsRevealing, Is.False);
            Assert.That(RequireCheckpoint().lineIndex, Is.EqualTo(index));
            Assert.That(Dialogue.IsBusy, Is.True);
        }

        private DialogueTypewriter RequireTypewriter()
        {
            DialogueTypewriter typewriter =
                RequireComponent<DialogueTypewriter>(LinePath);
            Assert.That(typewriter, Is.Not.Null);
            return typewriter;
        }

        private ProductionDialogueCheckpoint RequireCheckpoint()
        {
            Assert.That(State.DialogueCheckpoint, Is.Not.Null);
            return State.DialogueCheckpoint;
        }

        private static IEnumerator RawClick(Button button)
        {
            Assert.That(button, Is.Not.Null);
            Assert.That(button.gameObject.activeInHierarchy, Is.True);
            Assert.That(button.interactable, Is.True);
            button.onClick.Invoke();
            yield return null;
            yield return null;
        }
    }
}
