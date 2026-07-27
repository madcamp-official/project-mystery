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
            "Assets/_Project/Content/Dialogue/Under_the_Horizon_Dialogue_KR.csv";
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
            PlayerPrefs.DeleteKey("UNDER_THE_HORIZON_GAME_STATE_V2");
            PlayerPrefs.DeleteKey("THE_WAKE_GAME_STATE_V1");
        }
        [Test]
        public void PresentationMap_CoversAllOfficialSourceEmotions()
        {
            string[] emotions = records
                .Select(record => record.Emotion)
                .Distinct()
                .OrderBy(emotion => emotion)
                .ToArray();

            Assert.That(emotions, Has.Length.EqualTo(109));
            Assert.That(
                emotions.Where(emotion =>
                    !DialoguePresentationMap.IsKnownEmotion(emotion)),
                Is.Empty);
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

        [TestCase("ADRIAN_독백", "Adrian Vale · 독백")]
        [TestCase("EVELYN_RECORD", "Evelyn Shaw · 기록 음성")]
        [TestCase("NARRATION", "내레이션")]
        [TestCase("SYSTEM", "시스템")]
        [TestCase("승무원_NPC", "승무원")]
        public void PresentationMap_ProvidesPlayerFacingSpecialSpeakerLabels(
            string source,
            string expected)
        {
            DialogueSpeakerIdentity identity =
                DialoguePresentationMap.GetSpeaker(source);

            Assert.That(
                DialoguePresentationMap.GetSpeakerLabel(source, identity),
                Is.EqualTo(expected));
        }

        [Test]
        public void PortraitCatalog_MapsFourStatesForEveryPlayableCharacter()
        {
            foreach (DialoguePortraitDefinition definition in
                     DialoguePortraitCatalog.All)
            {
                string[] names =
                {
                    DialoguePortraitCatalog.GetSpriteName(
                        definition, PortraitEmotion.Neutral),
                    DialoguePortraitCatalog.GetSpriteName(
                        definition, PortraitEmotion.Concerned),
                    DialoguePortraitCatalog.GetSpriteName(
                        definition, PortraitEmotion.Angry),
                    DialoguePortraitCatalog.GetSpriteName(
                        definition, PortraitEmotion.Positive)
                };

                Assert.That(names, Has.Length.EqualTo(4));
                Assert.That(names.Distinct().Count(), Is.EqualTo(4));
                Assert.That(names[3], Does.EndWith("_happy"));
            }
        }

        [Test]
        public void PortraitCatalog_LoadsThirtySixCoreCharacterExpressionSprites()
        {
            int loaded = 0;
            foreach (DialoguePortraitDefinition definition in
                     DialoguePortraitCatalog.All.Where(definition =>
                         definition.UsesExpressionSprites))
            {
                Sprite[] sprites = Resources.LoadAll<Sprite>(
                    $"{DialoguePortraitCatalog.ResourceFolder}/" +
                    $"portrait_{definition.ExpressionSheet}_expressions");
                loaded += sprites.Length;
                Assert.That(sprites.Select(item => item.name), Does.Contain(
                    DialoguePortraitCatalog.GetSpriteName(
                        definition,
                        PortraitEmotion.Neutral)));
            }

            Assert.That(loaded, Is.EqualTo(36));
        }
        [Test]
        public void P01ThroughP03_FlowExecutesOfficialSceneUnlockEffects()
        {
            host = new GameObject("ProductionDialogueEffectFlow");
            GameStateManager state = host.AddComponent<GameStateManager>();
            var completed = new HashSet<string>();
            var flow = new ProductionDialogueFlow(records, completed, state);
            Assert.That(flow.StartScene("P-02"), Is.False);
            CompleteScene(flow, "P-01");
            Assert.That(completed, Contains.Item("P-01"));
            Assert.That(state.IsProductionSceneUnlocked("P-02"), Is.True);
            CompleteScene(flow, "P-02");
            Assert.That(completed, Contains.Item("P-02"));
            Assert.That(state.IsProductionSceneUnlocked("P-03"), Is.True);
            CompleteScene(flow, "P-03");
            Assert.That(completed, Contains.Item("P-03"));
            Assert.That(state.IsProductionSceneUnlocked("D1-01"), Is.True);
            Assert.That(flow.Warnings, Is.Empty);
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
                Is.EqualTo(new[] { "D8-01 correct" }));
            Assert.That(flow.CanStartScene("D8-02"), Is.False);
            Assert.That(flow.StartScene("D8-02"), Is.False);
            Assert.That(
                flow.Warnings,
                Has.Some.Contains("D8-01 correct"));
        }

        private static void CompleteScene(ProductionDialogueFlow flow, string sceneId)
        {
            Assert.That(flow.StartScene(sceneId), Is.True, string.Join("\n", flow.Warnings));
            int previousOrder = 0;
            while (!flow.IsComplete)
            {
                if (flow.IsAwaitingChoice)
                {
                    Assert.That(flow.Choices.Count, Is.GreaterThan(0));
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
