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
                [PortraitEmotion.Positive] = new[] { "ACK_POS", "LAUGH" },
                [PortraitEmotion.Angry] = new[]
                {
                    "ACK_NEG", "THINK", "SURPRISED", "SUSPICIOUS", "ANNOYED"
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
