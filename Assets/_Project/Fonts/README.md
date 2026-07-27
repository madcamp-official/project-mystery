# 프로젝트 타이포그래피 운영 가이드

이 문서는 프로젝트의 한국어 폰트 자산, 역할 체계, 재생성 절차,
글리프 검사, 화면 적용 규칙과 릴리스 확인 항목을 설명합니다.

새 UI는 폰트 파일을 직접 참조하지 않습니다.
`TypographyRole`을 선택하고 `TypographyService`를 통해 적용합니다.
이 원칙은 폰트 교체와 폴백 정책을 한곳에서 관리하기 위한 것입니다.

## 1. 폴더 구조

```text
Assets/_Project/Fonts/
├─ Source/
│  ├─ Pretendard/
│  ├─ SUITE/
│  ├─ IBMPlexMono/
│  └─ Special/
├─ TMP/
│  └─ ProjectGlyphs.txt
├─ Licenses/
└─ README.md

Assets/_Project/Resources/Typography/
├─ TypographyCatalog.asset
├─ Pretendard Medium SDF.asset
├─ Pretendard Regular SDF.asset
├─ Pretendard SemiBold SDF.asset
├─ SUITE SemiBold SDF.asset
├─ SUITE Bold SDF.asset
├─ SUITE ExtraBold SDF.asset
├─ IBM Plex Mono Medium SDF.asset
├─ IBM Plex Mono SemiBold SDF.asset
├─ Gowun Dodum Regular SDF.asset
├─ Black Han Sans Regular SDF.asset
└─ Jua Regular SDF.asset
```

`Source`에는 정적 TTF 원본만 둡니다.
`Resources/Typography`에는 Unity가 생성한 TMP SDF 자산을 둡니다.
`Licenses`에는 배포 폰트별 OFL 1.1 전문을 보관합니다.
`TMP/ProjectGlyphs.txt`는 실제 프로젝트 문자열에서 수집한 코퍼스입니다.

## 2. 사용 폰트

### Pretendard

- Regular 400
- Medium 500
- SemiBold 600
- 대사, 설명, 선택지와 일반 UI에 사용합니다.
- 한국어가 포함되는 다른 폰트의 최종 폴백입니다.

### SUITE

- SemiBold 600
- Bold 700
- ExtraBold 800
- 메뉴, 장소명, 인물명, 챕터와 강한 제목에 사용합니다.
- 긴 본문에는 사용하지 않습니다.

### IBM Plex Mono

- Medium 500
- SemiBold 600
- 시간, 증거 코드, 객실 번호와 장비 표시에 사용합니다.
- 한글이 섞이면 Pretendard Medium으로 폴백합니다.

### 특수 폰트

- Gowun Dodum Regular: 손편지와 개인 문서
- Black Han Sans Regular: 긴급 경고와 BAD END
- Jua Regular: 짧은 코믹 선택지
- 특수 폰트는 기본 UI 스타일로 사용하지 않습니다.

## 3. 역할 매핑

| 역할 | 자산 | 대표 용도 |
|---|---|---|
| `Body` | Pretendard Medium | 대사와 일반 설명 |
| `BodyRegular` | Pretendard Regular | 증거 설명과 엔딩 본문 |
| `Choice` | Pretendard SemiBold | 선택지와 동작 버튼 |
| `SpeakerName` | SUITE Bold | 인물 이름 |
| `Heading` | SUITE SemiBold | 메뉴와 장소명 |
| `HeadingStrong` | SUITE ExtraBold | 챕터와 주요 제목 |
| `Technical` | IBM Plex Mono Medium | 시간과 진행도 |
| `TechnicalStrong` | IBM Plex Mono SemiBold | 증거와 객실 코드 |
| `Handwritten` | Gowun Dodum Regular | 초대장과 개인 메모 |
| `SpecialAlert` | Black Han Sans Regular | BAD END와 긴급 경고 |
| `SpecialComic` | Jua Regular | 코믹 선택지 |

`TypographyCatalog`는 역할과 TMP 폰트 자산의 유일한 연결 지점입니다.
역할 자산이 없으면 `Body`를 먼저 사용합니다.
`Body`도 없으면 TMP 프로젝트 기본 폰트를 사용합니다.

## 4. 런타임 적용 원칙

새 텍스트에는 다음 형태를 사용합니다.

