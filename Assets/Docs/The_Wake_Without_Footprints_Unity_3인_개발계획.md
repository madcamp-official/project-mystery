# The Wake Without Footprints - Unity 3인 개발 계획

> 개발 스포일러 포함. 범인, 범행 장소, 살해 방식, 시신 운반법과 엔딩 조건이 기록되어 있다.

## 문서 정보

| 항목 | 값 |
|---|---|
| 문서 상태 | 새 시나리오 기준 전면 개정 |
| 개정일 | 2026-07-24 |
| 엔진 기준 | Unity 6.3 LTS / 현재 프로젝트 `6000.3.20f1` |
| 개발 방식 | 3인 코어 팀, GitHub 협업, AI 보조 |
| 프로젝트 장르 | 2D 1인칭 조사·심문·증거 조합형 추리 어드벤처 |
| 목표 플랫폼 | Windows PC 우선 |
| 기준 언어 | 한국어 |
| 목표 플레이타임 | 10-14시간 |
| 현재 구현 상태 | `UI Basic Scene` 와이어프레임, MCP for Unity 설치 |

Unity 프로젝트 폴더명에는 이전 기획명 `SEAT_0A`가 남아 있다. 프로젝트 경로와 저장소 이름 변경은 Unity Hub 및 팀 개발 환경에 영향을 주므로 별도 PR에서 처리한다. 이 문서의 콘텐츠 기준명은 `The Wake Without Footprints`이다.

## 1. 기준 자료와 우선순위

### 1.1 기준 자료

| 우선순위 | 자료 | 역할 | SHA-256 |
|---:|---|---|---|
| 1 | `The_Wake_Without_Footprints_Game_Scenario_KR.pdf` v1.0 | 사건 진상, 인물, 챕터, 씬, 퍼즐, 단서, 엔딩의 최종 기준 | `BA2C510E4CBB6D8253E41C0E8D78BEAC2781C2A8103A1A21D545090BEF455298` |
| 2 | `MV_Elysium_Cruise_Structure_Map_KR.pdf` | Deck 5-10 배치, 범행 장소, 화물 레일 이동 경로, 장소 코드 | `F7DF5E4610ADA4B74CF699D7AA41E2ACDA613C7B64947F78F718FB09D38A59B2` |
| 3 | `The_Wake_Without_Footprints_Dialogue_KR.csv` | 프로토타입 대사, 선택지, 조건, 효과, 장소, 음성 대상 | `26D65C01E7F4279A55E301C91F029C36FBF5612B172E7E648E57019598C2FA6E` |
| 4 | 이 개발 계획 | Unity 구현 방식, 데이터 계약, 일정, QA 기준 | 본문 |

충돌 시 사건의 사실관계는 시나리오 PDF, 공간 연결은 구조도 PDF를 우선한다. CSV는 실행용 초안이므로 PDF와 충돌하면 CSV를 수정한다. 구현 편의를 위해 시나리오의 범인·시간·장소를 바꾸지 않는다.

### 1.2 이전 문서에서 폐기한 기준

이 문서는 기존 항공기 밀실 사건 `SEAT 0A` 계획을 대체한다.

| 이전 기준 | 새 기준 |
|---|---|
| Nightjar Airlines Flight 709 | 크루즈선 MV Elysium |
| 플레이어 Claire Hale | 플레이어 Adrian Vale |
| 피해자 Adrian Vale | 피해자 Daniel Mercer |
| 범인 Marcus Reed | 범인 Evelyn Shaw |
| 기내 화장실과 Seat 0A | Horizon Room과 Ballast Control Annex |
| 사전 설치 충격 장치 | 질식 살해 후 천장 화물 레일로 시신 이동 |
| 프롤로그 + Chapter 1-4 | 프롤로그 + Day 1-8 |
| 150-180분 | 10-14시간 |
| 필수 증거 E01-E16 | 핵심 단서 C-01-C-18 |
| 단일 결론과 짧은 변형 | A/B/C 엔딩 + Panic Bad End |
| Cabin Timeline | 21:22-22:45 사건 타임라인 |

기존 코드나 에셋에 `Seat0A`, `Flight709`, `Cabin`, `Airport`, `MarcusCulprit` 같은 식별자가 새로 추가되어서는 안 된다.

---

## 2. 제품 기준선

### 2.1 핵심 명제

Horizon Room의 문은 잠겨 있지 않지만 외벽 발판, 덕트, 점검구, 복도 어느 쪽에도 범인이 빠져나간 흔적이 없다. 해답은 “범인이 방에서 나가지 않았다”가 아니라 “범인이 애초에 방에 들어오지 않았다”이다.

Daniel은 Deck 7의 Ballast Control Annex에서 살해된다. Evelyn은 시신과 혈흔을 천장 화물 레일과 자동장치로 Horizon Room에 나중에 투입해 현장을 만든다.

### 2.2 플레이어 경험

플레이어는 전직 강력계 형사 출신 민간 탐정 Adrian Vale이다. 7박 8일의 항해 동안 다음 질문에 답해야 한다.

1. 누가 Daniel을 유인하고 살해했는가?
2. 실제 살해 장소는 어디인가?
3. 직접 사인은 무엇인가?
4. 시신은 어떻게 Horizon Room으로 이동했는가?
5. Daniel은 왜 Richard를 범인으로 확신했는가?
6. 15년 전 MV Orpheus 사건의 설계자는 누구인가?

