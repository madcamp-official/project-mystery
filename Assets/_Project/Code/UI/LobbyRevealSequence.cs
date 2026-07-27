using System.Collections;
using UnityEngine;

namespace Wake.UI
{
    [DisallowMultipleComponent]
    public sealed class LobbyRevealSequence : MonoBehaviour
    {
        public static float ComputeWorldHeight(RectTransform canvasRect)
        {
            return canvasRect.sizeDelta.y * canvasRect.lossyScale.y;
        }
    }
}
