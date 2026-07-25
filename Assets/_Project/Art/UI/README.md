# UI Sprite Library

프로젝트의 버튼, 패널, 카드, 배지, 오버레이를 Unity UI에서 바로 사용할 수 있도록 정리한 투명 PNG Sprite 모음입니다.

## 폴더 구성

| 폴더 | 용도 |
| --- | --- |
| `Buttons` | Primary, Standard, Tab, Choice, Back 버튼 |
| `Panels` | 대화 패널, 팝업 패널, 캐릭터 이름표 |
| `Cards` | 증거 카드 기본 프레임과 선택 오버레이 |
| `Badges` | 원형·육각형 배지, 화살표, 회전, 설정 아이콘 |
| `Sliders` | 슬라이더 트랙과 노브 |
| `Overlays` | 포커스, 잠금, 신규, 위험 상태 오버레이 |
| `Markers` | 지도 및 룸 노드 마커 |
| `StatusHUD` | 시간, 신뢰도, 가설, 플래그와 전역 상태 HUD |

파일명은 `ui_<category>_<purpose>_<state>.png` 규칙을 사용합니다.
버튼 상태는 `normal`과 `pressed`를 한 쌍으로 제공합니다.

## Unity Import 설정

- Texture Type: `Sprite (2D and UI)`
- Sprite Mode: `Single`
- Mesh Type: `Full Rect`
- Pixels Per Unit: `100`
- Alpha Source: `Input Texture Alpha`
- Alpha Is Transparency: 활성화
- Generate Mip Maps: 비활성화
- Filter Mode: `Bilinear`
- Wrap Mode: `Clamp`
- Compression: `None`

2,048px보다 큰 원본을 보존해야 하는 버튼은 Default Platform Max Size를 `4096`으로 설정합니다.
플랫폼별 메모리 사용량이 문제가 되면 Standalone Override에서 `2048` 이하로 제한할 수 있습니다.

## 투명 배경 정책

- 모든 UI PNG는 실제 RGBA 알파 채널을 포함해야 합니다.
- 체크무늬, 흰색, 검은색 배경이 합성된 이미지는 사용하지 않습니다.
- 외곽 그림자와 반투명 안티앨리어싱은 보존합니다.
- 기존 자산을 교체할 때 PNG만 덮어쓰고 `.meta` GUID는 유지합니다.

## 9-slice 설정

가로로 늘어나는 버튼·대화 패널·이름표·슬라이더 트랙에는 가장자리 장식을 보호할 수 있는 Sprite Border를 지정합니다.
팝업처럼 세로와 가로가 모두 늘어나는 패널은 네 방향 Border를 지정합니다.

Unity `Image` 컴포넌트의 `Image Type`을 `Sliced`로 설정해야 9-slice가 적용됩니다.
증거 카드처럼 장식이 프레임 전체에 포함된 자산은 원본 비율을 유지한 `Simple` 타입을 우선 사용합니다.

## 사용 가이드

- 버튼은 `Button > Transition > Sprite Swap`에서 Normal과 Pressed Sprite를 연결합니다.
- Hover와 Disabled는 우선 `Color Tint`를 사용하고 필요할 때 별도 Sprite를 추가합니다.
- 포커스·잠금·신규 상태는 기본 카드나 버튼 위에 독립 Overlay Image로 배치합니다.
- 배지와 마커는 `Preserve Aspect`를 활성화합니다.
- 텍스트는 이미지에 합성하지 않고 TextMeshPro로 별도 배치합니다.
- 크기가 달라진 교체 자산은 씬에서 `Set Native Size`를 사용하기 전에 레이아웃 기준 크기를 확인합니다.

## 버전 관리

PNG와 대응 `.meta`를 함께 관리합니다.
원본 ZIP, 콜라주, 생성 중간 파일은 레포에 포함하지 않습니다.
교체 전후의 GUID가 동일한지 확인해 씬, 프리팹, ScriptableObject 참조가 유지되도록 합니다.
