# Status HUD Sprite Library

게임의 상태 지표를 표현하는 투명 PNG Sprite 모음입니다.
전역 상태, 인물별 신뢰도, 가설 슬롯, 시간대와 조사 권한을 기능별 폴더로 분류합니다.

## 폴더 구성

| 폴더 | 수량 | 용도 |
| --- | ---: | --- |
| `Global` | 10 | Public Anxiety, Evidence Integrity, 공통 HUD와 상태 변화 표시 |
| `Trust` | 5 | 인물별 Trust 0~5 표시와 증감 피드백 |
| `Theory` | 5 | Theory Slots 3개와 활성·가득 참·시간 비용 상태 |
| `Time` | 6 | AM/PM/NIGHT 다이얼과 행동 토큰 |
| `Flags` | 4 | 비서실 권한, 천장 조사, 화물 레일 조사 태그 |

## Unity Import 설정

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

모든 파일은 실제 알파 채널을 포함하며, 체크무늬나 단색 배경이 이미지에 합성된 파일은 사용하지 않습니다.

## 권장 사용

### Global

- `ui_meter_fill_anxiety`와 `ui_meter_fill_integrity`는 `Image Type: Filled` 또는 `RectMask2D`로 현재 값을 표시합니다.
- Public Anxiety 70 지점에 `ui_marker_anxiety_70`을 고정합니다.
- Anxiety 100과 Integrity 위험 상태는 대응 Overlay의 알파 또는 활성 상태로 표현합니다.
- 숫자와 상태 문구는 TextMeshPro로 별도 표시합니다.

### Trust

- `ui_trust_row_frame` 위에 Empty 또는 Filled Pip 5개를 배치합니다.
- 현재 Trust 값만큼 Filled Pip을 활성화합니다.
- 증감 시 Gain 또는 Loss Overlay를 짧게 재생합니다.

### Theory

- `ui_theory_slots_panel`은 3개 슬롯을 포함한 고정 비율 배경입니다.
- Empty와 Active 카드는 각 슬롯의 자식으로 배치합니다.
- 선택 강조에는 `Overlays/ui_overlay_focus`를 사용할 수 있습니다.
- 세 슬롯이 모두 차면 `ui_theory_full_overlay`를 표시합니다.

### Time

- AM, PM, NIGHT 아이콘 중 현재 시간대만 밝게 표시합니다.
- 사용할 수 있는 행동은 `ui_action_token_available`, 사용한 행동은 `ui_action_token_spent`를 사용합니다.

### Flags

- `ui_flag_tag_frame`을 공통 배경으로 사용하고 권한별 Sprite를 내부 아이콘처럼 배치합니다.
- 미획득 플래그는 스포일러 방지를 위해 목록에서 미리 노출하지 않습니다.
- 새 권한 획득 표시에는 `Overlays/ui_overlay_new`를 조합할 수 있습니다.

## 버전 관리

PNG와 Unity가 생성한 `.meta` 파일을 함께 커밋합니다.
기존 파일을 교체할 때는 `.meta`를 보존하여 GUID와 씬·프리팹 참조가 유지되게 합니다.
원본 ZIP과 콜라주 이미지는 레포에 포함하지 않습니다.
