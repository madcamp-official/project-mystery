using System.Collections;
using System.Linq;
using NUnit.Framework;
using TMPro;
using UnityEngine;
using UnityEngine.TestTools;
using UnityEngine.UI;
using Wake.Evidence;
using Wake.Puzzles;
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
            Assert.That(Ui.RuntimeModalControllerCount, Is.EqualTo(9));

            Assert.That(Ui.EnsureInitialized(), Is.True);
            Assert.That(Ui.EnsureInitialized(), Is.True);
            Assert.That(Ui.RuntimeModalControllerCount, Is.EqualTo(9));

            GameObject ingame = RequireObject("Ingame");
            Assert.That(
                ingame.GetComponents<ExitInspectionUIController>(),
                Has.Length.EqualTo(1));
            Assert.That(
                ingame.GetComponents<ProductionPuzzleUIController>(),
                Has.Length.EqualTo(1));
            Assert.That(
                ingame.GetComponents<BloodDirectionPuzzleUIController>(),
                Has.Length.EqualTo(1));
            Assert.That(
                ingame.GetComponents<FinalAccusationUIController>(),
                Has.Length.EqualTo(1));
            Assert.That(
                ingame.GetComponents<MarcusInterrogationUIController>(),
                Has.Length.EqualTo(1));
            Transform marcusRoot = RequireObject("Marcus Interrogation")
                .transform;
            Assert.That(
                marcusRoot.GetComponentsInChildren<Button>(true)
                    .Count(button =>
                        button.name.StartsWith("Question ")),
                Is.EqualTo(8));
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
        public IEnumerator NavigationIcons_HaveNoTextLabels()
        {
            TMP_Text startSettings = RequireObject("StartScene/Settings Btn")
                .GetComponentInChildren<TMP_Text>(true);
            Assert.That(startSettings, Is.Not.Null);
            Assert.That(
                startSettings.text,
                Is.Empty,
                "The start screen Settings button should render only its icon.");

            yield return StartNewGameFromVisibleButton();

            string[] iconButtonPaths =
            {
                "Ingame/Evidence Btn",
                "Ingame/Map Btn",
                "Ingame/Settings Btn"
            };
            foreach (string path in iconButtonPaths)
            {
                TMP_Text label = RequireObject(path)
                    .GetComponentInChildren<TMP_Text>(true);
                Assert.That(
                    label,
                    Is.Not.Null,
                    $"{path} should preserve its existing text component.");
                Assert.That(
                    label.text,
                    Is.Empty,
                    $"{path} should render only its icon.");
            }
        }

        [UnityTest]
        public IEnumerator TitleLogo_UsesEntireSourceImage()
        {
            yield return null;

            Image logo = RequireComponent<Image>(
                "StartScene/Title Presentation/Under the Horizon Logo");
            Assert.That(logo.sprite, Is.Not.Null);
            Assert.That(logo.preserveAspect, Is.True);
            Assert.That(logo.sprite.rect.x, Is.Zero);
            Assert.That(logo.sprite.rect.y, Is.Zero);
            Assert.That(
                logo.sprite.rect.width,
                Is.EqualTo(logo.sprite.texture.width));
            Assert.That(
                logo.sprite.rect.height,
                Is.EqualTo(logo.sprite.texture.height));
            AssertNoRuntimeErrors("타이틀 로고 전체 영역");
        }

        [UnityTest]
        public IEnumerator TitleScreen_HidesDecorativeTaglines()
        {
            yield return null;

            TMP_Text[] labels = RequireObject("StartScene/Title Presentation")
                .GetComponentsInChildren<TMP_Text>(true);
            Assert.That(
                labels,
                Has.None.Matches<TMP_Text>(label =>
                    label.text.Contains("2D 내러티브 미스터리 어드벤처")));
            Assert.That(
                labels,
                Has.None.Matches<TMP_Text>(label =>
                    label.text.Contains("PRESS ANY KEY")));
            AssertNoRuntimeErrors("타이틀 장식 문구 제거");
        }

        [UnityTest]
        public IEnumerator FinalAccusation_AutoPreparesDeductionsAndAdvances()
        {
            yield return StartNewGameFromVisibleButton();
            string[] requiredEvidence =
            {
                "C-01", "C-03", "C-04", "C-05", "C-06", "C-07",
                "C-08", "C-09", "C-10", "C-12", "C-14", "C-16"
            };
            foreach (string evidenceId in requiredEvidence)
            {
                Assert.That(
                    EvidenceInventory.Instance.TryAddById(evidenceId),
                    Is.True,
                    evidenceId);
            }

            FinalAccusationUIController accusation =
                RequireObject("Ingame")
                    .GetComponent<FinalAccusationUIController>();
            accusation.Open();
            yield return null;

            Assert.That(
                RequireObject("Ingame/Final Accusation").activeSelf,
                Is.True);
            Assert.That(
                FinalAccusationSession.RequiredDeductionIds.All(
                    State.HasUnlockedDeduction),
                Is.True);
            Assert.That(
                RequireComponent<Button>(
                    "Ingame/Final Accusation/최종 논증 제출").interactable,
                Is.True);

            yield return InvokeAndSettle(
                RequireComponent<Button>(
                    "Ingame/Final Accusation/Culprit"));
            yield return InvokeAndSettle(
                RequireComponent<Button>(
                    "Ingame/Final Accusation/최종 논증 제출"));

            Assert.That(
                RequireObject(
                    "Ingame/Final Accusation/MurderLocation").activeSelf,
                Is.True);
            Assert.That(
                RequireObject("Ingame/Final Accusation")
                    .GetComponentsInChildren<TMP_Text>(true)
                    .Any(text => text.text.Contains("1단계 정답")),
                Is.True);
            AssertNoRuntimeErrors("최종 심문 자동 논증 준비");
        }

        [UnityTest]
        public IEnumerator FinalAccusation_MissingEvidenceReturnsFromTheoryBoard()
        {
            yield return StartNewGameFromVisibleButton();
            FinalAccusationUIController accusation =
                RequireObject("Ingame")
                    .GetComponent<FinalAccusationUIController>();
            accusation.Open();
            yield return null;

            Assert.That(
                RequireComponent<Button>(
                    "Ingame/Final Accusation/최종 논증 제출").interactable,
                Is.False);
            Button boardButton = RequireComponent<Button>(
                "Ingame/Final Accusation/증거 보드 열기");
            Assert.That(boardButton.gameObject.activeSelf, Is.True);

            yield return InvokeAndSettle(boardButton);
            Assert.That(Ui.ActivePanel, Is.EqualTo(UiPrimaryPanel.Evidence));
            Assert.That(
                RequireObject("Evidence Theory Board").activeSelf,
                Is.True);

            yield return InvokeAndSettle(
                RequireComponent<Button>("Evidence Theory Board/Close"));

            Assert.That(Ui.ActivePanel, Is.EqualTo(UiPrimaryPanel.Ingame));
            Assert.That(
                RequireObject("Ingame/Final Accusation").activeSelf,
                Is.True);
            Assert.That(
                RequireObject("Ingame/Final Accusation")
                    .GetComponentsInChildren<TMP_Text>(true)
                    .Any(text => text.text.Contains(
                        "최종 심문에 필요한 핵심 논증")),
                Is.True);
            AssertNoRuntimeErrors("최종 심문 증거 보드 복귀");
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
            Assert.That(Ui.OpenRuntimeModalCount, Is.Zero);
            Assert.That(
                RequireComponent<Image>("Evidence/Image").sprite,
                Is.Null);
            TMP_Text placeholder =
                RequireComponent<TMP_Text>("Evidence/Description");
            Assert.That(placeholder.gameObject.activeSelf, Is.True);
            Assert.That(placeholder.text, Does.Contain("확보한 증거가 없습니다"));
            Assert.That(
                placeholder.font,
                Is.SameAs(
                    TypographyService.Resolve(
                        TypographyRole.BodyRegular)));
            TMP_Text title =
                RequireComponent<TMP_Text>("Evidence/Text (TMP)");
            Assert.That(title.text, Is.EqualTo("증거"));
            Assert.That(title.text, Does.Not.Contain("C-"));
            Assert.That(
                title.font,
                Is.SameAs(
                    TypographyService.Resolve(
                        TypographyRole.Heading)));
            Assert.That(
                RequireComponent<Button>("Evidence/Next").interactable,
                Is.False);
            Assert.That(
                RequireComponent<Button>("Evidence/Next (1)").interactable,
                Is.False);
            yield return InvokeAndSettle(
                RequireComponent<Button>("Evidence/Turn (2)"));
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
            Assert.That(Ui.OpenRuntimeModalCount, Is.Zero);
            yield return InvokeAndSettle(
                RequireComponent<Button>("Evidence/Turn (2)"));
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
