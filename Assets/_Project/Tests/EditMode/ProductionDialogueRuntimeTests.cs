using System.Collections.Generic;
using System.Linq;
using NUnit.Framework;
using UnityEditor;
using UnityEngine;
using Wake.Core;
using Wake.Narrative;
namespace Wake.Tests
{
    public class ProductionDialogueRuntimeTests
    {
        private const string Path =
            "Assets/_Project/Content/Dialogue/The_Wake_Without_Footprints_Dialogue_KR.csv";
        private List<DialogueRecord> records;
        private GameObject host;
        [OneTimeSetUp]
        public void LoadRecords()
        {
            string csv = AssetDatabase.LoadAssetAtPath<TextAsset>(Path).text;
            records = DialogueCsvParser.Parse(csv).Records.ToList();
        }
        [TearDown]
        public void TearDown()
        {
            if (host != null)
            {
                Object.DestroyImmediate(host);
            }
            PlayerPrefs.DeleteKey("THE_WAKE_GAME_STATE_V1");
        }
        [Test]
        public void PresentationMap_CoversAllSourceEmotions()
        {
            List<string> emotions = records.Select(item => item.Emotion).Distinct().ToList();
            Assert.That(emotions.Count, Is.EqualTo(29));
            Assert.That(emotions.All(DialoguePresentationMap.IsKnownEmotion), Is.True);
            Assert.That(
                DialoguePresentationMap.GetEmotion("fear"),
                Is.EqualTo(PortraitEmotion.Concerned));
            Assert.That(
                DialoguePresentationMap.GetEmotion("anger"),
                Is.EqualTo(PortraitEmotion.Angry));
        }
        [TestCase("ADRIAN_\uB3C5\uBC31", "ADRIAN", DialogueSpeakerKind.Monologue)]
        [TestCase("CLAIRE(\uC120\uD0DD)", "CLAIRE", DialogueSpeakerKind.Character)]
        [TestCase("EVELYN_RECORD", "EVELYN", DialogueSpeakerKind.RecordedVoice)]
        [TestCase("NARRATION", "", DialogueSpeakerKind.Narration)]
        [TestCase("SYSTEM", "", DialogueSpeakerKind.System)]
        [TestCase("\uC2B9\uBB34\uC6D0_NPC", "NPC", DialogueSpeakerKind.NonPlayer)]
        public void PresentationMap_NormalizesSpecialSpeakers(
            string source,
            string portrait,
            DialogueSpeakerKind kind)
        {
            DialogueSpeakerIdentity identity = DialoguePresentationMap.GetSpeaker(source);
            Assert.That(identity.PortraitId, Is.EqualTo(portrait));
            Assert.That(identity.Kind, Is.EqualTo(kind));
        }
        [Test]
        public void TypedEffect_ConnectsEverySupportedStateMutation()
        {
            host = new GameObject("ProductionDialogueRuntimeTests");
            GameStateManager state = host.AddComponent<GameStateManager>();
            var effect = new DialogueTypedEffect
            {
                TargetCharacterId = "DANIEL",
                TrustDelta = 2,
                AnxietyDelta = 5,
                IntegrityDelta = -10,
                AddFlags = new[] { "ceiling_access" },
                RemoveFlags = new[] { "temporary_flag" }
            };
            state.AddFlag("temporary_flag");
            effect.Apply(state);
            Assert.That(state.GetTrust("DANIEL"), Is.EqualTo(4));
            Assert.That(state.PublicAnxiety, Is.EqualTo(20));
            Assert.That(state.EvidenceIntegrity, Is.EqualTo(90));
            Assert.That(state.HasFlag("ceiling_access"), Is.True);
            Assert.That(state.HasFlag("temporary_flag"), Is.False);
        }
        [Test]
        public void EffectCatalog_ExecutesOnlyConfirmedMappings()
        {
            Assert.That(
                DialogueEffectCatalog.TryResolve(
                    "\uBE44\uC11C\uC2E4 \uAD8C\uD55C \uD50C\uB798\uADF8",
                    out DialogueTypedEffect confirmed),
                Is.True);
            Assert.That(confirmed.AddFlags, Contains.Item("secretary_access"));
            Assert.That(DialogueEffectCatalog.TryResolve("Daniel \uC2E0\uB8B0\uB3C4 \u00B11", out _), Is.False);
        }
        [Test]
        public void P01ThroughP03_FlowInOrderWithTwoChoices()
        {
            var completed = new HashSet<string>();
            var flow = new ProductionDialogueFlow(records, completed);
            Assert.That(flow.StartScene("P-02"), Is.False);
            CompleteScene(flow, "P-01");
            Assert.That(completed, Contains.Item("P-01"));
            CompleteScene(flow, "P-02");
            Assert.That(completed, Contains.Item("P-02"));
            CompleteScene(flow, "P-03");
            Assert.That(completed, Contains.Item("P-03"));
            Assert.That(flow.Warnings.Any(item => item.Contains("unconfirmed effect")), Is.True);
        }