### 2.3 핵심 루프

```text
장소 선택
-> 환경 관찰
-> 인물 대화·심문
-> 증거 획득·검사
-> 장소·시간·인물·기계 로그 연결
-> 가설 생성
-> 동선·타임라인 재구성
-> 다음 장소와 증언 해금
```

액션 조작보다 모순 발견과 논리 조합이 중심이다. 필수 단서는 반짝이는 오브젝트만 따라가서 얻는 방식이 아니라 환경의 불일치를 설명하도록 설계한다.

### 2.4 주요 진행 시스템

| 시스템 | 값 | 게임 영향 |
|---|---:|---|
| 인물별 Trust | 0-5 | 낮으면 핵심 증언 해금이 늦어짐 |
| Interrogation Pressure | 심문 중 변동 | 과도하면 증언 잠금 또는 거짓 자백 위험 |
| Public Anxiety | 0-100 | 70 이상이면 소요와 제한구역 폐쇄, 100이면 Bad End |
| Evidence Integrity | 0-100 권장 | 낮으면 일부 물증이 간접증거로 약화, 0이면 Bad End 조건 |
| Theory Slots | 3개 | 틀린 가설을 오래 유지하면 시간 블록 손실 |
| Time Blocks | 오전/오후/야간, 블록당 행동 2회 | 놓친 단서는 다른 경로로 보완 가능 |

수치의 초기값과 장면별 증감량은 기준 자료에 없으므로 밸런스 데이터로 분리하고 플레이테스트 후 확정한다.

---

## 3. 등장인물

| ID | 이름 | 역할 | 게임 기능 |
|---|---|---|---|
| `ADRIAN` | Adrian Vale, 42 | 플레이어, 민간 탐정 | 관찰, 심문, 타임라인 재구성 |
| `DANIEL` | Daniel Mercer, 38 | 피해자, 탐사보도 기자 | 잘못된 확신, 익명 제보 |
| `RICHARD` | Richard Hawthorne, 71 | 회장 | 과거 사건 은폐, Julian에 대한 죄책감 |
| `EVELYN` | Evelyn Shaw, 49 | COO 겸 수석비서, 진범 | 자동화 설비, 위조 초대, 통제 |
| `CLAIRE` | Claire Hawthorne, 35 | 후계자 | 태블릿 절도, 자작 습격 |
| `THOMAS` | Captain Thomas Reed, 57 | 선장 | Orpheus 원본 모듈의 존재 |
| `MARCUS` | Marcus Bell, 44 | 보안책임자 | 인증 제공, 도박 빚, 계단 추락 |
| `HELENA` | Dr. Helena Ward, 40 | 선박 전속의사 | 안정제, 사망 시각 오판, 법의학 |
| `OWEN` | Owen Price, 52 | 수석기관장 | 안정화 로그, 화물 레일, 과거 과실 |
| `JULIAN_RECORD` | Julian Hawthorne | 기록 음성 | Orpheus 사건의 Richard 무지 입증 |

`THOMAS`, `JULIAN_RECORD`, 승무원 NPC를 포함한 초상화·표정·음성 범위를 별도로 산정한다. 인물 ID는 저장 데이터와 현지화 키에 사용하므로 출시 후 변경하지 않는다.

---

## 4. 챕터와 씬 레지스터

CSV와 시나리오에는 총 41개 씬이 있다.

### 4.1 프롤로그

| ID | 시각 | 장소 | 목적 |
|---|---|---|---|
| P-01 | 15:10 | PORT | 관찰 튜토리얼, Daniel의 경고 |
| P-02 | 15:35 | GANGWAY | Richard 명의 초대와 명단 오류 |
| P-03 | 16:20 | DECK10_SUITE | 협박장, Orpheus 공식 설명, Richard 의뢰 |

### 4.2 Day 1 - 출항과 선상파티

| ID | 시각 | 장소 | 목적 |
|---|---|---|---|
| D1-01 | 17:00 | DECK8_ATRIUM | 주요 인물 소개 |
| D1-02 | 19:30 | DECK9_DINING | Daniel과 Claire의 언쟁 |
| D1-03 | 21:00 | DECK9_BALLROOM | 파티 동선과 카메라 |
| D1-04 | 21:22 | SERVICE7 | Daniel이 서비스 계단으로 향함 |
| D1-05 | 22:35 | DECK9_BALLROOM | Evelyn이 Richard에게 거짓 호출 전달 |
| D1-06 | 22:45 | HORIZON | 시신 발견과 현장 보존 선택 |
| D1-07 | 23:10 | MEDBAY | 비밀 수사 계약 |

### 4.3 Day 2 - 흔적이 없는 방

| ID | 시각 | 장소 | 목적 |
|---|---|---|---|
| D2-01 | 07:30 | HORIZON | 세 출구의 미사용 입증 |
| D2-02 | 08:40 | HORIZON | 혈흔 배열과 수직 낙하 |
| D2-03 | 09:30 | MEDBAY | 안정제와 사망 시각 오판 |
| D2-04 | 11:00 | SECURITY | 22:18 감지기 오류 |
| D2-05 | 14:00 | HORIZON | 천장 패널과 합성섬유 |
| D2-06 | 17:20 | CABIN_DANIEL | 기사 초안과 익명 채팅 |

### 4.4 Day 3 - 회장을 가리키는 모든 것

