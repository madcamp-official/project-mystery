using System;
using UnityEditor;
using UnityEngine;
using Wake.UI;

namespace Wake.Editor
{
    public static class ScreenShellPrefabAuthoring
    {
        public const string PrefabFolder =
            "Assets/_Project/Prefabs/UI/ScreenShells";

        [MenuItem("Tools/Wake/Rebuild Common Screen Shell Prefabs")]
        public static void RebuildAll()
        {
            EnsureFolder("Assets/_Project/Prefabs", "UI");
            EnsureFolder("Assets/_Project/Prefabs/UI", "ScreenShells");

            foreach (ScreenShellType type in
                     Enum.GetValues(typeof(ScreenShellType)))
            {
                Build(type);
            }

            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
            Debug.Log(
                "Seven common screen shell prefabs were rebuilt.");
        }

        private static void Build(ScreenShellType type)
        {
            string shellName = $"{type}ScreenShell";
            if (type == ScreenShellType.ModalOverlay)
            {
                shellName = "ModalOverlayShell";
            }

            GameObject root = new(
                shellName,
                typeof(RectTransform),
                typeof(CanvasGroup),
                typeof(RuntimeUiLayoutRegistry),
                typeof(ScreenRegionSet),
                typeof(ScreenShellLayout));
            try
            {
                RectTransform rootRect =
                    root.GetComponent<RectTransform>();
                rootRect.sizeDelta = new Vector2(1920f, 1080f);
                rootRect.pivot = new Vector2(.5f, .5f);

                Color color = ResolveColor(type);
                RectTransform safeArea = EnsureRect(
                    rootRect,
                    "Safe Area");
                Stretch(safeArea);
                RuntimeUiLayoutSlot safeAreaSlot = ConfigureSlot(
                    safeArea,
                    ScreenShellSlotIds.SafeArea,
                    color);

                RectTransform common = EnsureRect(
                    safeArea,
                    "Common Regions");
                Stretch(common);
                RuntimeUiLayoutSlot context = Slot(
                    common,
                    "Context Top Left",
                    ScreenRegionIds.ContextTopLeft,
                    new Vector2(.02f, .84f),
                    new Vector2(.24f, .98f),
                    color);
                RuntimeUiLayoutSlot objective = Slot(
                    common,
                    "Objective Top",
                    ScreenRegionIds.ObjectiveTop,
                    new Vector2(.25f, .84f),
                    new Vector2(.74f, .98f),
                    color);
                RuntimeUiLayoutSlot global = Slot(
                    common,
                    "Global Top Right",
                    ScreenRegionIds.GlobalTopRight,
                    new Vector2(.75f, .84f),
                    new Vector2(.98f, .98f),
                    color);
                RuntimeUiLayoutSlot tools = Slot(
                    common,
                    "Tools Bottom Left",
                    ScreenRegionIds.ToolsBottomLeft,
                    new Vector2(.02f, .03f),
                    new Vector2(.26f, .20f),
                    color);
                RuntimeUiLayoutSlot reading = Slot(
                    common,
                    "Reading Bottom",
                    ScreenRegionIds.ReadingBottom,
                    new Vector2(.20f, .03f),
                    new Vector2(.80f, .30f),
                    color);
                RuntimeUiLayoutSlot primary = Slot(
                    common,
                    "Primary Bottom Right",
                    ScreenRegionIds.PrimaryBottomRight,
                    new Vector2(.74f, .03f),
                    new Vector2(.98f, .20f),
                    color);
                RuntimeUiLayoutSlot content = Slot(
                    common,
                    "Content Center",
                    ScreenRegionIds.ContentCenter,
                    new Vector2(.02f, .18f),
                    new Vector2(.98f, .86f),
                    color);

                RectTransform extended = EnsureRect(
                    safeArea,
                    "Extended Regions");
                Stretch(extended);
                RuntimeUiLayoutSlot leftPortrait = Slot(
                    extended,
                    "Portrait Left",
                    ScreenShellSlotIds.PortraitLeft,
                    new Vector2(.02f, .18f),
                    new Vector2(.36f, .86f),
                    color);
                RuntimeUiLayoutSlot rightPortrait = Slot(
                    extended,
                    "Portrait Right",
                    ScreenShellSlotIds.PortraitRight,
                    new Vector2(.64f, .18f),
                    new Vector2(.98f, .86f),
                    color);
                RuntimeUiLayoutSlot choices = Slot(
                    extended,
                    "Choices",
                    ScreenShellSlotIds.Choices,
                    new Vector2(.22f, .08f),
                    new Vector2(.78f, .36f),
                    color);
                RuntimeUiLayoutSlot dim = Slot(
                    extended,
                    "Modal Dim",
                    ScreenShellSlotIds.ModalDim,
                    Vector2.zero,
                    Vector2.one,
                    color);
                RuntimeUiLayoutSlot panel = Slot(
                    extended,
                    "Modal Panel",
                    ScreenShellSlotIds.ModalPanel,
                    new Vector2(.20f, .18f),
                    new Vector2(.80f, .82f),
                    color);
                BuildTypeSpecificSlots(type, extended, color);

                ScreenRegionSet regions =
                    root.GetComponent<ScreenRegionSet>();
                regions.Configure(
                    context,
                    objective,
                    global,
                    tools,
                    reading,
                    primary,
                    content);
                root.GetComponent<ScreenShellLayout>().Configure(
                    type,
                    regions,
                    safeAreaSlot,
                    leftPortrait,
                    rightPortrait,
                    choices,
                    dim,
                    panel);
                root.GetComponent<RuntimeUiLayoutRegistry>().Rebuild();

                string path = $"{PrefabFolder}/{shellName}.prefab";
                PrefabUtility.SaveAsPrefabAsset(
                    root,
                    path,
                    out bool success);
                if (!success)
                {
                    throw new InvalidOperationException(
                        $"Failed to save screen shell prefab: {path}");
                }
            }
            finally
            {
                UnityEngine.Object.DestroyImmediate(root);
            }
        }

