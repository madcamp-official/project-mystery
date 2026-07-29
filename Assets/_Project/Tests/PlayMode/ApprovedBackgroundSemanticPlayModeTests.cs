using System.Collections;
using System.Collections.Generic;
using System.Linq;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;
using UnityEngine.UI;
using Wake.Exploration;
using Wake.UI;

namespace Wake.Tests.PlayMode
{
    public sealed class ApprovedBackgroundSemanticPlayModeTests :
        UiBasicScenePlayModeFixture
    {
        private int originalWidth;
        private int originalHeight;

        [UnitySetUp]
        public IEnumerator UseSixteenNineGameView()
        {
            originalWidth = Screen.width;
            originalHeight = Screen.height;
            Screen.SetResolution(1600, 900, false);
            yield return null;
            yield return null;
            UnityEngine.Canvas.ForceUpdateCanvases();
        }

        [UnityTearDown]
        public IEnumerator RestoreGameViewResolution()
        {
            if (originalWidth > 0 && originalHeight > 0)
                Screen.SetResolution(originalWidth, originalHeight, false);
            yield return null;
        }

        [UnityTest]
        public IEnumerator DiningScene_UsesApprovedSlotsAndDisablesOffCameraCast()
        {
            yield return StartNewGameFromVisibleButton(
                startOpeningDialogue: false);

            CompleteScenes(
                "P-01",
                "P-02",
                "P-03",
                "D1-01");
            State.UnlockProductionScene("D1-02");

            Ui.ShowMap();
            yield return WaitForUiTransition();
            MapController map = Object.FindObjectsByType<MapController>(
                    FindObjectsInactive.Include,
                    FindObjectsSortMode.None)
                .Single();
            SceneTravelResult travel = map.TryTravelToScene("D1-02");
            yield return WaitForUiTransition();
            UnityEngine.Canvas.ForceUpdateCanvases();

            Assert.That(travel.IsAllowed, Is.True);
            Assert.That(Ui.ActivePanel, Is.EqualTo(UiPrimaryPanel.Ingame));
            LocationLoader loader = LocationLoader.Instance;
            Assert.That(loader, Is.Not.Null);
            Assert.That(loader.IsPresentationVisible, Is.True);
            Assert.That(loader.NarrativeSceneContext, Is.EqualTo("D1-02"));
            Assert.That(
                loader.UsesApprovedSemanticCharacterPlacement,
                Is.True,
                "The in-game loader must activate the baked DINING profile.");
            Assert.That(loader.HasApprovedSemanticSceneLayout, Is.True);
            Assert.That(
                loader.ApprovedSemanticCastMatches,
                Is.True,
                "The runtime D1-02 cast must match the approved layout " +
                "fingerprint before fixed slots are used.");
            Assert.That(
                ApprovedBackgroundSemanticResolver.TryResolve(
                    loader.CurrentLocation.LocationCode,
                    loader.ActiveBackgroundVariantKey,
                    loader.ActiveBackgroundSprite,
                    "D1-02",
                    out BackgroundSemanticRuntimeResolution resolution),
                Is.True);
            Assert.That(resolution.SceneLayout, Is.Not.Null);
            Assert.That(resolution.SceneLayout.Assignments, Has.Count.EqualTo(3));
            Assert.That(
                resolution.SceneLayout.OffCameraCharacterIds,
                Is.EquivalentTo(new[] { "DINING_SOMMELIER" }));
            BackgroundCoverPresenter background =
                Object.FindFirstObjectByType<BackgroundCoverPresenter>(
                    FindObjectsInactive.Include);
            Assert.That(background, Is.Not.Null);
            Rect visibleBackground = CalculateVisibleBackgroundRect(
                background.ContentRect,
                background.ViewportRect);
            IEnumerable<BackgroundSemanticCharacterRequest>
                semanticRequests =
                    resolution.SceneLayout.Assignments.Select(
                            assignment =>
                                new BackgroundSemanticCharacterRequest(
                                    assignment.CharacterId,
                                    assignment.Role))
                        .Concat(
                            resolution.SceneLayout.OffCameraCharacterIds
                                .Select(characterId =>
                                    new BackgroundSemanticCharacterRequest(
                                        characterId,
                                        BackgroundSemanticCharacterRole
                                            .Context)));
            BackgroundSemanticPlacementResult semanticPlacement =
                BackgroundSemanticPlacementResolver.Resolve(
                    resolution,
                    semanticRequests,
                    visibleBackground,
                    loader.ActiveBackgroundSprite.rect.width /
                    loader.ActiveBackgroundSprite.rect.height);
            Assert.That(
                semanticPlacement.IsValid,
                Is.True,
                string.Join(" | ", semanticPlacement.Diagnostics));
            Assert.That(
                semanticPlacement.OffCameraCharacterIds,
                Is.EquivalentTo(new[] { "DINING_SOMMELIER" }));
            Assert.That(
                semanticPlacement.Assignments,
                Has.Count.EqualTo(3));

            Button[] worldCharacters = Object.FindObjectsByType<Button>(
                    FindObjectsInactive.Include,
                    FindObjectsSortMode.None)
                .Where(button =>
                    button.name.StartsWith("AmbientCharacter_"))
                .ToArray();
            AmbientCharacterHotspotOverlay semanticOverlay =
                Object.FindFirstObjectByType<
                    AmbientCharacterHotspotOverlay>(
                    FindObjectsInactive.Include);
            string semanticDiagnostics = semanticOverlay != null
                ? string.Join(
                    " | ",
                    semanticOverlay.SemanticPlacementDiagnostics)
                : "<overlay missing>";
            Button sommelier = worldCharacters.Single(button =>
                button.name.StartsWith(
                    "AmbientCharacter_DINING_SOMMELIER_"));
            Assert.That(
                sommelier.gameObject.activeSelf,
                Is.False,
                "An explicitly off-camera character must remain instantiated " +
                "but inactive, so it cannot intercept input.");
            Assert.That(
                sommelier.gameObject.activeInHierarchy,
                Is.False);

            var activeByCharacter = new Dictionary<string, Button>();
            foreach (BackgroundSemanticPlacementAssignment assignment in
                     semanticPlacement.Assignments)
            {
                string characterId = assignment.Character.CharacterId;
                Button button = worldCharacters.Single(candidate =>
                    candidate.name.StartsWith(
                        $"AmbientCharacter_{characterId}_"));
                Assert.That(
                    button.gameObject.activeInHierarchy,
                    Is.True,
                    $"{characterId} must be visible. " +
                    $"semantic={semanticDiagnostics}");
                activeByCharacter.Add(characterId, button);
                AssertApprovedSlotGeometry(
                    button,
                    assignment);
            }

            Assert.That(
                worldCharacters.Count(button =>
                    button.gameObject.activeInHierarchy),
                Is.EqualTo(activeByCharacter.Count),
                "Only characters assigned to approved on-camera slots may " +
                "remain visible.");
            AssertSilhouettesDoNotOverlap(
                semanticPlacement.Assignments,
                visibleBackground);
            AssertNoRuntimeErrors(
                "D1-02 approved semantic placement at 16:9");
        }

        private static void AssertApprovedSlotGeometry(
            Button button,
            BackgroundSemanticPlacementAssignment assignment)
        {
            string characterId = assignment.Character.CharacterId;
            BackgroundSemanticSlot slot = assignment.Slot;
            RectTransform rect = button.GetComponent<RectTransform>();
            UiCharacterIdleMotion motion =
                button.GetComponent<UiCharacterIdleMotion>();
            motion?.ApplyAtTime(0f);

            Assert.That(
                rect.anchorMin.x,
                Is.EqualTo(slot.Anchor.x).Within(0.0001f),
                $"{characterId} x anchor was overwritten.");
            Assert.That(
                rect.anchorMin.y,
                Is.EqualTo(slot.Anchor.y).Within(0.0001f),
                $"{characterId} y anchor was overwritten.");
            Assert.That(rect.anchorMax, Is.EqualTo(rect.anchorMin));
            Assert.That(
                AmbientWorldCharacterCatalog.TryGetAsset(
                    characterId,
                    out AmbientWorldCharacterAsset asset),
                Is.True);
            float visibleBodyHeight =
                rect.rect.height * asset.VisibleVerticalSpan;
            float expectedBodyHeight =
                ((RectTransform)rect.parent).rect.height *
                slot.NormalizedHeight;
            Assert.That(
                visibleBodyHeight,
                Is.EqualTo(expectedBodyHeight).Within(5f),
                $"{characterId} height must come from approved " +
                $"slot {slot.Id}.");

            if (RuntimeUiLayoutRegistry.TryGetNormalizedRect(
                    $"location.DINING.character." +
                    characterId,
                    out Rect legacyPlaceholder) &&
                Vector2.Distance(legacyPlaceholder.center, slot.Anchor) >
                0.001f)
            {
                Assert.That(
                    Vector2.Distance(rect.anchorMin, legacyPlaceholder.center),
                    Is.GreaterThan(0.001f),
                    "Legacy RuntimeUiLayoutRegistry coordinates must not " +
                    "overwrite approved semantic anchors.");
            }
        }

        private static void AssertSilhouettesDoNotOverlap(
            IEnumerable<BackgroundSemanticPlacementAssignment> assignments,
            Rect visibleBackground)
        {
            var silhouettes = new List<(string CharacterId, Rect Rect)>();
            foreach (BackgroundSemanticPlacementAssignment assignment in
                     assignments)
            {
                silhouettes.Add(
                    (
                        assignment.Character.CharacterId,
                        assignment.SilhouetteRect));
            }

            for (int current = 0; current < silhouettes.Count; current++)
            {
                Rect currentRect = silhouettes[current].Rect;
                Assert.That(
                    currentRect.xMin,
                    Is.GreaterThanOrEqualTo(
                        visibleBackground.xMin - 0.0001f));
                Assert.That(
                    currentRect.yMin,
                    Is.GreaterThanOrEqualTo(
                        visibleBackground.yMin - 0.0001f));
                Assert.That(
                    currentRect.xMax,
                    Is.LessThanOrEqualTo(
                        visibleBackground.xMax + 0.0001f));
                Assert.That(
                    currentRect.yMax,
                    Is.LessThanOrEqualTo(
                        visibleBackground.yMax + 0.0001f));
                for (int previous = 0; previous < current; previous++)
                {
                    Assert.That(
                        currentRect.Overlaps(
                            silhouettes[previous].Rect,
                            true),
                        Is.False,
                        $"{silhouettes[current].CharacterId} overlaps " +
                        $"{silhouettes[previous].CharacterId}.");
                }
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

        private void CompleteScenes(params string[] sceneIds)
        {
            foreach (string sceneId in sceneIds)
                State.RecordCompletedScene(sceneId);
        }
    }
}
