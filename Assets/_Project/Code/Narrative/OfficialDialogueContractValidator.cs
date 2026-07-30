using System;
using System.Collections.Generic;
using System.Linq;

namespace Wake.Narrative
{
    public sealed class OfficialDialogueContractReport
    {
        public OfficialDialogueContractReport(IEnumerable<string> errors)
        {
            Errors = (errors ?? Array.Empty<string>())
                .Distinct(StringComparer.Ordinal)
                .ToArray();
        }

        public IReadOnlyList<string> Errors { get; }
        public bool IsValid => Errors.Count == 0;
    }

    /// <summary>
    /// Validates the three deterministic CSV exports as one official content contract.
    /// This catches drift that cannot be detected by validating each sheet in isolation.
    /// </summary>
    public static class OfficialDialogueContractValidator
    {
        public const int ExpectedDialogueCount = 1083;
        public const int ExpectedChoiceCount = 100;
        public const int ExpectedSceneCount = 41;
        public const int ExpectedEndingCount = 4;

        public static OfficialDialogueContractReport Validate(
            string dialogueCsv,
            string choicesCsv,
            string sceneIndexCsv)
        {
            DialogueCsvParseResult dialogue = DialogueCsvParser.Parse(dialogueCsv);
            SupplementalCsvParseResult<ChoiceFlowRecord> choices =
                DialogueSupplementalCsvParser.ParseChoices(choicesCsv);
            SupplementalCsvParseResult<SceneIndexRecord> scenes =
                DialogueSupplementalCsvParser.ParseScenes(sceneIndexCsv);
            var errors = new List<string>();

            AddParseErrors("Dialogue_Master", dialogue.Errors, errors);
            AddParseErrors("Choice_Flow", choices.Errors, errors);
            AddParseErrors("Scene_Index", scenes.Errors, errors);
            if (errors.Count > 0)
                return new OfficialDialogueContractReport(errors);

            ValidateTotals(dialogue.Records, choices.Records, scenes.Records, errors);
            ValidateSceneMembership(dialogue.Records, choices.Records, scenes.Records, errors);
            ValidatePerSceneCounts(dialogue.Records, choices.Records, scenes.Records, errors);
            ValidateChoiceMirrors(dialogue.Records, choices.Records, errors);
            ValidateSceneGraph(scenes.Records, errors);
            ValidateSceneUnlockEffects(dialogue.Records, scenes.Records, errors);
            ValidateDeclaredTransitions(dialogue.Records, scenes.Records, errors);
            ValidateEndingContract(dialogue.Records, errors);
            return new OfficialDialogueContractReport(errors);
        }

        private static void ValidateTotals(
            IReadOnlyCollection<DialogueRecord> dialogue,
            IReadOnlyCollection<ChoiceFlowRecord> choices,
            IReadOnlyCollection<SceneIndexRecord> scenes,
            ICollection<string> errors)
        {
            RequireCount("dialogue rows", ExpectedDialogueCount, dialogue.Count, errors);
            RequireCount("choice rows", ExpectedChoiceCount, choices.Count, errors);
            RequireCount("scenes", ExpectedSceneCount, scenes.Count, errors);

            int indexedDialogue = scenes.Sum(scene => scene.DialogueLineCount);
            int indexedChoices = scenes.Sum(scene => scene.ChoiceCount);
            RequireCount(
                "Scene_Index dialogue_line_count total",
                ExpectedDialogueCount,
                indexedDialogue,
                errors);
            RequireCount(
                "Scene_Index choice_count total",
                ExpectedChoiceCount,
                indexedChoices,
                errors);
        }

        private static void ValidateSceneMembership(
            IEnumerable<DialogueRecord> dialogue,
            IEnumerable<ChoiceFlowRecord> choices,
            IEnumerable<SceneIndexRecord> scenes,
            ICollection<string> errors)
        {
            HashSet<string> sceneIds = scenes
                .Select(scene => scene.SceneId)
                .ToHashSet(StringComparer.Ordinal);
            foreach (string sceneId in dialogue
                         .Select(record => record.SceneId)
                         .Concat(choices.Select(choice => choice.SceneId))
                         .Distinct(StringComparer.Ordinal)
                         .Where(sceneId => !sceneIds.Contains(sceneId)))
                errors.Add($"Content references scene '{sceneId}' missing from Scene_Index.");
            foreach (string sceneId in sceneIds.Where(sceneId =>
                         !dialogue.Any(record => record.SceneId == sceneId)))
                errors.Add($"Scene_Index scene '{sceneId}' has no dialogue rows.");
        }