| ID | 시각 | 장소 | 목적 |
|---|---|---|---|
| D3-01 | 08:00 | NEWS_LOUNGE | 예약 기사 공개와 불안도 |
| D3-02 | 10:30 | DECK10_SUITE | Richard의 은폐 자백 |
| D3-03 | 13:00 | BRIDGE | 원본 모듈과 금고 |
| D3-04 | 16:00 | VAULT | 이중 인증과 덮어쓰기 |
| D3-05 | 20:00 | PROMENADE | Evelyn의 문장 습관 |

### 4.5 Day 4 - 고백 직전의 추락

| ID | 시각 | 장소 | 목적 |
|---|---|---|---|
| D4-01 | 08:30 | SECURITY | Marcus의 인증 대여 |
| D4-02 | 11:45 | STAIR_B | 계단 추락 현장 |
| D4-03 | 14:30 | STAIR_B | 자력 추락 재구성 |
| D4-04 | 18:00 | MEDBAY | 예/아니오 제한 심문 |

### 4.6 Day 5 - 두 번째 불가능 사건

| ID | 시각 | 장소 | 목적 |
|---|---|---|---|
| D5-01 | 09:00 | CABIN_CLAIRE | 연기 속 Claire 발견 |
| D5-02 | 11:00 | CABIN_CLAIRE | 서비스 로봇 자작극 규명 |
| D5-03 | 13:30 | INTERVIEW | 태블릿 절도와 비자금 자백 |
| D5-04 | 16:00 | HORIZON | 자동장치 현장 가설 |

### 4.7 Day 6 - 죽은 사람의 이동

| ID | 시각 | 장소 | 목적 |
|---|---|---|---|
| D6-01 | 07:00 | ENGINE_CTRL | 86kg 이동 로그 |
| D6-02 | 09:30 | SERVICE_RAIL | 레일 분기 추적 |
| D6-03 | 11:00 | BALLAST | 실제 살해 장소 확정 |
| D6-04 | 14:00 | FORENSIC | 질식 사인 확정 |
| D6-05 | 18:00 | EVIDENCE_BOARD | 21:22-22:45 타임라인 |

### 4.8 Day 7 - 금고와 15년 전 목소리

| ID | 시각 | 장소 | 목적 |
|---|---|---|---|
| D7-01 | 06:30 | VAULT | 모듈 파괴 저지 |
| D7-02 | 09:00 | FORENSIC | 보호면 DNA |
| D7-03 | 13:00 | ARCHIVE | Orpheus 음성 복원 |
| D7-04 | 18:00 | PROMENADE | Evelyn의 거래 제안 |

### 4.9 Day 8 - 최종 심문과 귀항

| ID | 시각 | 장소 | 목적 |
|---|---|---|---|
| D8-01 | 08:00 | HORIZON | 5단계 지목 퍼즐 |
| D8-02 | 09:00 | STERN | Evelyn과 마지막 대치 |
| D8-03 | 11:30 | PORT | 후일담과 엔딩 |

---

## 5. MV Elysium 공간 기준

### 5.1 Deck 5-10

| Deck | 선수에서 선미 방향 |
|---:|---|
| 10 | Richard Suite / VIP Lounge / Open Deck |
| 9 | Ballroom / Dining / Promenade / Horizon Room |
| 8 | Atrium / News Lounge / Security / Service Rail |
| 7 | Medbay / Ballast Annex / Engine Control / Crew Stairs |
| 6 | Vault / Archive / Laundry / Service Hub |
| 5 | Stabilizers / Ballast Tanks / Generator / Workshop |

### 5.2 확정 장소 코드

| 코드 | 장소 | 용도 |
|---|---|---|
| PORT | 승선항 | 프롤로그·귀항 |
| GANGWAY | 승선 통로 | 명단 오류 |
| DECK10_SUITE | Richard 스위트 | 회장 심문 |
| DECK9_BALLROOM | 볼룸 | 파티·Evelyn 알리바이 |
| DECK9_DINING | 다이닝 | Day 1 만찬 |
| DECK8_ATRIUM | 아트리움 | 인물 소개 |
| NEWS_LOUNGE | 뉴스 라운지 | 예약 기사 공개 |
| PROMENADE | 산책 갑판 | 문체 단서·거래 |
| HORIZON | Horizon Room | 발견·검증·최종 심문 |
| SERVICE7 | 7층 서비스 구역 | Daniel 동선 |
| BALLAST | Ballast Control Annex | 실제 살해 |
| ENGINE_CTRL | 기관 제어실 | 안정화 로그 |
| VAULT | 보안 금고 | 원본 모듈 |
| SECURITY | 보안실 | CCTV·인증 |
| MEDBAY | 의무실 | 부검·Marcus |
| STAIR_B | 승무원 계단 B | 추락 사고 |
| SERVICE_RAIL | 천장 화물 레일 | 시신 이동 |
| CABIN_CLAIRE | Claire 객실 | 자작극 |
| ARCHIVE | 선내 기록실 | Orpheus 복원 |

### 5.3 가상 설비와 사건 경로

```text
21:30 Daniel이 BALLAST 도착
21:42 공격
21:45 질소성 불활성 가스로 사망
21:48 운반백 적재
21:48-22:18 RAIL-B7 -> Deck 8 Service Rail -> RAIL-H9
22:18 Horizon 천장 분기 통과, 감지기 오류와 86kg 이동 기록
22:35 Evelyn이 Richard에게 Daniel의 대기 사실을 거짓 전달
22:45 Horizon Room에서 시신 발견
```

