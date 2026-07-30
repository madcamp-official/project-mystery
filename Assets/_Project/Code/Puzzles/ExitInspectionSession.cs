using System;
using System.Collections.Generic;
using System.Linq;
using Wake.Core;
using Wake.Evidence;
using Wake.Narrative;

namespace Wake.Puzzles
{
    public enum ExitInspectionResult
    {
        Recorded,
        AlreadyInspected,
        UnknownInspection,
        EvidenceUnavailable,
        SessionCompleted
    }

    public enum ExitRouteVerdict
    {
        None,
        Used,
        Unused,
        Inconclusive
    }

    public enum ExitInspectionTheory
    {
        None,
        PerfectCleanup,
        DoorExit,
        NoLiveThirdParty
    }

    public enum ExitInspectionActionCode
    {
        Recorded,
        RouteObservationCompleted,
        AlreadyRecorded,
        UnknownRoute,
        UnknownObservation,
        ObservationsIncomplete,
        InvalidVerdict,
        VerdictRecorded,
        VerdictUpdated,
        VerdictsIncomplete,
        VerdictsCorrect,
        VerdictsIncorrect,
        InvalidTheory,
        TheoryRecorded,
        EvidenceUnavailable,
        SessionCompleted
    }

    public enum ExitInspectionCompletionFailure
    {
        None,
        AlreadyCompleted,
        MissingObservations,
        MissingEvidence,
        MissingVerdicts,
        IncorrectVerdicts,
        MissingTheory,
        PerfectCleanupContradicted,
        DoorExitContradicted,
        IncorrectTheory,
        InteractionUnavailable,
        DeductionUnavailable,
        SceneCompletionFailed
    }

    public readonly struct ExitInspectionAction
    {
        public ExitInspectionAction(
            ExitInspectionActionCode code,
            bool accepted,
            string message,
            string routeId = "",
            string observationPointId = "")
        {
            Code = code;
            Accepted = accepted;
            Message = message ?? string.Empty;
            RouteId = routeId ?? string.Empty;
            ObservationPointId = observationPointId ?? string.Empty;
        }

        public ExitInspectionActionCode Code { get; }
        public bool Accepted { get; }
        public string Message { get; }
        public string RouteId { get; }
        public string ObservationPointId { get; }
    }

    public sealed class ExitInspectionObservationDefinition
    {
        public ExitInspectionObservationDefinition(
            string id,
            string title,
            string finding)
        {
            Id = Normalize(id);
            Title = title?.Trim() ?? string.Empty;
            Finding = finding?.Trim() ?? string.Empty;
        }

        public string Id { get; }
        public string Title { get; }
        public string Finding { get; }

        public static string Normalize(string value) =>
            string.IsNullOrWhiteSpace(value)
                ? string.Empty
                : value.Trim().ToUpperInvariant().Replace(' ', '_');
    }

    public sealed class ExitInspectionDefinition
    {
        private readonly IReadOnlyDictionary<string, ExitInspectionObservationDefinition>
            observationsById;

        public ExitInspectionDefinition(
            string id,
            string title,
            string finding,
            string evidenceId,
            params ExitInspectionObservationDefinition[] observations)
        {
            Id = Normalize(id);
            Title = title?.Trim() ?? string.Empty;
            Finding = finding?.Trim() ?? string.Empty;
            EvidenceId = CanonicalEvidenceCatalog.NormalizeId(evidenceId);
            Observations = (observations ?? Array.Empty<ExitInspectionObservationDefinition>())
                .Where(item => item != null)
                .ToArray();
            observationsById = Observations.ToDictionary(
                item => item.Id,
                StringComparer.Ordinal);
        }

        public string Id { get; }
        public string Title { get; }
        public string Finding { get; }
        public string EvidenceId { get; }
        public IReadOnlyList<ExitInspectionObservationDefinition> Observations { get; }
        public IReadOnlyList<string> ObservationPointIds =>
            Observations.Select(item => item.Id).ToArray();

        public bool TryGetObservation(
            string observationPointId,
            out ExitInspectionObservationDefinition observation) =>
            observationsById.TryGetValue(
                ExitInspectionObservationDefinition.Normalize(observationPointId),
                out observation);

