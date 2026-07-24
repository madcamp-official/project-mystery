using System.Collections.Generic;
using System.IO;
using System.Text;
using UnityEngine;

namespace Wake.Narrative
{
    /// CSV columns: line_id,scene_id,speaker_id,text,emotion,voice_required
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
                if (fields.Count < 4)
                {
                    continue;
                }

                string lineId = fields[0].Trim();
                string sceneId = fields[1].Trim();
                string speakerId = fields[2].Trim();
                string text = fields[3];
                string emotion = fields.Count > 4 ? fields[4].Trim() : string.Empty;
                bool voiceRequired = fields.Count > 5 && IsTruthy(fields[5]);

                lines[lineId] = new DialogueLine(sceneId, speakerId, text, emotion, voiceRequired);
            }
        }

        public bool TryGetLine(string lineId, out DialogueLine line)
        {
            return lines.TryGetValue(lineId, out line);
        }

        private static bool IsTruthy(string value)
        {
            string trimmed = value.Trim();
            return trimmed.Equals("Y", System.StringComparison.OrdinalIgnoreCase)
                || trimmed.Equals("TRUE", System.StringComparison.OrdinalIgnoreCase);
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