화물 레일과 Horizon 분기는 게임을 위한 가상 설비다. 실제 선박 안전 설비의 정확한 재현으로 홍보하지 않는다.

### 5.4 공간 결정이 필요한 항목

다음 코드는 CSV에 있으나 구조도에서 물리적 위치가 확정되지 않았다.

| 코드 | 처리 방안 |
|---|---|
| BRIDGE | Deck와 이동 연결을 구조도에 추가해야 함 |
| CABIN_DANIEL | 객실 Deck와 복도 연결을 추가해야 함 |
| STERN | 외부 갑판 접근 경로를 추가해야 함 |
| INTERVIEW | 독립 방인지 재사용 UI인지 결정 필요 |
| FORENSIC | Medbay 상태 변형인지 독립 공간인지 결정 필요 |
| EVIDENCE_BOARD | 월드 공간이 아닌 전역 UI로 구현 권장 |

또한 단서 C-09의 “86kg이 7층에서 8층으로 이동”은 최종 목적지 Deck 9 Horizon과 혼동될 수 있다. 구현 데이터는 `Deck 7 출발 -> Deck 8 센서 감지 -> Deck 9 도착`으로 분리한다.

---

## 6. 핵심 단서 데이터베이스

| ID | 단서 | 해석 |
|---|---|---|
| C-01 | Daniel의 초대장 | Richard 전자서명은 진짜지만 발송 서버는 비서실 |
| C-02 | 열린 출입문 | 잠금 트릭이 아니라 출입 흔적 부재가 문제 |
| C-03 | 외벽 발판 | 염분막과 센서 기록이 온전함 |
| C-04 | 덕트 먼지 | 통과 흔적 없음 |
| C-05 | 점검구 먼지 | 균일하게 유지 |
| C-06 | 구두 밑창 | Horizon 카펫이 아니라 Ballast 바닥 고무 |
| C-07 | 혈흔 중심 | 상처 위치와 불일치 |
| C-08 | 화재감지기 오류 | 22:18 천장 레일 통과 |
| C-09 | 안정화 로그 | 약 86kg 이동 |
| C-10 | 운반백 자국 | 어깨·허리 압박 흔적 |
| C-11 | 안정제 | 사망 시각 오판에 기여 |
| C-12 | 질소 로그 | 실제 직접 사인 |
| C-13 | 익명 채팅 | Richard 유죄 가설을 강화하는 선택적 진실 |
| C-14 | 문장 습관 | Evelyn의 반복 표현 |
| C-15 | Marcus 인증 | 금고 접근 지원 |
| C-16 | 보호면 DNA | Daniel과 Evelyn 직접 접촉 |
| C-17 | Orpheus 음성 | Richard의 무지와 Evelyn의 계획 |
| C-18 | 수정 기사 | 피해자의 오판을 사후에 바로잡음 |

각 `EvidenceDefinition`은 다음을 가져야 한다.

- `evidence_id`
- 한국어 이름과 설명 현지화 키
- 획득 씬과 대체 획득 경로
- 표면 정보와 심층 조사 정보
- 관련 장소·시간·인물·기계 태그
- Evidence Board에서 연결 가능한 노드
- Evidence Integrity가 낮을 때의 약화 상태
- 최종 심문에서의 사용 가능 여부

필수 단서가 한 번의 선택 실패로 영구 소실되지 않도록 대체 획득 경로 또는 다음 날 자동 보완을 정의한다.

---

## 7. 퍼즐과 최종 논증

### 7.1 주요 퍼즐

| ID | 퍼즐 | 입력 | 정답·출력 |
|---|---|---|---|
| PZ-EXIT | 흔적 없는 출구 검증 | 발판·덕트·점검구 조사 | 사건 현장에 살아 있는 제3자가 없었다 |
| PZ-BLOOD | 혈흔 배열 | 사진 조각 회전·배치 | 비산혈흔 부재, 중심 불일치, 수직 낙하 |
| PZ-MARCUS | 제한 심문 | 예/아니오 질문 5개 | Evelyn에게 인증 수단 제공 |
| PZ-RAIL | 화물 레일 분기 | 구조도와 이동 로그 | Ballast-Horizon 운반 경로 |
| PZ-TIMELINE | 타임라인 카드 | 사건 카드 12장 | 21:22-22:45 참 타임라인 |
| PZ-FINAL | 최종 논증 | 범인·장소·사인·운반·동기·과거 | 완전 정답과 엔딩 분기 |

### 7.2 최종 정답

1. 범인: Evelyn Shaw
2. 실제 살해 장소: Ballast Control Annex
3. 직접 사인: 질소성 불활성 가스에 의한 질식
4. 시신 운반: 천장 화물 레일
5. 유인 동기·방법: Daniel의 Richard 오해를 이용한 위조 초대와 익명 제보
6. 과거 사건: Orpheus 보험사기 설계자는 Evelyn이며 Richard는 은폐자

최종 심문 UI는 시나리오의 “5단계 지목”과 최종 논증의 6개 항목 사이를 정리해야 한다. 권장안은 5단계 화면에서 5번을 `유인 방법 + 과거 사건`의 2개 하위 선택으로 구성하는 것이다. 확정 전에는 데이터에서 6개 답을 모두 독립 필드로 저장한다.