        public static string Normalize(string value) =>
            string.IsNullOrWhiteSpace(value)
                ? string.Empty
                : value.Trim().ToLowerInvariant().Replace(' ', '_');
    }

    public static class ExitInspectionCatalog
    {
        public const string SessionId = "exit_inspection";
        public const string SceneId = "D2-01";
        public const string ParallelSceneId = "D2-04";
        public const string ExteriorLedge = "exterior_ledge";
        public const string AirDuct = "air_duct";
        public const string ServiceHatch = "service_hatch";
        public const string CompletionFlag = "pz_exit_solved";

        private static readonly ExitInspectionDefinition[] Definitions =
        {
            new(
                ExteriorLedge,
                "외벽 발판",
                "염분막이 연속되고 압력 센서 기록이 없어 사람이 지나간 흔적이 없다.",
                "C-03",
                Observation(
                    "SALT_FILM",
                    "염분막",
                    "발판 표면의 염분막이 끊기지 않았다."),
                Observation(
                    "PRESSURE_SENSOR",
                    "압력 센서",
                    "사람의 체중에 해당하는 압력 변화가 기록되지 않았다.")),
            new(
                AirDuct,
                "공조 덕트",
                "나사와 먼지가 흐트러지지 않았고 성인이 통과할 공간도 없다.",
                "C-04",
                Observation(
                    "DUCT_DUST",
                    "덕트 먼지",
                    "먼지가 가장자리까지 고르게 쌓여 있다."),
                Observation(
                    "INNER_WALL",
                    "내부 벽면",
                    "좁은 덕트 안쪽에 긁힘이나 통과 흔적이 없다.")),
            new(
                ServiceHatch,
                "설비 점검구",
                "공구 자국과 봉인선 훼손이 없고 내부 통로는 막혀 있다.",
                "C-05",
                Observation(
                    "SCREWS",
                    "점검구 나사",
                    "나사 홈에 새 공구 자국이 없다."),
                Observation(
                    "DUST_SEAL",
                    "먼지 봉인선",
                    "덮개 가장자리의 먼지와 실리콘 봉인선이 연속되어 있다."))
        };

        private static readonly IReadOnlyDictionary<string, ExitInspectionDefinition>
            ById = Definitions.ToDictionary(item => item.Id, StringComparer.Ordinal);

        public static IReadOnlyList<ExitInspectionDefinition> All => Definitions;

        public static bool TryGet(
            string inspectionId,
            out ExitInspectionDefinition definition) =>
            ById.TryGetValue(
                ExitInspectionDefinition.Normalize(inspectionId),
                out definition);

        private static ExitInspectionObservationDefinition Observation(
            string id,
            string title,
            string finding) =>
            new(id, title, finding);
    }

    public readonly struct ExitInspectionCompletion
    {
        public ExitInspectionCompletion(
            bool completed,
            IReadOnlyList<string> missingInspectionIds,
            IReadOnlyList<string> missingEvidenceIds)
            : this(
                completed,
                missingInspectionIds,
                missingEvidenceIds,
                Array.Empty<string>(),
                completed
                    ? ExitInspectionCompletionFailure.None
                    : ExitInspectionCompletionFailure.MissingObservations,
                string.Empty)
        {
        }

        public ExitInspectionCompletion(
            bool completed,
            IReadOnlyList<string> missingInspectionIds,
            IReadOnlyList<string> missingEvidenceIds,
            IReadOnlyList<string> incorrectVerdictIds,
            ExitInspectionCompletionFailure failure,
            string message)
        {
            Completed = completed;
            MissingInspectionIds = missingInspectionIds ?? Array.Empty<string>();
            MissingEvidenceIds = missingEvidenceIds ?? Array.Empty<string>();
            IncorrectVerdictIds = incorrectVerdictIds ?? Array.Empty<string>();
            Failure = failure;
            Message = message ?? string.Empty;
        }

        public bool Completed { get; }
        public IReadOnlyList<string> MissingInspectionIds { get; }
        public IReadOnlyList<string> MissingEvidenceIds { get; }
        public IReadOnlyList<string> IncorrectVerdictIds { get; }
        public ExitInspectionCompletionFailure Failure { get; }
        public string Message { get; }
    }

