using System.Collections.Generic;
using System.Linq;
using NUnit.Framework;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using Wake.UI;

namespace Wake.Tests
{
    public sealed class RuntimeUiLayoutSceneIntegrityTests
    {
        private const string ScenePath =
            "Assets/_Project/Scenes/UI/UI Basic Scene.unity";

        [Test]
        public void UiScene_ContainsValidDialogueLayoutSlots()
        {
            Scene scene = SceneManager.GetSceneByPath(ScenePath);
            bool openedByTest = !scene.isLoaded;
            if (openedByTest)
            {
                scene = EditorSceneManager.OpenScene(
                    ScenePath,
                    OpenSceneMode.Additive);
            }

            try
            {
                Transform layout = scene.GetRootGameObjects()
                    .SelectMany(root =>
                        root.GetComponentsInChildren<Transform>(true))
                    .FirstOrDefault(candidate =>
                        candidate.name == "Runtime UI Layout");
                Assert.That(layout, Is.Not.Null);
                Transform shellSlots =
                    layout.Find("Screen Shell Slots");
                Assert.That(shellSlots, Is.Not.Null);
                ScreenRegionSet regionSet =
                    shellSlots.GetComponent<ScreenRegionSet>();
                Assert.That(regionSet, Is.Not.Null);
                Assert.That(regionSet.IsComplete, Is.True);

                int missingScripts = layout
                    .GetComponentsInChildren<Transform>(true)
                    .Sum(candidate =>
                        GameObjectUtility
                            .GetMonoBehavioursWithMissingScriptCount(
                                candidate.gameObject));
                Assert.That(
                    missingScripts,
                    Is.Zero,
                    "Runtime UI placeholders must not contain missing scripts.");

                HashSet<string> slotIds = layout
                    .GetComponentsInChildren<RuntimeUiLayoutSlot>(true)
                    .Select(slot => slot.SlotId)
                    .ToHashSet();
                string[] required =
                {
                    "screen.context.topLeft",
                    "screen.objective.top",
                    "screen.global.topRight",
                    "screen.tools.bottomLeft",
                    "screen.reading.bottom",
                    "screen.primary.bottomRight",
                    "screen.content.center",
                    "dialogue.focus-panel-left",
                    "dialogue.focus-panel-right",
                    "dialogue.focus-portrait-left",
                    "dialogue.focus-portrait-right",
                    "dialogue.narration-panel"
                };
                Assert.That(slotIds, Is.SupersetOf(required));
                Assert.That(
                    layout.GetComponentsInChildren<RuntimeUiLayoutSlot>(true)
                        .GroupBy(slot => slot.SlotId)
                        .Where(group => group.Count() > 1)
                        .Select(group => group.Key),
                    Is.Empty,
                    "Runtime UI slot IDs must be unique.");
            }
            finally
            {
                if (openedByTest)
                    EditorSceneManager.CloseScene(scene, true);
            }
        }

        [Test]
        public void UiScene_ContainsInspectorAuthoredMapAndEvidenceLayouts()
        {
            Scene scene = SceneManager.GetSceneByPath(ScenePath);
            bool openedByTest = !scene.isLoaded;
            if (openedByTest)
            {
                scene = EditorSceneManager.OpenScene(
                    ScenePath,
                    OpenSceneMode.Additive);
            }

            try
            {
                Transform canvas = scene.GetRootGameObjects()
                    .SelectMany(root =>
                        root.GetComponentsInChildren<Transform>(true))
                    .FirstOrDefault(candidate =>
                        candidate.name == "Canvas");
                Assert.That(canvas, Is.Not.Null);

                Transform mapContent = canvas.Find(
                    "Map/Rooms/Dynamic Location Viewport/" +
                    "Dynamic Location Content");
                Assert.That(mapContent, Is.Not.Null);
                Transform[] mapNodes = mapContent
                    .Cast<Transform>()
                    .Where(child =>
                        child.name.StartsWith("Map Node "))
                    .ToArray();
                Assert.That(
                    mapNodes,
                    Has.Length.EqualTo(25),
                    "The project has 25 authored locations.");
                Assert.That(
                    mapNodes.All(node =>
                        node.GetComponent<RuntimeUiLayoutSlot>() != null),
                    Is.True);

                RuntimeUiLayoutSlot[] evidenceSlots = canvas
                    .Find("Evidence")
                    .GetComponentsInChildren<RuntimeUiLayoutSlot>(true);
                HashSet<string> evidenceSlotIds = evidenceSlots
                    .Select(slot => slot.SlotId)
                    .ToHashSet();
                string[] requiredEvidenceSlots =
                {
                    "evidence.panel",
                    "evidence.detail-image",
                    "evidence.title",
                    "evidence.description",
                    "evidence.carousel",
                    "evidence.previous",
                    "evidence.next",
                    "evidence.back",
                    "evidence.theory-board"
                };
                Assert.That(
                    evidenceSlotIds,
                    Is.SupersetOf(requiredEvidenceSlots));
            }
            finally
            {
                if (openedByTest)
                    EditorSceneManager.CloseScene(scene, true);
            }
        }
    }
}
