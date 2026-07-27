using System.Linq;
using NUnit.Framework;
using Wake.Narrative;

namespace Wake.Tests
{
    public sealed class ProductionChoiceCapacityTests
    {
        [Test]
        public void Flow_PresentsEightContiguousChoicesInOneGroup()
        {
            ProductionDialogueFlow flow = CreateFlow(8);

            Assert.That(flow.StartScene("TEST"), Is.True);
            Assert.That(flow.IsAwaitingChoice, Is.True);
            Assert.That(
                flow.Choices.Select(record => record.ChoiceId),
                Is.EqualTo(Enumerable.Range(1, 8)
                    .Select(index => $"D4-04_Q{index}")));
            Assert.That(flow.Warnings, Is.Empty);
        }

        [Test]
        public void Flow_CanSelectLastChoiceAndCompleteScene()
        {
            ProductionDialogueFlow flow = CreateFlow(8);
            flow.StartScene("TEST");

            Assert.That(flow.SelectChoice(7), Is.True);
            Assert.That(flow.IsAwaitingChoice, Is.False);
            Assert.That(flow.IsComplete, Is.True);
            Assert.That(flow.IsSceneCompleted("TEST"), Is.True);
        }

        [Test]
        public void Flow_ReportsContentBeyondSupportedCapacity()
        {
            ProductionDialogueFlow flow = CreateFlow(9);

            Assert.That(flow.StartScene("TEST"), Is.True);
            Assert.That(
                flow.Choices,
                Has.Count.EqualTo(ProductionDialogueFlow.ChoiceCapacity));
            Assert.That(
                flow.Warnings,
                Has.Some.Contains("9 contiguous choices"));
        }

        private static ProductionDialogueFlow CreateFlow(int choiceCount)
        {
            string header = string.Join(",", DialogueCsvParser.ProductionHeaders);
            string rows = string.Join(
                "\n",
                Enumerable.Range(1, choiceCount).Select(index =>
                    $"TEST_{index:D3},TEST,{index},choice,choice," +
                    $"PLAYER_CHOICE,선택 {index},choice,,D4-04_Q{index}," +
                    $",UI,N,D4-04_Q,"));
            DialogueCsvParseResult parsed =
                DialogueCsvParser.Parse($"{header}\n{rows}");
            Assert.That(parsed.Success, Is.True, string.Join("\n", parsed.Errors));
            return new ProductionDialogueFlow(parsed.Records);
        }
    }
}
