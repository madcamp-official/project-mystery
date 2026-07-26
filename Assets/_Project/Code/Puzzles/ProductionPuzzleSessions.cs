using System;
using System.Collections.Generic;
using System.Linq;
using Wake.Core;
using Wake.Evidence;

namespace Wake.Puzzles
{
    public sealed class ProductionPuzzleDefinition
    {
        public ProductionPuzzleDefinition(
            string id,
            string sceneId,
            string resultTheoryId,
            IEnumerable<string> allowedSelectionIds,
            IEnumerable<string> requiredSelectionIds,
            IEnumerable<string> requiredEvidenceIds)
        {
            Id = Normalize(id);
            SceneId = sceneId?.Trim().ToUpperInvariant() ?? string.Empty;
            ResultTheoryId = Normalize(resultTheoryId);
            AllowedSelectionIds = NormalizeMany(allowedSelectionIds);
            RequiredSelectionIds = NormalizeMany(requiredSelectionIds);
            RequiredEvidenceIds = (requiredEvidenceIds ?? Array.Empty<string>())
                .Select(CanonicalEvidenceCatalog.NormalizeId)
                .ToArray();
        }

        public string Id { get; }
        public string SceneId { get; }
        public string ResultTheoryId { get; }
        public IReadOnlyList<string> AllowedSelectionIds { get; }
        public IReadOnlyList<string> RequiredSelectionIds { get; }
        public IReadOnlyList<string> RequiredEvidenceIds { get; }

        public static string Normalize(string value) =>
            string.IsNullOrWhiteSpace(value)
                ? string.Empty
                : value.Trim().ToLowerInvariant().Replace(' ', '_');

        private static string[] NormalizeMany(IEnumerable<string> values) =>
            (values ?? Array.Empty<string>())
            .Select(Normalize)
            .Where(value => !string.IsNullOrEmpty(value))
            .Distinct()
            .ToArray();
    }

    public static class ProductionPuzzleCatalog
    {
        public const string BloodPattern = "blood_pattern";
        public const string CargoRailBranch = "cargo_rail_branch";

        private static readonly ProductionPuzzleDefinition[] Definitions =
        {
            new(
                BloodPattern,
                "D2-02",
                "body_insertion_hypothesis",
                new[]
                {
                    "no_spatter",
                    "center_mismatch",
                    "vertical_drop"
                },
                new[]
                {
                    "no_spatter",
                    "center_mismatch",
                    "vertical_drop"
                },
                new[] { "C-07" }),
            new(
                CargoRailBranch,
                "D6-02",
                "body_transport_route",
                new[]
                {
                    "horizon_branch_22_18",
                    "weight_86kg",
                    "ballast_horizon_route"
                },
                new[]
                {
                    "horizon_branch_22_18",
                    "weight_86kg",
                    "ballast_horizon_route"
                },
                new[] { "C-08", "C-09", "C-10" })
        };

        private static readonly IReadOnlyDictionary<string, ProductionPuzzleDefinition> ById =
            Definitions.ToDictionary(item => item.Id, StringComparer.Ordinal);

        public static IReadOnlyList<ProductionPuzzleDefinition> All => Definitions;

        public static bool TryGet(
            string puzzleId,
            out ProductionPuzzleDefinition definition) =>
            ById.TryGetValue(
                ProductionPuzzleDefinition.Normalize(puzzleId),
                out definition);

        public static bool TryGetByScene(
            string sceneId,
            out ProductionPuzzleDefinition definition)
        {
            string normalized = sceneId?.Trim().ToUpperInvariant();
            definition = Definitions.FirstOrDefault(item =>
                string.Equals(item.SceneId, normalized, StringComparison.Ordinal));
            return definition != null;
        }
    }

    public readonly struct PuzzleCompletionResult
    {
        public PuzzleCompletionResult(
            bool completed,
            IReadOnlyList<string> missingSelectionIds,
            IReadOnlyList<string> missingEvidenceIds)
        {
            Completed = completed;
            MissingSelectionIds = missingSelectionIds;
            MissingEvidenceIds = missingEvidenceIds;
        }

        public bool Completed { get; }
        public IReadOnlyList<string> MissingSelectionIds { get; }
        public IReadOnlyList<string> MissingEvidenceIds { get; }
    }

    public sealed class ProductionPuzzleSession
    {
        private readonly GameStateManager state;
        private readonly Func<string, bool> hasEvidence;
        private readonly HashSet<string> selections;

        public ProductionPuzzleSession(
            ProductionPuzzleDefinition definition,
            GameStateManager state,
            Func<string, bool> hasEvidence)
        {
            Definition = definition ??
                         throw new ArgumentNullException(nameof(definition));
            this.state = state ?? throw new ArgumentNullException(nameof(state));
            this.hasEvidence = hasEvidence ??
                               throw new ArgumentNullException(nameof(hasEvidence));

            if (!state.TryGetPuzzleSession(definition.Id, out PuzzleSessionState saved))
            {
                saved = new PuzzleSessionState { puzzleId = definition.Id };
            }

            selections = new HashSet<string>(
                saved.selectedIds ?? new List<string>(),
                StringComparer.Ordinal);
            Step = saved.step;
            HintLevel = saved.hintLevel;
            IsCompleted = saved.completed;
        }

        public ProductionPuzzleDefinition Definition { get; }
        public IReadOnlyCollection<string> SelectedIds => selections;
        public int Step { get; private set; }
        public int HintLevel { get; private set; }
        public bool IsCompleted { get; private set; }

        public bool Select(string selectionId)
        {
            string normalized = ProductionPuzzleDefinition.Normalize(selectionId);
            if (IsCompleted ||
                !Definition.AllowedSelectionIds.Contains(normalized) ||
                !selections.Add(normalized))
            {
                return false;
            }

            Save();
            return true;
        }

        public bool Deselect(string selectionId)
        {
            if (IsCompleted ||
                !selections.Remove(ProductionPuzzleDefinition.Normalize(selectionId)))
            {
                return false;
            }

            Save();
            return true;
        }

        public void SetStep(int step)
        {
            if (IsCompleted)
            {
                return;
            }

            Step = Math.Max(0, step);
            Save();
        }

        public bool UseHint()
        {
            if (IsCompleted || HintLevel >= 3)
            {
                return false;
            }

            HintLevel++;
            Save();
            return true;
        }

        public PuzzleCompletionResult TryComplete()
        {
            string[] missingSelections = Definition.RequiredSelectionIds
                .Where(value => !selections.Contains(value))
                .ToArray();
            string[] missingEvidence = Definition.RequiredEvidenceIds
                .Where(value => !hasEvidence(value))
                .ToArray();
            if (IsCompleted || missingSelections.Length > 0 || missingEvidence.Length > 0)
            {
                return new PuzzleCompletionResult(
                    false,
                    missingSelections,
                    missingEvidence);
            }

            IsCompleted = true;
            Save();
            state.AddFlag($"puzzle_{Definition.Id}_completed");
            state.RecordCompletedScene(Definition.SceneId);
            InvestigationEventHub.Publish(
                InvestigationEventKind.TheoryCompleted,
                Definition.ResultTheoryId,
                Definition.SceneId);
            return new PuzzleCompletionResult(
                true,
                Array.Empty<string>(),
                Array.Empty<string>());
        }

        private void Save()
        {
            state.SavePuzzleSession(new PuzzleSessionState
            {
                puzzleId = Definition.Id,
                selectedIds = selections.OrderBy(value => value).ToList(),
                step = Step,
                hintLevel = HintLevel,
                completed = IsCompleted
            });
        }
    }
}
