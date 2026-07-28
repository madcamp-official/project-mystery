using System;
using System.Collections.Generic;
using System.Linq;
using Wake.Core;
using Wake.Narrative;

namespace Wake.Puzzles
{
    public enum FacilityLogKind
    {
        Door,
        Detector,
        Lighting,
        Ventilation
    }

    public readonly struct CameraBlindSpotCompletion
    {
        public CameraBlindSpotCompletion(
            bool completed,
            IReadOnlyList<string> missingSteps)
        {
            Completed = completed;
            MissingSteps = missingSteps ?? Array.Empty<string>();
        }

        public bool Completed { get; }
        public IReadOnlyList<string> MissingSteps { get; }
    }

    /// <summary>
    /// State contract for D2-04. The core clue cannot be earned from CCTV alone:
    /// all video and facility-log observations must be combined.
    /// </summary>
    public sealed class CameraBlindSpotSession
    {
        public const string SessionId = "camera_blind_spot";
        public const string SceneId = "D2-04";
        public const string CompletionFlag = "camera_blind_spot_solved";
        public const string CeilingInvestigationFlag = "ceiling_investigation";
        public const string EvidenceId = "C-08";
        public const int StartMinute = 21 * 60 + 50;
        public const int EndMinute = 22 * 60 + 30;
        public const int DetectorErrorSecond = 28 * 60 + 7;
        public const int DetectorErrorDuration = 4;

        private const string ReviewedCctv = "reviewed_cctv";
        private const string DoorClear = "door_log_clear";
        private const string LogsOverlaid = "logs_overlaid";
        private const string DetectorSelected = "detector_error_selected";
        private const string LocationConfirmed = "location_confirmed";

        private readonly GameStateManager state;
        private readonly Func<string, bool> hasEvidence;
        private readonly Func<string, bool> tryGrantEvidence;
        private readonly HashSet<string> observations = new(StringComparer.Ordinal);

        public CameraBlindSpotSession(
            GameStateManager state,
            Func<string, bool> hasEvidence,
            Func<string, bool> tryGrantEvidence)
        {
            this.state = state ?? throw new ArgumentNullException(nameof(state));
            this.hasEvidence = hasEvidence ??
                               throw new ArgumentNullException(nameof(hasEvidence));
            this.tryGrantEvidence = tryGrantEvidence ??
                                    throw new ArgumentNullException(nameof(tryGrantEvidence));

            if (state.TryGetPuzzleSession(SessionId, out PuzzleSessionState saved))
            {
                foreach (string item in saved.selectedIds ?? new List<string>())
                {
                    observations.Add(item);
                }
                IsCompleted = saved.completed;
                CurrentSecond = Math.Clamp(
                    saved.step,
                    0,
                    (EndMinute - StartMinute) * 60);
            }
        }

        public bool HasReviewedCctv => observations.Contains(ReviewedCctv);
        public bool HasConfirmedDoorLog => observations.Contains(DoorClear);
        public bool HasOverlaidLogs => observations.Contains(LogsOverlaid);
        public bool HasSelectedDetectorError =>
            observations.Contains(DetectorSelected);
        public bool HasConfirmedLocation =>
            observations.Contains(LocationConfirmed);
        public bool IsCompleted { get; private set; }
        public int CurrentSecond { get; private set; }
        public FacilityLogKind ActiveLog { get; private set; }

        public void SetTime(int second, bool persist = true)
        {
            CurrentSecond = Math.Clamp(
                second,
                0,
                (EndMinute - StartMinute) * 60);
            if (persist)
            {
                Save();
            }
        }

        public bool ReviewCctv()
        {
            if (IsCompleted || !observations.Add(ReviewedCctv))
            {
                return false;
            }
            Save();
            return true;
        }

        public bool OpenFacilityLogs()
        {
            if (IsCompleted || !HasReviewedCctv)
            {
                return false;
            }
            observations.Add(LogsOverlaid);
            Save();
            return true;
        }

        public void SelectLog(FacilityLogKind kind)
        {
            ActiveLog = kind;
            if (!IsCompleted && HasOverlaidLogs && kind == FacilityLogKind.Door)
            {
                observations.Add(DoorClear);
            }
            Save();
        }

        public bool SelectDetectorError()
        {
            if (IsCompleted ||
                !HasOverlaidLogs ||
                !HasConfirmedDoorLog ||
                ActiveLog != FacilityLogKind.Detector)
            {
                return false;
            }

            observations.Add(DetectorSelected);
            CurrentSecond = DetectorErrorSecond;
            Save();
            return true;
        }

        public bool ConfirmErrorLocation()
        {
            if (IsCompleted || !HasSelectedDetectorError)
            {
                return false;
            }
            observations.Add(LocationConfirmed);
            Save();
            return true;
        }

        public CameraBlindSpotCompletion TryComplete()
        {
            string[] missing = RequiredObservations()
                .Where(item => !observations.Contains(item.Id))
                .Select(item => item.Label)
                .ToArray();
            if (IsCompleted ||
                missing.Length > 0 ||
                !ProductionSceneCompletionGate.CanStartInteraction(
                    state,
                    SceneId,
                    SessionId))
            {
                return new CameraBlindSpotCompletion(false, missing);
            }

            using IDisposable batch = state.BeginStateBatch();
            if (!hasEvidence(EvidenceId) && !tryGrantEvidence(EvidenceId))
            {
                return new CameraBlindSpotCompletion(
                    false,
                    new[] { "화재감지기 오류 기록 저장" });
            }

            IsCompleted = true;
            state.AddFlag(CompletionFlag);
            state.AddFlag(CeilingInvestigationFlag);
            Save();
            if (!ProductionSceneCompletionGate.TryComplete(
                    state,
                    SceneId,
                    SessionId))
            {
                IsCompleted = false;
                Save();
                return new CameraBlindSpotCompletion(false, Array.Empty<string>());
            }

            return new CameraBlindSpotCompletion(true, Array.Empty<string>());
        }

        private static IEnumerable<(string Id, string Label)> RequiredObservations()
        {
            yield return (ReviewedCctv, "CCTV 무인 구간 확인");
            yield return (DoorClear, "출입문 로그 확인");
            yield return (LogsOverlaid, "설비 로그 중첩");
            yield return (DetectorSelected, "22:18 감지기 오류 선택");
            yield return (LocationConfirmed, "오류 위치 확인");
        }

        private void Save()
        {
            state.SavePuzzleSession(new PuzzleSessionState
            {
                puzzleId = SessionId,
                selectedIds = observations.OrderBy(item => item).ToList(),
                step = CurrentSecond,
                completed = IsCompleted
            });
        }
    }
}
