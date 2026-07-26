using System;
using System.Collections.Generic;
using System.Linq;
using Wake.Puzzles;

namespace Wake.Core
{
    public enum AccusedPerson
    {
        Unknown,
        Evelyn,
        Richard
    }

    public enum MurderLocation
    {
        Unknown,
        HorizonRoom,
        BallastControlAnnex
    }

    public enum MurderMethod
    {
        Unknown,
        BluntForce,
        NitrogenSuffocation
    }

    public enum BodyTransport
    {
        Unknown,
        Exterior,
        CeilingServiceRail
    }

    public enum DanielTargetBelief
    {
        Unknown,
        Evelyn,
        Richard
    }

    public enum OrpheusEventDesign
    {
        Unknown,
        Accident,
        InsuranceFraud
    }

    public enum FinalEnding
    {
        Complete,
        ConvenientCulprit,
        WrongPerson,
        BadPanic,
        BadIntegrity
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
        public const string IntegrityEndingId = "ending_bad_integrity";

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
            return endingId == CompleteEndingId || endingId == ConvenientEndingId;
        }

        private FinalAccusationResult Evaluate(FinalAccusation accusation)
        {
            // 전역 실패 조건은 지목 답안보다 먼저 판정한다.
            if (state.PublicAnxiety >= GameStateManager.MaxPercent)
            {
                return Create(
                    FinalEnding.BadPanic,
                    PanicEndingId,
                    "승객 불안 100으로 최종 지목 전에 수사가 중단됐다.");
            }

            if (state.EvidenceIntegrity <= 0)
            {
                return Create(
                    FinalEnding.BadIntegrity,
                    IntegrityEndingId,
                    "현장 보존도 0으로 직접 증거를 최종 논증에 사용할 수 없다.");
            }

            bool answersAreCorrect =
                accusation.Accused == AccusedPerson.Evelyn &&
                accusation.Location == MurderLocation.BallastControlAnnex &&
                accusation.Method == MurderMethod.NitrogenSuffocation &&
                accusation.Transport == BodyTransport.CeilingServiceRail &&
                accusation.DanielBelievedTarget == DanielTargetBelief.Richard &&
                accusation.OrpheusDesign == OrpheusEventDesign.InsuranceFraud;
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
                [PanicEndingId] = FinalEnding.BadPanic,
                [IntegrityEndingId] = FinalEnding.BadIntegrity
            };
            FinalEnding ending = mappings.TryGetValue(endingId, out FinalEnding stored)
                ? stored
                : FinalEnding.WrongPerson;
            return new FinalAccusationResult(
                ending,
                endingId,
                "이미 확정된 엔딩은 다시 판정하지 않는다.",
                false);
        }
    }
}
