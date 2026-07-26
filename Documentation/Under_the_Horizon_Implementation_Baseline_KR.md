# Under the Horizon 구현 기준서

## 1. 문서 목적

이 문서는 공식 기획 자료 네 종과 현재 Unity 구현 사이의 연결 기준을 정의한다.

기획 원본을 다시 해석하지 않고 프로젝트에서 확인해야 할 계약을 한곳에 모은다.

새 기능, 버그 수정, 콘텐츠 동기화, QA는 이 기준서를 출발점으로 삼는다.

세부 문구와 장면 데이터가 충돌하면 공식 원본의 우선순위를 따른다.

이 문서는 공식 원본을 대체하지 않는다.

## 2. 게임 식별 정보

- 공식 게임명: `Under the Horizon`
- 배경 선박: `MV Elysium`
- 문서 언어: 한국어
- 기준 개정일: `2026-07-27`
- Unity 프로젝트 진입 장면: `UI Basic Scene`
- 공식 원본 디렉터리: `Documentation/Source`

사용자에게 보이는 제목, 저장 데이터 설명, 문서 제목에는 공식 게임명을 쓴다.

이전 작업명은 새 문서나 UI에 다시 추가하지 않는다.

레거시 식별자는 저장 데이터 마이그레이션에 필요할 때만 코드 내부에서 유지한다.

## 3. 공식 원본

### 3.1 파일 목록

1. `Under_the_Horizon_Dialogue_Complete_KR.xlsx`
2. `Under_the_Horizon_Production_Manual_KR.pdf`
3. `Under_the_Horizon_Game_Scenario_KR.pdf`
4. `Under_the_Horizon_MV_Elysium_Cruise_Structure_Map_KR.pdf`

### 3.2 원본 우선순위

1. XLSX의 `Dialogue_Master`, `Choice_Flow`, `Scene_Index`
2. 게임 시나리오의 사건 진상, 인물, 장면, 엔딩 정의
3. 크루즈 구조도의 장소 코드와 Deck 연결
4. 프로덕션 매뉴얼의 구현 및 QA 규격
5. 이 기준서를 포함한 프로젝트 내부 문서
6. 코드 주석과 임시 메모

하위 문서가 상위 원본과 다르면 하위 문서와 구현을 수정한다.

원본끼리 충돌하면 임의 각색하지 않고 충돌 항목을 기록해 기획 결정을 받는다.

### 3.3 원본 무결성

원본 식별값은 `Documentation/Source/sources.json`에 보관한다.

- XLSX SHA-256: `F3330B3775B5FDA57778C34877103DC7F60E39B9C8DD9BE626D41EA01C1B3020`
- 프로덕션 매뉴얼 SHA-256: `1116AD83AA70655CE765B1110B537CDC2BA35C8DE9D708AAB5A2EEAE1C32EEDA`
- 게임 시나리오 SHA-256: `E9AF158CBE15852E0B79A911D0FA5F64C9DC7D4CB0865C36BCC20F64814468EB`
- 구조도 SHA-256: `8AECC10E86409775F32FA928EC53B3BEBC9422878E21D8F4A56823A3C64635A8`

PDF 페이지 계약은 다음과 같다.

- 프로덕션 매뉴얼: 42쪽
- 게임 시나리오: 16쪽
- 크루즈 구조도: 7쪽

원본을 교체할 때 파일명만 같다고 동일한 개정판으로 간주하지 않는다.

해시, 바이트 크기, 페이지 수, 데이터 기대값을 함께 갱신한다.

## 4. 공식 콘텐츠 계약

### 4.1 수량

- 장면: 41개
- 메인 대사: 1,063개
- 선택지: 90개
- 동적 승객 및 승무원 바크: 32개
- 핵심 증거: 18개
- 최종 엔딩: 4개
- 최종 심문 단계: 6단계

### 4.2 XLSX 시트

공식 대사집에는 다음 9개 시트가 있다.

1. `Read_Me`
2. `Dialogue_Master`
3. `Scene_Index`
4. `Choice_Flow`
5. `Ambient_Barks`
6. `Character_Voice`
7. `Evidence_Master`
8. `Variables`
9. `QA_Coverage`

