using System.Collections.Generic;
using NUnit.Framework;
using UnityEngine;
using Wake.Narrative;

namespace Wake.Tests
{
    public class VoiceBarkPlayerTests
    {
        private sealed class FakeClipProvider : IVoiceBarkClipProvider
        {
            public Dictionary<(string character, string cue), AudioClip[]> Clips = new();

            public IReadOnlyList<AudioClip> GetClips(string characterId, string cueId) =>
                Clips.TryGetValue((characterId, cueId), out AudioClip[] clips)
                    ? clips
                    : System.Array.Empty<AudioClip>();
        }

        private GameObject host;
        private AudioSource source;

        [SetUp]
        public void SetUp()
        {
            host = new GameObject("VoiceBarkPlayerTests");
            source = host.AddComponent<AudioSource>();
        }

        [TearDown]
        public void TearDown()
        {
            Object.DestroyImmediate(host);
        }

        private static AudioClip FakeClip(string name)
        {
            var clip = AudioClip.Create(name, 1, 1, 44100, false);
            clip.name = name;
            return clip;
        }

        [Test]
        public void NewSpeakerTurn_AlwaysUsesGreetCue()
        {
            var provider = new FakeClipProvider();
            provider.Clips[("DANIEL", "GREET")] = new[] { FakeClip("DANIEL_GREET_01") };
            var player = new VoiceBarkPlayer(
                provider, source, randomIndexBelow: _ => 0);

            bool played = player.TryPlayBark(
                "DANIEL", PortraitEmotion.Angry, isNewSpeakerTurn: true, currentTime: 0f);

            // PlayOneShot doesn't set AudioSource.clip (that field is only
            // for the persistent/looping clip) - the return value is the
            // only observable signal that a clip was actually selected and
            // handed to PlayOneShot.
            Assert.That(played, Is.True);
        }

        [Test]
        public void MissingFolder_IsASilentNoOp()
        {
            var provider = new FakeClipProvider();
            var player = new VoiceBarkPlayer(
                provider, source, randomIndexBelow: _ => 0);

            bool played = player.TryPlayBark(
                "EVELYN", PortraitEmotion.Neutral, isNewSpeakerTurn: true, currentTime: 0f);

            Assert.That(played, Is.False);
        }

        [Test]
        public void EveryEligibleLine_PlaysWithNoCoverageSkip()
        {
            var provider = new FakeClipProvider();
            provider.Clips[("DANIEL", "ACK_POS")] = new[] { FakeClip("DANIEL_ACK_POS_01") };
            var player = new VoiceBarkPlayer(
                provider, source, randomIndexBelow: _ => 0);

            float t = 0f;
            for (int i = 0; i < 10; i++)
            {
                bool played = player.TryPlayBark(
                    "DANIEL", PortraitEmotion.Neutral, isNewSpeakerTurn: false, currentTime: t);
                Assert.That(played, Is.True, $"call {i}");
                t += CooldownSecondsForTest;
            }
        }

        private const float CooldownSecondsForTest = 1.5f;

        [Test]
        public void Cooldown_BlocksSecondPlayWithin1Point4Seconds()
        {
            var provider = new FakeClipProvider();
            provider.Clips[("DANIEL", "GREET")] = new[]
            {
                FakeClip("DANIEL_GREET_01"), FakeClip("DANIEL_GREET_02")
            };
            var player = new VoiceBarkPlayer(
                provider, source, randomIndexBelow: _ => 0);

            Assert.That(
                player.TryPlayBark(
                    "DANIEL", PortraitEmotion.Neutral, true, currentTime: 0f),
                Is.True);
            Assert.That(
                player.TryPlayBark(
                    "DANIEL", PortraitEmotion.Neutral, true, currentTime: 1.0f),
                Is.False);
            Assert.That(
                player.TryPlayBark(
                    "DANIEL", PortraitEmotion.Neutral, true, currentTime: 1.5f),
                Is.True);
        }

        [Test]
        public void RepeatGuard_ExcludesLastThreePlayedClipsForSameCharacter()
        {
            var provider = new FakeClipProvider();
            provider.Clips[("DANIEL", "ACK_POS")] = new[]
            {
                FakeClip("A"), FakeClip("B"), FakeClip("C"), FakeClip("D")
            };
            var seenIndexes = new List<int>();
            var player = new VoiceBarkPlayer(
                provider,
                source,
                randomIndexBelow: max =>
                {
                    seenIndexes.Add(max);
                    return 0;
                });

            float t = 0f;
            for (int i = 0; i < 3; i++)
            {
                player.TryPlayBark(
                    "DANIEL", PortraitEmotion.Neutral, false, currentTime: t);
                t += 2f;
            }

            // Pool of 4 clips, 3 already played -> exactly 1 candidate left,
            // so the 4th pick's randomIndexBelow call must receive max == 1.
            player.TryPlayBark("DANIEL", PortraitEmotion.Neutral, false, currentTime: t);

            Assert.That(seenIndexes[^1], Is.EqualTo(1));
        }
    }
}
