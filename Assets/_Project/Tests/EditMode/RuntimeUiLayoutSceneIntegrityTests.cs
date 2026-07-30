using System.Collections.Generic;
using System.Linq;
using NUnit.Framework;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
using TMPro;
using Wake.Narrative;
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
                    "save.title",
                    "save.cards",
                    "save.back",
                    "shell.puzzle.panel",
                    "shell.puzzle.finalAccusation",
                    "shell.ending.background",
                    "shell.ending.logo",
                    "shell.ending.route",
                    "shell.ending.title",
                    "shell.ending.epilogue",
                    "shell.ending.reason",
                    "shell.ending.primary",
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
                    "The scene keeps 25 legacy inspector nodes; five separated " +
                    "story locations are added by MapController for compatibility.");
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
                    "evidence.acquisition-place",
                    "evidence.related-people",
                    "evidence.reliability",
                    "evidence.description-viewport",
                    "evidence.description",
                    "evidence.carousel",
                    "evidence.previous-record",
                    "evidence.next-record",
                    "evidence.compare",
                    "evidence.view-previous",
                    "evidence.view-next",
                    "evidence.back",
                };
                Assert.That(
                    evidenceSlotIds,
                    Is.SupersetOf(requiredEvidenceSlots));

                Transform evidence = canvas.Find("Evidence");
                Transform descriptionViewport =
                    evidence.Find("Description Viewport");
                Transform description =
                    descriptionViewport?.Find("Description");
                Assert.That(descriptionViewport, Is.Not.Null);
                Assert.That(description, Is.Not.Null);
                Assert.That(
                    descriptionViewport.GetComponent<RectMask2D>(),
                    Is.Not.Null);
                ScrollRect descriptionScroll =
                    descriptionViewport.GetComponent<ScrollRect>();
                Assert.That(descriptionScroll, Is.Not.Null);
                Assert.That(descriptionScroll.horizontal, Is.False);
                Assert.That(descriptionScroll.vertical, Is.True);
                Assert.That(
                    descriptionScroll.viewport,
                    Is.SameAs(descriptionViewport));
                Assert.That(
                    descriptionScroll.content,
                    Is.SameAs(description));
                Assert.That(
                    description.GetComponent<ContentSizeFitter>(),
                    Is.Not.Null);

                string[] metadataNames =
                {
                    "Acquisition Place",
                    "Related People",
                    "Reliability"
                };
                Assert.That(
                    metadataNames.All(name =>
                    {
                        Transform label = evidence.Find(name);
                        return label != null &&
                               label.GetComponent<TMP_Text>() != null &&
                               label.GetComponent<RuntimeUiLayoutSlot>() != null;
                    }),
                    Is.True,
                    "Evidence metadata must be visible and authored.");

                RuntimeUiLayoutSlot[] canvasSlots = canvas
                    .GetComponentsInChildren<RuntimeUiLayoutSlot>(true);
                HashSet<string> canvasSlotIds = canvasSlots
                    .Select(slot => slot.SlotId)
                    .ToHashSet();
                Assert.That(
                    canvasSlotIds,
                    Is.SupersetOf(new[]
                    {
                        "evidence.tabs",
                        "evidence.people-panel"
                    }),
                    "Runtime notebook overlays must remain Inspector-authored.");

                RuntimeUiLayoutSlot peopleSlot = canvasSlots
                    .Single(slot =>
                        slot.SlotId == "evidence.people-panel");
                RuntimeUiLayoutSlot backSlot = canvasSlots
                    .Single(slot =>
                        slot.SlotId == "evidence.back");
                RectTransform peopleRect =
                    peopleSlot.transform as RectTransform;
                RectTransform backRect =
                    backSlot.transform as RectTransform;
                Assert.That(peopleRect, Is.Not.Null);
                Assert.That(backRect, Is.Not.Null);
                bool overlaps =
                    peopleRect.anchorMin.x < backRect.anchorMax.x &&
                    peopleRect.anchorMax.x > backRect.anchorMin.x &&
                    peopleRect.anchorMin.y < backRect.anchorMax.y &&
                    peopleRect.anchorMax.y > backRect.anchorMin.y;
                Assert.That(
                    overlaps,
                    Is.False,
                    "The authored character panel must leave the evidence " +
                    "back-button footer unobstructed.");
            }
            finally
            {
                if (openedByTest)
                    EditorSceneManager.CloseScene(scene, true);
            }
        }

        [Test]
        public void UiScene_DialogueSpeakerNameUsesContainedAuthoredLayout()
        {
            Scene scene = SceneManager.GetSceneByPath(ScenePath);
            bool openedByTest = !scene.isLoaded;
            if (openedByTest)
            {
                scene = EditorSceneManager.OpenScene(
                    ScenePath,
                    OpenSceneMode.Additive);
            }

            GameObject measurementCanvas = null;
            try
            {
                Transform canvas = scene.GetRootGameObjects()
                    .SelectMany(root =>
                        root.GetComponentsInChildren<Transform>(true))
                    .FirstOrDefault(candidate =>
                        candidate.name == "Canvas");
                Assert.That(canvas, Is.Not.Null);

                RectTransform speakerPlate = canvas.Find(
                    "Ingame/Line Panel/Image") as RectTransform;
                Assert.That(
                    speakerPlate,
                    Is.Not.Null,
                    "대사창 화자 이름표 배경을 찾을 수 없습니다.");

                TMP_Text speakerText = speakerPlate
                    .Find("Text (TMP)")
                    ?.GetComponent<TMP_Text>();
                Assert.That(
                    speakerText,
                    Is.Not.Null,
                    "화자 이름표의 TextMeshProUGUI를 찾을 수 없습니다.");

                RectTransform textRect = speakerText.rectTransform;
                Assert.That(textRect.anchorMin, Is.EqualTo(Vector2.zero));
                Assert.That(textRect.anchorMax, Is.EqualTo(Vector2.one));
                Assert.That(
                    textRect.offsetMin.x,
                    Is.EqualTo(32f).Within(0.01f),
                    "이름표 왼쪽에 Inspector-authored padding이 필요합니다.");
                Assert.That(
                    textRect.offsetMin.y,
                    Is.EqualTo(2f).Within(0.01f),
                    "이름표 아래쪽에 Inspector-authored padding이 필요합니다.");
                Assert.That(
                    textRect.offsetMax.x,
                    Is.EqualTo(-32f).Within(0.01f),
                    "이름표 오른쪽에 Inspector-authored padding이 필요합니다.");
                Assert.That(
                    textRect.offsetMax.y,
                    Is.EqualTo(-2f).Within(0.01f),
                    "이름표 위쪽에 Inspector-authored padding이 필요합니다.");
                Assert.That(textRect.rect.width, Is.GreaterThan(0f));
                Assert.That(textRect.rect.height, Is.GreaterThan(0f));

                Assert.That(speakerText.enableAutoSizing, Is.True);
                Assert.That(
                    speakerText.fontSizeMin,
                    Is.EqualTo(36f).Within(0.01f));
                Assert.That(
                    speakerText.fontSizeMax,
                    Is.EqualTo(46f).Within(0.01f));
                Assert.That(
                    speakerText.textWrappingMode,
                    Is.EqualTo(TextWrappingModes.NoWrap));
                Assert.That(
                    speakerText.overflowMode,
                    Is.EqualTo(TextOverflowModes.Ellipsis));
                Assert.That(
                    speakerText.horizontalAlignment,
                    Is.EqualTo(HorizontalAlignmentOptions.Center));
                Assert.That(
                    speakerText.verticalAlignment,
                    Is.EqualTo(VerticalAlignmentOptions.Middle));
                Assert.That(
                    speakerText.raycastTarget,
                    Is.False,
                    "장식용 이름 텍스트가 대화 입력을 가로채면 안 됩니다.");

                measurementCanvas = BuildSpeakerNameMeasurementCanvas(
                    speakerPlate,
                    out TMP_Text measurementText);
                string[] labels = DialoguePortraitCatalog.All
                    .Select(definition => definition.DisplayName)
                    .Concat(new[]
                    {
                        GetProductionSpeakerLabel("ADRIAN_독백"),
                        GetProductionSpeakerLabel("EVELYN_RECORD"),
                        GetProductionSpeakerLabel("DANIEL_CHAT"),
                        GetProductionSpeakerLabel("JULIAN_RECORD")
                    })
                    .Where(label =>
                        !string.IsNullOrWhiteSpace(label))
                    .Distinct()
                    .ToArray();

                Assert.That(labels, Is.Not.Empty);
                foreach (string label in labels)
                {
                    measurementText.text = label;
                    RectTransform measurementPlate =
                        measurementText.rectTransform.parent as RectTransform;
                    Assert.That(measurementPlate, Is.Not.Null);
                    measurementPlate.sizeDelta = new Vector2(480f, 64.8f);
                    Canvas.ForceUpdateCanvases();
                    measurementText.ForceMeshUpdate(
                        ignoreActiveState: true,
                        forceTextReparsing: true);

                    Assert.That(
                        measurementText.textInfo.lineCount,
                        Is.LessThanOrEqualTo(1),
                        $"화자명 '{label}'은 한 줄 이름표 안에 표시되어야 합니다.");
                    Assert.That(
                        measurementText.isTextOverflowing,
                        Is.False,
                        $"화자명 '{label}'이 Inspector-authored 이름표를 벗어납니다.");
                    Assert.That(
                        measurementText.firstOverflowCharacterIndex,
                        Is.EqualTo(-1),
                        $"화자명 '{label}'은 첫 글자부터 잘려서는 안 됩니다.");
                    Assert.That(
                        measurementText.textInfo.characterCount,
                        Is.EqualTo(label.Length),
                        $"화자명 '{label}'의 모든 글자가 레이아웃에 포함되어야 합니다.");
                    Assert.That(
                        measurementText.textInfo.characterInfo
                            .Take(measurementText.textInfo.characterCount)
                            .Count(character => character.isVisible),
                        Is.EqualTo(label.Count(character =>
                            !char.IsWhiteSpace(character))),
                        $"화자명 '{label}'의 실제 글리프가 모두 렌더링되어야 합니다.");
                    Assert.That(
                        measurementText.fontSize,
                        Is.GreaterThanOrEqualTo(
                            measurementText.fontSizeMin - 0.01f),
                        $"화자명 '{label}' 표시를 위해 최소 글자 크기보다 " +
                        "작아져서는 안 됩니다.");
                }
            }
            finally
            {
                if (measurementCanvas != null)
                    Object.DestroyImmediate(measurementCanvas);

                if (openedByTest)
                    EditorSceneManager.CloseScene(scene, true);
            }
        }

        private static GameObject BuildSpeakerNameMeasurementCanvas(
            RectTransform authoredPlate,
            out TMP_Text measurementText)
        {
            GameObject canvasObject = new(
                "Speaker Name Layout Measurement",
                typeof(RectTransform),
                typeof(Canvas));
            RectTransform canvasRect =
                canvasObject.GetComponent<RectTransform>();
            canvasRect.sizeDelta = new Vector2(1920f, 1080f);

            GameObject plateObject = Object.Instantiate(
                authoredPlate.gameObject,
                canvasRect,
                false);
            plateObject.name = "Speaker Name Plate";
            plateObject.SetActive(true);

            RectTransform plateRect =
                plateObject.GetComponent<RectTransform>();
            plateRect.anchorMin = new Vector2(0.5f, 0.5f);
            plateRect.anchorMax = new Vector2(0.5f, 0.5f);
            plateRect.pivot = new Vector2(0.5f, 0.5f);
            plateRect.anchoredPosition = Vector2.zero;
            plateRect.sizeDelta = authoredPlate.rect.size;

            measurementText = plateRect
                .Find("Text (TMP)")
                .GetComponent<TMP_Text>();
            measurementText.gameObject.SetActive(true);
            Canvas.ForceUpdateCanvases();
            return canvasObject;
        }

        private static string GetProductionSpeakerLabel(string sourceSpeaker)
        {
            DialogueSpeakerIdentity identity =
                DialoguePresentationMap.GetSpeaker(sourceSpeaker);
            return DialoguePresentationMap.GetSpeakerLabel(
                sourceSpeaker,
                identity);
        }
    }
}
