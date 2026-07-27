using System.Linq;
using NUnit.Framework;
using UnityEngine;
using Wake.Core;
using Wake.Narrative;

namespace Wake.Tests
{
    public sealed class OfficialFourEndingContractTests
    {
        private const string SaveKey = "UNDER_THE_HORIZON_GAME_STATE_V2";
        private const string LegacyKey = "THE_WAKE_GAME_STATE_V1";
        private GameObject host;
        private GameStateManager state;

        [SetUp]
        public void SetUp()
        {
            DestroyManager();
            PlayerPrefs.DeleteKey(SaveKey);
            PlayerPrefs.DeleteKey(LegacyKey);
            host = new GameObject("OfficialFourEndingContractTests");
            state = host.AddComponent<GameStateManager>();
            state.StartNewGame();
        }

        [TearDown]
        public void TearDown()
        {
            DestroyManager();
            PlayerPrefs.DeleteKey(SaveKey);
            PlayerPrefs.DeleteKey(LegacyKey);
        }

        [Test]
        public void Catalog_ContainsExactlyOfficialABCBadRoutes()
        {
            Assert.That(ProductionEndingCatalog.All, Has.Count.EqualTo(4));
            Assert.That(
                ProductionEndingCatalog.All.Select(item =>
                    FinalAccusationResolver.ToOfficialRoute(item.EndingId)),
                Is.EqualTo(new[] { "A", "B", "C", "Bad" }));
            Assert.That(
                ProductionEndingCatalog.All.Select(item => item.EndingId),
                Is.Unique);
        }

        [TestCase("A", "Complete Wake")]
        [TestCase("B", "Convenient Culprit")]
        [TestCase("C", "The Wrong Man")]
        [TestCase("Bad", "Panic at Sea")]
        public void Catalog_ResolvesOfficialRouteTokensAndTitles(
            string route,
            string expectedTitle)
        {
            Assert.That(
                ProductionEndingCatalog.TryGet(
                    route,
                    out ProductionEndingDefinition ending),
                Is.True);
            Assert.That(ending.Title, Is.EqualTo(expectedTitle));
            Assert.That(
                FinalAccusationResolver.ToOfficialRoute(ending.EndingId),
                Is.EqualTo(route));
        }

        [TestCase("A", FinalAccusationResolver.CompleteEndingId)]
        [TestCase("ending:A", FinalAccusationResolver.CompleteEndingId)]
        [TestCase("B", FinalAccusationResolver.ConvenientEndingId)]
        [TestCase("ending:B", FinalAccusationResolver.ConvenientEndingId)]
        [TestCase("C", FinalAccusationResolver.WrongPersonEndingId)]
        [TestCase("ending:C", FinalAccusationResolver.WrongPersonEndingId)]
        [TestCase("Bad", FinalAccusationResolver.BadEndingId)]
        [TestCase("ending:bad", FinalAccusationResolver.BadEndingId)]
        [TestCase(
            FinalAccusationResolver.LegacyIntegrityEndingId,
            FinalAccusationResolver.BadEndingId)]
        public void OfficialAndLegacyTokens_NormalizeToCanonicalIds(
            string source,
            string expected)
        {
            Assert.That(
                FinalAccusationResolver.NormalizeEndingId(source),
                Is.EqualTo(expected));
        }

        [Test]
        public void DialogueEffect_StoresCanonicalEndingAndConditionReadsRoute()
        {
            var executor = new ProductionEffectExecutor(state);
            ProductionEffectExecutionResult result = executor.Execute("ending:B");
            var conditions = new ProductionConditionEvaluator(state);

            Assert.That(result.Success, Is.True);
            Assert.That(
                state.FinalEndingId,
                Is.EqualTo(FinalAccusationResolver.ConvenientEndingId));
            Assert.That(conditions.Evaluate("ending:B").IsMet, Is.True);
            Assert.That(conditions.Evaluate("ending:A").IsMet, Is.False);
        }

        [Test]
        public void AnxietyAndIntegrityFailures_ResolveToSameOfficialBadEnding()
        {
            state.ChangePublicAnxiety(100);
            FinalAccusationResult anxiety =
                new FinalAccusationResolver(state).Resolve(new FinalAccusation());

            Assert.That(anxiety.Ending, Is.EqualTo(FinalEnding.Bad));
            Assert.That(anxiety.EndingId, Is.EqualTo(FinalAccusationResolver.BadEndingId));

            state.StartNewGame();
            state.ChangeEvidenceIntegrity(-100);
            FinalAccusationResult integrity =
                new FinalAccusationResolver(state).Resolve(new FinalAccusation());

            Assert.That(integrity.Ending, Is.EqualTo(FinalEnding.Bad));
            Assert.That(
                integrity.EndingId,
                Is.EqualTo(FinalAccusationResolver.BadEndingId));
        }

        [Test]
        public void LegacyIntegrityEnding_MigratesToOfficialBadRoute()
        {
            DestroyManager();
            PlayerPrefs.DeleteKey(SaveKey);
            PlayerPrefs.SetString(
                LegacyKey,
                "{\"finalEndingId\":\"ending_bad_integrity\"}");
            host = new GameObject("MigratedFourEndingContract");
            state = host.AddComponent<GameStateManager>();
            state.ReloadSavedState();

            Assert.That(
                state.FinalEndingId,
                Is.EqualTo(FinalAccusationResolver.BadEndingId));
            Assert.That(
                FinalAccusationResolver.ToOfficialRoute(state.FinalEndingId),
                Is.EqualTo("Bad"));
            Assert.That(
                PlayerPrefs.GetString(SaveKey),
                Does.Not.Contain("ending_bad_integrity"));
        }

        [Test]
        public void OnlyAAndBOpenConfessionBeforeEpilogue()
        {
            Assert.That(
                ProductionEndingCatalog.GetNextDialogueScene("A", false, false),
                Is.EqualTo("D8-02"));
            Assert.That(
                ProductionEndingCatalog.GetNextDialogueScene("B", false, false),
                Is.EqualTo("D8-02"));
            Assert.That(
                ProductionEndingCatalog.GetNextDialogueScene("C", false, false),
                Is.Empty);
            Assert.That(
                ProductionEndingCatalog.GetNextDialogueScene("Bad", false, false),
                Is.Empty);
        }

        private void DestroyManager()
        {
            if (host != null)
            {
                Object.DestroyImmediate(host);
            }
            if (GameStateManager.Instance != null)
            {
                Object.DestroyImmediate(GameStateManager.Instance.gameObject);
            }
            host = null;
            state = null;
        }
    }
}
