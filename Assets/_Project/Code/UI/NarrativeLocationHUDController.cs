using UnityEngine;
using Wake.Core;
using Wake.Exploration;
using Wake.Narrative;

namespace Wake.UI
{
    [DisallowMultipleComponent]
    public sealed class NarrativeLocationHUDController : MonoBehaviour
    {
        public NarrativeLocationContext CurrentContext { get; private set; }
        public NarrativeLocationHUDViewModel CurrentPresentation
        {
            get;
            private set;
        }
        public bool IsWarningVisible => false;

        private void OnEnable()
        {
            RemoveLegacyBanner();
            InvestigationEventHub.Published += HandleInvestigationEvent;
            RefreshFromRuntime();
        }

        private void OnDisable()
        {
            InvestigationEventHub.Published -= HandleInvestigationEvent;
        }

        public void ShowScene(string sceneId)
        {
            CurrentContext =
                NarrativeLocationContextResolver.Resolve(sceneId);
            CurrentPresentation =
                NarrativeLocationHUDPresentation.Create(CurrentContext);
            RemoveLegacyBanner();
        }

        public void Clear()
        {
            CurrentContext = default;
            CurrentPresentation = default;
            RemoveLegacyBanner();
        }

        private void HandleInvestigationEvent(
            InvestigationEvent investigationEvent)
        {
            if (investigationEvent.Kind == InvestigationEventKind.SceneEntered)
            {
                ShowScene(investigationEvent.SubjectId);
            }
        }

        private void RefreshFromRuntime()
        {
            string sceneId = DialogueController.Instance?
                .ActiveProductionSceneId;
            if (string.IsNullOrEmpty(sceneId))
            {
                sceneId = GameStateManager.Instance?
                    .DialogueCheckpoint?
                    .activeSceneId;
            }

            ShowScene(sceneId);
        }

        private void RemoveLegacyBanner()
        {
            Transform legacy =
                transform.Find("Narrative Location Context");
            if (legacy == null)
                return;

            legacy.gameObject.SetActive(false);
            Destroy(legacy.gameObject);
        }
    }
}
