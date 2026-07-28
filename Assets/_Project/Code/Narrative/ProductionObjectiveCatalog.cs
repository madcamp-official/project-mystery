using System;
using System.Collections.Generic;
using System.Linq;
using Wake.Core;

namespace Wake.Narrative
{
    public enum ProductionObjectiveStatus
    {
        Completed,
        Current,
        InteractionPending,
        Next,
        Locked
    }

    public sealed class ProductionObjectiveDefinition
    {
        public ProductionObjectiveDefinition(
            ProductionSceneDefinition scene,
            string title,
            string description)
        {
            Scene = scene ?? throw new ArgumentNullException(nameof(scene));
            Title = title ?? string.Empty;
            Description = description ?? string.Empty;
        }

        public ProductionSceneDefinition Scene { get; }
        public string SceneId => Scene.SceneId;
        public string Title { get; }
        public string Description { get; }
        public string ScheduleLabel =>
            $"Day {Scene.Day} · {Scene.TimeLabel} · {Scene.NarrativeLocationCode}";
    }

    public readonly struct ProductionObjectiveItem
    {
        public ProductionObjectiveItem(
            ProductionObjectiveDefinition definition,
            ProductionObjectiveStatus status)
        {
            Definition = definition;
            Status = status;
        }

        public ProductionObjectiveDefinition Definition { get; }
        public ProductionObjectiveStatus Status { get; }
        public string StateIcon => Status switch
        {
            ProductionObjectiveStatus.Completed => "✓",
            ProductionObjectiveStatus.Current => "●",
            ProductionObjectiveStatus.InteractionPending => "!",
            ProductionObjectiveStatus.Next => "→",
            _ => "◆"
        };
        public string StateLabel => Status switch
        {
            ProductionObjectiveStatus.Completed => "완료",
            ProductionObjectiveStatus.Current => "현재",
            ProductionObjectiveStatus.InteractionPending => "상호작용 대기",
            ProductionObjectiveStatus.Next => "다음",
            _ => "잠김"
        };
        public string AccessibilityLabel =>
            $"{StateLabel}: {Definition.Title}. {Definition.Description}";
    }

    public sealed class ProductionObjectiveViewModel
    {
        private ProductionObjectiveViewModel(
            IReadOnlyList<ProductionObjectiveItem> items)
        {
            Items = items;
            Current = Find(ProductionObjectiveStatus.InteractionPending) ??
                      Find(ProductionObjectiveStatus.Current);
            Next = Find(ProductionObjectiveStatus.Next);
        }

        public IReadOnlyList<ProductionObjectiveItem> Items { get; }
        public ProductionObjectiveItem? Current { get; }
        public ProductionObjectiveItem? Next { get; }
        public int CompletedCount =>
            Items.Count(item => item.Status == ProductionObjectiveStatus.Completed);
        public string Summary => $"전체 장면 {CompletedCount}/{Items.Count}";

        public static ProductionObjectiveViewModel Resolve(GameStateManager state)
        {
            if (state == null)
            {
                return new ProductionObjectiveViewModel(
                    Array.Empty<ProductionObjectiveItem>());
            }

            ProductionDialogueCheckpoint checkpoint = state.DialogueCheckpoint;
            string activeSceneId =
                checkpoint?.activeSceneId?.Trim().ToUpperInvariant() ?? string.Empty;
            int activeIndex = IndexOf(activeSceneId);
            if (activeIndex >= 0 &&
                (state.HasCompletedScene(activeSceneId) ||
                 !HasCompletedPrerequisites(
                     state,
                     ProductionObjectiveCatalog.All[activeIndex].Scene)))
            {
                activeIndex = -1;
            }

            bool[] eligible = ProductionObjectiveCatalog.All
                .Select(objective =>
                    !state.HasCompletedScene(objective.SceneId) &&
                    HasCompletedPrerequisites(state, objective.Scene))
                .ToArray();
            int primaryIndex = activeIndex >= 0
                ? activeIndex
                : Array.FindIndex(eligible, value => value);
            var items = new ProductionObjectiveItem[ProductionObjectiveCatalog.All.Count];

            for (int index = 0; index < items.Length; index++)
            {
                ProductionObjectiveDefinition definition =
                    ProductionObjectiveCatalog.All[index];
                ProductionObjectiveStatus status = ResolveStatus(
                    state,
                    checkpoint,
                    definition,
                    index,
                    activeIndex,
                    primaryIndex,
                    eligible[index]);
                items[index] = new ProductionObjectiveItem(definition, status);
            }

            return new ProductionObjectiveViewModel(items);
        }