    public sealed class ExitInspectionSession
    {
        private const string SchemaToken = "schema:2";
        private const string ObservationPrefix = "obs:";
        private const string VerdictPrefix = "verdict:";
        private const string TheoryPrefix = "theory:";

        private readonly GameStateManager state;
        private readonly Func<string, bool> hasEvidence;
        private readonly Func<string, bool> tryGrantEvidence;
        private readonly Func<GameStateManager, string, string, bool>
            tryCompleteScene;
        private readonly List<string> observationOrder = new();
        private readonly HashSet<string> observed = new(StringComparer.Ordinal);
        private readonly List<string> inspectionOrder = new();
        private readonly HashSet<string> inspected = new(StringComparer.Ordinal);
        private readonly Dictionary<string, ExitRouteVerdict> verdicts =
            new(StringComparer.Ordinal);

        public ExitInspectionSession(
            GameStateManager state,
            Func<string, bool> hasEvidence,
            Func<string, bool> tryGrantEvidence)
            : this(
                state,
                hasEvidence,
                tryGrantEvidence,
                ProductionSceneCompletionGate.TryComplete)
        {
        }

        public ExitInspectionSession(
            GameStateManager state,
            Func<string, bool> hasEvidence,
            Func<string, bool> tryGrantEvidence,
            Func<GameStateManager, string, string, bool> tryCompleteScene)
        {
            this.state = state ?? throw new ArgumentNullException(nameof(state));
            this.hasEvidence = hasEvidence ??
                throw new ArgumentNullException(nameof(hasEvidence));
            this.tryGrantEvidence = tryGrantEvidence ??
                throw new ArgumentNullException(nameof(tryGrantEvidence));
            this.tryCompleteScene = tryCompleteScene ??
                throw new ArgumentNullException(nameof(tryCompleteScene));

            if (state.TryGetPuzzleSession(
                    ExitInspectionCatalog.SessionId,
                    out PuzzleSessionState saved))
            {
                Restore(saved);
            }
        }

        public IReadOnlyList<string> InspectionOrder => inspectionOrder;
        public int Step => inspected.Count;
        public int ObservationCount => observed.Count;
        public int HintLevel { get; private set; }
        public bool IsCompleted { get; private set; }
        public ExitInspectionTheory SelectedTheory { get; private set; }

        public bool HasInspected(string inspectionId) =>
            inspected.Contains(ExitInspectionDefinition.Normalize(inspectionId));

        public bool HasObserved(
            string inspectionId,
            string observationPointId) =>
            observed.Contains(ObservationKey(inspectionId, observationPointId));

        public ExitRouteVerdict GetVerdict(string inspectionId) =>
            verdicts.TryGetValue(
                ExitInspectionDefinition.Normalize(inspectionId),
                out ExitRouteVerdict verdict)
                ? verdict
                : ExitRouteVerdict.None;