`Dialogue_Master`의 데이터 행은 1,063개이며 `line_id`는 모두 고유해야 한다.

`Scene_Index`의 장면은 41개이며 `scene_id`는 모두 고유해야 한다.

`Choice_Flow`의 선택지는 90개이며 `choice_id`는 모두 고유해야 한다.

대사와 선택지가 참조하는 장면은 모두 `Scene_Index`에 존재해야 한다.

`QA_Coverage`의 계약 결과는 모두 `PASS`여야 한다.

## 5. 사건 진상 계약

최종 심문과 엔딩 판정은 다음 진상을 기준으로 한다.

- 피해자: Daniel Mercer
- 범인: Evelyn Shaw
- 실제 살해 장소: Ballast Control Annex
- 직접 사인: 질소 질식
- 시신 이동 수단: 천장 서비스 레일
- 핵심 동기: Richard를 범인으로 믿게 만든 오판
- MV Orpheus 사건 설계자: Evelyn Shaw

사건 타임라인의 핵심 시각은 다음과 같다.

- 실제 살해 시각: 21:45
- 시신 이동 완료 시각: 22:18

Horizon Room은 시신 발견 장소이며 실제 살해 장소가 아니다.

이 차이는 최종 심문, 타임라인 퍼즐, 장소 조사에서 일관되어야 한다.

## 6. 장면 흐름 계약

### 6.1 프롤로그

| 장면 | 원본 위치 코드 | 역할 |
| --- | --- | --- |
| P-01 | PORT | 항구의 기자 |
| P-02 | GANGWAY | 승선 명단의 오류 |
| P-03 | DECK10_SUITE | 회장의 부탁 |

### 6.2 Day 1

| 장면 | 원본 위치 코드 | 역할 |
| --- | --- | --- |
| D1-01 | DECK8_ATRIUM | 승객 소개 |
| D1-02 | DECK9_DINING | 불편한 만찬 |
| D1-03 | DECK9_BALLROOM | 선상 파티 |
| D1-04 | SERVICE7 | 사라진 기자 |
| D1-05 | DECK9_BALLROOM | 수상한 호출 |
| D1-06 | HORIZON | 발견 |
| D1-07 | MEDBAY | 비밀 수사 계약 |

### 6.3 Day 2

| 장면 | 원본 위치 코드 | 역할 |
| --- | --- | --- |
| D2-01 | HORIZON | 현장 재검증 |
| D2-02 | HORIZON | 피의 방향 |
| D2-03 | MEDBAY | 사망 시각 |
| D2-04 | SECURITY | 카메라의 맹점 |
| D2-05 | HORIZON | 천장 레일 |
| D2-06 | CABIN_DANIEL | 기자의 객실 |

### 6.4 Day 3

| 장면 | 원본 위치 코드 | 역할 |
| --- | --- | --- |
| D3-01 | NEWS_LOUNGE | 예약 기사 공개 |
| D3-02 | DECK10_SUITE | Richard의 자백 1 |
| D3-03 | BRIDGE | Thomas의 침묵 |
| D3-04 | VAULT | 봉인된 기록 |
| D3-05 | PROMENADE | 익명 제보자의 문장 |

### 6.5 Day 4

| 장면 | 원본 위치 코드 | 역할 |
| --- | --- | --- |
| D4-01 | SECURITY | Marcus의 거짓말 |
| D4-02 | STAIR_B | 계단 추락 |
| D4-03 | STAIR_B | 사고의 재구성 |
| D4-04 | MEDBAY | 말하지 못한 증언 |

### 6.6 Day 5

| 장면 | 원본 위치 코드 | 역할 |
| --- | --- | --- |
| D5-01 | CABIN_CLAIRE | 두 번째 불가능 사건 |
| D5-02 | CABIN_CLAIRE | 자작극 |
| D5-03 | INTERVIEW | Claire의 자백 |
| D5-04 | HORIZON | 자동으로 완성된 방 |

### 6.7 Day 6

