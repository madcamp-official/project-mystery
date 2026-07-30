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