---

## 8. 엔딩

| ID | 이름 | 조건 |
|---|---|---|
| END-A | Complete Wake | Evelyn 범행과 Richard 은폐를 모두 공개, 핵심 증거 완비 |
| END-B | Convenient Culprit | Evelyn 체포, Richard의 은폐를 덮음 |
| END-C | The Wrong Man | Richard를 범인으로 지목, Evelyn 도주 |
| END-BAD | Panic at Sea | Public Anxiety 100 또는 Evidence Integrity 0 |

엔딩 판정은 단일 `ending_id`를 직접 쓰지 않고 최종 답안, 공개 선택, 불안도, 현장 보존도에서 계산한다. 판정 결과만 저장하여 디버그 화면에서 근거를 확인할 수 있게 한다.

---

## 9. 대사 CSV 분석과 실행 데이터 계약

### 9.1 현재 CSV 현황

| 항목 | 값 |
|---|---:|
| 행 수 | 200 |
| 씬 수 | 41 |
| 화자 값 수 | 19 |
| 기계 ID가 있는 선택지 행 | 30 |
| 선택 그룹 | 15 |
| `voice_required=Y` | 105 |
| `voice_required=N` | 95 |
| 감정 값 | 29종 |
| 순서 중복·누락 | 없음 |

200행은 10-14시간 분량의 완성 대본이 아니라 모든 씬을 잇는 프로토타입 골격이다. 이 데이터를 기준으로 시스템을 먼저 검증하고, 플레이타임 목표는 추가 조사문·심문·반응·퍼즐 설명을 작성한 뒤 다시 산정한다.

### 9.2 원본 열

```text
scene_id
order
speaker
text_ko
emotion
condition
choice_id
next_or_effect
stage_direction
voice_required
```

### 9.3 임포트 시 정규화

원본 CSV를 직접 런타임에서 해석하지 않고 Editor Importer가 검증된 ScriptableObject 또는 JSON으로 변환한다.

| 런타임 필드 | 생성 규칙 |
|---|---|
| `line_id` | `{scene_id}_{order:00}`, 예: `D1-06_03` |
| `scene_id` | 원본 유지 |
| `order` | 정수 |
| `speaker_id` | 화자 사전으로 정규화 |
| `text_key` | `dialogue.{scene_id}.{order:00}` |
| `emotion_id` | 감정 사전의 enum 또는 안정 ID |
| `condition_expr` | 파서가 이해하는 구조화 조건 |
| `choice_id` | 기계 ID 형식인 행만 사용 |
| `effects` | 구조화된 효과 목록 |
| `location_id` | `stage_direction`의 장소 코드 |
| `voice_required` | bool |
| `voice_id` | 녹음 전에는 null 허용 |

### 9.4 반드시 수정해야 하는 CSV 문제

1. 15개 일반 대사 행의 `choice_id`에 `선택 A / 선택 B`라는 표시용 문자열이 들어 있다. 실제 `PLAYER_CHOICE` 행의 `P-01_C1` 같은 ID와 역할이 충돌한다. 표시용 문자열은 `choice_prompt` 열로 옮기거나 비워야 한다.
2. `condition`은 `P-01`, `D8-01 정답`, `없음`처럼 자연어와 ID가 섞여 있다. `required_scene_id`, `required_flag`, `required_outcome`으로 구조화해야 한다.
3. `next_or_effect`는 `Daniel 신뢰도 ±1` 같은 자연어다. `effect_type`, `target_id`, `delta`, `next_scene_id`로 파싱 가능한 값이 필요하다.
4. `ADRIAN`과 `ADRIAN_독백`, `EVELYN`과 `EVELYN_RECORD`를 캐릭터 ID와 표현 모드로 분리해야 한다.
5. `전원`, `생존자`, `CLAIRE(선택)`, `승무원_NPC`의 표기 규칙을 확정해야 한다.
6. 동일한 안내 문장이 여러 씬에서 반복된다. 의도된 공통 시스템 문구라면 현지화 키를 공유하고, 대사라면 씬별 고유 `line_id`를 유지한다.
7. `voice_required=Y`인 105행 모두에 안정적인 `voice_id`와 실제 녹음 범위를 연결해야 한다.

### 9.5 콘텐츠 검증기

Editor 메뉴와 CI에서 다음을 검사한다.

- 모든 행의 `line_id` 유일성
- 씬 안에서 `order`가 1부터 연속인지
- 41개 씬이 시나리오 레지스터에 존재하는지
- 화자·감정·장소·선택·효과 ID가 사전에 존재하는지
- 조건-결과 그래프에 순환 잠금과 도달 불가 씬이 없는지
- P-01부터 D8-03까지 Golden Path가 존재하는지
- C-01-C-18의 획득 경로와 대체 경로가 존재하는지
- 15개 선택 그룹에 정확히 2개의 선택 행이 연결되는지
- `voice_required=Y` 행에 VO 누락이 없는지
- 한국어 텍스트가 설정된 대사창의 최대 3줄을 넘는지

---

## 10. Unity 프로젝트 구조

