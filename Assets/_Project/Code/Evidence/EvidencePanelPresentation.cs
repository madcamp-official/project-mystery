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
            int unreliableCount = 0;
            var items = new List<EvidencePanelItem>(collectedInOrder.Length);
            // 발견한 단서만 플레이어가 확보한 순서대로 보여 준다.
            // 미발견 단서는 슬롯, 실루엣, 전체 개수까지 모두 숨긴다.
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
            items.Add(CreateItem(entry, definition, state));
        }

        private static EvidencePanelItem CreateItem(
            CanonicalEvidenceEntry entry,
            EvidenceDefinition definition,
            EvidencePanelItemState state)
        {
            bool unreliable = state == EvidencePanelItemState.Unreliable;
            string reliability = unreliable
                ? "무결성 저하 · 가설 검증에 사용 불가"
                : entry.IsDirect ? "확보 · 직접 증거" : "확보 · 간접 증거";
            string warning = unreliable
                ? "\n현장 훼손으로 직접 증거의 신뢰성이 사라졌습니다."
                : string.Empty;
            return new EvidencePanelItem(
                entry,
                definition,
                state,
                entry.DisplayName,
                $"[{reliability}]\n" +
                $"논증 역할 · {entry.ArgumentRole}\n" +
                $"{entry.Description}{warning}",
                unreliable ? $"{entry.DisplayName}\n무결성 저하" : entry.DisplayName);
        }
    }
}
