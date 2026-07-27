using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using Wake.Exploration;
using Wake.Narrative;

namespace Wake.UI
{
    public enum ProductionMapEntryStatus
    {
        Available,
        Locked,
        Completed,
        LocationOnly
    }

    public sealed class ProductionMapEntry
    {
        public ProductionMapEntry(
            CanonicalLocationSpec spec,
            LocationDefinition location,
            string sceneId,
            ProductionMapEntryStatus status,
            SceneAccessDenialReason denialReason)
        {
            Spec = spec;
            Location = location;
            SceneId = sceneId ?? string.Empty;
            Status = status;
            DenialReason = denialReason;
        }

        public CanonicalLocationSpec Spec { get; }
        public LocationDefinition Location { get; }
        public string SceneId { get; }
        public ProductionMapEntryStatus Status { get; }
        public SceneAccessDenialReason DenialReason { get; }
        public bool UsesSceneTravel => !string.IsNullOrEmpty(SceneId);
        public bool StartsProductionScene =>
            UsesSceneTravel && Status == ProductionMapEntryStatus.Available;

        public string Header
        {
            get
            {
                string deck = Spec.Deck > 0 ? $"Deck {Spec.Deck}" : "Terminal";
                string room = string.IsNullOrEmpty(Spec.RoomCode)
                    ? string.Empty
                    : $" · {Spec.RoomCode}";
                return $"{deck}{room} · {Spec.DisplayName}";
            }
        }

        public string StatusLabel => Status switch
        {
            ProductionMapEntryStatus.Available => $"{SceneId} · 이동 가능",
            ProductionMapEntryStatus.Completed => $"{SceneId} · 완료",
            ProductionMapEntryStatus.LocationOnly => "자유 이동",
            _ => DenialReason switch
            {
                SceneAccessDenialReason.PrerequisiteSceneIncomplete =>
                    $"{SceneId} · 선행 장면 필요",
                SceneAccessDenialReason.RestrictedByPublicAnxiety =>
                    $"{SceneId} · 승객 불안으로 폐쇄",
                SceneAccessDenialReason.LocationVisualMissing =>
                    $"{SceneId} · 배경 누락",
                _ => $"{SceneId} · 이동 불가"
            }
        };
    }

    public sealed class DialogueOnlyMapEntry
    {
        public DialogueOnlyMapEntry(
            ProductionSceneDefinition scene,
            ProductionMapEntryStatus status,
            SceneAccessDenialReason denialReason)
        {
            Scene = scene;
            Status = status;
            DenialReason = denialReason;
        }

        public ProductionSceneDefinition Scene { get; }
        public string SceneId => Scene.SceneId;
        public string Header =>
            $"대화 전용 · {Scene.NarrativeLocationCode.Replace('_', ' ')}";
        public ProductionMapEntryStatus Status { get; }
        public SceneAccessDenialReason DenialReason { get; }
        public string StatusLabel => Status switch
        {
            ProductionMapEntryStatus.Available =>
                $"{SceneId} · 배경 유지 · 시작 가능",
            ProductionMapEntryStatus.Completed =>
                $"{SceneId} · 완료",
            _ => $"{SceneId} · 선행 장면 필요"
        };
    }

    public sealed class ProductionMapViewModel
    {
        private ProductionMapViewModel(
            IReadOnlyList<ProductionMapEntry> entries,
            IReadOnlyList<ProductionSceneDefinition> unresolvedScenes,
            IReadOnlyList<DialogueOnlyMapEntry> dialogueOnlyEntries)
        {
            Entries = entries;
            UnresolvedScenes = unresolvedScenes;
            DialogueOnlyEntries = dialogueOnlyEntries;
        }

        public IReadOnlyList<ProductionMapEntry> Entries { get; }
        public IReadOnlyList<ProductionSceneDefinition> UnresolvedScenes { get; }
        public IReadOnlyList<DialogueOnlyMapEntry> DialogueOnlyEntries { get; }

