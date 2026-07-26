using System.Collections.Generic;
using System;
using UnityEngine;

namespace Wake.Exploration
{
    [CreateAssetMenu(fileName = "LocationDefinition", menuName = "Wake/Location Definition")]
    public class LocationDefinition : ScriptableObject
    {
        [SerializeField] private string locationCode;
        [SerializeField] private string displayName;
        [SerializeField] private int deck;
        [SerializeField] private string roomCode;
        [SerializeField] private Sprite backgroundSprite;
        [SerializeField] private Vector2 backgroundFocus = new(0.5f, 0.5f);
        [SerializeField, Min(1f)] private float backgroundZoom = 1f;
        [SerializeField] private List<string> narrativeAliases = new();
        [SerializeField] private GameObject contentPrefab;
        [SerializeField] private List<LocationDefinition> connectedLocations = new();

        public string LocationCode => locationCode;
        public string DisplayName => displayName;
        public int Deck => deck;
        public string RoomCode => roomCode;
        public Sprite BackgroundSprite => backgroundSprite;
        public Vector2 BackgroundFocus =>
            backgroundFocus == Vector2.zero
                ? new Vector2(0.5f, 0.5f)
                : new Vector2(
                    Mathf.Clamp01(backgroundFocus.x),
                    Mathf.Clamp01(backgroundFocus.y));
        public float BackgroundZoom => Mathf.Max(1f, backgroundZoom);
        public IReadOnlyList<string> NarrativeAliases => narrativeAliases;
        public GameObject ContentPrefab => contentPrefab;
        public IReadOnlyList<LocationDefinition> ConnectedLocations => connectedLocations;

        public bool MatchesCode(string candidate)
        {
            string normalized = candidate?.Trim();
            if (string.IsNullOrEmpty(normalized))
            {
                return false;
            }

            if (string.Equals(locationCode, normalized, StringComparison.OrdinalIgnoreCase))
            {
                return true;
            }

            foreach (string alias in narrativeAliases)
            {
                if (string.Equals(alias?.Trim(), normalized, StringComparison.OrdinalIgnoreCase))
                {
                    return true;
                }
            }

            return false;
        }
    }
}
