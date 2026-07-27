using System;
using System.Collections.Generic;
using System.Linq;
using Wake.Core;

namespace Wake.Narrative
{
    public sealed class AmbientBarkRecord
    {
        public AmbientBarkRecord(
            string id,
            string speaker,
            string text,
            string emotion,
            string condition,
            string location)
        {
            Id = id;
            Speaker = speaker;
            Text = text;
            Emotion = emotion;
            Condition = condition;
            Location = location;
        }

        public string Id { get; }
        public string Speaker { get; }
        public string Text { get; }
        public string Emotion { get; }
        public string Condition { get; }
        public string Location { get; }
    }

    public static class AmbientBarkCatalog
    {
        private static readonly AmbientBarkRecord[] Entries =
        {
            B("ANX_LOW_01","PASSENGER_A","이 배의 유리 바닥은 멋지군요. 아래를 오래 보면 조금 어지럽지만요.","light","publicAnxiety<40"),
            B("ANX_LOW_02","PASSENGER_B","Hawthorne 회장이 직접 탄 항해라니, 홍보에 꽤 자신이 있나 봐요.","curious","publicAnxiety<40"),
            B("ANX_LOW_03","PASSENGER_C","오늘 파티 사진은 언제 공개된대요?","cheerful","publicAnxiety<40"),
            B("ANX_LOW_04","PASSENGER_D","서비스 로봇이 와인도 가져다주나요? 객실에 하나 불러 봐야겠어요.","amused","publicAnxiety<40"),
            B("ANX_LOW_05","CREW_ATTENDANT","좋은 항해 되십시오. 행사 일정은 단말기에서 확인하실 수 있습니다.","professional","publicAnxiety<40"),
            B("ANX_LOW_06","PASSENGER_E","바다가 이렇게 잔잔하면 배에 탄 느낌도 안 나네요.","relaxed","publicAnxiety<40"),
            B("ANX_LOW_07","PASSENGER_F","뉴스 라운지 커피는 형편없지만 전망은 좋습니다.","dry","publicAnxiety<40"),
            B("ANX_LOW_08","CREW_ENGINEER","천장 장비 아래에는 오래 서 계시지 마십시오.","professional","publicAnxiety<40"),
            B("ANX_MED_01","PASSENGER_A","급성 질환이라면서 왜 보안이 복도를 막았죠?","uneasy","publicAnxiety>=40 and publicAnxiety<70"),
            B("ANX_MED_02","PASSENGER_B","기자가 죽었다는 소문이 사실인가요?","whisper","publicAnxiety>=40 and publicAnxiety<70"),
            B("ANX_MED_03","PASSENGER_C","Richard Hawthorne가 파티에서 너무 취해 있었어요. 일부러 알리바이를 만든 걸까요?","suspicious","publicAnxiety>=40 and publicAnxiety<70"),
            B("ANX_MED_04","PASSENGER_D","밤새 천장에서 금속 소리가 났어요. 배가 원래 이런가요?","worried","publicAnxiety>=40 and publicAnxiety<70"),
            B("ANX_MED_05","CREW_ATTENDANT","승객 여러분은 지정 구역을 이용해 주십시오. 일부 서비스 통로가 점검 중입니다.","professional","publicAnxiety>=40 and publicAnxiety<70"),
            B("ANX_MED_06","PASSENGER_E","기사에 나온 Orpheus도 Hawthorne 배였죠?","concerned","publicAnxiety>=40 and publicAnxiety<70"),
            B("ANX_MED_07","PASSENGER_F","누가 진실을 말하는지 모르겠어요. 다들 너무 침착해요.","nervous","publicAnxiety>=40 and publicAnxiety<70"),
            B("ANX_MED_08","CREW_SECURITY","출입 기록 확인에 협조해 주십시오. 불편을 드려 죄송합니다.","firm","publicAnxiety>=40 and publicAnxiety<70"),
            B("ANX_HIGH_01","PASSENGER_A","우릴 다음 항구에 내려 줘요. 회사 발표는 못 믿겠습니다!","angry","publicAnxiety>=70"),
            B("ANX_HIGH_02","PASSENGER_B","제 객실 문도 누가 열 수 있는 거 아닌가요?","panicked","publicAnxiety>=70"),
            B("ANX_HIGH_03","PASSENGER_C","기자가 살해됐고 범인이 배 안에 있다잖아요!","fearful","publicAnxiety>=70"),
            B("ANX_HIGH_04","PASSENGER_D","제한구역을 왜 닫았죠? 증거를 없애는 거 아닙니까?","accusing","publicAnxiety>=70"),
            B("ANX_HIGH_05","CREW_ATTENDANT","통로를 비워 주십시오. 안전 절차에 따르지 않으면 객실로 안내하겠습니다.","urgent","publicAnxiety>=70"),
            B("ANX_HIGH_06","PASSENGER_E","화재경보도 가짜였다면 어떤 경보를 믿어야 하죠?","panicked","publicAnxiety>=70"),
            B("ANX_HIGH_07","PASSENGER_F","Hawthorne 가족을 체포하기 전엔 아무도 못 내리게 해야 해요.","angry","publicAnxiety>=70"),
            B("ANX_HIGH_08","CREW_SECURITY","밀지 마십시오! 계단과 승강기 앞에서 거리를 유지하세요.","commanding","publicAnxiety>=70"),
            B("REACT_D1_01","PASSENGER_A","파티가 끝나기도 전에 의무실로 사람이 실려 갔다더군요.","whisper","chapter=Day2","ATRIUM"),
            B("REACT_D2_01","CREW_ATTENDANT","Horizon Room은 예약이 중단되었습니다. 다른 라운지를 이용해 주세요.","professional","chapter>=Day2","HORIZON"),
            B("REACT_D3_01","PASSENGER_B","Daniel Mercer의 기사를 읽었어요. 삭제되기 전에 저장해 뒀습니다.","uneasy","flag:scheduled_article","NEWS_LOUNGE"),
            B("REACT_D4_01","PASSENGER_C","보안 책임자까지 사고를 당했다면 누가 우릴 지키죠?","worried","flag:marcus_accident"),
            B("REACT_D5_01","CREW_ATTENDANT","서비스 로봇은 임시 중단되었습니다. 객실 서비스가 지연될 수 있습니다.","professional","chapter=Day5","VIP_LOUNGE"),
            B("REACT_D6_01","CREW_ENGINEER","행사 레일은 조사 종료까지 전원 차단입니다.","firm","flag:corpse_moved_by_rail","SERVICE_RAIL"),
            B("REACT_D7_01","PASSENGER_D","아침 화재경보 때 아무 연기도 없었어요. 누가 장난친 건가요?","suspicious","flag:false_alarm_evelyn_device","VAULT"),
            B("REACT_D8_01","PASSENGER_E","탐정이 모두를 Horizon Room에 불렀대요. 드디어 끝나는 건가요?","anxious","scene=D8-01","HORIZON")
        };

