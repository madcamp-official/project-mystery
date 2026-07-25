using System.Collections.Generic;
using UnityEngine;

namespace Wake.Exploration
{
    [CreateAssetMenu(fileName = "LocationGraph", menuName = "Wake/Location Graph")]
    public class LocationGraph : ScriptableObject
    {
        [SerializeField] private List<LocationDefinition> locations = new();
        [SerializeField] private LocationDefinition startingLocation;

        public IReadOnlyList<LocationDefinition> Locations => locations;
        public LocationDefinition StartingLocation => startingLocation;

        public LocationDefinition FindByCode(string locationCode)
        {
            foreach (LocationDefinition location in locations)
            {
                if (location != null && location.LocationCode == locationCode)
                {
                    return location;
                }
            }

            return null;
        }
    }
}
