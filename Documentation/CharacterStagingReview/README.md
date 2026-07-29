# 배경 의미 지도·캐릭터 배치 승인본

상태: `Approved`
런타임 연결: `true`
승인자: `project-owner` / 리비전: `1`

이 폴더는 배경과 스토리 데이터를 합쳐 만든 승인된 의미 지도와
장면별 캐릭터 배치 기준입니다. 승인본은
`ApprovedBackgroundSemanticCatalog`로 베이크되어 `LocationLoader`와
`AmbientCharacterHotspotOverlay`의 런타임 배치에 연결됩니다.

## 분석 범위

| 항목 | 결과 |
|---|---:|
| 플레이 가능 장소 코드 | 24 |
| 승인 배경 variant | 26 |
| 플레이 가능 장소의 기존 공용 배경 | 19 |
| 분석한 고유 배경 | 45 |
| 장면별 출연진 검수 이미지 | 41 |
| 추천 후보 슬롯 | 202 |
| 이미지별 보호영역 합계 | 150 |
| 자동 검증 오류 | 0 |
| 사용자가 승인한 경고 | 40 |

명시적으로 제외한 비사용 장소는 `LAUNDRY`, `SERVICE_HUB`,
`STABILIZERS`, `BALLAST_TANKS`, `GENERATOR`, `WORKSHOP`입니다.

## 색상과 표기

- 초록색: 캐릭터의 발이 놓일 수 있는 바닥·통로
- 빨간색 영역: 바다, 벽, 책상, 침대, 기계 등 배치 금지영역
- 노란색: 메인 단서, 보조 단서, 맥거핀, 출입구 및 스토리 랜드마크
- 파란색 실루엣: 추천 후보 슬롯과 예상 인물 크기
- 금색 실루엣: 장면의 포커스 인물
- 청록색 실루엣: 환경·맥락 NPC
- 빨간색 실루엣과 `!`: 현재 슬롯에서 중요 단서를 가릴 가능성이 있는
  배치
- 자홍색 빗금: 분석 신뢰도가 낮거나 바닥 경계가 불명확한 영역
- 우상단 화살표와 Kelvin: 주광 방향과 추정 색온도
- 슬롯 라벨의 `#RRGGBB`, `S`, `E`: 추천 tint, saturation, exposure

배경 의미 지도는 원본 전체가 보이도록 비율을 유지한 `Fit` 화면입니다.
장면별 이미지는 실제 게임과 같은 `Cover + BackgroundFocus +
BackgroundZoom` 계산을 적용했습니다. 따라서 장면 이미지에서 잘린
실루엣은 실제 화면에서도 잘릴 가능성이 있습니다.

## 먼저 볼 파일

전체를 빠르게 훑으려면 다음 접촉 시트를 먼저 확인합니다.

- `ContactSheets/background_semantics_01.png` ~
  `background_semantics_05.png`
- `ContactSheets/scene_casts_01.png` ~ `scene_casts_05.png`

세부 좌표와 라벨은 축소된 접촉 시트가 아니라 아래 원본 검수 이미지에서
확인합니다.

- 배경별 전체 의미 지도: `Backgrounds/`
- 날짜·장면별 출연진 배치: `Scenes/`

## 승인된 예외와 자동 검증 항목

검증 오류는 0개입니다. 사용자가 함께 승인한 경고 40개는 추적할 수
있도록 이미지와 런타임 승인 메타데이터에 그대로 남겼습니다.

- 중요 단서 가림 가능성 34건: 장면 이미지에서 빨간 실루엣과 상단
  `RED=HARD CLUE OVERLAP`으로 표시됩니다. 런타임은 이 표시가 승인된
  해당 장면·캐릭터 배치에만 보호영역 중첩 예외를 허용합니다.
- 안전 슬롯 부족으로 화면 밖 처리 4명:
  - `D1-02`: `DINING_SOMMELIER`
  - `D3-04`: `MARCUS`, `VAULT_GUARD`
  - `D7-03`: `ARCHIVIST`
- 인게임 Cover/Zoom에 의해 제외된 슬롯:
  - `P-01`: 1개
  - `D3-04`: 2개
- 다중 보행 섬을 사용하는 배경:
  - `bg_crew_stairs_d4_reconstruction`
  - `bg_crew_stairs_d4_wet`
  - `bg_crew_stairs_default`
  - `bg_service_rail_d6_subtle`
- 분석 신뢰도 0.75 미만:
  - `bg_horizon_d8_finale` — 0.68
  - `bg_vault_d7_damaged` — 0.72
  - `bg_location_d7_4_crew_stairs` — 0.70

추가로 `HORIZON D8`은 카탈로그에 남아 있는 일부 단서 좌표와 실제
이미지의 시각 앵커가 일치하지 않습니다. 기존 ballast-control
배경의 `C-08`도 시각 앵커와 카탈로그 좌표가 어긋날 가능성을
`analysisNotes`에 기록했습니다.

## 데이터 파일

- `Data/background_analysis_inventory.json`
  - 최신 프로젝트에서 수집한 장소, 배경 hash, variant, 증거·조사물,
    장면별 출연진, Cover/Focus/Zoom
- `Data/background_semantic_profiles.json`
  - 45개 배경의 최종 분석 결과, 보호영역, 후보 슬롯, 조명·색상 보정
- `Data/analysis_validation_report.json`
  - 집계, 제외 장소, 오류와 검수 경고
- `Tools/BackgroundSemanticAnalysis/semantic_geometry_head.json`
  - 승인 variant 1차 묶음의 시각 분석 seed
- `Tools/BackgroundSemanticAnalysis/semantic_geometry_tail.json`
  - 승인 variant 2차 묶음의 시각 분석 seed
- `Tools/BackgroundSemanticAnalysis/semantic_geometry_base.json`
  - 플레이 가능 장소의 기존 공용 배경 분석 seed

분석 seed는 작성 편의를 위해 `depth=0`을 전경, `depth=1`을 후경으로
사용합니다. 최종 `background_semantic_profiles.json`의 `depth01`은
런타임 기반 모델과 맞춰 반대로 변환되며, `0=후경`, `1=전경`입니다.

## 재생성

Unity 메뉴에서 먼저 아래 항목을 실행해 최신 스토리·증거·배경
inventory를 만듭니다.

```text
Wake/Analysis/Export Background Semantic Inventory
```

이후 저장소 루트의 Windows PowerShell에서 실행합니다.

```powershell
powershell.exe -NoProfile -ExecutionPolicy Bypass `
  -File "Tools/BackgroundSemanticAnalysis/Generate-BackgroundSemanticReview.ps1" `
  -ApproveForRuntime `
  -ApprovedBy "project-owner" `
  -ApprovalRevision 1
```

배경 이미지 hash가 바뀌면 inventory를 다시 내보내고 검수 이미지를
재생성·재승인·재베이크해야 합니다. 빌드 전 검증기는 원본 배경 hash,
장면별 출연진 fingerprint, 의미 데이터 hash, 41개 검수 이미지
baseline hash 중 하나라도 달라지면 빌드를 중단합니다.
