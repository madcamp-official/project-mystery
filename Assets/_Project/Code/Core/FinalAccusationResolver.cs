using System;
using System.Collections.Generic;
using System.Linq;
using Wake.Puzzles;

namespace Wake.Core
{
    public enum AccusedPerson
    {
        Unknown = 0,
        Evelyn = 1,
        Richard = 2,
        Claire = 3,
        Marcus = 4
    }

    public enum MurderLocation
    {
        Unknown = 0,
        HorizonRoom = 1,
        BallastControlAnnex = 2,
        Vault = 3,
        ServiceArea = 4
    }

    public enum MurderMethod
    {
        Unknown = 0,
        Stabbing = 1,
        NitrogenSuffocation = 2,
        Drowning = 3,
        Drug = 4
    }

    public enum BodyTransport
    {
        Unknown = 0,
        Manual = 1,
        CeilingServiceRail = 2,
        Robot = 3,
        Elevator = 4
    }

    public enum DanielTargetBelief
    {
        Unknown = 0,
        CorporateFunds = 1,
        Misconception = 2,
        Revenge = 3,
        Ransom = 4
    }

    public enum OrpheusEventDesign
    {
        Unknown = 0,
        Richard = 1,
        Evelyn = 2,
        Julian = 3,
        Thomas = 4
    }

    public enum FinalEnding
    {
        Complete,
        ConvenientCulprit,
        WrongPerson,
        Bad
    }

    public sealed class FinalAccusation
    {
        public AccusedPerson Accused { get; set; }
        public MurderLocation Location { get; set; }
        public MurderMethod Method { get; set; }
        public BodyTransport Transport { get; set; }
        public DanielTargetBelief DanielBelievedTarget { get; set; }
        public OrpheusEventDesign OrpheusDesign { get; set; }
        public bool DiscloseRichardCoverup { get; set; }
    }

    public sealed class FinalAccusationResult
    {
        public FinalEnding Ending { get; }
        public string EndingId { get; }
        public string Reason { get; }
        public bool WasRecorded { get; }

        public FinalAccusationResult(
            FinalEnding ending,
            string endingId,
            string reason,
            bool wasRecorded)
        {
            Ending = ending;
            EndingId = endingId;
            Reason = reason;
            WasRecorded = wasRecorded;
        }
    }

    public sealed class FinalAccusationResolver
    {
        public const string CompleteEndingId = "ending_a_complete";
        public const string ConvenientEndingId = "ending_b_convenient_culprit";
        public const string WrongPersonEndingId = "ending_c_wrong_person";
        public const string PanicEndingId = "ending_bad_panic";
        public const string BadEndingId = PanicEndingId;
        public const string LegacyIntegrityEndingId = "ending_bad_integrity";

        private static readonly string[] CrimeDeductions =
        {
            CanonicalDeductionCatalog.SceneDenial,
            CanonicalDeductionCatalog.BodyInsertion,
            CanonicalDeductionCatalog.TransportRoute,
            CanonicalDeductionCatalog.ActualMurder,
            CanonicalDeductionCatalog.CulpritLink
        };

        private readonly GameStateManager state;

        public FinalAccusationResolver(GameStateManager state)
        {
            this.state = state ?? throw new ArgumentNullException(nameof(state));
        }

        public FinalAccusationResult Resolve(FinalAccusation accusation)
        {
            if (!string.IsNullOrEmpty(state.FinalEndingId))
            {
                return FromStoredEnding(state.FinalEndingId);
            }

            FinalAccusationResult result = Evaluate(accusation ?? new FinalAccusation());
            bool recorded = state.TryRecordFinalEnding(result.EndingId);
            return new FinalAccusationResult(
                result.Ending,
                result.EndingId,
                result.Reason,
                recorded);
        }

        public static bool OpensD8Confession(string endingId)
        {
            string normalized = NormalizeEndingId(endingId);
            return normalized == CompleteEndingId || normalized == ConvenientEndingId;
        }

