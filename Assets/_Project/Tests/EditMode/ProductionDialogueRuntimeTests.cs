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

            Assert.That(emotions, Has.Length.EqualTo(108));
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
        [TestCase("EVELYN_MESSAGE", "EVELYN", DialogueSpeakerKind.RecordedVoice)]
        [TestCase("THOMAS_RECORD", "THOMAS", DialogueSpeakerKind.RecordedVoice)]
        [TestCase("DANIEL_CHAT", "DANIEL", DialogueSpeakerKind.RecordedVoice)]
        [TestCase("NEWS_REPORT", "", DialogueSpeakerKind.Narration)]
        [TestCase("ANON_CHAT", "", DialogueSpeakerKind.Narration)]
        [TestCase("UI_HINT", "", DialogueSpeakerKind.System)]
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

        [TestCase("ADRIAN_독백", "아드리안 베일 · 독백")]
        [TestCase("EVELYN_RECORD", "이블린 쇼 · 기록 음성")]
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
        public void PresentationMap_UsesLineTypeForAdrianMonologue()
        {
            DialogueSpeakerIdentity identity =
                DialoguePresentationMap.GetSpeaker(
                    "ADRIAN",
                    "monologue");

            Assert.That(
                identity.Kind,
                Is.EqualTo(DialogueSpeakerKind.Monologue));
            Assert.That(
                DialoguePresentationMap.GetSpeakerLabel(
                    "ADRIAN",
                    identity),
                Is.EqualTo("아드리안 베일 · 독백"));
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

        [TestCase("D1-07", "D1-06", "D2-01")]
        [TestCase("D2-06", "D2-03", "D3-01")]
        [TestCase("D3-05", "D3-04", "D4-01")]
        [TestCase("D5-04", "D5-03", "D6-01")]
        [TestCase("D7-04", "D7-03", "D8-01")]
        public void CompletingDialogueAtDayBoundary_PublishesNextDayScene(
            string completedSceneId,
            string prerequisiteSceneId,
            string expectedNextSceneId)
        {
            host = new GameObject($"DayBoundary_{completedSceneId}");
            GameStateManager state = host.AddComponent<GameStateManager>();
            state.RecordCompletedScene(prerequisiteSceneId);
            var flow = new ProductionDialogueFlow(records, null, state);
            InvestigationEvent captured = default;
            int completionEvents = 0;

            void Capture(InvestigationEvent item)
            {
                if (item.Kind != InvestigationEventKind.SceneCompleted)
                    return;

                captured = item;
                completionEvents++;
            }

            InvestigationEventHub.Published += Capture;
            try
            {
                CompleteScene(flow, completedSceneId);
            }
            finally
            {
                InvestigationEventHub.Published -= Capture;
            }

            Assert.That(completionEvents, Is.EqualTo(1));
            Assert.That(captured.SubjectId, Is.EqualTo(completedSceneId));
            Assert.That(captured.ContextId, Is.EqualTo(expectedNextSceneId));
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
        public void CompletedScene_CannotBeStartedAgain()
        {
            host = new GameObject("ReplayProductionScene");
            GameStateManager state = host.AddComponent<GameStateManager>();
            var flow = new ProductionDialogueFlow(records, null, state);
            CompleteScene(flow, "P-01");

            Assert.That(flow.CanStartScene("P-01"), Is.False);
            Assert.That(flow.StartScene("P-01"), Is.False);
            Assert.That(state.CompletedProductionSceneIds, Is.EqualTo(
                new[] { "P-01" }));
        }

        [Test]
        public void ReplayingIncompleteChoice_DoesNotApplyTrustTwice()
        {
            host = new GameObject("OneShotDialogueEffect");
            GameStateManager state = host.AddComponent<GameStateManager>();
            var first = new ProductionDialogueFlow(records, null, state);

            Assert.That(first.StartScene("P-01"), Is.True);
            AdvanceUntilChoice(first);
            SelectChoice(first, "P-01_C1");
            Assert.That(state.GetTrust("DANIEL"), Is.EqualTo(3));

            var restarted = new ProductionDialogueFlow(records, null, state);
            Assert.That(restarted.StartScene("P-01"), Is.True);
            AdvanceUntilChoice(restarted);
            SelectChoice(restarted, "P-01_C1");

            Assert.That(state.GetTrust("DANIEL"), Is.EqualTo(3));
            Assert.That(
                state.AppliedDialogueEffectIds,
                Has.Member("p_01_20"));
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
        public void D101_FreeConversationsRemainAvailableUntilAllFourAreComplete()
        {
            host = new GameObject("D101FreeConversationFlow");
            GameStateManager state = host.AddComponent<GameStateManager>();
            var completed = new HashSet<string> { "P-03" };
            var flow = new ProductionDialogueFlow(records, completed, state);

            Assert.That(flow.StartScene("D1-01"), Is.True);
            AdvanceUntilChoice(flow);
            AssertChoiceIds(
                flow,
                "D1-01_CLAIRE",
                "D1-01_MARCUS",
                "D1-01_HELENA",
                "D1-01_OWEN");

            SelectChoice(flow, "D1-01_HELENA");
            Assert.That(flow.Current.Order, Is.EqualTo(17));
            AdvanceUntilChoice(flow);
            AssertChoiceIds(
                flow,
                "D1-01_CLAIRE",
                "D1-01_MARCUS",
                "D1-01_OWEN");

            SelectChoice(flow, "D1-01_CLAIRE");
            AdvanceUntilChoice(flow);
            AssertChoiceIds(flow, "D1-01_MARCUS", "D1-01_OWEN");

            SelectChoice(flow, "D1-01_OWEN");
            AdvanceUntilChoice(flow);
            AssertChoiceIds(flow, "D1-01_MARCUS");

            SelectChoice(flow, "D1-01_MARCUS");
            Assert.That(flow.Current.Order, Is.EqualTo(12));
            while (flow.Current != null && flow.Current.Order < 27)
            {
                flow.Advance();
            }

            Assert.That(flow.IsAwaitingChoice, Is.False);
            Assert.That(flow.Current.Order, Is.EqualTo(27));
            Assert.That(
                new[] { "met_claire", "met_marcus", "met_helena", "met_owen" }
                    .All(state.HasFlag),
                Is.True);

            flow.Advance();
            flow.Advance();
            Assert.That(flow.IsComplete, Is.True);
            Assert.That(state.IsProductionSceneUnlocked("D1-02"), Is.True);
        }

        [Test]
        public void D101_RestoredChoiceCheckpointHidesCompletedConversation()
        {
            host = new GameObject("D101FreeConversationRestore");
            GameStateManager state = host.AddComponent<GameStateManager>();
            var completed = new HashSet<string> { "P-03" };
            var flow = new ProductionDialogueFlow(records, completed, state);

            Assert.That(flow.StartScene("D1-01"), Is.True);
            AdvanceUntilChoice(flow);
            SelectChoice(flow, "D1-01_CLAIRE");
            AdvanceUntilChoice(flow);
            var checkpoint = new ProductionDialogueCheckpoint
            {
                activeSceneId = flow.ActiveSceneId,
                lineIndex = flow.CurrentIndex,
                awaitingChoice = true
            };

            var restored = new ProductionDialogueFlow(records, completed, state);
            Assert.That(restored.RestoreScene(checkpoint), Is.True);
            AssertChoiceIds(
                restored,
                "D1-01_MARCUS",
                "D1-01_HELENA",
                "D1-01_OWEN");
        }

        // Regression test for a bug where restoring straight into an
        // awaiting-choice checkpoint left the dialogue line UI showing its
        // scene-authored placeholder text ("Character line...."): the line
        // text is normally only ever set by rendering the non-choice record
        // that precedes a PLAYER_CHOICE block, but a restore jumps directly
        // to the choice block without ever rendering that preceding record
        // this session.
        [Test]
        public void RestoredAwaitingChoiceCheckpoint_ExposesPrecedingPromptRecord()
        {
            host = new GameObject("D101ChoicePromptRestore");
            GameStateManager state = host.AddComponent<GameStateManager>();
            var completed = new HashSet<string> { "P-03" };
            var flow = new ProductionDialogueFlow(records, completed, state);

            Assert.That(flow.StartScene("D1-01"), Is.True);
            AdvanceUntilChoice(flow);
            DialogueRecord expectedPrompt = flow.ChoicePromptRecord;
            Assert.That(expectedPrompt, Is.Not.Null);

            var checkpoint = new ProductionDialogueCheckpoint
            {
                activeSceneId = flow.ActiveSceneId,
                lineIndex = flow.CurrentIndex,
                awaitingChoice = true
            };

            var restored = new ProductionDialogueFlow(records, completed, state);
            Assert.That(restored.RestoreScene(checkpoint), Is.True);
            Assert.That(restored.IsAwaitingChoice, Is.True);
            Assert.That(restored.Current, Is.Null);
            Assert.That(restored.ChoicePromptRecord, Is.Not.Null);
            Assert.That(
                restored.ChoicePromptRecord.Order,
                Is.EqualTo(expectedPrompt.Order));
            Assert.That(
                restored.ChoicePromptRecord.Speaker,
                Is.Not.EqualTo("PLAYER_CHOICE"));
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

        private static void AdvanceUntilChoice(ProductionDialogueFlow flow)
        {
            int guard = 0;
            while (!flow.IsAwaitingChoice && !flow.IsComplete && guard++ < 100)
            {
                Assert.That(flow.Current, Is.Not.Null);
                flow.Advance();
            }

            Assert.That(guard, Is.LessThan(100));
            Assert.That(flow.IsAwaitingChoice, Is.True);
        }

        private static void SelectChoice(
            ProductionDialogueFlow flow,
            string choiceId)
        {
            int choiceIndex = Enumerable.Range(0, flow.Choices.Count)
                .First(index => flow.Choices[index].ChoiceId == choiceId);
            Assert.That(flow.SelectChoice(choiceIndex), Is.True);
        }

        private static void AssertChoiceIds(
            ProductionDialogueFlow flow,
            params string[] expected)
        {
            Assert.That(
                flow.Choices.Select(choice => choice.ChoiceId),
                Is.EqualTo(expected));
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
