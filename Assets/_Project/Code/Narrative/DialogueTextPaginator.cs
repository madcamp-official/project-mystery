using System.Collections.Generic;
using System.Text;
using TMPro;
using UnityEngine;

namespace Wake.Narrative
{
    public static class DialogueTextPaginator
    {
        public const int DefaultCharactersPerPage = 170;

        public static IReadOnlyList<string> Split(
            string text,
            int charactersPerPage = DefaultCharactersPerPage)
        {
            string content = text ?? string.Empty;
            if (content.Length == 0 || charactersPerPage <= 0)
                return new[] { content };

            var pages = new List<string>();
            var page = new StringBuilder();
            foreach (string token in Tokenize(content))
            {
                if (page.Length > 0 &&
                    page.Length + token.Length > charactersPerPage)
                {
                    pages.Add(page.ToString());
                    page.Clear();
                }

                if (token.Length <= charactersPerPage)
                {
                    page.Append(token);
                    continue;
                }

                int offset = 0;
                while (offset < token.Length)
                {
                    int remaining = charactersPerPage - page.Length;
                    if (remaining == 0)
                    {
                        pages.Add(page.ToString());
                        page.Clear();
                        remaining = charactersPerPage;
                    }

                    int length = System.Math.Min(
                        remaining,
                        token.Length - offset);
                    page.Append(token, offset, length);
                    offset += length;
                }
            }

            if (page.Length > 0 || pages.Count == 0)
                pages.Add(page.ToString());
            return pages;
        }

        public static IReadOnlyList<string> SplitToFit(
            string text,
            TMP_Text layoutText,
            float minimumFontSize,
            int fallbackCharactersPerPage = DefaultCharactersPerPage)
        {
            string content = text ?? string.Empty;
            if (content.Length == 0 ||
                layoutText == null ||
                layoutText.rectTransform.rect.width <= 1f ||
                layoutText.rectTransform.rect.height <= 1f)
            {
                return Split(content, fallbackCharactersPerPage);
            }

            float availableWidth = Mathf.Max(
                1f,
                layoutText.rectTransform.rect.width -
                layoutText.margin.x -
                layoutText.margin.z);
            float availableHeight = Mathf.Max(
                1f,
                layoutText.rectTransform.rect.height -
                layoutText.margin.y -
                layoutText.margin.w);
            bool originalAutoSizing = layoutText.enableAutoSizing;
            float originalFontSize = layoutText.fontSize;
            float measurementSize = Mathf.Max(
                minimumFontSize,
                originalAutoSizing
                    ? layoutText.fontSizeMin
                    : originalFontSize);

            layoutText.enableAutoSizing = false;
            layoutText.fontSize = measurementSize;
            try
            {
                var pages = new List<string>();
                var page = new StringBuilder();
                foreach (string token in Tokenize(content))
                {
                    string candidate = page.ToString() + token;
                    if (page.Length > 0 &&
                        !Fits(
                            layoutText,
                            candidate,
                            availableWidth,
                            availableHeight))
                    {
                        pages.Add(page.ToString());
                        page.Clear();
                    }

                    if (Fits(
                            layoutText,
                            token,
                            availableWidth,
                            availableHeight))
                    {
                        page.Append(token);
                        continue;
                    }

                    AppendOversizedToken(
                        token,
                        page,
                        pages,
                        layoutText,
                        availableWidth,
                        availableHeight);
                }

                if (page.Length > 0 || pages.Count == 0)
                    pages.Add(page.ToString());
                return pages;
            }
            finally
            {
                layoutText.fontSize = originalFontSize;
                layoutText.enableAutoSizing = originalAutoSizing;
            }
        }

        private static void AppendOversizedToken(
            string token,
            StringBuilder page,
            List<string> pages,
            TMP_Text layoutText,
            float width,
            float height)
        {
            foreach (char character in token)
            {
                string candidate = page.ToString() + character;
                if (page.Length > 0 &&
                    !Fits(layoutText, candidate, width, height))
                {
                    pages.Add(page.ToString());
                    page.Clear();
                }
                page.Append(character);
            }
        }

        private static bool Fits(
            TMP_Text layoutText,
            string candidate,
            float width,
            float height)
        {
            Vector2 preferred =
                layoutText.GetPreferredValues(candidate, width, 0f);
            return preferred.y <= height + 0.5f;
        }

        private static IEnumerable<string> Tokenize(string content)
        {
            var token = new StringBuilder();
            foreach (char character in content)
            {
                token.Append(character);
                if (char.IsWhiteSpace(character) ||
                    character is '.' or '!' or '?' or '。' or '！' or
                        '？' or '…' or '\n')
                {
                    yield return token.ToString();
                    token.Clear();
                }
            }

            if (token.Length > 0)
                yield return token.ToString();
        }
    }
}
