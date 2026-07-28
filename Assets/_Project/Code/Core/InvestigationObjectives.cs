using System;
using System.Collections.Generic;
using System.Linq;

namespace Wake.Core
{
    public enum InvestigationEventKind
    {
        SceneEntered,
        SceneCompleted,
        EvidenceCollected,
        ChoiceResolved,
        TheoryCompleted,
        StateThresholdReached
    }

    public readonly struct InvestigationEvent
    {
        public InvestigationEvent(
            InvestigationEventKind kind,
            string subjectId,
            string contextId = "")
        {
            Kind = kind;
            SubjectId = Normalize(subjectId);
            ContextId = Normalize(contextId);
        }

        public InvestigationEventKind Kind { get; }
        public string SubjectId { get; }
        public string ContextId { get; }

        private static string Normalize(string value) =>
            string.IsNullOrWhiteSpace(value)
                ? string.Empty
                : value.Trim().ToUpperInvariant();
    }

    public static class InvestigationEventHub
    {
        public static event Action<InvestigationEvent> Published;

        public static void Publish(
            InvestigationEventKind kind,
            string subjectId,
            string contextId = "")
        {
            Published?.Invoke(new InvestigationEvent(kind, subjectId, contextId));
        }
    }

    public readonly struct ObjectiveRequirement
    {
        public ObjectiveRequirement(InvestigationEventKind eventKind, string subjectId)
        {
            EventKind = eventKind;
            SubjectId = subjectId?.Trim().ToUpperInvariant() ?? string.Empty;
        }

        public InvestigationEventKind EventKind { get; }
        public string SubjectId { get; }
        public string Key => $"{EventKind}:{SubjectId}";

        public bool Matches(InvestigationEvent investigationEvent) =>
            investigationEvent.Kind == EventKind &&
            string.Equals(
                investigationEvent.SubjectId,
                SubjectId,
                StringComparison.Ordinal);
    }

    public sealed class InvestigationObjectiveDefinition
    {
        public InvestigationObjectiveDefinition(
            string id,
            string sceneId,
            string title,
            params ObjectiveRequirement[] requirements)
        {
            Id = id;
            SceneId = sceneId;
            Title = title;
            Requirements = requirements ?? Array.Empty<ObjectiveRequirement>();
        }

        public string Id { get; }
        public string SceneId { get; }
        public string Title { get; }
        public IReadOnlyList<ObjectiveRequirement> Requirements { get; }
    }

    public static class InvestigationObjectiveCatalog
    {
        private static readonly InvestigationObjectiveDefinition[] Definitions =
        {
            O(
                "objective_p_01_arrival",
                "P-01",
                "항구에서 다니엘을 만나기",
                R(InvestigationEventKind.SceneEntered, "P-01")),
            O(
                "objective_d2_01_exit_inspection",
                "D2-01",
                "호라이즌 룸의 출구 흔적 확인하기",
                R(InvestigationEventKind.EvidenceCollected, "C-03"),
                R(InvestigationEventKind.EvidenceCollected, "C-04"),
                R(InvestigationEventKind.EvidenceCollected, "C-05")),
            O(
                "objective_d6_03_ballast_trace",
                "D6-03",
                "Ballast Annex의 실제 살해 흔적 확인하기",
                R(InvestigationEventKind.EvidenceCollected, "C-06"),
                R(InvestigationEventKind.EvidenceCollected, "C-12"))
        };

        public static IReadOnlyList<InvestigationObjectiveDefinition> All => Definitions;

        public static bool TryGet(
            string objectiveId,
            out InvestigationObjectiveDefinition definition)
        {
            string normalized = objectiveId?.Trim().ToLowerInvariant();
            definition = Definitions.FirstOrDefault(item =>
                string.Equals(item.Id, normalized, StringComparison.Ordinal));
            return definition != null;
        }

        private static InvestigationObjectiveDefinition O(
            string id,
            string sceneId,
            string title,
            params ObjectiveRequirement[] requirements) =>
            new(id, sceneId, title, requirements);

        private static ObjectiveRequirement R(
            InvestigationEventKind kind,
            string subjectId) =>
            new(kind, subjectId);
    }

    public sealed class ObjectiveProgress
    {
        private readonly HashSet<string> matchedRequirementKeys =
            new(StringComparer.Ordinal);

        internal ObjectiveProgress(
            InvestigationObjectiveDefinition definition,
            bool isCompleted)
        {
            Definition = definition;
            IsCompleted = isCompleted;
            if (isCompleted)
            {
                matchedRequirementKeys.UnionWith(
                    definition.Requirements.Select(requirement => requirement.Key));
            }
        }

        public InvestigationObjectiveDefinition Definition { get; }
        public int CompletedRequirementCount => matchedRequirementKeys.Count;
        public int RequirementCount => Definition.Requirements.Count;
        public bool IsCompleted { get; internal set; }

        internal bool Apply(InvestigationEvent investigationEvent)
        {
            bool changed = false;
            foreach (ObjectiveRequirement requirement in Definition.Requirements)
            {
                if (requirement.Matches(investigationEvent))
                {
                    changed |= matchedRequirementKeys.Add(requirement.Key);
                }
            }

            return changed;
        }
    }

    public sealed class InvestigationObjectiveTracker : IDisposable
    {
        private readonly GameStateManager state;
        private readonly Dictionary<string, ObjectiveProgress> progressById;

        public InvestigationObjectiveTracker(
            GameStateManager state,
            IEnumerable<InvestigationObjectiveDefinition> definitions = null)
        {
            this.state = state ?? throw new ArgumentNullException(nameof(state));
            progressById = (definitions ?? InvestigationObjectiveCatalog.All)
                .ToDictionary(
                    definition => definition.Id,
                    definition => new ObjectiveProgress(
                        definition,
                        state.HasCompletedObjective(definition.Id)),
                    StringComparer.Ordinal);
            InvestigationEventHub.Published += Handle;
        }

        public event Action<ObjectiveProgress> ProgressChanged;
        public event Action<InvestigationObjectiveDefinition> ObjectiveCompleted;

        public IReadOnlyCollection<ObjectiveProgress> Progress => progressById.Values;

        public ObjectiveProgress GetProgress(string objectiveId)
        {
            progressById.TryGetValue(
                objectiveId?.Trim().ToLowerInvariant() ?? string.Empty,
                out ObjectiveProgress progress);
            return progress;
        }

        public void Dispose()
        {
            InvestigationEventHub.Published -= Handle;
        }

        private void Handle(InvestigationEvent investigationEvent)
        {
            foreach (ObjectiveProgress progress in progressById.Values)
            {
                if (progress.IsCompleted || !progress.Apply(investigationEvent))
                {
                    continue;
                }

                ProgressChanged?.Invoke(progress);
                if (progress.CompletedRequirementCount < progress.RequirementCount)
                {
                    continue;
                }

                progress.IsCompleted = true;
                state.RecordCompletedObjective(progress.Definition.Id);
                ObjectiveCompleted?.Invoke(progress.Definition);
            }
        }
    }
}
