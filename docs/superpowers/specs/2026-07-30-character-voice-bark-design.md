# Character Voice Bark & Story Recording Playback

## Problem

Dialogue lines currently render as text only. The team recorded short
per-emotion reaction clips ("barks") for the 9 main characters plus a
handful of full-sentence "story recording" clips for specific scenes, per
`Under_the_Horizon_Voice_Bark_Master_Plan_KR.xlsx`
(`Assets/_Project/Resources/SoundEffect/Dubbing/`). Nothing plays them yet.
Delivered assets, confirmed by listing the folder:

- `daniel/`, `claire/`, `helena/`, `adrian/`, `richard/`, `thomas/`,
  `owen/` — each has (nearly) full 12-cue coverage, `.mp3` (one `.m4a` in
  `adrian/`). Filename casing is inconsistent (`claire_greet_01.mp3` vs
  `DanielMercer_GREET_01.mp3`) — matching must not depend on exact casing
  or prefix format.
- `evelyn/`, `marcus/` — **no folder exists**. Per the user, wire the
  system so these just silently have nothing to play; drop files in later
  and they work with no code change.
- `story_recording/` — `D2-06_DANIEL_CHAT_01.mp3`, `D5-03_DANIEL_CHAT_01.mp3`,
  `D7-03_JULIAN_01..04.mp3`, `D7-03_THOMAS_01.mp3`. Everything else the
  xlsx's `Story_Recordings` sheet lists (D1-06 dying message, D2-06/D5-03
  anonymous-chat lines, D4-01 phone message, D7-03's `EVELYN_RECORD`
  lines) has no file yet — same silent-skip requirement.

The xlsx's own `Sources` sheet says it was written by inspecting this
repo's `AmbientWorldCharacterCatalog.cs` / `ScenePresenceCatalog.cs` /
`DialogueLine.cs` directly, so its terminology matches real code — but two
things in it don't match current reality and this design does **not**
follow the xlsx where it disagrees with what's actually shipped:

1. **Folder path.** Plan says `Resources/VoiceBarks/<SPEAKER_ID>/`. Actual
   delivery is `Resources/SoundEffect/Dubbing/<lowercase_name>/`. This
   design uses the real path.
