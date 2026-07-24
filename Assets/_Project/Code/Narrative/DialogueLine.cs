namespace Wake.Narrative
{
    public struct DialogueLine
    {
        public string SceneId;
        public string Speaker;
        public string Text;
        public string Emotion;
        public bool VoiceRequired;

        public DialogueLine(string sceneId, string speaker, string text, string emotion, bool voiceRequired)
        {
            SceneId = sceneId;
            Speaker = speaker;
            Text = text;
            Emotion = emotion;
            VoiceRequired = voiceRequired;
        }
    }
}
