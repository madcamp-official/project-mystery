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
                "엔딩 A",
                "Complete Wake",
                "Evelyn의 현재 살인과 Richard의 Orpheus 은폐가 모두 공개됩니다. " +
                "줄리언의 명예가 회복되고 다니엘의 기사는 사실에 맞게 수정됩니다.",
                true),
            new(
                FinalAccusationResolver.ConvenientEndingId,
                "엔딩 B",
                "Convenient Culprit",
                "현재 살인은 해결되지만 Orpheus 은폐는 남습니다. " +
                "다니엘의 기사는 일부만 수정되고 리처드의 책임은 다시 봉인됩니다.",
                true),
            new(
                FinalAccusationResolver.WrongPersonEndingId,
                "엔딩 C",
                "The Wrong Man",
                "논증에 실패한 채 Richard를 범인으로 기록합니다. " +
                "Evelyn은 빠져나가고 진범과 시신 이동 경로는 공식 기록에서 사라집니다.",
                false),
            new(
                FinalAccusationResolver.BadEndingId,
                "배드 엔딩",
                "Panic at Sea",
                "승객 불안이 폭발하거나 증거가 훼손되어 수사가 중단되고 " +
                "MV Elysium은 비상 입항합니다.",
                false)
        };

        private static readonly IReadOnlyDictionary<string, ProductionEndingDefinition>
            ById = new Dictionary<string, ProductionEndingDefinition>(
                StringComparer.Ordinal)
            {
                [Entries[0].EndingId] = Entries[0],
                [Entries[1].EndingId] = Entries[1],
                [Entries[2].EndingId] = Entries[2],
                [Entries[3].EndingId] = Entries[3]
            };

        public static IReadOnlyList<ProductionEndingDefinition> All => Entries;

        public static bool TryGet(
            string endingId,
            out ProductionEndingDefinition definition)
        {
            return ById.TryGetValue(
                FinalAccusationResolver.NormalizeEndingId(endingId),
                out definition);
        }

        public static string GetNextDialogueScene(
            string endingId,
            bool confessionCompleted,
            bool epilogueCompleted)
        {
            if (!TryGet(endingId, out ProductionEndingDefinition ending) ||
                epilogueCompleted)
            {
                return string.Empty;
            }

            if (ending.OpensConfession && !confessionCompleted)
            {
                return ConfessionSceneId;
            }
            return EpilogueSceneId;
        }
    }
}
