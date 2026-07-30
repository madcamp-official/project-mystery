# Atrium (D1-01) Click-to-Talk Redesign

## Problem

D1-01 ("아트리움") is the only investigation scene in the game where the player
interviews four suspects (Claire, Marcus, Helena, Owen) inside a single modal
dialogue session. The four "talk to X" options are rendered as a repeating
choice-button menu inside the dialogue box (CSV `BranchGroup` = `D1-01_FREE`),
and the player never leaves that fullscreen dialogue box until all four are
interviewed and the closing lines play.

Every other investigation scene with a single suspect (P-03, D3-03, D2-03,
D4-04, D5-03, D6-04, D7-04, D8-02, ...) instead has the player click directly
on that character's sprite in the exploration background to start their
conversation (`AmbientCharacterHotspotOverlay` + `ScenePresencePresentationPolicy`
focus-participant flag → `DialogueController.StartProductionScene`). D1-01
should follow the same pattern: no choice-menu, walk up and click each person.

The player must also not be able to leave the Atrium location (via the map)
until all four have been interviewed. Attempting to do so should not silently
fail or show a generic toast — it should show an in-character monologue line
from Adrian.

## Scope

Only D1-01 currently uses a repeatable (`_FREE`) multi-target choice group —
confirmed by grepping the dialogue CSV. No other scene is affected by this
change. The mechanism is built generically (keyed off the existing `_FREE`
BranchGroup convention and a small location→scene lookup table) so a future
scene could reuse it by adding data, but no other scene needs to be touched
today.

## Design

### 1. Suspend-to-exploration on repeatable choice

`ProductionDialogueFlow` already fully tracks which "_FREE" choices are
resolved (`resolvedChoiceIds`, `IsChoiceResolved`) and already recomputes the
correct remaining `Choices` list after a restore. Nothing about that state
machine needs to change.

What changes is `DialogueController.RenderProduction()`: today, whenever
`productionFlow.IsAwaitingChoice` is true, it renders the choice buttons.
Going forward, if the awaiting choice block belongs to a repeatable group
(the same condition `ProductionDialogueFlow` already uses internally to
decide repeatability), the controller instead does the equivalent of
`EndDialogue()` — saves the checkpoint, hides the dialogue panel, sets
`IsBusy = false` — returning control to exploration instead of drawing
buttons. Ordinary (non-repeatable) in-conversation choices elsewhere in the
game are unaffected and continue to render as buttons exactly as before.

`ProductionDialogueFlow` needs to expose whether the current awaiting block is
repeatable (a simple `bool IsAwaitingWorldSelection` getter over the existing
private `repeatableChoiceStart >= 0` state) so `DialogueController` can branch
on it without duplicating the repeatability check.

### 2. Click a character → resolve their specific branch

`ProductionDialogueFlow` gets one new method:

```csharp
public bool SelectFreeChoiceForCharacter(string characterId)
```

It scans `Choices` for one whose `ChoiceId` ends with `"_" + characterId`
(the CSV already authors `D1-01_CLAIRE`, `D1-01_MARCUS`, `D1-01_HELENA`,
`D1-01_OWEN` — this convention is reused, not invented) and calls the
existing `SelectChoice(index)` on it. Returns false if no match (already
resolved, or not currently awaiting choices).

`DialogueController` gets one new entry point:

```csharp
public bool TalkToWorldCharacter(string sceneId, string characterId)
```

Behavior:
- If a saved `DialogueCheckpoint` exists for `sceneId`, restore it, then call
  `SelectFreeChoiceForCharacter(characterId)`. If that succeeds, render
  normally (their branch lines play in the dialogue box). If it fails (this
  character was already interviewed), end the dialogue immediately and let
  the caller fall back to an ambient "이미 다 말씀드렸습니다" line — the same
  fallback pattern already used by `AmbientCharacterHotspotOverlay` for
  completed NPC interactions.
- Else if no checkpoint exists yet and `CanStartProductionScene(sceneId)` is
  true, start the scene (plays the intro narration/tutorial lines normally),
  then apply the same `SelectFreeChoiceForCharacter` once the flow reaches the
  awaiting-choice state, so the player's initiating click is honored instead
  of requiring a second click.
- Else return false (no-op; existing callers already handle this case for
  ordinary single-suspect scenes).

