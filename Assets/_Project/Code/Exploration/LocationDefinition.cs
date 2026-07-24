using System.Collections.Generic;
using UnityEngine;

namespace Wake.Exploration
{
    [CreateAssetMenu(fileName = "LocationDefinition", menuName = "Wake/Location Definition")]
    public class LocationDefinition : ScriptableObject
    {
        [SerializeField] private string locationCode;
        [SerializeField] private string displayName;
        [SerializeField] private GameObject contentPrefab;
        [SerializeField] private List<LocationDefinition> connectedLocations = new();

        public string LocationCode => locationCode;
        public string DisplayName => displayName;
        public GameObject ContentPrefab => contentPrefab;
        public IReadOnlyList<LocationDefinition> ConnectedLocations => connectedLocations;
    }
}