        private static void ValidatePerSceneCounts(
            IEnumerable<DialogueRecord> dialogue,
            IEnumerable<ChoiceFlowRecord> choices,
            IEnumerable<SceneIndexRecord> scenes,
            ICollection<string> errors)
        {
            var dialogueByScene = dialogue
                .GroupBy(record => record.SceneId, StringComparer.Ordinal)
                .ToDictionary(group => group.Key, group => group.ToArray(),
                    StringComparer.Ordinal);
            var choicesByScene = choices
                .GroupBy(record => record.SceneId, StringComparer.Ordinal)
                .ToDictionary(group => group.Key, group => group.Count(),
                    StringComparer.Ordinal);

            foreach (SceneIndexRecord scene in scenes)
            {
                DialogueRecord[] rows = dialogueByScene.TryGetValue(
                    scene.SceneId, out DialogueRecord[] found)
                    ? found
                    : Array.Empty<DialogueRecord>();
                int voiced = rows.Count(record => record.VoiceRequired);
                int choiceCount = choicesByScene.TryGetValue(
                    scene.SceneId, out int count) ? count : 0;
                CompareSceneCount(
                    scene, "dialogue_line_count", scene.DialogueLineCount,
                    rows.Length, errors);
                CompareSceneCount(
                    scene, "voiced_line_count", scene.VoicedLineCount,
                    voiced, errors);
                CompareSceneCount(
                    scene, "choice_count", scene.ChoiceCount,
                    choiceCount, errors);
            }
        }

        private static void ValidateChoiceMirrors(
            IEnumerable<DialogueRecord> dialogue,
            IEnumerable<ChoiceFlowRecord> choices,
            ICollection<string> errors)
        {
            Dictionary<string, DialogueRecord> dialogueChoices = dialogue
                .Where(record => !string.IsNullOrWhiteSpace(record.ChoiceId))
                .GroupBy(record => record.ChoiceId, StringComparer.Ordinal)
                .ToDictionary(group => group.Key, group => group.First(),
                    StringComparer.Ordinal);
            Dictionary<string, ChoiceFlowRecord> choiceRows = choices
                .ToDictionary(record => record.ChoiceId, StringComparer.Ordinal);

            foreach (string id in dialogueChoices.Keys
                         .Except(choiceRows.Keys, StringComparer.Ordinal))
                errors.Add($"Dialogue choice '{id}' is missing from Choice_Flow.");
            foreach (string id in choiceRows.Keys
                         .Except(dialogueChoices.Keys, StringComparer.Ordinal))
                errors.Add($"Choice_Flow choice '{id}' is missing from Dialogue_Master.");

            foreach (string id in dialogueChoices.Keys
                         .Intersect(choiceRows.Keys, StringComparer.Ordinal))
            {
                DialogueRecord line = dialogueChoices[id];
                ChoiceFlowRecord choice = choiceRows[id];
                CompareChoice(id, "scene_id", line.SceneId, choice.SceneId, errors);
                CompareChoice(id, "text_ko", line.TextKo, choice.TextKo, errors);
                CompareChoice(id, "condition", line.Condition, choice.Condition, errors);
                CompareChoice(
                    id, "effect", line.NextOrEffect, choice.Effect, errors);
                CompareChoice(
                    id, "branch_group", line.BranchGroup, choice.BranchGroup, errors);
                if (!string.Equals(
                        choice.ImplementationStatus, "READY",
                        StringComparison.Ordinal))
                    errors.Add(
                        $"Choice '{id}' implementation_status is " +
                        $"'{choice.ImplementationStatus}', expected 'READY'.");
            }
        }

        private static void ValidateSceneGraph(
            IReadOnlyCollection<SceneIndexRecord> scenes,
            ICollection<string> errors)
        {
            HashSet<string> sceneIds = scenes
                .Select(scene => scene.SceneId)
                .ToHashSet(StringComparer.Ordinal);
            foreach (SceneIndexRecord scene in scenes)
            {
                foreach (string reference in ExtractSceneReferences(scene.NextScene))
                {
                    if (!sceneIds.Contains(reference))
                        errors.Add(
                            $"Scene '{scene.SceneId}' next_scene references " +
                            $"unknown scene '{reference}'.");
                }
            }

            string[] requiredFinalScenes = { "D8-01", "D8-02", "D8-03" };
            foreach (string id in requiredFinalScenes.Where(id => !sceneIds.Contains(id)))
                errors.Add($"Required final sequence scene '{id}' is missing.");
        }

        private static IEnumerable<string> ExtractSceneReferences(string value)
        {
            if (string.IsNullOrWhiteSpace(value))
                yield break;
            string[] tokens = value.Split(
                new[] { ' ', '\t', '\r', '\n', ',', '/', ';' },
                StringSplitOptions.RemoveEmptyEntries);
            foreach (string token in tokens)
            {
                if (token.Length >= 4 &&
                    (token[0] == 'P' || token[0] == 'D') &&
                    token.Contains('-'))
                    yield return token;
            }
        }

        private static void ValidateEndingContract(
            IEnumerable<DialogueRecord> dialogue,
            ICollection<string> errors)
        {
            string[] endings = dialogue
                .Select(record => record.Emotion)
                .Where(emotion => emotion.StartsWith(
                    "ending:", StringComparison.Ordinal))
                .Select(emotion => emotion.Substring("ending:".Length))
                .Distinct(StringComparer.Ordinal)
                .OrderBy(value => value, StringComparer.Ordinal)
                .ToArray();
            RequireCount("ending markers", ExpectedEndingCount, endings.Length, errors);
            string[] expected = { "A_complete", "B_complete", "C_complete", "bad_complete" };
            if (!endings.SequenceEqual(expected, StringComparer.Ordinal))
                errors.Add(
                    "Ending markers must be A_complete, B_complete, C_complete, " +
                    "and bad_complete.");
        }

