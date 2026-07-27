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
- 폐기된 시스템: Theory Slots

게임 이름은 사용자에게 표시되는 모든 문서와 UI에서 `Under the Horizon`으로 쓴다.
