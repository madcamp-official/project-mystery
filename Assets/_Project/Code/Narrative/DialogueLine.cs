namespace Seat0A.Narrative
{
    public struct DialogueLine
    {
        public string Speaker;
        public string Text;

        public DialogueLine(string speaker, string text)
        {
            Speaker = speaker;
            Text = text;
        }
    }
}
