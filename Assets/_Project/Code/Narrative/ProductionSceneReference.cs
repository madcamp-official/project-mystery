using System;
using System.Collections.Generic;
using System.Linq;

namespace Wake.Narrative
{
    public static class ProductionSceneReference
    {
        private static readonly IReadOnlyDictionary<string, string> Aliases =
            new Dictionary<string, string>(StringComparer.Ordinal)
            {
                ["D8-03_A"] = "D8-03",
                ["D8-03_B"] = "D8-03",
                ["D8-03_C"] = "D8-03",
                ["D8-03_BAD"] = "D8-03"
            };

        public static string Normalize(string sceneId)
        {
            string normalized = string.IsNullOrWhiteSpace(sceneId)
                ? string.Empty
                : sceneId.Trim().ToUpperInvariant();
            return Aliases.TryGetValue(normalized, out string canonical)
                ? canonical
                : normalized;
        }

        public static bool IsRouteSpecificEpilogue(string sceneId)
        {
            string normalized = string.IsNullOrWhiteSpace(sceneId)
                ? string.Empty
                : sceneId.Trim().ToUpperInvariant();
            return Aliases.ContainsKey(normalized);
        }

        public static IReadOnlyList<string> NormalizeDistinct(
            IEnumerable<string> sceneIds)
        {
            return (sceneIds ?? Array.Empty<string>())
                .Select(Normalize)
                .Where(sceneId => sceneId.Length > 0)
                .Distinct(StringComparer.Ordinal)
                .ToArray();
        }
    }
}
