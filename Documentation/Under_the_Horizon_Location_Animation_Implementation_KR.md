# Under the Horizon 장소·캐릭터 애니메이션 구현 명세

## 1. 구현 범위

현재 게임에서 사용하는 장소 배경 아트 19종에 장소별 환경 애니메이션을 적용했다.
스토리에서 사용하는 논리 장소 코드는 23개지만, 다음 네 쌍은 같은 배경 아트를 공유하므로
동일한 애니메이션 마스크와 프로필을 사용한다.

| 논리 장소 | 공용 시각 프로필 |
|---|---|
| `SECURITY`, `INTERVIEW` | `SECURITY_INTERVIEW` |
| `NEWS_LOUNGE`, `CABIN_DANIEL` | `NEWS_DANIEL` |
| `ENGINE_CONTROL`, `BRIDGE` | `ENGINE_BRIDGE` |
| `SERVICE7`, `CREW_STAIRS` | `SERVICE_STAIRS` |

구현 목표는 배경 원화를 교체하거나 스켈레탈 애니메이션을 추가하는 것이 아니라,
기존 정지 이미지 위에 저강도 환경 효과를 합성하고 캐릭터 UI에 미세한 생체 움직임을
부여하는 것이다. 모든 효과는 클릭 판정, 대화 진행, 맵 이동, 저장 데이터에 영향을 주지
않는다.

## 2. 19개 장소별 연출

| 프로필 | 연결 장소 | 적용 효과 | 연출 의도 |
|---|---|---|---|
| `PORT` | `PORT` | 석양 광원 맥동, 수면 빛 스윕, 부유 입자, 전체 드리프트 | 바람과 물결이 이어지는 출항·귀항 항구 |
| `GANGWAY` | `GANGWAY` | 외광 맥동, 바닥 반사 스윕, 부유 입자 | 긴 승선 통로의 원근감과 외부광 유입 |
| `RICHARD_SUITE` | `RICHARD_SUITE` | 창가·스탠드 조명 맥동, 먼지, 간헐 깜박임 | 고급스럽지만 정적이고 불안한 객실 |
| `ATRIUM` | `ATRIUM` | 중앙 조명 맥동, 바닥 광택 스윕, 먼지, 전체 드리프트 | 넓은 중앙 홀에 잔잔한 호흡감 부여 |
| `DINING` | `DINING` | 천장·촛불 맥동, 촛불 깜박임, 먼지 | 따뜻하고 친밀한 만찬 공간 |
| `BALLROOM` | `BALLROOM` | 샹들리에 맥동, 바닥 반사 스윕, 먼지, 전체 드리프트 | 화려한 광택과 선체의 미세한 움직임 |
| `SERVICE_STAIRS` | `SERVICE7`, `CREW_STAIRS` | 작업등 맥동, 증기, 조명 깜박임, 미세 진동 | 좁은 서비스 계단의 기계적 압박감 |
| `HORIZON` | `HORIZON` | 석양 맥동, 바다 반사 스윕, 먼지, 전체 드리프트 | 평온한 전망 속 사건 현장의 불길함 |
| `MEDBAY` | `MEDBAY` | 의료 화면 맥동, 천장 조명, 진단 스캔, 먼지 | 차갑고 정밀한 의료 장비의 작동감 |
| `SECURITY_INTERVIEW` | `SECURITY`, `INTERVIEW` | 모니터 맥동, 하향 스캔, 화면 깜박임, 상부 조명 | 감시·심문 공간의 데이터 처리와 압박감 |
| `NEWS_DANIEL` | `NEWS_LOUNGE`, `CABIN_DANIEL` | 다중 화면 맥동, 화면 스캔, 조명 맥동, 먼지 | 정보가 계속 갱신되는 작업 공간 |
| `ENGINE_BRIDGE` | `ENGINE_CONTROL`, `BRIDGE` | 계기 화면, 녹색 기계광, 증기, 스파크, 진동 | 핵심 제어 구역의 중장비 가동과 긴박감 |
| `VAULT` | `VAULT` | 보안 화면, 접근 표시등, 탐색 스윕, 화면 깜박임 | 통제된 출입과 지속적인 보안 검색 |
| `PROMENADE` | `PROMENADE` | 석양 맥동, 바다 반사 스윕, 바람 입자, 전체 드리프트 | 개방된 산책 갑판의 해풍과 수평선 |
| `CABIN_CLAIRE` | `CABIN_CLAIRE` | 실내등·램프 맥동, 먼지, 간헐 깜박임 | 친밀하지만 이면이 있는 고급 객실 |
| `SERVICE_RAIL` | `SERVICE_RAIL` | 작업등 맥동, 바닥 스윕, 증기, 조명 깜박임 | 긴 산업 통로의 방향성과 설비 가동감 |
| `BALLAST_CONTROL_ANNEX` | `BALLAST_CONTROL_ANNEX` | 제어 화면, 고압 증기, 스파크, 진동 | 압력 설비가 불안정한 위험 구역 |
| `ARCHIVE` | `ARCHIVE` | 콘솔 화면, 데이터 스캔, 램프 맥동, 먼지 | 오래된 기록과 복구 장비의 정적인 작동감 |
| `OPEN_DECK` | `OPEN_DECK` | 태양 맥동, 수면 스윕, 바람 입자, 강한 전체 드리프트 | 외풍에 노출된 갑판과 최종 대치의 운동감 |

