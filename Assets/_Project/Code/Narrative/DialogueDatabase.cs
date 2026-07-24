using System.Collections.Generic;
using System.IO;
using System.Text;
using UnityEngine;

namespace Seat0A.Narrative
{
    /// CSV columns: line_id,speaker,text (text field may be double-quoted).
    public class DialogueDatabase : MonoBehaviour
    {
        public static DialogueDatabase Instance { get; private set; }

        [SerializeField] private TextAsset csvFile;

        private readonly Dictionary<string, DialogueLine> lines = new();

        private void Awake()
        {
            Instance = this;
            Load();
        }

        private void Load()
        {
            lines.Clear();
            if (csvFile == null)
            {
                Debug.LogWarning("DialogueDatabase has no CSV assigned.");
                return;
            }

            using var reader = new StringReader(csvFile.text);
            reader.ReadLine(); // header row

            string line;
            while ((line = reader.ReadLine()) != null)
            {
                if (string.IsNullOrWhiteSpace(line))
                {
                    continue;
                }

                List<string> fields = ParseCsvLine(line);
                if (fields.Count < 3)
                {
                    continue;
                }

                string lineId = fields[0].Trim();
                string speaker = fields[1].Trim();
                string text = fields[2];
                lines[lineId] = new DialogueLine(speaker, text);
            }
        }

        public bool TryGetLine(string lineId, out DialogueLine line)
        {
            return lines.TryGetValue(lineId, out line);
        }

        private static List<string> ParseCsvLine(string line)
        {
            var fields = new List<string>();
            var current = new StringBuilder();
            bool inQuotes = false;

            for (int i = 0; i < line.Length; i++)
            {
                char c = line[i];
                if (inQuotes)
                {
                    if (c == '"')
                    {
                        if (i + 1 < line.Length && line[i + 1] == '"')
                        {
                            current.Append('"');
                            i++;
                        }
                        else
                        {
                            inQuotes = false;
                        }
                    }
                    else
                    {
                        current.Append(c);
                    }
                }
                else if (c == '"')
                {
                    inQuotes = true;
                }
                else if (c == ',')
                {
                    fields.Add(current.ToString());
                    current.Clear();
                }
                else
                {
                    current.Append(c);
                }
            }

            fields.Add(current.ToString());
            return fields;
        }
    }
}
