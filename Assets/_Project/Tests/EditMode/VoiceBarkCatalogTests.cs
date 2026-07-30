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
        [TestCase(PortraitEmotion.Positive, new[] { "ACK_POS", "LAUGH" })]
        [TestCase(
            PortraitEmotion.Angry,
            new[] { "ACK_NEG", "THINK", "SURPRISED", "SUSPICIOUS", "ANNOYED" })]
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
