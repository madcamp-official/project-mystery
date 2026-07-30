using System.Linq;
using NUnit.Framework;
using UnityEngine;
using Wake.Core;
using Wake.Narrative;

namespace Wake.Tests
{
    public sealed class AmbientBarkCatalogTests
    {
        private GameObject host;
        private GameStateManager state;

        [SetUp]
        public void SetUp()
        {
            host = new GameObject("AmbientBarkCatalogTestState");
            state = host.AddComponent<GameStateManager>();
            state.StartNewGame();
        }

        [TearDown]
        public void TearDown()
        {
            Object.DestroyImmediate(host);
        }

        [Test]
        public void ChapterGreaterOrEqual_UsesBareIntegerAfterNormalization()
        {
            state.SetTime(1, TimeBlock.AM);
            Assert.That(
                AmbientBarkCatalog
                    .GetAvailable("HORIZON", state, maximum: 10)
                    .Any(entry => entry.Id == "HORIZON_CLOSED"),
                Is.False);

            state.SetTime(2, TimeBlock.AM);
            Assert.That(
                AmbientBarkCatalog
                    .GetAvailable("HORIZON", state, maximum: 10)
                    .Any(entry => entry.Id == "HORIZON_CLOSED"),
                Is.True);
        }

        [Test]
        public void CompoundAnxietyBand_StillMatchesAsTwoClauses()
        {
            SetAnxiety(state, 50);
            Assert.That(
                AmbientBarkCatalog
                    .GetAvailable("MEDBAY", state, maximum: 10)
                    .Any(entry => entry.Id == "MEDBAY_SECURITY"),
                Is.True);

            SetAnxiety(state, 80);
            Assert.That(
                AmbientBarkCatalog
                    .GetAvailable("MEDBAY", state, maximum: 10)
                    .Any(entry => entry.Id == "MEDBAY_SECURITY"),
                Is.False);
        }

        [Test]
        public void EveryLocation_HasBarksAcrossAllThreeDayBands()
        {
            foreach (string location in AmbientBarkCatalog.SupportedLocations)
            {
                state.SetTime(1, TimeBlock.AM);
                Assert.That(
                    AmbientBarkCatalog.GetAvailable(
                        location, state, maximum: 10),
                    Is.Not.Empty,
                    $"{location} day 1");

                state.SetTime(3, TimeBlock.AM);
                Assert.That(
                    AmbientBarkCatalog.GetAvailable(
                        location, state, maximum: 10),
                    Is.Not.Empty,
                    $"{location} day 3");

                state.SetTime(7, TimeBlock.AM);
                Assert.That(
                    AmbientBarkCatalog.GetAvailable(
                        location, state, maximum: 10),
                    Is.Not.Empty,
                    $"{location} day 7");
            }
        }

        [Test]
        public void NoArchetypeOccupiesMultipleLocationsWithinTheSameDayTier()
        {
            var byTierAndSpeaker = new System.Collections.Generic
                .Dictionary<(string Tier, string Speaker),
                    System.Collections.Generic.HashSet<string>>();

            foreach (AmbientBarkRecord entry in AmbientBarkCatalog.All)
            {
                string tier = ClassifyDayTier(entry.Condition);
                if (tier == null)
                {
                    continue;
                }

                var key = (tier, entry.Speaker);
                if (!byTierAndSpeaker.TryGetValue(
                        key,
                        out System.Collections.Generic.HashSet<string> locations))
                {
                    locations = new System.Collections.Generic.HashSet<string>();
                    byTierAndSpeaker[key] = locations;
                }

                locations.Add(entry.Location);
            }

            string[] violations = byTierAndSpeaker
                .Where(pair => pair.Value.Count > 1)
                .Select(pair =>
                    $"{pair.Key.Speaker} in tier {pair.Key.Tier}: " +
                    string.Join(", ", pair.Value))
                .ToArray();

            Assert.That(violations, Is.Empty);
        }

        private static string ClassifyDayTier(string condition)
        {
            if (condition == "always")
            {
                return "D1";
            }

            if (condition == "chapter>=5")
            {
                return "LATE";
            }

            if (condition == "chapter>=2 and chapter<=4")
            {
                return "MID";
            }

            return null;
        }

        private static void SetAnxiety(GameStateManager target, int value)
        {
            int delta = value - target.PublicAnxiety;
            if (delta != 0)
            {
                target.ChangePublicAnxiety(delta);
            }
        }
    }
}