        [Test]
        public void CompletingScene_PersistsProgressThroughGameState()
        {
            host = new GameObject("ProductionSceneProgress");
            GameStateManager state = host.AddComponent<GameStateManager>();
            var flow = new ProductionDialogueFlow(records, null, state);

            CompleteScene(flow, "P-01");

            Assert.That(state.HasCompletedScene("P-01"), Is.True);
            Assert.That(
                state.CompletedProductionSceneIds,
                Is.EqualTo(new[] { "P-01" }));
        }

        [Test]
        public void RestoredProgress_UnlocksDependentSceneInNewFlow()
        {
            host = new GameObject("ProductionSceneProgress");
            GameStateManager state = host.AddComponent<GameStateManager>();
            CompleteScene(new ProductionDialogueFlow(records, null, state), "P-01");

            Object.DestroyImmediate(host);
            host = new GameObject("RestoredProductionSceneProgress");
            state = host.AddComponent<GameStateManager>();
            state.ReloadSavedState();

            var restoredFlow = new ProductionDialogueFlow(records, null, state);

            Assert.That(restoredFlow.StartScene("P-02"), Is.True);
            Assert.That(restoredFlow.Warnings, Is.Empty);
        }

        [Test]
        public void BlockedScene_DoesNotPersistCompletion()
        {
            host = new GameObject("BlockedProductionScene");
            GameStateManager state = host.AddComponent<GameStateManager>();
            var flow = new ProductionDialogueFlow(records, null, state);

            Assert.That(flow.StartScene("P-02"), Is.False);

            Assert.That(state.HasCompletedScene("P-02"), Is.False);
            Assert.That(state.CompletedProductionSceneIds, Is.Empty);
        }

        [Test]
        public void ReplayingCompletedScene_DoesNotDuplicateSavedProgress()
        {
            host = new GameObject("ReplayProductionScene");
            GameStateManager state = host.AddComponent<GameStateManager>();
            var flow = new ProductionDialogueFlow(records, null, state);
            CompleteScene(flow, "P-01");

            CompleteScene(flow, "P-01");

            Assert.That(
                state.CompletedProductionSceneIds.Count(item => item == "P-01"),
                Is.EqualTo(1));
        }

        [Test]
        public void ExplicitProgressSet_CombinesWithSavedProgress()
        {
            host = new GameObject("CombinedProductionProgress");
            GameStateManager state = host.AddComponent<GameStateManager>();
            state.RecordCompletedScene("P-01");
            var transient = new HashSet<string> { "P-02" };
            var flow = new ProductionDialogueFlow(records, transient, state);

            Assert.That(flow.StartScene("P-03"), Is.True);
            Assert.That(transient, Contains.Item("P-01"));
            Assert.That(transient, Contains.Item("P-02"));
        }

