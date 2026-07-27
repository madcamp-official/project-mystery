using System;
using System.Collections.Generic;
using System.Linq;
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
            string carouselLabel)
        {
            Entry = entry;
            Definition = definition;
            State = state;
            Title = title ?? string.Empty;
            Detail = detail ?? string.Empty;
            CarouselLabel = carouselLabel ?? string.Empty;
        }

        public CanonicalEvidenceEntry Entry { get; }
        public EvidenceDefinition Definition { get; }
        public EvidencePanelItemState State { get; }
        public string Id => Entry?.Id ?? string.Empty;
        public string Title { get; }
        public string Detail { get; }
        public string CarouselLabel { get; }
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
        public static EvidencePanelViewModel Create(
            EvidenceInventory inventory,
            int evidenceIntegrity)
        {
            EvidenceDefinition[] collectedInOrder =
                (inventory?.Collected ?? Array.Empty<EvidenceDefinition>())
                .Where(item => item != null)
                .GroupBy(item =>
                    CanonicalEvidenceCatalog.NormalizeId(item.EvidenceId),
                    StringComparer.Ordinal)
                .Select(group => group.First())
                .ToArray();
            var collectedIds = collectedInOrder
                .Select(item =>
                    CanonicalEvidenceCatalog.NormalizeId(item.EvidenceId))
                .ToHashSet(StringComparer.Ordinal);
            bool empty = collectedInOrder.Length == 0;
            int unreliableCount = 0;
            var items = new List<EvidencePanelItem>(
                CanonicalEvidenceCatalog.All.Count);
            // 확보 항목은 플레이어가 발견한 순서를 유지한다. 아직 찾지 못한
            // 항목만 C-01~C-18 고정 순서로 뒤에 배치해 중복 없는 전체 카탈로그를 만든다.
            foreach (EvidenceDefinition definition in collectedInOrder)
            {
                if (!CanonicalEvidenceCatalog.TryGet(
                        definition.EvidenceId,
                        out CanonicalEvidenceEntry entry))
                {
                    continue;
                }
                AddCollectedItem(
                    items,
                    entry,
                    definition,
                    evidenceIntegrity,
                    ref unreliableCount);
            }
            foreach (CanonicalEvidenceEntry entry in CanonicalEvidenceCatalog.All)
            {
                if (!collectedIds.Contains(entry.Id))
                {
                    items.Add(CreateItem(
                        entry,
                        null,
                        EvidencePanelItemState.Missing,
                    empty));
                }
            }
            return new EvidencePanelViewModel(
                items,
                collectedInOrder.Length,
                unreliableCount);
        }

        private static void AddCollectedItem(
            List<EvidencePanelItem> items,
            CanonicalEvidenceEntry entry,
            EvidenceDefinition definition,
            int evidenceIntegrity,
            ref int unreliableCount)
        {
            EvidencePanelItemState state =
                evidenceIntegrity == 0 && entry.IsDirect
                    ? EvidencePanelItemState.Unreliable
                    : EvidencePanelItemState.Collected;
            if (state == EvidencePanelItemState.Unreliable)
            {
                unreliableCount++;
            }
            items.Add(CreateItem(entry, definition, state, false));
        }

        private static EvidencePanelItem CreateItem(
            CanonicalEvidenceEntry entry,
            EvidenceDefinition definition,
            EvidencePanelItemState state,
            bool inventoryEmpty)
        {
            if (state == EvidencePanelItemState.Missing)
            {
                string detail = inventoryEmpty
                    ? "확보한 증거가 없습니다.\n조사를 진행하면 이 슬롯의 정보가 해금됩니다."
                    : "아직 확보하지 못한 증거입니다.\n조사를 계속하면 이 슬롯의 정보가 해금됩니다.";
                return new EvidencePanelItem(
                    entry,
                    null,
                    state,
                    $"{entry.Id} · 미확보 증거",
                    detail,
                    $"{entry.Id}\n미확보");
            }
            bool unreliable = state == EvidencePanelItemState.Unreliable;
            string reliability = unreliable
                ? "무결성 저하 · 가설 검증에 사용 불가"
                : entry.IsDirect ? "확보 · 직접 증거" : "확보 · 간접 증거";
            string warning = unreliable
                ? "\n현장 훼손으로 직접 증거의 신뢰성이 사라졌습니다."
                : string.Empty;
            string source =
                string.Join(" · ", entry.SourceScenes);
            return new EvidencePanelItem(
                entry,
                definition,
                state,
                $"{entry.Id} · {entry.DisplayName}",
                $"[{reliability}]\n" +
                $"논증 역할 · {entry.ArgumentRole}\n" +
                $"획득 장면 · {source}\n" +
                $"{entry.Description}{warning}",
                $"{entry.Id}\n{(unreliable ? "무결성 저하" : entry.DisplayName)}");
        }
    }
}
