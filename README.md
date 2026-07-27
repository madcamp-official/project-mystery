# Under the Horizon

## Unity 에셋 메타 관리

- 프로젝트는 `6000.3.20f1` 버전의 Unity Editor를 기준으로 관리합니다.
- 에셋이나 폴더 이름은 가능하면 Unity Project 창에서 변경합니다.
- 파일 시스템이나 Git에서 이름을 변경할 때는 원본과 `.meta`를 `git mv`로 함께 이동합니다.
- `.meta` 파일을 새로 만들거나 GUID를 직접 수정하지 않습니다.
- 커밋 전 Unity의 에셋 새로고침을 완료하고 `git status`에 예상하지 않은 재직렬화가 없는지 확인합니다.
- `Wake/Production/Run Content Preflight`로 누락 메타, 고아 메타, GUID 누락 및 중복을 검사합니다.

몰입캠프 26s-w4-c2-01의 2D 추리 어드벤처 Unity 프로젝트입니다.

게임 구현의 공식 기준 문서와 최신 완성 대사집은
[`Documentation`](Documentation/README.md)에서 관리합니다.

현재 플레이 진입점은 `Assets/_Project/Scenes/UI/UI Basic Scene.unity`입니다.