프로필의 실제 좌표, 색, 강도, 주기, 방향, 시드는
`Assets/_Project/Code/Exploration/LocationBackgroundAnimationCatalog.cs`에서 관리한다.

## 3. 배경 효과 프리미티브

배경 애니메이션은 다음 9개 프리미티브를 조합한다.

| 효과 타입 | 용도 | 주요 튜닝 값 |
|---|---|---|
| `RadialLightPulse` | 태양, 램프, 샹들리에, 원형 계기광 | 영역, 색, 강도, 주기 |
| `RectangularScreenPulse` | 모니터·콘솔의 저강도 발광 | 영역, 색, 강도, 주기 |
| `LinearSweep` | 수면 반사, 바닥 광택, 스캔 라인 | 영역, 방향, 이동 거리, 주기 |
| `DriftingMotes` | 먼지·바람 입자의 장소별 색상 지정 | 색, 밀도 힌트, 이동 방향 |
| `DriftingSteam` | 배관·기계 구역의 상승 증기 | 영역, 개수, 방향, 이동 거리 |
| `OccasionalFlicker` | 조명·화면의 비주기적 깜박임 | 평균 빈도, 지속 시간, 시드 |
| `OccasionalSpark` | 기관실·밸러스트 구역의 간헐 스파크 | 영역, 개수, 빈도, 방향 |
| `FullBackgroundDrift` | 선체·바람에 의한 저속 이동 | 방향, 거리, 회전, 주기 |
| `FullBackgroundShake` | 기계 진동·압력 불안정 | 거리, 회전, 사건 빈도 |

효과 계산은 고정 시드 기반이며 `UnityEngine.Random`을 사용하지 않는다. 같은 장소를 다시
열면 재현 가능한 위상으로 시작하고, 같은 배경을 공유하는 논리 장소 사이에서는 재구축 없이
위상을 유지한다. 전체 배경 이동은 이동량보다 큰 오버스캔을 자동 적용해 화면 가장자리가
노출되지 않도록 한다.

`DriftingMotes`는 장소 프로필에서 색상과 분위기만 결정한다. 실제 입자는 기존
`AmbientRoomParticleOverlay`의 16개 블룸 입자를 재사용해 중복 생성과 과도한 밝기를
방지한다. 블룸은 512×512 오프스크린 렌더 텍스처 한 장으로 합성한다.

## 4. 캐릭터 공통 아이들 모션

모든 월드 캐릭터 `RawImage`에 `UiCharacterIdleMotion`을 런타임으로 추가한다.

| 움직임 | 기본값 | 목적 |
|---|---:|---|
| 상하 호흡 | 1.5 px | 정지 컷의 생체감 |
| 호흡 스케일 | Y축 ±0.6% | 흉곽 움직임을 과장 없이 표현 |
| 호흡 주기 | 3.6초 | 대화 장면을 방해하지 않는 저속 리듬 |
| 좌우 흔들림 | ±0.65° | 완전한 정지 인상을 제거 |
| 흔들림 주기 | 4.8초 | 호흡과 다른 위상으로 반복감 완화 |
| 시작 블렌드 | 0.35초 | 장소 전환 직후 튀는 현상 방지 |
장소 코드, 화자, 인스턴스 ID로 안정적인 시드를 만들어 같은 프레임에 모든 캐릭터가 함께
움직이지 않도록 했다. 움직임은 원래 `RectTransform`과 그래픽 색상을 기준으로 가산 적용되며,
장소 변경·비활성화·모션 감소 시 원본 포즈를 정확히 복원한다. 버튼과
`AlphaContourRaycastFilter`는 기존 그래픽을 그대로 사용하므로 클릭 영역도 캐릭터와 함께
이동한다.

## 5. 런타임 계층과 합성 순서

```text
LocationBackgroundCanvas (sortingOrder -100)
└─ LocationBackground
   └─ Viewport
      └─ Background Motion Root
         └─ Content
            ├─ Location Background Animation
            ├─ Cover Image
            ├─ Evidence / Inspectable hotspots
            ├─ Ambient Characters
            └─ Ambient Particle Composite
```