        public static IReadOnlyList<AmbientBarkRecord> All => Entries;

        public static IReadOnlyList<AmbientBarkRecord> GetAvailable(
            string locationCode,
            GameStateManager state,
            int maximum = 3)
        {
            string location = locationCode?.Trim().ToUpperInvariant() ?? "";
            AmbientBarkRecord[] available = Entries
                .Where(entry =>
                    (entry.Location == "ANY" || entry.Location == location) &&
                    Matches(entry.Condition, state))
                .OrderByDescending(entry => entry.Id.StartsWith("REACT_"))
                .ThenBy(entry => StableOrder(entry.Id, location, state?.Day ?? 1))
                .ToArray();

            var result = new List<AmbientBarkRecord>();
            foreach (AmbientBarkRecord entry in available)
            {
                if (result.Any(item => item.Speaker == entry.Speaker))
                    continue;
                result.Add(entry);
                if (result.Count >= maximum)
                    break;
            }
            return result;
        }

        private static bool Matches(string condition, GameStateManager state)
        {
            int anxiety = state?.PublicAnxiety ?? 15;
            int day = state?.Day ?? 1;
            if (condition == "publicAnxiety<40") return anxiety < 40;
            if (condition == "publicAnxiety>=40 and publicAnxiety<70")
                return anxiety >= 40 && anxiety < 70;
            if (condition == "publicAnxiety>=70") return anxiety >= 70;
            if (condition == "chapter=Day2") return day == 2;
            if (condition == "chapter>=Day2") return day >= 2;
            if (condition == "chapter=Day5") return day == 5;
            if (condition.StartsWith("flag:"))
                return state?.HasFlag(condition.Substring(5)) == true;
            if (condition.StartsWith("scene="))
            {
                string scene = condition.Substring(6);
                return state?.HasCompletedScene(scene) == true ||
                       state?.IsProductionSceneUnlocked(scene) == true;
            }
            return false;
        }

        private static int StableOrder(string id, string location, int day)
        {
            unchecked
            {
                int value = 17;
                foreach (char character in id + location)
                    value = value * 31 + character;
                return Math.Abs(value + day * 101);
            }
        }

        private static AmbientBarkRecord B(
            string id,
            string speaker,
            string text,
            string emotion,
            string condition,
            string location = "ANY") =>
            new(id, speaker, text, emotion, condition, location);
    }
}
