using System.Linq;
using NUnit.Framework;
using Wake.Exploration;

namespace Wake.Tests
{
    public sealed class ScenePresencePresentationPolicyTests
    {
        [Test]
        public void Atrium_MarksAllFourSuspectsAsFocusParticipants()
        {
            Assert.That(
                ScenePresenceCatalog.TryGet(
                    "D1-01",
                    out ScenePresenceRecord scene),
                Is.True);

            string[] focusParticipants =
                ScenePresencePresentationPolicy
                    .SelectVisible(scene, "ATRIUM", visibleLimit: 5)
                    .Where(character => character.IsFocusParticipant)
                    .Select(character => character.CharacterId)
                    .OrderBy(id => id)
                    .ToArray();

            Assert.That(
                focusParticipants,
                Is.EqualTo(new[] { "CLAIRE", "HELENA", "MARCUS", "OWEN" }));
        }
    }
}
