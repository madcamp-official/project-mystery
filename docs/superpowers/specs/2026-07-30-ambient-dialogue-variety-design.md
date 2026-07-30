# Ambient / World-Line Dialogue Variety

## Problem

Two systems produce the incidental dialogue players hear while exploring —
neither varies with story progress, so lines can read as tonally wrong once
the plot has moved on from them:

- `AmbientBarkCatalog` (`Assets/_Project/Code/Narrative/AmbientBarkCatalog.cs`):
  background NPC flavor lines, one per location, selected via
  `GetAvailable(locationCode, state, sceneId, maximum)`. Almost every entry's
  `Condition` is the literal string `"always"` — the same line (often a light,
  party-adjacent comment) plays whether it's Day 1 during the welcome party or
  Day 8 during the final accusation, after a murder, a public scandal, an
  on-ship arrest, and rising `publicAnxiety`.
- `MainCharacterWorldLineCatalog`
  (`Assets/_Project/Code/Exploration/MainCharacterWorldLineCatalog.cs`):
  the one-line brush-off a main character gives when clicked outside their
  focus scene (or after their scene is done). Each character has exactly one
  fixed line for the "Normal" state, no matter the day — `Injured` and
  `Detained` are the only variation that exists today.

Both systems already have the scaffolding for state-driven text (a condition
string on each bark; a `SceneCharacterState` switch on world lines) — they're
just missing day-based tiers.

## Scope

Only these two catalogs, and the small amount of code needed to let their
data vary by day. No changes to the scripted per-scene dialogue CSV, no
changes to `SceneContextBarkCatalog` (those are already scene-specific and
correctly gated). A full narrative-consistency pass over the 1063-line
scripted story is out of scope — that's a separate, much larger effort the
user has deferred to its own brainstorming session.

## Design

### 1. Generalize `AmbientBarkCatalog.Matches` to parse day ranges

Today `Matches(condition, state, sceneId)` is a chain of exact string
comparisons — `condition == "publicAnxiety>=40 and publicAnxiety<70"` is
matched as one literal string, not evaluated. That doesn't scale to adding
several new day-tier conditions (`chapter>=2 and chapter<=4`,
`chapter>=5`, ...) without hardcoding every combination used.

Replace the body with a small compound-clause evaluator: split the condition
on `" and "`, evaluate each clause independently, require all to pass.
Per-clause evaluation:

- `"always"` → true
- `chapter=N` → `day == N`
- `chapter>=N` → `day >= N`
- `chapter<=N` → `day <= N`
- `publicAnxiety>=N` / `publicAnxiety<N` → as today
- `flag:X` → as today (only valid as a whole condition, not inside an `and`
  clause list, same as today)
- `scene=X` → as today (same restriction)

Note: today's three literal chapter strings are written as
`"chapter=Day2"` / `"chapter>=Day2"` / `"chapter=Day5"` (a `Day` prefix on
the number). The new parser takes a bare integer (`"chapter=2"`); as part of
this change those three existing entries in `AmbientBarkCatalog` get
rewritten to the bare-integer form so one parser handles every entry —
`SceneContextBarkCatalog` is untouched since it only ever uses `scene=`
conditions, never `chapter=`. This is additive otherwise: every other
existing condition string (`"always"`, `"publicAnxiety>=70"`, `"scene=D8-01"`,
`flag:` entries) continues to evaluate identically. `ConditionPriority`
already ranks any
`chapter`/`flag`/`scene` clause above `"always"`, so a day-tier bark
automatically outranks the Day-1 default once its range matches — no changes
needed there. Because tiers are written as non-overlapping ranges
(`chapter>=2 and chapter<=4`, `chapter>=5`), exactly one tier (plus any
`"always"` fallback) is ever eligible for a given speaker on a given day, so
`GetAvailable`'s per-speaker dedup can't randomly pick between two
simultaneously-valid tiers for the same NPC.

