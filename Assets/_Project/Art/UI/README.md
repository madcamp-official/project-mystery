# UI Sprite Library

프로젝트의 핵심 버튼, 패널, 카드, 배지, 오버레이를 Unity UI에서 바로 사용할 수 있도록 정리한 PNG 스프라이트 세트입니다.

## 폴더 구성

| 폴더 | 용도 |
| --- | --- |
| `Buttons` | Primary, Standard, Tab, Choice, Back 버튼 |
| `Panels` | 대화 패널, 팝업 패널, 캐릭터 이름표 |
| `Cards` | 증거 카드 기본 프레임과 선택 오버레이 |
| `Badges` | 원형·육각형 배지, 화살표, 회전, 설정 아이콘 |
| `Sliders` | 슬라이더 트랙과 노브 |
| `Overlays` | 포커스, 잠금, 신규, 위험 상태 오버레이 |
| `Markers` | 지도 방 노드 마커 |

파일명은 `ui_<category>_<purpose>_<state>.png` 규칙을 사용합니다. 버튼 상태는 `normal`과 `pressed`를 한 쌍으로 제공합니다.

## Unity 임포트 설정

모든 PNG는 다음 설정으로 임포트되어 있습니다.

- Texture Type: `Sprite (2D and UI)`
- Sprite Mode: `Single`
- Mesh Type: `Full Rect`
- Pixels Per Unit: `100`
- Alpha Source: `Input Texture Alpha`
- Alpha Is Transparency: 활성화
- Generate Mip Maps: 비활성화
- Filter Mode: `Bilinear`
- Wrap Mode: `Clamp`
- Max Size: `4096`
- Compression: `None`

원본의 2172px 너비를 보존하기 위해 Max Size를 4096으로 설정했습니다. UI에서 최종 메모리 사용량이 문제가 되면 플랫폼별 Override를 추가해 2048 또는 압축 포맷을 적용합니다.

## 9-slice 설정

가로로 늘려 쓰는 버튼·대화 패널·이름표·슬라이더 트랙에는 `Left 320 / Bottom 160 / Right 320 / Top 160` 테두리를 적용했습니다.

세로·가로 팝업 패널에는 `Left 160 / Bottom 160 / Right 160 / Top 160` 테두리를 적용했습니다.

Unity `Image` 컴포넌트에서 `Image Type`을 `Sliced`로 설정해야 9-slice가 적용됩니다. 증거 카드는 하단 이름표 장식이 프레임에 포함되어 있어 비율을 유지하는 `Simple` 이미지로 사용하고, 크기 변형이 필요하면 별도 배경과 장식 레이어로 분리하는 것을 권장합니다.

## 사용 가이드

- 버튼은 `Button > Transition > Sprite Swap`에서 `normal`과 `pressed` 스프라이트를 연결합니다.
- Hover와 Disabled 상태는 먼저 `Color Tint`를 사용하고, 독립 아트가 필요해질 때 상태 스프라이트를 추가합니다.
- 선택·잠금·포커스 표시는 기본 카드 또는 버튼 위에 오버레이 이미지를 별도 레이어로 배치합니다.
- 배지와 마커는 원본 종횡비를 유지하고 `Preserve Aspect`를 활성화합니다.
- 텍스트는 이미지에 포함하지 않고 TextMeshPro로 별도 배치해 다국어와 접근성 대응을 유지합니다.

## 소스 및 버전 관리

이 라이브러리는 다음 작업용 압축 파일에서 실제 PNG만 선별해 가져왔습니다.

- 핵심 버튼 프레임 세트
- 보조 UI 세트
- 패널·카드·배지 세트

압축 파일 자체는 저장소에 포함하지 않습니다. Unity가 생성한 `.meta` 파일은 GUID와 임포트 설정을 팀 전체에서 동일하게 유지하므로 PNG와 함께 반드시 커밋합니다.
