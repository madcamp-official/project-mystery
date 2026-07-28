using NUnit.Framework;
using Wake.UI;

namespace Wake.Tests
{
    public sealed class InterrogationRelationshipPresentationTests
    {
        [TestCase(0, "증언을 거부함")]
        [TestCase(1, "경계하고 있음")]
        [TestCase(3, "답변을 망설임")]
        [TestCase(5, "신뢰하기 시작함")]
        public void Trust_UsesNaturalLanguage(
            int trust,
            string expected)
        {
            Assert.That(
                InterrogationRelationshipPresentation.ResolveTrust(trust),
                Is.EqualTo(expected));
        }

        [TestCase(5, 5, "질문할 여유가 있음")]
        [TestCase(2, 5, "질문 기회가 얼마 남지 않음")]
        [TestCase(1, 5, "마지막 질문을 신중히 선택해야 함")]
        [TestCase(0, 5, "더 이상 질문할 수 없음")]
        public void QuestionBudget_HidesInternalNumbers(
            int remaining,
            int maximum,
            string expected)
        {
            Assert.That(
                InterrogationRelationshipPresentation.ResolveQuestionBudget(
                    remaining,
                    maximum),
                Is.EqualTo(expected));
        }
    }
}
