using System;

namespace Wake.Narrative
{
    public enum DialoguePresentationMode
    {
        Hidden,
        Focus,
        Compact,
        Narration,
        Investigation
    }

    public enum DialoguePortraitSide
    {
        Hidden,
        Left,
        Right
    }

    public readonly struct DialoguePresentationSpec : IEquatable<DialoguePresentationSpec>
    {
        public DialoguePresentationSpec(
            DialoguePresentationMode mode,
            DialoguePortraitSide portraitSide,
            float backgroundDimAlpha,
            float portraitHeightRatio,
            bool showSpeakerName)
        {
            Mode = mode;
            PortraitSide = portraitSide;
            BackgroundDimAlpha = Clamp01(backgroundDimAlpha);
            PortraitHeightRatio = Clamp01(portraitHeightRatio);
            ShowSpeakerName = showSpeakerName;
        }

        public DialoguePresentationMode Mode { get; }
        public DialoguePortraitSide PortraitSide { get; }
        public float BackgroundDimAlpha { get; }
        public float PortraitHeightRatio { get; }
        public bool ShowSpeakerName { get; }
        public bool ShowPortrait =>
            PortraitSide != DialoguePortraitSide.Hidden &&
            PortraitHeightRatio > 0f;
        public bool IsVisible => Mode != DialoguePresentationMode.Hidden;
        public bool UsesFocusLayout =>
            Mode is DialoguePresentationMode.Focus or
                DialoguePresentationMode.Narration;

        public bool Equals(DialoguePresentationSpec other) =>
            Mode == other.Mode &&
            PortraitSide == other.PortraitSide &&
            Math.Abs(BackgroundDimAlpha - other.BackgroundDimAlpha) < 0.001f &&
            Math.Abs(PortraitHeightRatio - other.PortraitHeightRatio) < 0.001f &&
            ShowSpeakerName == other.ShowSpeakerName;

        public override bool Equals(object obj) =>
            obj is DialoguePresentationSpec other && Equals(other);

        public override int GetHashCode() =>
            HashCode.Combine(
                Mode,
                PortraitSide,
                BackgroundDimAlpha,
                PortraitHeightRatio,
                ShowSpeakerName);

        public static bool operator ==(
            DialoguePresentationSpec left,
            DialoguePresentationSpec right) => left.Equals(right);

        public static bool operator !=(
            DialoguePresentationSpec left,
            DialoguePresentationSpec right) => !left.Equals(right);

        private static float Clamp01(float value) =>
            value < 0f ? 0f : value > 1f ? 1f : value;
    }

    public static class DialoguePresentationPolicy
    {
        public static readonly DialoguePresentationSpec Hidden =
            new(
                DialoguePresentationMode.Hidden,
                DialoguePortraitSide.Hidden,
                0f,
                0f,
                showSpeakerName: false);

        public static DialoguePresentationSpec ForAmbient(
            DialogueSpeakerIdentity speaker)
        {
            if (HasNoPortrait(speaker))
                return ForSpeaker(speaker);

            return new DialoguePresentationSpec(
                DialoguePresentationMode.Focus,
                DialoguePortraitSide.Right,
                0.38f,
                0.68f,
                showSpeakerName: true);
        }

        public static DialoguePresentationSpec ForProduction(
            DialogueSpeakerIdentity speaker)
        {
            return ForSpeaker(speaker);
        }

        public static DialoguePresentationSpec ForInvestigation() =>
            new(
                DialoguePresentationMode.Investigation,
                DialoguePortraitSide.Hidden,
                0.20f,
                0f,
                showSpeakerName: false);

        private static DialoguePresentationSpec ForSpeaker(
            DialogueSpeakerIdentity speaker)
        {
            switch (speaker.Kind)
            {
                case DialogueSpeakerKind.Narration:
                    return new DialoguePresentationSpec(
                        DialoguePresentationMode.Narration,
                        DialoguePortraitSide.Hidden,
                        0.24f,
                        0f,
                        showSpeakerName: false);

                case DialogueSpeakerKind.System:
                    return Hidden;

                case DialogueSpeakerKind.Monologue:
                    return new DialoguePresentationSpec(
                        DialoguePresentationMode.Focus,
                        DialoguePortraitSide.Left,
                        0.30f,
                        0.48f,
                        showSpeakerName: true);

                case DialogueSpeakerKind.RecordedVoice:
                    return new DialoguePresentationSpec(
                        DialoguePresentationMode.Focus,
                        DialoguePortraitSide.Left,
                        0.32f,
                        0.48f,
                        showSpeakerName: true);

                case DialogueSpeakerKind.NonPlayer:
                    return new DialoguePresentationSpec(
                        DialoguePresentationMode.Focus,
                        DialoguePortraitSide.Right,
                        0.38f,
                        0.68f,
                        showSpeakerName: true);

                default:
                    return new DialoguePresentationSpec(
                        DialoguePresentationMode.Focus,
                        DialoguePortraitSide.Right,
                        0.38f,
                        0.68f,
                        showSpeakerName: true);
            }
        }

        private static bool HasNoPortrait(DialogueSpeakerIdentity speaker) =>
            speaker.Kind is DialogueSpeakerKind.Narration or
                DialogueSpeakerKind.System ||
            string.IsNullOrWhiteSpace(speaker.PortraitId);
    }
}
