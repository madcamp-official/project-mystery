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

    [ExecuteAlways]
    [DisallowMultipleComponent]
    public sealed class RuntimeUiLayoutSlot : MonoBehaviour
    {
        [SerializeField] private string slotId = string.Empty;
        [SerializeField] private Color editorColor =
            new(0.20f, 0.75f, 1f, 0.85f);

        public string SlotId => string.IsNullOrWhiteSpace(slotId)
            ? gameObject.name
            : slotId;

        public void Configure(string id, Color color)
        {
            slotId = id?.Trim() ?? string.Empty;
            editorColor = color;
        }

        private void OnDrawGizmos()
        {
            if (transform is not RectTransform rect)
                return;

            Vector3[] corners = new Vector3[4];
            rect.GetWorldCorners(corners);
            Gizmos.color = editorColor;
            for (int index = 0; index < corners.Length; index++)
            {
                Gizmos.DrawLine(
                    corners[index],
                    corners[(index + 1) % corners.Length]);
            }
        }
    }
}
