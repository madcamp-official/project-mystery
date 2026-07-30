using System.Collections;
using System.Collections.Generic;
using System.Linq;
using NUnit.Framework;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
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
            "엘리시움 호는 항구의 유리 지붕 너머에서 지나치게 새것처럼 빛나고 있었다.";

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
            LocationBackgroundAnimationOverlay backgroundAnimation =
                Object.FindFirstObjectByType<
                    LocationBackgroundAnimationOverlay>(
                    FindObjectsInactive.Include);
            Assert.That(backgroundAnimation, Is.Not.Null);
            Assert.That(
                backgroundAnimation.ActiveProfileId,
                Is.EqualTo("PORT"));
            Assert.That(
                backgroundAnimation.ActiveElementCount,
                Is.GreaterThan(0));

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
                "공식 P-01_C1의 다니엘 신뢰도 +1 효과가 적용되어야 합니다.");
            RawImage portrait = RequireComponent<RawImage>(
                "Ingame/Speaker Portrait");
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
        public IEnumerator AmbientCharacters_AppearInsideBackgroundAndStayClickable()
        {
            yield return StartNewGameFromVisibleButton();
            Dialogue.CancelActiveDialogue();
            yield return null;
            EventSystem.current?.SetSelectedGameObject(null);
            foreach (ExplorationHotspotFeedback feedback in
                     Object.FindObjectsByType<ExplorationHotspotFeedback>(
                         FindObjectsInactive.Include,
                         FindObjectsSortMode.None))
            {
                feedback.ResetTransientState();
                Assert.That(
                    feedback.transform.Find("Interaction Label"),
                    Is.Null,
                    $"{feedback.name} must not create a hover name label.");
                Assert.That(
                    feedback.transform.Find("State Label"),
                    Is.Null,
                    $"{feedback.name} must not keep a legacy hover name label.");
            }
            UnityEngine.Canvas.ForceUpdateCanvases();

            Button[] ambientButtons = Object.FindObjectsByType<Button>(
                    FindObjectsInactive.Exclude,
                    FindObjectsSortMode.None)
                .Where(button =>
                    button.name.StartsWith("AmbientCharacter_"))
                .OrderBy(button => button.transform.GetSiblingIndex())
                .ToArray();

            Assert.That(
                ambientButtons,
                Has.Length.EqualTo(2),
                "The port should expose only its location-specific characters.");
            LocationLoader loader = LocationLoader.Instance;
            Assert.That(loader, Is.Not.Null);
            Assert.That(
                loader.UsesApprovedSemanticCharacterPlacement,
                Is.True,
                "The approved PORT semantic profile must replace the legacy " +
                "stage catalog at runtime.");
            Assert.That(loader.HasApprovedSemanticSceneLayout, Is.True);
            Assert.That(
                loader.ApprovedSemanticCastMatches,
                Is.True,
                "The baked P-01 layout must match the cast spawned by the " +
                "actual UI runtime.");
            Assert.That(
                ApprovedBackgroundSemanticResolver.TryResolve(
                    loader.CurrentLocation.LocationCode,
                    loader.ActiveBackgroundVariantKey,
                    loader.ActiveBackgroundSprite,
                    loader.NarrativeSceneContext,
                    out BackgroundSemanticRuntimeResolution
                        semanticResolution),
                Is.True,
                "The active sprite, exact variant key, and scene must resolve " +
                "to the baked approved semantic data.");
            Assert.That(semanticResolution.SceneLayout, Is.Not.Null);
            Assert.That(
                semanticResolution.Profile.ProfileId,
                Is.EqualTo(loader.ActiveSemanticProfileId));
            BackgroundCoverPresenter semanticBackground =
                Object.FindFirstObjectByType<BackgroundCoverPresenter>(
                    FindObjectsInactive.Include);
            Rect visibleBackground = CalculateVisibleBackgroundRect(
                semanticBackground.ContentRect,
                semanticBackground.ViewportRect);
            IEnumerable<BackgroundSemanticCharacterRequest>
                semanticRequests =
                    semanticResolution.SceneLayout.Assignments.Select(
                        assignment =>
                            new BackgroundSemanticCharacterRequest(
                                assignment.CharacterId,
                                assignment.Role));
            BackgroundSemanticPlacementResult semanticPlacement =
                BackgroundSemanticPlacementResolver.Resolve(
                    semanticResolution,
                    semanticRequests,
                    visibleBackground,
                    loader.ActiveBackgroundSprite.rect.width /
                    loader.ActiveBackgroundSprite.rect.height);
            Assert.That(semanticPlacement.IsValid, Is.True);
            Assert.That(
                semanticPlacement.Assignments,
                Has.Count.EqualTo(ambientButtons.Length));
            Assert.That(
                BackgroundSemanticPlacementResolver.Validate(
                    semanticPlacement.Assignments,
                    visibleBackground,
                    out string semanticDiagnostic),
                Is.True,
                semanticDiagnostic);
            RawImage[] groundShadows = Object.FindObjectsByType<RawImage>(
                    FindObjectsInactive.Exclude,
                    FindObjectsSortMode.None)
                .Where(image =>
                    image.name.StartsWith("AmbientGroundShadow_"))
                .ToArray();
            Assert.That(
                groundShadows,
                Has.Length.EqualTo(ambientButtons.Length),
                "Every world character needs a separate contact shadow.");
            for (int characterIndex = 0;
                 characterIndex < ambientButtons.Length;
                 characterIndex++)
            {
                Button button = ambientButtons[characterIndex];
                RectTransform rect = button.GetComponent<RectTransform>();
                RawImage character = button.GetComponent<RawImage>();
                UiCharacterIdleMotion idleMotion =
                    button.GetComponent<UiCharacterIdleMotion>();
                Assert.That(
                    idleMotion,
                    Is.Not.Null,
                    $"{button.name} needs deterministic idle motion.");
                Assert.That(
                    idleMotion.TargetGraphic,
                    Is.SameAs(character));
                Assert.That(idleMotion.UseUnscaledTime, Is.True);
                // Geometry and tint checks below assert the authored stage,
                // so sample the neutral frame instead of a random idle phase.
                idleMotion.ApplyAtTime(0f);
                Assert.That(
                    rect.parent.name,
                    Is.EqualTo("Cover Image"),
                    $"{button.name} must be staged inside the location image.");
                Assert.That(
                    character.texture,
                    Is.Not.Null,
                    $"{button.name} must render a full-body world character.");
                Assert.That(
                    character.color,
                    Is.Not.EqualTo(Color.white),
                    $"{button.name} must inherit the location light tint.");
                Assert.That(
                    button.colors.normalColor,
                    Is.EqualTo(character.color),
                    $"{button.name} must preserve tint between UI states.");
                Assert.That(
                    rect.anchoredPosition.y,
                    Is.LessThan(0f),
                    $"{button.name} must remove transparent atlas padding " +
                    "so its visible feet reach the stage anchor.");
                Assert.That(
                    character.material.shader.name,
                    Is.EqualTo("Wake/UI/Ambient Character Blend"),
                    $"{button.name} must use the background blend shader.");
                ExplorationHotspotFeedback feedback =
                    button.GetComponent<ExplorationHotspotFeedback>();
                Assert.That(feedback, Is.Not.Null);
                Assert.That(
                    feedback.IsIndicatorVisible,
                    Is.False,
                    $"{button.name} highlight must start hidden.");
                Assert.That(
                    button.transform.Find("Interaction Label"),
                    Is.Null,
                    $"{button.name} must not create a hover name label.");
                AmbientBarkRecord bark = AmbientBarkCatalog
                    .GetAvailable("PORT", State)
                    .FirstOrDefault(item =>
                        button.name.StartsWith(
                            $"AmbientCharacter_{item.Speaker}_"));
                string speaker = bark?.Speaker ??
                    ScenePresenceCatalog.MainCharacterIds.First(item =>
                        button.name.StartsWith(
                            $"AmbientCharacter_{item}_"));
                Assert.That(
                    AmbientWorldCharacterCatalog.TryGetAsset(
                        speaker,
                        out AmbientWorldCharacterAsset asset),
                    Is.True);
                float visibleFootOffset =
                    rect.anchoredPosition.y +
                    rect.rect.height * asset.VisibleBottomMargin;
                Assert.That(
                    visibleFootOffset,
                    Is.EqualTo(0f).Within(0.5f),
                    $"{button.name} visible feet must coincide with the " +
                    "location stage anchor.");
                BackgroundSemanticPlacementAssignment assignment =
                    semanticPlacement.Assignments.Single(item =>
                        item.Character.CharacterId == speaker);
                BackgroundSemanticSlot slot = assignment.Slot;
                Assert.That(
                    BackgroundSemanticStageAdapter.TryCreate(
                        semanticResolution.Binding,
                        slot,
                        out AmbientWorldStageProfile stage),
                    Is.True);
                Assert.That(
                    rect.anchorMin.x,
                    Is.EqualTo(slot.Anchor.x).Within(0.0001f),
                    $"{button.name} must keep its approved semantic x anchor.");
                Assert.That(
                    rect.anchorMin.y,
                    Is.EqualTo(slot.Anchor.y).Within(0.0001f),
                    $"{button.name} must keep its approved semantic y anchor.");
                Assert.That(
                    rect.anchorMax,
                    Is.EqualTo(rect.anchorMin),
                    $"{button.name} must remain point-anchored after runtime " +
                    "layout placeholders are applied.");
                float visibleBodyHeight =
                    rect.rect.height * asset.VisibleVerticalSpan;
                float expectedBodyHeight =
                    rect.parent.GetComponent<RectTransform>().rect.height *
                    stage.NormalizedHeight;
                Assert.That(
                    visibleBodyHeight,
                    Is.EqualTo(expectedBodyHeight).Within(5f),
                    $"{button.name} must size the visible body, not the " +
                    "transparent atlas cell, to the perspective profile.");
            }
            foreach (RawImage groundShadow in groundShadows)
            {
                Assert.That(groundShadow.texture, Is.Not.Null);
                Assert.That(groundShadow.raycastTarget, Is.False);
                Assert.That(groundShadow.color.a, Is.GreaterThan(0f));
                Assert.That(
                    groundShadow.rectTransform.parent.name,
                    Is.EqualTo("Cover Image"));
            }
        }

        private static Rect CalculateVisibleBackgroundRect(
            RectTransform content,
            RectTransform viewport)
        {
            Assert.That(content, Is.Not.Null);
            Assert.That(viewport, Is.Not.Null);
            Vector2 contentSize = content.rect.size;
            Vector2 viewportSize = viewport.rect.size;
            Assert.That(contentSize.x, Is.GreaterThan(0f));
            Assert.That(contentSize.y, Is.GreaterThan(0f));
            Assert.That(viewportSize.x, Is.GreaterThan(0f));
            Assert.That(viewportSize.y, Is.GreaterThan(0f));

            Vector2 offset = content.anchoredPosition;
            Vector2 pivot = content.pivot;
            float xMin =
                (-viewportSize.x * .5f - offset.x) /
                contentSize.x +
                pivot.x;
            float xMax =
                (viewportSize.x * .5f - offset.x) /
                contentSize.x +
                pivot.x;
            float yMin =
                (-viewportSize.y * .5f - offset.y) /
                contentSize.y +
                pivot.y;
            float yMax =
                (viewportSize.y * .5f - offset.y) /
                contentSize.y +
                pivot.y;
            return Rect.MinMaxRect(
                Mathf.Clamp01(xMin),
                Mathf.Clamp01(yMin),
                Mathf.Clamp01(xMax),
                Mathf.Clamp01(yMax));
        }

        [UnityTest]
        public IEnumerator ContinueButton_RestoresProductionLineFromCheckpoint()
        {
            yield return StartNewGameFromVisibleButton();

            Button next =
                RequireComponent<Button>("Ingame/Line Panel/Panel/Next");
            yield return InvokeAndSettle(next);
            yield return InvokeAndSettle(next);

            string restoredText =
                RequireText("Ingame/Line Panel/Panel/line").text;
            Assert.That(
                RequireText("Ingame/Line Panel/Panel/line").text,
                Is.EqualTo(restoredText));
            Assert.That(State.DialogueCheckpoint, Is.Not.Null);
            Assert.That(State.DialogueCheckpoint.lineIndex, Is.EqualTo(1));

            State.RecordLocation("LAUNDRY");
            Assert.That(
                State.CurrentLocationCode,
                Is.EqualTo("LAUNDRY"),
                "회귀 조건: 저장 위치가 진행 중인 P-01의 PORT와 달라야 합니다.");

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
            Assert.That(
                LocationLoader.Instance.CurrentLocation.LocationCode,
                Is.EqualTo("PORT"));
            Assert.That(State.DialogueCheckpoint, Is.Not.Null);
            Assert.That(State.DialogueCheckpoint.lineIndex, Is.EqualTo(1));

            string actual =
                RequireText("Ingame/Line Panel/Panel/line").text;
            Assert.That(actual, Is.EqualTo(restoredText));
            AssertKoreanTextIsIntact(actual);
            AssertNoRuntimeErrors("체크포인트 이어하기");
        }
    }
}
