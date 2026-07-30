using System.Collections.Generic;

namespace Wake.Narrative
{
    public static class VoiceBarkCatalog
    {
        public static IReadOnlyList<string> AllCueIds { get; } = new[]
        {
            "GREET", "ACK_POS", "ACK_NEG", "THINK", "CONFUSED",
            "SURPRISED", "SUSPICIOUS", "LAUGH", "SIGH", "ANNOYED",
            "WORRIED", "PAIN_EFFORT"
        };

        private static readonly IReadOnlyDictionary<PortraitEmotion, string[]>
            CandidatesByEmotion = new Dictionary<PortraitEmotion, string[]>
            {
                [PortraitEmotion.Neutral] = new[] { "ACK_POS", "SUSPICIOUS" },
                // "realization" is this bucket's single largest tag (23 of
                // 58 lines) - an insight/"aha" beat fits THINK far better
                // than the flavor-only ACK_POS/LAUGH pair alone.
                [PortraitEmotion.Positive] = new[] { "ACK_POS", "THINK", "LAUGH" },
                // Dominated by "focused"/"firm" (234 of 362 lines) - steady,
                // concentrated investigation tone, not genuine anger. THINK
                // and SUSPICIOUS fit both that and the smaller real-anger
                // tags (angry/corrective/commanding/...), so they're
                // duplicated to weight selection toward them; ACK_NEG/
                // ANNOYED/SURPRISED stay in the pool once each for the
                // genuinely angry lines.
                [PortraitEmotion.Angry] = new[]
                {
                    "THINK", "THINK", "SUSPICIOUS", "SUSPICIOUS",
                    "ACK_NEG", "ANNOYED", "SURPRISED"
                },
                [PortraitEmotion.Concerned] = new[]
                {
                    "ACK_NEG", "THINK", "CONFUSED", "SURPRISED", "SIGH",
                    "WORRIED"
                }
            };

        public static IReadOnlyList<string> CandidateCues(PortraitEmotion emotion) =>
            CandidatesByEmotion.TryGetValue(emotion, out string[] cues)
                ? cues
                : System.Array.Empty<string>();
    }
}
