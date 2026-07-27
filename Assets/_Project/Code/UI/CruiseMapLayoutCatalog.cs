using System;
using System.Collections.Generic;
using UnityEngine;

namespace Wake.UI
{
    public static class CruiseMapLayoutCatalog
    {
        private static readonly IReadOnlyDictionary<string, Vector2> Positions =
            new Dictionary<string, Vector2>(StringComparer.Ordinal)
            {
                ["PORT"] = new(0.045f, 0.15f),
                ["GANGWAY"] = new(0.125f, 0.30f),
                ["RICHARD_SUITE"] = new(0.29f, 0.75f),
                ["VIP_LOUNGE"] = new(0.55f, 0.75f),
                ["OPEN_DECK"] = new(0.82f, 0.75f),
                ["BALLROOM"] = new(0.28f, 0.61f),
                ["DINING"] = new(0.51f, 0.61f),
                ["PROMENADE"] = new(0.68f, 0.61f),
                ["HORIZON"] = new(0.85f, 0.61f),
                ["ATRIUM"] = new(0.28f, 0.48f),
                ["NEWS_LOUNGE"] = new(0.50f, 0.48f),
                ["SECURITY"] = new(0.68f, 0.48f),
                ["SERVICE_RAIL"] = new(0.86f, 0.48f),
                ["MEDBAY"] = new(0.27f, 0.36f),
                ["BALLAST_CONTROL_ANNEX"] = new(0.49f, 0.36f),
                ["ENGINE_CONTROL"] = new(0.69f, 0.36f),
                ["CREW_STAIRS"] = new(0.87f, 0.36f),
                ["VAULT"] = new(0.27f, 0.24f),
                ["ARCHIVE"] = new(0.49f, 0.24f),
                ["LAUNDRY"] = new(0.69f, 0.24f),
                ["SERVICE_HUB"] = new(0.87f, 0.24f),
                ["STABILIZERS"] = new(0.27f, 0.11f),
                ["BALLAST_TANKS"] = new(0.49f, 0.11f),
                ["GENERATOR"] = new(0.69f, 0.11f),
                ["WORKSHOP"] = new(0.87f, 0.11f)
            };

        public static Vector2 PositionFor(string locationCode)
        {
            return locationCode != null &&
                   Positions.TryGetValue(locationCode, out Vector2 position)
                ? position
                : new Vector2(0.5f, 0.5f);
        }
    }
}
