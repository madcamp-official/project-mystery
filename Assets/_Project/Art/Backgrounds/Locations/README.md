# Location Backgrounds

`MV Elysium` 선내 장소 화면에 사용할 배경 스프라이트 모음입니다.
전체 25개 장소를 모두 포함하며, 원본 이미지는 자르거나 비율을 통일하지 않고 보존합니다.

## 포함된 장소

| Deck | Location | Asset |
| --- | --- | --- |
| — | PORT | `bg_location_port.png` |
| — | GANGWAY | `bg_location_gangway.png` |
| 10 | Richard Suite | `bg_location_d10_1_richard_suite.png` |
| 10 | VIP Lounge | `bg_location_d10_2_vip_lounge.png` |
| 10 | Open Deck | `bg_location_d10_3_open_deck.png` |
| 9 | Ballroom | `bg_location_d9_1_ballroom.png` |
| 9 | Dining | `bg_location_d9_2_dining.png` |
| 9 | Promenade | `bg_location_d9_3_promenade.png` |
| 9 | Horizon Room | `bg_location_d9_4_horizon_room.png` |
| 8 | Atrium | `bg_location_d8_1_atrium.png` |
| 8 | News Lounge | `bg_location_d8_2_news_lounge.png` |
| 8 | Security | `bg_location_d8_3_security.png` |
| 8 | Service Rail | `bg_location_d8_4_service_rail.png` |
| 7 | Medbay | `bg_location_d7_1_medbay.png` |
| 7 | Ballast Control Annex | `bg_location_d7_2_ballast_control_annex.png` |
| 7 | Engine Control | `bg_location_d7_3_engine_control.png` |
| 7 | Crew Stairs | `bg_location_d7_4_crew_stairs.png` |
| 6 | Vault | `bg_location_d6_1_vault.png` |
| 6 | Archive | `bg_location_d6_2_archive.png` |
| 6 | Laundry | `bg_location_d6_3_laundry.png` |
| 6 | Service Hub | `bg_location_d6_4_service_hub.png` |
| 5 | Stabilizers | `bg_location_d5_1_stabilizers.png` |
| 5 | Ballast Tanks | `bg_location_d5_2_ballast_tanks.png` |
| 5 | Generator | `bg_location_d5_3_generator.png` |
| 5 | Workshop | `bg_location_d5_4_workshop.png` |

Horizon Room은 자정의 사건 현장이 포함된 최신 배경으로 교체되었습니다.

## Unity 임포트 기준

- Texture Type: `Sprite (2D and UI)`
- Sprite Mode: `Single`
- Mesh Type: `Full Rect`
- Pixels Per Unit: `100`
- sRGB: 활성화
- Alpha Source: `None`
- Read/Write: 비활성화
- Mip Maps: 비활성화
- Wrap Mode: `Clamp`
- Filter Mode: `Bilinear`
- Max Size: `2048`
- Compression: `High Quality`

모든 파일은 불투명 RGB 배경이며 최대 변 길이가 2048 이하이므로 원본 해상도를 유지합니다.

## UI 사용 지침

- 전체 화면 UI에서는 `Image Type`을 `Simple`로 사용합니다.
- 배경의 가로세로 비율을 유지하고, 화면 비율에 따라 레터박스 또는 중앙 크롭 정책을 선택합니다.
- 현재 이미지에는 3:2, 16:9, 4:3 계열 비율이 섞여 있으므로 임의로 늘려서 왜곡하지 않습니다.
- 이 폴더는 재사용 가능한 원본 스프라이트 라이브러리이며, 씬·프리팹·`LocationDefinition` 연결은 별도 작업에서 진행합니다.

## 파일명 규칙

`bg_location_<deck>_<room>_<location>.png`

장소 코드와 Deck/Room 번호가 없는 장소는 `bg_location_<location>.png` 형식을 사용합니다.
따라서 PORT와 GANGWAY는 각각 `bg_location_port.png`, `bg_location_gangway.png`로 관리합니다.
