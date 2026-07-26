using System;
using System.Collections.Generic;

namespace Wake.Core
{
    public sealed class ProductionEndingDefinition
    {
        public ProductionEndingDefinition(
            string endingId,
            string routeLabel,
            string title,
            string epilogue,
            bool opensConfession)
        {
            EndingId = endingId ?? string.Empty;
            RouteLabel = routeLabel ?? string.Empty;
            Title = title ?? string.Empty;
            Epilogue = epilogue ?? string.Empty;
            OpensConfession = opensConfession;
        }

        public string EndingId { get; }
        public string RouteLabel { get; }
        public string Title { get; }
        public string Epilogue { get; }
        public bool OpensConfession { get; }
    }

    public static class ProductionEndingCatalog
    {
        public const string ConfessionSceneId = "D8-02";
        public const string EpilogueSceneId = "D8-03";

        private static readonly ProductionEndingDefinition[] Entries =
        {
            new(
                FinalAccusationResolver.CompleteEndingId,
                "A 엔딩",
                "흔적 없는 항적",
                "Evelyn의 현재 살인과 Richard의 Orpheus 은폐가 모두 공개됐다. " +
                "Julian의 명예가 회복되고 Daniel의 기사는 사실에 맞게 수정됐다.",
                true),
            new(
                FinalAccusationResolver.ConvenientEndingId,
                "B 엔딩",
                "편리한 범인",
                "현재 살인은 해결됐지만 Orpheus 은폐는 남았다. " +
                "Daniel의 기사는 일부만 수정되고 Richard의 책임은 다시 봉인됐다.",
                true),
            new(
                FinalAccusationResolver.WrongPersonEndingId,
                "C 엔딩",
                "잘못된 지목",
                "핵심 논증이 끊긴 채 잘못된 결론이 발표됐다. " +
                "진범과 시신 이동 경로는 공식 기록에서 사라졌다.",
                false),
            new(
                FinalAccusationResolver.PanicEndingId,
                "배드 엔딩",
                "통제 불능",
                "승객 불안이 한계에 도달해 최종 지목 전에 수사가 중단됐다.",
                false),
            new(
                FinalAccusationResolver.IntegrityEndingId,
                "배드 엔딩",
                "훼손된 진실",
                "현장 보존도가 소진돼 직접 증거를 최종 논증에 사용할 수 없었다.",
                false)
        };

        private static readonly IReadOnlyDictionary<string, ProductionEndingDefinition>
            ById = new Dictionary<string, ProductionEndingDefinition>(
                StringComparer.Ordinal)
            {
                [Entries[0].EndingId] = Entries[0],
                [Entries[1].EndingId] = Entries[1],
                [Entries[2].EndingId] = Entries[2],
                [Entries[3].EndingId] = Entries[3],
                [Entries[4].EndingId] = Entries[4]
            };

        public static IReadOnlyList<ProductionEndingDefinition> All => Entries;

        public static bool TryGet(
            string endingId,
            out ProductionEndingDefinition definition)
        {
            return ById.TryGetValue(endingId ?? string.Empty, out definition);
        }

        public static string GetNextDialogueScene(
            string endingId,
            bool confessionCompleted,
            bool epilogueCompleted)
        {
            if (!TryGet(endingId, out ProductionEndingDefinition ending) ||
                !ending.OpensConfession)
            {
                return string.Empty;
            }

            if (!confessionCompleted)
            {
                return ConfessionSceneId;
            }

            return epilogueCompleted ? string.Empty : EpilogueSceneId;
        }
    }
}
