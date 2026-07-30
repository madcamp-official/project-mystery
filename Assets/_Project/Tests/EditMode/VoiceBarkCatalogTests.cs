using System.Collections.Generic;
using System.Linq;
using NUnit.Framework;
using Wake.Narrative;

namespace Wake.Tests
{
    public class VoiceBarkCatalogTests
    {
        [Test]
        public void AllCueIds_ListsExactlyTheTwelveCues()
        {
            Assert.That(
                VoiceBarkCatalog.AllCueIds,
                Is.EqualTo(new[]
                {
                    "GREET", "ACK_POS", "ACK_NEG", "THINK", "CONFUSED",
                    "SURPRISED", "SUSPICIOUS", "LAUGH", "SIGH", "ANNOYED",
                    "WORRIED", "PAIN_EFFORT"
                }));
        }

        [TestCase(PortraitEmotion.Neutral, new[] { "ACK_POS", "SUSPICIOUS" })]
        [TestCase(PortraitEmotion.Positive, new[] { "ACK_POS", "THINK", "LAUGH" })]
        [TestCase(
            PortraitEmotion.Angry,
            new[]
            {
                "THINK", "THINK", "SUSPICIOUS", "SUSPICIOUS",
                "ACK_NEG", "ANNOYED", "SURPRISED"
            })]
        [TestCase(
            PortraitEmotion.Concerned,
            new[] { "ACK_NEG", "THINK", "CONFUSED", "SURPRISED", "SIGH", "WORRIED" })]
        public void CandidateCues_MatchesDesignTable(
            PortraitEmotion emotion,
            string[] expected)
        {
            Assert.That(VoiceBarkCatalog.CandidateCues(emotion), Is.EqualTo(expected));
        }

        [Test]
        public void CandidateCues_AngryBucketWeightsThinkAndSuspiciousHigher()
        {
            // Angry's dominant real-world tags are "focused"/"firm" (steady
            // investigation tone, not genuine anger) - THINK/SUSPICIOUS fit
            // that better than ACK_NEG/ANNOYED/SURPRISED, so they must
            // appear more than once to be picked more often by the uniform
            // random index in VoiceBarkPlayer.
            IReadOnlyList<string> angryCues = VoiceBarkCatalog.CandidateCues(
                PortraitEmotion.Angry);
            Assert.That(angryCues.Count(cue => cue == "THINK"), Is.EqualTo(2));
            Assert.That(angryCues.Count(cue => cue == "SUSPICIOUS"), Is.EqualTo(2));
        }

        [Test]
        public void CandidateCues_NeverReturnsGreetOrPainEffort()
        {
            foreach (PortraitEmotion emotion in
                     (PortraitEmotion[])System.Enum.GetValues(typeof(PortraitEmotion)))
            {
                Assert.That(
                    VoiceBarkCatalog.CandidateCues(emotion),
                    Has.None.EqualTo("GREET").And.None.EqualTo("PAIN_EFFORT"),
                    emotion.ToString());
            }
        }

        [Test]
        public void CandidateCues_OnlyEverReturnsKnownCueIds()
        {
            foreach (PortraitEmotion emotion in
                     (PortraitEmotion[])System.Enum.GetValues(typeof(PortraitEmotion)))
            {
                Assert.That(
                    VoiceBarkCatalog.CandidateCues(emotion).All(
                        cue => VoiceBarkCatalog.AllCueIds.Contains(cue)),
                    Is.True,
                    emotion.ToString());
            }
        }
    }
}