### 2. Add Day 2-4 / Day 5-8 bark tiers per location

For each of the 25 locations in `AmbientBarkCatalog`, keep the existing
`"always"` line(s) as the Day-1 baseline and add:

- one `chapter>=2 and chapter<=4` line reflecting early investigation mood
  (subdued, aware something happened, procedural)
- one `chapter>=5` line reflecting the later chapters (public scandal is out,
  an on-ship arrest has happened by D8, anxiety has likely escalated)

Locations that already have a specific `chapter=Day2` or `chapter>=Day2`
entry (e.g. `ATRIUM_MEDBAY_RUMOR`, `VIP_ROBOT`, `HORIZON_CLOSED`) keep that
entry as part of the relevant tier rather than getting a duplicate. Locations
that are structurally crew/technical spaces with already-neutral,
procedural tone (e.g. `ENGINE_CONTROL`, `WORKSHOP`) get lighter-touch
tier lines — the point is removing tonal *mismatch*, not rewriting content
that already reads fine at any day.

Example (Ballroom, currently two `"always"` lines about rehearsal and seat
maps):

```
B("BALLROOM_SINGER", "BALLROOM_MUSICIAN", "...", "light", "always", "BALLROOM"),
B("BALLROOM_SINGER_LATE", "BALLROOM_MUSICIAN",
    "무대는 치웠습니다. 오늘은 음악보다 통제선이 먼저 보이네요.",
    "subdued", "chapter>=2 and chapter<=4", "BALLROOM"),
B("BALLROOM_SINGER_FINALE", "BALLROOM_MUSICIAN",
    "다들 이 방보다 회의실 쪽 소식을 더 궁금해합니다.",
    "uneasy", "chapter>=5", "BALLROOM"),
```

### 3. Add day tiers to `MainCharacterWorldLineCatalog`

`Get` and `GetCompleted` gain a `day` parameter:

```csharp
public static string Get(string characterId, SceneCharacterState state, int day)
public static string GetCompleted(string characterId, SceneCharacterState state, int day)
```

`Injured`/`Detained` overrides stay first and unchanged (they already
correctly override any day-based line). For `Normal` state, each of the
eight living-and-not-yet-detained main characters gets three lines instead
of one, keyed by the same day bands as the barks (`day <= 1`,
`2 <= day <= 4`, `day >= 5`), written to track what that character has
actually been through by then (per `SceneContextBarkCatalog`'s existing
per-scene beats for tone reference — e.g. Marcus's Day 4 accident, Evelyn's
Day 7 approach to the detective, Richard's Day 3 confrontation over the
cover-up). Daniel is excluded from the day-tier work: he's already filtered
out of world-character rendering entirely once deceased
(`ScenePresencePresentationPolicy.IsPresentable`), so his line is only ever
seen on Day 1 and stays as-is.

The one call site, `AmbientCharacterHotspotOverlay.StartMainCharacterDialogue`
(and its two `MainCharacterWorldLineCatalog.Get/GetCompleted` calls), passes
`Wake.Core.GameStateManager.Instance.Day` (already resolved as `state` in
that method).

## Testing

- New EditMode coverage for the generalized `Matches` parser: each clause
  type in isolation, an `and`-compound of two clauses, and confirmation that
  the existing hardcoded condition strings still behave identically
  (regression safety net before touching the parser).
- New EditMode coverage asserting, for every location in
  `AmbientBarkCatalog.SupportedLocations`, that `GetAvailable` returns a
  non-empty result at day 1, at a day in the 2-4 band, and at a day in the
  5-8 band — catches any location left with only an `"always"` line, and
  catches any authored day-range typo that leaves a day with zero eligible
  barks for every NPC at that location.
- New EditMode coverage for `MainCharacterWorldLineCatalog.Get`/
  `GetCompleted`: for every character other than Daniel, the three day bands
  return three distinct strings; `Injured`/`Detained` still short-circuit
  regardless of day (regression check on existing behavior).