```text
Assets/
  _Project/
    Art/
      Backgrounds/
      Characters/
      Evidence/
      UI/
    Audio/
      Ambience/
      Music/
      SFX/
      VO/
    Code/
      Core/
      Exploration/
      Narrative/
      Evidence/
      Puzzles/
      Timeline/
      FinalInterrogation/
      Save/
      UI/
      Infrastructure/
      Editor/
    Content/
      Chapters/
      Characters/
      Dialogue/
      Evidence/
      Locations/
      Puzzles/
      Timeline/
      Localization/
    Prefabs/
    Scenes/
      Bootstrap/
      Ship/
      UI/
    Tests/
      EditMode/
      PlayMode/
ArtSource/
  AI_References/
  Figma/
  PSD/
  KRA/
  SVG/
Docs/
  ADR/
  AI/
  Design/
  Playtests/
```

현재 프로젝트에는 `Assets/Docs`, `Assets/Scenes`, `Assets/Settings`만 있으므로 이 구조는 필요한 기능부터 점진적으로 만든다. 빈 폴더를 한 번에 커밋하지 않는다.

권장 어셈블리:

- `Wake.Core`: 안정 ID, 조건, 효과, 이벤트, 시간, 공통 상태
- `Wake.Exploration`: 장소, 핫스폿, 캐릭터 배치
- `Wake.Narrative`: 대화 노드, 선택, Trust, Pressure
- `Wake.Evidence`: 단서 DB, 조사, Evidence Integrity, Evidence Board
- `Wake.Puzzles`: 출구·혈흔·제한 심문·레일 퍼즐
- `Wake.Timeline`: 카드, 제약, 순서 검증
- `Wake.FinalInterrogation`: 최종 답안과 엔딩 판정
- `Wake.Save`: 스냅샷과 마이그레이션
- `Wake.UI`: 화면, 입력, 포커스, 접근성
- `Wake.Infrastructure`: Addressables, 오디오, 현지화, 플랫폼

---

## 11. 씬과 화면 전략

### 11.1 Unity Scene

- `00_Bootstrap`: GameState, SaveService, AudioService, LocalizationService, InputRouter, SceneFlow
- `UI Basic Scene`: 현재 와이어프레임 검증용. 제품 UI 프리팹으로 분리한 뒤 이름 변경
- 선박 장소는 장소마다 거대한 Scene을 만들기보다 배경·핫스폿·캐릭터·조명 상태를 Addressable prefab과 데이터로 조합
- Evidence Board, Map, Evidence, Settings는 전역 UI
- 현장 퍼즐은 공통 Puzzle Host에 퍼즐별 데이터와 프리팹을 주입

### 11.2 기존 UI Basic Scene 매핑

| 현재 오브젝트 | 새 역할 |
|---|---|
| StartScene | 타이틀·새 게임·이어하기 |
| Ingame | 장소 배경, 대화, 핫스폿, 시간 블록 |
| Map | Deck 5-10 지도와 이동 가능 장소 |
| Evidence | C-01-C-18 수첩과 조사 |
| Settings Popup | 오디오·텍스트·접근성 |
| Rooms | 장소 선택 또는 지도 하위 패널 |
| Evidences | Evidence Board의 카드 목록 |
| Turn / Next | 대화 진행과 시간 블록 표시로 재정의 |

와이어프레임에 버튼이 존재한다고 기능이 구현된 것은 아니다. 화면 전환, 저장, 데이터 바인딩은 별도 시스템으로 연결한다.

---

## 12. 저장 상태

`GameState` 최소 필드:

```text
schema_version
current_day
current_time_block
remaining_actions
current_scene_id
current_location_id
completed_scene_ids
global_flags
character_trust
interrogation_pressure
public_anxiety
evidence_integrity
evidence_states
active_theories
dialogue_choices
puzzle_states
timeline_state
final_answers
ending_state
settings
```

자동 저장 시점:

- 장소 진입
- 씬 완료
- 단서 획득·심층 조사
- 선택 확정
- 퍼즐 완료
- 시간 블록 소비
- 심문 단계 종료
- 최종 답안 제출 전

저장 파일에는 Unity 오브젝트 GUID나 직접 참조를 넣지 않고 안정적인 문자열 ID만 기록한다.

---

## 13. 아트와 AI 제작 파이프라인

### 13.1 아트 기준

- 현대적 고급 크루즈선과 15년 전 해양사고 기록의 대비
- 짙은 남색, 차가운 청록, 황동·금색 포인트
- 사실적인 공간 구조 위에 읽기 쉬운 2D 조사 UI
- Horizon Room, Ballast Annex, 화물 레일은 같은 구조적 단서를 공유
- 증거는 장식보다 판독성과 확대 조사 포인트를 우선

### 13.2 AI 활용

AI에 적합:

- 배경 구도와 조명 시안
- 인물 실루엣·복장·표정 보드
- 증거물 콘셉트 변형
- 버튼·패널 장식 시안
- 반복 배경 소품과 텍스처 초안
- 이미지 부분 수정, 배경 확장, 투명 배경 분리

사람이 확정:

- 선박 Deck 연결과 사건 동선
- 반복 등장 인물의 얼굴 일관성
- 단서의 위치·크기·판독 가능성
- 최종 UI 레이아웃과 폰트
- 라이선스와 생성 이력

### 13.3 수정 가능한 원본

```text
ArtSource/AI_References/{asset_id}/
  prompt.md
  generated_original.png
  selected_reference.png
  editing_notes.md
  license.md
```

