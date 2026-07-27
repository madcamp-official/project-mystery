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
`DialogueController`는 프로덕션 CSV의 109개 감정 토큰을 4개 표정 상태로 정규화하고,
캐릭터 ID와 표정에 맞는 Sprite를 대화창 `RawImage`에 표시합니다.
표정 시트나 Sprite를 찾지 못한 경우에만 기존 `Resources/Characters` 초상화를 사용합니다.

## 공식 대사집 감정 계약

공식 XLSX 개정판에는 서로 다른 `emotion` 값이 109개 있습니다.

런타임은 이 값을 Neutral, Concerned, Angry, Positive 네 표정으로 정규화합니다.

표에 없는 값은 안전하게 Neutral로 표시하지만 콘텐츠 검증에서는 누락으로 취급합니다.

장소 코드처럼 보이는 여섯 값도 현재 공식 원본의 emotion 열에 있으므로 명시적으로 처리합니다.

### Neutral

| 공식 emotion | 표현 의도 |
| --- | --- |
| ARCHIVE | 장소 메타가 들어간 내레이션 |
| CABIN_DANIEL | 장소 메타가 들어간 내레이션 |
| PORT | 장소 메타가 들어간 내레이션 |
| PROMENADE | 장소 메타가 들어간 내레이션 |
| STERN | 장소 메타가 들어간 내레이션 |
| VAULT | 장소 메타가 들어간 내레이션 |
| alert | 경계하지만 표정 과장 없음 |
| businesslike | 업무적인 태도 |
| calm | 차분함 |
| choice | 선택지 시스템 |
| clinical | 임상적 설명 |
| cold | 냉정함 |
| controlled | 감정 통제 |
| cool | 침착함 |
| dry | 건조한 말투 |
| ending:B_complete | B 엔딩 제목 |
| formal | 공식적인 말투 |
| hint1 | 1단계 힌트 |
| hint2 | 2단계 힌트 |
| hint3 | 3단계 힌트 |
| internal | 내적 독백 |
| matter_of_fact | 사실 전달 |
| measured | 절제된 말투 |
| neutral | 기본 표정 |
| observe | 관찰 |
| polite | 예의 바름 |
| professional | 전문적 태도 |
| recorded | 레거시 녹음 음성 별칭 |
| system | 시스템 안내 |
| tutorial | 튜토리얼 안내 |

### Concerned

| 공식 emotion | 표현 의도 |
| --- | --- |
| afraid | 두려움 |
| alarmed | 놀람과 경계 |
| breathless | 숨 가쁜 상태 |
| broken | 무너진 상태 |
| cautious | 조심스러움 |
| concerned | 걱정 |
| conflicted | 내적 갈등 |
| defeated | 패배감 |
| desperate | 절박함 |
| disappointed | 실망 |
| emotional | 감정적 동요 |
| ending:C_complete | C 엔딩 제목 |
| ending:bad_complete | Bad 엔딩 제목 |
| fading | 기력이 약해짐 |
| fearful | 공포 |
| frightened | 겁먹음 |
| guilty | 죄책감 |
| horrified | 충격과 공포 |
| hurt | 상처받음 |
| low | 낮고 불안한 상태 |
| nervous | 긴장 |
| pained | 고통 |
| panicked | 공황 |
| pleading | 애원 |
| reluctant | 망설임 |
| sad | 슬픔 |
| shaken | 동요 |
| startled | 깜짝 놀람 |
| subdued | 위축 |
| surprised | 놀람 |
| uncertain | 불확실함 |
| uneasy | 불편함 |
| urgent | 다급함 |
| weary | 지침 |

### Angry

| 공식 emotion | 표현 의도 |
| --- | --- |
| angry | 분노 |
| authoritative | 권위적 압박 |
| bitter | 쓰라린 적대감 |
| challenging | 도발 |
| commanding | 명령 |
| corrective | 강한 정정 |
| defensive | 방어적 태도 |
| defiant | 반항 |
| deflect | 질문 회피 |
| drunk_irritated | 취중 짜증 |
| firm | 단호함 |
| focused | 날카로운 집중 |
| furious | 격분 |
| grave | 엄중함 |
| grim | 냉혹함 |
| gruff | 거친 태도 |
| guarded | 경계 |
| hard | 강경함 |
| insistent | 강한 주장 |
| intense | 강렬함 |
| irritated | 짜증 |
| lying | 거짓말 긴장 |
| offended | 불쾌함 |
| press | 심문 압박 |
| provoking | 도발 |
| resentful | 원망 |
| serious | 심각함 |
| severe | 준엄함 |
| sharp | 날카로움 |
| skeptical | 의심 |
| suspicious | 수상하게 여김 |
| warning | 경고 |

### Positive

| 공식 emotion | 표현 의도 |
| --- | --- |
| confident | 자신감 |
| curious | 호기심 |
| decisive | 결단 |
| direct | 명료한 확신 |
| ending:A_complete | A 엔딩 제목 |
| gentle | 온화함 |
| helpful | 협조 |
| light | 가벼운 분위기 |
| realization | 추리 성공 |
| relieved | 안도 |
| resolved | 결심 |
| smile | 미소 |
| warm | 따뜻함 |
| wry | 씁쓸한 유머 |

## 호환 별칭

초기 프로토타입과 저장된 대사 자산을 안전하게 읽기 위해 다음 별칭을 유지합니다.

| 별칭 | 표정 |
| --- | --- |
| confused | Concerned |
| ashamed | Concerned |
| fear | Concerned |
| fake_fear | Concerned |
| tense | Concerned |
| accuse | Angry |
| anger | Angry |
| deduction | Positive |
| final | Positive |

호환 별칭은 새 XLSX에 다시 쓰지 않습니다.

새 대사는 가능한 한 공식 109개 값 중 하나를 사용합니다.

## 매핑 변경 규칙

1. XLSX를 갱신한 뒤 `emotion` 고유값을 다시 계산합니다.
2. 새 토큰이 있으면 표정 의도를 기획 문맥에서 확인합니다.
3. 네 표정 중 가장 가까운 상태를 선택합니다.
4. 런타임 사전과 이 문서를 같은 PR에서 수정합니다.
5. 공식 CSV 전체가 `IsKnownEmotion`을 통과하는지 검사합니다.
6. 표정 시트 36개가 그대로 로드되는지 검사합니다.
7. 화자 전환 때 이전 초상이 남지 않는지 PlayMode에서 확인합니다.
8. 내레이션과 시스템 행은 초상을 숨기는 규칙을 우선합니다.
9. 엔딩 제목 토큰은 표정 전환보다 엔딩 UI 상태가 우선합니다.
10. 장소 코드 토큰은 원본 정정 전까지 Neutral 호환으로 유지합니다.

## 시각 QA 체크리스트

- Neutral이 과도한 감정 연기를 만들지 않는가?
- Concerned가 걱정, 공포, 슬픔 문맥을 자연스럽게 포괄하는가?
- Angry가 심문 압박과 실제 분노를 구분 가능한 연출과 함께 쓰이는가?
- Positive가 추리 성공과 안도 장면에 사용되는가?
- 감정 토큰이 바뀔 때 Sprite가 같은 프레임에 갱신되는가?
- 녹음 음성에서 현재 화자의 초상 규칙이 유지되는가?
- NPC와 시스템 대사에 잘못된 주연 초상이 표시되지 않는가?
- 16:9와 16:10에서 초상 비율이 유지되는가?
- 초상이 긴 대사나 선택지 버튼을 가리지 않는가?
- Sprite 누락 시 fallback 초상이 대사를 막지 않는가?
