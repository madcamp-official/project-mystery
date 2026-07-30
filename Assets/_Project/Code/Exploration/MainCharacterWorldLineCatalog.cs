using System;
using System.Collections.Generic;

namespace Wake.Exploration
{
    public static class MainCharacterWorldLineCatalog
    {
        private sealed class DayTieredLines
        {
            public DayTieredLines(string early, string mid, string late)
            {
                Early = early;
                Mid = mid;
                Late = late;
            }

            public string Early { get; }
            public string Mid { get; }
            public string Late { get; }

            public string ForDay(int day) =>
                day <= 1 ? Early : day <= 4 ? Mid : Late;
        }

        private static readonly IReadOnlyDictionary<string, DayTieredLines>
            Lines = new Dictionary<string, DayTieredLines>(
                StringComparer.Ordinal)
            {
                ["DANIEL"] = new DayTieredLines(
                    "확인되지 않은 소문보다 기록을 보시죠. 제가 본 순서대로 말씀드리겠습니다.",
                    "확인되지 않은 소문보다 기록을 보시죠. 제가 본 순서대로 말씀드리겠습니다.",
                    "확인되지 않은 소문보다 기록을 보시죠. 제가 본 순서대로 말씀드리겠습니다."),
                ["RICHARD"] = new DayTieredLines(
                    "동선에 관해서라면 기록으로 답하겠네. 추측으로 일을 키우진 말게.",
                    "가문 이름이 걸린 일이야. 확인된 것 외엔 아무 말도 하지 않겠네.",
                    "이젠 숨길 것도 별로 없네. 다만 묻는 순서는 지켜 주게."),
                ["EVELYN"] = new DayTieredLines(
                    "지금 공개할 수 있는 범위는 여기까지예요. 정식 질문이라면 답하겠습니다.",
                    "회사와 가족, 둘 다 지켜야 할 입장이라는 것만 알아 두세요.",
                    "제가 뭘 감추고 있다고 생각하시는군요. 틀린 짐작은 아니겠죠."),
                ["CLAIRE"] = new DayTieredLines(
                    "사람들이 불안해하고 있어요. 필요한 질문이라면 조용한 곳에서 해 주세요.",
                    "제 방 얘기라면 이미 다 말씀드렸어요. 같은 질문 반복하지 말아 주세요.",
                    "다들 절 그렇게 보시는 거 알아요. 그래도 대답할 건 대답할게요."),
                ["THOMAS"] = new DayTieredLines(
                    "장비 상태부터 확인해야 합니다. 수치와 기록은 숨기지 않겠습니다.",
                    "기관 기록은 요청하시면 그대로 넘겨드립니다. 지어낼 이유가 없어요.",
                    "원본 기록 얘기는 이제 저도 조심스럽습니다. 아는 만큼만 말씀드리죠."),
                ["MARCUS"] = new DayTieredLines(
                    "통제 기록을 확인 중입니다. 경비 동선에 관한 질문은 정확히 해 주십시오.",
                    "인증 기록을 다시 정리하고 있습니다. 지금은 그것만 봐 주십시오.",
                    "저도 예전 같지 않다는 거 압니다. 그래도 확인할 건 확인하겠습니다."),
                ["HELENA"] = new DayTieredLines(
                    "환자와 현장 보존이 우선이에요. 의학적으로 확인된 사실만 말씀드리죠.",
                    "검시 소견은 아직 정리 중입니다. 성급한 결론은 원치 않아요.",
                    "제가 본 걸 다 말씀드렸다고 생각했는데, 또 여쭤보시는군요."),
                ["OWEN"] = new DayTieredLines(
                    "기계는 흔적을 남깁니다. 정비 기록과 실제 손상부터 대조해 보죠.",
                    "정비 기록은 매일 새로 남기고 있습니다. 궁금하신 부분 짚어 주세요.",
                    "기계는 거짓말 안 합니다. 사람 쪽 이야기는 제 몫이 아니고요.")
            };

