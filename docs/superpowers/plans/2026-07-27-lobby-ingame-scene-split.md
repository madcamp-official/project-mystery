# Lobby/Ingame Scene Split Implementation Plan

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:subagent-driven-development (recommended) or superpowers:executing-plans to implement this plan task-by-task. Steps use checkbox (`- [ ]`) syntax for tracking.

**Goal:** Split the single `UI Basic Scene` into a `Bootstrap` scene (persistent
services), a `Lobby Scene` (World Space canvas, title + Water reveal + save
slot picker), and an `Ingame Scene` (Screen Space - Overlay canvas at
2880x1800), without modifying `UI Basic Scene` itself.

**Architecture:** New C# scripts (`GameSystemsBootstrap`, `LobbyUIManager`,
`IngameUIManager`, `LobbyRevealSequence`, `IIngameUiHost`) sit alongside the
existing `UIManager`/`RuntimeUiOverhaul.cs` code, which stays untouched
except for: one constant fix, one dual-host bridge line in
`SaveSlotSelectionController.Confirm()`, one new interface implementation
(purely additive), and one dual-host line in `ProductionEndingUIController`.
Every other gameplay controller that currently calls `UIManager.Instance`
directly (`EvidencePanelController`, `EvidenceLocationHotspotOverlay`,
`ExitInspectionUIController`, `FinalAccusationUIController`,
`MapController`, `SettingsController`) gets repointed at a small
scene-agnostic locator (`IngameUi.Current`) instead, so the same script
works unmodified in both `UI Basic Scene` (backed by `UIManager`) and
`Ingame Scene` (backed by `IngameUIManager`).

New scenes are built by duplicating `UI Basic Scene` (or, for the persistent
layer, by duplicating and relocating the existing `GameSystems` GameObject)
and pruning via Unity Editor tooling — this is Unity scene/asset surgery,
not application code, so most "tests" in those tasks are scripted Editor
verifications (hierarchy checks, Play Mode smoke checks, console error
checks) rather than classic unit tests. The pieces of pure C# logic (a
reference-resolution constant, the reveal-offset math, the locator) get real
NUnit EditMode/PlayMode tests.

**Tech Stack:** Unity 6 (URP), C#/NUnit Test Framework (`Assets/_Project/Tests`),
UnityMCP editor-automation tools (`manage_scene`, `manage_gameobject`,
`manage_asset`, `manage_build`, `manage_editor`, `read_console`).

## Global Constraints

- Do not modify `Assets/_Project/Scenes/UI/UI Basic Scene.unity` or its
  existing EditMode/PlayMode tests. (Spec: Non-goals.)
- Save/load slot background stays as its existing near-opaque look — no
  see-through change. (Spec: Non-goals.)
- No fallback/retry handling for a failed scene load mid-transition. (Spec:
  Non-goals / Error handling.)
- Ingame Scene `CanvasScaler`: Scale With Screen Size, reference resolution
  **2880x1800**, `matchWidthOrHeight` **0.5**. (Spec: Ingame Scene.)
- Lobby Scene `Canvas`: World Space, RectTransform size **2880x1800**,
  `localScale` **0.0056**, at world origin — kept exactly as currently
  authored on disk. (Spec: Lobby Scene.)
- No new animation/tweening package (no DOTween in this project) — reuse the
  existing hand-rolled coroutine + `SmoothStep` style already used by
  `UiPanelEntranceAnimator` / `EvidenceAcquisitionNoticeController` in
  `Assets/_Project/Code/UI/RuntimeUiOverhaul.cs`. (Spec: Lobby Scene.)
