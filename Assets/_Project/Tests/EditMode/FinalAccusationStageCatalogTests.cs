using System.Linq;
using NUnit.Framework;
using Wake.Core;

namespace Wake.Tests
{
    public class FinalAccusationStageCatalogTests
    {
        private static readonly string[] ExpectedCorrectChoiceIds =
        {
            "D8-01_A1_EVELYN",
            "D8-01_A2_BALLAST",
            "D8-01_A3_SUFFOCATION",
            "D8-01_A4_RAIL",
            "D8-01_A5_MISCONCEPTION",
            "D8-01_A6_EVELYN"
        };

        [Test]
        public void Catalog_ContainsSixStagesInWorkbookOrder()
        {
            FinalAccusationStage[] stages = FinalAccusationStageCatalog.All
                .Select(entry => entry.Stage)
                .ToArray();

            Assert.That(stages, Is.EqualTo(new[]
            {
                FinalAccusationStage.Culprit,
                FinalAccusationStage.MurderLocation,
                FinalAccusationStage.CauseOfDeath,
                FinalAccusationStage.BodyTransport,
                FinalAccusationStage.MurderMotive,
                FinalAccusationStage.OrpheusMastermind
            }));
        }

        [Test]
        public void Catalog_ProvidesFourChoicesForEveryStage()
        {
            foreach (FinalAccusationStageDefinition stage in
                     FinalAccusationStageCatalog.All)
            {
                Assert.That(stage.Options, Has.Count.EqualTo(4), stage.Prompt);
                Assert.That(
                    stage.Options.Select(option => option.Label),
                    Has.All.Not.Empty,
                    stage.Prompt);
            }
        }

        [Test]
        public void Catalog_UsesTwentyFourUniqueWorkbookChoiceIds()
        {
            string[] choiceIds = FinalAccusationStageCatalog.All
                .SelectMany(stage => stage.Options)
                .Select(option => option.ChoiceId)
                .ToArray();

            Assert.That(choiceIds, Has.Length.EqualTo(24));
            Assert.That(choiceIds.Distinct(), Has.Count.EqualTo(24));
            Assert.That(
                choiceIds,
                Has.All.StartsWith("D8-01_A"));
        }

        [Test]
        public void Catalog_MarksExactlyOneCorrectChoicePerStage()
        {
            foreach (FinalAccusationStageDefinition stage in
                     FinalAccusationStageCatalog.All)
            {
                Assert.That(
                    stage.Options.Count(option => option.IsCorrect),
                    Is.EqualTo(1),
                    stage.Prompt);
            }
        }

        [Test]
        public void Catalog_CorrectChoicesMatchOfficialWorkbook()
        {
            string[] correctChoiceIds = FinalAccusationStageCatalog.All
                .Select(stage => stage.CorrectOption.ChoiceId)
                .ToArray();

            Assert.That(correctChoiceIds, Is.EqualTo(ExpectedCorrectChoiceIds));
        }

        [Test]
        public void Catalog_AllEnumValuesAreSelectableAndNonZero()
        {
            foreach (FinalAccusationStageDefinition stage in
                     FinalAccusationStageCatalog.All)
            {
                Assert.That(
                    stage.Options.Select(option => option.EnumValue),
                    Has.All.GreaterThan(0),
                    stage.Prompt);
                Assert.That(
                    stage.Options.Select(option => option.EnumValue).Distinct(),
                    Has.Count.EqualTo(4),
                    stage.Prompt);
            }
        }

        [TestCase(FinalAccusationStage.Culprit)]
        [TestCase(FinalAccusationStage.MurderLocation)]
        [TestCase(FinalAccusationStage.CauseOfDeath)]
        [TestCase(FinalAccusationStage.BodyTransport)]
        [TestCase(FinalAccusationStage.MurderMotive)]
        [TestCase(FinalAccusationStage.OrpheusMastermind)]
        public void TryGet_ReturnsRequestedStage(FinalAccusationStage stage)
        {
            bool found = FinalAccusationStageCatalog.TryGet(
                stage,
                out FinalAccusationStageDefinition definition);

            Assert.That(found, Is.True);
            Assert.That(definition.Stage, Is.EqualTo(stage));
        }

        [Test]
        public void TryGet_RejectsUnknownStage()
        {
            bool found = FinalAccusationStageCatalog.TryGet(
                (FinalAccusationStage)999,
                out FinalAccusationStageDefinition definition);

            Assert.That(found, Is.False);
            Assert.That(definition, Is.Null);
        }

        [Test]
        public void CorrectValues_CreateOfficialAccusation()
        {
            var values = FinalAccusationStageCatalog.All
                .ToDictionary(
                    stage => stage.Stage,
                    stage => stage.CorrectOption.EnumValue);

            var accusation = new FinalAccusation
            {
                Accused = (AccusedPerson)values[FinalAccusationStage.Culprit],
                Location = (MurderLocation)values[
                    FinalAccusationStage.MurderLocation],
                Method = (MurderMethod)values[
                    FinalAccusationStage.CauseOfDeath],
                Transport = (BodyTransport)values[
                    FinalAccusationStage.BodyTransport],
                DanielBelievedTarget = (DanielTargetBelief)values[
                    FinalAccusationStage.MurderMotive],
                OrpheusDesign = (OrpheusEventDesign)values[
                    FinalAccusationStage.OrpheusMastermind]
            };

            Assert.That(accusation.Accused, Is.EqualTo(AccusedPerson.Evelyn));
            Assert.That(
                accusation.Location,
                Is.EqualTo(MurderLocation.BallastControlAnnex));
            Assert.That(
                accusation.Method,
                Is.EqualTo(MurderMethod.NitrogenSuffocation));
            Assert.That(
                accusation.Transport,
                Is.EqualTo(BodyTransport.CeilingServiceRail));
            Assert.That(
                accusation.DanielBelievedTarget,
                Is.EqualTo(DanielTargetBelief.Misconception));
            Assert.That(
                accusation.OrpheusDesign,
                Is.EqualTo(OrpheusEventDesign.Evelyn));
        }

        [Test]
        public void ExistingSaveValues_KeepPreviouslyCorrectMeanings()
        {
            Assert.That((int)AccusedPerson.Evelyn, Is.EqualTo(1));
            Assert.That(
                (int)MurderLocation.BallastControlAnnex,
                Is.EqualTo(2));
            Assert.That(
                (int)MurderMethod.NitrogenSuffocation,
                Is.EqualTo(2));
            Assert.That(
                (int)BodyTransport.CeilingServiceRail,
                Is.EqualTo(2));
            Assert.That(
                (int)DanielTargetBelief.Misconception,
                Is.EqualTo(2));
            Assert.That(
                (int)OrpheusEventDesign.Evelyn,
                Is.EqualTo(2));
        }
    }
}
