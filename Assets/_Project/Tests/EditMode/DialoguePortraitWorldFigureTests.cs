using NUnit.Framework;
using Wake.Narrative;

namespace Wake.Tests
{
    public sealed class DialoguePortraitWorldFigureTests
    {
        [TestCase("DANIEL")]
        [TestCase("CLAIRE")]
        [TestCase("DOCK_PORTER")]
        [TestCase("PASSENGER_A")]
        [TestCase("CREW_ENGINEER")]
        public void DialogueFigure_UsesCompleteCharacterArtwork(
            string characterId)
        {
            DialoguePortraitAsset asset =
                DialoguePortraitCatalog.ResolveWorldFigure(characterId);

            Assert.That(asset.Found, Is.True);
            Assert.That(asset.Texture, Is.Not.Null);
            Assert.That(asset.UvRect.y, Is.Zero);
            Assert.That(asset.UvRect.height, Is.EqualTo(1f));
            if (characterId == "DOCK_PORTER" ||
                characterId == "PASSENGER_A" ||
                characterId == "CREW_ENGINEER")
            {
                Assert.That(asset.UvRect.x, Is.Zero);
                Assert.That(asset.UvRect.width, Is.EqualTo(0.25f));
            }
            Assert.That(asset.AspectRatio, Is.GreaterThan(0f));
            Assert.That(asset.UsesExpression, Is.False);
        }
    }
}
