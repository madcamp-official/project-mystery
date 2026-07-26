using UnityEngine;
using UnityEngine.UI;
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
        private Image backgroundImage;

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

            if (backgroundImage == null)
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
            backgroundImage.sprite = location.BackgroundSprite;
            backgroundImage.gameObject.SetActive(location.BackgroundSprite != null);
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

            GameObject imageObject = new("LocationBackground", typeof(RectTransform), typeof(Image));
            imageObject.transform.SetParent(canvasObject.transform, false);
            RectTransform rect = imageObject.GetComponent<RectTransform>();
            rect.anchorMin = Vector2.zero;
            rect.anchorMax = Vector2.one;
            rect.offsetMin = Vector2.zero;
            rect.offsetMax = Vector2.zero;
            backgroundImage = imageObject.GetComponent<Image>();
            backgroundImage.preserveAspect = true;
            backgroundImage.raycastTarget = false;
            backgroundImage.gameObject.SetActive(false);
        }
    }
}