```csharp
TypographyService.Apply(label, TypographyRole.Body);
```

하위 텍스트 전체에 같은 기본 역할을 적용할 때는 다음을 사용합니다.

```csharp
TypographyService.ApplyRecursively(
    panel.transform,
    TypographyRole.Choice);
```

한 화면에 여러 역할이 있으면 화면 전용 정책 클래스를 둡니다.

```csharp
int count = TypographyService.ApplyRecursively(
    root,
    TypographyRole.Body);
TypographyService.Apply(title, TypographyRole.HeadingStrong);
TypographyService.Apply(code, TypographyRole.TechnicalStrong);
```

다음 방식은 새 코드에서 사용하지 않습니다.

```csharp
label.font = someFontAsset;
label.font = StatusHUDController.RuntimeKoreanFont;
```

`RuntimeKoreanFont`는 기존 호출자를 위한 호환 경로입니다.
신규 UI가 이 속성에 의존하지 않도록 감사 테스트가 보호합니다.

## 5. 화면별 적용 기준

### 대화

- 화자 이름은 `SpeakerName`입니다.
- 대사 본문은 `Body`입니다.
- 선택지는 `Choice`입니다.
- “농담” 토큰이 있는 선택지는 `SpecialComic`입니다.
- 재사용 라벨은 매번 역할을 다시 적용해야 합니다.

### 증거

- 증거 제목은 `Heading`입니다.
- 증거 코드는 `TechnicalStrong`입니다.
- 상세 설명은 `BodyRegular`입니다.
- category가 `invitation`이면 `Handwritten`입니다.
- category 비교는 대소문자만 무시합니다.

### HUD와 지도

- 시간 표시는 `Technical`입니다.
- 객실, 갑판, 증거 번호는 `TechnicalStrong`입니다.
- 장소명은 `Heading`입니다.
- 목표와 상태 설명은 `Body` 또는 `BodyRegular`입니다.

### 퍼즐과 엔딩

- 퍼즐 제목은 `HeadingStrong`입니다.
- 퍼즐 동작은 `Choice`입니다.
- 단서와 힌트는 `Body` 또는 `BodyRegular`입니다.
- 엔딩 경로 표시는 `Technical`입니다.
- 엔딩 제목은 `HeadingStrong`입니다.
- 엔딩 본문은 `BodyRegular`입니다.

### 특수 연출

- 일반 토스트는 `Body`입니다.
- BAD END 토스트는 `SpecialAlert`입니다.
- Jua는 “농담” 선택지에만 사용합니다.
- Gowun Dodum은 초대장처럼 개인 문서 맥락에만 사용합니다.
- 강조가 필요하다는 이유만으로 특수 역할을 선택하지 않습니다.

## 6. TMP 자산 재생성

Unity 메뉴에서 다음 순서로 실행합니다.

1. `Wake > Typography > Rebuild Font Assets`
2. `Wake > Typography > Collect Project Glyphs`
3. `Wake > Typography > Prepare Project Glyphs`
4. `Wake > Typography > Migrate Project Defaults`
5. `Wake > Typography > Validate Release Glyphs`

첫 단계는 11개 TMP 자산과 카탈로그를 다시 만듭니다.
생성 시점의 설정은 다음과 같습니다.

- Sampling Point Size: 60
- Atlas Padding: 6
- Glyph Render Mode: SDFAA
- Atlas Size: 1024 × 1024
- Multi Atlas: enabled
- 최초 Population Mode: Dynamic

`Prepare Project Glyphs`가 코퍼스 문자를 아틀라스에 추가합니다.
필수 역할의 아틀라스는 준비가 끝나면 Static으로 전환됩니다.
이후 `Validate Release Glyphs`는 동적 추가 없이 누락을 검사합니다.

재생성 후에는 생성된 `.asset` 파일을 모두 확인해야 합니다.
이 경로는 Git LFS로 관리됩니다.

```text
Assets/_Project/Resources/Typography/*.asset
```

일반 Git blob으로 들어가지 않았는지 `git lfs ls-files`로 확인합니다.

## 7. 글리프 코퍼스

수집기는 `Assets/_Project` 아래의 다음 확장자를 읽습니다.

- `.asset`
- `.cs`
- `.csv`
- `.json`
- `.txt`
- `.unity`

다음 경로는 수집에서 제외합니다.

