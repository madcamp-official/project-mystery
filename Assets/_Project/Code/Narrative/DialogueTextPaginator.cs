using System;
using System.Collections.Generic;

namespace Wake.Narrative
{
    public static class DialogueTextPaginator
    {
        public const int DefaultCharactersPerPage = 36;

        private static readonly char[] PreferredBreaks =
        {
            '\n', ' ', '.', ',', '!', '?', '。', '，', '！', '？',
            '…', '·', ':', ';'
        };

        public static IReadOnlyList<string> Split(
            string text,
            int charactersPerPage = DefaultCharactersPerPage)
        {
            string content = text ?? string.Empty;
            int pageSize = Math.Max(1, charactersPerPage);
            if (content.Length == 0)
                return new[] { string.Empty };

            var pages = new List<string>();
            int start = 0;
            while (content.Length - start > pageSize)
            {
                int tentativeEnd = start + pageSize;
                int minimumEnd =
                    start + Math.Max(1, (int)(pageSize * 0.55f));
                int end = FindBreak(
                    content,
                    tentativeEnd,
                    minimumEnd);
                pages.Add(content.Substring(start, end - start));
                start = end;
            }

            pages.Add(content.Substring(start));
            return pages;
        }

        private static int FindBreak(
            string content,
            int tentativeEnd,
            int minimumEnd)
        {
            for (int index = tentativeEnd - 1;
                 index >= minimumEnd;
                 index--)
            {
                if (Array.IndexOf(PreferredBreaks, content[index]) >= 0)
                    return index + 1;
            }

            return tentativeEnd;
        }
    }
}
