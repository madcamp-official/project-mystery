using System;
using System.Collections.Generic;
using System.Linq;
using Wake.Core;
using Wake.Exploration;

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
        private static readonly string[] LocationCodes =
        {
            "PORT", "GANGWAY", "RICHARD_SUITE", "VIP_LOUNGE",
            "OPEN_DECK", "BALLROOM", "DINING", "PROMENADE", "HORIZON",
            "ATRIUM", "NEWS_LOUNGE", "SECURITY", "SERVICE_RAIL",
            "MEDBAY", "BALLAST_CONTROL_ANNEX", "ENGINE_CONTROL",
            "CREW_STAIRS", "VAULT", "ARCHIVE", "LAUNDRY", "SERVICE_HUB",
            "STABILIZERS", "BALLAST_TANKS", "GENERATOR", "WORKSHOP"
        };

        private static readonly AmbientBarkRecord[] BaselineEntries =
        {
            B("PORT_ATTENDANT", "DOCK_PORTER",
                "승선 명단을 확인했습니다. 수하물은 객실로 먼저 보내 드리겠습니다.",
                "professional", "always", "PORT"),
            B("PORT_PHOTOGRAPHER", "PASSENGER_A",
                "출항 전 선체가 가장 잘 보이는 곳이 여기예요. 사진 한 장 남기시겠어요?",
                "cheerful", "always", "PORT"),
            B("PORT_DELAY", "DOCK_PORTER",
                "하선 요청이 몰리고 있습니다. 선장의 허가 없이는 출입문을 열 수 없습니다.",
                "urgent", "publicAnxiety>=70", "PORT"),
            B("PORT_ATTENDANT_MID", "DOCK_PORTER",
                "선적 목록에 조사 협조 물품 표시가 늘었습니다. 하선 심사도 그만큼 길어지고 있어요.",
                "uneasy", "chapter>=2 and chapter<=4", "PORT"),
            B("PORT_ATTENDANT_LATE", "DOCK_PORTER",
                "이번 항차 이야기가 항구까지 퍼졌더군요. 손님들 질문이 짐 확인보다 그쪽에 더 많습니다.",
                "dry", "chapter>=5", "PORT"),

            B("GANGWAY_SECURITY", "CREW_SECURITY",
                "승선표와 신분증을 함께 보여 주십시오. 이 통로의 출입은 모두 기록됩니다.",
                "firm", "always", "GANGWAY"),
            B("GANGWAY_PASSENGER", "PASSENGER_D",
                "제 카메라 가방도 다시 검사하더군요. 평소보다 경비가 삼엄한 것 같아요.",
                "uneasy", "always", "GANGWAY"),
            B("GANGWAY_SECURITY_MID", "CREW_SECURITY",
                "출입 기록을 이중으로 대조하라는 지시가 내려왔습니다. 평소보다 시간이 걸릴 겁니다.",
                "firm", "chapter>=2 and chapter<=4", "GANGWAY"),
            B("GANGWAY_SECURITY_LATE", "PASSENGER_D",
                "이 통로 경비가 처음보다 갑절은 늘었어요. 다들 말은 안 해도 이유는 알죠.",
                "uneasy", "chapter>=5", "GANGWAY"),

            B("RICHARD_SUITE_SECURITY", "SUITE_STEWARD",
                "호손 씨의 객실은 조사 중입니다. 허가된 인원만 들어갈 수 있습니다.",
                "firm", "always", "RICHARD_SUITE"),
            B("RICHARD_SUITE_RUMOR", "SUITE_STEWARD",
                "문 앞에 모이지 마십시오. 객실 내부 상황은 공식 발표 전까지 비공개입니다.",
                "commanding", "publicAnxiety>=70", "RICHARD_SUITE"),
            B("RICHARD_SUITE_MID", "SUITE_STEWARD",
                "회장님은 요즘 문을 걸어 잠그고 서류만 보십니다. 식사도 방으로 들여보내라 하셨어요.",
                "quiet", "chapter>=2 and chapter<=4", "RICHARD_SUITE"),
            B("RICHARD_SUITE_LATE", "SUITE_STEWARD",
                "기자분들 문의가 늘어 응대 인력을 더 붙였습니다. 회장님은 아직 공식 입장이 없으십니다.",
                "professional", "chapter>=5", "RICHARD_SUITE"),

            B("VIP_ATTENDANT", "VIP_HOST",
                "VIP 라운지는 예약 명단제로 운영됩니다. 원하시면 빈 좌석을 확인해 드리죠.",
                "professional", "always", "VIP_LOUNGE"),
            B("VIP_GAMBLER", "PASSENGER_B",
                "저쪽 카드 테이블은 밤 열 시에 열립니다. 판돈보다 소문이 더 크게 오가죠.",
                "amused", "always", "VIP_LOUNGE"),
            B("VIP_ROBOT", "VIP_HOST",
                "서비스 로봇은 임시 중단되었습니다. 객실 서비스가 지연될 수 있습니다.",
                "professional", "chapter=5", "VIP_LOUNGE"),
            B("VIP_ATTENDANT_MID", "PASSENGER_B",
                "카드 테이블보다 요즘은 다들 그 이야기뿐이에요. 판돈 이야기가 쏙 들어갔죠.",
                "dry", "chapter>=2 and chapter<=4", "VIP_LOUNGE"),
            B("VIP_ATTENDANT_LATE", "VIP_HOST",
                "예약 취소가 이어지고 있습니다. 남으신 분들도 라운지보다 방을 더 찾으세요.",
                "professional", "chapter>=5", "VIP_LOUNGE"),

            B("OPEN_DECK_NATURALIST", "PASSENGER_E",
                "바람 방향이 조금 바뀌었어요. 망원경으로 보면 항로 오른편에 돌고래 떼가 있습니다.",
                "relaxed", "always", "OPEN_DECK"),
            B("OPEN_DECK_SECURITY", "CREW_SECURITY",
                "난간 밖으로 몸을 내밀지 마십시오. 야간에는 갑판 일부가 폐쇄됩니다.",
                "firm", "always", "OPEN_DECK"),
            B("OPEN_DECK_SECURITY_MID", "CREW_SECURITY",
                "야간 갑판 통제 구역을 넓혔습니다. 안내선 밖으로는 나가지 말아 주십시오.",
                "firm", "chapter>=2 and chapter<=4", "OPEN_DECK"),
            B("OPEN_DECK_NATURALIST_LATE", "PASSENGER_E",
                "돌고래는커녕 요즘은 순찰 도는 것만 보여요. 다들 여기서도 목소리를 낮추네요.",
                "uneasy", "chapter>=5", "OPEN_DECK"),

            B("BALLROOM_SINGER", "BALLROOM_MUSICIAN",
                "악단은 리허설을 마쳤어요. 무대 왼쪽 마이크가 자꾸 잡음을 내긴 하지만요.",
                "light", "always", "BALLROOM"),
            B("BALLROOM_ATTENDANT", "CREW_ATTENDANT",
                "오늘 무도회 좌석표는 입구 단말기에 있습니다. 중앙 통로는 비워 주세요.",
                "professional", "always", "BALLROOM"),
            B("BALLROOM_SINGER_MID", "BALLROOM_MUSICIAN",
                "무대는 다시 치웠습니다. 오늘은 음악보다 통제선이 먼저 보이네요.",
                "uneasy", "chapter>=2 and chapter<=4", "BALLROOM"),
            B("BALLROOM_SINGER_LATE", "CREW_ATTENDANT",
                "다들 이 방보다 회의실 쪽 소식을 더 궁금해합니다. 좌석표 확인하는 분도 줄었어요.",
                "uneasy", "chapter>=5", "BALLROOM"),

            B("DINING_ATTENDANT", "DINING_SOMMELIER",
                "저녁 식사는 두 차례로 나뉩니다. 창가 자리는 첫 번째 시간대가 모두 찼습니다.",
                "professional", "always", "DINING"),
            B("DINING_GUEST", "PASSENGER_C",
                "와인 병 봉인이 조금 이상했어요. 소믈리에에게 확인해 달라고 했습니다.",
                "curious", "always", "DINING"),
            B("DINING_ATTENDANT_MID", "DINING_SOMMELIER",
                "요즘은 식사 시간을 줄여 달라는 분이 많습니다. 자리 배치도 그래서 조용한 구석부터 채웁니다.",
                "professional", "chapter>=2 and chapter<=4", "DINING"),
            B("DINING_GUEST_LATE", "PASSENGER_C",
                "다들 식사보다 신문 얘기가 먼저예요. 와인 봉인 이야기는 이제 아무도 안 물어요.",
                "dry", "chapter>=5", "DINING"),

            B("PROMENADE_PHOTOGRAPHER", "PASSENGER_A",
                "유리 복도 끝에서 항구 불빛이 프레임처럼 잡혀요. 밤에 다시 와 봐야겠어요.",
                "cheerful", "always", "PROMENADE"),
            B("PROMENADE_REPORTER", "PASSENGER_D",
                "이 복도 천장에서 새벽마다 금속 끌리는 소리가 납니다. 시간도 적어 뒀어요.",
                "worried", "always", "PROMENADE"),
            B("PROMENADE_PHOTOGRAPHER_MID", "PASSENGER_A",
                "요즘은 사진보다 순찰대와 마주치는 일이 더 많아요. 복도가 예전만큼 한가하지 않습니다.",
                "uneasy", "chapter>=2 and chapter<=4", "PROMENADE"),
            B("PROMENADE_REPORTER_LATE", "PASSENGER_D",
                "그 금속 끌리는 소리는 이제 아무도 신경 안 써요. 다들 더 큰 소문에 정신이 팔렸죠.",
                "dry", "chapter>=5", "PROMENADE"),

            B("HORIZON_NATURALIST", "PASSENGER_E",
                "수평선을 보기엔 이 방이 가장 좋습니다. 유리 반사 때문에 사진은 조금 어렵지만요.",
                "relaxed", "always", "HORIZON"),
            B("HORIZON_ATTENDANT", "CREW_ATTENDANT",
                "창가 테이블은 예약석입니다. 중앙 좌석은 자유롭게 이용하셔도 됩니다.",
                "professional", "always", "HORIZON"),
            B("HORIZON_CLOSED", "CREW_ATTENDANT",
                "호라이즌 룸은 예약이 중단되었습니다. 다른 라운지를 이용해 주세요.",
                "professional", "chapter>=2", "HORIZON"),
            B("HORIZON_FINALE", "PASSENGER_E",
                "탐정이 모두를 이 방에 불렀대요. 드디어 끝나는 건가요?",
                "anxious", "scene=D8-01", "HORIZON"),
            B("HORIZON_NATURALIST_LATE", "PASSENGER_E",
                "이 방에서 수평선을 보는 분보다 서류를 든 분이 더 많아졌어요.",
                "quiet", "chapter>=5", "HORIZON"),

            B("ATRIUM_PHOTOGRAPHER", "PASSENGER_A",
                "이 유리 바닥은 아래 갑판까지 보여요. 중앙 문양 위가 사진이 가장 잘 나옵니다.",
                "light", "always", "ATRIUM"),
            B("ATRIUM_ATTENDANT", "ATRIUM_GUIDE",
                "행사 일정과 선내 안내는 저쪽 안내 단말기에서 확인하실 수 있습니다.",
                "professional", "always", "ATRIUM"),
            B("ATRIUM_MEDBAY_RUMOR", "PASSENGER_A",
                "파티가 끝나기도 전에 의무실로 사람이 실려 갔다더군요.",
                "whisper", "chapter=2", "ATRIUM"),
            B("ATRIUM_PHOTOGRAPHER_MID", "PASSENGER_A",
                "유리 바닥 사진 찍는 분보다 안내원 붙잡고 묻는 분이 더 많아졌어요.",
                "curious", "chapter>=2 and chapter<=4", "ATRIUM"),
            B("ATRIUM_ATTENDANT_LATE", "ATRIUM_GUIDE",
                "행사 안내보다 문의 응대가 더 늘었습니다. 오늘 일정은 대부분 취소됐어요.",
                "professional", "chapter>=5", "ATRIUM"),

            B("NEWS_COFFEE", "PASSENGER_F",
                "뉴스 라운지 커피는 형편없지만 통신 수신은 선내에서 가장 빠릅니다.",
                "dry", "always", "NEWS_LOUNGE"),
            B("NEWS_REPORTER", "PASSENGER_D",
                "벽면 송고 단말기에 수정 기록이 남아 있어요. 기사가 몇 번이나 바뀐 모양입니다.",
                "curious", "always", "NEWS_LOUNGE"),
            B("NEWS_ARTICLE", "PASSENGER_B",
                "다니엘 머서의 기사를 읽었어요. 삭제되기 전에 저장해 뒀습니다.",
                "uneasy", "flag:scheduled_article", "NEWS_LOUNGE"),
            B("NEWS_COFFEE_MID", "PASSENGER_F",
                "요즘은 커피보다 송고 단말기 앞 줄이 더 길어요. 다들 최신 기사부터 확인하죠.",
                "dry", "chapter>=2 and chapter<=4", "NEWS_LOUNGE"),
            B("NEWS_REPORTER_LATE", "PASSENGER_B",
                "다니엘 머서 이름으로 된 기사가 다시 인용되고 있어요. 편집부도 조심스러워합니다.",
                "uneasy", "chapter>=5", "NEWS_LOUNGE"),

            B("SECURITY_OFFICER", "SECURITY_OPERATOR",
                "보안실 장비에는 손대지 마십시오. 출입 기록 열람은 수사 담당자만 가능합니다.",
                "firm", "always", "SECURITY"),
            B("SECURITY_ANXIETY", "SECURITY_OPERATOR",
                "신원 확인이 끝날 때까지 이 구역을 떠나지 마십시오.",
                "commanding", "publicAnxiety>=70", "SECURITY"),
            B("SECURITY_OFFICER_MID", "SECURITY_OPERATOR",
                "교대 인원을 늘렸습니다. 신원 미확인자는 이 구역에 오래 머물 수 없습니다.",
                "firm", "chapter>=2 and chapter<=4", "SECURITY"),
            B("SECURITY_OFFICER_LATE", "SECURITY_OPERATOR",
                "출입 기록을 매시간 대조합니다. 최근엔 사소한 불일치도 그냥 넘기지 않습니다.",
                "commanding", "chapter>=5", "SECURITY"),

            B("SERVICE_RAIL_ENGINEER", "RAIL_TECHNICIAN",
                "이 레일은 식자재와 장비 운반용입니다. 운행등이 켜지면 선 밖으로 물러나십시오.",
                "professional", "always", "SERVICE_RAIL"),
            B("SERVICE_RAIL_LOCKDOWN", "RAIL_TECHNICIAN",
                "운반 레일은 조사 종료까지 전원 차단입니다.",
                "firm", "flag:corpse_moved_by_rail", "SERVICE_RAIL"),
            B("SERVICE_RAIL_ENGINEER_MID", "RAIL_TECHNICIAN",
                "레일 운행 기록을 전부 다시 남기고 있습니다. 예전보다 절차가 늘었어요.",
                "professional", "chapter>=2 and chapter<=4", "SERVICE_RAIL"),
            B("SERVICE_RAIL_ENGINEER_LATE", "RAIL_TECHNICIAN",
                "이 레일 얘기는 이제 저희끼리도 잘 안 꺼냅니다. 기록만 넘기고 맙니다.",
                "quiet", "chapter>=5", "SERVICE_RAIL"),

            B("MEDBAY_ATTENDANT", "SHIP_MEDIC",
                "의무실은 환자 안정을 위해 면회가 제한됩니다. 증상은 접수대에 먼저 말씀해 주세요.",
                "professional", "always", "MEDBAY"),
            B("MEDBAY_SECURITY", "CREW_SECURITY",
                "진료 기록은 의료진 승인 없이는 반출할 수 없습니다.",
                "firm", "publicAnxiety>=40 and publicAnxiety<70", "MEDBAY"),
            B("MEDBAY_ATTENDANT_MID", "SHIP_MEDIC",
                "면회 제한이 더 엄격해졌습니다. 접수대에서 방문 사유부터 확인합니다.",
                "professional", "chapter>=2 and chapter<=4", "MEDBAY"),
            B("MEDBAY_ATTENDANT_LATE", "SHIP_MEDIC",
                "요즘은 진료보다 기록 요청이 더 많습니다. 승인 없이는 열람할 수 없다고 매번 안내드려요.",
                "clinical", "chapter>=5", "MEDBAY"),

            B("BALLAST_ANNEX_ENGINEER", "BALLAST_CONTROLLER",
                "이곳은 밸러스트 제어 보조실입니다. 파란 밸브는 원격 계통과 연결돼 있습니다.",
                "professional", "always", "BALLAST_CONTROL_ANNEX"),
            B("BALLAST_ANNEX_ENGINEER_MID", "BALLAST_CONTROLLER",
                "출입자 명단을 매번 새로 받고 있습니다. 절차가 늘어난 건 다들 이해해 주십니다.",
                "professional", "chapter>=2 and chapter<=4", "BALLAST_CONTROL_ANNEX"),
            B("BALLAST_ANNEX_ENGINEER_LATE", "BALLAST_CONTROLLER",
                "밸브 점검 기록을 다시 정리해 달라는 요청이 왔습니다. 예전 기록까지 전부요.",
                "focused", "chapter>=5", "BALLAST_CONTROL_ANNEX"),

            B("ENGINE_CONTROL_ENGINEER", "CHIEF_ENGINEER",
                "주기관 출력은 정상입니다. 우측 계기판의 흔들림은 파도 보정값이에요.",
                "professional", "always", "ENGINE_CONTROL"),
            B("ENGINE_CONTROL_ENGINEER_MID", "CHIEF_ENGINEER",
                "출력 기록을 하루 단위로 제출하고 있습니다. 평소엔 없던 절차입니다.",
                "professional", "chapter>=2 and chapter<=4", "ENGINE_CONTROL"),
            B("ENGINE_CONTROL_ENGINEER_LATE", "CHIEF_ENGINEER",
                "기관실 얘기도 이젠 조사 대상이더군요. 저희도 기록만 성실히 남길 뿐입니다.",
                "matter_of_fact", "chapter>=5", "ENGINE_CONTROL"),

            B("CREW_STAIRS_SECURITY", "CREW_SECURITY",
                "이 계단은 승무원 전용입니다. 허가증이 없으면 중앙 승강기를 이용하십시오.",
                "firm", "always", "CREW_STAIRS"),
            B("CREW_STAIRS_SECURITY_MID", "CREW_SECURITY",
                "이 계단 출입 기록도 전부 대조 대상입니다. 허가증을 꼭 챙겨 주십시오.",
                "firm", "chapter>=2 and chapter<=4", "CREW_STAIRS"),
            B("CREW_STAIRS_SECURITY_LATE", "CREW_SECURITY",
                "이 계단 얘기는 저희끼리도 조심스럽습니다. 사고 이후로 다들 예민해졌어요.",
                "uneasy", "chapter>=5", "CREW_STAIRS"),

            B("VAULT_SECURITY", "CREW_SECURITY",
                "보관고는 이중 인증 구역입니다. 경보 이력까지 모두 보안실에 전송됩니다.",
                "firm", "always", "VAULT"),
            B("VAULT_FALSE_ALARM", "CREW_SECURITY",
                "아침 경보는 발신 장치가 특정됐습니다. 보관고 화재는 없었습니다.",
                "firm", "flag:false_alarm_evelyn_device", "VAULT"),
            B("VAULT_SECURITY_MID", "CREW_SECURITY",
                "이중 인증 기록을 매일 보안실로 넘기고 있습니다. 예전엔 주간 단위였습니다.",
                "firm", "chapter>=2 and chapter<=4", "VAULT"),
            B("VAULT_SECURITY_LATE", "CREW_SECURITY",
                "보관고 근처엔 이제 혼자 오는 분이 없습니다. 다들 둘씩 짝지어 다니시더군요.",
                "uneasy", "chapter>=5", "VAULT"),

            B("ARCHIVE_SECURITY", "ARCHIVIST",
                "문서 보관실에서는 촬영이 금지됩니다. 열람한 상자는 원래 선반에 돌려놓으십시오.",
                "firm", "always", "ARCHIVE"),
            B("ARCHIVE_SECURITY_MID", "ARCHIVIST",
                "열람 신청서를 다시 받고 있습니다. 사유란을 비워 두시면 안내해 드릴 수 없어요.",
                "firm", "chapter>=2 and chapter<=4", "ARCHIVE"),
            B("ARCHIVE_SECURITY_LATE", "ARCHIVIST",
                "요즘은 옛날 기록 요청이 부쩍 늘었습니다. 다들 뭘 찾는지는 안 여쭤봅니다.",
                "dry", "chapter>=5", "ARCHIVE"),

            B("LAUNDRY_ATTENDANT", "LAUNDRY_SUPERVISOR",
                "세탁물 투입구마다 객실 번호가 표시돼 있습니다. 승무원 제복은 별도 라인으로 갑니다.",
                "professional", "always", "LAUNDRY"),
            B("LAUNDRY_ATTENDANT_MID", "LAUNDRY_SUPERVISOR",
                "제복 세탁 라인에 표식 확인 절차가 하나 늘었습니다. 번거로워도 양해 부탁드려요.",
                "professional", "chapter>=2 and chapter<=4", "LAUNDRY"),
            B("LAUNDRY_ATTENDANT_LATE", "LAUNDRY_SUPERVISOR",
                "요즘은 세탁물보다 이야기가 더 많이 돌아요. 저는 투입구 번호만 볼 뿐입니다.",
                "dry", "chapter>=5", "LAUNDRY"),

            B("SERVICE_HUB_ATTENDANT", "ROBOTICS_TECH",
                "서비스 로봇은 이 허브에서 충전됩니다. 이동 경로를 막지 않도록 주의해 주세요.",
                "professional", "always", "SERVICE_HUB"),
            B("SERVICE_HUB_ENGINEER", "ROBOTICS_TECH",
                "두 번째 충전 레일의 전압이 불안정합니다. 노란 표시 안으로 들어오지 마십시오.",
                "professional", "publicAnxiety>=40 and publicAnxiety<70", "SERVICE_HUB"),
            B("SERVICE_HUB_ATTENDANT_MID", "ROBOTICS_TECH",
                "충전 순번을 다시 정리하고 있습니다. 로봇 한 대가 아직 회수되지 않았거든요.",
                "focused", "chapter>=2 and chapter<=4", "SERVICE_HUB"),
            B("SERVICE_HUB_ATTENDANT_LATE", "ROBOTICS_TECH",
                "그 로봇 얘기는 이제 여기서도 꺼내기 조심스럽습니다. 점검만 계속하고 있어요.",
                "uneasy", "chapter>=5", "SERVICE_HUB"),

            B("STABILIZERS_ENGINEER", "CREW_ENGINEER",
                "안정기 진동은 허용 범위입니다. 바닥의 흰 선보다 안쪽에는 서지 마십시오.",
                "professional", "always", "STABILIZERS"),
            B("STABILIZERS_ENGINEER_MID", "CREW_ENGINEER",
                "점검 주기를 반으로 줄였습니다. 흰 선 안쪽엔 여전히 서지 말아 주세요.",
                "professional", "chapter>=2 and chapter<=4", "STABILIZERS"),
            B("STABILIZERS_ENGINEER_LATE", "CREW_ENGINEER",
                "안정기는 정상입니다. 다만 요즘은 저희도 기록을 두 번씩 확인합니다.",
                "matter_of_fact", "chapter>=5", "STABILIZERS"),

            B("BALLAST_TANKS_ENGINEER", "BALLAST_CONTROLLER",
                "수위는 자동 조절 중입니다. 젖은 발판은 미끄러우니 난간을 잡으세요.",
                "professional", "always", "BALLAST_TANKS"),
            B("BALLAST_TANKS_ENGINEER_MID", "BALLAST_CONTROLLER",
                "수위 기록을 매 교대마다 남기고 있습니다. 예전보다 촘촘해졌어요.",
                "professional", "chapter>=2 and chapter<=4", "BALLAST_TANKS"),
            B("BALLAST_TANKS_ENGINEER_LATE", "BALLAST_CONTROLLER",
                "여기까지 조사가 내려올 줄은 몰랐습니다. 기록은 늘 그대로였는데 말이죠.",
                "uneasy", "chapter>=5", "BALLAST_TANKS"),

            B("GENERATOR_ENGINEER", "CREW_ENGINEER",
                "발전기실에서는 보호구를 착용해야 합니다. 녹색등이 꺼지면 즉시 밖으로 나가세요.",
                "professional", "always", "GENERATOR"),
            B("GENERATOR_ENGINEER_MID", "CREW_ENGINEER",
                "출입 인원을 하나씩 기록하고 있습니다. 보호구 착용도 다시 한번 확인해 주세요.",
                "professional", "chapter>=2 and chapter<=4", "GENERATOR"),
            B("GENERATOR_ENGINEER_LATE", "CREW_ENGINEER",
                "발전기 자체는 문제없습니다. 다만 요즘은 누가 언제 들어왔는지가 더 중요해졌어요.",
                "matter_of_fact", "chapter>=5", "GENERATOR"),

            B("WORKSHOP_ENGINEER", "WORKSHOP_MACHINIST",
                "반출 공구는 전부 장부에 기록합니다. 작업대 위 도면은 현재 수리 중인 안정기용이에요.",
                "professional", "always", "WORKSHOP"),
            B("WORKSHOP_ENGINEER_MID", "WORKSHOP_MACHINIST",
                "공구 반출 장부를 매일 마감하고 있습니다. 이전엔 주 단위였어요.",
                "professional", "chapter>=2 and chapter<=4", "WORKSHOP"),
            B("WORKSHOP_ENGINEER_LATE", "WORKSHOP_MACHINIST",
                "안정기 도면은 아직 저희 작업대에 있습니다. 찾는 분이 있으면 저한테 먼저 말씀하세요.",
                "focused", "chapter>=5", "WORKSHOP")
        };

        private static readonly AmbientBarkRecord[] Entries =
            BaselineEntries
                .Concat(SceneContextBarkCatalog.All)
                .ToArray();

        public static IReadOnlyList<AmbientBarkRecord> All => BaselineEntries;
        public static IReadOnlyList<AmbientBarkRecord> Contextual =>
            SceneContextBarkCatalog.All;
        public static IReadOnlyList<string> SupportedLocations => LocationCodes;

        public static IReadOnlyList<AmbientBarkRecord> GetAvailable(
            string locationCode,
            GameStateManager state,
            int maximum = 3)
        {
            return GetAvailable(
                locationCode,
                state,
                ResolveCurrentSceneId(state, null),
                maximum);
        }

        public static IReadOnlyList<AmbientBarkRecord> GetAvailable(
            string locationCode,
            GameStateManager state,
            string sceneId,
            int maximum = 3)
        {
            string location = locationCode?.Trim().ToUpperInvariant() ?? "";
            string scene = NormalizeSceneId(sceneId);
            AmbientBarkRecord[] available = Entries
                .Where(entry =>
                    entry.Location == location &&
                    Matches(entry.Condition, state, scene))
                .OrderByDescending(entry => ConditionPriority(entry.Condition))
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

        public static string ResolveCurrentSceneId(
            GameStateManager state,
            string activeSceneId)
        {
            string active = NormalizeSceneId(activeSceneId);
            if (ScenePresenceCatalog.TryGet(active, out _))
                return active;

            string checkpoint =
                NormalizeSceneId(state?.DialogueCheckpoint?.activeSceneId);
            if (ScenePresenceCatalog.TryGet(checkpoint, out _))
                return checkpoint;

            string next = ProductionSceneUnlockPolicy
                .FindNextAvailableScene(state);
            if (ScenePresenceCatalog.TryGet(next, out _))
                return next;

            return ScenePresenceCatalog.All
                .LastOrDefault(record =>
                    state?.HasCompletedScene(record.SceneId) == true)
                ?.SceneId ?? string.Empty;
        }

        private static bool Matches(
            string condition,
            GameStateManager state,
            string sceneId)
        {
            if (string.IsNullOrWhiteSpace(condition))
            {
                return false;
            }

            if (condition.StartsWith("flag:") || condition.StartsWith("scene="))
            {
                return MatchesSingle(condition, state, sceneId);
            }

            return condition
                .Split(" and ", StringSplitOptions.RemoveEmptyEntries)
                .All(clause => MatchesSingle(clause.Trim(), state, sceneId));
        }

        private static bool MatchesSingle(
            string condition,
            GameStateManager state,
            string sceneId)
        {
            int anxiety = state?.PublicAnxiety ?? 15;
            int day = state?.Day ?? 1;
            if (condition == "always") return true;
            if (condition == "publicAnxiety<40") return anxiety < 40;
            if (condition == "publicAnxiety>=40") return anxiety >= 40;
            if (condition == "publicAnxiety<70") return anxiety < 70;
            if (condition == "publicAnxiety>=70") return anxiety >= 70;
            if (condition.StartsWith("chapter>="))
                return int.TryParse(condition.Substring(9), out int gte) &&
                       day >= gte;
            if (condition.StartsWith("chapter<="))
                return int.TryParse(condition.Substring(9), out int lte) &&
                       day <= lte;
            if (condition.StartsWith("chapter="))
                return int.TryParse(condition.Substring(8), out int eq) &&
                       day == eq;
            if (condition.StartsWith("flag:"))
                return state?.HasFlag(condition.Substring(5)) == true;
            if (condition.StartsWith("scene="))
            {
                string required = NormalizeSceneId(condition.Substring(6));
                return !string.IsNullOrEmpty(sceneId)
                    ? required == sceneId
                    : state?.HasCompletedScene(required) == true ||
                      state?.IsProductionSceneUnlocked(required) == true;
            }
            return false;
        }

        private static int ConditionPriority(string condition)
        {
            if (condition.StartsWith("scene=") ||
                condition.StartsWith("flag:") ||
                condition.StartsWith("chapter"))
            {
                return 30;
            }

            if (condition == "publicAnxiety>=70") return 20;
            if (condition == "publicAnxiety>=40 and publicAnxiety<70")
                return 10;
            return 0;
        }

        private static string NormalizeSceneId(string value) =>
            value?.Trim().ToUpperInvariant() ?? string.Empty;

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
            string location) =>
            new(id, speaker, text, emotion, condition, location);
    }
}
