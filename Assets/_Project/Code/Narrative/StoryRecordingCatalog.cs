using System.Collections.Generic;

namespace Wake.Narrative
{
    public static class StoryRecordingCatalog
    {
        private static readonly IReadOnlyDictionary<string, string> PathByStableLineId =
            new Dictionary<string, string>
            {
                ["d2_06_10"] = "D2-06_DANIEL_CHAT_01",
                ["d5_03_10"] = "D5-03_DANIEL_CHAT_01"
            };

        public static bool TryGet(string stableLineId, out string resourcePath) =>
            PathByStableLineId.TryGetValue(
                stableLineId ?? string.Empty, out resourcePath);
    }
}
