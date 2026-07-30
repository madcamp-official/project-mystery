using System.Linq;
using NUnit.Framework;
using UnityEditor;
using UnityEngine;
using Wake.Narrative;

namespace Wake.Tests
{
    public class StoryRecordingCatalogTests
    {
        private const string DialoguePath =
            "Assets/_Project/Content/Dialogue/Under_the_Horizon_Dialogue_KR.csv";

        [Test]
        public void TryGet_ResolvesTheWiredDanielChatLines()
        {
            Assert.That(
                StoryRecordingCatalog.TryGet("d2_06_10", out string d206),
                Is.True);
            Assert.That(d206, Is.EqualTo("D2-06_DANIEL_CHAT_01"));

            Assert.That(
                StoryRecordingCatalog.TryGet("d5_03_10", out string d503),
                Is.True);
            Assert.That(d503, Is.EqualTo("D5-03_DANIEL_CHAT_01"));
        }

        [Test]
        public void TryGet_ResolvesTheWiredAnonChatLines()
        {
            Assert.That(
                StoryRecordingCatalog.TryGet("d2_06_09", out string d206First),
                Is.True);
            Assert.That(d206First, Is.EqualTo("D2-06_ANON_CHAT_01"));
            Assert.That(
                StoryRecordingCatalog.TryGet("d2_06_11", out string d206Second),
                Is.True);
            Assert.That(d206Second, Is.EqualTo("D2-06_ANON_CHAT_02"));

            Assert.That(
                StoryRecordingCatalog.TryGet("d5_03_09", out string d503First),
                Is.True);
            Assert.That(d503First, Is.EqualTo("D5-03_ANON_CHAT_01"));
            Assert.That(
                StoryRecordingCatalog.TryGet("d5_03_11", out string d503Second),
                Is.True);
            Assert.That(d503Second, Is.EqualTo("D5-03_ANON_CHAT_02"));
        }

        [Test]
        public void TryGet_ResolvesTheWiredEvelynMessageLine()
        {
            Assert.That(
                StoryRecordingCatalog.TryGet("d4_01_21", out string d401),
                Is.True);
            Assert.That(d401, Is.EqualTo("D4-01_EVELYN_MESSAGE_01"));
        }

        [Test]
        public void TryGet_ReturnsFalseForLinesWithNoFileYet()
        {
            // D1-06's dying-message recording (DANIEL_DYING) has no file yet.
            Assert.That(
                StoryRecordingCatalog.TryGet("d1_06_19", out _), Is.False);
            Assert.That(
                StoryRecordingCatalog.TryGet("nonexistent_line", out _), Is.False);
        }

        [Test]
        public void EveryWiredStableLineId_ExistsInProductionDialogue()
        {
            TextAsset csv = AssetDatabase.LoadAssetAtPath<TextAsset>(DialoguePath);
            var stableIds = DialogueCsvParser.Parse(csv.text).Records
                .Select(record => record.StableLineId)
                .ToHashSet();

            foreach (string stableId in new[]
                     {
                         "d2_06_09", "d2_06_10", "d2_06_11",
                         "d5_03_09", "d5_03_10", "d5_03_11",
                         "d4_01_21"
                     })
            {
                Assert.That(stableIds, Contains.Item(stableId), stableId);
            }
        }
    }
}
