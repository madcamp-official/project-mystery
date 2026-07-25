# Character Expression Portraits

`The Wake Without Footprints` 주요 인물 9명의 대화 및 인물 UI용 표정 Sprite 모음입니다.
각 원본 PNG는 2×2 표정 시트이며 Unity에서 `Sprite Mode: Multiple`로 분할합니다.

## 포함된 인물

| Character | Sheet | Sprite prefix |
| --- | --- | --- |
| Adrian Vale | `portrait_adrian_vale_expressions.png` | `portrait_adrian_vale_*` |
| Daniel Mercer | `portrait_daniel_mercer_expressions.png` | `portrait_daniel_mercer_*` |
| Richard Hawthorne | `portrait_richard_hawthorne_expressions.png` | `portrait_richard_hawthorne_*` |
| Evelyn Shaw | `portrait_evelyn_shaw_expressions.png` | `portrait_evelyn_shaw_*` |
| Claire Hawthorne | `portrait_claire_hawthorne_expressions.png` | `portrait_claire_hawthorne_*` |
| Captain Thomas Reed | `portrait_thomas_reed_expressions.png` | `portrait_thomas_reed_*` |
| Marcus Bell | `portrait_marcus_bell_expressions.png` | `portrait_marcus_bell_*` |
| Dr. Helena Ward | `portrait_helena_ward_expressions.png` | `portrait_helena_ward_*` |
| Owen Price | `portrait_owen_price_expressions.png` | `portrait_owen_price_*` |

## 표정 배치와 Sprite 이름

Unity의 Sprite 좌표는 좌하단이 원점이지만, 표정 의미는 원본 이미지를 보는 방향으로 정의합니다.

| 원본 배치 | Expression | Sprite suffix |
| --- | --- | --- |
| 좌상단 | 기본·자신감 | `_neutral` |
| 우상단 | 걱정·불안 | `_concerned` |
| 좌하단 | 분노·경계 | `_angry` |
| 우하단 | 기쁨·웃음 | `_happy` |

각 인물은 위 네 가지 표정을 가지며 총 36개의 Sprite sub-asset이 생성됩니다.

## Unity 임포트 기준

- Texture Type: `Sprite (2D and UI)`
- Sprite Mode: `Multiple`
- Slice: `2 columns × 2 rows`
- Mesh Type: `Full Rect`
- Pivot: `Center`
- Pixels Per Unit: `100`
- sRGB: 활성화
- Alpha Source: `Input Texture Alpha`
- Alpha Is Transparency: 활성화
- Read/Write: 비활성화
- Mip Maps: 비활성화
- Wrap Mode: `Clamp`
- Filter Mode: `Bilinear`
- Max Size: `2048`
- Compression: `High Quality`
- Fallback Physics Shape: 비활성화

가로 길이가 홀수인 Richard 시트(`859×1024`)는 모든 픽셀을 보존하기 위해 왼쪽 열을 429px, 오른쪽 열을 430px로 분할합니다.

## 원본 알파 주의사항

8개 시트는 실제 투명 알파를 포함합니다. Richard 시트는 원본 PNG의 체크무늬가 이미지에 합성된 불투명 배경이며,
이번 작업에서는 원본을 임의 보정하지 않습니다. 투명 배경이 필요한 경우 체크무늬가 제거된 원본으로 교체해야 합니다.

## 런타임 연결 범위

이 폴더는 표정별 Sprite 라이브러리입니다. 현재 `Resources/Characters`의 기존 초상화와
`DialogueController`의 `RawImage` 기반 출력은 변경하지 않습니다. 표정 ID에 따른 대화창 교체 로직은 별도 작업에서 연결합니다.
