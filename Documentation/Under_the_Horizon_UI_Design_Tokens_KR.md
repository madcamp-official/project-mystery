# Under the Horizon UI 디자인 토큰

## 목적

WF-01~WF-40 화면은 개별 색상과 크기를 직접 지정하지 않고
`UiVisualTheme`의 의미 기반 토큰을 사용한다. 폰트 파일 선택은 기존
`TypographyCatalog`가 담당하고, `UiVisualTheme`는 글자 크기·색상·행간과
나머지 시각 속성을 담당한다.

기본 에셋 경로는 다음과 같다.

```text
Assets/_Project/Resources/UI/UiVisualTheme.asset
```

## 토큰 기준

### 색상

| 토큰 | 용도 |
|---|---|
| `Canvas` | 화면 최하단 남청색 배경 |
| `Surface` | 기본 패널 |
| `SurfaceRaised` | 선택지·카드·강조 패널 |
| `SurfaceOverlay` | 딤, 토스트, 모달 배경 |
| `Brass` | 주요 행동과 선택 상태 |
| `Cream` | 제목, 포커스, 장식선 |
| `TextPrimary` | 본문 |
| `TextSecondary` | 보조 설명과 비활성 정보 |
| `Disabled` | 잠금·비활성 상태 |
| `Success` | 완료·확인된 기록 |
| `Warning` | 주의·현재 진행 |
| `Danger` | 오류·불신·위험 |
| `Focus` | 키보드·게임패드 포커스 링 |

색상 토큰은 기능의 의미를 나타낸다. 화면 코드에서 RGB 값을 직접 지정하거나
`Brass`를 단순 장식색으로 남용하지 않는다.

### 간격

간격은 4, 8, 16, 24, 32, 48의 여섯 단계만 사용한다. 새 화면 셸과
Inspector 플레이스홀더는 이 간격을 기준으로 패딩과 요소 간 거리를 정한다.

### 글자

| 스타일 | 기본 역할 |
|---|---|
| `Caption` | 보조 설명 |
| `Body` | 일반 본문 |
| `BodyLarge` | 대화 본문 |
| `Choice` | 선택지 |
| `SpeakerName` | 화자명 |
| `Heading` | 화면·장소 제목 |
| `Display` | 챕터·엔딩 제목 |
| `Technical` | 시간·기계 정보 |
| `Handwritten` | 편지·개인 기록 |
| `Alert` | 긴급 연출 |

대화 본문이 길 때 `BodyLarge`의 크기를 임의로 계속 줄이지 않는다. 이후
대화 셸 단계에서 문장 단위 페이지 분할을 적용한다.

### 버튼

`Primary`, `Secondary`, `Quiet`, `Danger` 네 종류가 각자 기본·호버·누름·
선택·비활성 색상과 라벨 스타일을 가진다. 한 화면의 우하단 주요 행동은
`Primary` 하나만 사용한다.

## 코드와 Inspector 사용

코드에서는 의미를 요청한다.

```csharp
UiVisualThemeService.ApplyText(label, UiTextStyle.BodyLarge);
UiVisualThemeService.ApplyButton(confirmButton, UiButtonStyle.Primary);
UiVisualThemeService.ApplySurface(panel, UiSurfaceStyle.Panel);
```

Hierarchy에서 미리 확인해야 하는 authored UI에는 `UiThemeBinding`을 붙이고
적용할 Surface, Text, Button 스타일을 선택한다. 이 컴포넌트는 Edit Mode와
Play Mode에서 같은 `UiVisualTheme`을 사용하므로 실행 전 배치 검토가 가능하다.

## 현재 연결 범위

- 지도 배경 대체색, 장소 노드 상태, 포커스 외곽선
- Evidence 캐러셀의 수집·불확실·기본 상태
- Toast 패널과 본문·경고 스타일
- 공통 버튼 호버·누름 크기와 명도

이번 단계는 토큰 기반을 만드는 범위다. 공통 7구역 셸과 WF-01~WF-40의
전체 스타일 마이그레이션은 후속 구현 순서에서 진행한다.
