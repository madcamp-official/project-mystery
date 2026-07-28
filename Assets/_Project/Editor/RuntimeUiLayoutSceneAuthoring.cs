using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using Wake.Exploration;
using Wake.UI;

namespace Wake.Editor
{
    public static class RuntimeUiLayoutSceneAuthoring
    {
        [MenuItem("Tools/Wake/Rebuild Runtime UI Layout Placeholders")]
        public static void Rebuild()
        {
            GameObject canvasObject = GameObject.Find("Canvas");
            if (canvasObject == null)
            {
                Debug.LogError("Runtime UI layout authoring requires Canvas.");
                return;
            }

            RectTransform canvas =
                canvasObject.GetComponent<RectTransform>();
            RuntimeUiLayoutRegistry registry =
                canvasObject.GetComponent<RuntimeUiLayoutRegistry>() ??
                canvasObject.AddComponent<RuntimeUiLayoutRegistry>();
            RectTransform layout = EnsureRect(canvas, "Runtime UI Layout");
            Stretch(layout);
            layout.SetAsLastSibling();

            BuildHud(layout);
            BuildDialogue(layout);
            ConfigureDialogueAdvance(canvas);
            BuildModals(layout);
            BuildLocationOverlays(layout);

            Transform progress =
                canvas.Find("Status HUD/Investigation Progress");
            if (progress != null)
                Object.DestroyImmediate(progress.gameObject);

            registry.Rebuild();
            EditorUtility.SetDirty(canvasObject);
            EditorSceneManager.MarkSceneDirty(
                SceneManager.GetActiveScene());
            EditorSceneManager.SaveOpenScenes();
            Debug.Log(
                $"Runtime UI layout ready: " +
                $"{layout.GetComponentsInChildren<RuntimeUiLayoutSlot>(true).Length} slots.");
        }

        private static void BuildHud(RectTransform layout)
        {
            Color cyan = new(0.20f, 0.75f, 1f, 0.85f);
            RectTransform hud = EnsureRect(layout, "HUD Slots");
            hud.anchorMin = new Vector2(0f, 1f);
            hud.anchorMax = new Vector2(1f, 1f);
            hud.pivot = new Vector2(0.5f, 1f);
            hud.anchoredPosition = Vector2.zero;
            hud.sizeDelta = new Vector2(0f, 168f);
            Slot(hud, "Time Badge Slot", "hud.time",
                new Vector2(.01f, .08f), new Vector2(.25f, .92f),
                Vector2.zero, cyan);
            Slot(hud, "Anxiety Indicator Slot", "hud.anxiety",
                new Vector2(.26f, .08f), new Vector2(.62f, .92f),
                Vector2.zero, cyan);
            Slot(hud, "Integrity Indicator Slot", "hud.integrity",
                new Vector2(.63f, .08f), new Vector2(.99f, .92f),
                Vector2.zero, cyan);

            RectTransform floating =
                EnsureRect(layout, "Floating HUD Slots");
            Stretch(floating);
            RectTransform objectiveRect = Slot(
                floating,
                "Objective Banner Slot",
                "hud.objective",
                new Vector2(.5f, 0f),
                new Vector2(.5f, 0f),
                new Vector2(660f, 92f),
                cyan);
            objectiveRect.pivot = new Vector2(.5f, 1f);
            objectiveRect.anchoredPosition = new Vector2(0f, -12f);
            Slot(floating, "Location Banner Slot", "hud.location",
                new Vector2(.5f, .82f), new Vector2(.5f, .82f),
                new Vector2(620f, 70f), cyan);
            Slot(floating, "Toast Slot", "hud.toast",
                new Vector2(.5f, .74f), new Vector2(.5f, .74f),
                new Vector2(480f, 60f), cyan);
        }

        private static void BuildDialogue(RectTransform layout)
        {
            Color magenta = new(1f, .35f, .75f, .85f);
            RectTransform dialogue =
                EnsureRect(layout, "Dialogue Slots");
            Stretch(dialogue);
            Slot(dialogue, "Speaker Portrait Slot",
                "dialogue.speaker-portrait",
                new Vector2(.27f, .13f), new Vector2(.27f, .13f),
                new Vector2(360f, 430f), magenta);
            Slot(dialogue, "Focus Panel Slot",
                "dialogue.focus-panel",
                new Vector2(.09f, .18f), new Vector2(.83f, .56f),
                Vector2.zero, magenta);
            Slot(dialogue, "Compact Panel Slot",
                "dialogue.compact-panel",
                new Vector2(.18f, .08f), new Vector2(.82f, .32f),
                Vector2.zero, magenta);
            Slot(dialogue, "Narration Panel Slot",
                "dialogue.narration-panel",
                new Vector2(.14f, .22f), new Vector2(.86f, .49f),
                Vector2.zero, magenta);
            Slot(dialogue, "Focus Portrait Left Slot",
                "dialogue.focus-portrait-left",
                new Vector2(.04f, .12f), new Vector2(.36f, .78f),
                Vector2.zero, magenta);
            Slot(dialogue, "Focus Portrait Right Slot",
                "dialogue.focus-portrait-right",
                new Vector2(.64f, .12f), new Vector2(.96f, .82f),
                Vector2.zero, magenta);
            Slot(dialogue, "Compact Portrait Slot",
                "dialogue.compact-portrait",
                new Vector2(.12f, .10f), new Vector2(.34f, .40f),
                Vector2.zero, magenta);
            Slot(dialogue, "Investigation Overlay Slot",
                "dialogue.investigation",
                Vector2.zero, Vector2.one, Vector2.zero, magenta);
        }

