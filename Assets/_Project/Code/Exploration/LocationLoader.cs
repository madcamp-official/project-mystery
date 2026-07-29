using System;
using UnityEngine;
using Wake.Core;

namespace Wake.Exploration
{
    public class LocationLoader : MonoBehaviour
    {
        public enum LoadFailure
        {
            None,
            MissingLocation,
            MissingVisualContent
        }

        public static LocationLoader Instance { get; private set; }

        public LocationDefinition CurrentLocation { get; private set; }
        public string NarrativeSceneContext { get; private set; } =
            string.Empty;
        public event Action<LocationDefinition> LocationChanged;
        public bool IsPresentationVisible =>
            container != null && container.gameObject.activeSelf;
        public RectTransform BackgroundRect => backgroundPresenter?.ViewportRect;

        private GameObject currentInstance;
        private Transform container;
        private BackgroundCoverPresenter backgroundPresenter;
        private EvidenceLocationHotspotOverlay evidenceHotspots;
        private AmbientCharacterHotspotOverlay ambientCharacters;
        private AmbientInspectableOverlay ambientInspectables;
        private AmbientRoomParticleOverlay ambientParticles;

        private void Awake()
        {
            Instance = this;
            container = new GameObject("LocationContainer").transform;
            container.SetParent(transform, false);
            CreateBackgroundPresenter();
        }

        private void OnDestroy()
        {
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

        public bool TryLoadLocation(LocationDefinition location, out LoadFailure failure)
        {
            if (location == null)
            {
                failure = LoadFailure.MissingLocation;
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
                Destroy(currentInstance);
            }

            currentInstance = location.ContentPrefab != null
                ? Instantiate(location.ContentPrefab, container)
                : null;
            backgroundPresenter.Show(
                location.BackgroundSprite,
                location.BackgroundFocus,
                location.BackgroundZoom);
            evidenceHotspots?.Show(location.LocationCode);
            ambientCharacters?.Show(
                location.LocationCode,
                NarrativeSceneContext);
            ambientInspectables?.Show(location.LocationCode);
            ambientParticles?.Show(
                location.AmbientParticleTint, location.BackgroundSprite);
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

            evidenceHotspots?.Show(CurrentLocation.LocationCode);
            ambientCharacters?.Show(
                CurrentLocation.LocationCode,
                NarrativeSceneContext);
            ambientInspectables?.Show(CurrentLocation.LocationCode);
            ambientParticles?.Show(
                CurrentLocation.AmbientParticleTint,
                CurrentLocation.BackgroundSprite);
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
            evidenceHotspots =
                presenterObject.AddComponent<EvidenceLocationHotspotOverlay>();
            evidenceHotspots.Initialize(backgroundPresenter.ContentRect);
            ambientCharacters =
                presenterObject.AddComponent<AmbientCharacterHotspotOverlay>();
            // Ambient characters belong to the photographed space. Parenting
            // them to the cover image keeps their feet and scale aligned when
            // the background is cropped, focused, or zoomed.
            ambientCharacters.Initialize(backgroundPresenter.ContentRect);
            ambientInspectables =
                presenterObject.AddComponent<AmbientInspectableOverlay>();
            ambientInspectables.Initialize(backgroundPresenter.ContentRect);
            ambientParticles =
                presenterObject.AddComponent<AmbientRoomParticleOverlay>();
            ambientParticles.Initialize(backgroundPresenter.ContentRect);
        }
    }
}
