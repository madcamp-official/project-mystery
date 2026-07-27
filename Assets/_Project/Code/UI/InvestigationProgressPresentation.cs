using System;
using System.Collections.Generic;
using System.Linq;

namespace Wake.UI
{
    public readonly struct InvestigationProgressView
    {
        public InvestigationProgressView(int completed, int total)
        {
            Completed = completed;
            Total = total;
        }

        public int Completed { get; }
        public int Total { get; }
        public float Normalized =>
            Total <= 0 ? 0f : (float)Completed / Total;
        public bool IsComplete => Total > 0 && Completed == Total;
        public string Label => $"수사 진행  {Completed}/{Total}";
    }

    public static class InvestigationProgressPresentation
    {
        public static InvestigationProgressView Create(
            IEnumerable<string> completedSceneIds,
            IEnumerable<string> expectedSceneIds)
        {
            HashSet<string> expected = Normalize(expectedSceneIds);
            HashSet<string> completed = Normalize(completedSceneIds);
            completed.IntersectWith(expected);
            return new InvestigationProgressView(
                completed.Count,
                expected.Count);
        }

        private static HashSet<string> Normalize(
            IEnumerable<string> values)
        {
            return (values ?? Array.Empty<string>())
                .Select(value => value?.Trim().ToUpperInvariant())
                .Where(value => !string.IsNullOrEmpty(value))
                .ToHashSet(StringComparer.Ordinal);
        }
    }
}