| 장면 | 원본 위치 코드 | 역할 |
| --- | --- | --- |
| D6-01 | ENGINE_CTRL | 안정화 로그 |
| D6-02 | SERVICE_RAIL | 천장 위의 길 |
| D6-03 | BALLAST | 검은 바닥 |
| D6-04 | FORENSIC | 두 번의 죽음 |
| D6-05 | EVIDENCE_BOARD | 타임라인 퍼즐 |

### 6.8 Day 7

| 장면 | 원본 위치 코드 | 역할 |
| --- | --- | --- |
| D7-01 | VAULT | 마지막 파괴 시도 |
| D7-02 | FORENSIC | 보호면의 침방울 |
| D7-03 | ARCHIVE | 15년 전 목소리 |
| D7-04 | PROMENADE | Evelyn의 제안 |

### 6.9 Day 8

| 장면 | 원본 위치 코드 | 역할 |
| --- | --- | --- |
| D8-01 | HORIZON | 최종 심문 |
| D8-02 | STERN | 마지막 대치 |
| D8-03 | PORT | 귀항 |

장면 진행은 프롤로그부터 Day 8까지 단절 없이 이어져야 한다.

선택 분기가 있어도 공식 장면 수와 최종 도달 가능성을 바꾸지 않는다.

## 7. 장소와 배경 계약

프로젝트는 25개의 정규 물리 장소를 사용한다.

각 장소는 `CanonicalLocationCatalog`의 코드, Deck, 방 코드, 배경 파일을 따른다.

### 7.1 Deck 10 및 외부

- `PORT`
- `GANGWAY`
- `RICHARD_SUITE`
- `VIP_LOUNGE`
- `OPEN_DECK`

### 7.2 Deck 9

- `BALLROOM`
- `DINING`
- `PROMENADE`
- `HORIZON`

### 7.3 Deck 8

- `ATRIUM`
- `NEWS_LOUNGE`
- `SECURITY`
- `SERVICE_RAIL`

### 7.4 Deck 7

- `MEDBAY`
- `BALLAST_CONTROL_ANNEX`
- `ENGINE_CONTROL`
- `CREW_STAIRS`

### 7.5 Deck 6

- `VAULT`
- `ARCHIVE`
- `LAUNDRY`
- `SERVICE_HUB`

### 7.6 Deck 5

- `STABILIZERS`
- `BALLAST_TANKS`
- `GENERATOR`
- `WORKSHOP`

원본 대사 위치 코드는 정규 물리 장소와 다를 수 있다.

다음 별칭은 정규 장소로 해석한다.

| 원본 별칭 | 정규 장소 |
| --- | --- |
| DECK10_SUITE | RICHARD_SUITE |
| CABIN_CLAIRE | VIP_LOUNGE |
| STERN | OPEN_DECK |
| DECK9_BALLROOM | BALLROOM |
| DECK9_DINING | DINING |
| DECK8_ATRIUM | ATRIUM |
| CABIN_DANIEL | NEWS_LOUNGE |
| EVIDENCE_BOARD | NEWS_LOUNGE |
| INTERVIEW | SECURITY |
| FORENSIC | MEDBAY |
| BALLAST | BALLAST_CONTROL_ANNEX |
| ENGINE_CTRL | ENGINE_CONTROL |
| BRIDGE | ENGINE_CONTROL |
| STAIR_B | CREW_STAIRS |
| SERVICE7 | CREW_STAIRS |

모든 41개 장면은 경고 없는 정규 배경 위치를 가져야 한다.

## 8. UI 화면 계약

### 8.1 기준 해상도

- 주 기준: 1920x1080, 16:9
- 지원 기준: 1920x1200, 16:10

16:10에서 버튼이 보인다는 이유로 16:9를 포기하지 않는다.

두 비율 모두 같은 게임 흐름과 조작 가능 영역을 제공해야 한다.

### 8.2 배경 표시

배경 원본이 16:9여도 16:10 화면에서 늘이거나 찌그러뜨리지 않는다.

`BackgroundCoverLayout`의 cover 규칙으로 화면을 채우고 중앙 기준으로 일부를 자른다.

중요 단서와 상호작용 대상은 안전 영역 안에 둔다.

### 8.3 대사 UI

