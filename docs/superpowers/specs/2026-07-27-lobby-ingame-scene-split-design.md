# Lobby/Ingame Scene Split Design

## Background

`UI Basic Scene` currently holds the entire game in one Unity scene: a single
`Canvas` (recently switched to World Space to render the Bitgem
`StylisedWater` effect in front of UI) with sibling panels (`StartScene`,
`Ingame`, `Map`, `Evidence`, `Settings Popup`) toggled via `GameObject.SetActive`
by a single `UIManager`. There is no `SceneManager.LoadScene` anywhere in the
codebase — everything has always lived in one scene.

World Space Canvas breaks the existing Screen-Space-authored in-game UI
(`Ingame`/`Map`/`Evidence` panels), so lobby (title + water effect) and
in-game UI need genuinely different Canvas render modes. This requires
splitting into two Unity scenes.

## Goals

- `Lobby Scene`: World Space canvas, title screen + water effect + save/load
  slot picker.
- `Ingame Scene`: Screen Space - Overlay canvas, Scale With Screen Size,
  reference resolution 2880x1800, reusing `UI Basic Scene`'s existing
  in-game panel layout/scale as-is.
- Pressing "시작하기" in the Lobby: the title UI panel animates upward and
  off-screen while the save/load slot panel *and* the Water object — both
  starting off-screen below — animate upward in sync, arriving together.
- `UI Basic Scene` itself stays untouched; both new scenes are built by
  duplicating it and pruning to only what's needed.

## Non-goals

- Not touching `UI Basic Scene` or its existing EditMode/PlayMode tests in
  this pass (tests keep pointing at `UI Basic Scene`; retargeting them to the
  new scenes is a follow-up, not part of this work).
- Not making the save/load slot background see-through — it keeps its
  existing near-opaque look. Water is only meant to be visible during the
  reveal motion, not necessarily after the slot picker is fully in place.
- No fallback/retry handling for a failed scene load mid-transition.

## Architecture

### Persistent bootstrap layer (new)

Today all of `GameStateManager`, `GameFlow`, `AudioManager`,
`DialogueDatabase`, `DialogueController`, `EvidenceInventory`,
`EvidencePanelController`, `UIManager`, `SettingsController`,
`ToastController`, `MapController`, `ClickRouter`, `ExitPuzzle`, and
`LocationLoader` live as components on one `GameSystems` GameObject in the
single scene. Splitting into two scenes with a `LoadSceneMode.Single`
transition would destroy all of them when Lobby unloads — breaking save
slot selection and game state.

Decision: split `GameSystems` into two groups:

- **Persistent/data services** (must survive the Lobby → Ingame scene load):
  `GameStateManager`, `GameFlow`, `AudioManager`, `DialogueDatabase`,
  `DialogueController`, `EvidenceInventory`. These move to a small
  `Bootstrap` scene (Build Settings index 0) that marks its root
  `DontDestroyOnLoad` and then loads `Lobby Scene` additively on start.
- **UI-bound components** (scene-local, since each scene has its own
  Canvas): `EvidencePanelController`, `SettingsController`,
  `ToastController`, `MapController`, `ClickRouter`, `ExitPuzzle`,
  `LocationLoader`, plus the split `UIManager` (below). These live directly
  in whichever scene needs them.

### UIManager split

`UIManager.cs` currently binds one `Canvas` and owns both the start-screen
flow and all in-game panel flow. It splits into:

- `LobbyUIManager` (Lobby Scene only): binds the World Space Canvas's
  `StartScene` panel, owns `SaveSlotSelectionController` +
  `TitleScreenPresentationController` wiring, and owns the new reveal
  transition. On slot confirm, it calls
  `SceneManager.LoadScene("Ingame Scene", LoadSceneMode.Single)` instead of
  `ShowIngame()`.
- `IngameUIManager` (Ingame Scene only): everything `UIManager` currently
  does for `Ingame`/`Map`/`Evidence`/`Settings Popup`/`Status HUD`, minus all
  start-screen logic. Shows the `Ingame` panel immediately on `Awake`.

All call sites referencing `UIManager.Instance` get audited and repointed to
whichever of the two new types applies (e.g. `SaveSlotSelectionController`
calls `LobbyUIManager.Instance`, `Map/Back Btn` calls `IngameUIManager`).

### Lobby Scene

Duplicate of `UI Basic Scene`, pruned to: `Main Camera`, `Global Light 2D`,
`Canvas` (World Space, kept exactly as currently authored: RectTransform
size 2880x1800, `localScale` 0.0056, at world origin), `EventSystem`,
`Water`, and a new `Bootstrap`-facing `LobbyUIManager`. Removed:
`Ingame`/`Map`/`Evidence`/`Status HUD` panels and their controllers.

