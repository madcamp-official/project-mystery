using System.Linq;
using NUnit.Framework;
using Wake.Exploration;

namespace Wake.Tests
{
    public sealed class MainCharacterWorldLineCatalogTests
    {
        private static readonly string[] DayTieredCharacters =
        {
            "RICHARD", "EVELYN", "CLAIRE", "THOMAS",
            "MARCUS", "HELENA", "OWEN"
        };

        [Test]
        public void EveryDayTieredCharacter_HasThreeDistinctNormalStateLines()
        {
            foreach (string character in DayTieredCharacters)
            {
                string day1 = MainCharacterWorldLineCatalog.Get(
                    character, SceneCharacterState.Normal, 1);
                string day3 = MainCharacterWorldLineCatalog.Get(
                    character, SceneCharacterState.Normal, 3);
                string day7 = MainCharacterWorldLineCatalog.Get(
                    character, SceneCharacterState.Normal, 7);

                Assert.That(
                    new[] { day1, day3, day7 }.Distinct().Count(),
                    Is.EqualTo(3),
                    character);
            }
        }

        [Test]
        public void EveryDayTieredCharacter_HasThreeDistinctCompletedLines()
        {
            foreach (string character in DayTieredCharacters)
            {
                string day1 = MainCharacterWorldLineCatalog.GetCompleted(
                    character, SceneCharacterState.Normal, 1);
                string day3 = MainCharacterWorldLineCatalog.GetCompleted(
                    character, SceneCharacterState.Normal, 3);
                string day7 = MainCharacterWorldLineCatalog.GetCompleted(
                    character, SceneCharacterState.Normal, 7);

                Assert.That(
                    new[] { day1, day3, day7 }.Distinct().Count(),
                    Is.EqualTo(3),
                    character);
            }
        }

        [TestCase(1)]
        [TestCase(3)]
        [TestCase(7)]
        public void InjuredAndDetained_IgnoreDayAndOverrideNormalLine(
            int day)
        {
            Assert.That(
                MainCharacterWorldLineCatalog.Get(
                    "MARCUS", SceneCharacterState.Injured, day),
                Is.EqualTo("부상 부위가 아직 좋지 않습니다. 필요한 내용만 짧게 묻죠."));
            Assert.That(
                MainCharacterWorldLineCatalog.Get(
                    "EVELYN", SceneCharacterState.Detained, day),
                Is.EqualTo("경비가 지켜보는 자리군요. 정식 심문에서 같은 답을 드리겠습니다."));
        }

        [Test]
        public void Daniel_KeepsHisSingleLineRegardlessOfDay()
        {
            string day1 = MainCharacterWorldLineCatalog.Get(
                "DANIEL", SceneCharacterState.Normal, 1);
            string day3 = MainCharacterWorldLineCatalog.Get(
                "DANIEL", SceneCharacterState.Normal, 3);

            Assert.That(day1, Is.EqualTo(day3));
        }
    }
}
