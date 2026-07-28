using NUnit.Framework;
using Wake.Narrative;

namespace Wake.Tests
{
    public sealed class DialoguePortraitWorldFigureTests
    {
        [TestCase("DANIEL")]
        [TestCase("CLAIRE")]
        [TestCase("DOCK_PORTER")]
        public void DialogueFigure_UsesCompleteCharacterArtwork(
            string characterId)
        {
            DialoguePortraitAsset asset =
                DialoguePortraitCatalog.ResolveWorldFigure(characterId);

            Assert.That(asset.Found, Is.True);
            Assert.That(asset.Texture, Is.Not.Null);
            Assert.That(asset.UvRect.y, Is.Zero);
            Assert.That(asset.UvRect.height, Is.EqualTo(1f));
            Assert.That(asset.AspectRatio, Is.GreaterThan(0f));
            Assert.That(asset.UsesExpression, Is.False);
        }
    }
}
