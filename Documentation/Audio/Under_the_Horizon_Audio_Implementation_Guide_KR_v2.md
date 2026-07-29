# Under the Horizon — Audio Implementation Guide v2.0

기준: main branch, 신규 오디오 커밋 `4287b0b` (2026-07-29)

## 신규 리소스

- `SoundEffect/The_sound_of_an_iron_door_knocking`
- `SoundEffect/The_sound_of_an_iron_door_opening_and_closing`
- `SoundEffect/boat_engine_sound`
- `SoundEffect/factory_exhaust_fan_sound`
- `SoundEffect/wind_noise`

## 바뀐 기본 매핑

- PORT / PROMENADE / OPEN_DECK: `wind_noise + waves`
- GANGWAY: `boat_engine + interior crowd`
- ENGINE_CONTROL: `factory exhaust fan + boat engine`
- SERVICE_RAIL: `factory exhaust fan`
- BALLAST_CONTROL_ANNEX: `factory exhaust fan + low boat engine`
- VAULT / SERVICE_RAIL / BALLAST: 철문 노크·개폐 이벤트

## 적용 우선순위

`Event/Cutscene > Scene > Location Default > Keep Current`

## 권장 AudioSource 구성

```text
Music A
Music B
Ambience A
Ambience B
SFX
Voice
```

현재 `AudioManager.cs`는 `musicSource`와 `sfxSource`만 직렬화하므로 다음 확장이 필요하다.

```csharp
[SerializeField] private AudioSource musicSourceA;
[SerializeField] private AudioSource musicSourceB;
[SerializeField] private AudioSource ambienceSourceA;
[SerializeField] private AudioSource ambienceSourceB;
[SerializeField] private AudioSource sfxSource;
[SerializeField] private AudioSource voiceSource;
```

## 상태 적용 의사 코드

```csharp
void ApplyAudioContext(string locationCode, string sceneId, string situationState)
{
    AudioCue cue =
        FindEventOverride(sceneId, situationState) ??
        FindSceneCue(sceneId, situationState) ??
        FindLocationDefault(locationCode);

    CrossfadeMusic(cue.PrimaryBgm, cue.CrossfadeSec, cue.BgmVolume);
    FadeAmbience(0, cue.PrimaryAmbience, cue.PrimaryAmbienceVolume);
    FadeAmbience(1, cue.SecondaryAmbience, cue.SecondaryAmbienceVolume);
}
```

## QA 필수 항목

1. 신규 5개 mp3의 Unity `.meta` 생성 및 커밋 여부 확인.
2. `boat_engine_sound`와 BGM의 저역 충돌 확인.
3. `factory_exhaust_fan_sound`의 루프 이음새 확인.
4. `wind_noise + waves` 동시 재생 시 대화 가독성 확인.
5. 철문 열기/닫기가 한 파일이라면 각각의 편집본 생성 여부 결정.
6. 일반 객실문에 철문 SFX가 재생되지 않는지 확인.

소스:
- https://github.com/madcamp-official/project-mystery/commit/4287b0b0b97a28d7655b3d94fd9723756baba9a8
- https://github.com/madcamp-official/project-mystery/blob/main/Assets/_Project/Code/Core/AudioManager.cs