        public ExitInspectionAction Observe(
            string inspectionId,
            string observationPointId)
        {
            string routeId = ExitInspectionDefinition.Normalize(inspectionId);
            string pointId =
                ExitInspectionObservationDefinition.Normalize(observationPointId);
            if (IsCompleted)
            {
                return Action(
                    ExitInspectionActionCode.SessionCompleted,
                    false,
                    "이미 완료한 출구 검증입니다.",
                    routeId,
                    pointId);
            }

            if (!ExitInspectionCatalog.TryGet(routeId, out var definition))
            {
                return Action(
                    ExitInspectionActionCode.UnknownRoute,
                    false,
                    "알 수 없는 출구입니다.",
                    routeId,
                    pointId);
            }

            if (!definition.TryGetObservation(pointId, out var observation))
            {
                return Action(
                    ExitInspectionActionCode.UnknownObservation,
                    false,
                    $"{definition.Title}에서 확인할 수 없는 관찰 지점입니다.",
                    routeId,
                    pointId);
            }

            string key = ObservationKey(routeId, pointId);
            if (observed.Contains(key))
            {
                if (IsRouteObserved(definition) &&
                    !EnsureEvidence(definition.EvidenceId))
                {
                    return Action(
                        ExitInspectionActionCode.EvidenceUnavailable,
                        false,
                        $"{definition.Title}의 관찰 결과를 단서로 기록하지 못했습니다.",
                        routeId,
                        pointId);
                }

                return Action(
                    ExitInspectionActionCode.AlreadyRecorded,
                    false,
                    $"{observation.Title} 흔적은 이미 확인했습니다.",
                    routeId,
                    pointId);
            }

            using IDisposable batch = state.BeginStateBatch();
            observed.Add(key);
            observationOrder.Add(key);

            bool routeCompleted = IsRouteObserved(definition);
            if (routeCompleted && !EnsureEvidence(definition.EvidenceId))
            {
                observed.Remove(key);
                observationOrder.Remove(key);
                return Action(
                    ExitInspectionActionCode.EvidenceUnavailable,
                    false,
                    $"{definition.Title}의 관찰 결과를 단서로 기록하지 못했습니다.",
                    routeId,
                    pointId);
            }

            if (routeCompleted && inspected.Add(routeId))
            {
                inspectionOrder.Add(routeId);
            }

            Save();
            return routeCompleted
                ? Action(
                    ExitInspectionActionCode.RouteObservationCompleted,
                    true,
                    $"{definition.Title}의 필수 흔적을 모두 확인했습니다. " +
                    "이 경로의 사용 여부를 판정하세요.",
                    routeId,
                    pointId)
                : Action(
                    ExitInspectionActionCode.Recorded,
                    true,
                    $"{observation.Title} 흔적을 기록했습니다. " +
                    $"{definition.Title}에 확인하지 않은 흔적이 남아 있습니다.",
                    routeId,
                    pointId);
        }

        // Legacy UI compatibility: opening a route records its two authored
        // observation points. Completion still requires route verdicts and theory.
        public ExitInspectionResult Inspect(string inspectionId)
        {
            if (IsCompleted)
            {
                return ExitInspectionResult.SessionCompleted;
            }

            if (!ExitInspectionCatalog.TryGet(
                    inspectionId,
                    out ExitInspectionDefinition definition))
            {
                return ExitInspectionResult.UnknownInspection;
            }

            if (HasInspected(definition.Id))
            {
                return EnsureEvidence(definition.EvidenceId)
                    ? ExitInspectionResult.AlreadyInspected
                    : ExitInspectionResult.EvidenceUnavailable;
            }

            foreach (ExitInspectionObservationDefinition observation
                     in definition.Observations)
            {
                ExitInspectionAction result = Observe(definition.Id, observation.Id);
                if (result.Code == ExitInspectionActionCode.EvidenceUnavailable)
                {
                    return ExitInspectionResult.EvidenceUnavailable;
                }
            }

            return HasInspected(definition.Id)
                ? ExitInspectionResult.Recorded
                : ExitInspectionResult.EvidenceUnavailable;
        }

        public ExitInspectionAction SetRouteVerdict(
            string inspectionId,
            ExitRouteVerdict verdict)
        {
            string routeId = ExitInspectionDefinition.Normalize(inspectionId);
            if (IsCompleted)
            {
                return Action(
                    ExitInspectionActionCode.SessionCompleted,
                    false,
                    "이미 완료한 출구 검증입니다.",
                    routeId);
            }

            if (!ExitInspectionCatalog.TryGet(routeId, out var definition))
            {
                return Action(
                    ExitInspectionActionCode.UnknownRoute,
                    false,
                    "알 수 없는 출구입니다.",
                    routeId);
            }

            if (!HasInspected(routeId))
            {
                return Action(
                    ExitInspectionActionCode.ObservationsIncomplete,
                    false,
                    $"{definition.Title}의 필수 흔적 두 가지를 먼저 확인하세요.",
                    routeId);
            }

            if (verdict == ExitRouteVerdict.None)
            {
                return Action(
                    ExitInspectionActionCode.InvalidVerdict,
                    false,
                    "사용됨, 사용되지 않음, 판단 불가 중 하나를 선택하세요.",
                    routeId);
            }

            bool updated = verdicts.ContainsKey(routeId);
            verdicts[routeId] = verdict;
            Save();
            return Action(
                updated
                    ? ExitInspectionActionCode.VerdictUpdated
                    : ExitInspectionActionCode.VerdictRecorded,
                true,
                $"{definition.Title}을(를) '{VerdictLabel(verdict)}'으로 판정했습니다.",
                routeId);
        }