        public static string NormalizeEndingId(string endingId)
        {
            string value = endingId?.Trim() ?? string.Empty;
            return value.ToUpperInvariant() switch
            {
                "A" or "ENDING:A" or "ENDING_A_COMPLETE" => CompleteEndingId,
                "B" or "ENDING:B" or "ENDING_B_CONVENIENT_CULPRIT" =>
                    ConvenientEndingId,
                "C" or "ENDING:C" or "ENDING_C_WRONG_PERSON" => WrongPersonEndingId,
                "BAD" or "ENDING:BAD" or "ENDING_BAD_PANIC" or
                    "ENDING_BAD_INTEGRITY" => BadEndingId,
                _ => value
            };
        }

        public static string ToOfficialRoute(string endingId)
        {
            return NormalizeEndingId(endingId) switch
            {
                CompleteEndingId => "A",
                ConvenientEndingId => "B",
                WrongPersonEndingId => "C",
                BadEndingId => "Bad",
                _ => string.Empty
            };
        }

        private FinalAccusationResult Evaluate(FinalAccusation accusation)
        {
            // 전역 실패 조건은 지목 답안보다 먼저 판정한다.
            if (state.PublicAnxiety >= GameStateManager.MaxPercent)
            {
                return Create(
                    FinalEnding.Bad,
                    BadEndingId,
                    "승객 불안 100으로 최종 지목 전에 수사가 중단됐다.");
            }

            if (state.EvidenceIntegrity <= 0)
            {
                return Create(
                    FinalEnding.Bad,
                    BadEndingId,
                    "현장 보존도 0으로 직접 증거를 최종 논증에 사용할 수 없다.");
            }

            bool answersAreCorrect =
                accusation.Accused == AccusedPerson.Evelyn &&
                accusation.Location == MurderLocation.BallastControlAnnex &&
                accusation.Method == MurderMethod.NitrogenSuffocation &&
                accusation.Transport == BodyTransport.CeilingServiceRail &&
                accusation.DanielBelievedTarget == DanielTargetBelief.Misconception &&
                accusation.OrpheusDesign == OrpheusEventDesign.Evelyn;
            bool crimeCaseIsComplete = CrimeDeductions.All(state.HasUnlockedDeduction);

            if (!answersAreCorrect || !crimeCaseIsComplete)
            {
                return Create(
                    FinalEnding.WrongPerson,
                    WrongPersonEndingId,
                    "범인, 장소, 방법, 운반 경로 또는 핵심 논증이 완성되지 않았다.");
            }

            bool coverupCanBeDisclosed =
                state.HasUnlockedDeduction(CanonicalDeductionCatalog.PastEvent);
            if (accusation.DiscloseRichardCoverup && coverupCanBeDisclosed)
            {
                return Create(
                    FinalEnding.Complete,
                    CompleteEndingId,
                    "살인과 오르페우스 보험 사기 은폐를 모두 공개했다.");
            }

            return Create(
                FinalEnding.ConvenientCulprit,
                ConvenientEndingId,
                "살인은 해결했지만 리처드의 과거 은폐는 공개하지 않았다.");
        }

        private static FinalAccusationResult Create(
            FinalEnding ending,
            string endingId,
            string reason)
        {
            return new FinalAccusationResult(ending, endingId, reason, false);
        }

        private static FinalAccusationResult FromStoredEnding(string endingId)
        {
            var mappings = new Dictionary<string, FinalEnding>(StringComparer.Ordinal)
            {
                [CompleteEndingId] = FinalEnding.Complete,
                [ConvenientEndingId] = FinalEnding.ConvenientCulprit,
                [WrongPersonEndingId] = FinalEnding.WrongPerson,
                [BadEndingId] = FinalEnding.Bad,
                [LegacyIntegrityEndingId] = FinalEnding.Bad
            };
            string normalized = NormalizeEndingId(endingId);
            FinalEnding ending = mappings.TryGetValue(normalized, out FinalEnding stored)
                ? stored
                : FinalEnding.WrongPerson;
            return new FinalAccusationResult(
                ending,
                normalized,
                "이미 확정된 엔딩은 다시 판정하지 않는다.",
                false);
        }
    }
}