        public static ProductionMapViewModel Create(
            LocationGraph graph,
            IEnumerable<string> completedSceneIds,
            int publicAnxiety,
            string finalEndingId = "",
            IEnumerable<string> unlockedSceneIds = null)
        {
            var completed = new HashSet<string>(
                (completedSceneIds ?? Array.Empty<string>())
                .Where(value => !string.IsNullOrWhiteSpace(value))
                .Select(value => value.Trim().ToUpperInvariant()),
                StringComparer.Ordinal);
            HashSet<string> unlocked = unlockedSceneIds == null
                ? null
                : new HashSet<string>(
                    unlockedSceneIds
                        .Where(value => !string.IsNullOrWhiteSpace(value))
                        .Select(value => value.Trim().ToUpperInvariant()),
                    StringComparer.Ordinal);
            var unresolved = ProductionSceneCatalog.All
                .Where(scene =>
                    CanonicalLocationCatalog.UnresolvedCodes.Contains(
                        scene.NarrativeLocationCode))
                .ToArray();
            DialogueOnlyMapEntry[] dialogueOnly = unresolved
                .Select(scene =>
                {
                    SceneTravelResult result =
                        DialogueOnlySceneAccess.Evaluate(
                            scene.SceneId,
                            completed,
                            finalEndingId);
                    return new DialogueOnlyMapEntry(
                        scene,
                        completed.Contains(scene.SceneId)
                            ? ProductionMapEntryStatus.Completed
                            : result.IsAllowed
                                ? ProductionMapEntryStatus.Available
                                : ProductionMapEntryStatus.Locked,
                        result.DenialReason);
                })
                .ToArray();
            var entries = new List<ProductionMapEntry>();

            foreach (CanonicalLocationSpec spec in CanonicalLocationCatalog.All)
            {
                LocationDefinition location = graph?.FindByCode(spec.Code);
                ProductionSceneDefinition[] scenes = ProductionSceneCatalog.All
                    .Where(scene =>
                        CanonicalLocationCatalog.FindSpec(
                            scene.NarrativeLocationCode)?.Code == spec.Code)
                    .ToArray();
                ProductionSceneDefinition target =
                    scenes.FirstOrDefault(scene => !completed.Contains(scene.SceneId)) ??
                    scenes.LastOrDefault();

                if (target == null)
                {
                    SceneTravelResult locationResult =
                        SceneTravelPolicy.EvaluateLocation(location, publicAnxiety);
                    entries.Add(new ProductionMapEntry(
                        spec,
                        location,
                        string.Empty,
                        locationResult.IsAllowed
                            ? ProductionMapEntryStatus.LocationOnly
                            : ProductionMapEntryStatus.Locked,
                        locationResult.DenialReason));
                    continue;
                }

                SceneTravelResult result = SceneTravelPolicy.EvaluateScene(
                    target.SceneId,
                    graph,
                    completed,
                    publicAnxiety);
                bool isUnlocked =
                    target.SceneId == ProductionSceneDirector.OpeningSceneId ||
                    completed.Contains(target.SceneId) ||
                    unlocked == null ||
                    unlocked.Contains(target.SceneId);
                entries.Add(new ProductionMapEntry(
                    spec,
                    location,
                    target.SceneId,
                    completed.Contains(target.SceneId)
                        ? ProductionMapEntryStatus.Completed
                        : isUnlocked && result.IsAllowed
                            ? ProductionMapEntryStatus.Available
                            : ProductionMapEntryStatus.Locked,
                    !isUnlocked
                        ? SceneAccessDenialReason.SceneNotUnlocked
                        : result.DenialReason));
            }

            return new ProductionMapViewModel(
                entries,
                unresolved,
                dialogueOnly);
        }
    }

    public readonly struct ProductionMapLayout
    {
        public ProductionMapLayout(
            int columns,
            Vector2 cellSize,
            float contentHeight)
        {
            Columns = columns;
            CellSize = cellSize;
            ContentHeight = contentHeight;
        }

        public int Columns { get; }
        public Vector2 CellSize { get; }
        public float ContentHeight { get; }
    }

    public static class ProductionMapLayoutCalculator
    {
        public static ProductionMapLayout Calculate(
            int itemCount,
            float viewportWidth,
            Rect safeArea)
        {
            float usableWidth = Mathf.Max(
                280f,
                Mathf.Min(viewportWidth, safeArea.width) - 32f);
            int columns = usableWidth >= 1120f ? 3 : usableWidth >= 720f ? 2 : 1;
            const float gap = 12f;
            float cellWidth = (usableWidth - gap * (columns - 1)) / columns;
            const float cellHeight = 112f;
            int rows = Mathf.CeilToInt(Mathf.Max(0, itemCount) / (float)columns);
            float contentHeight = 16f + rows * cellHeight +
                                  Mathf.Max(0, rows - 1) * gap + 16f;
            return new ProductionMapLayout(
                columns,
                new Vector2(cellWidth, cellHeight),
                contentHeight);
        }
    }
}
