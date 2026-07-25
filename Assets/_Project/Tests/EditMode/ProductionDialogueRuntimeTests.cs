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
