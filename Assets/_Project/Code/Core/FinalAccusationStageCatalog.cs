using System;
using System.Collections.Generic;
using System.Linq;

namespace Wake.Core
{
    public enum FinalAccusationStage
    {
        Culprit = 1,
        MurderLocation = 2,
        CauseOfDeath = 3,
        BodyTransport = 4,
        MurderMotive = 5,
        OrpheusMastermind = 6
    }

    public sealed class FinalAccusationOptionDefinition
    {
        public FinalAccusationOptionDefinition(
            string choiceId,
            string label,
            int enumValue,
            bool isCorrect)
        {
            ChoiceId = choiceId;
            Label = label;
            EnumValue = enumValue;
            IsCorrect = isCorrect;
        }

        public string ChoiceId { get; }
        public string Label { get; }
        public int EnumValue { get; }
        public bool IsCorrect { get; }
    }

    public sealed class FinalAccusationStageDefinition
    {
        public FinalAccusationStageDefinition(
            FinalAccusationStage stage,
            string prompt,
            params FinalAccusationOptionDefinition[] options)
        {
            Stage = stage;
            Prompt = prompt;
            Options = options ?? Array.Empty<FinalAccusationOptionDefinition>();
        }

        public FinalAccusationStage Stage { get; }
        public string Prompt { get; }
        public IReadOnlyList<FinalAccusationOptionDefinition> Options { get; }
        public FinalAccusationOptionDefinition CorrectOption =>
            Options.Single(option => option.IsCorrect);
    }

    public static class FinalAccusationStageCatalog
    {
        private static FinalAccusationOptionDefinition O(
            string id,
            string label,
            int value,
            bool correct = false) =>
            new(id, label, value, correct);

        private static readonly FinalAccusationStageDefinition[] Entries =
        {
            new(
                FinalAccusationStage.Culprit,
                "다니엘 머서를 살해한 범인은 누구인가?",
                O("D8-01_A1_EVELYN", "이블린 쇼",
                    (int)AccusedPerson.Evelyn, true),
                O("D8-01_A1_RICHARD", "Richard Hawthorne",
                    (int)AccusedPerson.Richard),
                O("D8-01_A1_CLAIRE", "Claire Hawthorne",
                    (int)AccusedPerson.Claire),
                O("D8-01_A1_MARCUS", "Marcus Vale",
                    (int)AccusedPerson.Marcus)),
            new(
                FinalAccusationStage.MurderLocation,
                "실제 살해 장소는 어디인가?",
                O("D8-01_A2_BALLAST", "Ballast Control Annex",
                    (int)MurderLocation.BallastControlAnnex, true),
                O("D8-01_A2_HORIZON", "호라이즌 룸",
                    (int)MurderLocation.HorizonRoom),
                O("D8-01_A2_VAULT", "금고",
                    (int)MurderLocation.Vault),
                O("D8-01_A2_SERVICE", "서비스 구역",
                    (int)MurderLocation.ServiceArea)),
            new(
                FinalAccusationStage.CauseOfDeath,
                "직접 사인은 무엇인가?",
                O("D8-01_A3_SUFFOCATION", "질소 질식",
                    (int)MurderMethod.NitrogenSuffocation, true),
                O("D8-01_A3_STAB", "자상",
                    (int)MurderMethod.Stabbing),
                O("D8-01_A3_DROWNING", "익사",
                    (int)MurderMethod.Drowning),
                O("D8-01_A3_DRUG", "약물 중독",
                    (int)MurderMethod.Drug)),
            new(
                FinalAccusationStage.BodyTransport,
                "시신은 호라이즌 룸으로 어떻게 이동했는가?",
                O("D8-01_A4_RAIL", "천장 서비스 레일",
                    (int)BodyTransport.CeilingServiceRail, true),
                O("D8-01_A4_ROBOT", "서비스 로봇",
                    (int)BodyTransport.Robot),
                O("D8-01_A4_ELEVATOR", "승객용 엘리베이터",
                    (int)BodyTransport.Elevator),
                O("D8-01_A4_MANUAL", "외부 발판 수동 운반",
                    (int)BodyTransport.Manual)),
            new(
                FinalAccusationStage.MurderMotive,
                "이블린이 다니엘을 제거한 핵심 동기는 무엇인가?",
                O("D8-01_A5_MISCONCEPTION",
                    "Richard를 범인으로 믿게 만든 오판",
                    (int)DanielTargetBelief.Misconception, true),
                O("D8-01_A5_FUNDS", "비자금",
                    (int)DanielTargetBelief.CorporateFunds),
                O("D8-01_A5_REVENGE", "개인적 복수",
                    (int)DanielTargetBelief.Revenge),
                O("D8-01_A5_RANSOM", "금전 요구",
                    (int)DanielTargetBelief.Ransom)),
            new(
                FinalAccusationStage.OrpheusMastermind,
                "MV Orpheus 사건의 설계자는 누구인가?",
                O("D8-01_A6_EVELYN", "이블린 쇼",
                    (int)OrpheusEventDesign.Evelyn, true),
                O("D8-01_A6_RICHARD", "Richard Hawthorne",
                    (int)OrpheusEventDesign.Richard),
                O("D8-01_A6_JULIAN", "Julian Hawthorne",
                    (int)OrpheusEventDesign.Julian),
                O("D8-01_A6_THOMAS", "Thomas Reed",
                    (int)OrpheusEventDesign.Thomas))
        };

        public static IReadOnlyList<FinalAccusationStageDefinition> All => Entries;

        public static bool TryGet(
            FinalAccusationStage stage,
            out FinalAccusationStageDefinition definition)
        {
            definition = Entries.FirstOrDefault(item => item.Stage == stage);
            return definition != null;
        }
    }
}
