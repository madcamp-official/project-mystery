using System.Collections.Generic;
using NUnit.Framework;
using UnityEngine;
using Wake.Core;
using Wake.Evidence;
using Wake.Narrative;

namespace Wake.Tests
{
    public sealed class ProductionEffectExecutorTests
    {
        private const string SaveKey = "UNDER_THE_HORIZON_GAME_STATE_V2";
        private const string LegacyKey = "THE_WAKE_GAME_STATE_V1";

        private readonly HashSet<string> grantedEvidence = new();
        private GameObject host;
        private GameStateManager state;
        private ProductionEffectExecutor executor;

        [SetUp]
        public void SetUp()
        {
            DestroyManager();
            PlayerPrefs.DeleteKey(SaveKey);
            PlayerPrefs.DeleteKey(LegacyKey);
            host = new GameObject("ProductionEffectExecutorTests");
            state = host.AddComponent<GameStateManager>();
            state.StartNewGame();
            executor = new ProductionEffectExecutor(
                state,
                evidenceId => grantedEvidence.Add(evidenceId));
        }

        [TearDown]
        public void TearDown()
        {
            DestroyManager();
            PlayerPrefs.DeleteKey(SaveKey);
            PlayerPrefs.DeleteKey(LegacyKey);
        }

        [Test]
        public void CompoundOfficialEffect_AppliesEveryPersistentStateKind()
        {
            ProductionEffectExecutionResult result = executor.Execute(
                "trust_claire:+2; hostility_claire:+1; publicAnxiety:+10; " +
                "evidenceIntegrity:-5; flag:claire_cooperates; evidence:C-01,C-02; " +
                "scene_unlock:D2-02,D2-04; theory:horizon_no_live_third_party; " +
                "wrong_strike:+1; accusation1:correct; question_used; ending:A");

            Assert.That(result.Success, Is.True);
            Assert.That(result.AppliedInstructionCount, Is.EqualTo(12));
            Assert.That(state.GetTrust("CLAIRE"), Is.EqualTo(4));
            Assert.That(state.GetRuntimeCounter("hostility_claire"), Is.EqualTo(1));
            Assert.That(state.PublicAnxiety, Is.EqualTo(25));
            Assert.That(state.EvidenceIntegrity, Is.EqualTo(95));
            Assert.That(state.HasFlag("claire_cooperates"), Is.True);
            Assert.That(grantedEvidence, Is.EquivalentTo(new[] { "C-01", "C-02" }));
            Assert.That(state.IsProductionSceneUnlocked("D2-02"), Is.True);
            Assert.That(state.IsProductionSceneUnlocked("D2-04"), Is.True);
            Assert.That(
                state.HasUnlockedDeduction("horizon_no_live_third_party"),
                Is.True);
            Assert.That(state.WrongStrikeCount, Is.EqualTo(1));
            Assert.That(state.HasFlag("accusation1_correct"), Is.True);
            Assert.That(state.HasFlag("question_used"), Is.True);
            Assert.That(
                state.FinalEndingId,
                Is.EqualTo(FinalAccusationResolver.CompleteEndingId));
        }

        [Test]
        public void TrustAll_ChangesEveryOfficialCharacterOnce()
        {
            ProductionEffectExecutionResult result = executor.Execute("trust_all:-1");

            Assert.That(result.Success, Is.True);
            foreach (string characterId in new[]
            {
                "CLAIRE", "DANIEL", "EVELYN", "HELENA",
                "MARCUS", "OWEN", "RICHARD", "THOMAS"
            })
            {
                Assert.That(
                    state.GetTrust(characterId),
                    Is.EqualTo(1),
                    characterId);
            }
        }

        [Test]
        public void NegativeTimeBlock_ConsumesBlocksAcrossDayBoundary()
        {
            state.SetTime(2, TimeBlock.NIGHT);

            ProductionEffectExecutionResult result = executor.Execute("timeBlock:-2");

            Assert.That(result.Success, Is.True);
            Assert.That(state.Day, Is.EqualTo(3));
            Assert.That(state.CurrentTimeBlock, Is.EqualTo(TimeBlock.PM));
        }