        private static void ConfigureDialogueAdvance(RectTransform canvas)
        {
            Transform target =
                canvas.Find("Ingame/Line Panel/Panel/Next");
            if (target == null ||
                !target.TryGetComponent(out UnityEngine.UI.Button button))
            {
                Debug.LogWarning(
                    "Dialogue advance button was not found while authoring.");
                return;
            }

            const string assetRoot =
                "Assets/_Project/Art/UI/Dialogue/";
            Sprite normal = AssetDatabase.LoadAssetAtPath<Sprite>(
                assetRoot + "ui_btn_dialogue_advance_normal.png");
            Sprite pressed = AssetDatabase.LoadAssetAtPath<Sprite>(
                assetRoot + "ui_btn_dialogue_advance_pressed.png");
            DialogueAdvanceControl control =
                target.GetComponent<DialogueAdvanceControl>() ??
                target.gameObject.AddComponent<DialogueAdvanceControl>();
            control.Initialize(button);
            control.SetSprites(normal, pressed);
            EditorUtility.SetDirty(target.gameObject);
        }

        private static void BuildModals(RectTransform layout)
        {
            Color gold = new(1f, .68f, .20f, .85f);
            RectTransform modals = EnsureRect(layout, "Modal Slots");
            Stretch(modals);
            Modal(modals, "Exit Inspection Slot",
                "modal.exit-inspection", 920f, 680f, gold);
            Modal(modals, "Production Puzzle Slot",
                "modal.production-puzzle", 760f, 560f, gold);
            Modal(modals, "Marcus Interrogation Slot",
                "modal.marcus-interrogation", 1000f, 700f, gold);
            Modal(modals, "Timeline Puzzle Slot",
                "modal.timeline-puzzle", 1040f, 690f, gold);
            Modal(modals, "Orpheus Restoration Slot",
                "modal.orpheus-restoration", 960f, 640f, gold);
            Modal(modals, "Theory Board Slot",
                "modal.theory-board", 920f, 650f, gold);
            Slot(modals, "Final Accusation Slot",
                "modal.final-accusation",
                new Vector2(.20f, .08f), new Vector2(.80f, .92f),
                Vector2.zero, gold);
            Slot(modals, "Ending Slot", "modal.ending",
                Vector2.zero, Vector2.one, Vector2.zero, gold);
        }

        private static void BuildLocationOverlays(RectTransform layout)
        {
            Color green = new(.30f, 1f, .45f, .80f);
            RectTransform locations =
                EnsureRect(layout, "Location Overlay Slots");
            Stretch(locations);

            foreach (AmbientWorldStageRecord record in
                     AmbientWorldStageCatalog.All)
            {
                RectTransform group =
                    EnsureRect(locations, record.Location);
                Stretch(group);
                AmbientWorldStageProfile profile = record.Profile;
                float halfWidth = profile.NormalizedHeight * .15f;
                Vector2 min = new(
                    Mathf.Clamp01(profile.Anchor.x - halfWidth),
                    Mathf.Clamp01(profile.Anchor.y));
                Vector2 max = new(
                    Mathf.Clamp01(profile.Anchor.x + halfWidth),
                    Mathf.Clamp01(
                        profile.Anchor.y + profile.NormalizedHeight));
                Slot(group, $"Character - {record.Speaker}",
                    $"location.{record.Location}.character.{record.Speaker}",
                    min, max, Vector2.zero, green);
            }

            foreach (AmbientInspectableSpec spec in
                     AmbientInspectableCatalog.All)
            {
                RectTransform group =
                    EnsureRect(locations, spec.Location);
                Stretch(group);
                Slot(group, $"Inspectable - {spec.Id}",
                    $"location.{spec.Location}.inspectable.{spec.Id}",
                    spec.Hotspot.min, spec.Hotspot.max,
                    Vector2.zero, green);
            }

            foreach (EvidenceLocationHotspotSpec spec in
                     EvidenceLocationHotspotCatalog.All)
            {
                RectTransform group =
                    EnsureRect(locations, spec.LocationCode);
                Stretch(group);
                Slot(group, $"Evidence - {spec.EvidenceId}",
                    $"location.{spec.LocationCode}.evidence.{spec.EvidenceId}",
                    spec.NormalizedRect.min, spec.NormalizedRect.max,
                    Vector2.zero, green);
            }
        }

        private static void Modal(
            Transform parent,
            string name,
            string id,
            float width,
            float height,
            Color color)
        {
            Slot(parent, name, id,
                new Vector2(.5f, .5f), new Vector2(.5f, .5f),
                new Vector2(width, height), color);
        }

        private static RectTransform Slot(
            Transform parent,
            string name,
            string id,
            Vector2 min,
            Vector2 max,
            Vector2 size,
            Color color)
        {
            RectTransform rect = EnsureRect(parent, name);
            rect.anchorMin = min;
            rect.anchorMax = max;
            rect.pivot = new Vector2(.5f, .5f);
            rect.anchoredPosition = Vector2.zero;
            rect.sizeDelta = size;
            RuntimeUiLayoutSlot slot =
                rect.GetComponent<RuntimeUiLayoutSlot>() ??
                rect.gameObject.AddComponent<RuntimeUiLayoutSlot>();
            slot.Configure(id, color);
            return rect;
        }

        private static RectTransform EnsureRect(
            Transform parent,
            string name)
        {
            RectTransform existing =
                parent.Find(name) as RectTransform;
            if (existing != null)
                return existing;

            var target = new GameObject(name, typeof(RectTransform));
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
    }
}
