# Puzzle QA Debug Scene Implementation Plan

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:subagent-driven-development (recommended) or superpowers:executing-plans to implement this plan task-by-task. Steps use checkbox (`- [ ]`) syntax for tracking.

**Goal:** Build an Editor-only debug scene that lets QA jump straight into any of the 8 production puzzles without playing through the real dialogue flow, without ever risking the 3 real save slots.

**Architecture:** A new additive-loading bootstrap scene (`PuzzleQA.unity`) loads the real `UI Basic Scene` on top of itself, then a small always-on-top picker UI resets and opens whichever puzzle QA clicks, reusing the game's own puzzle controllers and `ProductionSceneCompletionCatalog` exactly as they are. A new `GameStateManager.DebugResetPuzzle` method (Editor-only) clears one puzzle's completion state without touching anything else on the active save slot.

**Tech Stack:** Unity 6, C# (uGUI + TextMeshPro), NUnit EditMode tests via Unity Test Runner.

## Global Constraints

- Every new type/method must compile out of Player builds: wrap `GameStateManager.DebugResetPuzzle` and the entire `PuzzleQaDebugController` class in `#if UNITY_EDITOR` / `#endif`.
- `PuzzleQA.unity` must **not** be added to `ProjectSettings/EditorBuildSettings.asset` (Build Settings scene list) — this is what keeps it out of Player builds by omission, in addition to the `#if UNITY_EDITOR` guard.
- Never call `GameStateManager.SelectSaveSlot`, `GameStateManager.StartNewGame`, or `GameStateSaveStore.ClearAll` from anything in this feature — real save slots are clamped to `[1, 3]` and there is no free QA-only slot; `StartNewGame()` wipes whichever of the 3 is currently active.
- `DebugResetPuzzle` must only ever touch the one scene id + one puzzle id it's given — every other flag, scene completion, evidence id, and trust value on the active slot stays untouched.
- Namespace for the new script is `Wake.QA`, not `Wake.Debug` — a namespace literally named `Debug` shadows `UnityEngine.Debug` for every file in it and breaks `Debug.Log(...)` calls at the call site.

---

## File Structure

- **Modify:** `Assets/_Project/Code/Core/GameStateManager.cs` — add `DebugResetPuzzle(string sceneId, string interactionId)`.
- **Modify:** `Assets/_Project/Tests/EditMode/GameStateManagerTests.cs` — add two tests for `DebugResetPuzzle`.
- **Create:** `Assets/_Project/Code/Debug/PuzzleQaDebugController.cs` — the picker UI + puzzle dispatch, `namespace Wake.QA`.
- **Create:** `Assets/_Project/Scenes/Debug/PuzzleQA.unity` — the bootstrap scene, one GameObject carrying `PuzzleQaDebugController`.

---

### Task 1: `GameStateManager.DebugResetPuzzle`

**Files:**
- Modify: `Assets/_Project/Code/Core/GameStateManager.cs:508` (insert immediately after the closing brace of `SavePuzzleSession`)
- Test: `Assets/_Project/Tests/EditMode/GameStateManagerTests.cs:261` (insert immediately after `CompletedScenes_AreCanonicalAndUnique`, before `CompletedScenes_RestoreAfterManagerRecreation`)

**Interfaces:**
- Consumes: `data.completedProductionSceneIds` (`List<string>`), `data.puzzleSessions` (`List<PuzzleSessionState>`), private static `NormalizeSceneId(string)`, private static `NormalizeObjectiveId(string)`, private `SaveAndNotify()` — all already defined in `GameStateManager.cs`.
- Produces: `public void DebugResetPuzzle(string sceneId, string interactionId)` on `GameStateManager`, compiled only under `UNITY_EDITOR`. Task 2 calls this.

Existing `SavePuzzleSession` only ORs `completed` in (`stored.completed |= session.completed`), so it can never be used to un-complete a puzzle — `DebugResetPuzzle` must mutate `data.puzzleSessions` directly instead of going through it.

- [ ] **Step 1: Write the two failing tests**

