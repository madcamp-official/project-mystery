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

        private GameObject currentInstance;
        private Transform container;
        private BackgroundCoverPresenter backgroundPresenter;

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
            CurrentLocation = location;
            AudioManager.Instance?.PlayLocationTheme(location.LocationCode);
            GameStateManager.Instance?.RecordLocation(location.LocationCode);
            failure = LoadFailure.None;
            return true;
        }

        private void CreateBackgroundPresenter()
        {
            GameObject canvasObject = new("LocationBackgroundCanvas", typeof(Canvas));
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
        }
    }
}
