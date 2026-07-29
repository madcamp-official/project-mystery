# Under the Horizon 공식 문서

이 디렉터리는 게임 구현의 기준이 되는 기획 문서와 콘텐츠 동기화 규칙을 보관한다.
Unity가 PDF와 XLSX를 불필요하게 임포트하지 않도록 원본 파일은 `Assets` 밖에 둔다.

## 공식 원본

`Source`에 있는 다음 네 파일을 구현 판단의 기준으로 사용한다.

- `Under_the_Horizon_Dialogue_Complete_KR.xlsx`
- `Under_the_Horizon_Production_Manual_KR.pdf`
- `Under_the_Horizon_Game_Scenario_KR.pdf`
- `Under_the_Horizon_MV_Elysium_Cruise_Structure_Map_KR.pdf`

파일 이름, SHA-256, 페이지 수와 데이터 기대값은 `Source/sources.json`에서 관리한다.
원본을 교체할 때는 같은 파일 이름을 유지하고 해시와 검증 기대값을 함께 갱신한다.

공식 원본과 현재 Unity 구현의 연결 기준, 장면·장소·UI·엔딩·QA 계약은
`Under_the_Horizon_Implementation_Baseline_KR.md`에서 확인한다.

현재 승인된 UI 표현과 상호작용 결정은
`Under_the_Horizon_UI_Decision_Record_KR.md`에서 확인한다. 이 결정 기록은
사용자에게 보이는 UI 표현에 한해 기존 프로덕션 매뉴얼과 내부 문서의 오래된
UI 설명보다 우선한다.

## 오디오 구현 문서

오디오 관련 자료는 `Audio` 디렉터리에서 다음 순서로 읽는다.

1. `Under_the_Horizon_Audio_Implementation_Guide_KR_v2.md`
   - 런타임 소스 구성, 큐 우선순위와 필수 QA 항목
2. `Under_the_Horizon_Audio_Cue_Config_v2.json`
   - 리소스 키, 장소 기본값, 장면·이벤트 큐의 기계 판독 원본
3. `Under_the_Horizon_Scene_Audio_Cues_v2.csv`
   - 41개 프로덕션 장면의 BGM·앰비언스·SFX 배정표
4. `Under_the_Horizon_Audio_Cue_Sheet_KR_v2.xlsx`
   - 사람이 검토하고 조정하기 위한 통합 오디오 작업표

Unity 런타임의 장소 기본 큐는
`Assets/_Project/Code/Core/AudioCueCatalog.cs`에서 관리한다. 문서의 큐를
변경할 때는 카탈로그와 관련 테스트도 함께 갱신한다.

## 맵 시스템 문서

맵 개편의 물리 장소 계약, 레이어 해금 시점, 이동 등급과 스토리 검수 기준은
`Map/Under_the_Horizon_Map_System_Overhaul_KR.md`에서 확인한다.

원본 층별 레이어 ZIP, 매니페스트와 제작 안내는
`Source/MapLayers`에 보관한다. Unity 런타임에는 이 중 Base, Restricted,
Technical 레이어만 Sprite 자산으로 복사한다.

## 내부 UI 구현 문서

UI 문서는 다음 순서로 읽는다.

1. `Under_the_Horizon_UI_Decision_Record_KR.md`
   - 사용자에게 보이는 UI 표현과 폐기할 기존 기준
2. `Under_the_Horizon_UI_Wireframe_Spec_KR.md`
   - 38개 기본 화면, 2개 상태 변형, WF-01~WF-40과 공통 7구역
3. `Under_the_Horizon_UI_Wireframe_Production_Order_KR.md`
   - WF-01~WF-40의 제작 차수, 선행 조건과 검수 게이트
4. `Under_the_Horizon_UI_State_Transition_Matrix_KR.md`
   - 후속 단계에서 작성할 장면·화면 상태·복귀 경로 매핑

공식 PDF와 XLSX는 위 내부 UI 문서를 반영하기 위해 직접 덮어쓰지 않는다.
프로덕션 매뉴얼 원본을 개정할 때는 `Source/sources.json`의 해시, 크기와 페이지
계약을 함께 갱신한다.

## 대사 데이터 흐름

```text
공식 XLSX
  -> Tools/DialogueSync/export_dialogue.py
  -> Unity용 UTF-8 BOM CSV
  -> DialogueCsvParser
  -> ProductionDialogueRuntime
```

XLSX가 유일한 편집 원본이다. Unity용 CSV는 생성 결과이므로 직접 수정하지 않는다.
동기화 명령과 생성 파일 목록은 `Tools/DialogueSync/README.md`를 따른다.

## 문서 우선순위

충돌이 생기면 다음 순서로 판단한다.

1. 완성 대사집의 `Dialogue_Master`, `Choice_Flow`, `Scene_Index`
2. 게임 시나리오의 사건 진상, 등장인물, 장면 및 엔딩 정의
3. 크루즈 구조도의 장소 코드와 Deck 연결 관계
4. 프로덕션 매뉴얼의 구현 규격과 QA 기준
5. 프로젝트 내부 구현 문서와 코드 주석

코드가 공식 원본과 다르면 코드를 고치는 것을 기본으로 한다. 원본 자체의 모순이
발견되면 임의로 각색하지 않고 진단으로 남긴 뒤 기획 결정을 받는다.

## 현재 데이터 계약

- 장면: 41개
- 대사: 1,063개
- 선택지: 90개
- 핵심 증거: C-01부터 C-18까지 18개
- 엔딩: A, B, C, Bad
- 최종 심문: 6단계

게임 이름은 사용자에게 표시되는 모든 문서와 UI에서 `Under the Horizon`으로 쓴다.