- 대사 패널은 화면 하단 안전 영역 안에 있어야 한다.
- 인물 이름은 한 줄 말줄임으로 처리한다.
- 긴 대사는 마스크 또는 스크롤 영역 안에 남아야 한다.
- 다음 버튼은 대사 패널 내부에서 조작 가능해야 한다.
- 인물 초상은 왼쪽 영역에 비율을 유지해 배치한다.
- 초상이 대사, 선택지, 상태 HUD를 가리지 않아야 한다.

### 8.4 상단 패널

증거, 지도, 설정 버튼과 패널은 상태 HUD 아래의 안전 영역을 사용한다.

패널을 열고 닫아도 대사 진행 버튼이 화면 밖으로 이동하지 않아야 한다.

루트 `Ingame` 오브젝트는 전체 캔버스 stretch와 scale 1을 유지한다.

## 9. 대사 동기화 계약

XLSX가 유일한 대사 편집 원본이다.

Unity용 CSV를 직접 수정하지 않는다.

동기화 명령은 다음과 같다.

```powershell
python Tools/DialogueSync/export_dialogue.py
python -m unittest Tools/DialogueSync/test_export_dialogue.py
```

생성 결과는 다음 세 파일이다.

- `Assets/_Project/Content/Dialogue/Under_the_Horizon_Dialogue_KR.csv`
- `Assets/_Project/Content/Dialogue/Under_the_Horizon_Choices_KR.csv`
- `Assets/_Project/Content/Dialogue/Under_the_Horizon_Scene_Index_KR.csv`

CSV는 UTF-8 BOM과 LF 줄바꿈을 사용한다.

`DialogueCsvParser`가 CSV 구조를 읽는다.

`ProductionDialogueRuntime`이 조건, 선택지, 효과, 다음 장면을 실행한다.

`OfficialDialogueContractValidator`가 공식 수량과 참조 무결성을 검사한다.

## 10. 최종 심문과 엔딩

최종 심문은 D8-01에서 6단계로 진행한다.

단계 순서는 범인, 장소, 사인, 운반, 동기, Orpheus 설계자다.

정답 선택은 사건 진상 계약과 정확히 일치해야 한다.

오답 누적은 게임 상태에 기록되어 저장과 불러오기 뒤에도 유지되어야 한다.

공식 엔딩은 다음 네 개뿐이다.

| 노선 | 표시명 | 내부 식별자 |
| --- | --- | --- |
| A | Complete Wake | ending_a_complete |
| B | Convenient Culprit | ending_b_convenient_culprit |
| C | The Wrong Man | ending_c_wrong_person |
| Bad | Panic at Sea | ending_bad_panic |

A와 B는 D8-02를 거쳐 마지막 대치로 진행한다.

C와 Bad는 D8-03 귀항 흐름으로 진행한다.

레거시 무결성 실패 엔딩은 공식 Bad 엔딩으로 정규화한다.

`Theory Slots`는 폐기된 시스템이며 새 UI나 저장 데이터에 추가하지 않는다.

## 11. 저장과 진행 상태

게임 상태는 현재 장면, 위치, 증거, 선택 결과, 심문 오답, 엔딩을 보존해야 한다.

대사 도중 저장된 체크포인트는 복원 후 같은 진행 맥락을 유지해야 한다.

완료한 장면을 다시 열어 진행도가 중복 증가하지 않아야 한다.

최종 엔딩이 기록된 저장은 재개 시 해당 엔딩 화면을 복원해야 한다.

레거시 저장 필드는 현재 공식 계약으로 마이그레이션한 뒤 읽는다.

## 12. 검증 기준

### 12.1 정적 계약

- 공식 원본 네 파일의 해시가 manifest와 일치한다.
- 9개 XLSX 시트를 모두 읽을 수 있다.
- 수식 오류 표식이 없어야 한다.
- 41개 장면과 25개 장소가 모두 연결된다.
- C-01부터 C-18까지 증거가 모두 존재한다.
- 최종 심문은 정확히 6단계다.
- 엔딩 집합은 A, B, C, Bad다.

### 12.2 UI 계약

