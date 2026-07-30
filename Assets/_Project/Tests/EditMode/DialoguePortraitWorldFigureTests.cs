using NUnit.Framework;
using Wake.Exploration;
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
        [TestCase("CREW_ATTENDANT")]
        public void DialogueFigure_UsesCompleteCharacterArtwork(
            string characterId)
        {
            DialoguePortraitAsset asset =
                DialoguePortraitCatalog.ResolveWorldFigure(characterId);

            Assert.That(asset.Found, Is.True);
            Assert.That(asset.Texture, Is.Not.Null);
            if ((characterId == "CREW_ATTENDANT" ||
                 characterId == "CREW_ATTENDANT_BALLROOM") &&
                AmbientWorldCharacterCatalog.TryGetAsset(
                    characterId,
                    out AmbientWorldCharacterAsset worldAsset))
            {
                Assert.That(
                    asset.UvRect,
                    Is.EqualTo(worldAsset.UvRect),
                    "Dialogue and exploration must share the exact same " +
                    "isolated world-figure crop.");
            }
            Assert.That(asset.AspectRatio, Is.GreaterThan(0f));
            Assert.That(asset.UsesExpression, Is.False);
        }
    }
}
