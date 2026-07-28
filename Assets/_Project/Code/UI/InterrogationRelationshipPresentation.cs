namespace Wake.UI
{
    public static class InterrogationRelationshipPresentation
    {
        public static string ResolveTrust(int trust) =>
            trust switch
            {
                <= 0 => "증언을 거부함",
                <= 2 => "경계하고 있음",
                3 => "답변을 망설임",
                _ => "신뢰하기 시작함"
            };

        public static string ResolveQuestionBudget(
            int remaining,
            int maximum)
        {
            if (remaining <= 0)
            {
                return "더 이상 질문할 수 없음";
            }

            if (remaining == 1)
            {
                return "마지막 질문을 신중히 선택해야 함";
            }

            return remaining <= maximum / 2
                ? "질문 기회가 얼마 남지 않음"
                : "질문할 여유가 있음";
        }
    }
}
