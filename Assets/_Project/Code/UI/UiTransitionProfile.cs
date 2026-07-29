using UnityEngine;

namespace Wake.UI
{
    public enum UiTransitionDirection
    {
        Auto,
        Left,
        Right,
        Up,
        Down,
        Scale
    }

    public enum UiTransitionCover
    {
        None,
        Fade
    }

    [CreateAssetMenu(
        fileName = "UiTransitionProfile",
        menuName = "Wake/UI/Transition Profile")]
    public sealed class UiTransitionProfile : ScriptableObject
    {
        [Header("Timing")]
        [SerializeField, Min(0f)] private float outDuration = .24f;
        [SerializeField, Min(0f)] private float inDuration = .34f;
        [SerializeField, Min(0f)] private float stagger = .03f;

        [Header("Motion")]
        [SerializeField, Min(0f)] private float minimumTravel = 72f;
        [SerializeField] private UiTransitionDirection defaultDirection =
            UiTransitionDirection.Auto;

        [Header("Cover")]
        [SerializeField] private UiTransitionCover cover =
            UiTransitionCover.None;
        [SerializeField, Min(0f)] private float coverDuration = .12f;
        [SerializeField] private Color coverColor =
            new(3f / 255f, 8f / 255f, 18f / 255f, 1f);

        public float OutDuration => outDuration;
        public float InDuration => inDuration;
        public float Stagger => stagger;
        public float MinimumTravel => minimumTravel;
        public UiTransitionDirection DefaultDirection => defaultDirection;
        public UiTransitionCover Cover => cover;
        public float CoverDuration => coverDuration;
        public Color CoverColor => coverColor;

        public static UiTransitionProfile CreateRuntimeDefault()
        {
            UiTransitionProfile profile = CreateInstance<UiTransitionProfile>();
            profile.name = "Runtime Default UI Transition";
            profile.hideFlags = HideFlags.HideAndDontSave;
            return profile;
        }
    }
}
