using System.Collections.Generic;

namespace Wake.Narrative
{
    public static class DialogueTextPaginator
    {
        // Kept for source compatibility with callers and older content tools.
        // Runtime dialogue now relies on TMP wrapping and auto-sizing so a
        // single authored line is never split into artificial extra clicks.
        public const int DefaultCharactersPerPage = int.MaxValue;

        public static IReadOnlyList<string> Split(
            string text,
            int charactersPerPage = DefaultCharactersPerPage)
        {
            string content = text ?? string.Empty;
            return new[] { content };
        }
    }
}
