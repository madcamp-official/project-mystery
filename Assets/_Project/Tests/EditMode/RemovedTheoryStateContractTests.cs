using System.Linq;
using NUnit.Framework;
using UnityEngine;
using Wake.Core;

namespace Wake.Tests
{
    public sealed class RemovedTheoryStateContractTests
    {
        private const string Primary = "UNDER_THE_HORIZON_GAME_STATE_V2";
        private const string Backup = Primary + "_BACKUP";
        private const string Pending = Primary + "_PENDING";
        private const string Legacy = "THE_WAKE_GAME_STATE_V1";
        private const string LegacyBackup = Legacy + "_BACKUP";
        private const string LegacyPending = Legacy + "_PENDING";

        private GameObject host;
        private GameStateManager state;

        [SetUp]
        public void SetUp()
        {
            DestroyManager();
            ClearSaves();
            host = new GameObject("RemovedTheoryStateContract");
            state = host.AddComponent<GameStateManager>();
            state.StartNewGame();
        }

        [TearDown]
        public void TearDown()
        {
            DestroyManager();
            ClearSaves();
        }

        [Test]
        public void MoreThanThreeDeductions_RemainAvailableTogether()
        {
            string[] deductions =
            {
                "scene_denial",
                "body_insertion",
                "transport_route",
                "actual_murder",
                "past_event",
                "access_route"
            };

            foreach (string deduction in deductions)
            {
                Assert.That(state.UnlockDeduction(deduction), Is.True);
            }

            Assert.That(
                state.UnlockedDeductionIds,
                Is.EquivalentTo(deductions));
        }

        [Test]
        public void Deductions_RoundTripWithoutSelectionOrCapacityState()
        {
            state.UnlockDeduction("scene_denial");
            state.UnlockDeduction("body_insertion");
            state.UnlockDeduction("transport_route");
            state.UnlockDeduction("actual_murder");

            RecreateManager();

            Assert.That(state.UnlockedDeductionIds, Has.Count.EqualTo(4));
            Assert.That(state.HasUnlockedDeduction("scene_denial"), Is.True);
            Assert.That(state.HasUnlockedDeduction("body_insertion"), Is.True);
            Assert.That(state.HasUnlockedDeduction("transport_route"), Is.True);
            Assert.That(state.HasUnlockedDeduction("actual_murder"), Is.True);
            AssertSaveDoesNotContainRemovedFields();
        }

        [Test]
        public void LegacyTheorySelection_DoesNotBecomeDeductionProgress()
        {
            const string legacyJson =
                "{\"day\":4,\"timeBlock\":2,\"theorySlots\":3," +
                "\"activeTheories\":[\"scene_denial\",\"body_insertion\"]," +
                "\"unlockedDeductionIds\":[\"transport_route\"]," +
                "\"flags\":[\"service_rail_access\"]}";
            DestroyManager();
            ClearSaves();
            PlayerPrefs.SetString(Legacy, legacyJson);
            PlayerPrefs.Save();

            CreateManager();
            state.ReloadSavedState();

            Assert.That(state.Day, Is.EqualTo(4));
            Assert.That(state.CurrentTimeBlock, Is.EqualTo(TimeBlock.NIGHT));
            Assert.That(state.HasFlag("service_rail_access"), Is.True);
            Assert.That(state.HasUnlockedDeduction("transport_route"), Is.True);
            Assert.That(state.HasUnlockedDeduction("scene_denial"), Is.False);
            Assert.That(state.HasUnlockedDeduction("body_insertion"), Is.False);
            AssertSaveDoesNotContainRemovedFields();
        }

        [Test]
        public void NewGame_ClearsMigratedDeductionsWithoutReintroducingTheoryFields()
        {
            state.UnlockDeduction("past_event");
            Assert.That(state.HasUnlockedDeduction("past_event"), Is.True);

            state.StartNewGame();

            Assert.That(state.UnlockedDeductionIds, Is.Empty);
            AssertSaveDoesNotContainRemovedFields();
        }

        private void AssertSaveDoesNotContainRemovedFields()
        {
            Assert.That(PlayerPrefs.HasKey(Primary), Is.True);
            string json = PlayerPrefs.GetString(Primary);
            Assert.That(json, Does.Contain("\"unlockedDeductionIds\""));
            Assert.That(json, Does.Not.Contain("\"theorySlots\""));
            Assert.That(json, Does.Not.Contain("\"activeTheories\""));
        }

        private void RecreateManager()
        {
            DestroyManager();
            CreateManager();
            state.ReloadSavedState();
        }

        private void CreateManager()
        {
            host = new GameObject("RestoredRemovedTheoryStateContract");
            state = host.AddComponent<GameStateManager>();
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

        private static void ClearSaves()
        {
            PlayerPrefs.DeleteKey(Primary);
            PlayerPrefs.DeleteKey(Backup);
            PlayerPrefs.DeleteKey(Pending);
            PlayerPrefs.DeleteKey(Legacy);
            PlayerPrefs.DeleteKey(LegacyBackup);
            PlayerPrefs.DeleteKey(LegacyPending);
            PlayerPrefs.Save();
        }
    }
}
