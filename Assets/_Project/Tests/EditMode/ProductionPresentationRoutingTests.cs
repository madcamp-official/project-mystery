using System.Linq;
using System.Text.RegularExpressions;
using NUnit.Framework;
using UnityEditor;
using UnityEngine;
using Wake.Narrative;

namespace Wake.Tests
{
    public class ProductionPresentationRoutingTests
    {
        private const string DialoguePath =
            "Assets/_Project/Content/Dialogue/Under_the_Horizon_Dialogue_KR.csv";

        private DialogueRecord[] records;

        [OneTimeSetUp]
        public void LoadOfficialDialogue()
        {
            string csv =
                AssetDatabase.LoadAssetAtPath<TextAsset>(DialoguePath).text;
            DialogueCsvParseResult parsed = DialogueCsvParser.Parse(csv);
            Assert.That(parsed.Errors, Is.Empty);
            records = parsed.Records.ToArray();
        }

        [Test]
        public void OfficialSystemRecords_AreAllRoutedOutsideDialoguePanel()
        {
            DialogueRecord[] systemRecords = records
                .Where(ProductionPresentationRouting.IsSystemEvent)
                .ToArray();

            Assert.That(systemRecords, Has.Length.EqualTo(133));
            Assert.That(
                systemRecords.Select(
                    ProductionPresentationRouting.ClassifySystemEvent),
                Has.All.Matches<ProductionUiEventPresentation>(
                    presentation =>
                        presentation.Channel >=
                            ProductionUiEventChannel.General &&
                        presentation.Channel <=
                            ProductionUiEventChannel.Ending));
        }

        [Test]
        public void OfficialInvestigationRecords_HaveCompleteMarkerAndResultSets()
        {
            Assert.That(
                records.Count(
                    InvestigationPresentationPolicy.IsMarker),
                Is.EqualTo(65));
            Assert.That(
                records.Count(record =>
                    string.Equals(
                        record.LineType,
                        "inspection",
                        System.StringComparison.OrdinalIgnoreCase)),
                Is.EqualTo(65));
            Assert.That(
                InvestigationPresentationPolicy.RoutedMonologueIds,
                Has.Count.EqualTo(15));
            Assert.That(
                InvestigationPresentationPolicy.ObservationMonologueIds,
                Has.Count.EqualTo(2));
        }

        [Test]
        public void PlayerFacingDialogue_PreservesOnlyApprovedLatinExceptions()
        {
            var latin = new Regex("[A-Za-z]");
            var allowed = new Regex(
                @"(?<![A-Za-z])(?:DNA|COO|VIP|kg|cm)(?![A-Za-z])|C-\d+",
                RegexOptions.IgnoreCase);
            string[] violations = records
                .Select(record => new
                {
                    record.CanonicalLineId,
                    Text = allowed.Replace(record.TextKo, string.Empty)
                })
                .Where(item => latin.IsMatch(item.Text))
                .Select(item =>
                    $"{item.CanonicalLineId}: {item.Text}")
                .ToArray();

            Assert.That(violations, Is.Empty);
        }

        [TestCase("D8-03_013")]
        [TestCase("D8-03_020")]
        [TestCase("D8-03_026")]
        [TestCase("D8-03_031")]
        public void EpilogueMonologues_RemainInDialogueSequence(string lineId)
        {
            DialogueRecord record = records.Single(
                candidate => candidate.CanonicalLineId == lineId);

            Assert.That(
                InvestigationPresentationPolicy.IsInvestigationResult(
                    record),
                Is.False);
        }
    }
}
