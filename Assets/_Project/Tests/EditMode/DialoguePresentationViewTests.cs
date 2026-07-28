using NUnit.Framework;
using Wake.Narrative;
using Wake.UI;

namespace Wake.Tests
{
    public sealed class DialoguePresentationViewTests
    {
        [TestCase(
            DialoguePresentationMode.Focus,
            "dialogue.focus-panel-left")]
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
        public void Ambient_SelectsRightFocusPortraitSlot()
        {
            DialoguePresentationSpec spec =
                DialoguePresentationPolicy.ForAmbient(
                    new DialogueSpeakerIdentity(
                        "PASSENGER_A",
                        DialogueSpeakerKind.Character));

            Assert.That(
                DialoguePresentationView.PortraitSlotFor(spec),
                Is.EqualTo("dialogue.focus-portrait-right"));
        }

        [Test]
        public void LeftPortrait_SelectsNonOverlappingRightPanel()
        {
            DialoguePresentationSpec spec =
                DialoguePresentationPolicy.ForProduction(
                    new DialogueSpeakerIdentity(
                        "ADRIAN",
                        DialogueSpeakerKind.Monologue));

            Assert.That(
                DialoguePresentationView.PanelSlotFor(spec),
                Is.EqualTo("dialogue.focus-panel-right"));
        }

        [Test]
        public void RightPortrait_SelectsNonOverlappingLeftPanel()
        {
            DialoguePresentationSpec spec =
                DialoguePresentationPolicy.ForProduction(
                    new DialogueSpeakerIdentity(
                        "CLAIRE",
                        DialogueSpeakerKind.Character));

            Assert.That(
                DialoguePresentationView.PanelSlotFor(spec),
                Is.EqualTo("dialogue.focus-panel-left"));
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

        [Test]
        public void LeftPortrait_SelectsRightAuthoredContentSlots()
        {
            DialoguePresentationSpec spec =
                DialoguePresentationPolicy.ForProduction(
                    new DialogueSpeakerIdentity(
                        "ADRIAN",
                        DialogueSpeakerKind.Monologue));

            Assert.That(
                DialoguePresentationView.TextSlotFor(spec),
                Is.EqualTo("dialogue.focus-text-right"));
            Assert.That(
                DialoguePresentationView.SpeakerNameSlotFor(spec),
                Is.EqualTo("dialogue.speaker-name-right"));
            Assert.That(
                DialoguePresentationView.AdvanceSlotFor(spec),
                Is.EqualTo("dialogue.advance-right"));
            Assert.That(
                DialoguePresentationView.ChoicesSlotFor(spec),
                Is.EqualTo("dialogue.choices-right"));
        }

        [Test]
        public void RightPortrait_SelectsLeftAuthoredContentSlots()
        {
            DialoguePresentationSpec spec =
                DialoguePresentationPolicy.ForProduction(
                    new DialogueSpeakerIdentity(
                        "CLAIRE",
                        DialogueSpeakerKind.Character));

            Assert.That(
                DialoguePresentationView.TextSlotFor(spec),
                Is.EqualTo("dialogue.focus-text-left"));
            Assert.That(
                DialoguePresentationView.SpeakerNameSlotFor(spec),
                Is.EqualTo("dialogue.speaker-name-left"));
            Assert.That(
                DialoguePresentationView.AdvanceSlotFor(spec),
                Is.EqualTo("dialogue.advance-left"));
            Assert.That(
                DialoguePresentationView.ChoicesSlotFor(spec),
                Is.EqualTo("dialogue.choices-left"));
        }

        [Test]
        public void Narration_SelectsCenterAuthoredContentSlots()
        {
            DialoguePresentationSpec spec =
                DialoguePresentationPolicy.ForProduction(
                    new DialogueSpeakerIdentity(
                        string.Empty,
                        DialogueSpeakerKind.Narration));

            Assert.That(
                DialoguePresentationView.TextSlotFor(spec),
                Is.EqualTo("dialogue.narration-text"));
            Assert.That(
                DialoguePresentationView.AdvanceSlotFor(spec),
                Is.EqualTo("dialogue.advance-center"));
        }

        [TestCase(UiPrimaryPanel.None, false)]
        [TestCase(UiPrimaryPanel.Start, false)]
        [TestCase(UiPrimaryPanel.Ingame, true)]
        [TestCase(UiPrimaryPanel.Map, true)]
        [TestCase(UiPrimaryPanel.Evidence, true)]
        public void HiddenDialogue_ShowsHudOnlyOutsideStartScreen(
            UiPrimaryPanel panel,
            bool expected)
        {
            Assert.That(
                DialoguePresentationView.ShouldShowHud(
                    DialoguePresentationPolicy.Hidden,
                    panel),
                Is.EqualTo(expected));
        }

        [Test]
        public void VisibleDialogue_AlwaysHidesHud()
        {
            DialoguePresentationSpec spec =
                DialoguePresentationPolicy.ForProduction(
                    new DialogueSpeakerIdentity(
                        "DANIEL",
                        DialogueSpeakerKind.Character));

            Assert.That(
                DialoguePresentationView.ShouldShowHud(
                    spec,
                    UiPrimaryPanel.Ingame),
                Is.False);
        }
    }
}
