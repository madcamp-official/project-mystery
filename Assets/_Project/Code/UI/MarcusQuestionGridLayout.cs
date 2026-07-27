using System;
using UnityEngine;

namespace Wake.UI
{
    public readonly struct MarcusQuestionGridCell
    {
        public MarcusQuestionGridCell(
            Vector2 anchorMin,
            Vector2 anchorMax)
        {
            AnchorMin = anchorMin;
            AnchorMax = anchorMax;
        }

        public Vector2 AnchorMin { get; }
        public Vector2 AnchorMax { get; }
        public float Width => AnchorMax.x - AnchorMin.x;
        public float Height => AnchorMax.y - AnchorMin.y;
    }

    public static class MarcusQuestionGridLayout
    {
        public const int MaximumColumns = 2;
        public const float Left = 0.06f;
        public const float Right = 0.94f;
        public const float Bottom = 0.34f;
        public const float Top = 0.73f;
        public const float ColumnGap = 0.02f;
        public const float RowGap = 0.012f;

        public static MarcusQuestionGridCell Calculate(
            int index,
            int itemCount)
        {
            if (itemCount <= 0 || index < 0 || index >= itemCount)
            {
                throw new ArgumentOutOfRangeException(nameof(index));
            }

            int columns = itemCount > 4 ? MaximumColumns : 1;
            int rows = Mathf.CeilToInt(itemCount / (float)columns);
            int column = index % columns;
            int row = index / columns;
            float cellWidth =
                (Right - Left - ColumnGap * (columns - 1)) / columns;
            float cellHeight =
                (Top - Bottom - RowGap * (rows - 1)) / rows;
            float xMin = Left + column * (cellWidth + ColumnGap);
            float yMax = Top - row * (cellHeight + RowGap);
            return new MarcusQuestionGridCell(
                new Vector2(xMin, yMax - cellHeight),
                new Vector2(xMin + cellWidth, yMax));
        }
    }
}