        private static void ValidateSceneUnlockEffects(
            IEnumerable<DialogueRecord> dialogue,
            IEnumerable<SceneIndexRecord> scenes,
            ICollection<string> errors)
        {
            HashSet<string> sceneIds = scenes
                .Select(scene => scene.SceneId)
                .ToHashSet(StringComparer.Ordinal);
            foreach (DialogueRecord record in dialogue.Where(record =>
                         !string.IsNullOrWhiteSpace(record.NextOrEffect)))
            {
                ProductionEffectParseResult parsed =
                    ProductionEffectParser.Parse(record.NextOrEffect);
                if (!parsed.Success)
                {
                    continue;
                }

                foreach (string rawSceneId in parsed.Instructions
                             .Where(instruction =>
                                 instruction.Kind ==
                                 ProductionEffectKind.SceneUnlock)
                             .SelectMany(instruction => instruction.Values))
                {
                    string sceneId =
                        ProductionSceneReference.Normalize(rawSceneId);
                    if (!sceneIds.Contains(sceneId))
                    {
                        errors.Add(
                            $"Dialogue line '{record.StableLineId}' unlocks " +
                            $"unknown scene '{rawSceneId}'.");
                    }
                }
            }
        }

        private static void ValidateDeclaredTransitions(
            IEnumerable<DialogueRecord> dialogue,
            IEnumerable<SceneIndexRecord> scenes,
            ICollection<string> errors)
        {
            var unlockedByScene = new Dictionary<string, HashSet<string>>(
                StringComparer.Ordinal);
            foreach (DialogueRecord record in dialogue.Where(record =>
                         !string.IsNullOrWhiteSpace(record.NextOrEffect)))
            {
                ProductionEffectParseResult parsed =
                    ProductionEffectParser.Parse(record.NextOrEffect);
                if (!parsed.Success)
                {
                    continue;
                }

                foreach (string target in parsed.Instructions
                             .Where(instruction =>
                                 instruction.Kind ==
                                 ProductionEffectKind.SceneUnlock)
                             .SelectMany(instruction => instruction.Values)
                             .Select(ProductionSceneReference.Normalize))
                {
                    if (!unlockedByScene.TryGetValue(
                            record.SceneId,
                            out HashSet<string> targets))
                    {
                        targets = new HashSet<string>(StringComparer.Ordinal);
                        unlockedByScene.Add(record.SceneId, targets);
                    }
                    targets.Add(target);
                }
            }

            foreach (ProductionSceneCompletionRequirement requirement in
                     ProductionSceneCompletionCatalog.All)
            {
                if (!unlockedByScene.TryGetValue(
                        requirement.SceneId,
                        out HashSet<string> targets))
                {
                    targets = new HashSet<string>(StringComparer.Ordinal);
                    unlockedByScene.Add(requirement.SceneId, targets);
                }
                targets.Add(ProductionSceneReference.Normalize(
                    requirement.NextSceneId));
            }

            foreach (SceneIndexRecord scene in scenes)
            {
                string[] declared = ExtractSceneReferences(scene.NextScene)
                    .Select(ProductionSceneReference.Normalize)
                    .Distinct(StringComparer.Ordinal)
                    .ToArray();
                HashSet<string> actual = unlockedByScene.TryGetValue(
                    scene.SceneId,
                    out HashSet<string> found)
                    ? found
                    : new HashSet<string>(StringComparer.Ordinal);
                foreach (string target in declared.Where(target =>
                             !actual.Contains(target)))
                {
                    errors.Add(
                        $"Scene '{scene.SceneId}' declares transition to " +
                        $"'{target}' without a matching scene_unlock effect " +
                        "or interaction completion route.");
                }
            }
        }

        private static void AddParseErrors(
            string sheet,
            IEnumerable<string> parseErrors,
            ICollection<string> errors)
        {
            foreach (string error in parseErrors)
                errors.Add($"{sheet}: {error}");
        }

        private static void RequireCount(
            string label,
            int expected,
            int actual,
            ICollection<string> errors)
        {
            if (actual != expected)
                errors.Add($"{label}: expected {expected}, found {actual}.");
        }

        private static void CompareSceneCount(
            SceneIndexRecord scene,
            string field,
            int indexed,
            int actual,
            ICollection<string> errors)
        {
            if (indexed != actual)
                errors.Add(
                    $"Scene '{scene.SceneId}' {field}: index says {indexed}, " +
                    $"content has {actual}.");
        }

        private static void CompareChoice(
            string id,
            string field,
            string dialogueValue,
            string choiceValue,
            ICollection<string> errors)
        {
            if (!string.Equals(
                    dialogueValue, choiceValue, StringComparison.Ordinal))
                errors.Add(
                    $"Choice '{id}' {field} differs between Dialogue_Master " +
                    "and Choice_Flow.");
        }
    }
}
