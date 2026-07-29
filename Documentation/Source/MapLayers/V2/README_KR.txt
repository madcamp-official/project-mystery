Under the Horizon - 맵 에셋 v2 (Base + Restricted)

최종 에셋
- Deck07_Base.png / Deck07_Restricted.png
- Deck08_Base.png / Deck08_Restricted.png
- Deck09_Base.png / Deck09_Restricted.png
- Deck10_Base.png / Deck10_Restricted.png

검수 자료
- Deck07~10_Composite_QA.png: Base와 Restricted 실제 합성 결과
- Map_Assets_v2_QA_Contact_Sheet.jpg: 전 층 Base/Alpha/Composite 비교
- Map_Assets_v2_QA_Report.json: 해상도, 알파, 공개 장소 비침범, 합성 일치 자동 검증
- Map_Assets_v2_Manifest.json: 파일 정보

적용 방법
1. Base와 Restricted를 같은 RectTransform에 배치합니다.
2. Anchor, Pivot, Preserve Aspect 값을 동일하게 설정합니다.
3. Restricted Image를 Base Image보다 위에 둡니다.
4. Restricted는 RGBA 투명 PNG이며, 별도의 검은 배경을 사용하지 않습니다.
5. 이번 패키지는 계획의 1단계(Base 정상화)와 2단계(벽선 기반 Restricted 에셋)까지만 포함합니다.

Unity 권장 임포트
- Texture Type: Default
- Alpha Source: Input Texture Alpha
- Alpha Is Transparency: On
- Wrap Mode: Clamp
- Filter Mode: Bilinear
- Compression: None 또는 High Quality
