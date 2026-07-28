using NUnit.Framework;
using Wake.Narrative;

namespace Wake.Tests
{
    public sealed class DialoguePresentationPolicyTests
    {
        [Test]
        public void Character_UsesRightFocusPortrait()
        {
            DialoguePresentationSpec spec =
                DialoguePresentationPolicy.ForProduction(
                    Speaker("CLAIRE", DialogueSpeakerKind.Character));

            Assert.That(spec.Mode, Is.EqualTo(DialoguePresentationMode.Focus));
            Assert.That(spec.PortraitSide, Is.EqualTo(DialoguePortraitSide.Right));
            Assert.That(spec.ShowPortrait, Is.True);
            Assert.That(spec.ShowSpeakerName, Is.True);
            Assert.That(spec.BackgroundDimAlpha, Is.InRange(0.25f, 0.4f));
        }

        [Test]
        public void Monologue_UsesLeftFocusPortrait()
        {
            DialoguePresentationSpec spec =
                DialoguePresentationPolicy.ForProduction(
                    Speaker("ADRIAN", DialogueSpeakerKind.Monologue));

            Assert.That(spec.Mode, Is.EqualTo(DialoguePresentationMode.Focus));
            Assert.That(spec.PortraitSide, Is.EqualTo(DialoguePortraitSide.Left));
            Assert.That(spec.PortraitHeightRatio, Is.LessThan(0.55f));
        }

        [Test]
        public void AmbientCharacter_UsesRightFocusLayout()
        {
            DialoguePresentationSpec spec =
                DialoguePresentationPolicy.ForAmbient(
                    Speaker("CLAIRE", DialogueSpeakerKind.Character));

            Assert.That(spec.Mode, Is.EqualTo(DialoguePresentationMode.Focus));
            Assert.That(spec.PortraitSide, Is.EqualTo(DialoguePortraitSide.Right));
            Assert.That(spec.BackgroundDimAlpha, Is.GreaterThanOrEqualTo(0.35f));
            Assert.That(spec.ShowSpeakerName, Is.True);
        }

        [Test]
        public void GenericNpc_UsesRightFocusLayout()
        {
            DialoguePresentationSpec spec =
                DialoguePresentationPolicy.ForProduction(
                    Speaker("NPC", DialogueSpeakerKind.NonPlayer));

            Assert.That(spec.Mode, Is.EqualTo(DialoguePresentationMode.Focus));
            Assert.That(spec.PortraitSide, Is.EqualTo(DialoguePortraitSide.Right));
            Assert.That(spec.ShowPortrait, Is.True);
        }

        [Test]
        public void Narration_HidesPortraitAndName()
        {
            DialoguePresentationSpec spec =
                DialoguePresentationPolicy.ForProduction(
                    Speaker(string.Empty, DialogueSpeakerKind.Narration));

            Assert.That(spec.Mode, Is.EqualTo(DialoguePresentationMode.Narration));
            Assert.That(spec.PortraitSide, Is.EqualTo(DialoguePortraitSide.Hidden));
            Assert.That(spec.ShowPortrait, Is.False);
            Assert.That(spec.ShowSpeakerName, Is.False);
        }

        [Test]
        public void System_IsNotPresentedAsDialogue()
        {
            DialoguePresentationSpec spec =
                DialoguePresentationPolicy.ForProduction(
                    Speaker(string.Empty, DialogueSpeakerKind.System));

            Assert.That(spec, Is.EqualTo(DialoguePresentationPolicy.Hidden));
            Assert.That(spec.IsVisible, Is.False);
        }

        [Test]
        public void RecordedVoice_UsesLeftFocusLayout()
        {
            DialoguePresentationSpec spec =
                DialoguePresentationPolicy.ForProduction(
                    Speaker("EVELYN", DialogueSpeakerKind.RecordedVoice));

            Assert.That(spec.Mode, Is.EqualTo(DialoguePresentationMode.Focus));
            Assert.That(spec.PortraitSide, Is.EqualTo(DialoguePortraitSide.Left));
            Assert.That(spec.ShowSpeakerName, Is.True);
        }

        [Test]
        public void Investigation_HasDimWithoutDialoguePortrait()
        {
            DialoguePresentationSpec spec =
                DialoguePresentationPolicy.ForInvestigation();

            Assert.That(
                spec.Mode,
                Is.EqualTo(DialoguePresentationMode.Investigation));
            Assert.That(spec.ShowPortrait, Is.False);
            Assert.That(spec.BackgroundDimAlpha, Is.GreaterThan(0f));
        }

        [Test]
        public void Spec_ClampsVisualRatios()
        {
            var spec = new DialoguePresentationSpec(
                DialoguePresentationMode.Focus,
                DialoguePortraitSide.Right,
                2f,
                -1f,
                showSpeakerName: true);

            Assert.That(spec.BackgroundDimAlpha, Is.EqualTo(1f));
            Assert.That(spec.PortraitHeightRatio, Is.EqualTo(0f));
            Assert.That(spec.ShowPortrait, Is.False);
        }

        [Test]
        public void EqualSpecs_HaveStableEquality()
        {
            DialoguePresentationSpec first =
                DialoguePresentationPolicy.ForInvestigation();
            DialoguePresentationSpec second =
                DialoguePresentationPolicy.ForInvestigation();

            Assert.That(first, Is.EqualTo(second));
            Assert.That(first == second, Is.True);
            Assert.That(first.GetHashCode(), Is.EqualTo(second.GetHashCode()));
        }

        private static DialogueSpeakerIdentity Speaker(
            string portraitId,
            DialogueSpeakerKind kind) =>
            new(portraitId, kind);
    }
}
