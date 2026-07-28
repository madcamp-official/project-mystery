using System;
using System.Collections.Generic;
using System.Linq;
using Wake.Exploration;
using Wake.Narrative;

namespace Wake.Evidence
{
    public enum EvidencePanelItemState
    {
        Missing,
        Collected,
        Unreliable
    }

    public readonly struct EvidencePanelItem
    {
        public EvidencePanelItem(
            CanonicalEvidenceEntry entry,
            EvidenceDefinition definition,
            EvidencePanelItemState state,
            string title,
            string detail,
            string carouselLabel,
            string acquisitionPlace,
            string relatedPeople,
            string reliability)
        {
            Entry = entry;
            Definition = definition;
            State = state;
            Title = title ?? string.Empty;
            Detail = detail ?? string.Empty;
            CarouselLabel = carouselLabel ?? string.Empty;
            AcquisitionPlace = acquisitionPlace ?? string.Empty;
            RelatedPeople = relatedPeople ?? string.Empty;
            Reliability = reliability ?? string.Empty;
        }

        public CanonicalEvidenceEntry Entry { get; }
        public EvidenceDefinition Definition { get; }
        public EvidencePanelItemState State { get; }
        public string Id => Entry?.Id ?? string.Empty;
        public string Title { get; }
        public string Detail { get; }
        public string CarouselLabel { get; }
        public string AcquisitionPlace { get; }
        public string RelatedPeople { get; }
        public string Reliability { get; }
        public bool HasImage =>
            State != EvidencePanelItemState.Missing &&
            Definition?.Views != null &&
            Definition.Views.Length > 0;
    }

    public sealed class EvidencePanelViewModel
    {
        public EvidencePanelViewModel(
            IReadOnlyList<EvidencePanelItem> items,
            int collectedCount,
            int unreliableCount)
        {
            Items = items ?? Array.Empty<EvidencePanelItem>();
            CollectedCount = collectedCount;
            UnreliableCount = unreliableCount;
        }

        public IReadOnlyList<EvidencePanelItem> Items { get; }
        public int CollectedCount { get; }
        public int UnreliableCount { get; }
    }

    public static class EvidencePanelPresentation
    {
        private static readonly IReadOnlyDictionary<string, string>
            RelatedPeopleByEvidence =
                new Dictionary<string, string>(StringComparer.Ordinal)
                {
                    ["C-01"] = "리처드 호손 · 에이드리언 베일",
                    ["C-02"] = "에블린 그레이",
                    ["C-03"] = "피해자 · 최초 목격자",
                    ["C-04"] = "피해자",
                    ["C-05"] = "피해자 · 용의자",
                    ["C-06"] = "피해자 · 기관 승무원",
                    ["C-07"] = "피해자",
                    ["C-08"] = "보안 승무원 · 기관 승무원",
                    ["C-09"] = "기관 승무원",
                    ["C-10"] = "기관 승무원",
                    ["C-11"] = "피해자 · 목격자",
                    ["C-12"] = "피해자 · 검시 담당자",
                    ["C-13"] = "리처드 호손 · 에블린 그레이",
                    ["C-14"] = "클레어 호손",
                    ["C-15"] = "마커스 케인",
                    ["C-16"] = "피해자 · 에블린 그레이",
                    ["C-17"] = "리처드 호손 · 에블린 그레이",
                    ["C-18"] = "리처드 호손"
                };

        public static EvidencePanelViewModel Create(
            EvidenceInventory inventory,
            int evidenceIntegrity)
        {
            EvidenceDefinition[] collectedInOrder =
                (inventory?.Collected ?? Array.Empty<EvidenceDefinition>())
                .Where(item => item != null)
                .GroupBy(
                    item => CanonicalEvidenceCatalog.NormalizeId(
                        item.EvidenceId),
                    StringComparer.Ordinal)
                .Select(group => group.First())
                .ToArray();
            int unreliableCount = 0;
            var items = new List<EvidencePanelItem>(
                collectedInOrder.Length);

            // 발견한 기록만 획득 순서대로 보여 준다. 미발견 슬롯과
            // 전체 단서 수는 플레이어 화면에 노출하지 않는다.
            foreach (EvidenceDefinition definition in collectedInOrder)
            {
                if (!CanonicalEvidenceCatalog.TryGet(
                        definition.EvidenceId,
                        out CanonicalEvidenceEntry entry))
                {
                    continue;
                }

                EvidencePanelItemState state =
                    evidenceIntegrity == 0 && entry.IsDirect
                        ? EvidencePanelItemState.Unreliable
                        : EvidencePanelItemState.Collected;
                if (state == EvidencePanelItemState.Unreliable)
                {
                    unreliableCount++;
                }
                items.Add(CreateItem(entry, definition, state));
            }

            return new EvidencePanelViewModel(
                items,
                collectedInOrder.Length,
                unreliableCount);
        }

        private static EvidencePanelItem CreateItem(
            CanonicalEvidenceEntry entry,
            EvidenceDefinition definition,
            EvidencePanelItemState state)
        {
            bool unreliable =
                state == EvidencePanelItemState.Unreliable;
            string reliability = unreliable
                ? "현장 훼손으로 신뢰성을 다시 확인해야 함"
                : entry.IsDirect
                    ? "직접 확인한 기록"
                    : "정황을 보강하는 기록";
            return new EvidencePanelItem(
                entry,
                definition,
                state,
                entry.DisplayName,
                entry.Description,
                entry.DisplayName,
                ResolveAcquisitionPlace(definition),
                RelatedPeopleByEvidence.TryGetValue(
                    entry.Id,
                    out string people)
                    ? people
                    : "관련 인물 확인 중",
                reliability);
        }

        private static string ResolveAcquisitionPlace(
            EvidenceDefinition definition)
        {
            string[] locations = (definition?.SourceScenes ??
                                  Array.Empty<string>())
                .Select(sceneId =>
                    ProductionSceneCatalog.TryGet(
                        sceneId,
                        out ProductionSceneDefinition scene)
                        ? CanonicalLocationCatalog.FindSpec(
                            scene.NarrativeLocationCode)?.DisplayName
                        : null)
                .Where(value => !string.IsNullOrWhiteSpace(value))
                .Distinct(StringComparer.Ordinal)
                .ToArray();
            return locations.Length > 0
                ? string.Join(" · ", locations)
                : "획득 장소 기록 없음";
        }
    }
}
