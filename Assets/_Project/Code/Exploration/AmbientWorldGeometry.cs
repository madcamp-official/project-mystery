using UnityEngine;

namespace Wake.Exploration
{
    public readonly struct AmbientWorldLayoutMetrics
    {
        public AmbientWorldLayoutMetrics(
            Vector2 rectSize,
            float anchoredOffsetY,
            float visibleHeight,
            float visibleFootY,
            float visibleHeadY,
            Vector2 groundShadowSize)
        {
            RectSize = rectSize;
            AnchoredOffsetY = anchoredOffsetY;
            VisibleHeight = visibleHeight;
            VisibleFootY = visibleFootY;
            VisibleHeadY = visibleHeadY;
            GroundShadowSize = groundShadowSize;
        }

        public Vector2 RectSize { get; }
        public float AnchoredOffsetY { get; }
        public float VisibleHeight { get; }
        public float VisibleFootY { get; }
        public float VisibleHeadY { get; }
        public Vector2 GroundShadowSize { get; }
    }

    public static class AmbientWorldGeometry
    {
        public static AmbientWorldLayoutMetrics Calculate(
            Vector2 contentSize,
            AmbientWorldStageProfile stage,
            AmbientWorldCharacterAsset asset)
        {
            float contentWidth = Mathf.Max(1f, contentSize.x);
            float contentHeight = Mathf.Max(1f, contentSize.y);
            float visibleHeight =
                contentHeight * stage.NormalizedHeight;
            float rectHeight =
                visibleHeight / asset.VisibleVerticalSpan;
            float rectWidth =
                rectHeight * asset.CellAspectRatio;
            float anchoredOffsetY =
                -rectHeight * asset.VisibleBottomMargin;
            float visibleFootY =
                contentHeight * stage.Anchor.y;
            float visibleHeadY =
                visibleFootY + visibleHeight;
            float shadowWidth =
                rectWidth * stage.GroundShadowScale;
            float shadowHeight =
                Mathf.Max(8f, rectHeight * 0.045f);

            return new AmbientWorldLayoutMetrics(
                new Vector2(
                    Mathf.Min(contentWidth, rectWidth),
                    rectHeight),
                anchoredOffsetY,
                visibleHeight,
                visibleFootY,
                visibleHeadY,
                new Vector2(shadowWidth, shadowHeight));
        }

        public static float VisibleFootOffset(
            AmbientWorldLayoutMetrics metrics,
            AmbientWorldCharacterAsset asset)
        {
            return metrics.AnchoredOffsetY +
                   metrics.RectSize.y * asset.VisibleBottomMargin;
        }

        public static bool FitsVerticalStage(
            AmbientWorldLayoutMetrics metrics,
            float contentHeight,
            float topSafeBand = 0.75f)
        {
            float safeHeight =
                Mathf.Max(1f, contentHeight) *
                Mathf.Clamp01(topSafeBand);
            return metrics.VisibleFootY >= 0f &&
                   metrics.VisibleHeadY <= safeHeight;
        }
    }
}