        private static void BuildTypeSpecificSlots(
            ScreenShellType type,
            Transform parent,
            Color color)
        {
            if (type == ScreenShellType.Puzzle)
            {
                Slot(
                    parent,
                    "Puzzle Panel",
                    ScreenShellSlotIds.PuzzlePanel,
                    new Vector2(.04f, .08f),
                    new Vector2(.96f, .92f),
                    color);
                Slot(
                    parent,
                    "Final Accusation Panel",
                    ScreenShellSlotIds.FinalAccusationPanel,
                    new Vector2(.18f, .07f),
                    new Vector2(.82f, .93f),
                    color);
            }

            if (type != ScreenShellType.Ending)
                return;

            Slot(
                parent,
                "Ending Background",
                ScreenShellSlotIds.EndingBackground,
                Vector2.zero,
                Vector2.one,
                color);
            Slot(
                parent,
                "Ending Logo",
                ScreenShellSlotIds.EndingLogo,
                new Vector2(.07f, .70f),
                new Vector2(.38f, .95f),
                color);
            Slot(
                parent,
                "Ending Route",
                ScreenShellSlotIds.EndingRoute,
                new Vector2(.07f, .61f),
                new Vector2(.42f, .69f),
                color);
            Slot(
                parent,
                "Ending Title",
                ScreenShellSlotIds.EndingTitle,
                new Vector2(.07f, .50f),
                new Vector2(.42f, .61f),
                color);
            Slot(
                parent,
                "Ending Epilogue",
                ScreenShellSlotIds.EndingEpilogue,
                new Vector2(.075f, .37f),
                new Vector2(.415f, .50f),
                color);
            Slot(
                parent,
                "Ending Reason",
                ScreenShellSlotIds.EndingReason,
                new Vector2(.075f, .14f),
                new Vector2(.415f, .35f),
                color);
            Slot(
                parent,
                "Ending Primary",
                ScreenShellSlotIds.EndingPrimary,
                new Vector2(.08f, .04f),
                new Vector2(.31f, .12f),
                color);
        }

        private static RuntimeUiLayoutSlot Slot(
            Transform parent,
            string name,
            string id,
            Vector2 anchorMin,
            Vector2 anchorMax,
            Color color)
        {
            RectTransform rect = EnsureRect(parent, name);
            rect.anchorMin = anchorMin;
            rect.anchorMax = anchorMax;
            rect.pivot = new Vector2(.5f, .5f);
            rect.anchoredPosition = Vector2.zero;
            rect.sizeDelta = Vector2.zero;
            return ConfigureSlot(rect, id, color);
        }

        private static RuntimeUiLayoutSlot ConfigureSlot(
            RectTransform rect,
            string id,
            Color color)
        {
            RuntimeUiLayoutSlot slot =
                rect.GetComponent<RuntimeUiLayoutSlot>() ??
                rect.gameObject.AddComponent<RuntimeUiLayoutSlot>();
            slot.Configure(id, color);
            return slot;
        }

        private static RectTransform EnsureRect(
            Transform parent,
            string name)
        {
            GameObject target = new(name, typeof(RectTransform));
            RectTransform rect = target.GetComponent<RectTransform>();
            rect.SetParent(parent, false);
            return rect;
        }

        private static void Stretch(RectTransform rect)
        {
            rect.anchorMin = Vector2.zero;
            rect.anchorMax = Vector2.one;
            rect.pivot = new Vector2(.5f, .5f);
            rect.offsetMin = Vector2.zero;
            rect.offsetMax = Vector2.zero;
        }

        private static Color ResolveColor(ScreenShellType type)
        {
            float hue = ((int)type * .11f + .52f) % 1f;
            Color color = Color.HSVToRGB(hue, .70f, 1f);
            color.a = .85f;
            return color;
        }

        private static void EnsureFolder(string parent, string child)
        {
            string path = $"{parent}/{child}";
            if (!AssetDatabase.IsValidFolder(path))
            {
                AssetDatabase.CreateFolder(parent, child);
            }
        }
    }
}