- 배경·인물은 PSD 또는 KRA 레이어 원본을 보관
- 아이콘·테두리는 Figma 또는 SVG 원본을 보관
- Unity에는 투명 PNG 또는 검증된 Sprite를 넣음
- 버튼·패널은 글자 없는 9-sliced Sprite로 제작
- 텍스트는 이미지에 굽지 않고 TextMeshPro 사용
- AI 생성 한 장을 그대로 최종 UI로 사용하지 않고 배경·전경·장식·버튼·텍스트를 분리

### 13.4 에셋 승인 조건

- 1920x1080과 1280x720에서 핵심 단서가 읽힘
- UI 160% 확대에서 텍스트와 버튼이 잘리지 않음
- 같은 인물의 얼굴·복장·색상표가 씬 간 일치
- 프롬프트, 모델·도구, 생성일, 수정자, 라이선스 기록 존재
- Unity Import 설정과 압축 후 품질 확인
- 원본과 Unity용 출력물이 서로 연결되는 `asset_id` 보유

---

## 14. 오디오와 VO

현재 CSV에서 VO 대상은 105행이다. 이는 최종 녹음 확정량이 아니라 초안 표시다.

- 우선 녹음: Daniel의 경고, 발견, Marcus 제한 심문, Evelyn 거래·최종 대치, Julian 기록
- `NARRATION`, `SYSTEM`, `PLAYER_CHOICE`는 기본적으로 VO 제외
- `ADRIAN_독백`은 플레이어 캐릭터 음성 정책을 확정한 뒤 포함
- `voice_id` 규칙: `vo.ko.{scene_id}.{order:00}`
- 음성 파일명과 현지화 키를 동일 ID로 연결
- 배경음: 항구, 엔진 저주파, 볼룸, 의무실, 서비스 레일, Ballast Annex, 바람 부는 선미
- 접근성: 모든 음성 자막, 화자명, 중요 환경음의 텍스트 표시

---

## 15. 3인 역할

| 역할 | 최종 책임 |
|---|---|
| A - Systems & Build | 상태 머신, 데이터 임포터, 장소·시간 흐름, 저장, 퍼즐 런타임, 빌드, 자동 테스트 |
| B - Narrative & Puzzle | 시나리오 추적, CSV 정규화, 대사·조건·효과, 단서 공정성, 퍼즐 정답, 플레이테스트 |
| C - Art, UI & Experience | 배경·캐릭터·증거·UI, AI 에셋 원본 관리, 애니메이션, 오디오 통합, 접근성 |

고위험 기능은 구현자 외 한 명이 반드시 검증한다.

| 영역 | 구현 | 필수 검수 |
|---|---|---|
| Save·Build·Core | A | B |
| 대사 임포트·조건 | B/A | A/B 상호 |
| Evidence Board | A/C | B |
| 퍼즐 정답·힌트 | B/A | C |
| UI·아트·접근성 | C | A/B |
| 엔딩 판정 | A/B | 전원 |

---

## 16. 구현 순서

### Phase 0 - 기준선 정리

- 기준 PDF·CSV를 저장소 내 관리 위치로 복사하고 Git LFS 여부 결정
- 장소 코드 사전과 화자 사전 확정
- CSV의 선택·조건·효과 열 정규화
- `Wake.*` 네임스페이스와 폴더 생성
- UI Basic Scene의 화면 오브젝트를 프리팹으로 분리

완료 조건: CSV 200행이 오류 없이 임포트되고 P-01부터 D8-03까지 그래프가 연결된다.

### Phase 1 - Vertical Slice

권장 범위는 D1-06 발견과 D2-01 출구 검증이다.

- HORIZON 한 장소
- 핫스폿 조사
- 대화 재생과 2지 선택
- C-02-C-05 획득
- Evidence 화면
- PZ-EXIT
- 자동 저장·로드
- 임시 배경·인물·사운드

완료 조건: 처음 보는 플레이어가 “문이 잠긴 것이 아니라 출입 흔적이 없다는 사건”을 설명할 수 있다.

### Phase 2 - 프롤로그와 Day 1

- PORT, GANGWAY, DECK10_SUITE, ATRIUM, DINING, BALLROOM, SERVICE7, HORIZON, MEDBAY
- 9명 소개와 기본 Trust
- 시신 발견의 현장 보존 선택
- 비밀 수사 계약

### Phase 3 - Day 2-3

- 출구·혈흔·사망 시각·감지기 로그
- Daniel 객실과 기사 공개
- Richard의 은폐 자백
- 금고와 Marcus 인증
- Public Anxiety 첫 실제 분기

### Phase 4 - Day 4-5

- Marcus 계단 사고와 제한 심문
- Claire 자작극과 자동장치 학습
- 태블릿 회수
- Evidence Board 가설 해금

### Phase 5 - Day 6-8

- 안정화 로그·화물 레일·Ballast 조사
- 타임라인 카드
- Orpheus 음성 복원
- 최종 심문
- A/B/C/Bad End

### Phase 6 - 콘텐츠 확장과 출시 준비

- 10-14시간 목표에 맞춘 대사·조사·선택 콘텐츠 확장
- KO 전체 교정, VO 범위 잠금
- 아트·사운드 최종화
- 접근성·성능·저장 마이그레이션
- 전체 Golden Path와 엔딩 회귀 테스트

---

## 17. 테스트 계획

### 17.1 EditMode

