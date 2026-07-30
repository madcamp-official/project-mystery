using System;
using UnityEngine;
using Wake.Core;
using Wake.UI;

namespace Wake.Exploration
{
    public class LocationLoader : MonoBehaviour
    {
        public enum LoadFailure
        {
            None,
            MissingLocation,
            MissingVisualContent,
            UnusedLocation
        }

        public static LocationLoader Instance { get; private set; }

        public LocationDefinition CurrentLocation { get; private set; }
        public string NarrativeSceneContext { get; private set; } =
            string.Empty;
        public event Action<LocationDefinition> LocationChanged;
        public bool IsPresentationVisible =>
            container != null && container.gameObject.activeSelf;
        public bool IsWorldInteractionSuppressed =>
            ambientCharacters?.IsModalPresentationSuppressed == true;
        public RectTransform BackgroundRect => backgroundPresenter?.ViewportRect;
        public Sprite ActiveBackgroundSprite =>
            backgroundPresenter?.Sprite;
        public string ActiveBackgroundVariantKey =>
            activeBackgroundSelection.VariantKey;
        public string ActiveSemanticProfileId =>
            !string.IsNullOrEmpty(
                ambientCharacters?.ActiveSemanticProfileId)
                ? ambientCharacters.ActiveSemanticProfileId
                : activeBackgroundSelection.SemanticProfileId;
        public bool UsesApprovedSemanticCharacterPlacement =>
            ambientCharacters?.UsesApprovedSemanticPlacement == true;
        public bool HasApprovedSemanticSceneLayout =>
            ambientCharacters?.HasApprovedSceneLayout == true;
        public bool ApprovedSemanticCastMatches =>
            ambientCharacters?.ActiveSceneLayoutFingerprintMatched == true;
        public string ActiveBackgroundAnimationProfileId =>
            backgroundAnimations?.ActiveProfileId ?? string.Empty;
        public bool IsAmbientMotionPaused =>
            systemMotionPaused || reducedMotion;

        private GameObject currentInstance;
        private Transform container;
        private BackgroundCoverPresenter backgroundPresenter;
        private LocationBackgroundAnimationOverlay backgroundAnimations;
        private EvidenceLocationHotspotOverlay evidenceHotspots;
        private AmbientCharacterHotspotOverlay ambientCharacters;
        private AmbientInspectableOverlay ambientInspectables;
        private AmbientRoomParticleOverlay ambientParticles;
        private LocationBackgroundSelection activeBackgroundSelection;
        private bool systemMotionPaused;
        private bool reducedMotion;

        private void Awake()
        {
            Instance = this;
            reducedMotion = ReducedMotionSettings.Enabled;
            ReducedMotionSettings.Changed +=
                HandleReducedMotionChanged;
            container = new GameObject("LocationContainer").transform;
            container.SetParent(transform, false);
            CreateBackgroundPresenter();
        }

        private void OnDestroy()
        {
            ReducedMotionSettings.Changed -=
                HandleReducedMotionChanged;
            if (Instance == this)
            {
                Instance = null;
            }
        }

        public void LoadLocation(LocationDefinition location)
        {
            TryLoadLocation(location, out _);
        }

        public void SetPresentationVisible(bool visible)
        {
            if (container != null)
            {
                container.gameObject.SetActive(visible);
            }
        }

        public void SetWorldInteractionSuppressed(bool suppressed)
        {
            ambientCharacters?.SetModalPresentationSuppressed(suppressed);
        }

        public void SetAmbientMotionPaused(bool paused)
        {
            systemMotionPaused = paused;
            ApplyAmbientMotionPolicy();
        }

        public bool TryLoadLocation(LocationDefinition location, out LoadFailure failure)
        {
            if (location == null)
            {
                failure = LoadFailure.MissingLocation;
                return false;
            }

            if (CanonicalLocationCatalog.IsUnused(location.LocationCode))
            {
                failure = LoadFailure.UnusedLocation;
                return false;
            }

            if (location == CurrentLocation)
            {
                RefreshInteractionOverlays();
                failure = LoadFailure.None;
                return true;
            }

            if (location.ContentPrefab == null && location.BackgroundSprite == null)
            {
                failure = LoadFailure.MissingVisualContent;
                return false;
            }

            if (backgroundPresenter == null)
            {
                container ??= new GameObject("LocationContainer").transform;
                container.SetParent(transform, false);
                CreateBackgroundPresenter();
            }

            if (currentInstance != null)
            {
                if (Application.isPlaying)
                    Destroy(currentInstance);
                else
                    DestroyImmediate(currentInstance);
            }

            currentInstance = location.ContentPrefab != null
                ? Instantiate(location.ContentPrefab, container)
                : null;
            LocationBackgroundSelection backgroundSelection =
                LocationBackgroundVariantCatalog.ResolveSelection(
                    location.LocationCode,
                    NarrativeSceneContext,
                    location.BackgroundSprite,
                    GameStateManager.Instance?
                        .CompletedProductionSceneIds);
            activeBackgroundSelection = backgroundSelection;
            Sprite backgroundSprite = backgroundSelection.Sprite;
            backgroundPresenter.Show(
                backgroundSprite,
                location.BackgroundFocus,
                location.BackgroundZoom);
            backgroundAnimations?.Show(location.LocationCode);
            ApplyAmbientMotionPolicy();
            evidenceHotspots?.Show(
                location.LocationCode,
                backgroundSelection);
            ambientCharacters?.Show(
                location.LocationCode,
                NarrativeSceneContext,
                backgroundSelection);
            ambientInspectables?.Show(
                location.LocationCode,
                NarrativeSceneContext,
                backgroundSelection);
            ambientParticles?.Show(
                backgroundAnimations?.ResolveAmbientParticleTint(
                    location.AmbientParticleTint) ??
                location.AmbientParticleTint,
                backgroundSprite);
            CurrentLocation = location;
            LocationChanged?.Invoke(location);
            AudioManager.Instance?.PlayLocationTheme(location.LocationCode);
            GameStateManager.Instance?.RecordLocation(location.LocationCode);
            failure = LoadFailure.None;
            return true;
        }

