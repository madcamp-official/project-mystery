using System;
using System.Collections.Generic;
using Wake.Core;
using Wake.Evidence;
using Wake.Exploration;
using Wake.UI;

namespace Wake.Narrative
{
    public sealed class ProductionEffectExecutionResult
    {
        internal readonly List<string> warnings = new();

        public int AppliedInstructionCount { get; internal set; }
        public IReadOnlyList<string> Warnings => warnings;
        public bool Success => warnings.Count == 0;
    }

    public sealed class ProductionEffectExecutor
    {
        private static readonly string[] AllTrustCharacters =
        {
            "CLAIRE",
            "DANIEL",
            "EVELYN",
            "HELENA",
            "MARCUS",
            "OWEN",
            "RICHARD",
            "THOMAS"
        };

        private readonly GameStateManager state;
        private readonly Func<string, bool> tryGrantEvidence;

        public ProductionEffectExecutor(
            GameStateManager state,
            Func<string, bool> tryGrantEvidence = null)
        {
            this.state = state;
            this.tryGrantEvidence = tryGrantEvidence;
        }

        public ProductionEffectExecutionResult Execute(string source)
        {
            ProductionEffectParseResult parsed = ProductionEffectParser.Parse(source);
            var result = new ProductionEffectExecutionResult();
            if (!parsed.Success)
            {
                result.warnings.AddRange(parsed.Errors);
                return result;
            }

            if (state == null)
            {
                result.warnings.Add("Game state is not available.");
                return result;
            }

            using (state.BeginStateBatch())
            {
                foreach (ProductionEffectInstruction instruction in parsed.Instructions)
                {
                    Apply(instruction, result);
                }
            }
            return result;
        }

        private void Apply(
            ProductionEffectInstruction instruction,
            ProductionEffectExecutionResult result)
        {
            switch (instruction.Kind)
            {
                case ProductionEffectKind.Trust:
                    ApplyTrust(instruction);
                    break;
                case ProductionEffectKind.Hostility:
                    state.ChangeRuntimeCounter(
                        instruction.Key,
                        instruction.NumericValue.GetValueOrDefault());
                    break;
                case ProductionEffectKind.PublicAnxiety:
                    state.ChangePublicAnxiety(instruction.NumericValue.GetValueOrDefault());
                    break;
                case ProductionEffectKind.EvidenceIntegrity:
                    state.ChangeEvidenceIntegrity(instruction.NumericValue.GetValueOrDefault());
                    break;
                case ProductionEffectKind.TimeBlock:
                    ApplyTime(instruction, result);
                    break;
                case ProductionEffectKind.WrongStrike:
                    state.ChangeRuntimeCounter(
                        "wrong_strike",
                        instruction.NumericValue.GetValueOrDefault());
                    break;
                case ProductionEffectKind.Flag:
                    state.AddFlag(instruction.Value);
                    break;
                case ProductionEffectKind.Evidence:
                    foreach (string evidenceId in instruction.Values)
                    {
                        if (tryGrantEvidence != null && tryGrantEvidence(evidenceId))
                        {
                            continue;
                        }
                        // tryGrantEvidence fails when the evidence still
                        // requires an inspection the player hasn't done -
                        // this dialogue-authored grant bypasses that, but
                        // it must still go through EvidenceInventory (not
                        // just GameStateManager) or the item is marked
                        // collected in save data while never appearing in
                        // the evidence notebook.
                        if (EvidenceInventory.Instance == null ||
                            !EvidenceInventory.Instance.TryAddById(evidenceId))
                        {
                            state.RecordEvidenceCollected(evidenceId);
                        }
                    }
                    break;
                case ProductionEffectKind.SceneUnlock:
                    foreach (string sceneId in
                             ProductionSceneReference.NormalizeDistinct(
                                 instruction.Values))
                    {
                        if (state.UnlockProductionScene(sceneId))
                        {
                            NotifyLocationUnlocked(sceneId);
                        }
                    }
                    break;
                case ProductionEffectKind.Theory:
                    foreach (string deductionId in instruction.Values)
                    {
                        state.UnlockDeduction(deductionId);
                    }
                    break;
                case ProductionEffectKind.Ending:
                    state.TryRecordFinalEnding(instruction.Value);
                    break;
                case ProductionEffectKind.Marker:
                    state.AddFlag(instruction.Key);
                    break;
                case ProductionEffectKind.Metadata:
                    state.AddFlag($"{instruction.Key}_{instruction.Value}");
                    break;
                default:
                    result.warnings.Add(
                        $"Unsupported effect kind '{instruction.Kind}' for '{instruction.Key}'.");
                    return;
            }

            result.AppliedInstructionCount++;
        }

        private static void NotifyLocationUnlocked(string sceneId)
        {
            if (!ProductionSceneCatalog.TryGet(sceneId, out ProductionSceneDefinition scene))
            {
                return;
            }

            CanonicalLocationSpec spec =
                CanonicalLocationCatalog.FindSpec(scene.NarrativeLocationCode);
            string displayName = spec != null
                ? spec.DisplayName
                : scene.NarrativeLocationCode;
            if (string.IsNullOrWhiteSpace(displayName))
            {
                return;
            }

            ToastController.Instance?.Show($"장소 해금: {displayName}");
        }

        private void ApplyTrust(ProductionEffectInstruction instruction)
        {
            int delta = instruction.NumericValue.GetValueOrDefault();
            string target = instruction.Key.Substring("trust_".Length);
            if (target.Equals("all", StringComparison.OrdinalIgnoreCase))
            {
                foreach (string characterId in AllTrustCharacters)
                {
                    state.ChangeTrust(characterId, delta);
                }
                return;
            }

            state.ChangeTrust(target, delta);
        }

        private void ApplyTime(
            ProductionEffectInstruction instruction,
            ProductionEffectExecutionResult result)
        {
            int delta = instruction.NumericValue.GetValueOrDefault();
            if (delta >= 0)
            {
                result.warnings.Add(
                    $"timeBlock expects a negative consumed-block value, found '{delta}'.");
                return;
            }

            state.ConsumeTimeBlocks(Math.Abs(delta));
        }
    }
}
