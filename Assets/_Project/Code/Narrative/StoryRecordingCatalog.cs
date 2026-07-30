using System.Collections.Generic;

namespace Wake.Narrative
{
    public static class StoryRecordingCatalog
    {
        private static readonly IReadOnlyDictionary<string, string> PathByStableLineId =
            new Dictionary<string, string>
            {
                ["d2_06_09"] = "D2-06_ANON_CHAT_01",
                ["d2_06_10"] = "D2-06_DANIEL_CHAT_01",
                ["d2_06_11"] = "D2-06_ANON_CHAT_02",
                ["d5_03_09"] = "D5-03_ANON_CHAT_01",
                ["d5_03_10"] = "D5-03_DANIEL_CHAT_01",
                ["d5_03_11"] = "D5-03_ANON_CHAT_02",
                ["d4_01_21"] = "D4-01_EVELYN_MESSAGE_01"
            };

        public static bool TryGet(string stableLineId, out string resourcePath) =>
            PathByStableLineId.TryGetValue(
                stableLineId ?? string.Empty, out resourcePath);
    }
}
