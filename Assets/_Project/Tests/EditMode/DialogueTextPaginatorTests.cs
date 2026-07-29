using NUnit.Framework;
using Wake.Narrative;

namespace Wake.Tests
{
    public sealed class DialogueTextPaginatorTests
    {
        [Test]
        public void LongDialogue_IsSplitWithoutLosingContent()
        {
            const string text =
                "첫 번째 문장을 모두 보여 줍니다. " +
                "두 번째 문장도 생략하지 않습니다. " +
                "마지막 문장까지 플레이어가 읽을 수 있어야 합니다.";

            var pages = DialogueTextPaginator.Split(text, 24);

            Assert.That(pages.Count, Is.GreaterThan(1));
            Assert.That(
                string.Concat(pages).Replace(" ", string.Empty),
                Is.EqualTo(text.Replace(" ", string.Empty)));
        }

        [Test]
        public void LongUnbrokenText_IsSplitAtCharacterLimit()
        {
            const string text = "가나다라마바사아자차카타파하가나다라마바사";

            var pages = DialogueTextPaginator.Split(text, 20);

            Assert.That(pages, Has.Count.EqualTo(2));
            Assert.That(string.Concat(pages), Is.EqualTo(text));
            Assert.That(pages[0].Length, Is.LessThanOrEqualTo(20));
        }

        [Test]
        public void EmptyDialogue_StillProducesOneDisplayPage()
        {
            var pages = DialogueTextPaginator.Split(string.Empty);

            Assert.That(pages.Count, Is.EqualTo(1));
            Assert.That(pages[0], Is.Empty);
        }
    }
}
