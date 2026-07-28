using UnityEngine;

namespace Wake.UI
{
    [ExecuteAlways]
    [DisallowMultipleComponent]
    public sealed class RuntimeUiLayoutSlot : MonoBehaviour
    {
        [SerializeField] private string slotId = string.Empty;
        [SerializeField] private Color editorColor =
            new(0.20f, 0.75f, 1f, 0.85f);
        [SerializeField] private bool showEditorFill = true;
        [SerializeField] private bool showEditorLabel = true;

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
#if UNITY_EDITOR
            Color fill = editorColor;
            fill.a = showEditorFill ? 0.08f : 0f;
            UnityEditor.Handles.DrawSolidRectangleWithOutline(
                corners,
                fill,
                editorColor);
            if (showEditorLabel)
            {
                UnityEditor.Handles.Label(
                    (corners[0] + corners[2]) * 0.5f,
                    SlotId);
            }
#else
            Gizmos.color = editorColor;
            for (int index = 0; index < corners.Length; index++)
            {
                Gizmos.DrawLine(
                    corners[index],
                    corners[(index + 1) % corners.Length]);
            }
#endif
        }
    }
}
