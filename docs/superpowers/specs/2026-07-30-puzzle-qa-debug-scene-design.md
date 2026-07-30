# Puzzle QA Debug Scene

## Problem

QA has no way to jump straight into any of the 8 production puzzles to test
them. Today the only path is playing the real dialogue flow from the start
of the case up to that puzzle's scene, every time. There are 8 puzzle
interactions total, registered in `ProductionSceneCompletionCatalog.All`:

| Scene | Interaction | Controller |
|---|---|---|
| D2-01 | `exit_inspection` | `ExitInspectionUIController` |
| D2-02 | `blood_pattern` | `BloodDirectionPuzzleUIController` |
| D2-04 | `camera_blind_spot` | `CameraBlindSpotUIController` |
| D4-04 | `marcus_interrogation` | `MarcusInterrogationUIController` |
| D6-02 | `cargo_rail_branch` | `ProductionPuzzleUIController` |
| D6-05 | `timeline_12_cards` | `TimelinePuzzleUIController` |
| D7-03 | `orpheus_audio_restoration` | `OrpheusAudioRestorationUIController` |
| D8-01 | `final_accusation` | `FinalAccusationUIController` |

Confirmed by reading every controller's `Open()`: all 8 gate on exactly one
check, `ProductionSceneCompletionGate.CanStartInteraction(state, sceneId,
interactionId)`, which is satisfied as long as that scene isn't already
marked completed and no completed `PuzzleSessionState` exists for that
interaction id. There is no additional bespoke precondition at open time —
evidence requirements (e.g. `cargo_rail_branch` needs C-08/C-09/C-10) are
only checked at submit/`TryComplete()` time, not at `Open()`.

All 8 controllers build their own UI procedurally in `Awake()`
(`BuildUi()`), so they are not wired into "UI Basic Scene"'s authored
hierarchy beyond expecting to live under its `Canvas/Ingame` panel
(`UIManager.EnsureRuntimeControllers` adds them as components there). There
is exactly one gameplay scene in the project,
`Assets/_Project/Scenes/UI/UI Basic Scene.unity` — everything (dialogue,
evidence, puzzles) runs inside it.

**Save-slot constraint found during design:** `GameStateSaveStore.SelectSlot`
clamps to `[1, 3]` — there is no free "QA-only" slot, and `SelectSaveSlot` +
`StartNewGame()` calls `GameStateSaveStore.ClearAll()`, which wipes whichever
of the 3 real slots is currently active (PlayerPrefs, local to this
machine). The debug scene must never call this. Instead it only resets the
one puzzle being opened, on whatever slot is already active, and leaves
everything else untouched.

## Scope

- New scene: `Assets/_Project/Scenes/Debug/PuzzleQA.unity`. Not added to
  `EditorBuildSettings` — stays out of Player builds by omission.
- New script: `Assets/_Project/Code/Debug/PuzzleQaDebugController.cs`.
  Entire file wrapped in `#if UNITY_EDITOR` / `#endif` as a second,
  independent guard against ever shipping in a build.
- One small new method on `GameStateManager`, `#if UNITY_EDITOR`-guarded,
  to reset a single scene's completion + puzzle-session state without
  touching anything else (no existing public API does this — every
  existing mutator only *sets* flags/completion, never clears them).
- No changes to any of the 8 puzzle controllers or
  `ProductionSceneCompletionGate` — they're reused exactly as they are.

## Design

### Scene bootstrap

`PuzzleQA.unity` contains one GameObject with `PuzzleQaDebugController`.
On `Start()`:

1. `SceneManager.LoadSceneAsync("Assets/_Project/Scenes/UI/UI Basic Scene",
   LoadSceneMode.Additive)` — brings in the real game scene (and with it
   `GameStateManager`, `EvidenceInventory`, `UIManager`, `Canvas`, camera,
   event system) exactly as a normal play session would boot it, on
   whatever save slot was already active (defaults to slot 1).
2. Poll until `GameStateManager.Instance` and `UIManager.Instance` are
   non-null and `UIManager.Instance.IsInitialized`.