- `Background Motion Root`만 전체 드리프트·진동을 받는다.
- 장소별 광원·스캔 효과는 `Content`의 첫 번째 형제로 유지된다.
- 모든 배경 효과의 `raycastTarget`은 `false`다.
- 캐릭터와 조사 핫스폿은 배경 크롭·포커스·줌과 같은 좌표계를 사용한다.
- 장소가 바뀔 때만 배경 프로필을 재구축한다. 같은 장소의 대화 상태 갱신은 상호작용
  오버레이만 갱신하므로 애니메이션 위상이 끊기지 않는다.

## 6. 일시정지와 접근성

다음 경우 모든 장소·캐릭터 모션을 정지한다.

- 일시정지 화면
- 확인 모달과 챕터 전환 등 시스템 화면
- 인게임 설정 팝업
- `ReducedMotionSettings.Enabled`가 활성화된 경우

일시정지는 재생 시간을 고정해 닫은 뒤 같은 위상에서 재개한다. 블룸 카메라·볼륨·입자
캔버스는 일시정지 중 비활성화하되 마지막 합성 텍스처는 남겨 화면이 갑자기 사라지지 않게
한다. 설정 팝업과 시스템 화면은 별도 정지 사유로 합성하므로, 일시정지 화면 위에서 설정을
열었다 닫아도 배경이 먼저 재생되지 않는다.

모션 감소에서는 재생 시간을 진행하지 않고 전체 배경을 원래 위치·회전·스케일로 복원한다.
장소를 바꾸더라도 이 정책을 다시 적용한다. 캐릭터 모션과 블룸 입자는 숨기거나 원본 상태로
복원한다.

## 7. 튜닝 규칙

1. 배경의 주 피사체와 캐릭터 얼굴 위에는 강한 광원 마스크를 두지 않는다.
2. 실내 광원 맥동은 원본 명암을 보조하는 수준으로 유지하고, 색상 변화보다 알파 변화로
   조절한다.
3. 반복 효과 주기는 서로 소수가 되도록 벌려 동시 반복을 피한다.
4. `OccasionalFlicker`와 `OccasionalSpark`는 고정 간격이 아니라 시드 기반 사건 창을 쓴다.
5. 전체 이동 거리 증가 시 오버스캔 테스트를 반드시 통과시킨다.
6. 입자 수를 장소 프로필에서 직접 늘리지 않는다. 공용 블룸 입자 16개의 색상만 조절한다.
7. 캐릭터 아이들 모션은 대사 가독성과 클릭 정확도를 우선하며 기본 상한
   8 px, 2°, 스케일 2%를 넘기지 않는다.
8. 새 장소는 카탈로그 프로필과 논리 장소 바인딩을 함께 추가하고, 배경 파일명이 실제
   `LocationDefinition`의 스프라이트와 일치하는지 테스트한다.

## 8. 검증 기준

자동 검증 범위:

- 19개 프로필과 23개 논리 장소 바인딩 완전성
- 공용 배경 네 쌍의 동일 프로필 연결
- 모든 효과의 결정성, 유효 좌표, 알파·스케일 범위
- 전체 배경 이동의 오버스캔 여유
- 효과 레이어의 비상호작용성과 형제 순서
- 캐릭터 원본 포즈·색 복원과 안정적 시드
- 장소 재진입 시 위상 유지
- 일시정지·확인·설정·모션 감소 수명주기
- 씬 종료 시 블룸 보조 오브젝트 안전 정리

최종 구현 시점 검증 결과:

- Runtime, EditModeTests, PlayModeTests 프로젝트 빌드: 경고 0, 오류 0
- 관련 EditMode 테스트: 41/41 통과
- 프로덕션 씬 및 시스템 화면 PlayMode 테스트: 13/13 통과
- Game View 표본 확인: `PORT`, `SECURITY`, `ENGINE_CONTROL`, `PROMENADE`

## 9. 주요 구현 파일

- `Assets/_Project/Code/Exploration/LocationBackgroundAnimationCatalog.cs`
- `Assets/_Project/Code/Exploration/LocationBackgroundAnimationOverlay.cs`
- `Assets/_Project/Code/Exploration/UiCharacterIdleMotion.cs`
- `Assets/_Project/Code/Exploration/LocationLoader.cs`
- `Assets/_Project/Code/Exploration/BackgroundCoverLayout.cs`
- `Assets/_Project/Code/Exploration/AmbientCharacterHotspotOverlay.cs`
- `Assets/_Project/Code/Exploration/AmbientRoomParticleOverlay.cs`
- `Assets/_Project/Code/UI/UIManager.cs`

장소별 연출 조정은 카탈로그 값만 수정하는 것을 원칙으로 한다. 렌더링 계층, 공통 모션
계산기, UI 수명주기는 새로운 효과 타입이나 접근성 정책이 추가될 때만 변경한다.
