using UnityEngine;

namespace Wake.UI
{
    public readonly struct DialogueChoiceLayoutSpec
    {
        public DialogueChoiceLayoutSpec(
            int columns,
            int rows,
            Vector2 cellSize,
            float requiredHeight)
        {
            Columns = columns;
            Rows = rows;
            CellSize = cellSize;
            RequiredHeight = requiredHeight;
        }

        public int Columns { get; }
        public int Rows { get; }
        public Vector2 CellSize { get; }
        public float RequiredHeight { get; }
    }

    public static class DialogueChoiceLayoutPolicy
    {
        public const int MaximumColumns = 2;
        public const float MinimumTwoColumnCellWidth = 400f;
        public const float MinimumCellHeight = 108f;
        public const float MaximumCellHeight = 168f;
        public const float LabelHorizontalPadding = 48f;
        public const float LabelVerticalPadding = 32f;
        public const float Spacing = 16f;
        public const int Padding = 10;

        public static DialogueChoiceLayoutSpec Calculate(
            float availableWidth,
            int activeChoiceCount,
            float maximumPreferredLabelHeight = 0f)
        {
            if (activeChoiceCount <= 0 || availableWidth <= 0f)
            {
                return new DialogueChoiceLayoutSpec(
                    1,
                    0,
                    Vector2.zero,
                    0f);
            }

            float contentWidth = Mathf.Max(
                1f,
                availableWidth - Padding * 2f);
            float twoColumnWidth =
                (contentWidth - Spacing) / MaximumColumns;
            int columns =
                activeChoiceCount > 1 &&
                twoColumnWidth >= MinimumTwoColumnCellWidth
                    ? MaximumColumns
                    : 1;
            int rows = Mathf.CeilToInt(
                activeChoiceCount / (float)columns);
            float cellWidth =
                (contentWidth - Spacing * (columns - 1)) / columns;
            float cellHeight = Mathf.Clamp(
                maximumPreferredLabelHeight + LabelVerticalPadding,
                MinimumCellHeight,
                MaximumCellHeight);
            float requiredHeight =
                Padding * 2f +
                rows * cellHeight +
                Mathf.Max(0, rows - 1) * Spacing;

            return new DialogueChoiceLayoutSpec(
                columns,
                rows,
                new Vector2(cellWidth, cellHeight),
                requiredHeight);
        }

        public static float GetLabelWidth(
            DialogueChoiceLayoutSpec spec) =>
            Mathf.Max(
                1f,
                spec.CellSize.x - LabelHorizontalPadding);
    }
}
