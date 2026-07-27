using System;
using System.Collections.Generic;

namespace Wake.Exploration
{
    public static class MainCharacterWorldLineCatalog
    {
        private static readonly IReadOnlyDictionary<string, string> Lines =
            new Dictionary<string, string>(StringComparer.Ordinal)
            {
                ["DANIEL"] =
                    "확인되지 않은 소문보다 기록을 보시죠. 제가 본 순서대로 말씀드리겠습니다.",
                ["RICHARD"] =
                    "동선에 관해서라면 기록으로 답하겠네. 추측으로 일을 키우진 말게.",
                ["EVELYN"] =
                    "지금 공개할 수 있는 범위는 여기까지예요. 정식 질문이라면 답하겠습니다.",
                ["CLAIRE"] =
                    "사람들이 불안해하고 있어요. 필요한 질문이라면 조용한 곳에서 해 주세요.",
                ["THOMAS"] =
                    "장비 상태부터 확인해야 합니다. 수치와 기록은 숨기지 않겠습니다.",
                ["MARCUS"] =
                    "통제 기록을 확인 중입니다. 경비 동선에 관한 질문은 정확히 해 주십시오.",
                ["HELENA"] =
                    "환자와 현장 보존이 우선이에요. 의학적으로 확인된 사실만 말씀드리죠.",
                ["OWEN"] =
                    "기계는 흔적을 남깁니다. 정비 기록과 실제 손상부터 대조해 보죠."
            };

        public static string Get(
            string characterId,
            SceneCharacterState state)
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
            return Lines.TryGetValue(key, out string line)
                ? line
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
    }
}