- CSV 스키마, 행 순서, ID 유일성
- 41개 씬과 선행 조건 그래프
- C-01-C-18 획득·약화·최종 사용 상태
- Trust, Anxiety, Integrity 경계값
- Theory Slot 최대 3개
- Time Block 행동 차감과 대체 단서
- 퍼즐 정답과 힌트 단계
- 최종 6개 답과 엔딩 판정
- 저장 스키마 마이그레이션

### 17.2 PlayMode

- P-01부터 D8-03까지 Golden Path
- 각 Day 시작 체크포인트에서 진행 가능
- 잘못된 선택과 놓친 단서 이후에도 엔딩 도달 가능
- Anxiety 100과 Integrity 0 Bad End
- HORIZON과 BALLAST 사이 시신 경로 이해
- 저장·로드 후 대화·퍼즐·시간 상태 복원
- 키보드·마우스와 게임패드 포커스
- 1280x720, 1920x1080, UI 100-160%

### 17.3 미스터리 공정성 플레이테스트

- Day 2 종료 시 “출구가 사용되지 않았다”를 이해하는가?
- Day 3 종료 시 Richard가 은폐자이지만 살인범으로 확정되지는 않는가?
- Day 5 종료 시 자동장치가 현장을 만들 수 있음을 학습하는가?
- Day 6 종료 시 Ballast와 화물 레일을 스스로 연결하는가?
- Evelyn을 C-14 하나만으로 조기 확정하지 않는가?
- 최종 정답을 맞힌 근거가 우연이 아니라 독립 단서 두 종류 이상인가?

---

## 18. 위험과 결정 필요 항목

| ID | 위험·결정 | 대응 |
|---|---|---|
| DEC-01 | 10-14시간 목표와 200행 대사 골격의 격차 | Slice 이후 실제 플레이타임을 측정하고 씬별 콘텐츠 예산 확정 |
| DEC-02 | 구조도에 BRIDGE·CABIN_DANIEL·STERN 위치 없음 | 환경 제작 전에 Deck와 이동 연결 승인 |
| DEC-03 | INTERVIEW·FORENSIC가 실제 장소인지 UI인지 불명 | 재사용 화면 또는 Medbay 상태 변형으로 결정 |
| DEC-04 | 최종 심문 5단계와 최종 논증 6항목 차이 | 5단계 + 하위 2선택 권장안 검증 |
| DEC-05 | C-09의 Deck 7->8 표현과 Horizon Deck 9 목적지 | 센서 감지와 최종 도착을 별도 이벤트로 데이터화 |
| DEC-06 | 선택 실패로 필수 단서 영구 소실 위험 | 대체 획득 또는 다음 날 보완 경로 의무화 |
| DEC-07 | `choice_id`, `condition`, `next_or_effect`가 자연어 혼합 | CSV v2 스키마 또는 임포트 매핑표 작성 |
| DEC-08 | VO 105행의 비용 | 우선 녹음 세트로 Slice 검증 후 범위 잠금 |
| DEC-09 | AI 아트의 일관성과 권리 | 원본·프롬프트·수정·라이선스 manifest 필수 |
| DEC-10 | Unity Scene 동시 수정 충돌 | 화면·장소 프리팹 분리, 주간 파일 소유자 지정 |
| DEC-11 | OneDrive 동기 충돌 | 가능하면 `C:\Dev\project-mystery` 같은 비동기 경로 사용 |
| DEC-12 | 프로젝트와 문서에 이전 이름 잔존 | 기능 안정 후 별도 rename PR |

OneDrive 아래에서 계속 개발할 경우 Unity 종료 후 `git pull`하고, `Library`, `Temp`, `Obj`, `Build`, `Assets/_Recovery`는 버전 관리하지 않는다.

---

## 19. Definition of Ready

- 연결된 씬 ID와 기준 자료 위치
- 정상 경로와 실패·대체 경로
- 입력·출력 단서 및 상태 변화
- 저장해야 할 필드
- 키보드·게임패드·접근성 요구
- 사용할 임시 또는 최종 에셋 ID
- 테스트 가능한 완료 조건

## 20. Definition of Done

- 기준 자료의 사건 진상과 충돌하지 않음
- 콘텐츠 검증기 통과
- EditMode 또는 PlayMode 테스트 존재
- 저장·로드 후 동일 상태 복원
- 키보드·마우스와 게임패드로 완료 가능
- 자막·포커스·색상 외 표현 등 접근성 반영
- AI 에셋은 원본·생성 이력·수정 파일·라이선스 기록
- Unity Console 컴파일 오류 0
- 다른 팀원이 PR에서 재현하고 승인

---

## 21. 바로 실행할 작업

1. 기준 PDF·CSV를 저장소의 `Docs/Source` 또는 팀이 합의한 경로로 이동한다.
2. 장소 코드와 화자 ID의 미확정 항목을 승인한다.
3. CSV v2 스키마를 만들고 200행을 자동 변환한다.
4. `UI Basic Scene`에서 StartScene, Ingame, Map, Evidence, Settings를 프리팹으로 분리한다.
5. D1-06 + D2-01 HORIZON Vertical Slice를 구현한다.
6. C-02-C-05와 PZ-EXIT를 연결한다.
7. 저장·로드와 콘텐츠 검증기를 붙인다.
8. 임시 아트로 첫 외부 플레이테스트를 진행한다.
