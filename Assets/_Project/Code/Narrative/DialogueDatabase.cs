using System.Collections.Generic;
using UnityEngine;

namespace Wake.Narrative
{
    public class DialogueDatabase : MonoBehaviour
    {
        public static DialogueDatabase Instance { get; private set; }

        [SerializeField] private TextAsset csvFile;

        private readonly Dictionary<string, DialogueLine> lines = new();
        private readonly Dictionary<string, DialogueRecord> records = new();

        public IReadOnlyDictionary<string, DialogueRecord> Records => records;
        public IReadOnlyList<string> LoadErrors { get; private set; } = new List<string>();

        private void Awake()
        {
            Instance = this;
            if (csvFile != null)
            {
                LoadFromText(csvFile.text);
            }
            else
            {
                Debug.LogWarning("DialogueDatabase has no CSV assigned.");
            }
        }

        public bool LoadFromText(string csv)
        {
            lines.Clear();
            records.Clear();

            DialogueCsvParseResult result = DialogueCsvParser.Parse(csv);
            LoadErrors = result.Errors;
            foreach (DialogueRecord record in result.Records)
            {
                records[record.StableLineId] = record;
                lines[record.StableLineId] = record.ToLegacyLine();
                if (!string.IsNullOrWhiteSpace(record.ChoiceId) &&
                    !record.ChoiceId.Contains(" / "))
                {
                    lines.TryAdd(record.ChoiceId, record.ToLegacyLine());
                }
            }
            return result.Success;
        }

        public bool TryGetLine(string lineId, out DialogueLine line)
        {
            return lines.TryGetValue(lineId, out line);
        }

        public bool TryGetRecord(string stableLineId, out DialogueRecord record)
        {
            return records.TryGetValue(stableLineId, out record);
        }
    }
}