This is a strict superset of what `AmbientCharacterHotspotOverlay
.StartMainCharacterDialogue` already does for a focus participant — for
scenes with no `_FREE` group, `SelectFreeChoiceForCharacter` is simply never
reached/never matches, so P-03/D3-03/D8-02/etc. behave exactly as they do
today.

### 3. Fix the focus-participant list for D1-01

`ScenePresencePresentationPolicy.ScenePriorities["D1-01"]` is currently
`("EVELYN", "MARCUS", "HELENA")`. Evelyn is never physically at ATRIUM in
this scene (her location for D1-01 is GANGWAY), so listing her there is
inert. Claire and Owen are missing, which today means clicking them (once
they're exploration-clickable) would *not* count as "focus participants" and
would fall through to the generic ambient one-off line system instead of
their real interrogation branch.

Change to `("CLAIRE", "MARCUS", "HELENA", "OWEN")` — the four suspects at the
Atrium, matching `ProductionObjectiveCatalog`'s existing D1-01 objective steps
("클레어와 이야기하기" / "마커스와 이야기하기" / "헬레나와 이야기하기" /
"오웬과 이야기하기"), which were already authored for exactly this
click-per-character interaction model.

### 4. Visual completion state

After a successful `SelectFreeChoiceForCharacter`, the caller
(`AmbientCharacterHotspotOverlay`) records `state.RecordCompletedNpcInteraction`
for that character's interaction id — the same call the existing ambient-line
paths already make. `RefreshCompletionPresentation` already reads this to
grey out completed characters, so no changes are needed there.

### 5. Block travel until all four are interviewed

A small lookup, alongside `SceneTravelPolicy`'s existing data tables:

```csharp
private static readonly IReadOnlyDictionary<string, string>
    LocationInvestigationGate = new Dictionary<string, string>(StringComparer.Ordinal)
    {
        ["ATRIUM"] = "D1-01"
    };
```

`MapController.SelectLocation` (the single known travel entry point — no
other in-scene exit hotspot exists at the Atrium) checks, before evaluating
the travel request: if the player's current location has an entry in this
table and `GameStateManager.HasCompletedScene(requiredSceneId)` is false, the
travel is refused. Instead of the normal toast feedback, it calls
`DialogueController.Instance.StartAmbientLine("ADRIAN", line, "internal")`
where `line` is chosen at random, every time, from:

- "다른 사람의 이야기도 들어보자."
- "아직은 더 탐문을 할 때야."

(Confirmed with the user: reuse these two lines verbatim, picked randomly
each attempt — no per-remaining-count variants.)

## Error handling / edge cases

- Restoring a save mid-Atrium-interview must land back in the same
  suspended-to-exploration state. Since suspension reuses the existing
  checkpoint mechanism verbatim, `ProductionSceneDirector.ResumeGame()` /
  `RestoreProductionScene` already handle this without changes — the
  restored flow will immediately re-enter the awaiting-choice state, which
  `RenderProduction` will suspend-to-exploration on load, same as any other
  suspend.
- Re-clicking an already-interviewed character mid-scene (before all four are
  done) shows the "already told you" ambient line rather than replaying
  their branch or restarting the scene.
- The four can be clicked in any order (matches the current unordered
  `_FREE` design).
- The closing beats (D1-01_027 monologue, D1-01_028 system unlock) are
  unchanged — they already auto-play once no repeatable choices remain,
  and that path never renders as a button menu today either.

## Testing

Extend existing EditMode coverage rather than add new test files where a
current one already targets this behavior:
- `ProductionInvestigationFlowSmokeTests` / a new focused test: clicking all
  four Atrium characters in arbitrary order completes D1-01 and unlocks
  D1-02, same as the current choice-menu path does today.
- `SceneMainCharacterHotspotPolicyTests`: D1-01's focus participants are now
  CLAIRE/MARCUS/HELENA/OWEN, not EVELYN/MARCUS/HELENA.
- New coverage for `ProductionDialogueFlow.SelectFreeChoiceForCharacter`:
  matches the right choice by suffix, returns false when already resolved or
  not awaiting.
- New coverage for the travel gate: traveling away from ATRIUM is denied
  while D1-01 is incomplete and allowed once it's completed.