        public ExitInspectionAction SelectTheory(ExitInspectionTheory theory)
        {
            if (IsCompleted)
            {
                return Action(
                    ExitInspectionActionCode.SessionCompleted,
                    false,
                    "이미 완료한 출구 검증입니다.");
            }

            if (ExitInspectionCatalog.All.Any(item =>
                    GetVerdict(item.Id) == ExitRouteVerdict.None))
            {
                return Action(
                    ExitInspectionActionCode.VerdictsIncomplete,
                    false,
                    "세 출구의 사용 여부를 모두 판정한 뒤 가설을 선택하세요.");
            }

            if (theory == ExitInspectionTheory.None)
            {
                return Action(
                    ExitInspectionActionCode.InvalidTheory,
                    false,
                    "검증할 가설을 선택하세요.");
            }

            SelectedTheory = theory;
            Save();
            return Action(
                ExitInspectionActionCode.TheoryRecorded,
                true,
                $"'{TheoryLabel(theory)}' 가설을 검증 대상으로 선택했습니다.");
        }

        public bool UseHint()
        {
            if (IsCompleted || HintLevel >= 3)
            {
                return false;
            }

            HintLevel++;
            Save();
            return true;
        }

        public ExitInspectionAction ValidateRouteVerdicts()
        {
            string[] missing = ExitInspectionCatalog.All
                .Where(item => GetVerdict(item.Id) == ExitRouteVerdict.None)
                .Select(item => item.Id)
                .ToArray();
            if (missing.Length > 0)
            {
                return Action(
                    ExitInspectionActionCode.VerdictsIncomplete,
                    false,
                    "세 후보 출구의 판정을 모두 선택하세요.");
            }

            string[] incorrect = ExitInspectionCatalog.All
                .Where(item => GetVerdict(item.Id) != ExitRouteVerdict.Unused)
                .Select(item => item.Id)
                .ToArray();
            if (incorrect.Length > 0)
            {
                string routes = string.Join(
                    ", ",
                    incorrect.Select(id =>
                        ExitInspectionCatalog.TryGet(
                            id,
                            out ExitInspectionDefinition definition)
                            ? definition.Title
                            : id));
                return Action(
                    ExitInspectionActionCode.VerdictsIncorrect,
                    false,
                    $"판정 불일치: {routes}의 선택이 관찰 기록과 모순됩니다.");
            }

            return Action(
                ExitInspectionActionCode.VerdictsCorrect,
                true,
                "판정 정확: 세 후보 출구 모두 사람의 이동에 사용되지 않았습니다.");
        }

