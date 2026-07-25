using UnityEngine;
using Wake.Core;

namespace Wake.Exploration
{
    public class LocationLoader : MonoBehaviour
    {
        public static LocationLoader Instance { get; private set; }

        public LocationDefinition CurrentLocation { get; private set; }

        private GameObject currentInstance;
        private Transform container;

        private void Awake()
        {
            Instance = this;
            container = new GameObject("LocationContainer").transform;
        }

        public void LoadLocation(LocationDefinition location)
        {
            if (location == null || location == CurrentLocation || location.ContentPrefab == null)
            {
                return;
            }

            if (currentInstance != null)
            {
                Destroy(currentInstance);
            }

            currentInstance = Instantiate(location.ContentPrefab, container);
            CurrentLocation = location;
            AudioManager.Instance?.PlayLocationTheme(location.LocationCode);
            GameStateManager.Instance?.RecordLocation(location.LocationCode);
        }
    }
}