        public static string Get(
            string characterId,
            SceneCharacterState state,
            int day)
        {
            if (state == SceneCharacterState.Injured)
            {
                return "부상 부위가 아직 좋지 않습니다. 필요한 내용만 짧게 묻죠.";
            }

            if (state == SceneCharacterState.Detained)
            {
                return "경비가 지켜보는 자리군요. 정식 심문에서 같은 답을 드리겠습니다.";
            }

            string key = characterId?.Trim().ToUpperInvariant() ?? "";
            return Lines.TryGetValue(key, out DayTieredLines lines)
                ? lines.ForDay(day)
                : "지금 확인 중인 내용이 있습니다. 정식 질문이라면 답하겠습니다.";
        }

        public static string GetEmotion(SceneCharacterState state)
        {
            return state switch
            {
                SceneCharacterState.Injured => "strained",
                SceneCharacterState.Detained => "guarded",
                _ => "neutral"
            };
        }

        public static string GetCompleted(
            string characterId,
            SceneCharacterState state,
            int day)
        {
            if (state == SceneCharacterState.Injured)
            {
                return "지금은 더 이야기하기 어렵습니다. 앞서 말씀드린 내용을 확인해 주세요.";
            }

            if (state == SceneCharacterState.Detained)
            {
                return "이미 진술을 마쳤습니다. 추가 내용은 정식 심문에서 말씀드리겠습니다.";
            }

            string key = characterId?.Trim().ToUpperInvariant() ?? string.Empty;
            int tier = day <= 1 ? 0 : day <= 4 ? 1 : 2;
            return (key, tier) switch
            {
                ("DANIEL", _) => "이미 말씀드릴 수 있는 건 전부 말씀드렸습니다.",
                ("RICHARD", 0) => "같은 질문에는 같은 답밖에 해 줄 수 없네.",
                ("RICHARD", 1) => "가문 이름을 걸고 이미 말했네. 더는 보탤 게 없어.",
                ("RICHARD", _) => "이제 와서 더 말한다고 달라질 게 있겠나.",
                ("EVELYN", 0) => "제 진술은 끝났습니다. 기록을 확인해 주세요.",
                ("EVELYN", 1) => "드릴 수 있는 답은 이미 다 드렸어요.",
                ("EVELYN", _) => "더 물으셔도 같은 답뿐이에요. 기록을 보세요.",
                ("CLAIRE", 0) => "조금 전 말씀드린 내용이 전부예요.",
                ("CLAIRE", 1) => "그 얘기는 이미 끝났잖아요. 다른 걸 물어봐 주세요.",
                ("CLAIRE", _) => "몇 번을 물으셔도 대답은 똑같아요.",
                ("THOMAS", 0) => "정비 기록 외에 덧붙일 내용은 없습니다.",
                ("THOMAS", 1) => "기록은 이미 넘겨드렸습니다. 그대로입니다.",
                ("THOMAS", _) => "제가 아는 건 이미 다 말씀드렸습니다.",
                ("MARCUS", 0) => "진술은 기록됐습니다. 추가 사항이 생기면 보고하겠습니다.",
                ("MARCUS", 1) => "인증 기록은 이미 제출했습니다. 확인해 보십시오.",
                ("MARCUS", _) => "더 드릴 말씀은 없습니다. 기록이 전부입니다.",
                ("HELENA", 0) => "검시 소견은 이미 전달했습니다. 기록을 확인해 주세요.",
                ("HELENA", 1) => "소견서에 적은 내용이 전부예요. 더는 추측하지 않겠습니다.",
                ("HELENA", _) => "몇 번을 여쭤보셔도 소견은 그대로예요.",
                ("OWEN", 0) => "기관 기록과 제 진술은 이미 제출했습니다.",
                ("OWEN", 1) => "정비 기록은 이미 넘겨드렸습니다. 달라질 게 없어요.",
                ("OWEN", _) => "기계는 그대로고, 제 답도 그대로입니다.",
                _ => "앞서 말씀드린 내용이 전부입니다."
            };
        }
    }
}
