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
                    new Vector2(.26f, .98f),
                    color);
                RuntimeUiLayoutSlot objective = Slot(
                    common,
                    "Objective Top",
                    ScreenRegionIds.ObjectiveTop,
                    new Vector2(.25f, .84f),
                    new Vector2(.75f, .98f),
                    color);
                RuntimeUiLayoutSlot global = Slot(
                    common,
                    "Global Top Right",
                    ScreenRegionIds.GlobalTopRight,
                    new Vector2(.74f, .84f),
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
