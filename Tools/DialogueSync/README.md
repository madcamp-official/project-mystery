# 대사집 동기화 도구

공식 XLSX에서 Unity가 읽는 CSV 세 개를 결정론적으로 생성한다. Python 외부 패키지는
필요하지 않으며 Python 3.10 이상을 권장한다.

```powershell
python Tools/DialogueSync/export_dialogue.py
python -m unittest Tools/DialogueSync/test_export_dialogue.py
```

생성 파일:

- `Assets/_Project/Content/Dialogue/Under_the_Horizon_Dialogue_KR.csv`
- `Assets/_Project/Content/Dialogue/Under_the_Horizon_Choices_KR.csv`
- `Assets/_Project/Content/Dialogue/Under_the_Horizon_Scene_Index_KR.csv`

모든 CSV는 UTF-8 BOM과 LF 줄바꿈을 사용한다. 도구는 원본 XLSX의 SHA-256을 먼저
검사하고 다음 조건이 맞지 않으면 생성에 실패한다.

- `Dialogue_Master` 데이터 1,063개
- 고유한 `line_id` 1,063개
- `Scene_Index` 장면 41개
- 고유한 `scene_id` 41개
- `Choice_Flow` 선택지 90개
- 고유한 `choice_id` 90개
- 대사와 선택지가 참조하는 장면이 모두 Scene Index에 존재

원본을 개정했다면 `Documentation/Source/sources.json`의 해시와 기대값을 의도적으로
갱신한 뒤 다시 실행한다. 생성 CSV만 수정한 변경은 원본과 재동기화할 때 사라진다.
