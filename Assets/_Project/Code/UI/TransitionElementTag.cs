using UnityEngine;

namespace Wake.UI
{
    [DisallowMultipleComponent]
    public sealed class TransitionElementTag : MonoBehaviour
    {
        [SerializeField] private UiTransitionDirection direction =
            UiTransitionDirection.Auto;
        [SerializeField] private int order;
        [SerializeField, Min(.1f)] private float distanceMultiplier = 1f;
        [SerializeField] private bool exclude;

        public UiTransitionDirection Direction => direction;
        public int Order => order;
        public float DistanceMultiplier => distanceMultiplier;
        public bool Exclude => exclude;
    }
}