Open `Assets/_Project/Tests/EditMode/GameStateManagerTests.cs` and insert after line 261 (right after `CompletedScenes_AreCanonicalAndUnique`'s closing `}`):

```csharp
        [Test]
        public void DebugResetPuzzle_ClearsCompletionAndSession()
        {
            state.RecordCompletedScene("D2-02");
            state.SavePuzzleSession(new PuzzleSessionState
            {
                puzzleId = "blood_pattern",
                completed = true,
                step = 3,
                hintLevel = 2
            });

            state.DebugResetPuzzle("D2-02", "blood_pattern");

            Assert.That(state.HasCompletedScene("D2-02"), Is.False);
            Assert.That(
                state.TryGetPuzzleSession("blood_pattern", out _),
                Is.False);
        }

        [Test]
        public void DebugResetPuzzle_LeavesOtherSceneAndPuzzleUntouched()
        {
            state.RecordCompletedScene("D2-02");
            state.RecordCompletedScene("D6-05");
            state.SavePuzzleSession(new PuzzleSessionState
            {
                puzzleId = "timeline_12_cards",
                completed = true
            });

            state.DebugResetPuzzle("D2-02", "blood_pattern");

            Assert.That(state.HasCompletedScene("D6-05"), Is.True);
            Assert.That(
                state.TryGetPuzzleSession(
                    "timeline_12_cards", out PuzzleSessionState session),
                Is.True);
            Assert.That(session.completed, Is.True);
        }
```

- [ ] **Step 2: Run the tests to verify they fail to compile**

Use `mcp__UnityMCP__run_tests` with `mode: "EditMode"`, `test_names: ["Wake.Tests.GameStateManagerTests.DebugResetPuzzle_ClearsCompletionAndSession", "Wake.Tests.GameStateManagerTests.DebugResetPuzzle_LeavesOtherSceneAndPuzzleUntouched"]`, `include_failed_tests: true`.
Expected: compile error, `DebugResetPuzzle` does not exist on `GameStateManager`.

- [ ] **Step 3: Implement `DebugResetPuzzle`**

In `Assets/_Project/Code/Core/GameStateManager.cs`, insert immediately after `SavePuzzleSession`'s closing `}` (line 508):

```csharp

#if UNITY_EDITOR
        /// QA-only: clears one puzzle's completion state (scene completion
        /// + its PuzzleSessionState) without touching anything else on the
        /// active save slot. Never call StartNewGame/SelectSaveSlot here —
        /// real slots are 1-3 and StartNewGame wipes whichever is active.
        public void DebugResetPuzzle(string sceneId, string interactionId)
        {
            string normalizedScene = NormalizeSceneId(sceneId);
            if (!string.IsNullOrEmpty(normalizedScene))
            {
                data.completedProductionSceneIds.Remove(normalizedScene);
            }

            string normalizedInteraction = NormalizeObjectiveId(interactionId);
            if (!string.IsNullOrEmpty(normalizedInteraction))
            {
                data.puzzleSessions.RemoveAll(item =>
                    item != null && item.puzzleId == normalizedInteraction);
            }

            SaveAndNotify();
        }
#endif
```

- [ ] **Step 4: Run the tests to verify they pass**

Use `mcp__UnityMCP__run_tests` with the same `test_names` as Step 2.
Expected: both PASS.

- [ ] **Step 5: Run the full EditMode suite to check for regressions**

Use `mcp__UnityMCP__run_tests` with `mode: "EditMode"` and no `test_names` filter (full `GameStateManagerTests` class at minimum; whole EditMode assembly if it finishes in reasonable time).
Expected: no new failures.

- [ ] **Step 6: Commit**

```bash
git add Assets/_Project/Code/Core/GameStateManager.cs Assets/_Project/Tests/EditMode/GameStateManagerTests.cs
git commit -m "feat: add GameStateManager.DebugResetPuzzle for QA puzzle reset"
```

---

### Task 2: `PuzzleQaDebugController` script

**Files:**
- Create: `Assets/_Project/Code/Debug/PuzzleQaDebugController.cs`

**Interfaces:**
- Consumes: `GameStateManager.Instance`, `GameStateManager.DebugResetPuzzle(string, string)` (Task 1), `UIManager.Instance`, `UIManager.IsInitialized`, `UIManager.ShowIngame()`, `IRuntimeModalController { bool IsOpen; void Close(); }`, `ProductionSceneCompletionCatalog.All` (`IReadOnlyList<ProductionSceneCompletionRequirement>`) and its `ExitInspectionInteraction`/`BloodPatternInteraction`/`CameraBlindSpotInteraction`/`MarcusInterrogationInteraction`/`CargoRailInteraction`/`TimelineInteraction`/`OrpheusInteraction`/`FinalAccusationInteraction` constants, `ProductionSceneCompletionRequirement.SceneId`/`.InteractionId`, `ProductionPuzzleCatalog.TryGet(string, out ProductionPuzzleDefinition)`, `ProductionPuzzleDefinition.RequiredEvidenceIds`, `EvidenceInventory.Instance.TryAddById(string)`, the 8 controllers' `Open()`/`Open(string)` methods and `FindFirstObjectByType<T>()`.
- Produces: `PuzzleQaDebugController : MonoBehaviour` (`Wake.QA` namespace) — Task 3 attaches this to the bootstrap scene's GameObject. No other task depends on its internals.

This is Editor-only QA tooling with no automated test target (procedural UI + scene bootstrap, matching how the game's other `*UIController` classes are also untested — only their pure logic classes like `BloodDirectionPuzzleSession` have unit tests). Verification is manual, in Task 3.

- [ ] **Step 1: Write the script**

Create `Assets/_Project/Code/Debug/PuzzleQaDebugController.cs`:

```csharp
#if UNITY_EDITOR
using System.Collections;
using TMPro;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
using Wake.Core;
using Wake.Evidence;
using Wake.Narrative;
using Wake.Puzzles;
using Wake.UI;

namespace Wake.QA
{
    /// Editor Play Mode only QA tool: additively loads the real game scene,
    /// then lets QA open any of the 8 production puzzles directly. Resets
    /// only the one puzzle being opened on whatever save slot is already
    /// active - never touches StartNewGame/SelectSaveSlot (see
    /// GameStateManager.DebugResetPuzzle).
    public sealed class PuzzleQaDebugController : MonoBehaviour
    {
        private const string GameSceneName = "UI Basic Scene";

        private GameObject pickerRoot;
        private TMP_Text statusText;
        private IRuntimeModalController openController;

        private void Start()
        {
            StartCoroutine(Bootstrap());
        }

        private IEnumerator Bootstrap()
        {
            AsyncOperation load = SceneManager.LoadSceneAsync(
                GameSceneName, LoadSceneMode.Additive);
            while (load != null && !load.isDone)
            {
                yield return null;
            }

            while (GameStateManager.Instance == null ||
                   UIManager.Instance == null ||
                   !UIManager.Instance.IsInitialized)
            {
                yield return null;
            }

            UIManager.Instance.ShowIngame();
            BuildPicker();
        }

        private void BuildPicker()
        {
            var canvasObject = new GameObject("Puzzle QA Picker");
            canvasObject.transform.SetParent(transform, false);
            Canvas canvas = canvasObject.AddComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            canvas.overrideSorting = true;
            canvas.sortingOrder = 1000;
            canvasObject.AddComponent<CanvasScaler>();
            canvasObject.AddComponent<GraphicRaycaster>();

            pickerRoot = new GameObject("Root");
            pickerRoot.transform.SetParent(canvasObject.transform, false);
            var rootRect = pickerRoot.AddComponent<RectTransform>();
            rootRect.anchorMin = new Vector2(0f, 0.5f);
            rootRect.anchorMax = new Vector2(0f, 0.5f);
            rootRect.pivot = new Vector2(0f, 0.5f);
            rootRect.anchoredPosition = new Vector2(16f, 0f);
            rootRect.sizeDelta = new Vector2(320f, 460f);
            pickerRoot.AddComponent<Image>().color =
                new Color(0f, 0f, 0f, 0.75f);
            var layout = pickerRoot.AddComponent<VerticalLayoutGroup>();
            layout.padding = new RectOffset(12, 12, 12, 12);
            layout.spacing = 6f;
            layout.childForceExpandHeight = false;
            layout.childControlHeight = false;
            layout.childControlWidth = true;

            AddLabel(
                pickerRoot.transform,
                "퍼즐 QA 선택 — 현재 활성 세이브 슬롯의 완료 상태를 " +
                "직접 초기화합니다.");

            foreach (ProductionSceneCompletionRequirement requirement in
                     ProductionSceneCompletionCatalog.All)
            {
                AddPuzzleButton(requirement);
            }

            statusText = AddLabel(pickerRoot.transform, string.Empty);
        }

        private TMP_Text AddLabel(Transform parent, string text)
        {
            var labelObject = new GameObject("Label");
            labelObject.transform.SetParent(parent, false);
            var label = labelObject.AddComponent<TextMeshProUGUI>();
            label.text = text;
            label.fontSize = 14f;
            label.color = Color.white;
            label.enableWordWrapping = true;
            var rect = labelObject.GetComponent<RectTransform>();
            rect.sizeDelta = new Vector2(296f, 40f);
            return label;
        }

        private void AddPuzzleButton(
            ProductionSceneCompletionRequirement requirement)
        {
            var buttonObject = new GameObject($"Btn_{requirement.InteractionId}");
            buttonObject.transform.SetParent(pickerRoot.transform, false);
            buttonObject.AddComponent<RectTransform>().sizeDelta =
                new Vector2(296f, 32f);
            buttonObject.AddComponent<Image>().color =
                new Color(1f, 1f, 1f, 0.12f);
            Button button = buttonObject.AddComponent<Button>();

            TMP_Text label = AddLabel(
                buttonObject.transform,
                $"{requirement.SceneId} · {requirement.InteractionId}");
            label.alignment = TextAlignmentOptions.Center;
            var labelRect = label.GetComponent<RectTransform>();
            labelRect.anchorMin = Vector2.zero;
            labelRect.anchorMax = Vector2.one;
            labelRect.sizeDelta = Vector2.zero;

            button.onClick.AddListener(() => OpenPuzzle(requirement));
        }

        private void OpenPuzzle(ProductionSceneCompletionRequirement requirement)
        {
            GameStateManager state = GameStateManager.Instance;
            if (state == null)
            {
                return;
            }

            state.DebugResetPuzzle(
                requirement.SceneId, requirement.InteractionId);
            InjectRequiredEvidence(requirement.InteractionId);

            IRuntimeModalController controller =
                OpenController(requirement.InteractionId);
            if (controller == null)
            {
                statusText.text =
                    $"열기 실패: {requirement.InteractionId} " +
                    "(컨트롤러를 찾을 수 없거나 Open()이 실패했습니다.)";
                return;
            }

            openController = controller;
            pickerRoot.SetActive(false);
            StartCoroutine(WaitForClose());
        }

        private IEnumerator WaitForClose()
        {
            while (openController != null && openController.IsOpen)
            {
                yield return null;
            }

            openController = null;
            pickerRoot.SetActive(true);
        }

        private static void InjectRequiredEvidence(string interactionId)
        {
            if (EvidenceInventory.Instance == null ||
                !ProductionPuzzleCatalog.TryGet(
                    interactionId,
                    out ProductionPuzzleDefinition definition))
            {
                return;
            }

            foreach (string evidenceId in definition.RequiredEvidenceIds)
            {
                EvidenceInventory.Instance.TryAddById(evidenceId);
            }
        }

        private static IRuntimeModalController OpenController(
            string interactionId)
        {
            if (interactionId ==
                ProductionSceneCompletionCatalog.ExitInspectionInteraction)
            {
                var controller = FindFirstObjectByType<ExitInspectionUIController>();
                return controller != null && controller.Open()
                    ? controller : null;
            }

            if (interactionId ==
                ProductionSceneCompletionCatalog.BloodPatternInteraction)
            {
                var controller =
                    FindFirstObjectByType<BloodDirectionPuzzleUIController>();
                return controller != null && controller.Open()
                    ? controller : null;
            }

            if (interactionId ==
                ProductionSceneCompletionCatalog.CameraBlindSpotInteraction)
            {
                var controller = FindFirstObjectByType<CameraBlindSpotUIController>();
                return controller != null && controller.Open()
                    ? controller : null;
            }

            if (interactionId ==
                ProductionSceneCompletionCatalog.MarcusInterrogationInteraction)
            {
                var controller =
                    FindFirstObjectByType<MarcusInterrogationUIController>();
                return controller != null && controller.Open()
                    ? controller : null;
            }

            if (interactionId ==
                ProductionSceneCompletionCatalog.CargoRailInteraction)
            {
                var controller = FindFirstObjectByType<ProductionPuzzleUIController>();
                return controller != null &&
                       controller.Open(
                           ProductionSceneCompletionCatalog.CargoRailInteraction)
                    ? controller : null;
            }

            if (interactionId ==
                ProductionSceneCompletionCatalog.TimelineInteraction)
            {
                var controller = FindFirstObjectByType<TimelinePuzzleUIController>();
                return controller != null && controller.Open()
                    ? controller : null;
            }

            if (interactionId ==
                ProductionSceneCompletionCatalog.OrpheusInteraction)
            {
                var controller =
                    FindFirstObjectByType<OrpheusAudioRestorationUIController>();
                return controller != null && controller.Open()
                    ? controller : null;
            }

            if (interactionId ==
                ProductionSceneCompletionCatalog.FinalAccusationInteraction)
            {
                var controller = FindFirstObjectByType<FinalAccusationUIController>();
                if (controller == null)
                {
                    return null;
                }

                controller.Open();
                return controller;
            }

            return null;
        }
    }
}
#endif
```

- [ ] **Step 2: Let Unity compile and confirm no errors**

Use `mcp__UnityMCP__refresh_unity` with `mode: "force"`, `compile: "request"`, `wait_for_ready: true`, then `mcp__UnityMCP__read_console` with `types: ["error"]` to confirm zero compile errors from the new file.

- [ ] **Step 3: Commit**

```bash
git add Assets/_Project/Code/Debug/PuzzleQaDebugController.cs
git commit -m "feat: add PuzzleQaDebugController for QA puzzle picker UI"
```

---

### Task 3: `PuzzleQA.unity` bootstrap scene + manual verification

**Files:**
- Create: `Assets/_Project/Scenes/Debug/PuzzleQA.unity`

**Interfaces:**
- Consumes: `PuzzleQaDebugController` (Task 2) as a component to attach.
- Produces: nothing further downstream — this is the last task.

- [ ] **Step 1: Create the scene**

Use `mcp__UnityMCP__manage_scene` with `action: "create"`, `name: "PuzzleQA"`, `path: "Assets/_Project/Scenes/Debug"`.

- [ ] **Step 2: Remove the scene's default Main Camera and Directional Light**

The default Unity scene template includes a camera and light; both would collide with (or sit uselessly alongside) the ones "UI Basic Scene" brings in when additively loaded. Use `mcp__UnityMCP__manage_gameobject` with `action: "delete"`, `target: "Main Camera"`, `search_method: "by_name"`, then the same for `target: "Directional Light"`. If either is absent (empty template), skip it.

- [ ] **Step 3: Create the controller GameObject and attach the script**

Use `mcp__UnityMCP__manage_gameobject` with `action: "create"`, `name: "PuzzleQaDebugController"`.
Then `mcp__UnityMCP__manage_components` with `action: "add"`, `target: "PuzzleQaDebugController"`, `search_method: "by_name"`, `component_type: "PuzzleQaDebugController"`.

- [ ] **Step 4: Save the scene**

Use `mcp__UnityMCP__manage_scene` with `action: "save"`.

- [ ] **Step 5: Confirm the scene is NOT in Build Settings**

Use `mcp__UnityMCP__manage_scene` with `action: "get_build_settings"`. Confirm `Assets/_Project/Scenes/Debug/PuzzleQA.unity` is absent from the list — only `Assets/_Project/Scenes/UI/UI Basic Scene.unity` should be there. If it somehow got added, remove it with `mcp__UnityMCP__manage_build` (`action: "scenes"`).

- [ ] **Step 6: Commit the scene**

```bash
git add "Assets/_Project/Scenes/Debug/PuzzleQA.unity" "Assets/_Project/Scenes/Debug/PuzzleQA.unity.meta"
git commit -m "feat: add PuzzleQA debug scene for puzzle QA"
```

- [ ] **Step 7: Manual verification — full click-through**

With `PuzzleQA.unity` open and active, enter Play Mode (`mcp__UnityMCP__manage_editor` or the Editor UI). Confirm via `mcp__UnityMCP__read_console` (`types: ["error"]`) that no errors appear during boot, then for each of the 8 picker buttons in turn:
1. Click it.
2. Confirm the matching puzzle's UI actually opens (screenshot via `mcp__UnityMCP__manage_camera` `action: "screenshot"`, or read console for the puzzle's own "restored session" log lines where applicable).
3. Close it (each puzzle has its own close/back control).
4. Confirm the picker reappears.

Expected: all 8 open and close cleanly, zero console errors.

- [ ] **Step 8: Manual verification — reset actually resets**

Pick one puzzle (e.g. `blood_pattern`), open it, and drive it to completion. Close it, return to the picker, and click the same button again.
Expected: the puzzle opens in its fresh/initial state, not the just-completed one — confirms `DebugResetPuzzle` is doing its job end-to-end, not just in the Task 1 unit tests.

- [ ] **Step 9: Exit Play Mode**

Stop Play Mode before leaving the scene, same as any other manual Editor verification.