3. Call `UIManager.Instance.ShowIngame()` so the `Ingame` panel (which
   hosts all 8 runtime modal controllers) is active.
4. Build a small always-on-top picker `Canvas` (`sortingOrder` above
   everything else) listing the 8 entries from
   `ProductionSceneCompletionCatalog.All`, one button each, labelled with
   `SceneId` + `InteractionId`. Plain uGUI + TMP, no art — this never
   ships, so it doesn't need to match the game's visual theme.
5. A persistent small "QA 메뉴" button stays visible in a corner so the
   picker can be reopened after closing a puzzle.

### Opening a puzzle

`OpenPuzzle(ProductionSceneCompletionRequirement requirement)`:

1. `GameStateManager.Instance.DebugResetPuzzle(requirement.SceneId,
   requirement.InteractionId)` (new method, see below) — makes sure
   `CanStartInteraction` will pass regardless of prior state, without
   touching any other scene/puzzle/flag.
2. If `requirement.InteractionId` is `blood_pattern` or
   `cargo_rail_branch`: look it up in `ProductionPuzzleCatalog`, loop
   `RequiredEvidenceIds`, call `EvidenceInventory.Instance.TryAddById` for
   each — lets QA reach a real "complete" attempt, not just view the UI.
3. Dispatch to the right controller's `Open()` — the same mapping
   `UIManager.ResumePendingInteraction` already uses
   (`FindFirstObjectByType<T>()?.Open(...)`), duplicated here rather than
   refactored out, since `ResumePendingInteraction` takes a
   `ProductionDialogueCheckpoint` and rewiring it to also accept a bare
   interaction id is out of scope for a debug-only caller.
4. Hide the picker canvas while a puzzle is open; each controller's own
   `Close()` (they all implement `IRuntimeModalController`) already
   restores normal ingame state, so the debug controller just re-shows the
   picker afterward (poll `IsOpen` on the controller that was opened).

### `GameStateManager.DebugResetPuzzle`

```csharp
#if UNITY_EDITOR
public void DebugResetPuzzle(string sceneId, string interactionId)
{
    string normalizedScene = NormalizeSceneId(sceneId);
    if (!string.IsNullOrEmpty(normalizedScene))
    {
        data.completedProductionSceneIds.Remove(normalizedScene);
    }
    SavePuzzleSession(new PuzzleSessionState
    {
        puzzleId = interactionId,
        completed = false
    });
    SaveAndNotify();
}
#endif
```

Scoped to exactly the one scene id + one puzzle session; every other flag,
scene completion, evidence id, and trust value on the active slot is left
alone. This does still write to the real active slot's save data for that
one puzzle (not a true sandbox) — the debug picker's header text says so
explicitly ("이 화면은 현재 활성 세이브 슬롯의 퍼즐 완료 상태를 직접
초기화합니다"), so QA knows to pick a slot they don't mind touching before
pressing Play.

## Known limits (accepted for v1)

- Opening every puzzle is fully automatic. *Completing* some of them may
  still require upstream case-progress state this tool doesn't fabricate —
  e.g. `FinalAccusationUIController.Open()` also runs
  `CreatePreparationService(state).Prepare()`, whose readiness depends on
  which deductions/evidence are unlocked elsewhere. QA can open and
  interact with the mechanic either way; a "submit" may report not-ready
  until those flags are also set by hand. Deeper auto-satisfaction of every
  puzzle's downstream narrative state is out of scope here.
- Editor Play Mode only, by design (per user decision) — not wired for
  Player builds at all.

## Testing

- Manual: open `PuzzleQA.unity`, press Play, click through all 8 buttons,
  confirm each puzzle's UI opens and the picker returns after closing.
- Manual: open the same puzzle twice in a row (reset-and-reopen) to confirm
  `DebugResetPuzzle` actually clears prior completion.
- No EditMode/PlayMode automated tests planned — this is Editor-only QA
  tooling, not shipped game logic, and `#if UNITY_EDITOR` keeps it out of
  the code paths the project's existing test suites exercise.
