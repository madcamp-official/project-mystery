# Character Expression Portraits

`Under the Horizon`의 주요 인물 9명을 위한 표정 Sprite 모음입니다.
각 PNG는 2×2 표정 시트이며 Unity에서 `Sprite Mode: Multiple`로 분할됩니다.

## 포함 인물

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

## 표정 배치

Unity Sprite 좌표는 왼쪽 아래가 원점이지만, 표정 이름은 원본 이미지를 보는 방향으로 정의합니다.

| 원본 배치 | Expression | Sprite suffix |
| --- | --- | --- |
| 왼쪽 위 | 기본·자신감 | `_neutral` |
| 오른쪽 위 | 걱정·불안 | `_concerned` |
| 왼쪽 아래 | 분노·경계 | `_angry` |
| 오른쪽 아래 | 기쁨·웃음 | `_happy` |

## Sprite 명세

| Character | Expression | Sprite name |
| --- | --- | --- |
| Adrian Vale | Neutral | `portrait_adrian_vale_neutral` |
| Adrian Vale | Concerned | `portrait_adrian_vale_concerned` |
| Adrian Vale | Angry | `portrait_adrian_vale_angry` |
| Adrian Vale | Happy | `portrait_adrian_vale_happy` |
| Daniel Mercer | Neutral | `portrait_daniel_mercer_neutral` |
| Daniel Mercer | Concerned | `portrait_daniel_mercer_concerned` |
| Daniel Mercer | Angry | `portrait_daniel_mercer_angry` |
| Daniel Mercer | Happy | `portrait_daniel_mercer_happy` |
| Richard Hawthorne | Neutral | `portrait_richard_hawthorne_neutral` |
| Richard Hawthorne | Concerned | `portrait_richard_hawthorne_concerned` |
| Richard Hawthorne | Angry | `portrait_richard_hawthorne_angry` |
| Richard Hawthorne | Happy | `portrait_richard_hawthorne_happy` |
| Evelyn Shaw | Neutral | `portrait_evelyn_shaw_neutral` |
| Evelyn Shaw | Concerned | `portrait_evelyn_shaw_concerned` |
| Evelyn Shaw | Angry | `portrait_evelyn_shaw_angry` |
| Evelyn Shaw | Happy | `portrait_evelyn_shaw_happy` |
| Claire Hawthorne | Neutral | `portrait_claire_hawthorne_neutral` |
| Claire Hawthorne | Concerned | `portrait_claire_hawthorne_concerned` |
| Claire Hawthorne | Angry | `portrait_claire_hawthorne_angry` |
| Claire Hawthorne | Happy | `portrait_claire_hawthorne_happy` |
| Captain Thomas Reed | Neutral | `portrait_thomas_reed_neutral` |
| Captain Thomas Reed | Concerned | `portrait_thomas_reed_concerned` |
| Captain Thomas Reed | Angry | `portrait_thomas_reed_angry` |
| Captain Thomas Reed | Happy | `portrait_thomas_reed_happy` |
| Marcus Bell | Neutral | `portrait_marcus_bell_neutral` |
| Marcus Bell | Concerned | `portrait_marcus_bell_concerned` |
| Marcus Bell | Angry | `portrait_marcus_bell_angry` |
| Marcus Bell | Happy | `portrait_marcus_bell_happy` |
| Dr. Helena Ward | Neutral | `portrait_helena_ward_neutral` |
| Dr. Helena Ward | Concerned | `portrait_helena_ward_concerned` |
| Dr. Helena Ward | Angry | `portrait_helena_ward_angry` |
| Dr. Helena Ward | Happy | `portrait_helena_ward_happy` |
| Owen Price | Neutral | `portrait_owen_price_neutral` |
| Owen Price | Concerned | `portrait_owen_price_concerned` |
| Owen Price | Angry | `portrait_owen_price_angry` |
| Owen Price | Happy | `portrait_owen_price_happy` |

## Unity Import 기준

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

가로 길이가 홀수인 Richard 시트(`859×1024`)는 모든 픽셀을 보존하기 위해
왼쪽 열을 429px, 오른쪽 열을 430px로 분할합니다.

## 투명 배경 정책

9개 표정 시트는 모두 실제 알파 채널을 포함합니다.
Richard 원본에 합성되어 있던 회색 체크무늬 배경은 제거했으며,
Unity UI에서 인물 외곽만 자연스럽게 표시되도록 반투명 경계와 디스필을 적용했습니다.
향후 표정 시트를 추가할 때도 체크무늬가 이미지에 합성된 파일은 사용하지 않습니다.

## 런타임 연결

이 폴더는 `Resources/CharacterExpressions`에서 로드되는 표정별 Sprite 라이브러리입니다.
`DialogueController`는 프로덕션 CSV의 29개 감정 토큰을 4개 표정 상태로 정규화하고,
캐릭터 ID와 표정에 맞는 Sprite를 대화창 `RawImage`에 표시합니다.
표정 시트나 Sprite를 찾지 못한 경우에만 기존 `Resources/Characters` 초상화를 사용합니다.
