using System;
using System.Collections.Generic;
using System.Linq;
using Wake.Exploration;
using Wake.Puzzles;

namespace Wake.Core
{
    public enum InvestigationFeedbackSeverity
    {
        Information,
        Warning,
        Error
    }

    public readonly struct InvestigationFeedback
    {
        public InvestigationFeedback(
            string code,
            string title,
            string message,
            string actionLabel,
            InvestigationFeedbackSeverity severity,
            string diagnosticDetail = "")
        {
            Code = code ?? string.Empty;
            Title = title ?? string.Empty;
            Message = message ?? string.Empty;
            ActionLabel = actionLabel ?? string.Empty;
            Severity = severity;
            DiagnosticDetail = diagnosticDetail ?? string.Empty;
        }

        public string Code { get; }
        public string Title { get; }
        public string Message { get; }
        public string ActionLabel { get; }
        public InvestigationFeedbackSeverity Severity { get; }

        // 개발자용 원문은 플레이어 표시 문자열과 분리한다.
        public string DiagnosticDetail { get; }
        public bool HasAction => !string.IsNullOrWhiteSpace(ActionLabel);
    }

    public static class InvestigationFeedbackCatalog
    {
        public static InvestigationFeedback ForTravel(SceneTravelResult result)
        {
            if (result.IsAllowed)
            {
                string destination = result.Location != null
                    ? result.Location.DisplayName
                    : "목적지";
                return Create(
                    "travel_allowed",
                    "이동 가능",
                    $"{destination}(으)로 이동할 수 있습니다.",
                    "이동",
                    InvestigationFeedbackSeverity.Information,
                    result.Detail);
            }

            return result.DenialReason switch
            {
                SceneAccessDenialReason.SceneNotRegistered => Create(
                    "scene_not_registered",
                    "이동할 수 없음",
                    "아직 등록되지 않은 조사 장면입니다.",
                    "확인",
                    InvestigationFeedbackSeverity.Error,
                    result.Detail),
                SceneAccessDenialReason.PhysicalLocationUnresolved => Create(
                    "location_unresolved",
                    "목적지 확인 필요",
                    "이 장면에 사용할 장소가 아직 확정되지 않았습니다.",
                    "확인",
                    InvestigationFeedbackSeverity.Warning,
                    result.Detail),
                SceneAccessDenialReason.LocationUnused => Create(
                    "location_unused",
                    "사용하지 않는 장소",
                    "현재 이야기와 플레이 동선에서는 사용하지 않는 장소입니다.",
                    "확인",
                    InvestigationFeedbackSeverity.Information,
                    result.Detail),
                SceneAccessDenialReason.PrerequisiteSceneIncomplete => Create(
                    "prerequisite_incomplete",
                    "선행 조사 필요",
                    BuildPrerequisiteMessage(result),
                    "목표 보기",
                    InvestigationFeedbackSeverity.Warning,
                    result.Detail),
                SceneAccessDenialReason.BoardingSequenceIncomplete => Create(
                    "boarding_sequence_incomplete",
                    "아직 승선 중입니다",
                    "항구에서 승선 통로와 회장실을 차례로 거친 뒤 선내 자유 이동이 열립니다.",
                    "프롤로그 계속하기",
                    InvestigationFeedbackSeverity.Warning,
                    result.Detail),
                SceneAccessDenialReason.NarrativeWindowClosed => Create(
                    "narrative_window_closed",
                    "이미 통과한 경로입니다",
                    "승선 통로는 승선 절차 중에만 이용할 수 있습니다.",
                    "현재 목표 보기",
                    InvestigationFeedbackSeverity.Information,
                    result.Detail),
                SceneAccessDenialReason.RestrictedByPublicAnxiety => Create(
                    "restricted_by_anxiety",
                    "제한구역 폐쇄",
                    "승객 불안이 70 이상이라 승무원·기술 제한구역이 폐쇄되었습니다.",
                    "상태 보기",
                    InvestigationFeedbackSeverity.Warning,
                    result.Detail),
                SceneAccessDenialReason.DialogueUnavailable => Create(
                    "dialogue_unavailable",
                    "대화 시작 불가",
                    "진행 중인 대화를 마친 뒤 다시 시도해 주세요.",
                    "확인",
                    InvestigationFeedbackSeverity.Warning,
                    result.Detail),
                SceneAccessDenialReason.LocationLoadFailed => Create(
                    "location_load_failed",
                    "장소 표시 실패",
                    "장소 화면을 불러오지 못했습니다. 다시 시도해 주세요.",
                    "다시 시도",
                    InvestigationFeedbackSeverity.Error,
                    result.Detail),
                _ => Create(
                    "travel_denied",
                    "이동할 수 없음",
                    "현재 상태에서는 이 장소로 이동할 수 없습니다.",
                    "확인",
                    InvestigationFeedbackSeverity.Warning,
                    result.Detail)
            };
        }