        private ProductionObjectiveItem? Find(ProductionObjectiveStatus status)
        {
            foreach (ProductionObjectiveItem item in Items)
            {
                if (item.Status == status)
                {
                    return item;
                }
            }

            return null;
        }

        private static ProductionObjectiveStatus ResolveStatus(
            GameStateManager state,
            ProductionDialogueCheckpoint checkpoint,
            ProductionObjectiveDefinition definition,
            int index,
            int activeIndex,
            int primaryIndex,
            bool isEligible)
        {
            if (state.HasCompletedScene(definition.SceneId))
            {
                return ProductionObjectiveStatus.Completed;
            }

            if (index == activeIndex &&
                IsKnownPendingInteraction(checkpoint, definition.SceneId))
            {
                return ProductionObjectiveStatus.InteractionPending;
            }

            if (index == primaryIndex)
            {
                return ProductionObjectiveStatus.Current;
            }

            return isEligible
                ? ProductionObjectiveStatus.Next
                : ProductionObjectiveStatus.Locked;
        }

        private static bool IsKnownPendingInteraction(
            ProductionDialogueCheckpoint checkpoint,
            string sceneId)
        {
            return checkpoint != null &&
                   ProductionSceneCompletionCatalog.TryGet(
                       sceneId,
                       out ProductionSceneCompletionRequirement requirement) &&
                   requirement.Matches(checkpoint.pendingInteractionId);
        }

        private static bool HasCompletedPrerequisites(
            GameStateManager state,
            ProductionSceneDefinition scene)
        {
            return scene.Prerequisites.All(prerequisite =>
                ProductionSceneCatalog.TryGet(prerequisite, out _)
                    ? state.HasCompletedScene(prerequisite)
                    : scene.SceneId == "D8-02" &&
                      FinalAccusationResolver.OpensD8Confession(
                          state.FinalEndingId));
        }

        private static int IndexOf(string sceneId)
        {
            for (int index = 0; index < ProductionObjectiveCatalog.All.Count; index++)
            {
                if (ProductionObjectiveCatalog.All[index].SceneId == sceneId)
                {
                    return index;
                }
            }

            return -1;
        }
    }

