using System;
using System.Text;

namespace Wake.Narrative
{
    [Serializable]
    public sealed class DialogueRecord
    {
        public string SceneId { get; }
        public int Order { get; }
        public string Speaker { get; }
        public string TextKo { get; }
        public string Emotion { get; }
        public string Condition { get; }
        public string ChoiceId { get; }
        public string NextOrEffect { get; }
        public string StageDirection { get; }
        public bool VoiceRequired { get; }
        public int SourceRow { get; }
        public string StableLineId => CreateStableLineId(SceneId, Order);

        public DialogueRecord(
            string sceneId,
            int order,
            string speaker,
            string textKo,
            string emotion,
            string condition,
            string choiceId,
            string nextOrEffect,
            string stageDirection,
            bool voiceRequired,
            int sourceRow)
        {
            SceneId = sceneId;
            Order = order;
            Speaker = speaker;
            TextKo = textKo;
            Emotion = emotion;
            Condition = condition;
            ChoiceId = choiceId;
            NextOrEffect = nextOrEffect;
            StageDirection = stageDirection;
            VoiceRequired = voiceRequired;
            SourceRow = sourceRow;
        }

        public DialogueLine ToLegacyLine()
        {
            return new DialogueLine(SceneId, Speaker, TextKo, Emotion, VoiceRequired);
        }

        public static string CreateStableLineId(string sceneId, int order)
        {
            var result = new StringBuilder();
            bool lastWasSeparator = false;

            foreach (char character in (sceneId ?? string.Empty).Trim().ToLowerInvariant())
            {
                if (char.IsLetterOrDigit(character))
                {
                    result.Append(character);
                    lastWasSeparator = false;
                }
                else if (!lastWasSeparator && result.Length > 0)
                {
                    result.Append('_');
                    lastWasSeparator = true;
                }
            }

            while (result.Length > 0 && result[result.Length - 1] == '_')
            {
                result.Length--;
            }

            return $"{result}_{Math.Max(0, order):00}";
        }
    }
}
