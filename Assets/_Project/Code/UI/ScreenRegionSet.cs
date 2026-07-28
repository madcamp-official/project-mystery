using System.Collections.Generic;
using UnityEngine;

namespace Wake.UI
{
    public enum ScreenRegion
    {
        ContextTopLeft,
        ObjectiveTop,
        GlobalTopRight,
        ToolsBottomLeft,
        ReadingBottom,
        PrimaryBottomRight,
        ContentCenter
    }

    public static class ScreenRegionIds
    {
        public const string ContextTopLeft = "screen.context.topLeft";
        public const string ObjectiveTop = "screen.objective.top";
        public const string GlobalTopRight = "screen.global.topRight";
        public const string ToolsBottomLeft = "screen.tools.bottomLeft";
        public const string ReadingBottom = "screen.reading.bottom";
        public const string PrimaryBottomRight =
            "screen.primary.bottomRight";
        public const string ContentCenter = "screen.content.center";

        public static readonly IReadOnlyList<string> All = new[]
        {
            ContextTopLeft,
            ObjectiveTop,
            GlobalTopRight,
            ToolsBottomLeft,
            ReadingBottom,
            PrimaryBottomRight,
            ContentCenter
        };
    }

    [ExecuteAlways]
    [DisallowMultipleComponent]
    public sealed class ScreenRegionSet : MonoBehaviour
    {
        [SerializeField] private RuntimeUiLayoutSlot contextTopLeft;
        [SerializeField] private RuntimeUiLayoutSlot objectiveTop;
        [SerializeField] private RuntimeUiLayoutSlot globalTopRight;
        [SerializeField] private RuntimeUiLayoutSlot toolsBottomLeft;
        [SerializeField] private RuntimeUiLayoutSlot readingBottom;
        [SerializeField] private RuntimeUiLayoutSlot primaryBottomRight;
        [SerializeField] private RuntimeUiLayoutSlot contentCenter;

        public bool IsComplete =>
            contextTopLeft != null &&
            objectiveTop != null &&
            globalTopRight != null &&
            toolsBottomLeft != null &&
            readingBottom != null &&
            primaryBottomRight != null &&
            contentCenter != null;

        public void Configure(
            RuntimeUiLayoutSlot context,
            RuntimeUiLayoutSlot objective,
            RuntimeUiLayoutSlot global,
            RuntimeUiLayoutSlot tools,
            RuntimeUiLayoutSlot reading,
            RuntimeUiLayoutSlot primary,
            RuntimeUiLayoutSlot content)
        {
            contextTopLeft = context;
            objectiveTop = objective;
            globalTopRight = global;
            toolsBottomLeft = tools;
            readingBottom = reading;
            primaryBottomRight = primary;
            contentCenter = content;
        }

        public bool TryGet(ScreenRegion region, out RectTransform rect)
        {
            RuntimeUiLayoutSlot slot = region switch
            {
                ScreenRegion.ContextTopLeft => contextTopLeft,
                ScreenRegion.ObjectiveTop => objectiveTop,
                ScreenRegion.GlobalTopRight => globalTopRight,
                ScreenRegion.ToolsBottomLeft => toolsBottomLeft,
                ScreenRegion.ReadingBottom => readingBottom,
                ScreenRegion.PrimaryBottomRight => primaryBottomRight,
                ScreenRegion.ContentCenter => contentCenter,
                _ => null
            };
            rect = slot != null ? slot.transform as RectTransform : null;
            return rect != null;
        }

        public IEnumerable<RuntimeUiLayoutSlot> Enumerate()
        {
            yield return contextTopLeft;
            yield return objectiveTop;
            yield return globalTopRight;
            yield return toolsBottomLeft;
            yield return readingBottom;
            yield return primaryBottomRight;
            yield return contentCenter;
        }
    }
}