- `/Editor/`
- `/Fonts/`
- `/Tests/`

제어 문자, 공백, 서로게이트 문자는 코퍼스에 넣지 않습니다.
알려진 인코딩 손상 감지 문자는 코퍼스에서 제외합니다.
문자는 정렬하고 중복을 제거한 뒤 UTF-8 BOM 없이 저장합니다.

대사나 증거 데이터를 추가했다면 코퍼스를 다시 수집해야 합니다.
코드에 사용자 노출 문자열을 추가했을 때도 다시 수집해야 합니다.
새 문장부호나 UI 기호를 추가한 경우 반드시 릴리스 검사를 실행합니다.

IBM Plex Mono에 한글이 없는 것은 정상입니다.
해당 자산은 Pretendard Medium을 폴백으로 가집니다.
검사는 폴백까지 포함해 실제 표시 가능 여부를 확인합니다.

## 8. TMP 기본값과 씬 마이그레이션

프로젝트 TMP 기본 폰트는 Pretendard Medium입니다.
TMP 폴백 목록에도 Pretendard Medium이 연결됩니다.
미지정 텍스트가 OS 폰트에 의존하지 않도록 하기 위한 설정입니다.

`Migrate Project Defaults`는 다음을 처리합니다.

- TMP Settings 기본 폰트 교체
- UI Basic Scene의 Liberation Sans 참조 교체
- 작성자가 만든 커스텀 TMP 머티리얼 보존
- 커스텀 머티리얼의 atlas texture 갱신

씬에 남은 `LiberationSans SDF Material (Instance)` 이름은
직렬화된 커스텀 머티리얼의 표시 이름일 수 있습니다.
실제 폰트 GUID와 atlas texture가 Pretendard를 가리키는지 확인합니다.
이름만 보고 기존 폰트를 다시 연결하지 않습니다.

## 9. 크기와 행간

기준 Canvas 해상도는 2880 × 1800입니다.
1080p 목표 픽셀 크기를 TMP 숫자에 그대로 복사하지 않습니다.
Canvas Scaler가 적용된 실제 렌더 크기를 기준으로 판단합니다.

현재 대화 시작값은 다음 범위입니다.

- 대사: 52–64
- 선택지: 48–58
- 인물 이름: 44–52
- 대사 line spacing: 12
- 선택지 line spacing: 10
- 인물 이름 line spacing: 6

1080p에서 기대하는 시각 크기는 다음과 같습니다.

- 대사: 약 36–40px
- 선택지: 약 32–36px
- 인물 이름: 약 28–32px
- 행 높이: 글자 높이의 약 140–150%
- 경고와 챕터 제목: 약 40–56px

문구 길이와 해상도에 따라 자동 크기 범위를 조정합니다.
폰트 크기만 줄여 넘침을 숨기지 않습니다.
버튼 높이, 패딩과 줄바꿈을 함께 확인합니다.

## 10. 해상도 검증

최소한 다음 화면 크기를 확인합니다.

- 1920 × 1080
- 2560 × 1440
- 1280 × 720
- 16:10 화면
- 울트라와이드 화면

각 해상도에서 다음을 확인합니다.

- 가장 긴 대사의 잘림
- 두 줄 이상 선택지의 버튼 높이
- 화자 이름의 기준선
- 증거 제목과 코드의 정렬
- 지도 장소명의 넘침
- 타임스탬프의 고정폭 정렬
- 엔딩 본문의 행간
- 특수 폰트가 지정 맥락에만 나타나는지

## 11. 테스트

타이포그래피 관련 EditMode 테스트는 다음 영역을 다룹니다.

- 카탈로그 역할 해석과 폴백
- 빌더의 원본 TTF 사양
- 대화 역할과 크기 지표
- 증거, HUD, 지도 역할
- 퍼즐, 엔딩, 조사 화면 역할
- 특수 폰트 제한 정책
- 글리프 수집과 릴리스 검사
- TMP 기본값과 씬 마이그레이션
- 런타임 직접 폰트 참조 회귀

변경 범위가 작아도 관련 필터 테스트를 먼저 실행합니다.
릴리스 전에는 전체 EditMode와 PlayMode 테스트를 실행합니다.
테스트가 0개 실행된 경우 성공으로 간주하지 않습니다.
필터의 전체 네임스페이스가 정확한지 확인합니다.

## 12. 빌드 전 체크리스트

