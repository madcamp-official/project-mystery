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
            RemoveMissingScripts(layout);
            RemoveObsoletePlaceholder(
                layout,
                "Dialogue Slots/Focus Panel Slot");

            BuildScreenShellSlots(layout);
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

        private static void BuildScreenShellSlots(RectTransform layout)
        {
            Color blue = new(.25f, .80f, 1f, .90f);
            RectTransform shell =
                EnsureRect(layout, "Screen Shell Slots");
            Stretch(shell);

            RuntimeUiLayoutSlot context = Slot(
                shell,
                "Context Top Left Slot",
                ScreenRegionIds.ContextTopLeft,
                new Vector2(.02f, .84f),
                new Vector2(.26f, .98f),
                Vector2.zero,
                blue).GetComponent<RuntimeUiLayoutSlot>();
            RuntimeUiLayoutSlot objective = Slot(
                shell,
                "Objective Top Slot",
                ScreenRegionIds.ObjectiveTop,
                new Vector2(.25f, .84f),
                new Vector2(.75f, .98f),
                Vector2.zero,
                blue).GetComponent<RuntimeUiLayoutSlot>();
            RuntimeUiLayoutSlot global = Slot(
                shell,
                "Global Top Right Slot",
                ScreenRegionIds.GlobalTopRight,
                new Vector2(.74f, .84f),
                new Vector2(.98f, .98f),
                Vector2.zero,
                blue).GetComponent<RuntimeUiLayoutSlot>();
            RuntimeUiLayoutSlot tools = Slot(
                shell,
                "Tools Bottom Left Slot",
                ScreenRegionIds.ToolsBottomLeft,
                new Vector2(.02f, .03f),
                new Vector2(.26f, .20f),
                Vector2.zero,
                blue).GetComponent<RuntimeUiLayoutSlot>();
            RuntimeUiLayoutSlot reading = Slot(
                shell,
                "Reading Bottom Slot",
                ScreenRegionIds.ReadingBottom,
                new Vector2(.20f, .03f),
                new Vector2(.80f, .30f),
                Vector2.zero,
                blue).GetComponent<RuntimeUiLayoutSlot>();
            RuntimeUiLayoutSlot primary = Slot(
                shell,
                "Primary Bottom Right Slot",
                ScreenRegionIds.PrimaryBottomRight,
                new Vector2(.74f, .03f),
                new Vector2(.98f, .20f),
                Vector2.zero,
                blue).GetComponent<RuntimeUiLayoutSlot>();
            RuntimeUiLayoutSlot content = Slot(
                shell,
                "Content Center Slot",
                ScreenRegionIds.ContentCenter,
                new Vector2(.02f, .18f),
                new Vector2(.98f, .86f),
                Vector2.zero,
                blue).GetComponent<RuntimeUiLayoutSlot>();

            ScreenRegionSet regions =
                shell.GetComponent<ScreenRegionSet>() ??
                shell.gameObject.AddComponent<ScreenRegionSet>();
            regions.Configure(
                context,
                objective,
                global,
                tools,
                reading,
                primary,
                content);
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
            Slot(dialogue, "Focus Panel Left Slot",
                "dialogue.focus-panel-left",
                new Vector2(.06f, .18f), new Vector2(.60f, .68f),
                Vector2.zero, magenta);
            Slot(dialogue, "Focus Panel Right Slot",
                "dialogue.focus-panel-right",
                new Vector2(.40f, .18f), new Vector2(.94f, .68f),
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
                new Vector2(.04f, .10f), new Vector2(.38f, .86f),
                Vector2.zero, magenta);
            Slot(dialogue, "Focus Portrait Right Slot",
                "dialogue.focus-portrait-right",
                new Vector2(.62f, .10f), new Vector2(.96f, .86f),
                Vector2.zero, magenta);
            Slot(dialogue, "Compact Portrait Slot",
                "dialogue.compact-portrait",
                new Vector2(.12f, .10f), new Vector2(.34f, .40f),
                Vector2.zero, magenta);
            Slot(dialogue, "Investigation Overlay Slot",
                "dialogue.investigation",
                Vector2.zero, Vector2.one, Vector2.zero, magenta);
            Slot(dialogue, "Dialogue Dim Slot",
                "dialogue.dim",
                Vector2.zero, Vector2.one, Vector2.zero, magenta);
            Slot(dialogue, "Focus Text Left Slot",
                "dialogue.focus-text-left",
                new Vector2(.09f, .31f), new Vector2(.55f, .61f),
                Vector2.zero, magenta);
            Slot(dialogue, "Focus Text Right Slot",
                "dialogue.focus-text-right",
                new Vector2(.45f, .31f), new Vector2(.91f, .61f),
                Vector2.zero, magenta);
            Slot(dialogue, "Compact Text Slot",
                "dialogue.compact-text",
                new Vector2(.37f, .15f), new Vector2(.77f, .27f),
                Vector2.zero, magenta);
            Slot(dialogue, "Narration Text Slot",
                "dialogue.narration-text",
                new Vector2(.20f, .29f), new Vector2(.80f, .43f),
                Vector2.zero, magenta);
            Slot(dialogue, "Speaker Name Left Slot",
                "dialogue.speaker-name-left",
                new Vector2(.06f, .69f), new Vector2(.31f, .75f),
                Vector2.zero, magenta);
            Slot(dialogue, "Speaker Name Right Slot",
                "dialogue.speaker-name-right",
                new Vector2(.69f, .69f), new Vector2(.94f, .75f),
                Vector2.zero, magenta);
            Slot(dialogue, "Advance Left Slot",
                "dialogue.advance-left",
                new Vector2(.54f, .20f), new Vector2(.59f, .28f),
                Vector2.zero, magenta);
            Slot(dialogue, "Advance Right Slot",
                "dialogue.advance-right",
                new Vector2(.41f, .20f), new Vector2(.46f, .28f),
                Vector2.zero, magenta);
            Slot(dialogue, "Advance Center Slot",
                "dialogue.advance-center",
                new Vector2(.75f, .08f), new Vector2(.80f, .16f),
                Vector2.zero, magenta);
            Slot(dialogue, "Choices Left Slot",
                "dialogue.choices-left",
                new Vector2(.06f, .04f), new Vector2(.58f, .20f),
                Vector2.zero, magenta);
            Slot(dialogue, "Choices Right Slot",
                "dialogue.choices-right",
                new Vector2(.42f, .04f), new Vector2(.94f, .20f),
                Vector2.zero, magenta);
            Slot(dialogue, "Investigation Frame Slot",
                "dialogue.investigation.frame",
                new Vector2(.16f, .14f), new Vector2(.84f, .86f),
                Vector2.zero, magenta);
            Slot(dialogue, "Investigation Section Slot",
                "dialogue.investigation.section",
                new Vector2(.21f, .73f), new Vector2(.79f, .82f),
                Vector2.zero, magenta);
            Slot(dialogue, "Investigation Title Slot",
                "dialogue.investigation.title",
                new Vector2(.21f, .58f), new Vector2(.79f, .73f),
                Vector2.zero, magenta);
            Slot(dialogue, "Investigation Body Slot",
                "dialogue.investigation.body",
                new Vector2(.21f, .31f), new Vector2(.79f, .58f),
                Vector2.zero, magenta);
            Slot(dialogue, "Investigation Action Slot",
                "dialogue.investigation.action",
                new Vector2(.40f, .18f), new Vector2(.60f, .28f),
                Vector2.zero, magenta);
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
            BuildMarcusInterrogation(modals, gold);
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

        private static void BuildMarcusInterrogation(
            RectTransform modals,
            Color color)
        {
            Slot(modals, "Marcus Interrogation Dim Slot",
                "interrogation.dim",
                Vector2.zero, Vector2.one, Vector2.zero, color);
            Slot(modals, "Marcus Interrogation Panel Slot",
                "interrogation.panel",
                new Vector2(.15f, .08f), new Vector2(.85f, .92f),
                Vector2.zero, color);
            Slot(modals, "Marcus Interrogation Title Slot",
                "interrogation.title",
                new Vector2(.20f, .82f), new Vector2(.80f, .90f),
                Vector2.zero, color);
            Slot(modals, "Marcus Interrogation Guidance Slot",
                "interrogation.guidance",
                new Vector2(.20f, .75f), new Vector2(.80f, .82f),
                Vector2.zero, color);
            Slot(modals, "Marcus Interrogation State Slot",
                "interrogation.state",
                new Vector2(.20f, .68f), new Vector2(.80f, .75f),
                Vector2.zero, color);

            const int columns = 2;
            const int rows = 4;
            const float left = .19f;
            const float right = .81f;
            const float bottom = .31f;
            const float top = .68f;
            const float horizontalGap = .018f;
            const float verticalGap = .014f;
            float cellWidth =
                (right - left - horizontalGap) / columns;
            float cellHeight =
                (top - bottom - verticalGap * (rows - 1)) / rows;
            for (int index = 0; index < 8; index++)
            {
                int column = index % columns;
                int row = index / columns;
                float xMin =
                    left + column * (cellWidth + horizontalGap);
                float yMax =
                    top - row * (cellHeight + verticalGap);
                Slot(modals, $"Marcus Question {index + 1} Slot",
                    $"interrogation.question.{index + 1}",
                    new Vector2(xMin, yMax - cellHeight),
                    new Vector2(xMin + cellWidth, yMax),
                    Vector2.zero, color);
            }

            Slot(modals, "Marcus Interrogation Feedback Slot",
                "interrogation.feedback",
                new Vector2(.20f, .23f), new Vector2(.80f, .30f),
                Vector2.zero, color);
            Slot(modals, "Marcus Interrogation Back Slot",
                "interrogation.back",
                new Vector2(.17f, .11f), new Vector2(.31f, .19f),
                Vector2.zero, color);
            Slot(modals, "Marcus Interrogation Yes Slot",
                "interrogation.answer.yes",
                new Vector2(.38f, .11f), new Vector2(.49f, .19f),
                Vector2.zero, color);
            Slot(modals, "Marcus Interrogation No Slot",
                "interrogation.answer.no",
                new Vector2(.51f, .11f), new Vector2(.62f, .19f),
                Vector2.zero, color);
            Slot(modals, "Marcus Interrogation Submit Slot",
                "interrogation.submit",
                new Vector2(.69f, .11f), new Vector2(.83f, .19f),
                Vector2.zero, color);
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
            GameObjectUtility.RemoveMonoBehavioursWithMissingScript(
                rect.gameObject);
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

        private static void RemoveMissingScripts(Transform root)
        {
            foreach (Transform child in root.GetComponentsInChildren<Transform>(
                         true))
            {
                GameObjectUtility.RemoveMonoBehavioursWithMissingScript(
                    child.gameObject);
            }
        }

        private static void RemoveObsoletePlaceholder(
            Transform root,
            string path)
        {
            Transform obsolete = root.Find(path);
            if (obsolete != null)
                Object.DestroyImmediate(obsolete.gameObject);
        }
    }
}
