# 지도 개편 2단계 구현 기록

## 적용 범위

2단계는 지도 이미지의 레이어 분리와 방 단위 상호작용 기하를
런타임에 연결하는 단계다. Deck 6의 비사용 장소 6개는 이 데이터에
포함하지 않는다.

- 활성 장소: 24개
- 지도: Port, Deck 7, Deck 8, Deck 9, Deck 10
- 노드 에셋: 지도별 1개
- 방 마스크 에셋: 지도별 1개
- 좌표계: Unity UI와 같은 좌하단 원점의 0~1 정규화 좌표

## 런타임 레이어 순서

지도 화면은 다음 순서로 합성한다.

1. Passenger Base
2. Passenger Spoiler Redactions
3. 동적 제한구역과 Investigation 주석
4. Technical Overlay
5. Room Hit Area
6. 장소 이름 노드

Passenger 차폐는 불투명하다. Base 이미지에 이미 그려진 내부 코드나
서비스 구조가 반투명 레이어 아래로 비치는 문제를 막는다.

## 상호작용 에셋

다음 폴더의 에셋은 `Resources.LoadAll`로 로드한다.

```text
Assets/_Project/Resources/Maps/MapNodes/
├─ Port_MapNodes.asset
├─ Deck07_MapNodes.asset
├─ Deck08_MapNodes.asset
├─ Deck09_MapNodes.asset
└─ Deck10_MapNodes.asset

Assets/_Project/Resources/Maps/RoomMasks/
├─ Port_RoomMasks.asset
├─ Deck07_RoomMasks.asset
├─ Deck08_RoomMasks.asset
├─ Deck09_RoomMasks.asset
└─ Deck10_RoomMasks.asset
```

`MapNodesAsset`은 장소 코드, 노드 위치, 향후 사용할 진입점 ID만
저장한다. 표시 이름, Deck, 설명, 이동 등급은 기존
`CanonicalLocationCatalog`와 `MapDeckCatalog`를 원본으로 유지한다.

`RoomMasksAsset`은 각 장소의 불규칙한 클릭 polygon을 저장한다.
런타임은 polygon 내부에서만 클릭을 허용한다. 사각 경계 안이더라도
실제 polygon 바깥이면 선택되지 않는다.

## Deck별 장소 수

| 지도 | 장소 수 | 장소 |
| --- | ---: | --- |
| Port | 2 | `PORT`, `GANGWAY` |
| Deck 7 | 6 | `CABIN_DANIEL`, `SERVICE7`, `ENGINE_CONTROL`, `BALLAST_CONTROL_ANNEX`, `CREW_STAIRS`, `SERVICE_RAIL` |
| Deck 8 | 5 | `ATRIUM`, `NEWS_LOUNGE`, `SECURITY`, `MEDBAY`, `CABIN_CLAIRE` |
| Deck 9 | 4 | `BALLROOM`, `DINING`, `PROMENADE`, `HORIZON` |
| Deck 10 | 7 | `RICHARD_SUITE`, `VIP_LOUNGE`, `BRIDGE`, `VAULT`, `ARCHIVE`, `INTERVIEW`, `OPEN_DECK` |

## Passenger 차폐 규칙

항상 가리는 정보:

- 이미지 내부 개발 코드
- Deck 8에 잘못 표기된 `PROMENADE`
- Deck 7의 기술 구역 성격을 직접 밝히는 제목

스토리 진행 뒤에만 해제하는 정보:

- `CABIN_CLAIRE`: `D5-01` 완료
- `BRIDGE`: `D3-03` 완료
- `INTERVIEW`: `D5-03` 완료
- `ARCHIVE`: `D7-03` 완료
- 직원·주방 구역: `D1-04` 완료 후 Investigation에서 표시
- 서비스·기관·하부 구조: `D6-02` 완료 후 Technical에서 표시

`D6-02`가 단순히 unlock된 상태로는 Technical 탭이 열리지 않는다.
완료 기록이 있어야 열린다.

## 추가 아트 권장 장소

다음 4개 장소는 현재 Base 이미지에 독립 공간이 없거나 다른 장소의
시각 의미와 겹친다. 클릭 polygon은 겹치지 않도록 임시 구획에
연결했지만, 최종 아트 단계에서 별도 공간 표현을 추가하는 것이 좋다.

| 장소 | 현재 해석 |
| --- | --- |
| `SECURITY` | 의무실과 클레어 객실 사이의 제어 구획 |
| `PROMENADE` | Deck 9 하단 외곽 산책 통로 |
| `VIP_LOUNGE` | Richard Suite의 Sitting Lounge 구획 |
| `OPEN_DECK` | Deck 10 상단 외곽 갑판 통로 |

해당 4개 마스크에는 `CorrectiveArtworkRecommended`가 저장되어 있어
테스트와 후속 제작 도구가 자동으로 추적할 수 있다.

## 생성과 검증

에셋을 다시 생성할 때는 Unity 메뉴의
`Wake > Map > Bake Interaction Geometry`를 사용한다. 기존 에셋을
삭제하지 않고 내용을 갱신하므로 GUID가 유지된다.

자동 검증 항목:

- 활성 24개 장소와 노드·마스크 집합의 정확한 일치
- Deck 6 에셋 부재
- 지도별 에셋 1개
- 모든 좌표의 0~1 범위
- 최소 polygon 면적과 자기 교차 금지
- 노드가 자기 방 polygon 안에 존재
- 같은 Deck의 방 내부 중첩 금지
- Base Sprite와 저작 해시 일치
- Day 1 차폐 규칙 존재
- `D6-02` 완료 전 Technical 잠금
- 실제 PlayMode에서 방 polygon 생성과 Technical 전환
