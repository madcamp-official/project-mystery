using NUnit.Framework;
using Wake.Narrative;

namespace Wake.Tests
{
    public sealed class NpcAnxietyExpressionPolicyTests
    {
        [Test]
        public void LowAnxiety_PreservesAuthoredEmotion()
        {
            Assert.That(
                NpcAnxietyExpressionPolicy.Resolve(
                    "HELENA",
                    PortraitEmotion.Positive,
                    39),
                Is.EqualTo(PortraitEmotion.Positive));
        }

        [Test]
        public void HighAnxiety_UsesConcernedOrAngryNpcExpressions()
        {
            PortraitEmotion[] emotions =
            {
                NpcAnxietyExpressionPolicy.Resolve(
                    "HELENA", PortraitEmotion.Neutral, 70),
                NpcAnxietyExpressionPolicy.Resolve(
                    "OWEN", PortraitEmotion.Positive, 70),
                NpcAnxietyExpressionPolicy.Resolve(
                    "RICHARD", PortraitEmotion.Neutral, 100)
            };

            Assert.That(
                emotions,
                Has.All.Matches<PortraitEmotion>(emotion =>
                    emotion == PortraitEmotion.Concerned ||
                    emotion == PortraitEmotion.Angry));
            Assert.That(emotions, Does.Contain(PortraitEmotion.Concerned));
            Assert.That(emotions, Does.Contain(PortraitEmotion.Angry));
        }

        [Test]
        public void PlayerExpression_IsNotOverriddenByShipAnxiety()
        {
            Assert.That(
                NpcAnxietyExpressionPolicy.Resolve(
                    "ADRIAN",
                    PortraitEmotion.Positive,
                    100),
                Is.EqualTo(PortraitEmotion.Positive));
        }

        [TestCase("PASSENGER_A")]
        [TestCase("PASSENGER_B")]
        [TestCase("PASSENGER_C")]
        [TestCase("PASSENGER_D")]
        [TestCase("PASSENGER_E")]
        [TestCase("PASSENGER_F")]
        [TestCase("CREW_ATTENDANT")]
        [TestCase("CREW_ENGINEER")]
        [TestCase("CREW_SECURITY")]
        public void HighAnxiety_ChangesPassengerAndCrewExpression(
            string characterId)
        {
            PortraitEmotion emotion =
                NpcAnxietyExpressionPolicy.Resolve(
                    characterId,
                    PortraitEmotion.Neutral,
                    70);

            Assert.That(
                emotion,
                Is.EqualTo(PortraitEmotion.Concerned)
                    .Or.EqualTo(PortraitEmotion.Angry));
        }
    }
}
