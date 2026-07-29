using System;
using System.Collections.Generic;
using System.Linq;
using Wake.Core;
using Wake.Exploration;

namespace Wake.Narrative
{
    public enum ObjectiveActionType
    {
        Move,
        Talk,
        Find,
        Inspect,
        Compare,
        Present,
        Solve,
        Decide,
        Chase,
        Review
    }

    public enum ObjectivePriority
    {
        Main,
        Sub,
        Optional,
        Context
    }

    public enum ObjectiveMarkerMode
    {
        Map,
        Edge,
        Npc,
        Area,
        Hover,
        None
    }

    public enum ObjectiveSpoilerStage
    {
        Known,
        Suspected,
        Confirmed
    }

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
            ObjectiveActionType actionType,
            string displayText,
            string detailText,
            ObjectiveMarkerMode markerMode,
            params string[] steps)
        {
            Scene = scene ?? throw new ArgumentNullException(nameof(scene));
            ActionType = actionType;
            DisplayText = displayText ?? string.Empty;
            DetailText = detailText ?? string.Empty;
            MarkerMode = markerMode;
            Steps = (steps ?? Array.Empty<string>())
                .Where(step => !string.IsNullOrWhiteSpace(step))
                .Select(step => step.Trim())
                .ToArray();
        }

        public ProductionSceneDefinition Scene { get; }
        public string ObjectiveId => $"OBJ_{SceneId.Replace("-", "_")}_MAIN";
        public string SceneId => Scene.SceneId;
        public ObjectivePriority Priority => ObjectivePriority.Main;
        public ObjectiveActionType ActionType { get; }
        public string DisplayText { get; }
        public string DetailText { get; }
        public string TargetLocation => Scene.NarrativeLocationCode;
        public string TargetEntity => string.Empty;
        public ObjectiveMarkerMode MarkerMode { get; }
        public string FallbackText => "현재 진행 가능한 목표부터 확인하기";
        public ObjectiveSpoilerStage SpoilerStage => ObjectiveSpoilerStage.Known;
        public IReadOnlyList<string> Steps { get; }

        // Kept as aliases for callers that still consume the old scene objective API.
        public string Title => DisplayText;
        public string Description => DetailText;
    }

    public readonly struct ProductionObjectivePresentation
    {
        public ProductionObjectivePresentation(
            ProductionObjectiveDefinition definition,
            ObjectiveActionType actionType,
            string displayText,
            string detailText,
            ObjectiveMarkerMode markerMode,
            string targetLocation,
            bool isTravel)
        {
            Definition = definition;
            ActionType = actionType;
            DisplayText = displayText ?? string.Empty;
            DetailText = detailText ?? string.Empty;
            MarkerMode = markerMode;
            TargetLocation = targetLocation ?? string.Empty;
            IsTravel = isTravel;
        }

        public ProductionObjectiveDefinition Definition { get; }
        public ObjectiveActionType ActionType { get; }
        public string DisplayText { get; }
        public string DetailText { get; }
        public ObjectiveMarkerMode MarkerMode { get; }
        public string TargetLocation { get; }
        public bool IsTravel { get; }
        public string ActionLabel => ActionType switch
        {
            ObjectiveActionType.Move => "이동",
            ObjectiveActionType.Talk => "대화",
            ObjectiveActionType.Find => "탐색",
            ObjectiveActionType.Inspect => "조사",
            ObjectiveActionType.Compare => "대조",
            ObjectiveActionType.Present => "제시",
            ObjectiveActionType.Solve => "재구성",
            ObjectiveActionType.Decide => "결정",
            ObjectiveActionType.Chase => "추적",
            _ => "확인"
        };
        public string StateIcon => ActionType switch
        {
            ObjectiveActionType.Move => "이동",
            ObjectiveActionType.Talk => "●",
            ObjectiveActionType.Find => "찾기",
            ObjectiveActionType.Inspect => "조사",
            ObjectiveActionType.Compare => "≍",
            ObjectiveActionType.Present => "▣",
            ObjectiveActionType.Solve => "◇",
            ObjectiveActionType.Decide => "◆",
            ObjectiveActionType.Chase => "추적",
            _ => "✓"
        };
        public string AccessibilityLabel =>
            $"다음 목표. {DisplayText}. {DetailText}";
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
            ProductionObjectiveStatus.Next => "다음",
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
            IReadOnlyList<ProductionObjectiveItem> items,
            GameStateManager state)
        {
            Items = items;
            Current = Find(ProductionObjectiveStatus.InteractionPending) ??
                      Find(ProductionObjectiveStatus.Current);
            Next = Find(ProductionObjectiveStatus.Next);
            Presentation = ProductionObjectivePresentationResolver.Resolve(
                state,
                Current ?? Next);
        }

        public IReadOnlyList<ProductionObjectiveItem> Items { get; }
        public ProductionObjectiveItem? Current { get; }
        public ProductionObjectiveItem? Next { get; }
        public ProductionObjectivePresentation? Presentation { get; }
        public int CompletedCount =>
            Items.Count(item => item.Status == ProductionObjectiveStatus.Completed);
        public string Summary => $"전체 장면 {CompletedCount}/{Items.Count}";

        public static ProductionObjectiveViewModel Resolve(GameStateManager state)
        {
            if (state == null)
            {
                return new ProductionObjectiveViewModel(
                    Array.Empty<ProductionObjectiveItem>(),
                    null);
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

            return new ProductionObjectiveViewModel(items, state);
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
            {
                if (ProductionSceneCatalog.TryGet(prerequisite, out _))
                {
                    return state.HasCompletedScene(prerequisite);
                }

                if (prerequisite ==
                    ProductionSceneCatalog.FinalAccusationPrerequisite)
                {
                    return FinalAccusationResolver.OpensD8Confession(
                        state.FinalEndingId);
                }

                if (prerequisite ==
                    ProductionSceneCatalog.EpiloguePrerequisite)
                {
                    string route = FinalAccusationResolver.ToOfficialRoute(
                        state.FinalEndingId);
                    return state.HasCompletedScene(
                               ProductionEndingCatalog.ConfessionSceneId) ||
                           route == "C" ||
                           route == "Bad";
                }

                return false;
            });
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

    public static class ProductionObjectivePresentationResolver
    {
        public static ProductionObjectivePresentation? Resolve(
            GameStateManager state,
            ProductionObjectiveItem? selected)
        {
            if (state == null || !selected.HasValue)
            {
                return null;
            }

            ProductionObjectiveDefinition definition =
                selected.Value.Definition;
            CanonicalLocationSpec target =
                CanonicalLocationCatalog.FindSpec(definition.TargetLocation);
            CanonicalLocationSpec current =
                CanonicalLocationCatalog.FindSpec(state.CurrentLocationCode);
            bool requiresTravel =
                target != null &&
                (current == null ||
                 !string.Equals(
                     current.Code,
                     target.Code,
                     StringComparison.Ordinal));

            if (requiresTravel)
            {
                return new ProductionObjectivePresentation(
                    definition,
                    ObjectiveActionType.Move,
                    $"{AppendDirectionalParticle(target.DisplayName)} 향하기",
                    "지도에서 목적지를 선택해 이동하자.",
                    ObjectiveMarkerMode.Map,
                    target.Code,
                    true);
            }

            return new ProductionObjectivePresentation(
                definition,
                definition.ActionType,
                definition.DisplayText,
                definition.DetailText,
                definition.MarkerMode,
                target?.Code ?? definition.TargetLocation,
                false);
        }

        private static string AppendDirectionalParticle(string value)
        {
            if (string.IsNullOrEmpty(value))
            {
                return string.Empty;
            }

            char last = value[value.Length - 1];
            int syllable = last - 0xAC00;
            if (syllable < 0 || syllable > 11171)
            {
                return value + "로";
            }

            int finalConsonant = syllable % 28;
            return value + (finalConsonant == 0 || finalConsonant == 8
                ? "로"
                : "으로");
        }
    }

    public static class ProductionObjectiveNpcTargets
    {
        private static readonly IReadOnlyDictionary<string, string[]> ByScene =
            new Dictionary<string, string[]>(StringComparer.Ordinal)
            {
                ["P-01"] = new[] { "DANIEL" },
                ["P-03"] = new[] { "RICHARD" },
                ["D1-01"] = new[] { "CLAIRE", "MARCUS", "HELENA", "OWEN" },
                ["D1-05"] = new[] { "EVELYN", "RICHARD" },
                ["D1-07"] = new[] { "RICHARD", "THOMAS", "MARCUS", "HELENA" },
                ["D2-03"] = new[] { "HELENA" },
                ["D3-02"] = new[] { "RICHARD" },
                ["D3-03"] = new[] { "THOMAS" },
                ["D3-05"] = new[] { "CLAIRE" },
                ["D4-01"] = new[] { "MARCUS" },
                ["D4-04"] = new[] { "MARCUS" },
                ["D5-03"] = new[] { "CLAIRE" },
                ["D6-04"] = new[] { "HELENA" },
                ["D7-04"] = new[] { "EVELYN" },
                ["D8-02"] = new[] { "EVELYN" }
            };

        public static bool Contains(string sceneId, string characterId)
        {
            string scene = sceneId?.Trim().ToUpperInvariant() ??
                           string.Empty;
            string character =
                characterId?.Trim().ToUpperInvariant() ?? string.Empty;
            return ByScene.TryGetValue(scene, out string[] targets) &&
                   targets.Contains(character, StringComparer.Ordinal);
        }

        public static IReadOnlyList<string> ForScene(string sceneId)
        {
            string scene = sceneId?.Trim().ToUpperInvariant() ??
                           string.Empty;
            return ByScene.TryGetValue(scene, out string[] targets)
                ? targets
                : Array.Empty<string>();
        }
    }

    public static class ProductionObjectiveCatalog
    {
        private static readonly ProductionObjectiveDefinition[] Entries =
        {
            O("P-01", ObjectiveActionType.Find, "항구의 기자를 찾기", "항구를 둘러보고 다니엘을 찾아보자.", ObjectiveMarkerMode.Npc, "다니엘 머서 찾기", "구겨진 초대장 살펴보기", "메신저 알림 확인하기", "다니엘과 이야기하기", "그의 경고에 답하기"),
            O("P-02", ObjectiveActionType.Inspect, "승선 명단의 오류 확인하기", "승선 명단과 관계자들의 반응을 살펴보자.", ObjectiveMarkerMode.Hover, "이블린과 이야기하기", "승선 명단 살펴보기", "리처드의 반응 확인하기", "명단 수정자를 묻기"),
            O("P-03", ObjectiveActionType.Talk, "리처드의 부탁 듣기", "리처드를 클릭해 그의 부탁을 들어보자.", ObjectiveMarkerMode.Npc, "리처드와 이야기하기", "협박장 조사하기", "아들 사진 살펴보기", "질문을 결정하기"),
            O("D1-01", ObjectiveActionType.Talk, "주요 승객들과 이야기하기", "아트리움에 있는 주요 승객들을 차례로 만나보자.", ObjectiveMarkerMode.Npc, "클레어와 이야기하기", "마커스와 이야기하기", "헬레나와 이야기하기", "오웬과 이야기하기"),
            O("D1-02", ObjectiveActionType.Inspect, "다니엘과 클레어의 언쟁 살펴보기", "언쟁이 벌어지는 테이블 주변을 살펴보자.", ObjectiveMarkerMode.Area, "언쟁이 벌어지는 테이블로 다가가기", "끼어들거나 관찰하기", "익명 제보에 관한 말을 확인하기"),
            O("D1-03", ObjectiveActionType.Inspect, "파티장의 동선 조사하기", "카메라와 출입구를 직접 살펴 동선을 확인하자.", ObjectiveMarkerMode.Hover, "카메라 위치 확인하기", "출입구 동선 살펴보기", "다니엘을 추적하거나 파티 인물을 탐문하기"),
            O("D1-04", ObjectiveActionType.Find, "다니엘의 행방 찾기", "서비스 구역에서 다니엘의 이동 흔적을 찾아보자.", ObjectiveMarkerMode.Area, "승무원과 이야기하기", "서비스 출입문 살펴보기", "개폐 로그 확인하기", "경비에게 알릴지 결정하기"),
            O("D1-05", ObjectiveActionType.Inspect, "리처드에게 전달된 호출 확인하기", "이블린과 리처드를 통해 호출 내용을 확인하자.", ObjectiveMarkerMode.Npc, "이블린과 리처드의 대화 듣기", "리처드 명의 메시지 확인하기", "메시지 원문을 요구하기"),
            O("D1-06", ObjectiveActionType.Inspect, "호라이즌 룸의 상황 확인하기", "방 안의 인물과 주변 흔적을 차분히 확인하자.", ObjectiveMarkerMode.Area, "방 안으로 들어가기", "다니엘의 상태 확인하기", "현장을 봉쇄할지 결정하기", "열린 문과 주변 흔적 조사하기"),
            O("D1-07", ObjectiveActionType.Decide, "비밀 수사의 조건 정하기", "관련 인물들의 설명을 듣고 수사 조건을 정하자.", ObjectiveMarkerMode.Npc, "관련 인물들과 이야기하기", "공식 발표 확인하기", "수락 또는 제한 조건 결정하기"),
            O("D2-01", ObjectiveActionType.Inspect, "가능한 출구 세 곳 검증하기", "발판과 덕트, 점검구를 하나씩 직접 조사하자.", ObjectiveMarkerMode.Hover, "외벽 발판의 흔적 확인하기", "공조 덕트 조사하기", "설비 점검구 조사하기", "출구 조사 결과 정리하기"),
            O("D2-02", ObjectiveActionType.Solve, "혈흔의 방향 재구성하기", "혈흔 사진과 위치 관계를 이용해 방향을 재구성하자.", ObjectiveMarkerMode.None, "혈흔 사진 살펴보기", "사진 조각 배열하기", "시신 위치와 혈흔 중심 비교하기", "결론 선택하기"),
            O("D2-03", ObjectiveActionType.Compare, "사망 시각 추정을 다시 확인하기", "헬레나의 설명과 의료 기록을 함께 확인하자.", ObjectiveMarkerMode.Npc, "헬레나와 이야기하기", "안정제 처방 확인하기", "체온 기록 비교하기", "압박 또는 협조 요청 결정하기"),
            O("D2-04", ObjectiveActionType.Compare, "영상과 설비 로그 대조하기", "보안 영상과 출입·감지 기록의 시간을 맞춰보자.", ObjectiveMarkerMode.Hover, "마커스와 이야기하기", "보안 영상 확인하기", "출입 로그 확인하기", "감지기 기록과 시간 맞추기"),
            O("D2-05", ObjectiveActionType.Find, "천장 패널 조사하기", "천장 전체를 살펴 이상한 패널을 찾아보자.", ObjectiveMarkerMode.Area, "패널 위치 찾기", "오웬을 부를지 결정하기", "먼지와 섬유 채취하기"),
            O("D2-06", ObjectiveActionType.Find, "다니엘의 객실 수색하기", "책상과 가방, 단말을 직접 살펴보자.", ObjectiveMarkerMode.Area, "예약 기사 찾기", "태블릿 흔적 확인하기", "익명 제보 채팅 살펴보기", "클레어의 행동 단서 확인하기"),
            O("D3-01", ObjectiveActionType.Review, "공개된 기사 확인하기", "기사 화면과 공개 시각을 확인하자.", ObjectiveMarkerMode.Hover, "기사 화면 살펴보기", "공개 시각 확인하기", "리처드와 클레어에게 이야기하기", "공개 해명을 권할지 결정하기"),
            O("D3-02", ObjectiveActionType.Present, "리처드의 은폐를 추궁하기", "리처드에게 기록을 제시하고 설명을 들어보자.", ObjectiveMarkerMode.Npc, "리처드와 이야기하기", "은폐 기록 제시하기", "심문 태도 선택하기", "줄리언에 관해 묻기"),
            O("D3-03", ObjectiveActionType.Talk, "토마스에게 원본 기록의 행방 묻기", "토마스를 클릭해 원본 기록에 관해 물어보자.", ObjectiveMarkerMode.Npc, "토마스와 이야기하기", "다니엘에게 알려준 내용을 묻기", "지목하거나 설득하기"),
            O("D3-04", ObjectiveActionType.Find, "금고 접근 기록 추적하기", "모듈과 인증 패널에서 필요한 기록을 찾아보자.", ObjectiveMarkerMode.Hover, "데이터 모듈 조사하기", "이중 인증 패널 확인하기", "21시 05분 기록 찾기", "사용된 인증 추적하기"),
            O("D3-05", ObjectiveActionType.Compare, "익명 제보자의 문체 확인하기", "클레어의 설명과 채팅 기록의 표현을 비교하자.", ObjectiveMarkerMode.Npc, "클레어와 이야기하기", "채팅 기록 열기", "반복되는 표현 비교하기", "문체 특징 저장하기"),
            O("D4-01", ObjectiveActionType.Present, "마커스의 인증 사용을 추궁하기", "마커스에게 관련 기록을 제시해 사실을 확인하자.", ObjectiveMarkerMode.Npc, "마커스와 이야기하기", "도박 채무 기록 제시하기", "이블린과의 연락 확인하기", "대응 방식을 결정하기"),
            O("D4-02", ObjectiveActionType.Inspect, "마커스의 추락 현장 조사하기", "계단 주변을 넓게 살핀 뒤 흔적을 조사하자.", ObjectiveMarkerMode.Area, "마커스의 상태 확인하기", "젖은 계단 조사하기", "장갑 섬유 채취하기", "이블린을 구금할지 결정하기"),
            O("D4-03", ObjectiveActionType.Solve, "추락 경로 재구성하기", "발자국과 난간의 흔적으로 움직임을 재구성하자.", ObjectiveMarkerMode.None, "발자국 간격 확인하기", "난간 손자국 조사하기", "동작 카드 배열하기", "추락 원인 결론 내리기"),
            O("D4-04", ObjectiveActionType.Talk, "마커스에게 핵심 질문하기", "마커스를 클릭해 답할 수 있는 질문을 건네자.", ObjectiveMarkerMode.Npc, "마커스의 상태 확인하기", "예·아니오 질문 선택하기", "인증 제공 여부 확인하기"),
            O("D5-01", ObjectiveActionType.Inspect, "클레어의 객실 사건 조사하기", "클레어의 상태와 객실의 변화를 확인하자.", ObjectiveMarkerMode.Area, "클레어의 상태 확인하기", "출입문 기록 조사하기", "연기와 자동커튼 확인하기"),
            O("D5-02", ObjectiveActionType.Solve, "침입 없이 현장이 만들어진 방법 밝히기", "객실의 장치와 흔적 사이 모순을 연결하자.", ObjectiveMarkerMode.Area, "서비스 로봇 조사하기", "적재함의 물기 확인하기", "드라이아이스 흔적 찾기", "클레어의 진술과 모순 연결하기", "태블릿 찾기"),
            O("D5-03", ObjectiveActionType.Present, "클레어에게 태블릿 절도를 확인하기", "클레어에게 확보한 증거를 제시해 확인하자.", ObjectiveMarkerMode.Npc, "클레어와 이야기하기", "자작극 증거 제시하기", "태블릿에 관해 추궁하기", "대응 방식을 결정하기", "전체 채팅 확보하기"),
            O("D5-04", ObjectiveActionType.Solve, "첫 사건의 자동장치 가설 완성하기", "현장 기록을 추리 보드에서 하나의 가설로 연결하자.", ObjectiveMarkerMode.None, "세면대 장치 확인하기", "천장 패널 기록 불러오기", "감지기 오류 연결하기", "증거 보드에서 가설 저장하기"),
            O("D6-01", ObjectiveActionType.Find, "비정상적인 무게 이동 찾기", "안정화 로그와 파도 기록을 같은 시간축에서 살펴보자.", ObjectiveMarkerMode.Hover, "오웬과 이야기하기", "안정화 로그 열기", "파도 기록과 시간축 맞추기", "기상과 무관한 변동 찾기", "이동 무게 계산하기"),
            O("D6-02", ObjectiveActionType.Chase, "화물 레일 경로 따라가기", "레일의 분기와 스위치를 따라 경로를 확인하자.", ObjectiveMarkerMode.Edge, "레일 진입구 찾기", "오웬과 이야기하기", "분기 스위치 조사하기", "시험 카트 경로 설정하기", "호라이즌 방향 분기 확인하기"),
            O("D6-03", ObjectiveActionType.Compare, "밸러스트 구역의 흔적 검증하기", "바닥과 배수구를 살핀 뒤 확보한 흔적과 대조하자.", ObjectiveMarkerMode.Area, "검은 바닥 조사하기", "다니엘의 구두 흔적과 비교하기", "세척 흔적 찾기", "루미놀 조사하기", "질소 설비 기록 확인하기"),
            O("D6-04", ObjectiveActionType.Compare, "다니엘의 직접 사인 확정하기", "헬레나와 검시 자료를 비교해 사망 순서를 정리하자.", ObjectiveMarkerMode.Npc, "헬레나와 이야기하기", "자상 기록 확인하기", "폐와 혈액 소견 비교하기", "보호면 파편 조사하기", "사망 순서 배열하기"),
            O("D6-05", ObjectiveActionType.Solve, "21시 22분부터 22시 45분까지 재구성하기", "사건 카드를 시간순으로 배치하자.", ObjectiveMarkerMode.None, "마지막 목격 배열하기", "실제 사망 시점 배치하기", "시신 이동 기록 연결하기", "감지기 오류와 발견 시각 배치하기"),
            O("D7-01", ObjectiveActionType.Chase, "원본 모듈의 파괴를 막기", "경보와 이동 흔적을 따라 모듈을 확보하자.", ObjectiveMarkerMode.Edge, "가짜 화재경보 확인하기", "이블린을 따라가기", "추격 경로 결정하기", "모듈 확보하기"),
            O("D7-02", ObjectiveActionType.Compare, "압수한 보호면 조사하기", "보호면의 시료와 확보한 기록을 대조하자.", ObjectiveMarkerMode.None, "압수 근거 구성하기", "보호면 내부 시료 확인하기", "다니엘의 유전자 정보와 비교하기", "손톱의 수지 흔적 대조하기"),
            O("D7-03", ObjectiveActionType.Solve, "오르페우스 호 원본 음성 복원하기", "오디오 단말에서 손상된 음성을 복원하자.", ObjectiveMarkerMode.Hover, "손상된 음성 열기", "기계음 제거하기", "재생 속도 보정하기", "파형 조각 배열하기", "핵심 발언 확인하기"),
            O("D7-04", ObjectiveActionType.Decide, "이블린의 제안에 대응하기", "이블린의 제안을 듣고 대응 방식을 정하자.", ObjectiveMarkerMode.Npc, "이블린과 이야기하기", "제안의 조건 듣기", "대응 방식 결정하기", "추가 발언 확인하기"),
            O("D8-01", ObjectiveActionType.Solve, "최종 논증 완성하기", "확보한 인물·장소·기록으로 사건을 논증하자.", ObjectiveMarkerMode.None, "다니엘을 유인한 인물 지목하기", "실제 사건 장소 선택하기", "직접 사인 선택하기", "시신 운반 방법 선택하기", "유인 동기 선택하기", "과거 사건의 설계자 선택하기", "공개 범위 결정하기"),
            O("D8-02", ObjectiveActionType.Talk, "선미 갑판에서 이블린과 대면하기", "이블린을 클릭해 확보한 논증으로 대면하자.", ObjectiveMarkerMode.Npc, "이블린의 정당화 듣기", "확보한 논증으로 반박하기", "마지막 말을 결정하기"),
            O("D8-03", ObjectiveActionType.Review, "사건의 결과 확인하기", "기사와 후일담, 사건 평가를 차례로 확인하자.", ObjectiveMarkerMode.None, "수정된 기사 확인하기", "생존자들의 후일담 보기", "사건 평가 확인하기", "엔딩 선택 결과 보기")
        };

        public static IReadOnlyList<ProductionObjectiveDefinition> All => Entries;

        static ProductionObjectiveCatalog()
        {
            if (Entries.Length != ProductionSceneCatalog.All.Count ||
                Entries.Where((entry, index) =>
                        entry.SceneId != ProductionSceneCatalog.All[index].SceneId)
                    .Any())
            {
                throw new InvalidOperationException(
                    "Production objective data must match the scene catalog.");
            }
        }

        private static ProductionObjectiveDefinition O(
            string sceneId,
            ObjectiveActionType actionType,
            string displayText,
            string detailText,
            ObjectiveMarkerMode markerMode,
            params string[] steps)
        {
            if (!ProductionSceneCatalog.TryGet(
                    sceneId,
                    out ProductionSceneDefinition scene))
            {
                throw new InvalidOperationException(
                    $"Unknown production scene for objective '{sceneId}'.");
            }

            return new ProductionObjectiveDefinition(
                scene,
                actionType,
                displayText,
                detailText,
                markerMode,
                steps);
        }
    }
}
