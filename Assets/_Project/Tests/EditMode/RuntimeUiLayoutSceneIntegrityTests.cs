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
                    "dialogue.narration-panel",
                    "dialogue.dim",
                    "dialogue.focus-text-left",
                    "dialogue.focus-text-right",
                    "dialogue.compact-text",
                    "dialogue.narration-text",
                    "dialogue.speaker-name-left",
                    "dialogue.speaker-name-right",
                    "dialogue.advance-left",
                    "dialogue.advance-right",
                    "dialogue.advance-center",
                    "dialogue.choices-left",
                    "dialogue.choices-right",
                    "dialogue.investigation",
                    "dialogue.investigation.frame",
                    "dialogue.investigation.section",
                    "dialogue.investigation.title",
                    "dialogue.investigation.body",
                    "dialogue.investigation.action",
                    "interrogation.dim",
                    "interrogation.panel",
                    "interrogation.title",
                    "interrogation.guidance",
                    "interrogation.state",
                    "interrogation.question.1",
                    "interrogation.question.2",
                    "interrogation.question.3",
                    "interrogation.question.4",
                    "interrogation.question.5",
                    "interrogation.question.6",
                    "interrogation.question.7",
                    "interrogation.question.8",
                    "interrogation.feedback",
                    "interrogation.back",
                    "interrogation.answer.yes",
                    "interrogation.answer.no",
                    "interrogation.submit"
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