New `LobbyRevealSequence` component drives the transition:

- Title UI panel (`StartScene`'s `Title Presentation` content): animates
  `RectTransform.anchoredPosition.y` from `0` to `+1800` (its own local
  units, matching the canvas's authored height).
- A new "Reveal Group" containing the `SaveSlotSelectionController` panel:
  starts at `anchoredPosition.y = -1800`, animates to `0`.
- `Water`: a 3D object, not a RectTransform, so its equivalent travel
  distance is expressed in world units: canvas height in world space is
  `1800 * 0.0056 ≈ 10.08` units. `Water`'s `transform.position.y` animates by
  that same amount, in lockstep (same duration/easing) as the two
  RectTransforms, so the motion reads as one continuous surface.
- Easing/timing matches the existing hand-rolled coroutine style already in
  the codebase (`UiPanelEntranceAnimator`, `EvidenceAcquisitionNoticeController`:
  `SmoothStep` over a fixed duration) rather than introducing a new
  animation dependency (no DOTween in this project).

`SaveSlotSelectionController`'s existing near-opaque overlay is unchanged.

### Ingame Scene

Separate duplicate of `UI Basic Scene`, pruned to: `Ingame`/`Map`/`Evidence`/
`Settings Popup`/`Status HUD` panels and their controllers, plus
`IngameUIManager`. Removed: `StartScene` panel, `SaveSlotSelectionController`,
`TitleScreenPresentationController`, `Water`, World-Space-specific setup.

Canvas: Screen Space - Overlay, `CanvasScaler.uiScaleMode` = Scale With
Screen Size, `referenceResolution` = 2880x1800, `matchWidthOrHeight` = 0.5
(mirrors the current on-disk value), reusing `UI Basic Scene`'s existing
in-game panel layout/scale unchanged.

**Bug fix required**: `RuntimeUiOverhaulController.ConfigureCanvas()`
currently hardcodes `referenceResolution = new Vector2(1920, 1080)` at
runtime, which would silently overwrite the 2880x1800 setting the instant
the scene plays. This gets updated to 2880x1800 to match.

On load, `IngameUIManager.EnsureInitialized()` shows the `Ingame` panel
immediately — no title/start flow here, since the game already started via
the Lobby.

## Data flow / transition sequence

1. `Bootstrap` scene loads first, marks persistent services
   `DontDestroyOnLoad`, then loads `Lobby Scene` additively.
2. Player clicks "시작하기" in Lobby → `LobbyUIManager.OpenSaveSlots()` →
   `LobbyRevealSequence.Play()`.
3. Title panel and (slot-panel + Water) group animate in sync (~0.4–0.5s,
   `SmoothStep`).
4. `SaveSlotSelectionController` behaves as today; player picks a slot →
   `Confirm()`.
5. `GameStateManager.SelectSaveSlot`, `GameFlow.ResetSession/BeginGame` (or
   `ResumeGame`) run unchanged — these now live on the persistent Bootstrap
   layer, so nothing about their behavior changes.
6. `LobbyUIManager` calls `SceneManager.LoadScene("Ingame Scene",
   LoadSceneMode.Single)`.
7. `IngameUIManager.EnsureInitialized()` runs on `Awake`, shows the `Ingame`
   panel immediately.

## Error handling / edge cases

- Both new scenes (`Bootstrap`, `Lobby Scene`, `Ingame Scene`) must be added
  to Build Settings — currently empty, first time this project uses
  multi-scene loading.
- Every `UIManager.Instance` call site gets audited during implementation
  and repointed to `LobbyUIManager` or `IngameUIManager` as appropriate.
- `AudioManager`/settings continuity across the scene boundary: since
  `AudioManager` is now persistent (Bootstrap), `SettingsController`
  (scene-local, duplicated into both Lobby and Ingame scenes) keeps working
  against the same persistent `AudioManager.Instance`.
- No handling added for a failed/aborted scene load mid-transition — out of
  scope for this pass.

## Testing

- Play `Lobby Scene` (via `Bootstrap`) standalone in Editor: title → reveal
  → slot pick → scene switch to `Ingame Scene` works end to end.
- Play `Ingame Scene` standalone (bypassing Lobby, e.g. a temporary debug
  entry point) to confirm Screen-Overlay UI still lays out correctly at
  2880x1800 and scales via `MatchWidthOrHeight` at other resolutions.
- Existing EditMode/PlayMode tests continue to target `UI Basic Scene`
  unchanged; retargeting them to `Lobby Scene`/`Ingame Scene` is explicitly
  out of scope here.
