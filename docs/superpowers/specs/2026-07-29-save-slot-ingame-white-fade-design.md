# Save Slot → In-Game White Fade Transition

## Background

Selecting a save slot (new game or continue) currently plays the existing
lobby/water "closing" animation in `SaveSlotSelectionController.TransitionRoutine(showing: false)`
(slot rises out, water surfaces, lobby returns - `RiseDuration` 2.2s +
`DiveDuration` 3s = `RevealDuration` 5.2s total), and only once that finishes
does it call `UIManager.StartNewGameInSlot`/`ContinueGameInSlot`, which
synchronously switches to the in-game panel via `ShowIngame()`. That panel
switch is currently an abrupt cut with nothing masking it.

`ScreenFadeTransition` (used today only by `MapController` for travel
transitions) already implements a full-screen cover/reveal overlay
(`CanvasGroup` alpha fade over a solid-color `Image`), just with a
hardcoded dark navy color and an all-in-one `Run(midpoint, ...)` API that
runs a synchronous `Action` between the fade-in and fade-out.

## Goals

- While the existing slot-exit/water-surface/lobby-return closing sequence
  plays (5.2s), a white overlay fades in over the same duration, in sync
  with it.
- Once that sequence finishes and `StartNewGameInSlot`/`ContinueGameInSlot`
  has switched to the in-game panel, the white overlay fades out over 0.4s,
  revealing the in-game screen.
- Applies to both new-game and continue-game slot selection.
- `ScreenFadeTransition` gains reusable `FadeIn(duration, color)` /
  `FadeOut(duration)` methods (returning the `Coroutine` so callers can
  `yield return` them) without changing its existing `Run(...)` behavior
  used by `MapController` (color becomes a parameter there too, defaulted
  to the current dark navy so that call site needs no changes).

## Non-goals

- No changes to the existing slot rise / water-surface / lobby-return
  animation itself - this only adds a white overlay on top of it.
- No changes to the *opening* transition (title → lobby → slot list).
- No configurable color/duration exposed to designers beyond the two call
  sites this spec touches - white and 5.2s/0.4s are hardcoded at the call
  site, matching how the existing animation constants are hardcoded too.

## Design

### `ScreenFadeTransition` changes

- `EnsureOverlay()` keeps its current dark navy default so `Run(...)`'s
  behavior for `MapController` is unchanged.
- New methods:
  - `public Coroutine FadeIn(float duration, Color color)` - ensures the
    overlay exists, sets the blocker's color, activates it, and fades
    `CanvasGroup.alpha` from its current value to `1`.
  - `public Coroutine FadeOut(float duration)` - fades alpha to `0`, then
    deactivates the blocker.
  - Both reuse the existing private `Fade(from, to, duration)` coroutine
    (extended to read the current alpha as `from` rather than always
    starting from a fixed value, since `FadeIn` here starts from whatever
    the overlay already is - `0` in this flow, but this keeps the method
    correct if called again mid-fade).
- `Run(...)` is refactored to call `FadeIn`/`FadeOut` internally so there's
  one fade implementation, not two.

### `SaveSlotSelectionController` changes

In `TransitionRoutine(bool showing)`'s closing branch (`showing == false`):

- At the very start (alongside the existing `StartCoroutine(MoveRect(...))`
  / `MoveWater(...)` calls that already run in parallel), start
  `ScreenFadeTransition.Ensure()?.FadeIn(RevealDuration, Color.white)` -
  fire-and-forget, same pattern as the other parallel coroutines in this
  method.
- Immediately after the existing `ContinueGameInSlot(slot)` /
  `StartNewGameInSlot(slot)` call (which synchronously calls `ShowIngame()`
  as part of its body), add
  `yield return ScreenFadeTransition.Ensure()?.FadeOut(0.4f);`.

### Testing

- No existing test coverage for `SaveSlotSelectionController`'s transition
  coroutines or `ScreenFadeTransition` (both are animation-timing-heavy
  MonoBehaviours with no precedent EditMode/PlayMode tests in this
  codebase - matches how `LobbyBackdropController` and the lobby dive
  animation are untested too). No new tests planned; verified manually in
  the Editor (enter Play, select a save slot, confirm white fade in/out
  around the panel switch, no console errors).

## Open questions for implementation

- None - exact durations (5.2s tied to `RevealDuration`, 0.4s fade-out) and
  color (white) are already fixed by this spec.