        [Test]
        public void CompletingWithExplicitSet_UpdatesSetAndSave()
        {
            host = new GameObject("MirroredProductionProgress");
            GameStateManager state = host.AddComponent<GameStateManager>();
            var transient = new HashSet<string>();
            var flow = new ProductionDialogueFlow(records, transient, state);

            CompleteScene(flow, "P-01");

            Assert.That(transient, Contains.Item("P-01"));
            Assert.That(state.HasCompletedScene("P-01"), Is.True);
        }

        [Test]
        public void ExplicitProgressSet_NormalizesWhitespaceCaseAndDuplicates()
        {
            var transient = new HashSet<string>
            {
                " p-01 ",
                "P-01",
                "",
                " d1-01 "
            };

            var flow = new ProductionDialogueFlow(records, transient);

            Assert.That(
                transient,
                Is.EquivalentTo(new[] { "P-01", "D1-01" }));
            Assert.That(flow.IsSceneCompleted("p-01"), Is.True);
            Assert.That(flow.IsSceneCompleted(" D1-01 "), Is.True);
            Assert.That(
                flow.CompletedSceneIds,
                Is.EquivalentTo(new[] { "P-01", "D1-01" }));
        }

        [Test]
        public void StartScene_NormalizesRequestedSceneId()
        {
            var flow = new ProductionDialogueFlow(records);

            Assert.That(flow.StartScene(" p-01 "), Is.True);
            Assert.That(flow.ActiveSceneId, Is.EqualTo("P-01"));
        }

        [Test]
        public void MissingPrerequisites_ReportsIncompleteScene()
        {
            var flow = new ProductionDialogueFlow(records);

            Assert.That(
                flow.GetMissingPrerequisites("P-02"),
                Is.EqualTo(new[] { "P-01" }));
            Assert.That(flow.CanStartScene("P-02"), Is.False);

            CompleteScene(flow, "P-01");

            Assert.That(flow.GetMissingPrerequisites("P-02"), Is.Empty);
            Assert.That(flow.CanStartScene("P-02"), Is.True);
        }

        [Test]
        public void UnknownScene_IsNotStartableAndReportsItsId()
        {
            var flow = new ProductionDialogueFlow(records);

            Assert.That(flow.CanStartScene("missing-scene"), Is.False);
            Assert.That(
                flow.GetMissingPrerequisites(" missing-scene "),
                Is.EqualTo(new[] { "MISSING-SCENE" }));
            Assert.That(flow.StartScene("missing-scene"), Is.False);
        }

        [Test]
        public void UndefinedTypedCondition_RemainsBlockedAndVisible()
        {
            var completed = new HashSet<string> { "D8-01" };
            var flow = new ProductionDialogueFlow(records, completed);

            Assert.That(
                flow.GetMissingPrerequisites("D8-02"),
                Is.EqualTo(new[] { "D8-01 \uC815\uB2F5" }));
            Assert.That(flow.CanStartScene("D8-02"), Is.False);
            Assert.That(flow.StartScene("D8-02"), Is.False);
            Assert.That(
                flow.Warnings,
                Has.Some.Contains("D8-01 \uC815\uB2F5"));
        }

        private static void CompleteScene(ProductionDialogueFlow flow, string sceneId)
        {
            Assert.That(flow.StartScene(sceneId), Is.True, string.Join("\n", flow.Warnings));
            int previousOrder = 0;
            while (!flow.IsComplete)
            {
                if (flow.IsAwaitingChoice)
                {
                    Assert.That(flow.Choices.Count, Is.EqualTo(2));
                    Assert.That(flow.Choices.Count, Is.LessThanOrEqualTo(
                        ProductionDialogueFlow.ChoiceCapacity));
                    Assert.That(flow.SelectChoice(0), Is.True);
                    continue;
                }
                Assert.That(flow.Current.Order, Is.GreaterThan(previousOrder));
                previousOrder = flow.Current.Order;
                flow.Advance();
            }
        }
    }
}
