using UnityEngine;
using UnityEngine.UI;
using TMPro;
using Wake.Core;
using Wake.Exploration;

namespace Wake.UI
{
    public class MapController : MonoBehaviour
    {
        [SerializeField] private LocationGraph locationGraph;

        public SceneTravelResult LastTravelResult { get; private set; }

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
            GameStateManager state = GameStateManager.Instance;
            LastTravelResult = SceneTravelPolicy.EvaluateLocation(
                location,
                state != null ? state.PublicAnxiety : 0);
            if (TryLoadAllowedDestination(LastTravelResult))
            {
                UIManager.Instance?.ShowIngame();
            }
        }

        public SceneTravelResult TryTravelToScene(string sceneId)
        {
            GameStateManager state = GameStateManager.Instance;
            LastTravelResult = SceneTravelPolicy.EvaluateScene(
                sceneId,
                locationGraph,
                state?.CompletedProductionSceneIds,
                state != null ? state.PublicAnxiety : 0);
            if (!TryLoadAllowedDestination(LastTravelResult))
            {
                return LastTravelResult;
            }

            if (state != null && LastTravelResult.Scene.Day > 0)
            {
                state.SetTime(
                    LastTravelResult.Scene.Day,
                    LastTravelResult.Scene.TimeBlock);
            }

            InvestigationEventHub.Publish(
                InvestigationEventKind.SceneEntered,
                LastTravelResult.Scene.SceneId,
                LastTravelResult.Location.LocationCode);
            UIManager.Instance?.ShowIngame();
            return LastTravelResult;
        }

        private bool TryLoadAllowedDestination(SceneTravelResult result)
        {
            if (!result.IsAllowed || LocationLoader.Instance == null ||
                LocationLoader.Instance.TryLoadLocation(result.Location, out _))
            {
                return result.IsAllowed && LocationLoader.Instance != null;
            }

            LastTravelResult = SceneTravelResult.Denied(
                SceneAccessDenialReason.LocationLoadFailed,
                $"Location '{result.Location.LocationCode}' could not load visual content.",
                result.Scene,
                result.Location);
            return false;
        }
    }
}