        public void PrepareNarrativeScene(string sceneId)
        {
            NarrativeSceneContext =
                sceneId?.Trim().ToUpperInvariant() ?? string.Empty;
            RefreshInteractionOverlays();
        }

        private void RefreshInteractionOverlays()
        {
            if (CurrentLocation == null)
                return;

            LocationBackgroundSelection backgroundSelection =
                LocationBackgroundVariantCatalog.ResolveSelection(
                    CurrentLocation.LocationCode,
                    NarrativeSceneContext,
                    CurrentLocation.BackgroundSprite,
                    GameStateManager.Instance?
                        .CompletedProductionSceneIds);
            activeBackgroundSelection = backgroundSelection;
            Sprite backgroundSprite = backgroundSelection.Sprite;
            if (backgroundPresenter?.Sprite != backgroundSprite)
            {
                backgroundPresenter?.Show(
                    backgroundSprite,
                    CurrentLocation.BackgroundFocus,
                    CurrentLocation.BackgroundZoom);
            }
            evidenceHotspots?.Show(
                CurrentLocation.LocationCode,
                backgroundSelection);
            ambientCharacters?.Show(
                CurrentLocation.LocationCode,
                NarrativeSceneContext,
                backgroundSelection);
            ambientInspectables?.Show(
                CurrentLocation.LocationCode,
                NarrativeSceneContext,
                backgroundSelection);
            ambientParticles?.Show(
                backgroundAnimations?.ResolveAmbientParticleTint(
                    CurrentLocation.AmbientParticleTint) ??
                CurrentLocation.AmbientParticleTint,
                backgroundSprite);
        }

        private void CreateBackgroundPresenter()
        {
            GameObject canvasObject = new(
                "LocationBackgroundCanvas",
                typeof(Canvas),
                typeof(UnityEngine.UI.GraphicRaycaster));
            canvasObject.transform.SetParent(container, false);
            Canvas canvas = canvasObject.GetComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            canvas.sortingOrder = -100;

            GameObject presenterObject = new(
                "LocationBackground",
                typeof(RectTransform),
                typeof(BackgroundCoverPresenter));
            backgroundPresenter =
                presenterObject.GetComponent<BackgroundCoverPresenter>();
            backgroundPresenter.Initialize(
                canvasObject.GetComponent<RectTransform>());
            backgroundAnimations =
                presenterObject.AddComponent<
                    LocationBackgroundAnimationOverlay>();
            backgroundAnimations.Initialize(
                backgroundPresenter.ContentRect,
                backgroundPresenter.MotionRect);
            evidenceHotspots =
                presenterObject.AddComponent<EvidenceLocationHotspotOverlay>();
            evidenceHotspots.Initialize(backgroundPresenter.ContentRect);
            ambientCharacters =
                presenterObject.AddComponent<AmbientCharacterHotspotOverlay>();
            // Ambient characters belong to the photographed space. Parenting
            // them to the cover image keeps their feet and scale aligned when
            // the background is cropped, focused, or zoomed.
            ambientCharacters.Initialize(
                backgroundPresenter.ContentRect,
                backgroundPresenter.ViewportRect);
            ambientInspectables =
                presenterObject.AddComponent<AmbientInspectableOverlay>();
            ambientInspectables.Initialize(backgroundPresenter.ContentRect);
            ambientParticles =
                presenterObject.AddComponent<AmbientRoomParticleOverlay>();
            ambientParticles.Initialize(backgroundPresenter.ContentRect);
            ApplyAmbientMotionPolicy();
        }

        private void HandleReducedMotionChanged(bool value)
        {
            reducedMotion = value;
            ApplyAmbientMotionPolicy();
        }

        private void ApplyAmbientMotionPolicy()
        {
            backgroundAnimations?.SetPaused(systemMotionPaused);
            backgroundAnimations?.SetReducedMotion(reducedMotion);
            ambientCharacters?.SetMotionSuppressed(
                systemMotionPaused || reducedMotion);
            ambientParticles?.SetPaused(systemMotionPaused);
            ambientParticles?.SetReducedMotion(reducedMotion);
        }
    }
}
