using UnityEngine;
using Wake.Core;
using Wake.Exploration;
using Wake.Narrative;

namespace Wake.UI
{
    [DisallowMultipleComponent]
    public sealed class ChapterTransitionPresenter : MonoBehaviour
    {
        private bool transitionPending;

        private void OnEnable()
        {
            InvestigationEventHub.Published += HandleInvestigationEvent;
        }

        private void OnDisable()
        {
            InvestigationEventHub.Published -= HandleInvestigationEvent;
            if (transitionPending)
                AudioManager.Instance?.StopTransitionAudio();
            transitionPending = false;
        }

        private void HandleInvestigationEvent(
            InvestigationEvent investigationEvent)
        {
            if (transitionPending ||
                investigationEvent.Kind !=
                    InvestigationEventKind.SceneCompleted ||
                !ProductionChapterTransitionCatalog.TryGet(
                    investigationEvent.SubjectId,
                    out ChapterTransitionRequest transition) ||
                !string.Equals(
                    transition.NextSceneId,
                    investigationEvent.ContextId,
                    System.StringComparison.OrdinalIgnoreCase) ||
                !ProductionSceneCatalog.TryGet(
                    transition.NextSceneId,
                    out ProductionSceneDefinition next))
            {
                return;
            }

            GameStateManager.Instance?.SaveCurrentState();
            transitionPending = true;
            AudioManager.Instance?.BeginChapterTransitionAudio(
                transition.MusicKey,
                transition.StingerKey,
                transition.IsDeparture);
            UIManager.Instance?.ShowChapterTransition(
                transition,
                () => ContinueToNextScene(transition, next));
        }

        private void ContinueToNextScene(
            ChapterTransitionRequest transition,
            ProductionSceneDefinition next)
        {
            transitionPending = false;
            string nextLocation =
                CanonicalLocationCatalog.FindSpec(
                    next.NarrativeLocationCode)?.Code ??
                next.NarrativeLocationCode;
            AudioManager.Instance?.EndChapterTransitionAudio(nextLocation);

            MapController map = FindFirstObjectByType<MapController>();
            SceneTravelResult travel = map != null
                ? map.TryTravelToScene(transition.NextSceneId)
                : default;
            if (travel.IsAllowed)
                return;

            LocationLoader.Instance?.PrepareNarrativeScene(
                transition.NextSceneId);
            UIManager.Instance?.ShowIngame();
        }
    }
}
