# Save Slot → In-Game White Fade Transition Implementation Plan

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:subagent-driven-development (recommended) or superpowers:executing-plans to implement this plan task-by-task. Steps use checkbox (`- [ ]`) syntax for tracking.

**Goal:** Add a white full-screen fade that covers the existing slot-selection-to-in-game animation and reveals the in-game screen once it's loaded.

**Architecture:** `ScreenFadeTransition` (already used by `MapController` for travel transitions) gets two new general-purpose methods, `FadeIn(duration, color)` and `FadeOut(duration)`, alongside its existing all-in-one `Run(...)`. `SaveSlotSelectionController.EnterGameRoutine` (the coroutine that plays when a save slot is confirmed) starts a white `FadeIn` in parallel with its existing animation at the start, and `yield return`s a `FadeOut` right after it switches to the in-game panel.

**Tech Stack:** Unity 6000.3, C#, `UnityEngine.UI` (`CanvasGroup`/`Image`), coroutines.

## Global Constraints

- Reuses `ScreenFadeTransition`, not a new component (spec: Goals).
- `Run(...)`'s existing behavior/signature for `MapController` must be unchanged - color parameter defaults to the current dark navy (spec: Goals, Non-goals).
- Applies to both new-game and continue-game slot selection (spec: Goals - confirmed by user).
- White fade-in duration matches the existing closing animation total, referenced via the existing `RevealDuration` constant (5.2s) rather than a new hardcoded duplicate value. Fade-out is a fixed 0.4s (spec: Design, user-confirmed).
- No new automated tests - this codebase has no test precedent for animation-timing MonoBehaviours like this (`LobbyBackdropController` is the closest match, also untested). Verify manually in the Editor (spec: Testing).

---

### Task 1: `ScreenFadeTransition.FadeIn` / `FadeOut`