        public ExitInspectionCompletion TryComplete()
        {
            if (IsCompleted)
            {
                return Failure(
                    ExitInspectionCompletionFailure.AlreadyCompleted,
                    "이미 완료한 출구 검증입니다.");
            }

            string[] missingInspections = ExitInspectionCatalog.All
                .Where(item => !HasInspected(item.Id))
                .Select(item => item.Id)
                .ToArray();
            if (missingInspections.Length > 0)
            {
                return Failure(
                    ExitInspectionCompletionFailure.MissingObservations,
                    "각 출구에서 두 가지 필수 흔적을 모두 조사해야 합니다.",
                    missingInspections);
            }

            string[] missingEvidence = ExitInspectionCatalog.All
                .Where(item => !hasEvidence(item.EvidenceId))
                .Select(item => item.EvidenceId)
                .ToArray();
            if (missingEvidence.Length > 0)
            {
                return Failure(
                    ExitInspectionCompletionFailure.MissingEvidence,
                    "조사 결과가 단서 기록에 모두 보존되지 않았습니다.",
                    missingEvidenceIds: missingEvidence);
            }

            string[] missingVerdicts = ExitInspectionCatalog.All
                .Where(item => GetVerdict(item.Id) == ExitRouteVerdict.None)
                .Select(item => item.Id)
                .ToArray();
            if (missingVerdicts.Length > 0)
            {
                return Failure(
                    ExitInspectionCompletionFailure.MissingVerdicts,
                    "세 출구의 사용 여부를 모두 판정해야 합니다.",
                    incorrectVerdictIds: missingVerdicts);
            }

            string[] incorrectVerdicts = ExitInspectionCatalog.All
                .Where(item => GetVerdict(item.Id) != ExitRouteVerdict.Unused)
                .Select(item => item.Id)
                .ToArray();
            if (incorrectVerdicts.Length > 0)
            {
                string message = incorrectVerdicts.Any(id =>
                        GetVerdict(id) == ExitRouteVerdict.Used)
                    ? "사용됨 판정은 연속된 염분막·먼지·봉인선 및 센서 기록과 모순됩니다."
                    : "판단 불가로는 출구 가설을 검증할 수 없습니다. 필수 흔적을 다시 비교하세요.";
                return Failure(
                    ExitInspectionCompletionFailure.IncorrectVerdicts,
                    message,
                    incorrectVerdictIds: incorrectVerdicts);
            }

            switch (SelectedTheory)
            {
                case ExitInspectionTheory.None:
                    return Failure(
                        ExitInspectionCompletionFailure.MissingTheory,
                        "세 출구의 판정을 설명할 최종 가설을 선택하세요.");
                case ExitInspectionTheory.PerfectCleanup:
                    return Failure(
                        ExitInspectionCompletionFailure.PerfectCleanupContradicted,
                        "서로 다른 물리 흔적과 센서 기록을 동시에 원상 복구했다는 " +
                        "설명은 증거로 뒷받침되지 않습니다.");
                case ExitInspectionTheory.DoorExit:
                    return Failure(
                        ExitInspectionCompletionFailure.DoorExitContradicted,
                        "문으로 두 번째 사람이 이동했다면 문턱에 두 번째 젖은 " +
                        "발자국이 남아야 합니다.");
                case not ExitInspectionTheory.NoLiveThirdParty:
                    return Failure(
                        ExitInspectionCompletionFailure.IncorrectTheory,
                        "현재 출구 흔적을 모두 설명하지 못하는 가설입니다.");
            }

            if (!ProductionSceneCompletionGate.CanStartInteraction(
                    state,
                    ExitInspectionCatalog.SceneId,
                    ExitInspectionCatalog.SessionId))
            {
                return Failure(
                    ExitInspectionCompletionFailure.InteractionUnavailable,
                    "현재 진행 상태에서는 출구 검증을 확정할 수 없습니다.");
            }

            var deduction = new CanonicalDeductionService(state, hasEvidence);
            bool hasSceneDenial =
                state.HasUnlockedDeduction(CanonicalDeductionCatalog.SceneDenial);
            DeductionEvaluation evaluation = hasSceneDenial
                ? null
                : deduction.Evaluate(CanonicalDeductionCatalog.SceneDenial);
            if (!hasSceneDenial && evaluation?.CanUnlock != true)
            {
                return Failure(
                    ExitInspectionCompletionFailure.DeductionUnavailable,
                    "필수 단서가 부족하여 최종 논증을 기록하지 못했습니다.");
            }

            using IDisposable batch = state.BeginStateBatch();
            if (!tryCompleteScene(
                    state,
                    ExitInspectionCatalog.SceneId,
                    ExitInspectionCatalog.SessionId))
            {
                return Failure(
                    ExitInspectionCompletionFailure.SceneCompletionFailed,
                    "논증은 기록했지만 다음 조사 단계로 진행하지 못했습니다.");
            }

            if (!hasSceneDenial &&
                !deduction.TryUnlock(CanonicalDeductionCatalog.SceneDenial) &&
                !state.HasUnlockedDeduction(
                    CanonicalDeductionCatalog.SceneDenial))
            {
                return Failure(
                    ExitInspectionCompletionFailure.DeductionUnavailable,
                    "필수 단서가 부족하여 최종 논증을 기록하지 못했습니다.");
            }

            IsCompleted = true;
            Save();
            state.AddFlag(
                ExitInspectionCatalog.CompletionFlag,
                "흔적 없는 출구 검증 완료");
            if (ProductionSceneCompletionCatalog.TryGet(
                    ExitInspectionCatalog.SceneId,
                    out ProductionSceneCompletionRequirement requirement))
            {
                state.UnlockProductionScene(requirement.NextSceneId);
            }
            state.UnlockProductionScene(ExitInspectionCatalog.ParallelSceneId);

            return new ExitInspectionCompletion(
                true,
                Array.Empty<string>(),
                Array.Empty<string>(),
                Array.Empty<string>(),
                ExitInspectionCompletionFailure.None,
                "사건 당시 방 안에 살아 있는 제3자가 없었다는 가설을 입증했습니다.");
        }

