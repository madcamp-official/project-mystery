using System.Collections.Generic;
using UnityEngine;
using Wake.Core;
using Wake.Exploration;
using Wake.Narrative;

namespace Wake.UI
{
    [DisallowMultipleComponent]
    public sealed class ChapterTransitionPresenter : MonoBehaviour
    {
        private static readonly IReadOnlyDictionary<int, string> DaySummaries =
            new Dictionary<int, string>
            {
                [1] = "첫 만찬과 공식 브리핑이 시작됩니다.",
                [2] = "밀실의 출구 흔적과 기록을 다시 추적합니다.",
                [3] = "승객들의 증언과 사라진 기록을 대조합니다.",
                [4] = "감시망의 빈틈과 이동 경로를 좁혀 갑니다.",
                [5] = "숨겨진 관계와 조작된 증언을 확인합니다.",
                [6] = "기관 구역에서 사건의 실제 흔적을 찾습니다.",
                [7] = "남은 기록을 복원해 마지막 모순을 정리합니다.",
                [8] = "확보한 증거로 최종 지목을 준비합니다."
            };

        private bool transitionPending;

        private void OnEnable()
        {
            InvestigationEventHub.Published += HandleInvestigationEvent;
        }

        private void OnDisable()
        {
            InvestigationEventHub.Published -= HandleInvestigationEvent;
            transitionPending = false;
        }

        private void HandleInvestigationEvent(
            InvestigationEvent investigationEvent)
        {
            if (transitionPending)
                return;

            if (investigationEvent.Kind !=
                    InvestigationEventKind.SceneCompleted ||
                !ProductionDayBoundaryCatalog.TryGet(
                    investigationEvent.SubjectId,
                    out ProductionDayBoundary boundary) ||
                !string.Equals(
                    boundary.NextSceneId,
                    investigationEvent.ContextId,
                    System.StringComparison.OrdinalIgnoreCase) ||
                !ProductionSceneCatalog.TryGet(
                    boundary.CompletedSceneId,
                    out ProductionSceneDefinition completed) ||
                !ProductionSceneCatalog.TryGet(
                    boundary.NextSceneId,
                    out ProductionSceneDefinition next))
            {
                return;
            }

            ChapterTransitionDefinition transition =
                CreateDefinition(completed, next);
            if (transition.AutoSave)
                GameStateManager.Instance?.SaveCurrentState();
            transitionPending = true;
            UIManager.Instance?.ShowChapterTransition(
                $"DAY {transition.Day} · {transition.TimeLabel}",
                transition.Title,
                transition.Summary,
                () => ContinueToNextScene(transition));
        }

        private void ContinueToNextScene(
            ChapterTransitionDefinition transition)
        {
            transitionPending = false;
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

        private static ChapterTransitionDefinition CreateDefinition(
            ProductionSceneDefinition completed,
            ProductionSceneDefinition next)
        {
            string location = CanonicalLocationCatalog
                .FindSpec(next.NarrativeLocationCode)
                ?.DisplayName;
            if (string.IsNullOrWhiteSpace(location))
                location = "다음 조사 장소";

            string daySummary = DaySummaries.TryGetValue(
                next.Day,
                out string summary)
                ? summary
                : "새로운 단서를 따라 수사를 이어갑니다.";
            return new ChapterTransitionDefinition(
                $"{completed.SceneId}_TO_{next.SceneId}",
                completed.SceneId,
                next.SceneId,
                next.Day,
                next.TimeLabel,
                $"{next.Day}일 차",
                $"{location}. {daySummary}",
                true);
        }

        private readonly struct ChapterTransitionDefinition
        {
            public ChapterTransitionDefinition(
                string id,
                string previousSceneId,
                string nextSceneId,
                int day,
                string timeLabel,
                string title,
                string summary,
                bool autoSave)
            {
                Id = id;
                PreviousSceneId = previousSceneId;
                NextSceneId = nextSceneId;
                Day = day;
                TimeLabel = timeLabel;
                Title = title;
                Summary = summary;
                AutoSave = autoSave;
            }

            public string Id { get; }
            public string PreviousSceneId { get; }
            public string NextSceneId { get; }
            public int Day { get; }
            public string TimeLabel { get; }
            public string Title { get; }
            public string Summary { get; }
            public bool AutoSave { get; }
        }
    }
}
