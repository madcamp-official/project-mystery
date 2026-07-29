using System.Linq;
using System.Reflection;
using NUnit.Framework;
using TMPro;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
using Wake.Narrative;
using Wake.UI;

namespace Wake.Tests.EditMode
{
    public sealed class CharacterRelationshipProfileCatalogTests
    {
        [Test]
        public void MainPortraitCharacters_HaveCompleteRelationshipProfiles()
        {
            DialoguePortraitDefinition[] mainCharacters =
                DialoguePortraitCatalog.All
                    .Where(entry => entry.UsesExpressionSprites)
                    .ToArray();

            Assert.That(mainCharacters, Has.Length.EqualTo(9));
            Assert.That(
                CharacterRelationshipProfileCatalog.All.Count,
                Is.EqualTo(mainCharacters.Length));

            foreach (DialoguePortraitDefinition character in mainCharacters)
            {
                Assert.That(
                    CharacterRelationshipProfileCatalog.TryGet(
                        character.CharacterId,
                        out CharacterRelationshipProfile profile),
                    Is.True,
                    $"{character.CharacterId} 인물 프로필이 없습니다.");
                Assert.That(profile.Role, Is.Not.Empty);
                Assert.That(profile.Affiliation, Is.Not.Empty);
                Assert.That(profile.Summary, Is.Not.Empty);
                Assert.That(profile.KnownNote, Is.Not.Empty);
            }
        }

        [Test]
        public void Catalog_UsesUniqueCharacterIds()
        {
            string[] ids = CharacterRelationshipProfileCatalog.All
                .Select(profile => profile.CharacterId)
                .ToArray();

            Assert.That(
                ids.Distinct(System.StringComparer.OrdinalIgnoreCase).Count(),
                Is.EqualTo(ids.Length));
        }

        [Test]
        public void NotebookCards_OpenProfileDetailAndRestoreList()
        {
            const string scenePath =
                "Assets/_Project/Scenes/UI/UI Basic Scene.unity";
            Scene scene = SceneManager.GetSceneByPath(scenePath);
            bool openedByTest = !scene.isLoaded;
            if (openedByTest)
            {
                scene = EditorSceneManager.OpenScene(
                    scenePath,
                    OpenSceneMode.Additive);
            }

            GameObject duplicate = null;
            try
            {
                Transform canvas = scene.GetRootGameObjects()
                    .SelectMany(root =>
                        root.GetComponentsInChildren<Transform>(true))
                    .First(candidate => candidate.name == "Canvas");
                Transform evidence = canvas.Find("Evidence");
                Assert.That(evidence, Is.Not.Null);

                duplicate = Object.Instantiate(
                    evidence.gameObject,
                    evidence.parent);
                duplicate.name = "Evidence Relationship Test";
                foreach (EvidenceNotebookTabsController existing in
                         duplicate.GetComponents<EvidenceNotebookTabsController>())
                {
                    Object.DestroyImmediate(existing);
                }

                EvidenceNotebookTabsController controller =
                    duplicate.AddComponent<EvidenceNotebookTabsController>();
                Assert.That(controller, Is.Not.Null);
                InvokePrivate(controller, "Build");
                InvokePrivate(controller, "ShowCharacters");

                Transform card = duplicate.transform.Find(
                    "Characters And Relationships/Viewport/Content/ADRIAN");
                Assert.That(
                    card,
                    Is.Not.Null,
                    "생성된 경로:\n" +
                    string.Join(
                        "\n",
                        duplicate
                            .GetComponentsInChildren<Transform>(true)
                            .Select(item =>
                                AnimationUtility.CalculateTransformPath(
                                    item,
                                    duplicate.transform))));
                Button cardButton = card.GetComponent<Button>();
                Assert.That(cardButton, Is.Not.Null);
                Assert.That(cardButton.interactable, Is.True);
                cardButton.onClick.Invoke();

                Transform detail = duplicate.transform.Find(
                    "Characters And Relationships/Character Detail");
                Assert.That(detail, Is.Not.Null);
                Assert.That(detail.gameObject.activeSelf, Is.True);
                Assert.That(
                    detail.Find("Name").GetComponent<TMP_Text>().text,
                    Is.EqualTo("아드리안 베일"));
                Assert.That(
                    detail.Find("Role").GetComponent<TMP_Text>().text,
                    Does.Contain("사립 탐정"));

                Button listButton = detail
                    .Find("Back To Character List")
                    .GetComponent<Button>();
                listButton.onClick.Invoke();
                Assert.That(detail.gameObject.activeSelf, Is.False);
                Assert.That(
                    duplicate.transform
                        .Find("Characters And Relationships/Viewport")
                        .gameObject.activeSelf,
                    Is.True);

                Transform back = duplicate.transform.Find("Back Btn");
                Assert.That(
                    back.GetSiblingIndex(),
                    Is.EqualTo(back.parent.childCount - 1));
            }
            finally
            {
                if (duplicate != null)
                {
                    Object.DestroyImmediate(duplicate);
                }
                if (openedByTest)
                {
                    EditorSceneManager.CloseScene(scene, true);
                }
            }
        }

        private static void InvokePrivate(
            EvidenceNotebookTabsController controller,
            string methodName)
        {
            MethodInfo method = typeof(EvidenceNotebookTabsController)
                .GetMethod(
                    methodName,
                    BindingFlags.Instance | BindingFlags.NonPublic);
            Assert.That(method, Is.Not.Null);
            method.Invoke(controller, null);
        }
    }
}