**Files:**
- Modify: `Assets/_Project/Code/UI/ScreenFadeTransition.cs` (full rewrite of the file's internals; public API grows, `Run(...)` signature unchanged)

**Interfaces:**
- Produces:
  - `ScreenFadeTransition.FadeIn(float duration, Color color) -> Coroutine` - ensures the overlay exists, sets its color, activates it, fades `CanvasGroup.alpha` from its current value to `1`, blocks raycasts while up.
  - `ScreenFadeTransition.FadeOut(float duration) -> Coroutine` - fades alpha to `0`, then deactivates the overlay and stops blocking raycasts.
  - `ScreenFadeTransition.Run(...)` - same public signature as before, now implemented on top of `FadeIn`/`FadeOut` internally.

- [ ] **Step 1: Replace the file with the extended implementation**

Replace the full contents of `Assets/_Project/Code/UI/ScreenFadeTransition.cs` with:

```csharp
using System;
using System.Collections;
using UnityEngine;
using UnityEngine.UI;

namespace Wake.UI
{
    [DisallowMultipleComponent]
    public sealed class ScreenFadeTransition : MonoBehaviour
    {
        private static readonly Color DefaultColor = new Color32(3, 8, 18, 255);

        private CanvasGroup group;
        private Image blocker;
        private Coroutine transition;

        public bool IsRunning => transition != null;

        public static ScreenFadeTransition Ensure()
        {
            GameObject canvas = GameObject.Find("Canvas");
            if (canvas == null)
                return null;
            ScreenFadeTransition existing =
                canvas.GetComponent<ScreenFadeTransition>();
            return existing != null
                ? existing
                : canvas.AddComponent<ScreenFadeTransition>();
        }

        public bool Run(
            Action midpoint,
            float fadeOutSeconds = .25f,
            float fadeInSeconds = .25f,
            Action started = null,
            Action completed = null)
        {
            if (transition != null)
                return false;

            started?.Invoke();
            transition = StartCoroutine(RunSequence(
                midpoint,
                fadeOutSeconds,
                fadeInSeconds,
                completed));
            return true;
        }

        // Fades the overlay to fully covering the screen. Callers that
        // start this in parallel with some other animation (rather than
        // via Run(...)) are responsible for eventually calling FadeOut.
        public Coroutine FadeIn(float duration, Color color)
        {
            EnsureOverlay();
            blocker.color = color;
            blocker.gameObject.SetActive(true);
            blocker.transform.SetAsLastSibling();
            group.blocksRaycasts = true;
            return StartCoroutine(Fade(group.alpha, 1f, duration));
        }

        public Coroutine FadeOut(float duration)
        {
            EnsureOverlay();
            return StartCoroutine(FadeOutSequence(duration));
        }

        private IEnumerator FadeOutSequence(float duration)
        {
            yield return Fade(group.alpha, 0f, duration);
            group.blocksRaycasts = false;
            blocker.gameObject.SetActive(false);
        }

        private void EnsureOverlay()
        {
            if (group != null)
                return;

            GameObject overlay = new(
                "Screen Travel Fade",
                typeof(RectTransform),
                typeof(CanvasRenderer),
                typeof(Image),
                typeof(CanvasGroup));
            overlay.transform.SetParent(transform, false);
            RectTransform rect =
                overlay.GetComponent<RectTransform>();
            rect.anchorMin = Vector2.zero;
            rect.anchorMax = Vector2.one;
            rect.offsetMin = Vector2.zero;
            rect.offsetMax = Vector2.zero;
            blocker = overlay.GetComponent<Image>();
            blocker.color = DefaultColor;
            group = overlay.GetComponent<CanvasGroup>();
            group.alpha = 0f;
            group.interactable = false;
            group.blocksRaycasts = false;
            overlay.SetActive(false);
        }

        private IEnumerator RunSequence(
            Action midpoint,
            float fadeOutSeconds,
            float fadeInSeconds,
            Action completed)
        {
            yield return FadeIn(fadeOutSeconds, DefaultColor);
            try
            {
                midpoint?.Invoke();
            }
            catch (Exception exception)
            {
                Debug.LogException(exception);
            }
            blocker.transform.SetAsLastSibling();
            yield return FadeOut(fadeInSeconds);
            transition = null;
            completed?.Invoke();
        }

        private IEnumerator Fade(float from, float to, float duration)
        {
            float safeDuration = Mathf.Max(0f, duration);
            float elapsed = 0f;
            group.alpha = from;
            while (elapsed < safeDuration)
            {
                elapsed += Time.unscaledDeltaTime;
                group.alpha = Mathf.Lerp(
                    from,
                    to,
                    Mathf.SmoothStep(
                        0f,
                        1f,
                        Mathf.Clamp01(elapsed / safeDuration)));
                yield return null;
            }
            group.alpha = to;
        }
    }
}
```

- [ ] **Step 2: Compile check**

Run `mcp__UnityMCP__refresh_unity` then `mcp__UnityMCP__read_console` (types: `["error"]`).
Expected: no errors.

- [ ] **Step 3: Manually verify `MapController`'s existing usage is unaffected**

Enter Play mode (`mcp__UnityMCP__manage_editor` action `play`), reach a state where a map travel transition can be triggered (or call `MapController`'s travel-confirmation path directly via `mcp__UnityMCP__execute_code` if reaching it through normal play is slow), trigger one, and confirm the dark-navy fade still plays exactly as before (visually, or via `read_console` showing no errors during the transition).
Expected: no change in `MapController`'s travel-transition look or behavior.

- [ ] **Step 4: Commit**

```bash
git add Assets/_Project/Code/UI/ScreenFadeTransition.cs
git commit -m "feat: add FadeIn/FadeOut to ScreenFadeTransition"
```

---

### Task 2: Wire the white fade into `EnterGameRoutine`

**Files:**
- Modify: `Assets/_Project/Code/UI/RuntimeUiOverhaul.cs:1118-1196` (the `EnterGameRoutine` method on `SaveSlotSelectionController`)

**Interfaces:**
- Consumes: `ScreenFadeTransition.FadeIn(float, Color) -> Coroutine`, `ScreenFadeTransition.FadeOut(float) -> Coroutine` (Task 1). `RevealDuration` (existing private const, `RuntimeUiOverhaul.cs:604`, value `5.2f`) - already in scope inside this class, no import needed.
- Produces: nothing consumed by a later task - this is the final task in this plan.

- [ ] **Step 1: Start the white fade-in at the top of `EnterGameRoutine`**

In `Assets/_Project/Code/UI/RuntimeUiOverhaul.cs`, find:

```csharp
        private IEnumerator EnterGameRoutine(int slot, bool continuing)
        {
            ingamePanel = ingamePanel != null
```

Replace with:

```csharp
        private IEnumerator EnterGameRoutine(int slot, bool continuing)
        {
            ScreenFadeTransition.Ensure()?.FadeIn(RevealDuration, Color.white);

            ingamePanel = ingamePanel != null
```

- [ ] **Step 2: Fade back out after switching to the in-game panel**

In the same method, find:

```csharp
            revealRoutine = null;
            if (continuing)
            {
                UIManager.Instance?.ContinueGameInSlot(slot);
            }
            else
            {
                UIManager.Instance?.StartNewGameInSlot(slot);
            }
        }
```

Replace with:

```csharp
            revealRoutine = null;
            if (continuing)
            {
                UIManager.Instance?.ContinueGameInSlot(slot);
            }
            else
            {
                UIManager.Instance?.StartNewGameInSlot(slot);
            }

            ScreenFadeTransition fadeOverlay = ScreenFadeTransition.Ensure();
            if (fadeOverlay != null)
            {
                yield return fadeOverlay.FadeOut(0.4f);
            }
        }
```

- [ ] **Step 3: Compile check**

Run `mcp__UnityMCP__refresh_unity` then `mcp__UnityMCP__read_console` (types: `["error"]`).
Expected: no errors.

- [ ] **Step 4: Manually verify both new-game and continue-game slot selection**

Enter Play mode. Reach the save-slot screen (title -> lobby dive -> slot list, or jump there directly via `mcp__UnityMCP__execute_code` calling the same path the "시작" button uses if that's faster). Select an empty slot (new game) and confirm: the screen should go white while the existing slot-exit/water-surface/in-game-rise animation plays (~5.2s), then fade out over ~0.4s to reveal the in-game screen. Repeat selecting a slot with an existing save (continue game) and confirm the same behavior.
Expected: white covers the screen for the duration of the existing animation in both cases, then fades out cleanly onto the in-game screen; no console errors (`mcp__UnityMCP__read_console`, types `["error"]`).

- [ ] **Step 5: Commit**

```bash
git add Assets/_Project/Code/UI/RuntimeUiOverhaul.cs
git commit -m "feat: fade to white over the save-slot-to-in-game transition"
```
