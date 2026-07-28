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
                    "dialogue.focus-panel-left",
                    "dialogue.focus-panel-right",
                    "dialogue.focus-portrait-left",
                    "dialogue.focus-portrait-right",
                    "dialogue.narration-panel"
                };
                Assert.That(slotIds, Is.SupersetOf(required));
            }
            finally
            {
                if (openedByTest)
                    EditorSceneManager.CloseScene(scene, true);
            }
        }
    }
}