- Ending → title-screen flow (`ProductionEndingUIController`) becomes: unload
  `Ingame Scene`, load `Lobby Scene` fresh via `LoadSceneMode.Single`
  (confirmed with user; persistent services survive via `DontDestroyOnLoad`,
  same reset calls as today's `ShowStartScene()` run before the load).

---

## File Structure

**Create:**
- `Assets/_Project/Code/UI/IIngameUiHost.cs` — shared interface + scene-
  agnostic locator (`IngameUi.Current`).
- `Assets/_Project/Code/Core/GameSystemsBootstrap.cs` — marks its host object
  `DontDestroyOnLoad`, loads `Lobby Scene` additively on `Awake`.
- `Assets/_Project/Code/UI/LobbyUIManager.cs` — Lobby-scene UI bootstrap
  (StartScene panel, Settings, save-slot open, scene handoff to Ingame).
- `Assets/_Project/Code/UI/IngameUIManager.cs` — Ingame-scene UI bootstrap
  (Ingame/Map/Evidence/Settings/Status HUD panels).
- `Assets/_Project/Code/UI/LobbyRevealSequence.cs` — drives the synced
  title-exit / slot-and-water-entry animation.
- `Assets/_Project/Tests/EditMode/RuntimeUiOverhaulControllerTests.cs`
- `Assets/_Project/Tests/EditMode/LobbyRevealSequenceTests.cs`
- `Assets/_Project/Tests/PlayMode/LobbyRevealSequencePlayModeTests.cs`
- `Assets/_Project/Tests/EditMode/IngameUiHostLocatorTests.cs`
- `Assets/_Project/Scenes/Bootstrap.unity`
- `Assets/_Project/Scenes/Lobby Scene.unity`
- `Assets/_Project/Scenes/Ingame Scene.unity`

**Modify:**
- `Assets/_Project/Code/UI/RuntimeUiOverhaul.cs:184` — reference resolution
  `1920x1080` → `2880x1800` via a new named constant.
- `Assets/_Project/Code/UI/RuntimeUiOverhaul.cs:580-591` (`Confirm()`) —
  call whichever of `UIManager`/`LobbyUIManager` is present in the scene.
- `Assets/_Project/Code/UI/UIManager.cs` — implement `IIngameUiHost`
  (additive: one interface on the class declaration, one new property).
- `Assets/_Project/Code/Evidence/EvidencePanelController.cs:107`
- `Assets/_Project/Code/Exploration/EvidenceLocationHotspotOverlay.cs:195,206`
- `Assets/_Project/Code/UI/ExitInspectionUIController.cs:199-207`
- `Assets/_Project/Code/UI/FinalAccusationUIController.cs:615-645`
- `Assets/_Project/Code/UI/MapController.cs:287-315`
- `Assets/_Project/Code/UI/SettingsController.cs:25`
- `Assets/_Project/Code/UI/ProductionEndingUIController.cs:143`

---

## Task 1: Fix hardcoded Ingame canvas reference resolution

`RuntimeUiOverhaulController.ConfigureCanvas()` currently hardcodes
`referenceResolution = new Vector2(1920f, 1080f)` at runtime
(`Assets/_Project/Code/UI/RuntimeUiOverhaul.cs:184`), which would silently
overwrite the Ingame Scene's authored 2880x1800 the instant the scene plays.
Replace the literal with a named constant set to 2880x1800.

**Files:**
- Modify: `Assets/_Project/Code/UI/RuntimeUiOverhaul.cs:175-188`
- Test: `Assets/_Project/Tests/EditMode/RuntimeUiOverhaulControllerTests.cs`

**Interfaces:**
- Produces: `RuntimeUiOverhaulController.ReferenceResolution` (public static
  readonly `Vector2`).

- [ ] **Step 1: Write the failing test**

```csharp
using NUnit.Framework;
using UnityEngine;
using Wake.UI;

namespace Wake.Tests
{
    public class RuntimeUiOverhaulControllerTests
    {
        [Test]
        public void ReferenceResolution_Is2880x1800()
        {
            Assert.That(
                RuntimeUiOverhaulController.ReferenceResolution,
                Is.EqualTo(new Vector2(2880f, 1800f)));
        }
    }
}
```

- [ ] **Step 2: Run test to verify it fails**

Run (Unity Test Runner, EditMode, or via `mcp__UnityMCP__run_tests` with
`test_mode: "EditMode"` filtered to `RuntimeUiOverhaulControllerTests`).
Expected: FAIL — `ReferenceResolution` does not exist (compile error until
Step 3).

- [ ] **Step 3: Add the constant and use it in `ConfigureCanvas()`**

In `Assets/_Project/Code/UI/RuntimeUiOverhaul.cs`, inside
`RuntimeUiOverhaulController` (around line 161-174), add:

```csharp
        public static readonly Vector2 ReferenceResolution =
            new(2880f, 1800f);
```

Then change `ConfigureCanvas()` (currently lines 175-188) to:

```csharp
        private static void ConfigureCanvas()
        {
            CanvasScaler scaler = GameObject.Find("Canvas")
                ?.GetComponent<CanvasScaler>();
            if (scaler == null)
            {
                return;
            }
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = ReferenceResolution;
            scaler.screenMatchMode =
                CanvasScaler.ScreenMatchMode.MatchWidthOrHeight;
            scaler.matchWidthOrHeight = 0.5f;
        }
```

- [ ] **Step 4: Run test to verify it passes**

Expected: PASS.

- [ ] **Step 5: Commit**

```bash
git add Assets/_Project/Code/UI/RuntimeUiOverhaul.cs Assets/_Project/Tests/EditMode/RuntimeUiOverhaulControllerTests.cs Assets/_Project/Tests/EditMode/RuntimeUiOverhaulControllerTests.cs.meta
git commit -m "fix: 인게임 캔버스 참조 해상도를 2880x1800으로 고정"
```

---

## Task 2: Reveal-offset math helper for `LobbyRevealSequence`

Pure, deterministic math (world height of the canvas in world units) —
write it as a static method with a real test before wiring it into any
MonoBehaviour animation.

**Files:**
- Create: `Assets/_Project/Code/UI/LobbyRevealSequence.cs`
- Test: `Assets/_Project/Tests/EditMode/LobbyRevealSequenceTests.cs`

**Interfaces:**
- Produces: `LobbyRevealSequence.ComputeWorldHeight(RectTransform canvasRect)`
  → `float`, used by Task 3.

- [ ] **Step 1: Write the failing test**

```csharp
using NUnit.Framework;
using UnityEngine;
using Wake.UI;

namespace Wake.Tests
{
    public class LobbyRevealSequenceTests
    {
        [Test]
        public void ComputeWorldHeight_MatchesCanvasHeightTimesScale()
        {
            var go = new GameObject("Canvas", typeof(RectTransform));
            try
            {
                RectTransform rect = go.GetComponent<RectTransform>();
                rect.sizeDelta = new Vector2(2880f, 1800f);
                rect.localScale = new Vector3(0.0056f, 0.0056f, 0.0056f);

                float height = LobbyRevealSequence.ComputeWorldHeight(rect);

                Assert.That(height, Is.EqualTo(1800f * 0.0056f).Within(0.0001f));
            }
            finally
            {
                Object.DestroyImmediate(go);
            }
        }
    }
}
```

- [ ] **Step 2: Run test to verify it fails**

Expected: FAIL — `LobbyRevealSequence` type does not exist yet.

- [ ] **Step 3: Create `LobbyRevealSequence.cs` with the math method only**

```csharp
using System.Collections;
using UnityEngine;

namespace Wake.UI
{
    [DisallowMultipleComponent]
    public sealed class LobbyRevealSequence : MonoBehaviour
    {
        public static float ComputeWorldHeight(RectTransform canvasRect)
        {
            return canvasRect.sizeDelta.y * canvasRect.lossyScale.y;
        }
    }
}
```

- [ ] **Step 4: Run test to verify it passes**

Expected: PASS.

- [ ] **Step 5: Commit**

```bash
git add Assets/_Project/Code/UI/LobbyRevealSequence.cs Assets/_Project/Code/UI/LobbyRevealSequence.cs.meta Assets/_Project/Tests/EditMode/LobbyRevealSequenceTests.cs Assets/_Project/Tests/EditMode/LobbyRevealSequenceTests.cs.meta
git commit -m "feat: 로비 리빌 연출용 월드 오프셋 계산 헬퍼 추가"
```

---

## Task 3: Implement the synced reveal animation

Wire the actual animation into `LobbyRevealSequence`: the title panel exits
upward while a "reveal group" (save-slot panel's root) and `Water` enter
from below, in lockstep, using the same `SmoothStep`-over-fixed-duration
approach already used elsewhere in this codebase
(`UiPanelEntranceAnimator.Slide`,
`Assets/_Project/Code/UI/RuntimeUiOverhaul.cs:115-152`).

**Files:**
- Modify: `Assets/_Project/Code/UI/LobbyRevealSequence.cs`
- Test: `Assets/_Project/Tests/PlayMode/LobbyRevealSequencePlayModeTests.cs`

**Interfaces:**
- Consumes: `LobbyRevealSequence.ComputeWorldHeight` (Task 2).
- Produces: `LobbyRevealSequence.Configure(RectTransform titlePanel,
  RectTransform revealGroup, Transform water, RectTransform canvasRect)` and
  `LobbyRevealSequence.Play()`, consumed by Task 5 (`LobbyUIManager`).

- [ ] **Step 1: Write the failing PlayMode test**

```csharp
using System.Collections;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;
using Wake.UI;

namespace Wake.Tests
{
    public class LobbyRevealSequencePlayModeTests
    {
        [UnityTest]
        public IEnumerator Play_MovesTitleUpAndRevealGroupAndWaterIntoPlace()
        {
            var canvasGo = new GameObject("Canvas", typeof(RectTransform));
            RectTransform canvasRect = canvasGo.GetComponent<RectTransform>();
            canvasRect.sizeDelta = new Vector2(2880f, 1800f);
            canvasRect.localScale = new Vector3(0.0056f, 0.0056f, 0.0056f);

            var titleGo = new GameObject("Title", typeof(RectTransform));
            RectTransform title = titleGo.GetComponent<RectTransform>();
            title.SetParent(canvasRect, false);

            var revealGo = new GameObject("RevealGroup", typeof(RectTransform));
            RectTransform reveal = revealGo.GetComponent<RectTransform>();
            reveal.SetParent(canvasRect, false);
            reveal.anchoredPosition = new Vector2(0f, -1800f);

            var waterGo = new GameObject("Water");
            float waterStartY = waterGo.transform.position.y;

            var sequenceGo = new GameObject("Sequence");
            LobbyRevealSequence sequence =
                sequenceGo.AddComponent<LobbyRevealSequence>();
            sequence.Configure(title, reveal, waterGo.transform, canvasRect);
            sequence.Play();

            yield return new WaitForSeconds(1f);

            Assert.That(title.anchoredPosition.y, Is.EqualTo(1800f).Within(0.01f));
            Assert.That(reveal.anchoredPosition.y, Is.EqualTo(0f).Within(0.01f));
            Assert.That(
                waterGo.transform.position.y,
                Is.EqualTo(waterStartY + 1800f * 0.0056f).Within(0.01f));

            Object.Destroy(canvasGo);
            Object.Destroy(waterGo);
            Object.Destroy(sequenceGo);
        }
    }
}
```

- [ ] **Step 2: Run test to verify it fails**

Expected: FAIL — `Configure`/`Play` do not exist yet.

- [ ] **Step 3: Implement `Configure`/`Play` in `LobbyRevealSequence.cs`**

```csharp
using System.Collections;
using UnityEngine;

namespace Wake.UI
{
    [DisallowMultipleComponent]
    public sealed class LobbyRevealSequence : MonoBehaviour
    {
        private const float Duration = 0.45f;

        private RectTransform titlePanel;
        private RectTransform revealGroup;
        private Transform water;
        private float travelDistance;
        private bool played;

        public static float ComputeWorldHeight(RectTransform canvasRect)
        {
            return canvasRect.sizeDelta.y * canvasRect.lossyScale.y;
        }

        public void Configure(
            RectTransform titlePanel,
            RectTransform revealGroup,
            Transform water,
            RectTransform canvasRect)
        {
            this.titlePanel = titlePanel;
            this.revealGroup = revealGroup;
            this.water = water;
            travelDistance = canvasRect.sizeDelta.y;
            played = false;
        }

        public void Play()
        {
            if (played || titlePanel == null || revealGroup == null)
            {
                return;
            }
            played = true;
            StartCoroutine(Animate());
        }

        private IEnumerator Animate()
        {
            Vector2 titleStart = titlePanel.anchoredPosition;
            Vector2 titleEnd = titleStart + new Vector2(0f, travelDistance);
            Vector2 revealStart = revealGroup.anchoredPosition;
            Vector2 revealEnd = revealStart + new Vector2(0f, travelDistance);
            float waterWorldTravel = water != null
                ? travelDistance * titlePanel.root.localScale.y
                : 0f;
            Vector3 waterStart = water != null ? water.position : default;
            Vector3 waterEnd = waterStart + new Vector3(0f, waterWorldTravel, 0f);

            float elapsed = 0f;
            while (elapsed < Duration)
            {
                elapsed += Time.unscaledDeltaTime;
                float t = Mathf.SmoothStep(0f, 1f, elapsed / Duration);
                titlePanel.anchoredPosition =
                    Vector2.LerpUnclamped(titleStart, titleEnd, t);
                revealGroup.anchoredPosition =
                    Vector2.LerpUnclamped(revealStart, revealEnd, t);
                if (water != null)
                {
                    water.position =
                        Vector3.LerpUnclamped(waterStart, waterEnd, t);
                }
                yield return null;
            }
            titlePanel.anchoredPosition = titleEnd;
            revealGroup.anchoredPosition = revealEnd;
            if (water != null)
            {
                water.position = waterEnd;
            }
        }
    }
}
```

Note: `titlePanel.root` returns the topmost `Transform` in the hierarchy
regardless of nesting depth, so `titlePanel.root.localScale.y` correctly
resolves to the World Space `Canvas`'s own authored `localScale` (`0.0056`)
whether `titlePanel` is a direct child of `Canvas` or nested deeper (as it
is in the real Lobby Scene — `Canvas/StartScene/Title Presentation`, Task
12). The test above parents `title` directly under `canvasRect` purely for
setup brevity; the code under test does not depend on that depth.

- [ ] **Step 4: Run test to verify it passes**

Expected: PASS.

- [ ] **Step 5: Commit**

```bash
git add Assets/_Project/Code/UI/LobbyRevealSequence.cs Assets/_Project/Tests/PlayMode/LobbyRevealSequencePlayModeTests.cs Assets/_Project/Tests/PlayMode/LobbyRevealSequencePlayModeTests.cs.meta
git commit -m "feat: 타이틀 패널/세이브슬롯/water 동기화 리빌 애니메이션 구현"
```

---

## Task 4: `IIngameUiHost` interface + `IngameUi` locator

Real audit of the codebase found **7 files / 15 call sites** calling
`UIManager.Instance` directly for in-game navigation
(`EvidencePanelController`, `EvidenceLocationHotspotOverlay`,
`ExitInspectionUIController`, `FinalAccusationUIController`,
`MapController`, `ProductionEndingUIController`, `SettingsController`). All
of these scripts are reused unmodified in the new `Ingame Scene`, where
`UIManager` does not exist (only `IngameUIManager` does) — so every one of
those calls would silently no-op there. A shared interface implemented by
both managers, resolved through one static locator, lets every call site
(except `SaveSlotSelectionController.Confirm()` and
`ProductionEndingUIController`, handled separately in Tasks 8-9) be
repointed once and work correctly in both scenes.

**Files:**
- Create: `Assets/_Project/Code/UI/IIngameUiHost.cs`
- Test: `Assets/_Project/Tests/EditMode/IngameUiHostLocatorTests.cs`

**Interfaces:**
- Produces: `IIngameUiHost` (interface), `IngameUi.Current` (static
  property returning `IIngameUiHost`, null if neither manager exists).
  Implemented by `UIManager` (Task 7) and `IngameUIManager` (Task 6).

- [ ] **Step 1: Write the failing test**

```csharp
using NUnit.Framework;
using Wake.UI;

namespace Wake.Tests
{
    public class IngameUiHostLocatorTests
    {
        private sealed class FakeHost : IIngameUiHost
        {
            public bool IsShowingIngamePanel { get; set; }
            public bool IsSettingsOpen { get; set; }
            public int OpenRuntimeModalCount { get; set; }
            public int ShowIngameCalls { get; private set; }
            public void ShowIngame() => ShowIngameCalls++;
            public void ShowEvidence() { }
            public void ShowEvidence(string evidenceId) { }
            public void CloseSettings() { }
        }

        [Test]
        public void Current_ReturnsNull_WhenNoHostRegistered()
        {
            IngameUi.Register(null);
            Assert.That(IngameUi.Current, Is.Null);
        }

        [Test]
        public void Current_ReturnsRegisteredHost()
        {
            var host = new FakeHost();
            IngameUi.Register(host);
            Assert.That(IngameUi.Current, Is.SameAs(host));
            IngameUi.Register(null);
        }
    }
}
```

- [ ] **Step 2: Run test to verify it fails**

Expected: FAIL — `IIngameUiHost`/`IngameUi` do not exist yet.

- [ ] **Step 3: Create `IIngameUiHost.cs`**

```csharp
namespace Wake.UI
{
    public interface IIngameUiHost
    {
        bool IsShowingIngamePanel { get; }
        bool IsSettingsOpen { get; }
        int OpenRuntimeModalCount { get; }
        void ShowIngame();
        void ShowEvidence();
        void ShowEvidence(string evidenceId);
        void CloseSettings();
    }

    public static class IngameUi
    {
        public static IIngameUiHost Current { get; private set; }

        public static void Register(IIngameUiHost host)
        {
            Current = host;
        }
    }
}
```

Registration is explicit (`Register`) rather than reading two static
`Instance` properties, so the locator has exactly one source of truth
regardless of which manager type is present — both `IngameUIManager` (Task
6) and `UIManager` (Task 7) call `IngameUi.Register(this)` in `Awake` and
`IngameUi.Register(null)` in `OnDestroy`.

- [ ] **Step 4: Run test to verify it passes**

Expected: PASS.

- [ ] **Step 5: Commit**

```bash
git add Assets/_Project/Code/UI/IIngameUiHost.cs Assets/_Project/Code/UI/IIngameUiHost.cs.meta Assets/_Project/Tests/EditMode/IngameUiHostLocatorTests.cs Assets/_Project/Tests/EditMode/IngameUiHostLocatorTests.cs.meta
git commit -m "feat: 씬 무관 인게임 UI 호스트 인터페이스/로케이터(IngameUi) 추가"
```

---

## Task 5: `LobbyUIManager` script

New Lobby-scene UI bootstrap, adapted from `UIManager.cs`
(`Assets/_Project/Code/UI/UIManager.cs:84-154, 248-286`) but scoped to only
`StartScene` + `Settings Popup`, and handing off to `SceneManager.LoadScene`
instead of calling `ShowIngame()` in place.

**Files:**
- Create: `Assets/_Project/Code/UI/LobbyUIManager.cs`

**Interfaces:**
- Consumes: `SaveSlotSelectionController`, `TitleScreenPresentationController`
  (existing, `Assets/_Project/Code/UI/RuntimeUiOverhaul.cs`),
  `LobbyRevealSequence.Configure/Play` (Task 3), `GameStateManager.Instance`,
  `GameFlow.Instance`, `EvidenceInventory.Instance` (existing, persistent —
  Task 11/12).
- Produces: `LobbyUIManager.Instance` (static), `LobbyUIManager
  .StartNewGameInSlot(int)`, `LobbyUIManager.ContinueGameInSlot(int)` —
  consumed by Task 8 (`SaveSlotSelectionController.Confirm()`).

- [ ] **Step 1: Create the script**

```csharp
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
using Wake.Core;
using Wake.Evidence;

namespace Wake.UI
{
    [DisallowMultipleComponent]
    public class LobbyUIManager : MonoBehaviour
    {
        private const string IngameSceneName = "Ingame Scene";

        public static LobbyUIManager Instance { get; private set; }

        private GameObject startScenePanel;
        private GameObject settingsPopup;
        private GameObject continueButton;
        private SaveSlotSelectionController saveSlotSelection;
        private LobbyRevealSequence revealSequence;

        public bool IsInitialized { get; private set; }

        private void Awake()
        {
            if (Instance != null && Instance != this)
            {
                enabled = false;
                return;
            }
            Instance = this;
            EnsureInitialized();
        }

        private void OnDestroy()
        {
            if (Instance == this)
            {
                Instance = null;
            }
        }

        public bool EnsureInitialized()
        {
            GameObject canvasObject = GameObject.Find("Canvas");
            if (canvasObject == null)
            {
                Debug.LogError("LobbyUIManager requires an active Canvas root.");
                return false;
            }
            Transform canvas = canvasObject.transform;
            var missing = new List<string>();
            startScenePanel = FindRequired(canvas, "StartScene", missing);
            settingsPopup = FindRequired(canvas, "Settings Popup", missing);
            continueButton =
                FindRequired(canvas, "StartScene/Continue Btn", missing);
            if (missing.Count > 0)
            {
                Debug.LogError(
                    "LobbyUIManager could not bind required objects: " +
                    string.Join(", ", missing));
                return false;
            }

            saveSlotSelection =
                EnsureComponent<SaveSlotSelectionController>(startScenePanel);
            EnsureComponent<TitleScreenPresentationController>(startScenePanel);
            revealSequence = EnsureComponent<LobbyRevealSequence>(gameObject);
            RectTransform revealGroup =
                saveSlotSelection.GetComponent<RectTransform>();
            revealSequence.Configure(
                startScenePanel.transform.Find("Title Presentation")
                    as RectTransform,
                revealGroup,
                GameObject.Find("Water")?.transform,
                canvas as RectTransform);

            bool buttonsBound =
                BindButton(canvas, "StartScene/Start Game Btn", OpenSaveSlots) &
                BindButton(canvas, "StartScene/Settings Btn", OpenSettings) &
                BindButton(
                    canvas, "StartScene/Continue Btn", OnContinueClicked);
            if (!buttonsBound)
            {
                return false;
            }

            continueButton.SetActive(false);
            IsInitialized = true;
            return true;
        }

        private static T EnsureComponent<T>(GameObject host)
            where T : Component
        {
            T existing = host.GetComponent<T>();
            return existing != null ? existing : host.AddComponent<T>();
        }

        private static GameObject FindRequired(
            Transform root, string path, ICollection<string> missing)
        {
            Transform target = root.Find(path);
            if (target == null)
            {
                missing.Add(path);
                return null;
            }
            return target.gameObject;
        }

        private static bool BindButton(
            Transform root, string path, UnityAction action)
        {
            Button button = root.Find(path)?.GetComponent<Button>();
            if (button == null)
            {
                Debug.LogError($"LobbyUIManager requires Button at Canvas/{path}.");
                return false;
            }
            button.onClick.RemoveListener(action);
            button.onClick.AddListener(action);
            return true;
        }

        private void OpenSaveSlots()
        {
            revealSequence.Play();
            saveSlotSelection?.Open();
        }

        public void OpenSettings()
        {
            if (settingsPopup == null)
            {
                return;
            }
            settingsPopup.transform.SetAsLastSibling();
            settingsPopup.SetActive(true);
        }

        private void OnContinueClicked() => ContinueGameInSlot(1);

        public void StartNewGameInSlot(int slot)
        {
            GameStateManager.Instance?.SelectSaveSlot(slot);
            GameFlow.Instance?.ResetSession();
            GameStateManager.Instance?.StartNewGame();
            EvidenceInventory.Instance?.Clear();
            SceneManager.LoadScene(IngameSceneName, LoadSceneMode.Single);
            GameFlow.Instance?.BeginGame();
        }

        public void ContinueGameInSlot(int slot)
        {
            GameStateManager.Instance?.SelectSaveSlot(slot);
            SceneManager.LoadScene(IngameSceneName, LoadSceneMode.Single);
            GameFlow.Instance?.ResumeGame();
        }
    }
}
```

- [ ] **Step 2: Compile check**

Run: `mcp__UnityMCP__read_console` (types `["error"]`) after Unity
recompiles. Expected: no new compile errors referencing `LobbyUIManager.cs`.

- [ ] **Step 3: Commit**

```bash
git add Assets/_Project/Code/UI/LobbyUIManager.cs Assets/_Project/Code/UI/LobbyUIManager.cs.meta
git commit -m "feat: 로비 씬 전용 UI 매니저(LobbyUIManager) 추가"
```

---

## Task 6: `IngameUIManager` script

New Ingame-scene UI bootstrap, adapted from `UIManager.cs:84-186, 287-395`
minus all start-screen logic, minus every reference to `startScenePanel`,
implementing `IIngameUiHost` (Task 4) and registering itself with
`IngameUi`. Also implements `ReturnToLobby()` for the ending flow (Task 9).

**Files:**
- Create: `Assets/_Project/Code/UI/IngameUIManager.cs`

**Interfaces:**
- Consumes: `IIngameUiHost` (Task 4), same runtime-modal controllers
  `UIManager.EnsureRuntimeControllers` already uses
  (`Assets/_Project/Code/UI/UIManager.cs:156-186`).
- Produces: `IngameUIManager.Instance` (static), `IngameUIManager.ShowMap()`,
  `.ShowEvidence()`, `.ShowEvidence(string)`, `.OpenSettings()`,
  `.CloseSettings()`, `.ReturnToLobby()` (consumed by Task 9).

- [ ] **Step 1: Create the script**

```csharp
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
using Wake.Core;
using Wake.Evidence;
using Wake.Exploration;
using Wake.Narrative;

namespace Wake.UI
{
    public enum IngamePrimaryPanel
    {
        None,
        Ingame,
        Map,
        Evidence
    }

    [DisallowMultipleComponent]
    public class IngameUIManager : MonoBehaviour, IIngameUiHost
    {
        private const string LobbySceneName = "Lobby Scene";

        public static IngameUIManager Instance { get; private set; }

        private GameObject ingamePanel;
        private GameObject mapPanel;
        private GameObject evidencePanel;
        private GameObject settingsPopup;
        private GameObject statusHud;
        private readonly List<IRuntimeModalController> runtimeModals = new();

        public bool IsInitialized { get; private set; }
        public IngamePrimaryPanel ActivePanel { get; private set; }
        public bool IsShowingIngamePanel => ActivePanel == IngamePrimaryPanel.Ingame;
        public bool IsSettingsOpen =>
            settingsPopup != null && settingsPopup.activeSelf;
        public int OpenRuntimeModalCount
        {
            get
            {
                int count = 0;
                foreach (IRuntimeModalController modal in runtimeModals)
                {
                    if (modal != null && modal.IsOpen)
                    {
                        count++;
                    }
                }
                return count;
            }
        }

        private void Awake()
        {
            if (Instance != null && Instance != this)
            {
                enabled = false;
                return;
            }
            Instance = this;
            EnsureInitialized();
            IngameUi.Register(this);
        }

        private void OnDestroy()
        {
            if (Instance == this)
            {
                Instance = null;
                IngameUi.Register(null);
            }
        }

        public bool EnsureInitialized()
        {
            GameObject canvasObject = GameObject.Find("Canvas");
            if (canvasObject == null)
            {
                Debug.LogError("IngameUIManager requires an active Canvas root.");
                return false;
            }
            Transform canvas = canvasObject.transform;
            var missing = new List<string>();
            ingamePanel = FindRequired(canvas, "Ingame", missing);
            mapPanel = FindRequired(canvas, "Map", missing);
            evidencePanel = FindRequired(canvas, "Evidence", missing);
            settingsPopup = FindRequired(canvas, "Settings Popup", missing);
            Transform statusHudTransform = canvas.Find("Status HUD");
            statusHud =
                statusHudTransform != null ? statusHudTransform.gameObject : null;
            if (missing.Count > 0)
            {
                Debug.LogError(
                    "IngameUIManager could not bind required objects: " +
                    string.Join(", ", missing));
                return false;
            }

            EnsureRuntimeControllers();
            bool buttonsBound =
                BindButton(canvas, "Ingame/Map Btn", ShowMap) &
                BindButton(canvas, "Ingame/Evidence Btn", ShowEvidence) &
                BindButton(canvas, "Ingame/Settings Btn", OpenSettings) &
                BindButton(canvas, "Map/Back Btn", ShowIngame);
            if (!buttonsBound)
            {
                return false;
            }

            IsInitialized = true;
            ShowIngame();
            return true;
        }

        private void EnsureRuntimeControllers()
        {
            runtimeModals.Clear();
            RegisterModal(EnsureComponent<ExitInspectionUIController>(ingamePanel));
            RegisterModal(EnsureComponent<ProductionPuzzleUIController>(ingamePanel));
            RegisterModal(EnsureComponent<FinalAccusationUIController>(ingamePanel));
            RegisterModal(EnsureComponent<MarcusInterrogationUIController>(ingamePanel));
            RegisterModal(EnsureComponent<TimelinePuzzleUIController>(ingamePanel));
            RegisterModal(EnsureComponent<OrpheusAudioRestorationUIController>(ingamePanel));
            RegisterModal(EnsureComponent<ProductionEndingUIController>(ingamePanel));
            RegisterModal(EnsureComponent<EvidenceTheoryBoardController>(evidencePanel));
            EnsureComponent<NarrativeLocationHUDController>(ingamePanel);
            EnsureComponent<EvidenceNotebookTabsController>(evidencePanel);
            EnsureComponent<RuntimeUiOverhaulController>(gameObject);
            EnsureComponent<EvidenceAcquisitionNoticeController>(gameObject);
            if (statusHud != null)
            {
                EnsureComponent<ObjectiveMapHUDController>(statusHud);
            }
        }

        private void RegisterModal(IRuntimeModalController modal)
        {
            if (modal != null && !runtimeModals.Contains(modal))
            {
                runtimeModals.Add(modal);
            }
        }

        private static T EnsureComponent<T>(GameObject host)
            where T : Component
        {
            if (host == null)
            {
                return null;
            }
            T existing = host.GetComponent<T>();
            return existing != null ? existing : host.AddComponent<T>();
        }

        private static GameObject FindRequired(
            Transform root, string path, ICollection<string> missing)
        {
            Transform target = root.Find(path);
            if (target == null)
            {
                missing.Add(path);
                return null;
            }
            return target.gameObject;
        }

        private static bool BindButton(
            Transform root, string path, UnityAction action)
        {
            Button button = root.Find(path)?.GetComponent<Button>();
            if (button == null)
            {
                Debug.LogError(
                    $"IngameUIManager requires Button at Canvas/{path}.");
                return false;
            }
            button.onClick.RemoveListener(action);
            button.onClick.AddListener(action);
            return true;
        }

        public void ShowIngame() =>
            SetActivePanel(ingamePanel, IngamePrimaryPanel.Ingame);

        public void ShowMap()
        {
            SetActivePanel(mapPanel, IngamePrimaryPanel.Map);
            FindFirstObjectByType<MapController>()?.RefreshMap();
        }

        public void ShowEvidence()
        {
            SetActivePanel(evidencePanel, IngamePrimaryPanel.Evidence);
            EvidencePanelController.Instance?.Refresh();
        }

        public void ShowEvidence(string evidenceId)
        {
            SetActivePanel(evidencePanel, IngamePrimaryPanel.Evidence);
            EvidencePanelController.Instance?.Refresh(evidenceId);
        }

        public void OpenSettings()
        {
            if (!IsInitialized || settingsPopup == null || IsSettingsOpen)
            {
                return;
            }
            CloseRuntimeModals();
            SetPrimaryInteraction(false);
            settingsPopup.transform.SetAsLastSibling();
            settingsPopup.SetActive(true);
        }

        public void CloseSettings()
        {
            if (settingsPopup != null)
            {
                settingsPopup.SetActive(false);
            }
            SetPrimaryInteraction(true);
        }

        public void ReturnToLobby()
        {
            DialogueController.Instance?.CancelActiveDialogue();
            GameFlow.Instance?.ResetSession();
            EvidenceInventory.Instance?.Clear();
            SceneManager.LoadScene(LobbySceneName, LoadSceneMode.Single);
        }

        private void SetActivePanel(GameObject panel, IngamePrimaryPanel kind)
        {
            if (!IsInitialized || panel == null)
            {
                return;
            }
            CloseRuntimeModals();
            CloseSettings();
            ingamePanel.SetActive(panel == ingamePanel);
            mapPanel.SetActive(panel == mapPanel);
            evidencePanel.SetActive(panel == evidencePanel);
            ActivePanel = kind;
            LocationLoader.Instance?.SetPresentationVisible(true);
            if (statusHud != null)
            {
                statusHud.SetActive(true);
            }
            SetPrimaryInteraction(true);
        }

        private void CloseRuntimeModals()
        {
            foreach (IRuntimeModalController modal in runtimeModals)
            {
                if (modal != null && modal.IsOpen)
                {
                    modal.Close();
                }
            }
        }

        private void SetPrimaryInteraction(bool enabled)
        {
            GameObject primary = ActivePanel switch
            {
                IngamePrimaryPanel.Ingame => ingamePanel,
                IngamePrimaryPanel.Map => mapPanel,
                IngamePrimaryPanel.Evidence => evidencePanel,
                _ => null
            };
            SetInputState(primary, enabled);
            SetInputState(statusHud, enabled);
        }

        private static void SetInputState(GameObject target, bool enabled)
        {
            if (target == null)
            {
                return;
            }
            CanvasGroup group = EnsureComponent<CanvasGroup>(target);
            group.interactable = enabled;
            group.blocksRaycasts = enabled;
        }
    }
}
```

- [ ] **Step 2: Compile check**

Run: `mcp__UnityMCP__read_console` (types `["error"]`).
Expected: no new compile errors. Note `IRuntimeModalController` is already
declared in `UIManager.cs` inside `namespace Wake.UI` — it is reused as-is,
not redefined.

- [ ] **Step 3: Commit**

```bash
git add Assets/_Project/Code/UI/IngameUIManager.cs Assets/_Project/Code/UI/IngameUIManager.cs.meta
git commit -m "feat: 인게임 씬 전용 UI 매니저(IngameUIManager) 추가"
```

---

## Task 7: `UIManager` implements `IIngameUiHost` and registers with the locator

Purely additive change to the existing, untouched-behaviorally `UIManager`:
implement the shared interface (Task 4) and register/unregister with
`IngameUi`, so `UI Basic Scene` keeps working exactly as today while also
satisfying every repointed call site from Task 8.

**Files:**
- Modify: `Assets/_Project/Code/UI/UIManager.cs`

**Interfaces:**
- Consumes: `IIngameUiHost`, `IngameUi` (Task 4).

- [ ] **Step 1: Add the interface and the missing property**

Change the class declaration
(`Assets/_Project/Code/UI/UIManager.cs:29`) from:

```csharp
    public class UIManager : MonoBehaviour
```

to:

```csharp
    public class UIManager : MonoBehaviour, IIngameUiHost
```

Add a new property near `ActivePanel`
(`Assets/_Project/Code/UI/UIManager.cs:44`):

```csharp
        public bool IsShowingIngamePanel => ActivePanel == UiPrimaryPanel.Ingame;
```

Register with the locator in `Awake`/`OnDestroy`
(`Assets/_Project/Code/UI/UIManager.cs:64-82`):

```csharp
        private void Awake()
        {
            if (Instance != null && Instance != this)
            {
                enabled = false;
                return;
            }

            Instance = this;
            EnsureInitialized();
            IngameUi.Register(this);
        }

        private void OnDestroy()
        {
            if (Instance == this)
            {
                Instance = null;
                IngameUi.Register(null);
            }
        }
```

- [ ] **Step 2: Compile check**

Run: `mcp__UnityMCP__read_console` (types `["error"]`).
Expected: no new compile errors — `UIManager` already has
`ShowIngame()`, `ShowEvidence()`, `ShowEvidence(string)`,
`CloseSettings()`, `IsSettingsOpen`, `OpenRuntimeModalCount` with matching
signatures, so the interface is satisfied with only the one new property
added above.

- [ ] **Step 3: Run existing `UI Basic Scene` tests**

Run: `mcp__UnityMCP__run_tests` (`test_mode: "PlayMode"` and `"EditMode"`).
Expected: all pre-existing tests still PASS (behavior unchanged, purely
additive).

- [ ] **Step 4: Commit**

```bash
git add Assets/_Project/Code/UI/UIManager.cs
git commit -m "feat: UIManager가 IIngameUiHost를 구현하도록 확장"
```

---

## Task 8: Repoint gameplay call sites at `IngameUi.Current`

Replace direct `UIManager.Instance` calls with `IngameUi.Current` in every
file that only needs the shared subset of behavior (everything except
`SaveSlotSelectionController.Confirm()`, handled in Task 8b below, and
`ProductionEndingUIController`, handled in Task 9).

**Files:**
- Modify: `Assets/_Project/Code/Evidence/EvidencePanelController.cs:107`
- Modify: `Assets/_Project/Code/Exploration/EvidenceLocationHotspotOverlay.cs:195,206`
- Modify: `Assets/_Project/Code/UI/ExitInspectionUIController.cs:199-207`
- Modify: `Assets/_Project/Code/UI/FinalAccusationUIController.cs:615-645`
- Modify: `Assets/_Project/Code/UI/MapController.cs:287-315`
- Modify: `Assets/_Project/Code/UI/SettingsController.cs:25`

- [ ] **Step 1: `EvidencePanelController.cs:107`**

Replace:

```csharp
            backButton.onClick.AddListener(() => UIManager.Instance.ShowIngame());
```

with:

```csharp
            backButton.onClick.AddListener(() => IngameUi.Current?.ShowIngame());
```

- [ ] **Step 2: `EvidenceLocationHotspotOverlay.cs:195,206`**

Replace both occurrences of:

```csharp
                UIManager.Instance?.ShowEvidence(spec.EvidenceId);
```

and

```csharp
            UIManager.Instance?.ShowEvidence(spec.EvidenceId);
```

with:

```csharp
                IngameUi.Current?.ShowEvidence(spec.EvidenceId);
```

and

```csharp
            IngameUi.Current?.ShowEvidence(spec.EvidenceId);
```

respectively (same indentation as each original line).

- [ ] **Step 3: `ExitInspectionUIController.cs:199-207`**

Replace:

```csharp
            UIManager ui = UIManager.Instance;
            GameStateManager state = GameStateManager.Instance;
            ProductionDialogueCheckpoint checkpoint = state?.DialogueCheckpoint;
            bool pending = checkpoint != null &&
                           checkpoint.pendingInteractionId == ExitInspectionCatalog.SessionId &&
                           !state.HasCompletedScene(ExitInspectionCatalog.SceneId);
            bool visible = pending && ui?.ActivePanel == UiPrimaryPanel.Ingame &&
                           !ui.IsSettingsOpen && ui.OpenRuntimeModalCount == 0 &&
                           DialogueController.Instance?.IsBusy != true;
```

with:

```csharp
            IIngameUiHost ui = IngameUi.Current;
            GameStateManager state = GameStateManager.Instance;
            ProductionDialogueCheckpoint checkpoint = state?.DialogueCheckpoint;
            bool pending = checkpoint != null &&
                           checkpoint.pendingInteractionId == ExitInspectionCatalog.SessionId &&
                           !state.HasCompletedScene(ExitInspectionCatalog.SceneId);
            bool visible = pending && ui != null && ui.IsShowingIngamePanel &&
                           !ui.IsSettingsOpen && ui.OpenRuntimeModalCount == 0 &&
                           DialogueController.Instance?.IsBusy != true;
```

- [ ] **Step 4: `FinalAccusationUIController.cs:615-645`**

Line 615, `            UIManager.Instance?.ShowEvidence();` (12-space indent,
inside `OpenTheoryBoard()`), is the only `ShowEvidence()` call in this file —
single edit, replace with:

```csharp
            IngameUi.Current?.ShowEvidence();
```

Lines 620 and 631 are both `                UIManager.Instance?.ShowIngame();`
at **16-space** indent (each nested inside an `if` block in
`OpenTheoryBoard()`) — identical text, so use one `replace_all` edit on that
exact 16-space-indented string, replacing both with:

```csharp
                IngameUi.Current?.ShowIngame();
```

Line 645, `            UIManager.Instance?.ShowIngame();` at **12-space**
indent (top level of `ReturnFromTheoryBoard()`), has different indentation
from the pair above and is therefore a distinct, unique string — a separate
single edit, replaced with:

```csharp
            IngameUi.Current?.ShowIngame();
```

- [ ] **Step 5: `MapController.cs:287-315`**

Line 287, `                UIManager.Instance?.ShowIngame();` at **16-space**
indent (nested inside `if (TryLoadAllowedDestination(...))` in
`TryTravelToFreeLocation`), is unique in the file at that indentation — a
single edit, replaced with:

```csharp
                IngameUi.Current?.ShowIngame();
```

Lines 303 and 315 are both `            UIManager.Instance?.ShowIngame();`
at **12-space** indent (top level of `TryTravelToScene`/
`TryEnterDialogueOnlyScene`) — identical text, so use one `replace_all` edit
on that exact 12-space-indented string, replacing both with:

```csharp
            IngameUi.Current?.ShowIngame();
```

- [ ] **Step 6: `SettingsController.cs:25`**

Replace:

```csharp
            closeButton.onClick.AddListener(() => UIManager.Instance.CloseSettings());
```

with:

```csharp
            closeButton.onClick.AddListener(() => IngameUi.Current?.CloseSettings());
```

- [ ] **Step 7: Compile check**

Run: `mcp__UnityMCP__read_console` (types `["error"]`).
Expected: no new compile errors. Each modified file needs
`using Wake.UI;` already present (all six already sit in or reference
`Wake.UI` types today — verify each file's `using` list still resolves
`IngameUi`/`IIngameUiHost`, both in `namespace Wake.UI`, so no new `using`
is required for files already in that namespace; add `using Wake.UI;` to
any that aren't).

- [ ] **Step 8: Run existing `UI Basic Scene` tests**

Run: `mcp__UnityMCP__run_tests` (`test_mode: "PlayMode"` and `"EditMode"`).
Expected: all pre-existing tests still PASS — in `UI Basic Scene`,
`UIManager.Awake()` now registers itself via `IngameUi.Register(this)`
(Task 7), so `IngameUi.Current` resolves to the same `UIManager` instance
these tests already exercise.

- [ ] **Step 9: Commit**

```bash
git add Assets/_Project/Code/Evidence/EvidencePanelController.cs Assets/_Project/Code/Exploration/EvidenceLocationHotspotOverlay.cs Assets/_Project/Code/UI/ExitInspectionUIController.cs Assets/_Project/Code/UI/FinalAccusationUIController.cs Assets/_Project/Code/UI/MapController.cs Assets/_Project/Code/UI/SettingsController.cs
git commit -m "refactor: 인게임 컨트롤러들이 IngameUi 로케이터를 쓰도록 전환"
```

---

## Task 8b: Dual-host bridge in `SaveSlotSelectionController.Confirm()`

`SaveSlotSelectionController.Confirm()`
(`Assets/_Project/Code/UI/RuntimeUiOverhaul.cs:580-591`) currently calls
`UIManager.Instance?.ContinueGameInSlot`/`StartNewGameInSlot` — methods that
only exist on `UIManager` and `LobbyUIManager` (not part of
`IIngameUiHost`, since they are Lobby-only concerns). Call both; whichever
manager is absent in the current scene no-ops.

**Files:**
- Modify: `Assets/_Project/Code/UI/RuntimeUiOverhaul.cs:580-591`

**Interfaces:**
- Consumes: `UIManager.Instance` (existing), `LobbyUIManager.Instance`
  (Task 5).

- [ ] **Step 1: Change `Confirm()`**

Replace:

```csharp
        private void Confirm()
        {
            confirmation.SetActive(false);
            overlay.SetActive(false);
            if (pendingContinue)
            {
                UIManager.Instance?.ContinueGameInSlot(pendingSlot);
            }
            else
            {
                UIManager.Instance?.StartNewGameInSlot(pendingSlot);
            }
        }
```

with:

```csharp
        private void Confirm()
        {
            confirmation.SetActive(false);
            overlay.SetActive(false);
            if (pendingContinue)
            {
                UIManager.Instance?.ContinueGameInSlot(pendingSlot);
                LobbyUIManager.Instance?.ContinueGameInSlot(pendingSlot);
            }
            else
            {
                UIManager.Instance?.StartNewGameInSlot(pendingSlot);
                LobbyUIManager.Instance?.StartNewGameInSlot(pendingSlot);
            }
        }
```

- [ ] **Step 2: Compile check**

Run: `mcp__UnityMCP__read_console` (types `["error"]`).
Expected: no new compile errors.

- [ ] **Step 3: Run existing `UI Basic Scene` tests**

Run the existing `UI Basic Scene`-targeting PlayMode tests (e.g.
`UiBasicSceneEndToEndPlayModeTests`) via `mcp__UnityMCP__run_tests`
(`test_mode: "PlayMode"`).
Expected: PASS — `LobbyUIManager.Instance` is null in that scene, so the
added line is a no-op there.

- [ ] **Step 4: Commit**

```bash
git add Assets/_Project/Code/UI/RuntimeUiOverhaul.cs
git commit -m "feat: SaveSlotSelectionController가 LobbyUIManager도 지원하도록 브릿지 추가"
```

---

## Task 9: Ending → title-screen dual-host bridge

`ProductionEndingUIController.cs:143` currently calls
`UIManager.Instance?.ShowStartScene()`. In the split-scene world there is no
"start scene panel" inside `Ingame Scene` to switch back to — returning to
the title now means unloading `Ingame Scene` and loading `Lobby Scene` fresh
(confirmed with user). Call both; `UIManager.ShowStartScene()` keeps
`UI Basic Scene` working unchanged, `IngameUIManager.ReturnToLobby()` (Task
6) handles the new scene, and only one of the two managers is ever present.

**Files:**
- Modify: `Assets/_Project/Code/UI/ProductionEndingUIController.cs:143`

**Interfaces:**
- Consumes: `UIManager.Instance.ShowStartScene()` (existing),
  `IngameUIManager.Instance.ReturnToLobby()` (Task 6).

- [ ] **Step 1: Change the call site**

Replace:

```csharp
            UIManager.Instance?.ShowStartScene();
```

with:

```csharp
            UIManager.Instance?.ShowStartScene();
            IngameUIManager.Instance?.ReturnToLobby();
```

- [ ] **Step 2: Compile check**

Run: `mcp__UnityMCP__read_console` (types `["error"]`).
Expected: no new compile errors.

- [ ] **Step 3: Run existing `UI Basic Scene` tests**

Run: `mcp__UnityMCP__run_tests` (`test_mode: "PlayMode"`,
`ProductionEndingUIController`-related tests).
Expected: PASS unchanged (`IngameUIManager.Instance` is null in
`UI Basic Scene`).

- [ ] **Step 4: Commit**

```bash
git add Assets/_Project/Code/UI/ProductionEndingUIController.cs
git commit -m "feat: 엔딩 후 Lobby Scene으로 복귀하는 브릿지 추가"
```

---

## Task 10: `GameSystemsBootstrap` script

Marks the persistent-services GameObject `DontDestroyOnLoad` and loads
`Lobby Scene` additively.

**Files:**
- Create: `Assets/_Project/Code/Core/GameSystemsBootstrap.cs`

**Interfaces:**
- Produces: nothing consumed by other C# files — this drives the
  `Bootstrap` scene created in Task 11, and is added directly to the
  `PersistentSystems` GameObject via the Unity Editor (not code).

- [ ] **Step 1: Create the script**

```csharp
using UnityEngine;
using UnityEngine.SceneManagement;

namespace Wake.Core
{
    [DisallowMultipleComponent]
    public sealed class GameSystemsBootstrap : MonoBehaviour
    {
        [SerializeField] private string lobbySceneName = "Lobby Scene";

        private void Awake()
        {
            DontDestroyOnLoad(gameObject);
        }

        private void Start()
        {
            if (!SceneManager.GetSceneByName(lobbySceneName).isLoaded)
            {
                SceneManager.LoadScene(lobbySceneName, LoadSceneMode.Additive);
            }
        }
    }
}
```

- [ ] **Step 2: Compile check**

Run: `mcp__UnityMCP__read_console` (types `["error"]`).
Expected: no new compile errors.

- [ ] **Step 3: Commit**

```bash
git add Assets/_Project/Code/Core/GameSystemsBootstrap.cs Assets/_Project/Code/Core/GameSystemsBootstrap.cs.meta
git commit -m "feat: 씬 전환에도 살아남는 GameSystemsBootstrap 추가"
```

---

## Task 11: Build the `Bootstrap` scene

Create a new scene holding a `PersistentSystems` GameObject carrying only
the six cross-scene services (`GameStateManager`, `GameFlow`, `AudioManager`,
`DialogueDatabase`, `DialogueController`, `EvidenceInventory`), built by
duplicating the existing `GameSystems` object from `UI Basic Scene` (to
faithfully copy its serialized field values) and stripping everything
UI-bound. `UI Basic Scene` itself is never saved/modified in this task.

**Files:**
- Create: `Assets/_Project/Scenes/Bootstrap.unity`

- [ ] **Step 1: Create the new empty scene**

`mcp__UnityMCP__manage_scene` `action: "create"`, `name: "Bootstrap"`,
`path: "Assets/_Project/Scenes"`.
Expected: scene created and made active.

- [ ] **Step 2: Load `UI Basic Scene` additively (read-only source)**

`mcp__UnityMCP__manage_scene` `action: "load"`,
`scene_path: "Assets/_Project/Scenes/UI/UI Basic Scene.unity"`,
`additive: true`.
Expected: both `Bootstrap` and `UI Basic Scene` are loaded; `Bootstrap`
remains the active scene (verify with `action: "get_active"` — if
`UI Basic Scene` became active, call `action: "set_active_scene"`,
`scene_name: "Bootstrap"`).

- [ ] **Step 3: Duplicate `GameSystems` and move the duplicate into `Bootstrap`**

`mcp__UnityMCP__manage_gameobject` duplicate action (check the exact action
name via `ToolSearch("select:mcp__UnityMCP__manage_gameobject")` if it
differs from `"duplicate"`) targeting the `GameSystems` object found via
`mcp__UnityMCP__find_gameobjects` (`search_term: "GameSystems"`,
`search_method: "by_name"`) inside `UI Basic Scene`.
Then `mcp__UnityMCP__manage_scene` `action: "move_to_scene"`, `target:
<duplicate instance ID>`, `scene_name: "Bootstrap"`.
Rename the moved duplicate to `PersistentSystems`.
Expected: `PersistentSystems` now lives under the `Bootstrap` scene root;
the original `GameSystems` in `UI Basic Scene` is untouched.

- [ ] **Step 4: Strip UI-bound components from `PersistentSystems`**

Remove from `PersistentSystems`: `UIManager`, `EvidencePanelController`,
`SettingsController`, `ToastController`, `MapController`, `ClickRouter`,
`ExitPuzzle`, `LocationLoader`. Keep: `GameStateManager`, `GameFlow`,
`AudioManager`, `DialogueDatabase`, `DialogueController`,
`EvidenceInventory`.
Verify via `mcp__UnityMCP__manage_asset` `action: "get_components"` (or
`find_gameobjects` → component list) on `PersistentSystems`.
Expected: exactly those six components remain.

- [ ] **Step 5: Add `GameSystemsBootstrap`**

Add the `GameSystemsBootstrap` component (Task 10) to `PersistentSystems`.
Expected: component present with `lobbySceneName` = `"Lobby Scene"`.

- [ ] **Step 6: Unload the read-only source scene without saving it**

`mcp__UnityMCP__manage_scene` `action: "close_scene"`,
`scene_name: "UI Basic Scene"` (do **not** save it — the original
`GameSystems` was never touched).
Expected: `UI Basic Scene` no longer loaded.

- [ ] **Step 7: Save the `Bootstrap` scene**

`mcp__UnityMCP__manage_scene` `action: "save"`.
Expected: `Assets/_Project/Scenes/Bootstrap.unity` written to disk.

- [ ] **Step 8: Verify `UI Basic Scene` is untouched**

Run: `git status --short -- "Assets/_Project/Scenes/UI/UI Basic Scene.unity"`
Expected: empty output.

- [ ] **Step 9: Commit**

```bash
git add "Assets/_Project/Scenes/Bootstrap.unity" "Assets/_Project/Scenes/Bootstrap.unity.meta"
git commit -m "feat: 영속 서비스용 Bootstrap 씬 추가"
```

---

## Task 12: Build the `Lobby Scene`

Duplicate `UI Basic Scene` into `Lobby Scene`, prune to
`Ingame`/`Map`/`Evidence`/`Status HUD` removed, wire `LobbyUIManager` +
`LobbyRevealSequence`.

**Files:**
- Create: `Assets/_Project/Scenes/Lobby Scene.unity`

- [ ] **Step 1: Duplicate the scene asset**

`mcp__UnityMCP__manage_asset` `action: "duplicate"`,
`path: "Assets/_Project/Scenes/UI/UI Basic Scene.unity"`,
`destination: "Assets/_Project/Scenes/Lobby Scene.unity"`.

- [ ] **Step 2: Open the duplicate**

`mcp__UnityMCP__manage_scene` `action: "load"`,
`scene_path: "Assets/_Project/Scenes/Lobby Scene.unity"`.
Expected: `get_hierarchy` (`max_depth: 1`) shows the same 6 roots as
`UI Basic Scene` (`Main Camera`, `Global Light 2D`, `Canvas`, `EventSystem`,
`GameSystems`, `Water`).

- [ ] **Step 3: Remove the local `GameSystems`**

Delete the `GameSystems` root object from this scene (persistent services
now come from `Bootstrap`).
Expected: `Lobby Scene` no longer has a `GameSystems` root.

- [ ] **Step 4: Remove unneeded panels from `Canvas`**

Under `Canvas`, delete `Ingame`, `Map`, `Evidence`, and `Status HUD`. Keep
`StartScene` and `Settings Popup`.
Expected: `Canvas` has exactly 2 children: `StartScene`, `Settings Popup`.

- [ ] **Step 5: Add `LobbyUIManager` to a new `Lobby` root object**

Create a new empty GameObject `Lobby` at the scene root, add the
`LobbyUIManager` component (Task 5).
Expected: entering Play Mode triggers `LobbyUIManager.EnsureInitialized()`
via `Awake`, binding `StartScene`/`Settings Popup`/buttons with no console
errors (verified in Task 16).

- [ ] **Step 6: Verify the save-slot panel exists for the reveal**

`LobbyUIManager.EnsureInitialized()` (Task 5) already calls
`revealSequence.Configure(...)` passing
`saveSlotSelection.GetComponent<RectTransform>()` as the reveal group — the
`StartScene` object itself, matching `UIManager`'s existing pattern
(`Assets/_Project/Code/UI/UIManager.cs:180-181`). Confirm via
`find_gameobjects` (`search_method: "by_path"`, `"Canvas/StartScene"`) that
it exists with a `RectTransform`.
Expected: present.

- [ ] **Step 7: Save the scene**

`mcp__UnityMCP__manage_scene` `action: "save"`.

- [ ] **Step 8: Commit**

```bash
git add "Assets/_Project/Scenes/Lobby Scene.unity" "Assets/_Project/Scenes/Lobby Scene.unity.meta"
git commit -m "feat: World Space 로비 씬 구성 (UI Basic Scene 기반)"
```

---

## Task 13: Build the `Ingame Scene`

Duplicate `UI Basic Scene` into `Ingame Scene`, prune `StartScene`/save-slot/
title/`Water`, set Canvas to Screen Space - Overlay at 2880x1800, wire
`IngameUIManager`.

**Files:**
- Create: `Assets/_Project/Scenes/Ingame Scene.unity`

- [ ] **Step 1: Duplicate the scene asset**

`mcp__UnityMCP__manage_asset` `action: "duplicate"`,
`path: "Assets/_Project/Scenes/UI/UI Basic Scene.unity"`,
`destination: "Assets/_Project/Scenes/Ingame Scene.unity"`.

- [ ] **Step 2: Open the duplicate**

`mcp__UnityMCP__manage_scene` `action: "load"`,
`scene_path: "Assets/_Project/Scenes/Ingame Scene.unity"`.

- [ ] **Step 3: Remove `GameSystems` and `Water`**

Delete both root objects.
Expected: scene roots are now `Main Camera`, `Global Light 2D`, `Canvas`,
`EventSystem`.

- [ ] **Step 4: Remove `StartScene` from `Canvas`, keep the rest**

Delete `Canvas/StartScene`. Keep `Ingame`, `Map`, `Evidence`,
`Settings Popup`, `Status HUD`.

- [ ] **Step 5: Set Canvas to Screen Space - Overlay with the 2880x1800 scaler**

On `Canvas`'s `Canvas` component, set `renderMode` to `ScreenSpaceOverlay`
(0). On its `CanvasScaler` component: `uiScaleMode` = `ScaleWithScreenSize`
(already 1), `referenceResolution` = `{2880, 1800}` (already set on disk —
confirm unchanged), `matchWidthOrHeight` = `0.5` (already set). Reset the
`Canvas` RectTransform's `localScale` to `{1, 1, 1}` (World Space's `0.0056`
scale no longer applies once the render mode is Overlay).
Expected: `Canvas.renderMode == ScreenSpaceOverlay`,
`CanvasScaler.referenceResolution == (2880, 1800)`.

- [ ] **Step 6: Add `IngameUIManager` to a new `Ingame Systems` root object**

Create a new empty GameObject `Ingame Systems` at the scene root, add the
`IngameUIManager` component (Task 6).
Expected: entering Play Mode triggers `IngameUIManager.EnsureInitialized()`,
showing the `Ingame` panel immediately with no console errors (verified in
Task 16).

- [ ] **Step 7: Save the scene**

`mcp__UnityMCP__manage_scene` `action: "save"`.

- [ ] **Step 8: Commit**

```bash
git add "Assets/_Project/Scenes/Ingame Scene.unity" "Assets/_Project/Scenes/Ingame Scene.unity.meta"
git commit -m "feat: Screen Space Overlay 인게임 씬 구성 (2880x1800)"
```

---

## Task 14: Register scenes in Build Settings

Add `Bootstrap`, `Lobby Scene`, `Ingame Scene` to Build Settings (currently
empty). Keep `UI Basic Scene` registered too, since existing tests target
it.

**Files:**
- Modify: `ProjectSettings/EditorBuildSettings.asset` (via tool, not hand
  edit)

- [ ] **Step 1: Add scenes to Build Settings**

`mcp__UnityMCP__manage_build` `action: "scenes"` (fetch exact parameter
names via `ToolSearch("select:mcp__UnityMCP__manage_build")` if they
differ) to add, in order:
1. `Assets/_Project/Scenes/Bootstrap.unity`
2. `Assets/_Project/Scenes/Lobby Scene.unity`
3. `Assets/_Project/Scenes/Ingame Scene.unity`
4. `Assets/_Project/Scenes/UI/UI Basic Scene.unity`

- [ ] **Step 2: Verify**

`mcp__UnityMCP__manage_scene` `action: "get_build_settings"`.
Expected: all four scenes listed, all enabled.

- [ ] **Step 3: Commit**

```bash
git add ProjectSettings/EditorBuildSettings.asset
git commit -m "chore: Bootstrap/Lobby/Ingame 씬을 빌드 세팅에 등록"
```

---

## Task 15: End-to-end smoke test

Play through `Bootstrap` → `Lobby Scene` → slot pick → `Ingame Scene`, then
trigger the ending path back to `Lobby Scene`, confirming no console errors
at any point.

- [ ] **Step 1: Open `Bootstrap` and enter Play Mode**

`mcp__UnityMCP__manage_scene` `action: "load"`,
`scene_path: "Assets/_Project/Scenes/Bootstrap.unity"`.
`mcp__UnityMCP__manage_editor` `action: "play"`.

- [ ] **Step 2: Confirm Lobby loaded and title screen visible**

`mcp__UnityMCP__manage_scene` `action: "get_loaded_scenes"`.
Expected: both `Bootstrap` and `Lobby Scene` loaded.
`mcp__UnityMCP__read_console` (`types: ["error"]`). Expected: no errors.

- [ ] **Step 3: Click "시작하기" and confirm the reveal + slot picker**

Locate "Start Game Btn" via `find_gameobjects` and invoke its click (via
`execute_code` calling `button.onClick.Invoke()`). Wait ~0.5s
(`LobbyRevealSequence`'s `Duration`), then read `anchoredPosition.y` of the
title panel and reveal group via `execute_code`, and confirm the
slot-selection overlay is active.
Expected: title panel `anchoredPosition.y ≈ 1800`, reveal group
`anchoredPosition.y ≈ 0`, slot-selection overlay `activeSelf == true`.

- [ ] **Step 4: Pick a slot and confirm the scene switch to Ingame**

Simulate clicking a slot button, then the confirm button. Wait one frame.
`mcp__UnityMCP__manage_scene` `action: "get_active"`.
Expected: active scene is `Ingame Scene`.
`mcp__UnityMCP__read_console` (`types: ["error"]`). Expected: no errors.

- [ ] **Step 5: Confirm gameplay UI navigation still works via `IngameUi`**

Via `execute_code`, invoke the "Ingame/Map Btn" click and confirm
`IngameUIManager.Instance.ActivePanel == IngamePrimaryPanel.Map`; invoke
"Map/Back Btn" and confirm it returns to `IngamePrimaryPanel.Ingame`.
Expected: both transitions succeed with no console errors.

- [ ] **Step 6: Confirm the ending → Lobby round trip**

Via `execute_code`, call `IngameUIManager.Instance.ReturnToLobby()`
directly (bypassing the full ending sequence, which requires full game
progression). Wait one frame.
`mcp__UnityMCP__manage_scene` `action: "get_active"`.
Expected: active scene is `Lobby Scene` again; title screen visible;
`mcp__UnityMCP__read_console` (`types: ["error"]`) shows no errors.

- [ ] **Step 7: Stop Play Mode**

`mcp__UnityMCP__manage_editor` `action: "stop"`.

- [ ] **Step 8: Final full-repo verification**

Run: `git status --short -- "Assets/_Project/Scenes/UI/UI Basic Scene.unity"`
Expected: empty (still untouched).
Run the full EditMode + PlayMode suite via `mcp__UnityMCP__run_tests`.
Expected: all pre-existing tests still PASS, plus every new test added in
Tasks 1-4.

- [ ] **Step 9: Commit (only if something legitimately changed)**

```bash
git status --short
# only commit if Step 3-6's manual click-through left a scene file dirty
# with an intentional change (e.g. a field you had to fix live) — do not
# commit stray Play Mode artifacts.
```
