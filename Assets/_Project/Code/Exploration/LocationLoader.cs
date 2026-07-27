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
        public bool IsPresentationVisible =>
            container != null && container.gameObject.activeSelf;

        private GameObject currentInstance;
        private Transform container;
        private BackgroundCoverPresenter backgroundPresenter;
        private EvidenceLocationHotspotOverlay evidenceHotspots;
        private AmbientCharacterHotspotOverlay ambientCharacters;
        private AmbientInspectableOverlay ambientInspectables;

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
            ambientCharacters?.Show(location.LocationCode);
            ambientInspectables?.Show(location.LocationCode);
            CurrentLocation = location;
            AudioManager.Instance?.PlayLocationTheme(location.LocationCode);
            GameStateManager.Instance?.RecordLocation(location.LocationCode);
            failure = LoadFailure.None;
            return true;
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
            // Character buttons are navigation UI, not image-space hotspots.
            // Parenting them to the cover image makes its crop/focus offset
            // move the buttons beyond the visible screen edge.
            ambientCharacters.Initialize(backgroundPresenter.ViewportRect);
            ambientInspectables =
                presenterObject.AddComponent<AmbientInspectableOverlay>();
            ambientInspectables.Initialize(backgroundPresenter.ContentRect);
        }
    }
}