        private void Restore(PuzzleSessionState saved)
        {
            List<string> selected = saved.selectedIds ?? new List<string>();
            bool newFormat = selected.Any(item =>
                string.Equals(item, SchemaToken, StringComparison.Ordinal));

            if (newFormat)
            {
                RestoreCurrent(selected);
            }
            else
            {
                RestoreLegacy(selected);
            }

            HintLevel = Math.Clamp(saved.hintLevel, 0, 3);
            if (saved.completed)
            {
                RestoreCompletedState();
            }
        }

        private void RestoreCurrent(IEnumerable<string> selectedIds)
        {
            foreach (string encoded in selectedIds)
            {
                if (encoded == null ||
                    string.Equals(encoded, SchemaToken, StringComparison.Ordinal))
                {
                    continue;
                }

                if (encoded.StartsWith(
                        ObservationPrefix,
                        StringComparison.Ordinal))
                {
                    RestoreObservation(encoded[ObservationPrefix.Length..]);
                    continue;
                }

                if (encoded.StartsWith(VerdictPrefix, StringComparison.Ordinal))
                {
                    RestoreVerdict(encoded[VerdictPrefix.Length..]);
                    continue;
                }

                if (encoded.StartsWith(TheoryPrefix, StringComparison.Ordinal) &&
                    Enum.TryParse(
                        encoded[TheoryPrefix.Length..],
                        true,
                        out ExitInspectionTheory theory) &&
                    theory != ExitInspectionTheory.None)
                {
                    SelectedTheory = theory;
                }
            }
        }

        private void RestoreLegacy(IEnumerable<string> selectedIds)
        {
            foreach (string sourceId in selectedIds)
            {
                string routeId = ExitInspectionDefinition.Normalize(sourceId);
                if (!ExitInspectionCatalog.TryGet(routeId, out var definition))
                {
                    continue;
                }

                foreach (ExitInspectionObservationDefinition observation
                         in definition.Observations)
                {
                    AddRestoredObservation(routeId, observation.Id);
                }
            }
        }

        private void RestoreObservation(string encoded)
        {
            int separator = encoded.LastIndexOf(':');
            if (separator <= 0 || separator >= encoded.Length - 1)
            {
                return;
            }

            string routeId = ExitInspectionDefinition.Normalize(
                encoded[..separator]);
            string pointId = ExitInspectionObservationDefinition.Normalize(
                encoded[(separator + 1)..]);
            if (ExitInspectionCatalog.TryGet(routeId, out var definition) &&
                definition.TryGetObservation(pointId, out _))
            {
                AddRestoredObservation(routeId, pointId);
            }
        }

        private void RestoreVerdict(string encoded)
        {
            int separator = encoded.LastIndexOf(':');
            if (separator <= 0 || separator >= encoded.Length - 1)
            {
                return;
            }

            string routeId = ExitInspectionDefinition.Normalize(
                encoded[..separator]);
            if (ExitInspectionCatalog.TryGet(routeId, out _) &&
                Enum.TryParse(
                    encoded[(separator + 1)..],
                    true,
                    out ExitRouteVerdict verdict) &&
                verdict != ExitRouteVerdict.None)
            {
                verdicts[routeId] = verdict;
            }
        }