        [Test]
        public void PositiveTimeBlock_IsRejectedWithoutChangingClock()
        {
            state.SetTime(3, TimeBlock.PM);

            ProductionEffectExecutionResult result = executor.Execute("timeBlock:+1");

            Assert.That(result.Success, Is.False);
            Assert.That(result.Warnings, Has.Count.EqualTo(1));
            Assert.That(state.Day, Is.EqualTo(3));
            Assert.That(state.CurrentTimeBlock, Is.EqualTo(TimeBlock.PM));
        }

        [Test]
        public void InvalidEffect_DoesNotApplyEarlierTokensPartially()
        {
            ProductionEffectExecutionResult result =
                executor.Execute("trust_claire:+2; wrong_strike:many");

            Assert.That(result.Success, Is.False);
            Assert.That(result.AppliedInstructionCount, Is.Zero);
            Assert.That(state.GetTrust("CLAIRE"), Is.EqualTo(2));
            Assert.That(state.WrongStrikeCount, Is.Zero);
        }

        [Test]
        public void EvidenceFallsBackToSaveState_WhenInventoryRejectsId()
        {
            executor = new ProductionEffectExecutor(state, _ => false);

            ProductionEffectExecutionResult result =
                executor.Execute("evidence:C-07_partial,module_case");

            Assert.That(result.Success, Is.True);
            Assert.That(
                state.CollectedEvidenceIds,
                Is.EquivalentTo(new[] { "C-07_PARTIAL", "MODULE_CASE" }));
        }

        [Test]
        public void EvidenceFallback_StillReachesEvidenceInventory_WhenIdIsCanonical()
        {
            GameObject inventoryHost = new("EvidenceInventoryHost");
            EvidenceInventory inventory =
                inventoryHost.AddComponent<EvidenceInventory>();
            // EvidenceInventory.Awake() (which sets Instance) only runs in
            // Play Mode - EditMode tests never trigger it, so the
            // singleton has to be wired up manually here the same way
            // Awake would.
            System.Reflection.PropertyInfo instanceProperty =
                typeof(EvidenceInventory).GetProperty(
                    nameof(EvidenceInventory.Instance));
            instanceProperty.SetValue(null, inventory);
            // Add() reports the grant to GameStateManager.Instance unless
            // bound explicitly - also Awake-only wiring in real play, so
            // point it at this test's local state instance directly.
            inventory.BindState(state);
            try
            {
                executor = new ProductionEffectExecutor(state, _ => false);

                ProductionEffectExecutionResult result =
                    executor.Execute("evidence:C-01");

                Assert.That(result.Success, Is.True);
                // Regression guard: the fallback used to write only to
                // GameStateManager, leaving the item marked collected in
                // save data while never appearing in the evidence
                // notebook (EvidenceInventory is what the UI reads).
                Assert.That(inventory.Contains("C-01"), Is.True);
                Assert.That(state.CollectedEvidenceIds, Contains.Item("C-01"));
            }
            finally
            {
                instanceProperty.SetValue(null, null);
                Object.DestroyImmediate(inventoryHost);
            }
        }

        [Test]
        public void ExecutedEffects_RestoreAfterManagerRecreation()
        {
            executor.Execute(
                "hostility_claire:+2; wrong_strike:+1; " +
                "scene_unlock:D8-02; theory:true_death_sequence");

            DestroyManager();
            host = new GameObject("RestoredProductionEffectExecutorTests");
            state = host.AddComponent<GameStateManager>();
            state.ReloadSavedState();

            Assert.That(state.GetRuntimeCounter("hostility_claire"), Is.EqualTo(2));
            Assert.That(state.WrongStrikeCount, Is.EqualTo(1));
            Assert.That(state.IsProductionSceneUnlocked("D8-02"), Is.True);
            Assert.That(state.HasUnlockedDeduction("true_death_sequence"), Is.True);
        }

        [Test]
        public void RouteSpecificEpilogueUnlock_UsesCanonicalSceneId()
        {
            ProductionEffectExecutionResult result =
                executor.Execute("scene_unlock:D8-03_C");

            Assert.That(result.Success, Is.True);
            Assert.That(state.IsProductionSceneUnlocked("D8-03"), Is.True);
            Assert.That(state.IsProductionSceneUnlocked("D8-03_C"), Is.False);
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
            grantedEvidence.Clear();
        }
    }
}