        public static InvestigationFeedback ForPuzzle(
            ProductionPuzzleDefinition definition,
            PuzzleCompletionResult result)
        {
            if (result.Completed)
            {
                return Create(
                    "puzzle_completed",
                    "추론 완료",
                    "필수 단서와 선택이 모두 확인되었습니다.",
                    "계속",
                    InvestigationFeedbackSeverity.Information);
            }

            int missingSelections = result.MissingSelectionIds?.Count ?? 0;
            int missingEvidence = result.MissingEvidenceIds?.Count ?? 0;
            string message = BuildPuzzleMessage(missingSelections, missingEvidence);
            string diagnostic = BuildPuzzleDiagnostic(
                definition,
                result.MissingSelectionIds,
                result.MissingEvidenceIds);
            return Create(
                "puzzle_incomplete",
                "퍼즐을 완료할 수 없음",
                message,
                missingEvidence > 0 ? "증거 보기" : "다시 확인",
                InvestigationFeedbackSeverity.Warning,
                diagnostic);
        }

        public static InvestigationFeedback ForObjective(ObjectiveProgress progress)
        {
            if (progress == null)
            {
                return Create(
                    "objective_missing",
                    "조사 목표 없음",
                    "현재 표시할 조사 목표가 없습니다.",
                    string.Empty,
                    InvestigationFeedbackSeverity.Warning);
            }

            if (progress.IsCompleted)
            {
                return ForObjectiveCompleted(progress.Definition);
            }

            return Create(
                "objective_progress",
                "조사 목표",
                $"{progress.Definition.Title} " +
                $"({progress.CompletedRequirementCount}/{progress.RequirementCount})",
                "목표 보기",
                InvestigationFeedbackSeverity.Information);
        }

        public static InvestigationFeedback ForObjectiveCompleted(
            InvestigationObjectiveDefinition definition)
        {
            string title = definition?.Title ?? "조사 목표";
            return Create(
                "objective_completed",
                "목표 완료",
                $"{title} 목표를 완료했습니다.",
                "계속",
                InvestigationFeedbackSeverity.Information);
        }

        private static InvestigationFeedback Create(
            string code,
            string title,
            string message,
            string actionLabel,
            InvestigationFeedbackSeverity severity,
            string diagnosticDetail = "")
        {
            return new InvestigationFeedback(
                code,
                title,
                message,
                actionLabel,
                severity,
                diagnosticDetail);
        }

        private static string BuildPrerequisiteMessage(SceneTravelResult result)
        {
            string sceneId = result.Scene?.SceneId;
            return string.IsNullOrEmpty(sceneId)
                ? "먼저 선행 조사를 완료해야 합니다."
                : $"{sceneId} 장면으로 가려면 선행 조사를 완료해야 합니다.";
        }

        private static string BuildPuzzleMessage(
            int missingSelections,
            int missingEvidence)
        {
            if (missingSelections > 0 && missingEvidence > 0)
            {
                return $"선택 {missingSelections}개와 필수 증거 " +
                       $"{missingEvidence}개를 더 확인해야 합니다.";
            }

            if (missingEvidence > 0)
            {
                return $"필수 증거 {missingEvidence}개가 부족합니다.";
            }

            if (missingSelections > 0)
            {
                return $"정답 선택 {missingSelections}개를 더 확인해야 합니다.";
            }

            return "이미 완료했거나 현재 제출할 수 없는 퍼즐입니다.";
        }

        private static string BuildPuzzleDiagnostic(
            ProductionPuzzleDefinition definition,
            IReadOnlyList<string> missingSelections,
            IReadOnlyList<string> missingEvidence)
        {
            string puzzleId = definition?.Id ?? "unknown";
            string selections = string.Join(
                ",",
                missingSelections ?? Array.Empty<string>());
            string evidence = string.Join(
                ",",
                missingEvidence ?? Array.Empty<string>());
            return $"Puzzle '{puzzleId}' missing selections " +
                   $"[{selections}] and evidence [{evidence}].";
        }
    }

    public static class KoreanTextIntegrity
    {
        private static readonly string[] MojibakeTokens =
        {
            "\uFFFD",
            "占쏙옙",
            "紐⑺",
            "?쒕",
            "?꾨"
        };

        public static bool IsClean(string value) =>
            FindProblems(value).Count == 0;

        public static IReadOnlyList<string> FindProblems(string value)
        {
            if (string.IsNullOrEmpty(value))
            {
                return Array.Empty<string>();
            }

            return MojibakeTokens
                .Where(value.Contains)
                .Distinct(StringComparer.Ordinal)
                .ToArray();
        }
    }
}
