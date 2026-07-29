using NUnit.Framework;
using Wake.Core;

namespace Wake.Tests.EditMode
{
    public class AudioCueCatalogTests
    {
        [Test]
        public void ExteriorLocations_UseWindAndWavesLayers()
        {
            Assert.That(
                AudioCueCatalog.TryGetLocationCue(
                    "PORT",
                    out LocationAudioCue port),
                Is.True);
            Assert.That(
                port.PrimaryAmbienceKey,
                Is.EqualTo("SoundEffect/Sound_of_waves_on_the_beach_1"));
            Assert.That(
                port.SecondaryAmbienceKey,
                Is.EqualTo("SoundEffect/wind_noise"));

            Assert.That(
                AudioCueCatalog.TryGetLocationCue(
                    "OPEN_DECK",
                    out LocationAudioCue openDeck),
                Is.True);
            Assert.That(openDeck.PrimaryAmbienceKey, Does.Contain("wind_noise"));
            Assert.That(
                openDeck.SecondaryAmbienceKey,
                Does.Contain("Sound_of_waves"));
        }

        [Test]
        public void MechanicalLocations_UseFanAndEngineLayers()
        {
            Assert.That(
                AudioCueCatalog.TryGetLocationCue(
                    "ENGINE_CONTROL",
                    out LocationAudioCue engine),
                Is.True);
            Assert.That(
                engine.PrimaryAmbienceKey,
                Is.EqualTo("SoundEffect/factory_exhaust_fan_sound"));
            Assert.That(
                engine.SecondaryAmbienceKey,
                Is.EqualTo("SoundEffect/boat_engine_sound"));
        }

        [Test]
        public void MetalFootstepPlaceholder_IsReplacedWithIronDoorKnock()
        {
            Assert.That(
                AudioCueCatalog.MetalFootstepReplacementKey,
                Is.EqualTo(AudioCueCatalog.IronDoorKnockKey));
            Assert.That(
                AudioCueCatalog.MetalFootstepReplacementKey,
                Does.Not.Contain("Footsteps"));
        }

        [Test]
        public void LocationLookup_IsCaseInsensitiveAndTrimmed()
        {
            Assert.That(
                AudioCueCatalog.TryGetLocationCue(
                    "  horizon ",
                    out LocationAudioCue cue),
                Is.True);
            Assert.That(cue.MusicKey, Is.EqualTo("BGM/The_Horizon_Room"));
            Assert.That(cue.CrossfadeSeconds, Is.GreaterThan(0f));
        }
    }
}
