using System.Collections;
using UnityEngine;

namespace Wake.UI
{
    [DisallowMultipleComponent]
    public sealed class LobbyRevealSequence : MonoBehaviour
    {
        private const float Duration = 0.45f;

        private RectTransform titlePanel;
        private RectTransform revealGroup;
        private Transform water;
        private float travelDistance;
        private bool played;

        public static float ComputeWorldHeight(RectTransform canvasRect)
        {
            return canvasRect.sizeDelta.y * canvasRect.lossyScale.y;
        }

        public void Configure(
            RectTransform titlePanel,
            RectTransform revealGroup,
            Transform water,
            RectTransform canvasRect)
        {
            this.titlePanel = titlePanel;
            this.revealGroup = revealGroup;
            this.water = water;
            travelDistance = canvasRect.sizeDelta.y;
            played = false;
        }

        public void Play()
        {
            if (played || titlePanel == null || revealGroup == null)
            {
                return;
            }
            played = true;
            StartCoroutine(Animate());
        }

        private IEnumerator Animate()
        {
            Vector2 titleStart = titlePanel.anchoredPosition;
            Vector2 titleEnd = titleStart + new Vector2(0f, travelDistance);
            Vector2 revealStart = revealGroup.anchoredPosition;
            Vector2 revealEnd = revealStart + new Vector2(0f, travelDistance);
            float waterWorldTravel = water != null
                ? travelDistance * titlePanel.root.localScale.y
                : 0f;
            Vector3 waterStart = water != null ? water.position : default;
            Vector3 waterEnd = waterStart + new Vector3(0f, waterWorldTravel, 0f);

            float elapsed = 0f;
            while (elapsed < Duration)
            {
                elapsed += Time.unscaledDeltaTime;
                float t = Mathf.SmoothStep(0f, 1f, elapsed / Duration);
                titlePanel.anchoredPosition =
                    Vector2.LerpUnclamped(titleStart, titleEnd, t);
                revealGroup.anchoredPosition =
                    Vector2.LerpUnclamped(revealStart, revealEnd, t);
                if (water != null)
                {
                    water.position =
                        Vector3.LerpUnclamped(waterStart, waterEnd, t);
                }
                yield return null;
            }
            titlePanel.anchoredPosition = titleEnd;
            revealGroup.anchoredPosition = revealEnd;
            if (water != null)
            {
                water.position = waterEnd;
            }
        }
    }
}
