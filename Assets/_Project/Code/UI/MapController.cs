using UnityEngine;
using UnityEngine.UI;
using TMPro;
using Seat0A.Exploration;

namespace Seat0A.UI
{
    public class MapController : MonoBehaviour
    {
        [SerializeField] private RoomGraph roomGraph;

        private void Start()
        {
            if (roomGraph == null)
            {
                Debug.LogWarning("MapController has no RoomGraph assigned.");
                return;
            }

            Transform canvas = GameObject.Find("Canvas").transform;
            Transform roomsContainer = canvas.Find("Map/Rooms");
            Button[] buttons = roomsContainer.GetComponentsInChildren<Button>(true);
            var rooms = roomGraph.Rooms;

            for (int i = 0; i < buttons.Length; i++)
            {
                if (i >= rooms.Count)
                {
                    buttons[i].gameObject.SetActive(false);
                    continue;
                }

                RoomDefinition room = rooms[i];
                TMP_Text label = buttons[i].GetComponentInChildren<TMP_Text>();
                if (label != null)
                {
                    label.text = room.DisplayName;
                }

                buttons[i].onClick.AddListener(() => SelectRoom(room));
            }

            if (rooms.Count > buttons.Length)
            {
                Debug.LogWarning($"RoomGraph has {rooms.Count} rooms but Map only exposes {buttons.Length} button slots. Extra rooms are not shown yet.");
            }
        }

        private void SelectRoom(RoomDefinition room)
        {
            RoomLoader.Instance.LoadRoom(room);
            UIManager.Instance.ShowIngame();
        }
    }
}
