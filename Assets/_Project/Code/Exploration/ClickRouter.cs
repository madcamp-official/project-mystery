using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem;
using Seat0A.Narrative;

namespace Seat0A.Exploration
{
    public class ClickRouter : MonoBehaviour
    {
        private Camera worldCamera;

        private void Awake()
        {
            worldCamera = Camera.main;
        }

        private void Update()
        {
            if (worldCamera == null)
            {
                worldCamera = Camera.main;
                if (worldCamera == null)
                {
                    return;
                }
            }

            if (Mouse.current == null || !Mouse.current.leftButton.wasPressedThisFrame)
            {
                return;
            }

            if (EventSystem.current != null && EventSystem.current.IsPointerOverGameObject())
            {
                return;
            }

            DialogueController dialogue = DialogueController.Instance;
            if (dialogue != null && dialogue.IsBusy)
            {
                return;
            }

            Vector2 screenPos = Mouse.current.position.ReadValue();
            Vector2 worldPos = worldCamera.ScreenToWorldPoint(screenPos);
            Collider2D hit = Physics2D.OverlapPoint(worldPos);
            if (hit != null && hit.TryGetComponent(out Hotspot hotspot))
            {
                hotspot.Interact();
            }
        }
    }
}
