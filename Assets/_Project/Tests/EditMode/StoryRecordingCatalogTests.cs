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
        public void TryGet_ResolvesTheTwoWiredDanielChatLines()
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
        public void TryGet_ReturnsFalseForLinesWithNoFileYet()
        {
            Assert.That(
                StoryRecordingCatalog.TryGet("d2_06_09", out _), Is.False);
            Assert.That(
                StoryRecordingCatalog.TryGet("d4_01_01", out _), Is.False);
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

            foreach (string stableId in new[] { "d2_06_10", "d5_03_10" })
            {
                Assert.That(stableIds, Contains.Item(stableId), stableId);
            }
        }
    }
}
