using System;
using System.Collections.Generic;
using UnityEngine;

namespace Wake.UI
{
    [ExecuteAlways]
    [DisallowMultipleComponent]
    public sealed class RuntimeUiLayoutRegistry : MonoBehaviour
    {
        private readonly Dictionary<string, RectTransform> slots =
            new(StringComparer.OrdinalIgnoreCase);

        private void OnEnable()
        {
            Rebuild();
        }

        private void OnTransformChildrenChanged()
        {
            Rebuild();
        }

        public void Rebuild()
        {
            slots.Clear();
            foreach (RuntimeUiLayoutSlot slot in
                     GetComponentsInChildren<RuntimeUiLayoutSlot>(true))
            {
                if (slot == null ||
                    string.IsNullOrWhiteSpace(slot.SlotId) ||
                    slot.transform is not RectTransform rect)
                {
                    continue;
                }

                slots[slot.SlotId.Trim()] = rect;
            }
        }

        public bool TryGet(string slotId, out RectTransform rect)
        {
            rect = null;
            if (string.IsNullOrWhiteSpace(slotId))
                return false;

            if (slots.Count == 0)
                Rebuild();
            if (slots.TryGetValue(slotId.Trim(), out rect) &&
                rect != null)
            {
                return true;
            }

            Rebuild();
            return slots.TryGetValue(slotId.Trim(), out rect) &&
                   rect != null;
        }

        public static bool TryResolve(
            string slotId,
            out RectTransform rect)
        {
            rect = null;
            foreach (RuntimeUiLayoutRegistry registry in
                     FindObjectsByType<RuntimeUiLayoutRegistry>(
                         FindObjectsInactive.Include,
                         FindObjectsSortMode.None))
            {
                if (registry != null &&
                    registry.TryGet(slotId, out rect))
                {
                    return true;
                }
            }
            return false;
        }

        public static bool Attach(
            RectTransform runtimeRect,
            string slotId)
        {
            if (runtimeRect == null ||
                !TryResolve(slotId, out RectTransform slot))
            {
                return false;
            }

            runtimeRect.SetParent(slot, false);
            runtimeRect.anchorMin = Vector2.zero;
            runtimeRect.anchorMax = Vector2.one;
            runtimeRect.pivot = new Vector2(0.5f, 0.5f);
            runtimeRect.anchoredPosition = Vector2.zero;
            runtimeRect.sizeDelta = Vector2.zero;
            runtimeRect.offsetMin = Vector2.zero;
            runtimeRect.offsetMax = Vector2.zero;
            return true;
        }

        public static bool CopyLayout(
            RectTransform runtimeRect,
            string slotId)
        {
            if (runtimeRect == null ||
                !TryResolve(slotId, out RectTransform slot))
            {
                return false;
            }

            runtimeRect.anchorMin = slot.anchorMin;
            runtimeRect.anchorMax = slot.anchorMax;
            runtimeRect.pivot = slot.pivot;
            runtimeRect.anchoredPosition = slot.anchoredPosition;
            runtimeRect.sizeDelta = slot.sizeDelta;
            runtimeRect.localRotation = slot.localRotation;
            runtimeRect.localScale = slot.localScale;
            return true;
        }

        public static bool CopyWorldLayout(
            RectTransform runtimeRect,
            string slotId)
        {
            if (runtimeRect == null ||
                runtimeRect.parent is not RectTransform parent ||
                !TryResolve(slotId, out RectTransform slot))
            {
                return false;
            }

            Vector3[] worldCorners = new Vector3[4];
            slot.GetWorldCorners(worldCorners);

            Vector2 localMin = parent.InverseTransformPoint(worldCorners[0]);
            Vector2 localMax = localMin;
            for (int index = 1; index < worldCorners.Length; index++)
            {
                Vector2 localCorner =
                    parent.InverseTransformPoint(worldCorners[index]);
                localMin = Vector2.Min(localMin, localCorner);
                localMax = Vector2.Max(localMax, localCorner);
            }

            Rect parentRect = parent.rect;
            if (parentRect.width <= Mathf.Epsilon ||
                parentRect.height <= Mathf.Epsilon)
            {
                return false;
            }

            Vector2 parentSize = parentRect.size;
            runtimeRect.anchorMin = new Vector2(
                (localMin.x - parentRect.xMin) / parentSize.x,
                (localMin.y - parentRect.yMin) / parentSize.y);
            runtimeRect.anchorMax = new Vector2(
                (localMax.x - parentRect.xMin) / parentSize.x,
                (localMax.y - parentRect.yMin) / parentSize.y);
            runtimeRect.pivot = new Vector2(0.5f, 0.5f);
            runtimeRect.anchoredPosition = Vector2.zero;
            runtimeRect.sizeDelta = Vector2.zero;
            runtimeRect.localRotation = Quaternion.identity;
            runtimeRect.localScale = Vector3.one;
            return true;
        }

        public static bool TryGetNormalizedRect(
            string slotId,
            out Rect normalizedRect)
        {
            normalizedRect = default;
            if (!TryResolve(slotId, out RectTransform slot))
                return false;

            Vector2 min = Vector2.Min(slot.anchorMin, slot.anchorMax);
            Vector2 max = Vector2.Max(slot.anchorMin, slot.anchorMax);
            normalizedRect = Rect.MinMaxRect(
                min.x,
                min.y,
                max.x,
                max.y);
            return normalizedRect.width > 0f &&
                   normalizedRect.height > 0f;
        }
    }

}
