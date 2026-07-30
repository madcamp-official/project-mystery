# Atrium Click-to-Talk Implementation Plan

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:subagent-driven-development (recommended) or superpowers:executing-plans to implement this plan task-by-task. Steps use checkbox (`- [ ]`) syntax for tracking.

**Goal:** Replace D1-01's (Atrium) repeatable choice-button interview menu with direct click-on-character interaction, matching every other investigation scene, and block map travel out of the Atrium until all four suspects have been interviewed.

**Architecture:** `ProductionDialogueFlow` already tracks which repeatable ("_FREE") choices are resolved; we expose that state and add a way to resolve a specific one by character id. `DialogueController` uses that to suspend to exploration instead of drawing a choice menu, and to route a world-character click straight into that character's branch. `ScenePresencePresentationPolicy` gets a one-line data fix so Claire/Owen count as clickable focus participants. `SceneTravelPolicy` gets a small location→required-scene lookup so `MapController` can block leaving the Atrium and show an in-character monologue instead.

**Tech Stack:** Unity 2022+ (C#), NUnit EditMode tests, existing CSV-driven dialogue system (`Under_the_Horizon_Dialogue_KR.csv`).

## Global Constraints

- Only D1-01 uses a repeatable (`_FREE`) multi-target choice group today; no other scene's behavior may change.
- The two monologue lines must be used verbatim, picked at random each blocked attempt: "다른 사람의 이야기도 들어보자." / "아직은 더 탐문을 할 때야."
- Reuse the existing checkpoint/`EndDialogue()` suspend mechanism rather than inventing new persistence.
- Spec: `docs/superpowers/specs/2026-07-30-atrium-click-to-talk-design.md`

---

## Task 1: `ProductionDialogueFlow` world-selection API

**Files:**
- Modify: `Assets/_Project/Code/Narrative/ProductionDialogueRuntime.cs:273` (add property near `IsAwaitingChoice`), `:456` (add method right after `SelectChoice`)
- Test: `Assets/_Project/Tests/EditMode/ProductionDialogueRuntimeTests.cs` (append before the `CompleteScene` helper at line 556)

**Interfaces:**
- Produces: `ProductionDialogueFlow.IsAwaitingWorldSelection` (bool), `ProductionDialogueFlow.SelectFreeChoiceForCharacter(string characterId)` (bool) — both consumed by Task 3.

- [ ] **Step 1: Write the failing tests**

Add to `Assets/_Project/Tests/EditMode/ProductionDialogueRuntimeTests.cs`, immediately before the `private static void CompleteScene(...)` method:

```csharp
        [Test]
        public void D101_ClickingEachSuspectByNameCompletesTheAtriumScene()
        {
            host = new GameObject("AtriumWorldSelection");
            GameStateManager state = host.AddComponent<GameStateManager>();
            state.RecordCompletedScene("P-03");
            var flow = new ProductionDialogueFlow(records, null, state);

            Assert.That(
                flow.StartScene("D1-01"),
                Is.True,
                string.Join("\n", flow.Warnings));
            while (!flow.IsAwaitingWorldSelection && !flow.IsComplete)
            {
                flow.Advance();
            }

            Assert.That(flow.IsAwaitingWorldSelection, Is.True);
            Assert.That(
                flow.Choices.Select(choice => choice.ChoiceId),
                Is.EquivalentTo(new[]
                {
                    "D1-01_CLAIRE",
                    "D1-01_MARCUS",
                    "D1-01_HELENA",
                    "D1-01_OWEN"
                }));

            foreach (string character in
                     new[] { "OWEN", "HELENA", "MARCUS", "CLAIRE" })
            {
                Assert.That(
                    flow.SelectFreeChoiceForCharacter(character),
                    Is.True,
                    character);
                Assert.That(flow.Current.Speaker, Is.EqualTo(character));

                while (!flow.IsAwaitingChoice && !flow.IsComplete)
                {
                    flow.Advance();
                }
            }

            Assert.That(flow.IsComplete, Is.True);
            Assert.That(flow.IsSceneCompleted("D1-01"), Is.True);
            Assert.That(state.HasFlag("met_claire"), Is.True);
            Assert.That(state.HasFlag("met_marcus"), Is.True);
            Assert.That(state.HasFlag("met_helena"), Is.True);
            Assert.That(state.HasFlag("met_owen"), Is.True);
            Assert.That(flow.Warnings, Is.Empty);
        }

        [Test]
        public void D101_ReselectingAnAlreadyInterviewedSuspectFails()
        {
            host = new GameObject("AtriumWorldSelectionRepeat");
            GameStateManager state = host.AddComponent<GameStateManager>();
            state.RecordCompletedScene("P-03");
            var flow = new ProductionDialogueFlow(records, null, state);

            flow.StartScene("D1-01");
            while (!flow.IsAwaitingWorldSelection)
            {
                flow.Advance();
            }

            Assert.That(flow.SelectFreeChoiceForCharacter("CLAIRE"), Is.True);
            while (!flow.IsAwaitingChoice && !flow.IsComplete)
            {
                flow.Advance();
            }

            Assert.That(
                flow.SelectFreeChoiceForCharacter("CLAIRE"),
                Is.False,
                "Claire was already interviewed and should not be re-selectable.");
        }
```

- [ ] **Step 2: Run tests to verify they fail**

Run (Unity Test Runner, EditMode, or): `Unity -batchmode -runTests -testPlatform EditMode -testFilter ProductionDialogueRuntimeTests`
Expected: FAIL — `'ProductionDialogueFlow' does not contain a definition for 'IsAwaitingWorldSelection'` / `'SelectFreeChoiceForCharacter'` (compile error).

- [ ] **Step 3: Implement `IsAwaitingWorldSelection`**

In `Assets/_Project/Code/Narrative/ProductionDialogueRuntime.cs`, right after the existing:

```csharp
        public bool IsAwaitingChoice => Choices.Count > 0;
```

add:

```csharp
        public bool IsAwaitingWorldSelection =>
            IsAwaitingChoice && repeatableChoiceStart >= 0;
```

- [ ] **Step 4: Implement `SelectFreeChoiceForCharacter`**

Right after the closing brace of the existing `SelectChoice(int choiceIndex)` method (the method that ends just before `private void PresentCurrent()`), add:

```csharp
        public bool SelectFreeChoiceForCharacter(string characterId)
        {
            if (!IsAwaitingWorldSelection || string.IsNullOrWhiteSpace(characterId))
            {
                return false;
            }

            string suffix = "_" + characterId.Trim();
            for (int i = 0; i < Choices.Count; i++)
            {
                if (Choices[i].ChoiceId != null &&
                    Choices[i].ChoiceId.EndsWith(
                        suffix,
                        StringComparison.OrdinalIgnoreCase))
                {
                    return SelectChoice(i);
                }
            }

            return false;
        }
```

- [ ] **Step 5: Run tests to verify they pass**

Run: `Unity -batchmode -runTests -testPlatform EditMode -testFilter ProductionDialogueRuntimeTests`
Expected: PASS (all tests in the file, including the two new ones).

- [ ] **Step 6: Commit**

```bash
git add Assets/_Project/Code/Narrative/ProductionDialogueRuntime.cs Assets/_Project/Tests/EditMode/ProductionDialogueRuntimeTests.cs
git commit -m "feat: let ProductionDialogueFlow resolve free choices by character id"
```

---

## Task 2: Fix D1-01 focus-participant list

**Files:**
- Modify: `Assets/_Project/Code/Exploration/ScenePresencePresentationPolicy.cs:44`
- Test: Create `Assets/_Project/Tests/EditMode/ScenePresencePresentationPolicyTests.cs`

**Interfaces:**
- Consumes: nothing new.
- Produces: nothing new (data fix only) — Task 4 relies on Claire/Marcus/Helena/Owen being `IsFocusParticipant` for D1-01.

- [ ] **Step 1: Write the failing test**

Create `Assets/_Project/Tests/EditMode/ScenePresencePresentationPolicyTests.cs`:

```csharp
using System.Linq;
using NUnit.Framework;
using Wake.Exploration;

namespace Wake.Tests
{
    public sealed class ScenePresencePresentationPolicyTests
    {
        [Test]
        public void Atrium_MarksAllFourSuspectsAsFocusParticipants()
        {
            Assert.That(
                ScenePresenceCatalog.TryGet(
                    "D1-01",
                    out ScenePresenceRecord scene),
                Is.True);

            string[] focusParticipants =
                ScenePresencePresentationPolicy
                    .SelectVisible(scene, "ATRIUM", visibleLimit: 5)
                    .Where(character => character.IsFocusParticipant)
                    .Select(character => character.CharacterId)
                    .OrderBy(id => id)
                    .ToArray();

            Assert.That(
                focusParticipants,
                Is.EqualTo(new[] { "CLAIRE", "HELENA", "MARCUS", "OWEN" }));
        }
    }
}
```

- [ ] **Step 2: Run test to verify it fails**

Run: `Unity -batchmode -runTests -testPlatform EditMode -testFilter ScenePresencePresentationPolicyTests`
Expected: FAIL — actual result is `{ "HELENA", "MARCUS" }` (Evelyn isn't physically at ATRIUM in D1-01 so she's filtered out by location; Claire/Owen are missing from the priority list).

- [ ] **Step 3: Fix the data**

In `Assets/_Project/Code/Exploration/ScenePresencePresentationPolicy.cs`, change:

```csharp
                    ["D1-01"] = C("EVELYN", "MARCUS", "HELENA"),
```

to:

```csharp
                    ["D1-01"] = C("CLAIRE", "MARCUS", "HELENA", "OWEN"),
```

- [ ] **Step 4: Run test to verify it passes**

Run: `Unity -batchmode -runTests -testPlatform EditMode -testFilter ScenePresencePresentationPolicyTests`
Expected: PASS

- [ ] **Step 5: Commit**

```bash
git add Assets/_Project/Code/Exploration/ScenePresencePresentationPolicy.cs Assets/_Project/Tests/EditMode/ScenePresencePresentationPolicyTests.cs
git commit -m "fix: mark all four Atrium suspects as clickable focus participants"
```

---

## Task 3: `DialogueController.TalkToWorldCharacter` + suspend-to-exploration

**Files:**
- Modify: `Assets/_Project/Code/Narrative/DialogueController.cs`
  - `:51` (add field after `pendingInvestigationTitle`)
  - `:668-672` (suspend branch inside `RenderProduction()`)
  - `:520` (add `TalkToWorldCharacter` after `CanStartProductionScene`)
  - `:973-974` (clear the new field in `EndDialogue()`)
  - `:529-530` (clear the new field in `CancelActiveDialogue()`)

**Interfaces:**
- Consumes: `ProductionDialogueFlow.IsAwaitingWorldSelection`, `ProductionDialogueFlow.SelectFreeChoiceForCharacter` (Task 1).
- Produces: `DialogueController.TalkToWorldCharacter(string sceneId, string characterId) : bool` — consumed by Task 4.

There is no isolated unit test for this task: `DialogueController` is a `MonoBehaviour` singleton wired to a live Canvas hierarchy (see `BindUi()`), so it's only exercisable through a running scene. Its logic is a thin, mostly line-for-line reuse of the already-tested `ProductionDialogueFlow` API from Task 1 (`IsAwaitingWorldSelection`, `SelectFreeChoiceForCharacter`) plus the already-existing `EndDialogue()`/`RestoreProductionScene()`/`StartProductionScene()` methods (unchanged). Correctness here is verified by:
- the compiler (no new types, only calls into already-tested methods),
- the manual PlayMode smoke pass in Task 7, which is where this method is actually exercised end-to-end.

- [ ] **Step 1: Add the pending-character field**

In `Assets/_Project/Code/Narrative/DialogueController.cs`, right after:

```csharp
        private string pendingInvestigationTitle = string.Empty;
```

add:

```csharp
        private string pendingWorldCharacterId = string.Empty;
```

- [ ] **Step 2: Suspend to exploration on a repeatable choice instead of rendering the menu**

In `RenderProduction()`, right after this existing block:

```csharp
            if (productionFlow == null || productionFlow.IsComplete)
            {
                EndDialogue();
                return;
            }
```

add:

```csharp
            if (productionFlow.IsAwaitingWorldSelection)
            {
                string pendingCharacterId = pendingWorldCharacterId;
                pendingWorldCharacterId = string.Empty;
                if (!string.IsNullOrEmpty(pendingCharacterId) &&
                    productionFlow.SelectFreeChoiceForCharacter(pendingCharacterId))
                {
                    RenderProduction();
                    return;
                }

                EndDialogue();
                return;
            }
```

(This leaves every non-repeatable choice scene's existing button-menu rendering completely untouched — `IsAwaitingWorldSelection` is only ever true for a `_FREE` branch group, which today is D1-01 alone.)

- [ ] **Step 3: Add `TalkToWorldCharacter`**

Right after the closing brace of the existing `CanStartProductionScene(string sceneId)` method (just before `public void CancelActiveDialogue()`), add:

```csharp
        public bool TalkToWorldCharacter(string sceneId, string characterId)
        {
            if (string.IsNullOrWhiteSpace(sceneId) ||
                string.IsNullOrWhiteSpace(characterId))
            {
                return false;
            }

            string normalizedSceneId = sceneId.Trim();
            Wake.Core.ProductionDialogueCheckpoint checkpoint =
                Wake.Core.GameStateManager.Instance?.DialogueCheckpoint;
            if (checkpoint != null &&
                string.Equals(
                    checkpoint.activeSceneId,
                    normalizedSceneId,
                    StringComparison.OrdinalIgnoreCase))
            {
                if (IsBusy || !RestoreProductionScene(checkpoint))
                {
                    return false;
                }

                if (!productionFlow.IsAwaitingWorldSelection)
                {
                    // Not a world-click checkpoint - resume playback exactly
                    // as a direct RestoreProductionScene call would.
                    return true;
                }

                if (!productionFlow.SelectFreeChoiceForCharacter(characterId))
                {
                    EndDialogue();
                    return false;
                }

                RenderProduction();
                return true;
            }

            if (!CanStartProductionScene(normalizedSceneId))
            {
                return false;
            }

            pendingWorldCharacterId = characterId;
            if (!StartProductionScene(normalizedSceneId))
            {
                pendingWorldCharacterId = string.Empty;
                return false;
            }

            return true;
        }
```

- [ ] **Step 4: Clear the pending field on every dialogue teardown**

In `EndDialogue()`, right after:

```csharp
            ambientLineActive = false;
            pendingInvestigationTitle = string.Empty;
```

add:

```csharp
            pendingWorldCharacterId = string.Empty;
```

In `CancelActiveDialogue()`, right after:

```csharp
            ambientLineActive = false;
            pendingInvestigationTitle = string.Empty;
```

add the same line:

```csharp
            pendingWorldCharacterId = string.Empty;
```

- [ ] **Step 5: Verify the project compiles**

Run: open Unity Editor (or `Unity -batchmode -quit -logFile -`) and confirm the Console shows no compile errors for `DialogueController.cs`.

- [ ] **Step 6: Commit**

```bash
git add Assets/_Project/Code/Narrative/DialogueController.cs
git commit -m "feat: add DialogueController.TalkToWorldCharacter and suspend-to-exploration on repeatable choices"
```

---

## Task 4: Route world-character clicks through `TalkToWorldCharacter`

**Files:**
- Modify: `Assets/_Project/Code/Exploration/AmbientCharacterHotspotOverlay.cs:660-738` (`StartMainCharacterDialogue`)

**Interfaces:**
- Consumes: `DialogueController.TalkToWorldCharacter(string, string) : bool` (Task 3), `ProductionConditionEvaluator.ChoiceFlag(string choiceId) : string` (existing, `Wake.Narrative`, already used by `ProductionDialogueFlow.SelectChoice`).

No isolated unit test: this method is only reachable through the live `AmbientCharacterHotspotOverlay` UI spawn/click pipeline (see `CreateWorldCharacter`/`BeginCharacterInteraction`), which requires a running Canvas. Covered by the Task 7 manual PlayMode pass. The change here is a substitution of three existing call sites for one new call, with no new branching logic of its own.

- [ ] **Step 1: Replace the focus-participant branch**

In `Assets/_Project/Code/Exploration/AmbientCharacterHotspotOverlay.cs`, replace the entire `StartMainCharacterDialogue` method:

```csharp
        private void StartMainCharacterDialogue(
            SceneWorldCharacter character)
        {
            DialogueController dialogue = DialogueController.Instance;
            if (dialogue == null)
            {
                Debug.LogWarning(
                    $"Character interaction ignored: dialogue controller missing " +
                    $"for {currentSceneId}/{character.CharacterId}.");
                return;
            }

            Wake.Core.GameStateManager state =
                Wake.Core.GameStateManager.Instance;
            if (character.IsFocusParticipant)
            {
                if (state?.HasCompletedScene(currentSceneId) == true)
                {
                    dialogue.StartAmbientLine(
                        character.CharacterId,
                        MainCharacterWorldLineCatalog.GetCompleted(
                            character.CharacterId,
                            character.State),
                        MainCharacterWorldLineCatalog.GetEmotion(
                            character.State));
                    return;
                }

                if (dialogue.TalkToWorldCharacter(
                        currentSceneId,
                        character.CharacterId))
                {
                    return;
                }

                dialogue.StartAmbientLine(
                    character.CharacterId,
                    MainCharacterWorldLineCatalog.GetCompleted(
                        character.CharacterId,
                        character.State),
                    MainCharacterWorldLineCatalog.GetEmotion(character.State));
                return;
            }

            string interactionId = CreateInteractionId(
                "world",
                currentSceneId,
                currentLocationCode,
                character.CharacterId);
            if (state?.HasCompletedNpcInteraction(interactionId) == true)
            {
                dialogue.StartAmbientLine(
                    character.CharacterId,
                    MainCharacterWorldLineCatalog.GetCompleted(
                        character.CharacterId,
                        character.State),
                    MainCharacterWorldLineCatalog.GetEmotion(character.State));
                return;
            }

            if (dialogue.StartAmbientLine(
                    character.CharacterId,
                    MainCharacterWorldLineCatalog.Get(
                        character.CharacterId,
                        character.State),
                    MainCharacterWorldLineCatalog.GetEmotion(character.State)))
            {
                state?.RecordCompletedNpcInteraction(interactionId);
                RefreshCompletionPresentation();
            }
        }
```

Note the behavior change from today's code: previously, if `CanStartProductionScene` failed for a reason other than "already completed" (e.g. missing prerequisites — never actually reachable for a character already rendered as a scene focus participant), the click silently logged a warning and did nothing. Now it falls back to the same "already told you" ambient line used for a re-clicked already-interviewed character. This removes a dead-end no-op without changing any reachable player-facing path.

- [ ] **Step 2: Grey out each suspect as soon as their own choice is resolved**

Without this, Claire/Marcus/Helena/Owen only visually grey out once the *entire* D1-01 scene completes (i.e. never mid-interview), because `RecordCompletedNpcInteraction` is never called for a focus participant — only `HasCompletedScene` is checked for them. Reuse the flag `ProductionDialogueFlow.SelectChoice` already sets on every free-choice pick (`state.AddFlag(ProductionConditionEvaluator.ChoiceFlag(selectedChoice.ChoiceId))`, where `ChoiceId` follows the `{sceneId}_{CHARACTER}` convention) instead of adding new bookkeeping calls.

In `RefreshCompletionPresentation()`, replace:

```csharp
                bool completed =
                    (view.IsFocusParticipant &&
                     state.HasCompletedScene(currentSceneId)) ||
                    state.HasCompletedNpcInteraction(view.InteractionId);
```

with:

```csharp
                bool completed =
                    (view.IsFocusParticipant &&
                     state.HasCompletedScene(currentSceneId)) ||
                    state.HasCompletedNpcInteraction(view.InteractionId) ||
                    (view.IsFocusParticipant &&
                     state.HasFlag(
                         ProductionConditionEvaluator.ChoiceFlag(
                             $"{currentSceneId}_{view.Speaker}")));
```

(`ProductionConditionEvaluator` is in `Wake.Narrative`, already imported by this file.) This is inert for every scene other than D1-01: the flag `choice_{sceneId}_{speaker}` is only ever set when a matching `PLAYER_CHOICE` row with `ChoiceId = "{sceneId}_{speaker}"` exists, which today only D1-01 has.

- [ ] **Step 3: Verify the project compiles**

Run: open Unity Editor and confirm the Console shows no compile errors for `AmbientCharacterHotspotOverlay.cs`.

- [ ] **Step 4: Commit**

```bash
git add Assets/_Project/Code/Exploration/AmbientCharacterHotspotOverlay.cs
git commit -m "feat: route focus-participant world clicks through TalkToWorldCharacter"
```

---

## Task 5: Travel gate policy

**Files:**
- Modify: `Assets/_Project/Code/Exploration/SceneTravelPolicy.cs:86` (add data table + method after `RestrictedLocations`)
- Test: `Assets/_Project/Tests/EditMode/SceneTravelPolicyTests.cs` (append new tests)

**Interfaces:**
- Produces: `SceneTravelPolicy.IsTravelBlockedByIncompleteInvestigation(string currentLocationCode, string destinationLocationCode, GameStateManager state) : bool` — consumed by Task 6.

- [ ] **Step 1: Write the failing tests**

Append to the `SceneTravelPolicyTests` class in `Assets/_Project/Tests/EditMode/SceneTravelPolicyTests.cs` (inside the class body, after the existing tests):

```csharp
        [Test]
        public void AtriumTravel_IsBlockedUntilAllFourSuspectsAreInterviewed()
        {
            Assert.That(
                SceneTravelPolicy.IsTravelBlockedByIncompleteInvestigation(
                    "ATRIUM",
                    "DINING",
                    state),
                Is.True);

            state.RecordCompletedScene("D1-01");

            Assert.That(
                SceneTravelPolicy.IsTravelBlockedByIncompleteInvestigation(
                    "ATRIUM",
                    "DINING",
                    state),
                Is.False);
        }

        [Test]
        public void AtriumTravel_IsNeverBlockedWhenDestinationIsTheSameLocation()
        {
            Assert.That(
                SceneTravelPolicy.IsTravelBlockedByIncompleteInvestigation(
                    "ATRIUM",
                    "ATRIUM",
                    state),
                Is.False);
        }

        [Test]
        public void UnrelatedLocation_IsNeverBlockedByTheInvestigationGate()
        {
            Assert.That(
                SceneTravelPolicy.IsTravelBlockedByIncompleteInvestigation(
                    "DINING",
                    "ATRIUM",
                    state),
                Is.False);
        }
```

- [ ] **Step 2: Run tests to verify they fail**

Run: `Unity -batchmode -runTests -testPlatform EditMode -testFilter SceneTravelPolicyTests`
Expected: FAIL — compile error, `IsTravelBlockedByIncompleteInvestigation` does not exist.

- [ ] **Step 3: Implement the gate**

In `Assets/_Project/Code/Exploration/SceneTravelPolicy.cs`, right after:

```csharp
        public static IReadOnlyCollection<string> RestrictedLocations =>
            RestrictedLocationCodes;
```

add:

```csharp
        private static readonly IReadOnlyDictionary<string, string>
            LocationInvestigationGate =
                new Dictionary<string, string>(StringComparer.Ordinal)
                {
                    ["ATRIUM"] = "D1-01"
                };

        public static bool IsTravelBlockedByIncompleteInvestigation(
            string currentLocationCode,
            string destinationLocationCode,
            GameStateManager state)
        {
            string current =
                currentLocationCode?.Trim().ToUpperInvariant() ?? string.Empty;
            string destination =
                destinationLocationCode?.Trim().ToUpperInvariant() ??
                string.Empty;
            if (string.Equals(current, destination, StringComparison.Ordinal))
            {
                return false;
            }

            return LocationInvestigationGate.TryGetValue(
                       current,
                       out string requiredSceneId) &&
                   state?.HasCompletedScene(requiredSceneId) != true;
        }
```

- [ ] **Step 4: Run tests to verify they pass**

Run: `Unity -batchmode -runTests -testPlatform EditMode -testFilter SceneTravelPolicyTests`
Expected: PASS (all tests in the file, including the three new ones).

- [ ] **Step 5: Commit**

```bash
git add Assets/_Project/Code/Exploration/SceneTravelPolicy.cs Assets/_Project/Tests/EditMode/SceneTravelPolicyTests.cs
git commit -m "feat: add a location-to-required-scene travel gate for the Atrium investigation"
```

---

## Task 6: Wire the travel gate into `MapController`

**Files:**
- Modify: `Assets/_Project/Code/UI/MapController.cs:14` (add monologue-lines constant), `:460-476` (`SelectLocation`)

**Interfaces:**
- Consumes: `SceneTravelPolicy.IsTravelBlockedByIncompleteInvestigation` (Task 5), `DialogueController.StartAmbientLine` (existing, unchanged).

No isolated unit test: `SelectLocation` is a private method on a `MonoBehaviour` driven by map-node button clicks and depends on `LocationLoader.Instance`/`GameStateManager.Instance`/`DialogueController.Instance` singletons that only exist in a running scene. The decision logic itself (`IsTravelBlockedByIncompleteInvestigation`) is already covered by Task 5's tests; this task only wires the call and the toast-vs-monologue branch. Covered by the Task 7 manual PlayMode pass.

- [ ] **Step 1: Add the monologue lines**

In `Assets/_Project/Code/UI/MapController.cs`, right after:

```csharp
        private const float MapTravelFadeSeconds = .45f;
```

add:

```csharp
        private static readonly string[] AtriumInvestigationMonologueLines =
        {
            "다른 사람의 이야기도 들어보자.",
            "아직은 더 탐문을 할 때야."
        };
```

- [ ] **Step 2: Gate `SelectLocation`**

Replace:

```csharp
        private void SelectLocation(LocationDefinition location)
        {
            GameStateManager state = GameStateManager.Instance;
            LastTravelResult = SceneTravelPolicy.EvaluateMapTravel(
                location,
                state?.CompletedProductionSceneIds,
                state?.UnlockedProductionSceneIds,
                state != null ? state.PublicAnxiety : 0);
            if (TryLoadAllowedDestination(LastTravelResult))
            {
                UIManager.Instance?.ShowIngame();
            }
            else
            {
                ShowTravelFeedback();
            }
        }
```

with:

```csharp
        private void SelectLocation(LocationDefinition location)
        {
            GameStateManager state = GameStateManager.Instance;
            string currentLocationCode =
                LocationLoader.Instance?.CurrentLocation?.LocationCode ??
                state?.CurrentLocationCode ??
                string.Empty;
            if (SceneTravelPolicy.IsTravelBlockedByIncompleteInvestigation(
                    currentLocationCode,
                    location?.LocationCode,
                    state))
            {
                DialogueController.Instance?.StartAmbientLine(
                    "ADRIAN",
                    AtriumInvestigationMonologueLines[
                        UnityEngine.Random.Range(
                            0,
                            AtriumInvestigationMonologueLines.Length)],
                    "internal");
                return;
            }

            LastTravelResult = SceneTravelPolicy.EvaluateMapTravel(
                location,
                state?.CompletedProductionSceneIds,
                state?.UnlockedProductionSceneIds,
                state != null ? state.PublicAnxiety : 0);
            if (TryLoadAllowedDestination(LastTravelResult))
            {
                UIManager.Instance?.ShowIngame();
            }
            else
            {
                ShowTravelFeedback();
            }
        }
```

- [ ] **Step 3: Verify the project compiles**

Run: open Unity Editor and confirm the Console shows no compile errors for `MapController.cs`.

- [ ] **Step 4: Commit**

```bash
git add Assets/_Project/Code/UI/MapController.cs
git commit -m "feat: block map travel out of the Atrium until the investigation is complete"
```

---

## Task 7: Full regression pass + manual PlayMode verification

**Files:** none (verification only)

- [ ] **Step 1: Run the full EditMode suite**

Run: `Unity -batchmode -runTests -testPlatform EditMode`
Expected: PASS, zero failures (in particular: `ProductionDialogueRuntimeTests`, `ScenePresencePresentationPolicyTests`, `SceneTravelPolicyTests`, and every other pre-existing EditMode test file — none of them should have changed behavior).

- [ ] **Step 2: Run the full PlayMode suite**

Run: `Unity -batchmode -runTests -testPlatform PlayMode`
Expected: PASS, zero failures (in particular `ProductionFullFlowPlayModeTests`, `ProductionMapDialogueLaunchPlayModeTests`, `UiBasicSceneEndToEndPlayModeTests`).

- [ ] **Step 3: Manual smoke test in the Unity Editor**

1. Enter Play mode from the title/lobby, start a new game, and fast-forward (via existing debug/skip tooling, or by playing through) up to P-03's completion so D1-01 unlocks.
2. Enter the Atrium (D1-01). Confirm: intro narration and the tutorial hint line play normally, then the dialogue box closes and Claire, Marcus, Helena, and Owen are visible and clickable in the background (no choice-button menu appears).
3. Click Owen first (an arbitrary, non-first order). Confirm his specific interrogation lines play, then the dialogue box closes back to the same exploration view with the remaining three still clickable.
4. Click Owen again. Confirm a short "already told you" line plays instead of restarting the scene or crashing.
5. Open the map (지도) and try to travel to another location. Confirm travel is refused and Adrian's monologue line plays instead (click through a few attempts to confirm both of the two lines can appear).
6. Click Helena, Marcus, and Claire (in any order) to finish all four. Confirm the closing monologue ("네 사람 모두 알리바이를...") and the keyword-unlock system line play automatically, and D1-02 becomes reachable.
7. Open the map again and confirm travel is now unrestricted.

If any step fails, stop and re-open the relevant task above rather than patching ad hoc.

- [ ] **Step 4: Final commit (if anything was fixed during manual verification)**

```bash
git add -A
git commit -m "fix: address issues found during Atrium click-to-talk manual verification"
```

(Skip this step entirely if manual verification found nothing to fix.)