        private void AddRestoredObservation(string routeId, string pointId)
        {
            string key = ObservationKey(routeId, pointId);
            if (!observed.Add(key))
            {
                return;
            }

            observationOrder.Add(key);
            if (ExitInspectionCatalog.TryGet(routeId, out var definition) &&
                IsRouteObserved(definition) &&
                inspected.Add(routeId))
            {
                inspectionOrder.Add(routeId);
            }
        }

        private void RestoreCompletedState()
        {
            foreach (ExitInspectionDefinition definition
                     in ExitInspectionCatalog.All)
            {
                foreach (ExitInspectionObservationDefinition observation
                         in definition.Observations)
                {
                    AddRestoredObservation(definition.Id, observation.Id);
                }

                verdicts[definition.Id] = ExitRouteVerdict.Unused;
            }

            SelectedTheory = ExitInspectionTheory.NoLiveThirdParty;
            IsCompleted = true;
        }

        private void Save()
        {
            var selected = new List<string> { SchemaToken };
            selected.AddRange(observationOrder.Select(key =>
                ObservationPrefix + key));
            selected.AddRange(ExitInspectionCatalog.All
                .Where(item => verdicts.ContainsKey(item.Id))
                .Select(item =>
                    $"{VerdictPrefix}{item.Id}:{verdicts[item.Id]}"));
            if (SelectedTheory != ExitInspectionTheory.None)
            {
                selected.Add(TheoryPrefix + SelectedTheory);
            }

            state.SavePuzzleSession(new PuzzleSessionState
            {
                puzzleId = ExitInspectionCatalog.SessionId,
                selectedIds = selected,
                step = Step,
                hintLevel = HintLevel,
                completed = IsCompleted
            });
        }

        private bool IsRouteObserved(ExitInspectionDefinition definition) =>
            definition.Observations.All(observation =>
                observed.Contains(ObservationKey(definition.Id, observation.Id)));

        private bool EnsureEvidence(string evidenceId) =>
            hasEvidence(evidenceId) || tryGrantEvidence(evidenceId);

        private ExitInspectionCompletion Failure(
            ExitInspectionCompletionFailure failure,
            string message,
            IReadOnlyList<string> missingInspectionIds = null,
            IReadOnlyList<string> missingEvidenceIds = null,
            IReadOnlyList<string> incorrectVerdictIds = null) =>
            new(
                false,
                missingInspectionIds ?? Array.Empty<string>(),
                missingEvidenceIds ?? Array.Empty<string>(),
                incorrectVerdictIds ?? Array.Empty<string>(),
                failure,
                message);

        private static ExitInspectionAction Action(
            ExitInspectionActionCode code,
            bool accepted,
            string message,
            string routeId = "",
            string observationPointId = "") =>
            new(code, accepted, message, routeId, observationPointId);

        private static string ObservationKey(
            string inspectionId,
            string observationPointId) =>
            $"{ExitInspectionDefinition.Normalize(inspectionId)}:" +
            ExitInspectionObservationDefinition.Normalize(observationPointId);

        private static string VerdictLabel(ExitRouteVerdict verdict) =>
            verdict switch
            {
                ExitRouteVerdict.Used => "사용됨",
                ExitRouteVerdict.Unused => "사용되지 않음",
                ExitRouteVerdict.Inconclusive => "판단 불가",
                _ => "미선택"
            };

        private static string TheoryLabel(ExitInspectionTheory theory) =>
            theory switch
            {
                ExitInspectionTheory.PerfectCleanup => "모든 흔적을 완벽히 지웠다",
                ExitInspectionTheory.DoorExit => "피해자가 문으로 범인을 내보냈다",
                ExitInspectionTheory.NoLiveThirdParty =>
                    "사건 당시 방 안에 살아 있는 제3자가 없었다",
                _ => "미선택"
            };
    }
}
