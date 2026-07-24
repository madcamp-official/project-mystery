using UnityEngine;
using UnityEngine.UI;
using TMPro;
using Wake.Exploration;

namespace Wake.UI
{
    public class MapController : MonoBehaviour
    {
        [SerializeField] private LocationGraph locationGraph;

        private void Start()
        {
            if (locationGraph == null)
            {
                Debug.LogWarning("MapController has no LocationGraph assigned.");
                return;
            }

            Transform canvas = GameObject.Find("Canvas").transform;
            Transform roomsContainer = canvas.Find("Map/Rooms");
            Button[] buttons = roomsContainer.GetComponentsInChildren<Button>(true);
            var locations = locationGraph.Locations;

            for (int i = 0; i < buttons.Length; i++)
            {
                if (i >= locations.Count)
                {
                    buttons[i].gameObject.SetActive(false);
                    continue;
                }

                LocationDefinition location = locations[i];
                TMP_Text label = buttons[i].GetComponentInChildren<TMP_Text>();
                if (label != null)
                {
                    label.text = location.DisplayName;
                }

                buttons[i].onClick.AddListener(() => SelectLocation(location));
            }

            if (locations.Count > buttons.Length)
            {
                Debug.LogWarning($"LocationGraph has {locations.Count} locations but Map only exposes {buttons.Length} button slots.");
            }
        }

        private void SelectLocation(LocationDefinition location)
        {
            LocationLoader.Instance.LoadLocation(location);
            UIManager.Instance.ShowIngame();
        }
    }
}
