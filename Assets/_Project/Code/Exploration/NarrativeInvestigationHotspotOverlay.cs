using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using Wake.Narrative;
using Wake.UI;

namespace Wake.Exploration
{
    /// <summary>
    /// Makes story-specific documents and terminals painted into a location
    /// background directly selectable. The authored polygon is the hit area,
    /// so nearby characters do not turn the whole rectangular sprite into a
    /// competing click target.
    /// </summary>
    [DisallowMultipleComponent]
    public sealed class NarrativeInvestigationHotspotOverlay : MonoBehaviour
    {
        private readonly List<GameObject> spawned = new();
        private RectTransform contentRect;

        public void Initialize(RectTransform backgroundContentRect)
        {
            contentRect = backgroundContentRect;
        }

        public void Show(
            string locationCode,
            string activeSceneId,
            LocationBackgroundSelection backgroundSelection)
        {
            Clear();
            if (contentRect == null ||
                string.IsNullOrWhiteSpace(activeSceneId))
            {
                return;
            }

            foreach (NarrativeInvestigationDefinition target in
                     NarrativeInvestigationCatalog.GetForLocation(
                         locationCode,
                         activeSceneId))
            {
                if (!BackgroundInteractionShapeCatalog.TryGet(
                        target.TargetId,
                        locationCode,
                        backgroundSelection,
                        out BackgroundInteractionShape shape) ||
                    !shape.IsPresent)
                {
                    continue;
                }

                CreateButton(target, shape);
            }
        }

        private void CreateButton(
            NarrativeInvestigationDefinition definition,
            BackgroundInteractionShape shape)
        {
            GameObject target = new(
                $"NarrativeInvestigationHotspot_{definition.TargetId}",
                typeof(RectTransform),
                typeof(CanvasRenderer),
                typeof(Image),
                typeof(Button));
            target.transform.SetParent(contentRect, false);

            RectTransform rect = target.GetComponent<RectTransform>();
            rect.anchorMin = shape.NormalizedBounds.min;
            rect.anchorMax = shape.NormalizedBounds.max;
            rect.offsetMin = Vector2.zero;
            rect.offsetMax = Vector2.zero;

            Image image = target.GetComponent<Image>();
            image.color = Color.clear;
            image.raycastTarget = true;

            Button button = target.GetComponent<Button>();
            button.targetGraphic = image;
            button.transition = Selectable.Transition.None;
            button.colors = AmbientInteractionPresentation.HotspotColors();
            button.onClick.AddListener(() => Interact(definition));

            target.AddComponent<PolygonHotspotRaycastFilter>()
                .Configure(shape.LocalPolygon);
            target.AddComponent<ExplorationHotspotFeedback>()
                .ConfigureExactShape(shape.LocalLabelAnchor);
            target.transform.SetAsLastSibling();
            spawned.Add(target);
        }

        private static void Interact(
            NarrativeInvestigationDefinition definition)
        {
            InvestigationScreenController controller =
                InvestigationScreenController.Instance;
            if (controller == null)
            {
                ToastController.Instance?.Show(
                    $"조사 화면을 열 수 없습니다: {definition.DisplayName}");
                return;
            }

            controller.BeginNarrative(
                definition.TargetId,
                () => DialogueController.Instance?.StartProductionScene(
                    definition.SceneId));
        }

        private void Clear()
        {
            foreach (GameObject target in spawned)
            {
                if (target == null)
                    continue;
                if (Application.isPlaying)
                    Destroy(target);
                else
                    DestroyImmediate(target);
            }
            spawned.Clear();
        }

        private void OnDisable() => Clear();
        private void OnDestroy() => Clear();
    }
}