- [ ] TTF 원본 11개가 존재한다.
- [ ] OFL 고지 6개가 존재한다.
- [ ] TMP 자산 11개가 존재한다.
- [ ] TypographyCatalog가 모든 역할을 해석한다.
- [ ] ProjectGlyphs.txt가 최신 콘텐츠를 포함한다.
- [ ] 필수 역할 글리프 검사가 통과한다.
- [ ] TMP 기본 폰트가 Pretendard Medium이다.
- [ ] UI Basic Scene에 Liberation Sans 폰트 GUID가 없다.
- [ ] 런타임 UI가 역할 API를 사용한다.
- [ ] 특수 폰트가 제한된 맥락에만 사용된다.
- [ ] 전체 EditMode 테스트가 통과한다.
- [ ] 전체 PlayMode 테스트가 통과한다.
- [ ] Windows 플레이어 빌드가 성공한다.
- [ ] OS 폰트가 없는 환경에서도 한국어가 표시된다.
- [ ] Git LFS 객체가 원격에 업로드됐다.

## 13. 라이선스와 배포

반입한 모든 폰트는 OFL 1.1 조건으로 관리합니다.
라이선스 전문은 `Assets/_Project/Fonts/Licenses`에 둡니다.
게임 빌드에 폰트를 임베딩해 재배포할 수 있습니다.
폰트 파일 자체를 단독 상품으로 판매하지 않습니다.
예약 폰트 이름과 수정본 이름 조건은 각 고지를 확인합니다.

폰트를 업데이트하거나 교체할 때는 다음을 함께 변경합니다.

- 정적 TTF 원본
- 대응하는 OFL 또는 LICENSE 파일
- `TypographyCatalogBuilder` 사양
- TMP 생성 자산
- 역할 및 폴백 테스트
- 이 운영 문서의 폰트 목록

## 14. 문제 해결

### 한글이 네모로 표시될 때

1. 해당 문자가 `ProjectGlyphs.txt`에 있는지 확인합니다.
2. `Prepare Project Glyphs`를 실행합니다.
3. `Validate Release Glyphs`를 실행합니다.
4. 기술 역할이면 Pretendard 폴백 연결을 확인합니다.

### 폰트가 역할과 다를 때

1. `TypographyCatalog.asset`의 역할 연결을 확인합니다.
2. UI가 폰트를 직접 대입하는지 검색합니다.
3. 재사용 라벨에서 역할을 매번 갱신하는지 확인합니다.
4. 화면 전용 Typography 정책 호출 순서를 확인합니다.

### TMP 자산 diff가 매우 클 때

1. 코퍼스가 예상치 않게 넓어지지 않았는지 확인합니다.
2. Editor, Tests, Fonts 경로가 제외됐는지 확인합니다.
3. 인코딩 손상 문자가 소스에 들어오지 않았는지 확인합니다.
4. 생성 자산이 Git LFS 포인터로 추적되는지 확인합니다.

### 커스텀 머티리얼이 깨질 때

1. 원본 머티리얼을 삭제하지 않습니다.
2. 머티리얼의 atlas texture가 새 폰트를 가리키는지 확인합니다.
3. 씬 마이그레이션 테스트를 실행합니다.
4. 실제 씬 렌더링을 캡처해 외곽선과 그림자를 비교합니다.

## 15. 변경 리뷰 기준

타이포그래피 PR에서는 다음 순서로 리뷰합니다.

1. 역할 선택이 콘텐츠 의미와 맞는가
2. 일반 UI에 특수 폰트가 섞이지 않았는가
3. 폴백이 한국어를 안정적으로 표시하는가
4. 재사용 UI가 이전 역할을 유지하지 않는가
5. 글리프 코퍼스와 TMP 자산이 함께 갱신됐는가
6. 해상도별 넘침과 줄바꿈이 안전한가
7. 라이선스 고지가 누락되지 않았는가
8. 생성 자산이 Git LFS로 관리되는가

역할을 새로 추가하려면 기존 역할로 표현할 수 없는 이유를 기록합니다.
폰트 패밀리를 추가하려면 기본 세 패밀리로 해결할 수 없는 이유를 기록합니다.
특수 폰트 사용 범위를 넓히는 변경은 화면 캡처와 함께 리뷰합니다.

이 문서를 폰트 파이프라인의 운영 계약으로 취급합니다.