    public static class ProductionObjectiveCatalog
    {
        private static readonly string[,] SourceText =
        {
            { "P-01", "항구의 경고", "Daniel이 Richard를 경고하는 이유를 확인한다." },
            { "P-02", "승선 명단의 오류", "다니엘의 초대가 리처드 명의임을 확인한다." },
            { "P-03", "회장의 부탁", "익명 협박장과 Orpheus 사고의 공식 설명을 듣는다." },
            { "D1-01", "승객 소개", "주요 용의자들과 자유 대화를 진행한다." },
            { "D1-02", "불편한 만찬", "Daniel과 Claire의 언쟁을 목격한다." },
            { "D1-03", "선상파티", "파티장 동선과 카메라 위치를 조사한다." },
            { "D1-04", "사라진 기자", "Daniel이 서비스 계단으로 향한 사실을 알아낸다." },
            { "D1-05", "수상한 호출", "이블린이 리처드에게 다니엘의 대기 사실을 알린 정황을 확인한다." },
            { "D1-06", "흔적 없는 밀실", "호라이즌 룸의 시신과 사용되지 않은 출구를 확인한다." },
            { "D1-07", "비밀 수사 계약", "회항이 어려운 이유와 비밀수사 조건을 정한다." },
            { "D2-01", "세 출구 검증", "외벽·덕트·점검구에 탈출 흔적이 없음을 확인한다." },
            { "D2-02", "피의 방향", "혈흔 퍼즐로 수직 낙하와 비산혈흔 부재를 확인한다." },
            { "D2-03", "사망 시각", "약물과 저온 환경이 사망 추정을 왜곡했음을 의심한다." },
            { "D2-04", "카메라의 맹점", "출입자는 없지만 감지기 오류가 있었음을 찾는다." },
            { "D2-05", "천장 패널", "장식 패널 먼지와 미세 섬유를 채취한다." },
            { "D2-06", "기자의 객실", "예약 기사와 Richard 유죄 가설을 확인한다." },
            { "D3-01", "예약 기사 공개", "승객 사이에 Richard 범인설이 퍼지는 과정을 확인한다." },
            { "D3-02", "Richard의 은폐", "Orpheus 조사 기록을 숨긴 이유를 추궁한다." },
            { "D3-03", "Thomas의 침묵", "금고와 원본 모듈의 존재를 확인한다." },
            { "D3-04", "봉인된 기록", "모듈 일부가 덮어쓰기 된 사실을 발견한다." },
            { "D3-05", "익명 제보자의 문장", "다니엘의 채팅에서 반복되는 어휘를 찾는다." },
            { "D4-01", "Marcus의 거짓말", "보안 인증 대여 사실을 추궁한다." },
            { "D4-02", "계단 추락", "Marcus 추락 현장을 조사한다." },
            { "D4-03", "사고의 재구성", "발자국 간격과 난간 손자국으로 자력 추락을 입증한다." },
            { "D4-04", "말하지 못한 증언", "의식이 돌아온 Marcus에게 예·아니오 심문을 한다." },
            { "D5-01", "두 번째 불가능 사건", "Claire를 연기 속에서 발견하고 현장을 확인한다." },
            { "D5-02", "사라진 태블릿", "침입자 없는 객실에서 Claire의 태블릿 행방을 확인한다." },
            { "D5-03", "Claire의 자백", "태블릿 절도와 비자금 은폐를 자백시킨다." },
            { "D5-04", "자동으로 완성된 방", "첫 사건도 자동장치가 현장을 만들었다는 가설을 세운다." },
            { "D6-01", "86킬로그램의 이동", "기관 제어 기록에서 화물 무게의 이동 경로를 확인한다." },
            { "D6-02", "천장 위의 길", "화물 레일을 직접 따라간다." },
            { "D6-03", "검은 바닥", "다니엘 구두의 고무 조각과 바닥을 대조한다." },
            { "D6-04", "두 번의 죽음", "자상은 비치명상이고 직접 사인은 질식임을 확정한다." },
            { "D6-05", "타임라인 퍼즐", "21:22부터 22:45까지 사건의 참 타임라인을 완성한다." },
            { "D7-01", "마지막 파괴 시도", "이블린이 모듈을 파괴하려는 것을 저지한다." },
            { "D7-02", "보호면의 침방울", "이블린의 장비와 다니엘의 DNA를 연결한다." },
            { "D7-03", "Orpheus 원본 음성", "복원된 기록에서 Julian과 Evelyn의 대화를 확인한다." },
            { "D7-04", "Evelyn의 제안", "Richard를 희생양으로 삼으려는 거래 제안을 심문한다." },
            { "D8-01", "흔적 없는 밀실의 해답", "다니엘의 시신만 호라이즌 룸에 들어온 수법을 논증한다." },
            { "D8-02", "이블린의 책임", "오르페우스호와 다니엘 사건에 대한 이블린의 책임을 추궁한다." },
            { "D8-03", "귀항", "후일담과 사건 평가를 확인한다." }
        };

        private static readonly ProductionObjectiveDefinition[] Entries =
            BuildEntries();

        public static IReadOnlyList<ProductionObjectiveDefinition> All => Entries;

        private static ProductionObjectiveDefinition[] BuildEntries()
        {
            if (SourceText.GetLength(0) != ProductionSceneCatalog.All.Count)
            {
                throw new InvalidOperationException(
                    "Production objective text must match the scene catalog.");
            }

            var entries = new ProductionObjectiveDefinition[SourceText.GetLength(0)];
            for (int index = 0; index < entries.Length; index++)
            {
                ProductionSceneDefinition scene = ProductionSceneCatalog.All[index];
                if (scene.SceneId != SourceText[index, 0])
                {
                    throw new InvalidOperationException(
                        $"Objective order mismatch at {SourceText[index, 0]}.");
                }

                entries[index] = new ProductionObjectiveDefinition(
                    scene,
                    SourceText[index, 1],
                    SourceText[index, 2]);
            }

            return entries;
        }
    }
}
