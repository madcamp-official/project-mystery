Under the Horizon 맵 레이어 에셋

각 층별 파일:
- DeckXX_Base.png: 초반 공개용 기본 지도
- DeckXX_Restricted.png: 조사 레이어 폴백 및 제작 기준용 투명 오버레이
- DeckXX_Technical.png: 후반 기술 설비 투명 오버레이
- DeckXX_Preview_AllLayers.png: 세 레이어 합성 확인용

주의:
- Restricted/Technical PNG는 투명 배경입니다.
- 런타임은 Base → 동적 제한영역 → 조사 주석 → Technical → 장소 노드
  순서로 표시합니다.
- Deck 7~10의 제한 상태는 MapAreaCatalog의 개별 폴리곤으로 표시합니다.
  Restricted PNG는 카탈로그가 없는 덱의 호환 폴백과 에디터 미리보기에
  사용합니다. 기존 Unity GUID를 지키기 위해 파일명은 유지합니다.
- Deck 7은 Service & Engineering Deck으로 재구성해 Engine Control과 Ballast 구역을 포함했습니다.
- Deck 6은 Lower Machinery Deck 원본을 보존하지만 현재 층 선택과 런타임
  MapAreaCatalog에서는 비활성 상태입니다.
- 2026-07 V2 교체 원본과 자동 QA 자료는 V2 폴더에 함께 보관합니다.
