using NUnit.Framework;
using Wake.Narrative;
using Wake.UI;

namespace Wake.Tests
{
    public sealed class DialoguePresentationViewTests
    {
        [TestCase(
            DialoguePresentationMode.Focus,
            "dialogue.focus-panel")]
        [TestCase(
            DialoguePresentationMode.Compact,
            "dialogue.compact-panel")]
        [TestCase(
            DialoguePresentationMode.Narration,
            "dialogue.narration-panel")]
        public void Mode_SelectsExpectedPanelSlot(
            DialoguePresentationMode mode,
            string expected)
        {
            Assert.That(
                DialoguePresentationView.PanelSlotFor(mode),
                Is.EqualTo(expected));
        }

        [Test]
        public void FocusCharacter_SelectsRightPortraitSlot()
        {
            DialoguePresentationSpec spec =
                DialoguePresentationPolicy.ForProduction(
                    new DialogueSpeakerIdentity(
                        "CLAIRE",
                        DialogueSpeakerKind.Character));

            Assert.That(
                DialoguePresentationView.PortraitSlotFor(spec),
                Is.EqualTo("dialogue.focus-portrait-right"));
        }

        [Test]
        public void Monologue_SelectsLeftPortraitSlot()
        {
            DialoguePresentationSpec spec =
                DialoguePresentationPolicy.ForProduction(
                    new DialogueSpeakerIdentity(
                        "ADRIAN",
                        DialogueSpeakerKind.Monologue));

            Assert.That(
                DialoguePresentationView.PortraitSlotFor(spec),
                Is.EqualTo("dialogue.focus-portrait-left"));
        }

        [Test]
        public void Ambient_SelectsCompactPortraitSlot()
        {
            DialoguePresentationSpec spec =
                DialoguePresentationPolicy.ForAmbient(
                    new DialogueSpeakerIdentity(
                        "PASSENGER_A",
                        DialogueSpeakerKind.Character));

            Assert.That(
                DialoguePresentationView.PortraitSlotFor(spec),
                Is.EqualTo("dialogue.compact-portrait"));
        }

        [Test]
        public void Narration_DoesNotSelectPortraitSlot()
        {
            DialoguePresentationSpec spec =
                DialoguePresentationPolicy.ForProduction(
                    new DialogueSpeakerIdentity(
                        string.Empty,
                        DialogueSpeakerKind.Narration));

            Assert.That(
                DialoguePresentationView.PortraitSlotFor(spec),
                Is.Empty);
        }
    }
}