2. **`D7-03` content.** The xlsx's 8-entry `Story_Recordings` table
   (JULIAN×4, EVELYN×3, THOMAS×1) does not match the 4 segments already
   hardcoded in `OrpheusRecordCatalog.Segments`
   (`Assets/_Project/Code/Puzzles/OrpheusAudioRestoration.cs:41-63`:
   JULIAN, EVELYN, JULIAN, **RICHARD** — different transcript on the 4th
   line than the xlsx's 4th JULIAN line) — nor do either match the 5
   actually-delivered filenames (`JULIAN_01..04`, `THOMAS_01`, no EVELYN
   file at all). Per the user: **do not touch `OrpheusRecordCatalog`'s
   content in this change** — that's a puzzle-content decision for
   whoever owns the story, not a wiring bug. This design only fixes the
   resource-loading bug that stops any clip from ever being found (see
   Component 4) and matches by speaker identity, not by hardcoding which
   specific file goes with which of the 4 existing segments.

## Scope

In scope: the 9 main characters' generic emotion barks, the 6 confirmed
story-recording line slots (D1-06, D2-06×2, D4-01, D5-03×2) via a new
lightweight catalog, and fixing `D7-03`'s existing (but non-functional)
`IOrpheusAudioProvider` wiring. Out of scope: the ambient/NPC bark system
(`AmbientBarkCatalog.cs`, `SceneContextBarkCatalog.cs` — a separate,
already-working system for the 24+ ambient NPC voices the xlsx also
catalogs; no audio was delivered for them and the user's ask is about the
9 named characters). Also out of scope: rewriting `OrpheusRecordCatalog`'s
segment content (see above).

## Design

### 1. `VoiceBarkCatalog` (new, `Wake.Narrative`, pure data/logic)

Maps `PortraitEmotion` (the 4-value enum already used for portraits,
`ProductionDialogueRuntime.cs:8`) to a candidate list of the 12 cue IDs,
built directly from the xlsx `Cue_Definitions` sheet's "감정 매핑" column
(confirmed with the user this table's tags — `neutral, happy, angry,
concerned, focused, dry, defeated, urgent, injured, effort` — are read
through the *existing* `ProductionDialogueRuntime.cs` emotion-tag
dictionary, i.e. those words already resolve to one of the 4
`PortraitEmotion` values today):

| PortraitEmotion | Candidate cues |
|---|---|
| `Neutral` | `ACK_POS`, `SUSPICIOUS` |
| `Positive` | `ACK_POS`, `LAUGH` |
| `Angry` | `ACK_NEG`, `THINK`, `SURPRISED`, `SUSPICIOUS`, `ANNOYED` |
| `Concerned` | `ACK_NEG`, `THINK`, `CONFUSED`, `SURPRISED`, `SIGH`, `WORRIED` |

`GREET` is not in this table — it's selected structurally (new speaker
turn), not from emotion, per Implementation_Rules' "새 화자의 첫 줄은
GREET... 1회 재생". `PAIN_EFFORT` is not reachable from any of the 90
dialogue `Emotion` tags in the current mapping (confirmed by grep — no
tag maps to it), so it has no automatic trigger in this design; the
per-character clips still get scanned/loaded (so they're available), they
just never get auto-selected. Documented as a known gap, not fixed here —
there's no existing "character is injured" signal on `DialogueRecord` to
hang it off.

```csharp
public static class VoiceBarkCatalog
{
    public static IReadOnlyList<string> CandidateCues(PortraitEmotion emotion);
}
```

### 2. `VoiceBarkPlayer` (new, `MonoBehaviour`, lives beside `AudioManager`)

Owns its own `AudioSource` (not `AudioManager.sfxSource` — keeps bark
volume/ducking independent of other one-shot SFX). Character-ID → folder
name is a fixed lowercase map for the 9 speaker IDs the game already uses
(`DANIEL`→`daniel`, `CLAIRE`→`claire`, `HELENA`→`helena`, `ADRIAN`→`adrian`,
`RICHARD`→`richard`, `THOMAS`→`thomas`, `OWEN`→`owen`, `EVELYN`→`evelyn`,
`MARCUS`→`marcus`); the last two folders don't exist yet, which is fine —
see clip lookup below.

Clip lookup, per (character, cue):

```csharp
Resources.LoadAll<AudioClip>($"SoundEffect/Dubbing/{folder}")
    .Where(clip => clip.name.ToUpperInvariant().Contains(cueId))
    .ToArray();
```

Handles both `claire_greet_01` and `DanielMercer_GREET_01` with one rule
(cue ID compared uppercase, filename uppercased for the `Contains` check).
Missing folder → `LoadAll` returns empty → no candidates → play is a
silent no-op. This is the whole "wire it now, files arrive later" story;
no per-character "is this wired yet" flag needed.

Public entry point, called once per rendered dialogue line:

```csharp
public void TryPlayBark(
    string characterId,
    PortraitEmotion emotion,
    bool isNewSpeakerTurn);
```

Selection logic inside:

1. If `isNewSpeakerTurn`: candidate cue is `GREET`. Else: pick randomly
   among `VoiceBarkCatalog.CandidateCues(emotion)`.
2. Resolve clip candidates for (character, cue) as above. If empty, try
   once more with the *previous* cue actually played for this character
   this scene (soft fallback) — if still empty, return without playing
   (silent skip, not an error).
3. Repetition guard: track, per character, the last 3 clip names played.
   Exclude those from the candidate pool before picking (if that empties
   the pool, drop the guard for this one pick rather than blocking
   playback entirely — running out of variants is expected for
   thin-coverage characters).
4. Cooldown: track last-played timestamp per character. If under 1.4s
   since that character's last bark, skip.
5. Coverage gate: `Random.value` against a configurable ratio (default
   0.4, i.e. within the spec's 35–50% band) — skip if it misses. Checked
   *before* steps 2–4 do any file I/O, so a skip is cheap.
6. On play: `AudioManager.Instance.PlaySfx(clip)` +
   `AudioManager.Instance.DuckMusic(0.6f, clip.length)` (new method, see
   Component 5) + record the clip name/timestamp for steps 3–4.

### 3. `StoryRecordingCatalog` (new, `Wake.Narrative`, pure data)

Explicit `StableLineId → resource path` table for the 6 currently-real
slots, keyed off the actual dialogue CSV line IDs already read during
investigation:

| StableLineId | Resource path (under `SoundEffect/Dubbing/story_recording/`) |
|---|---|
| `d1_06_XX` (Daniel's dying message line — confirm exact order via CSV, no file yet) | *(none yet — entry omitted until a file exists)* |
| `d2_06_10` | `D2-06_DANIEL_CHAT_01` |
| `d5_03_10` | `D5-03_DANIEL_CHAT_01` |

(`d2_06_009`/`d2_06_011` `ANON_CHAT` lines and `d5_03_009`/`_011`
`ANON_CHAT` lines, and D4-01's `EVELYN_MESSAGE` line, have no delivered
file — simply not in the table. `TryGet` returning false is the "not
wired yet" state; no separate flag.)

```csharp
public static class StoryRecordingCatalog
{
    public static bool TryGet(string stableLineId, out string resourcePath);
}
```

### 4. `DialogueController.PresentPromptRecord` hook

`Assets/_Project/Code/Narrative/DialogueController.cs:749-768` is the one
place a production dialogue line's text actually gets shown (both normal
advance, via the call at line 875, and checkpoint restore). Add, after the
existing `ShowPortrait(...)` call:

```csharp
if (record.VoiceRequired)
{
    if (speaker.Kind == DialogueSpeakerKind.RecordedVoice)
    {
        if (StoryRecordingCatalog.TryGet(record.StableLineId, out string path))
        {
            AudioClip clip = Resources.Load<AudioClip>(
                $"SoundEffect/Dubbing/story_recording/{path}");
            if (clip != null)
            {
                AudioManager.Instance?.PlaySfx(clip);
                AudioManager.Instance?.DuckMusic(0.5f, clip.length);
            }
        }
    }
    else if (speaker.Kind is DialogueSpeakerKind.Character
                 or DialogueSpeakerKind.Monologue)
    {
        bool isNewSpeakerTurn = speaker.PortraitId != lastBarkSpeakerId;
        lastBarkSpeakerId = speaker.PortraitId;
        voiceBarkPlayer?.TryPlayBark(
            speaker.PortraitId,
            DialoguePresentationMap.GetEmotion(record.Emotion),
            isNewSpeakerTurn);
    }
}
```

`speaker.Kind == RecordedVoice` is exactly the marker
`DialoguePresentationMap.GetSpeaker` already assigns to
`EVELYN_RECORD`/`EVELYN_MESSAGE`/`JULIAN_RECORD`/`THOMAS_RECORD`/
`DANIEL_CHAT`/`ANON_CHAT` (`ProductionDialogueRuntime.cs:177-187`) — no
new speaker-classification logic needed, it's reused as-is. `Monologue` is
the existing `ADRIAN`-internal-narration kind, matching "Adrian 독백만
후보" from the plan. `lastBarkSpeakerId` is one new private field on
`DialogueController` to detect a speaker turn change (reset to empty in
`EndDialogue()`/`BeginProductionPresentation()` alongside the other
per-dialogue-session fields already reset there).

Lines whose `speaker.Kind` is `Narration`, `System`, or `NonPlayer` never
reach either branch — matches "NARRATION/SYSTEM/PLAYER_CHOICE/UI_HINT는
재생하지 않음" (player-choice option lines don't even route through
`PresentPromptRecord` — they're rendered directly from
`productionFlow.Choices[i].TextKo` in the `hasChoices` branch — so they
were never reachable here regardless).

### 5. `AudioManager.DuckMusic` (new method, small addition)

`AudioManager.cs` has no existing "temporarily lower the currently
playing track and restore" primitive — `CrossfadeMusic`'s `mixVolume`
sets the *target* level for a track that's starting, it doesn't animate
one already playing. Add:

```csharp
public void DuckMusic(float duckMultiplier, float holdSeconds)
{
    if (activeMusicSource == null) return;
    StartCoroutine(DuckMusicRoutine(
        Mathf.Clamp01(duckMultiplier), Mathf.Max(0.05f, holdSeconds)));
}

private IEnumerator DuckMusicRoutine(float duckMultiplier, float holdSeconds)
{
    float baseVolume = MusicVolume * currentMusicMix;
    float duckedVolume = baseVolume * duckMultiplier;
    yield return FadeSourceVolume(activeMusicSource, duckedVolume, 0.15f);
    yield return new WaitForSeconds(holdSeconds);
    yield return FadeSourceVolume(activeMusicSource, baseVolume, 0.25f);
}
```

(`FadeSourceVolume` as a small local coroutine helper, same shape as the
existing `FadeMusicSources` in the same file.) Overlapping calls (a bark
firing again before the previous duck finished restoring) just restart
the coroutine at the current volume — acceptable for short bark clips.

### 6. `ResourcesOrpheusAudioProvider` path fix

`Assets/_Project/Code/UI/OrpheusAudioRestorationUIController.cs:13-25`
currently does `Resources.Load<AudioClip>("Audio/Dialogue/" +
normalized_stable_line_id)` — that folder doesn't exist, so this has
never found a clip, ever (confirmed: no `Assets/_Project/Resources/Audio`
folder exists at all). Fix, without touching `OrpheusRecordCatalog`'s
segment content:

```csharp
public bool TryGetClip(string stableLineId, out AudioClip clip)
{
    if (!OrpheusRecordCatalog.TryGet(stableLineId, out OrpheusRecordSegment segment))
    {
        clip = null;
        return false;
    }

    string speakerPrefix = segment.Speaker
        .Replace("_RECORD", string.Empty)
        .Replace("_MESSAGE", string.Empty);
    AudioClip[] candidates = Resources
        .LoadAll<AudioClip>("SoundEffect/Dubbing/story_recording")
        .Where(item => item.name.ToUpperInvariant()
            .Contains(speakerPrefix.ToUpperInvariant()))
        .OrderBy(item => item.name, StringComparer.Ordinal)
        .ToArray();

    // Segment index among same-speaker segments picks which same-speaker
    // file this is (1st JULIAN_RECORD segment -> JULIAN_01, 2nd -> JULIAN_02,
    // ...) - the only pairing that doesn't require guessing which specific
    // take goes with which segment.
    int sameSpeakerIndex = OrpheusRecordCatalog.All
        .Where(item => item.Speaker == segment.Speaker)
        .ToList()
        .IndexOf(segment);

    clip = sameSpeakerIndex >= 0 && sameSpeakerIndex < candidates.Length
        ? candidates[sameSpeakerIndex]
        : null;
    return clip != null;
}
```

For today's 4 segments (JULIAN, EVELYN, JULIAN, RICHARD) against today's 5
files (`JULIAN_01..04`, `THOMAS_01`): both `JULIAN_RECORD` segments match
(1st→`JULIAN_01`, 2nd→`JULIAN_02`), `EVELYN_RECORD` and `RICHARD` find no
same-name file and fall through to the existing subtitle-only fallback
(`OrpheusPlaybackRequest.UsesTranscriptFallback`, already handled by the
puzzle code — no change needed there). `THOMAS_01` stays unused by this
puzzle under option 1, since nothing in the current 4-segment catalog is
a Thomas line — also a known, called-out gap, not fixed here per the
user's choice not to touch catalog content.

## Testing

- `VoiceBarkCatalog`: EditMode test asserting the `PortraitEmotion` →
  cue-list table matches the design table above, and that every cue ID it
  returns is one of the 12 from `Cue_Definitions` (typo guard).
- `VoiceBarkPlayer`: EditMode tests against a fake `Resources.LoadAll`
  seam (constructor-injectable clip lookup function, same pattern as
  `IOrpheusAudioProvider`, so the class isn't hard-coupled to
  `Resources.*` and is unit-testable) covering: repetition guard (3 fake
  clips, 4th pick excludes the first 3), cooldown (two calls under 1.4s
  apart → second is a no-op), coverage gate (seeded `Random` or an
  injectable probability source so the 35–50% check is deterministic in
  tests), missing-folder silent no-op (empty clip list → no exception, no
  play).
- `StoryRecordingCatalog`: EditMode test — every `StableLineId` key
  actually exists in `Under_the_Horizon_Dialogue_KR.csv` (guards against a
  typo'd line id silently never firing).
- `ResourcesOrpheusAudioProvider`: EditMode test for the same-speaker-index
  pairing logic against a fake clip list (no real `Resources.Load`
  needed) — asserts the 1st/2nd JULIAN_RECORD segments get distinct
  clips and EVELYN_RECORD/RICHARD correctly return `false`.
- Manual: Play Mode smoke test through a scene with Daniel/Claire/Adrian
  lines, confirm barks audible, confirm D2-06/D5-03 Daniel-chat lines play
  their recording instead of a generic bark, confirm D7-03 restoration
  still completes normally when a segment has no matching clip.
