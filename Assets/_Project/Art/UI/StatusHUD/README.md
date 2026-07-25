# Status HUD Sprite Library

게임 상태 지표를 표현하기 위한 투명 PNG 스프라이트 세트입니다. 전역 상태, 인물별 신뢰도, 가설 슬롯, 시간대 및 조사 권한을 기능 단위로 분류합니다.

## 폴더 구성

| 폴더 | 수량 | 용도 |
| --- | ---: | --- |
| `Global` | 10 | Public Anxiety, Evidence Integrity, 공통 HUD와 상태 변화 토스트 |
| `Trust` | 5 | 인물별 Trust 0~5 표시와 증감 피드백 |
| `Theory` | 5 | Theory Slots 3개와 활성·가득 참·시간 비용 상태 |
| `Time` | 6 | AM/PM/NIGHT 다이얼과 행동 토큰 |
| `Flags` | 4 | 비서실 권한, 천장 조사, 화물 레일 조사 태그 |

## Unity 임포트 설정

모든 PNG는 다음 설정으로 임포트합니다.

- Texture Type: `Sprite (2D and UI)`
- Sprite Mode: `Single`
- Mesh Type: `Full Rect`
- Pixels Per Unit: `100`
- Alpha Source: `Input Texture Alpha`
- Alpha Is Transparency: 활성화
- Generate Mip Maps: 비활성화
- Filter Mode: `Bilinear`
- Wrap Mode: `Clamp`
- Max Size: `2048`
- Compression: `None`

모든 원본은 최대 1536×1536이므로 Max Size 2048에서 원본 해상도가 유지됩니다.

## 크기와 9-slice

이 세트의 프레임에는 중앙 엠블럼, 고정된 Trust 핀 5개, Theory 슬롯 3개, 리본 장식이 하나의 이미지로 포함되어 있습니다. 중앙 영역을 늘리면 장식과 슬롯 간격이 왜곡되므로 Sprite Border를 적용하지 않습니다.

Unity `Image` 컴포넌트에서는 `Image Type: Simple`과 `Preserve Aspect`를 사용하고, 기준 비율을 유지한 균일 스케일로 배치합니다. 가변 폭 패널이 필요한 경우에는 기존 `Panels`, `Buttons`, `Sliders` 폴더의 9-slice 스프라이트를 배경으로 사용하고 이 세트의 장식을 별도 레이어로 올립니다.

## 런타임 사용

### Global

- `ui_meter_fill_anxiety`와 `ui_meter_fill_integrity`는 `Image Type: Filled` 또는 `RectMask2D`로 현재값을 표시합니다.
- Public Anxiety의 70 지점에 `ui_marker_anxiety_70`을 고정합니다.
- Anxiety 100과 Integrity 저하 상태는 해당 Overlay의 알파 또는 활성 상태로 표현합니다.
- 숫자와 상태 문구는 TextMeshPro로 별도 표시합니다.

### Trust

- `ui_trust_row_frame` 위에 Empty 또는 Filled Pip 5개를 배치합니다.
- 현재 Trust 값만큼 Filled Pip을 활성화합니다.
- 증감 시 Gain 또는 Loss Overlay를 짧게 재생합니다.

### Theory

- `ui_theory_slots_panel`은 3개 슬롯이 포함된 고정 비율 배경입니다.
- Empty와 Active 카드는 각 슬롯의 자식으로 배치합니다.
- 선택 강조에는 기존 `Overlays/ui_overlay_focus`를 재사용합니다.
- 세 슬롯이 모두 차면 `ui_theory_full_overlay`를 표시합니다.

### Time

- AM, PM, NIGHT 아이콘은 현재 시간대만 밝게 표시하고 나머지는 `Graphic.color`로 채도를 낮춥니다.
- 사용 가능한 행동에는 `ui_action_token_available`, 사용한 행동에는 `ui_action_token_spent`를 사용합니다.

### Flags

- `ui_flag_tag_frame`을 공통 배경으로 사용하고 권한별 스프라이트를 내부 아이콘처럼 배치합니다.
- 미획득 플래그는 스포일러 방지를 위해 목록에 미리 노출하지 않습니다.
- 새 권한 획득 표시는 기존 `Overlays/ui_overlay_new`를 재사용할 수 있습니다.

## 버전 관리

작업용 ZIP과 콘택트 시트는 저장소에 포함하지 않습니다. 실제 런타임에서 사용하는 PNG와 Unity가 생성한 `.meta`만 함께 커밋하여 팀 전체가 동일한 GUID와 임포트 설정을 사용하도록 합니다.