- 1920x1080에서 모든 주요 버튼을 누를 수 있다.
- 1920x1200에서 모든 주요 버튼을 누를 수 있다.
- 두 비율에서 대사 텍스트가 화면 밖으로 나가지 않는다.
- 초상과 배경의 종횡비가 유지된다.
- 패널 전환 뒤에도 다음 대사 진행이 가능하다.

### 12.3 빌드 검사

Unity가 닫혀 있거나 자동화 연결을 사용할 수 없을 때는 테스트 어셈블리 컴파일을 확인한다.

```powershell
dotnet build Wake.EditModeTests.csproj --no-restore --nologo
dotnet build Wake.PlayModeTests.csproj --no-restore --nologo
```

이 명령은 Unity Test Runner 실행을 대신하지 않는다.

최종 수동 QA에서는 Unity에서 EditMode와 PlayMode 테스트를 실제 실행한다.

### 12.4 플레이 스모크 경로

1. `UI Basic Scene`을 연다.
2. 1920x1080 Game View로 시작한다.
3. 새 게임으로 P-01을 시작한다.
4. 대사, 선택지, 다음 장면 전환을 확인한다.
5. 증거, 지도, 설정 패널을 각각 연다.
6. 1920x1200으로 바꾸고 같은 조작을 반복한다.
7. 퍼즐 장면 진입과 성공 및 실패 피드백을 확인한다.
8. D8-01 6단계 심문을 확인한다.
9. A, B, C, Bad 네 엔딩 도달 경로를 확인한다.
10. 저장 후 재시작해 진행 상태를 복원한다.

## 13. 공식 원본 갱신 절차

1. 새 원본 네 파일의 이름이 공식 파일명과 같은지 확인한다.
2. 원본을 `Documentation/Source`에 교체한다.
3. 네 파일의 SHA-256과 크기를 계산한다.
4. PDF 페이지 수와 표지 게임명을 확인한다.
5. XLSX의 9개 시트와 계약 수량을 확인한다.
6. `sources.json`을 새 개정 정보로 갱신한다.
7. 대사 동기화 도구를 실행한다.
8. 생성 CSV diff에서 의도하지 않은 손실을 확인한다.
9. 공식 계약 검증 테스트를 갱신한다.
10. 관련 구현과 이 기준서를 함께 수정한다.

## 14. 변경 리뷰 체크리스트

- 변경 제목이 `feat:`, `fix:`, `docs:`, `test:` 중 목적에 맞는 접두사를 쓰는가?
- 제목과 커밋 메시지가 한국어로 변경 내용을 설명하는가?
- 공식 원본과 생성 결과를 구분했는가?
- 한 PR의 의미 있는 변경량이 300~500줄인가?
- 앞선 PR의 병합을 기다리지 않는 올바른 stacked base를 사용했는가?
- PR을 Draft가 아닌 Ready 상태로 열었는가?
- 사용자 소유 이미지 메타 변경을 포함하지 않았는가?
- 관련 정적 계약과 어셈블리 컴파일을 확인했는가?
- Unity에서 실행하지 못한 검증을 실행했다고 표현하지 않았는가?
- 다음 PR이 현재 PR을 base로 이어갈 수 있는가?

## 15. 현재 기준의 완료 정의

프로젝트가 완료로 간주되려면 다음 조건을 모두 만족해야 한다.

- 공식 게임명이 모든 사용자용 문서와 UI에 반영된다.
- 공식 XLSX의 1,063개 대사와 90개 선택지가 실행된다.
- 41개 장면을 처음부터 끝까지 진행할 수 있다.
- 25개 정규 배경이 장면 위치와 연결된다.
- 18개 증거를 수집하고 열람할 수 있다.
- 필수 퍼즐을 진행하고 피드백을 받을 수 있다.
- D8-01의 6단계 최종 심문이 동작한다.
- A, B, C, Bad 네 엔딩을 도달할 수 있다.
- 16:9와 16:10에서 UI 조작이 가능하다.
- 저장 및 불러오기 뒤 진행 상태가 유지된다.
- 공식 원본, manifest, 생성 CSV, 코드, 테스트, 문서가 같은 계약을 가리킨다.
