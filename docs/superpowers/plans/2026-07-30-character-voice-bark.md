# Character Voice Bark & Story Recording Playback Implementation Plan

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:subagent-driven-development (recommended) or superpowers:executing-plans to implement this plan task-by-task. Steps use checkbox (`- [ ]`) syntax for tracking.

**Goal:** When a voiced dialogue line displays, play a matching short character reaction clip ("bark") or, for the handful of lines that have one, a real story-recording clip — with graceful silent no-ops everywhere audio doesn't exist yet.

**Architecture:** Three new pure/testable `Wake.Narrative` classes (`VoiceBarkCatalog` data table, `VoiceBarkPlayer` selection logic behind an injectable clip-provider interface, `StoryRecordingCatalog` data table) plus small, targeted additions to three existing files (`AudioManager.DuckMusic`, `ResourcesOrpheusAudioProvider`'s broken resource path, one hook in `DialogueController.PresentPromptRecord`). No new MonoBehaviours, no new scene objects — `DialogueController` already owns a dedicated `AudioSource` for typewriter SFX; this reuses that exact pattern for a second, bark-dedicated one.

**Tech Stack:** Unity 6, C#, NUnit EditMode tests via Unity Test Runner (`mcp__UnityMCP__run_tests`).

## Global Constraints

- Every character-folder / clip lookup must silently no-op (never throw, never log an error) when the folder or file doesn't exist — `EVELYN`/`MARCUS` have no dubbing folder at all today and that must not be a special case anywhere.
- Do **not** modify `OrpheusRecordCatalog.Segments`' content (speaker order, transcripts, count) — confirmed out of scope with the user; only its audio *lookup* is being fixed.
- Filename/clip-name matching is always case-insensitive substring matching on the cue token (handles both `claire_greet_01` and `DanielMercer_GREET_01`) — never assume an exact naming convention.
- `GREET` is selected structurally (new speaker turn), never from the `PortraitEmotion` table.
- Match existing code conventions: `Wake.Narrative` namespace for the new catalogs/player (same namespace as `DialogueController`, `ProductionDialogueRuntime.cs`), 4-space indent, no comments beyond a one-line "why" where non-obvious, PascalCase types / camelCase locals as used throughout the file being edited.

---

## File Structure

- **Create:** `Assets/_Project/Code/Narrative/VoiceBarkCatalog.cs` — `PortraitEmotion` → candidate cue IDs table.
- **Create:** `Assets/_Project/Code/Narrative/VoiceBarkPlayer.cs` — `IVoiceBarkClipProvider`, `ResourcesVoiceBarkClipProvider`, `VoiceBarkPlayer` (selection/repeat-guard/cooldown/coverage logic).
- **Create:** `Assets/_Project/Code/Narrative/StoryRecordingCatalog.cs` — `StableLineId` → resource path table.
- **Modify:** `Assets/_Project/Code/Core/AudioManager.cs` — add `DuckMusic`.
- **Modify:** `Assets/_Project/Code/UI/OrpheusAudioRestorationUIController.cs` — fix `ResourcesOrpheusAudioProvider.TryGetClip`'s resource path, extract testable `SelectClipIndex`.
- **Modify:** `Assets/_Project/Code/Narrative/DialogueController.cs` — wire everything into `PresentPromptRecord`.
- **Test:** `Assets/_Project/Tests/EditMode/VoiceBarkCatalogTests.cs`, `VoiceBarkPlayerTests.cs`, `StoryRecordingCatalogTests.cs` (new); `Assets/_Project/Tests/EditMode/OrpheusAudioRestorationTests.cs` (extended).

---

### Task 1: `VoiceBarkCatalog`

**Files:**
- Create: `Assets/_Project/Code/Narrative/VoiceBarkCatalog.cs`
- Test: `Assets/_Project/Tests/EditMode/VoiceBarkCatalogTests.cs`

**Interfaces:**
- Consumes: `Wake.Narrative.PortraitEmotion` (existing enum, `ProductionDialogueRuntime.cs:8` — values `Neutral`, `Concerned`, `Angry`, `Positive`).
- Produces: `public static class VoiceBarkCatalog { public static IReadOnlyList<string> CandidateCues(PortraitEmotion emotion); public static IReadOnlyList<string> AllCueIds { get; } }`. Task 2 (`VoiceBarkPlayer`) calls `CandidateCues`.

- [ ] **Step 1: Write the failing test**

Create `Assets/_Project/Tests/EditMode/VoiceBarkCatalogTests.cs`:

```csharp
using System.Linq;
using NUnit.Framework;
using Wake.Narrative;

namespace Wake.Tests
{
    public class VoiceBarkCatalogTests
    {
        [Test]
        public void AllCueIds_ListsExactlyTheTwelveCues()
        {
            Assert.That(
                VoiceBarkCatalog.AllCueIds,
                Is.EqualTo(new[]
                {
                    "GREET", "ACK_POS", "ACK_NEG", "THINK", "CONFUSED",
                    "SURPRISED", "SUSPICIOUS", "LAUGH", "SIGH", "ANNOYED",
                    "WORRIED", "PAIN_EFFORT"
                }));
        }

        [TestCase(PortraitEmotion.Neutral, new[] { "ACK_POS", "SUSPICIOUS" })]
        [TestCase(PortraitEmotion.Positive, new[] { "ACK_POS", "LAUGH" })]
        [TestCase(
            PortraitEmotion.Angry,
            new[] { "ACK_NEG", "THINK", "SURPRISED", "SUSPICIOUS", "ANNOYED" })]
        [TestCase(
            PortraitEmotion.Concerned,
            new[] { "ACK_NEG", "THINK", "CONFUSED", "SURPRISED", "SIGH", "WORRIED" })]
        public void CandidateCues_MatchesDesignTable(
            PortraitEmotion emotion,
            string[] expected)
        {
            Assert.That(VoiceBarkCatalog.CandidateCues(emotion), Is.EqualTo(expected));
        }

        [Test]
        public void CandidateCues_NeverReturnsGreetOrPainEffort()
        {
            foreach (PortraitEmotion emotion in
                     (PortraitEmotion[])System.Enum.GetValues(typeof(PortraitEmotion)))
            {
                Assert.That(
                    VoiceBarkCatalog.CandidateCues(emotion),
                    Has.None.EqualTo("GREET").And.None.EqualTo("PAIN_EFFORT"),
                    emotion.ToString());
            }
        }

        [Test]
        public void CandidateCues_OnlyEverReturnsKnownCueIds()
        {
            foreach (PortraitEmotion emotion in
                     (PortraitEmotion[])System.Enum.GetValues(typeof(PortraitEmotion)))
            {
                Assert.That(
                    VoiceBarkCatalog.CandidateCues(emotion).All(
                        cue => VoiceBarkCatalog.AllCueIds.Contains(cue)),
                    Is.True,
                    emotion.ToString());
            }
        }
    }
}
```

- [ ] **Step 2: Run the tests to verify they fail**

Use `mcp__UnityMCP__run_tests`, `mode: "EditMode"`, `test_names: ["Wake.Tests.VoiceBarkCatalogTests"]`, `include_failed_tests: true`.
Expected: compile error, `VoiceBarkCatalog` does not exist.

- [ ] **Step 3: Implement `VoiceBarkCatalog`**

Create `Assets/_Project/Code/Narrative/VoiceBarkCatalog.cs`:

```csharp
using System.Collections.Generic;

namespace Wake.Narrative
{
    public static class VoiceBarkCatalog
    {
        public static IReadOnlyList<string> AllCueIds { get; } = new[]
        {
            "GREET", "ACK_POS", "ACK_NEG", "THINK", "CONFUSED",
            "SURPRISED", "SUSPICIOUS", "LAUGH", "SIGH", "ANNOYED",
            "WORRIED", "PAIN_EFFORT"
        };

        private static readonly IReadOnlyDictionary<PortraitEmotion, string[]>
            CandidatesByEmotion = new Dictionary<PortraitEmotion, string[]>
            {
                [PortraitEmotion.Neutral] = new[] { "ACK_POS", "SUSPICIOUS" },
                [PortraitEmotion.Positive] = new[] { "ACK_POS", "LAUGH" },
                [PortraitEmotion.Angry] = new[]
                {
                    "ACK_NEG", "THINK", "SURPRISED", "SUSPICIOUS", "ANNOYED"
                },
                [PortraitEmotion.Concerned] = new[]
                {
                    "ACK_NEG", "THINK", "CONFUSED", "SURPRISED", "SIGH",
                    "WORRIED"
                }
            };

        public static IReadOnlyList<string> CandidateCues(PortraitEmotion emotion) =>
            CandidatesByEmotion.TryGetValue(emotion, out string[] cues)
                ? cues
                : System.Array.Empty<string>();
    }
}
```

- [ ] **Step 4: Run the tests to verify they pass**

Use `mcp__UnityMCP__run_tests`, same `test_names` as Step 2.
Expected: all PASS.

- [ ] **Step 5: Commit**

```bash
git add Assets/_Project/Code/Narrative/VoiceBarkCatalog.cs Assets/_Project/Tests/EditMode/VoiceBarkCatalogTests.cs
git commit -m "feat: add VoiceBarkCatalog emotion-to-cue mapping"
```

---

### Task 2: `VoiceBarkPlayer` and its clip provider

**Files:**
- Create: `Assets/_Project/Code/Narrative/VoiceBarkPlayer.cs`
- Test: `Assets/_Project/Tests/EditMode/VoiceBarkPlayerTests.cs`

**Interfaces:**
- Consumes: `VoiceBarkCatalog.CandidateCues` (Task 1).
- Produces:
  ```csharp
  public interface IVoiceBarkClipProvider
  {
      IReadOnlyList<AudioClip> GetClips(string characterId, string cueId);
  }

  public sealed class ResourcesVoiceBarkClipProvider : IVoiceBarkClipProvider { ... }

  public sealed class VoiceBarkPlayer
  {
      public VoiceBarkPlayer(
          IVoiceBarkClipProvider clipProvider,
          AudioSource audioSource,
          System.Func<float> randomUnit = null,
          System.Func<int, int> randomIndexBelow = null);

      public bool TryPlayBark(
          string characterId,
          PortraitEmotion emotion,
          bool isNewSpeakerTurn,
          float currentTime);
  }
  ```
  Task 6 (`DialogueController`) constructs one `VoiceBarkPlayer` with `ResourcesVoiceBarkClipProvider` and calls `TryPlayBark`.

`randomUnit`/`randomIndexBelow` are injected so tests can force deterministic outcomes instead of fighting `UnityEngine.Random` global state; production code leaves them `null` and the constructor defaults them to `UnityEngine.Random.value` / `UnityEngine.Random.Range(0, max)`.

- [ ] **Step 1: Write the failing tests**

Create `Assets/_Project/Tests/EditMode/VoiceBarkPlayerTests.cs`:

```csharp
using System.Collections.Generic;
using NUnit.Framework;
using UnityEngine;
using Wake.Narrative;

namespace Wake.Tests
{
    public class VoiceBarkPlayerTests
    {
        private sealed class FakeClipProvider : IVoiceBarkClipProvider
        {
            public Dictionary<(string character, string cue), AudioClip[]> Clips = new();

            public IReadOnlyList<AudioClip> GetClips(string characterId, string cueId) =>
                Clips.TryGetValue((characterId, cueId), out AudioClip[] clips)
                    ? clips
                    : System.Array.Empty<AudioClip>();
        }

        private GameObject host;
        private AudioSource source;

        [SetUp]
        public void SetUp()
        {
            host = new GameObject("VoiceBarkPlayerTests");
            source = host.AddComponent<AudioSource>();
        }

        [TearDown]
        public void TearDown()
        {
            Object.DestroyImmediate(host);
        }

        private static AudioClip FakeClip(string name)
        {
            var clip = AudioClip.Create(name, 1, 1, 44100, false);
            clip.name = name;
            return clip;
        }

        [Test]
        public void NewSpeakerTurn_AlwaysUsesGreetCue()
        {
            var provider = new FakeClipProvider();
            provider.Clips[("DANIEL", "GREET")] = new[] { FakeClip("DANIEL_GREET_01") };
            var player = new VoiceBarkPlayer(
                provider, source, randomUnit: () => 0f, randomIndexBelow: _ => 0);

            bool played = player.TryPlayBark(
                "DANIEL", PortraitEmotion.Angry, isNewSpeakerTurn: true, currentTime: 0f);

            // PlayOneShot doesn't set AudioSource.clip (that field is only
            // for the persistent/looping clip) - the return value is the
            // only observable signal that a clip was actually selected and
            // handed to PlayOneShot.
            Assert.That(played, Is.True);
        }

        [Test]
        public void MissingFolder_IsASilentNoOp()
        {
            var provider = new FakeClipProvider();
            var player = new VoiceBarkPlayer(
                provider, source, randomUnit: () => 0f, randomIndexBelow: _ => 0);

            bool played = player.TryPlayBark(
                "EVELYN", PortraitEmotion.Neutral, isNewSpeakerTurn: true, currentTime: 0f);

            Assert.That(played, Is.False);
        }

        [Test]
        public void CoverageGate_SkipsWhenRandomUnitMissesRatio()
        {
            var provider = new FakeClipProvider();
            provider.Clips[("DANIEL", "GREET")] = new[] { FakeClip("DANIEL_GREET_01") };
            var player = new VoiceBarkPlayer(
                provider, source, randomUnit: () => 0.99f, randomIndexBelow: _ => 0);

            bool played = player.TryPlayBark(
                "DANIEL", PortraitEmotion.Neutral, isNewSpeakerTurn: true, currentTime: 0f);

            Assert.That(played, Is.False);
        }

        [Test]
        public void Cooldown_BlocksSecondPlayWithin1Point4Seconds()
        {
            var provider = new FakeClipProvider();
            provider.Clips[("DANIEL", "GREET")] = new[]
            {
                FakeClip("DANIEL_GREET_01"), FakeClip("DANIEL_GREET_02")
            };
            var player = new VoiceBarkPlayer(
                provider, source, randomUnit: () => 0f, randomIndexBelow: _ => 0);

            Assert.That(
                player.TryPlayBark(
                    "DANIEL", PortraitEmotion.Neutral, true, currentTime: 0f),
                Is.True);
            Assert.That(
                player.TryPlayBark(
                    "DANIEL", PortraitEmotion.Neutral, true, currentTime: 1.0f),
                Is.False);
            Assert.That(
                player.TryPlayBark(
                    "DANIEL", PortraitEmotion.Neutral, true, currentTime: 1.5f),
                Is.True);
        }

        [Test]
        public void RepeatGuard_ExcludesLastThreePlayedClipsForSameCharacter()
        {
            var provider = new FakeClipProvider();
            provider.Clips[("DANIEL", "ACK_POS")] = new[]
            {
                FakeClip("A"), FakeClip("B"), FakeClip("C"), FakeClip("D")
            };
            var seenIndexes = new List<int>();
            var player = new VoiceBarkPlayer(
                provider,
                source,
                randomUnit: () => 0f,
                randomIndexBelow: max =>
                {
                    seenIndexes.Add(max);
                    return 0;
                });

            float t = 0f;
            for (int i = 0; i < 3; i++)
            {
                player.TryPlayBark(
                    "DANIEL", PortraitEmotion.Neutral, false, currentTime: t);
                t += 2f;
            }

            // Pool of 4 clips, 3 already played -> exactly 1 candidate left,
            // so the 4th pick's randomIndexBelow call must receive max == 1.
            player.TryPlayBark("DANIEL", PortraitEmotion.Neutral, false, currentTime: t);

            Assert.That(seenIndexes[^1], Is.EqualTo(1));
        }
    }
}
```

- [ ] **Step 2: Run the tests to verify they fail**

Use `mcp__UnityMCP__run_tests`, `test_names: ["Wake.Tests.VoiceBarkPlayerTests"]`, `include_failed_tests: true`.
Expected: compile error, types don't exist yet.

- [ ] **Step 3: Implement `VoiceBarkPlayer`**

Create `Assets/_Project/Code/Narrative/VoiceBarkPlayer.cs`:

```csharp
using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace Wake.Narrative
{
    public interface IVoiceBarkClipProvider
    {
        IReadOnlyList<AudioClip> GetClips(string characterId, string cueId);
    }

    public sealed class ResourcesVoiceBarkClipProvider : IVoiceBarkClipProvider
    {
        private static readonly IReadOnlyDictionary<string, string> FolderByCharacter =
            new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
            {
                ["DANIEL"] = "daniel",
                ["CLAIRE"] = "claire",
                ["HELENA"] = "helena",
                ["ADRIAN"] = "adrian",
                ["RICHARD"] = "richard",
                ["THOMAS"] = "thomas",
                ["OWEN"] = "owen",
                ["EVELYN"] = "evelyn",
                ["MARCUS"] = "marcus"
            };

        public IReadOnlyList<AudioClip> GetClips(string characterId, string cueId)
        {
            if (string.IsNullOrEmpty(characterId) ||
                !FolderByCharacter.TryGetValue(characterId, out string folder))
            {
                return Array.Empty<AudioClip>();
            }

            string upperCue = cueId.ToUpperInvariant();
            return Resources.LoadAll<AudioClip>($"SoundEffect/Dubbing/{folder}")
                .Where(clip => clip.name.ToUpperInvariant().Contains(upperCue))
                .ToArray();
        }
    }

    public sealed class VoiceBarkPlayer
    {
        private const float CooldownSeconds = 1.4f;
        private const int RepeatGuardCount = 3;
        private const float CoverageRatio = 0.4f;
        private const string GreetCue = "GREET";

        private readonly IVoiceBarkClipProvider clipProvider;
        private readonly AudioSource audioSource;
        private readonly Func<float> randomUnit;
        private readonly Func<int, int> randomIndexBelow;
        private readonly Dictionary<string, Queue<string>> recentClipsByCharacter = new();
        private readonly Dictionary<string, float> lastPlayTimeByCharacter = new();
        private readonly Dictionary<string, string> lastCueByCharacter = new();

        public VoiceBarkPlayer(
            IVoiceBarkClipProvider clipProvider,
            AudioSource audioSource,
            Func<float> randomUnit = null,
            Func<int, int> randomIndexBelow = null)
        {
            this.clipProvider = clipProvider;
            this.audioSource = audioSource;
            this.randomUnit = randomUnit ?? (() => UnityEngine.Random.value);
            this.randomIndexBelow =
                randomIndexBelow ?? (max => UnityEngine.Random.Range(0, max));
        }

        public bool TryPlayBark(
            string characterId,
            PortraitEmotion emotion,
            bool isNewSpeakerTurn,
            float currentTime)
        {
            if (randomUnit() > CoverageRatio)
            {
                return false;
            }

            if (lastPlayTimeByCharacter.TryGetValue(characterId, out float lastPlayed) &&
                currentTime - lastPlayed < CooldownSeconds)
            {
                return false;
            }

            string cue = isNewSpeakerTurn
                ? GreetCue
                : PickCue(VoiceBarkCatalog.CandidateCues(emotion));
            if (cue == null)
            {
                return false;
            }

            IReadOnlyList<AudioClip> candidates = clipProvider.GetClips(characterId, cue);
            if (candidates.Count == 0 &&
                lastCueByCharacter.TryGetValue(characterId, out string previousCue))
            {
                candidates = clipProvider.GetClips(characterId, previousCue);
            }

            if (candidates.Count == 0)
            {
                return false;
            }

            AudioClip chosen = PickClip(characterId, candidates);
            audioSource.PlayOneShot(chosen);
            RecordPlay(characterId, chosen.name, cue, currentTime);
            return true;
        }

        private string PickCue(IReadOnlyList<string> candidates) =>
            candidates.Count == 0 ? null : candidates[randomIndexBelow(candidates.Count)];

        private AudioClip PickClip(string characterId, IReadOnlyList<AudioClip> candidates)
        {
            Queue<string> recent = recentClipsByCharacter.TryGetValue(
                characterId, out Queue<string> existing)
                ? existing
                : new Queue<string>();
            AudioClip[] filtered = candidates
                .Where(clip => !recent.Contains(clip.name))
                .ToArray();
            AudioClip[] pool = filtered.Length > 0
                ? filtered
                : candidates.ToArray();
            return pool[randomIndexBelow(pool.Length)];
        }

        private void RecordPlay(
            string characterId, string clipName, string cue, float currentTime)
        {
            lastPlayTimeByCharacter[characterId] = currentTime;
            lastCueByCharacter[characterId] = cue;
            Queue<string> recent = recentClipsByCharacter.TryGetValue(
                characterId, out Queue<string> existing)
                ? existing
                : recentClipsByCharacter[characterId] = new Queue<string>();
            recent.Enqueue(clipName);
            while (recent.Count > RepeatGuardCount)
            {
                recent.Dequeue();
            }
        }
    }
}
```

- [ ] **Step 4: Run the tests to verify they pass**

Use `mcp__UnityMCP__run_tests`, `test_names: ["Wake.Tests.VoiceBarkPlayerTests"]`.
Expected: all PASS. If `RepeatGuard_ExcludesLastThreePlayedClipsForSameCharacter` fails on the exact `seenIndexes` value, check the queue eviction order against the test's assumption (4 clips, 3 in the recent-queue after 3 plays with `randomIndexBelow` always returning 0 - i.e. always picking the first candidate each time - leaves exactly 1 of the 4 clips unplayed by the 4th call).

- [ ] **Step 5: Commit**

```bash
git add Assets/_Project/Code/Narrative/VoiceBarkPlayer.cs Assets/_Project/Tests/EditMode/VoiceBarkPlayerTests.cs
git commit -m "feat: add VoiceBarkPlayer selection logic with repeat guard, cooldown, coverage gate"
```

---

### Task 3: `StoryRecordingCatalog`

**Files:**
- Create: `Assets/_Project/Code/Narrative/StoryRecordingCatalog.cs`
- Test: `Assets/_Project/Tests/EditMode/StoryRecordingCatalogTests.cs`

**Interfaces:**
- Consumes: none new.
- Produces: `public static class StoryRecordingCatalog { public static bool TryGet(string stableLineId, out string resourcePath); }`. Task 6 (`DialogueController`) calls this.

- [ ] **Step 1: Write the failing test**

Create `Assets/_Project/Tests/EditMode/StoryRecordingCatalogTests.cs`:

```csharp
using System.Linq;
using NUnit.Framework;
using UnityEditor;
using UnityEngine;
using Wake.Narrative;

namespace Wake.Tests
{
    public class StoryRecordingCatalogTests
    {
        private const string DialoguePath =
            "Assets/_Project/Content/Dialogue/Under_the_Horizon_Dialogue_KR.csv";

        [Test]
        public void TryGet_ResolvesTheTwoWiredDanielChatLines()
        {
            Assert.That(
                StoryRecordingCatalog.TryGet("d2_06_10", out string d206),
                Is.True);
            Assert.That(d206, Is.EqualTo("D2-06_DANIEL_CHAT_01"));

            Assert.That(
                StoryRecordingCatalog.TryGet("d5_03_10", out string d503),
                Is.True);
            Assert.That(d503, Is.EqualTo("D5-03_DANIEL_CHAT_01"));
        }

        [Test]
        public void TryGet_ReturnsFalseForLinesWithNoFileYet()
        {
            Assert.That(
                StoryRecordingCatalog.TryGet("d2_06_09", out _), Is.False);
            Assert.That(
                StoryRecordingCatalog.TryGet("d4_01_01", out _), Is.False);
            Assert.That(
                StoryRecordingCatalog.TryGet("nonexistent_line", out _), Is.False);
        }

        [Test]
        public void EveryWiredStableLineId_ExistsInProductionDialogue()
        {
            TextAsset csv = AssetDatabase.LoadAssetAtPath<TextAsset>(DialoguePath);
            var stableIds = DialogueCsvParser.Parse(csv.text).Records
                .Select(record => record.StableLineId)
                .ToHashSet();

            foreach (string stableId in new[] { "d2_06_10", "d5_03_10" })
            {
                Assert.That(stableIds, Contains.Item(stableId), stableId);
            }
        }
    }
}
```

- [ ] **Step 2: Run the tests to verify they fail**

Use `mcp__UnityMCP__run_tests`, `test_names: ["Wake.Tests.StoryRecordingCatalogTests"]`, `include_failed_tests: true`.
Expected: compile error, `StoryRecordingCatalog` doesn't exist.

- [ ] **Step 3: Implement `StoryRecordingCatalog`**

Create `Assets/_Project/Code/Narrative/StoryRecordingCatalog.cs`:

```csharp
using System.Collections.Generic;

namespace Wake.Narrative
{
    public static class StoryRecordingCatalog
    {
        private static readonly IReadOnlyDictionary<string, string> PathByStableLineId =
            new Dictionary<string, string>
            {
                ["d2_06_10"] = "D2-06_DANIEL_CHAT_01",
                ["d5_03_10"] = "D5-03_DANIEL_CHAT_01"
            };

        public static bool TryGet(string stableLineId, out string resourcePath) =>
            PathByStableLineId.TryGetValue(
                stableLineId ?? string.Empty, out resourcePath);
    }
}
```

- [ ] **Step 4: Run the tests to verify they pass**

Use `mcp__UnityMCP__run_tests`, `test_names: ["Wake.Tests.StoryRecordingCatalogTests"]`.
Expected: all PASS.

- [ ] **Step 5: Commit**

```bash
git add Assets/_Project/Code/Narrative/StoryRecordingCatalog.cs Assets/_Project/Tests/EditMode/StoryRecordingCatalogTests.cs
git commit -m "feat: add StoryRecordingCatalog for the two currently-recorded chat lines"
```

---

### Task 4: `AudioManager.DuckMusic`

**Files:**
- Modify: `Assets/_Project/Code/Core/AudioManager.cs`

**Interfaces:**
- Consumes: existing private fields `activeMusicSource`, `MusicVolume`, `currentMusicMix` (all already in this file).
- Produces: `public void DuckMusic(float duckMultiplier, float holdSeconds)`. Task 6 (`DialogueController`) calls `AudioManager.Instance?.DuckMusic(...)`.

No existing `AudioManagerTests.cs` in the project (confirmed by search) — this class has never had automated coverage, matching that, this task is implementation + manual verification only, consistent with the rest of the file.

- [ ] **Step 1: Add `DuckMusic` and its coroutine**

In `Assets/_Project/Code/Core/AudioManager.cs`, insert immediately after the `PlaySfx` method (right after its closing `}`, currently around line 456):

```csharp

        public void DuckMusic(float duckMultiplier, float holdSeconds)
        {
            if (activeMusicSource == null)
            {
                return;
            }

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

        private static IEnumerator FadeSourceVolume(
            AudioSource source, float targetVolume, float duration)
        {
            float safeDuration = Mathf.Max(0f, duration);
            float startVolume = source.volume;
            float elapsed = 0f;
            while (elapsed < safeDuration)
            {
                elapsed += Time.unscaledDeltaTime;
                source.volume = Mathf.Lerp(
                    startVolume, targetVolume, Mathf.Clamp01(elapsed / safeDuration));
                yield return null;
            }

            source.volume = targetVolume;
        }
```

- [ ] **Step 2: Compile check**

Use `mcp__UnityMCP__refresh_unity` (`mode: "force"`, `compile: "request"`, `wait_for_ready: true`), then `mcp__UnityMCP__read_console` (`types: ["error"]`).
Expected: zero errors.

- [ ] **Step 3: Commit**

```bash
git add Assets/_Project/Code/Core/AudioManager.cs
git commit -m "feat: add AudioManager.DuckMusic for temporary music ducking"
```

---

### Task 5: Fix `ResourcesOrpheusAudioProvider`'s resource path

**Files:**
- Modify: `Assets/_Project/Code/UI/OrpheusAudioRestorationUIController.cs:13-25`
- Test: `Assets/_Project/Tests/EditMode/OrpheusAudioRestorationTests.cs` (append)

**Interfaces:**
- Consumes: `OrpheusRecordCatalog.All`, `OrpheusRecordCatalog.TryGet` (existing, `Assets/_Project/Code/Puzzles/OrpheusAudioRestoration.cs`).
- Produces: `public static int ResourcesOrpheusAudioProvider.SelectClipIndex(OrpheusRecordSegment segment, IReadOnlyList<OrpheusRecordSegment> allSegments, IReadOnlyList<string> candidateClipNames)` — pure, testable without touching `Resources`. No other task depends on this.

- [ ] **Step 1: Write the failing test**

Append to `Assets/_Project/Tests/EditMode/OrpheusAudioRestorationTests.cs` (inside the existing `OrpheusAudioRestorationTests` class, after the last `[Test]` method — check the file's current end and add before the closing braces):

```csharp
        [Test]
        public void SelectClipIndex_PairsBothJulianSegmentsToDistinctClips()
        {
            OrpheusRecordSegment julian1 = OrpheusRecordCatalog.All[0];
            OrpheusRecordSegment julian2 = OrpheusRecordCatalog.All[2];
            string[] candidateNames =
            {
                "D7-03_JULIAN_01", "D7-03_JULIAN_02",
                "D7-03_JULIAN_03", "D7-03_JULIAN_04", "D7-03_THOMAS_01"
            };

            int index1 = ResourcesOrpheusAudioProvider.SelectClipIndex(
                julian1, OrpheusRecordCatalog.All, candidateNames);
            int index2 = ResourcesOrpheusAudioProvider.SelectClipIndex(
                julian2, OrpheusRecordCatalog.All, candidateNames);

            Assert.That(index1, Is.EqualTo(0));
            Assert.That(index2, Is.EqualTo(1));
        }

        [Test]
        public void SelectClipIndex_ReturnsMinusOneWhenNoFileMatchesSpeaker()
        {
            OrpheusRecordSegment evelynSegment = OrpheusRecordCatalog.All[1];
            string[] candidateNames =
            {
                "D7-03_JULIAN_01", "D7-03_THOMAS_01"
            };

            int index = ResourcesOrpheusAudioProvider.SelectClipIndex(
                evelynSegment, OrpheusRecordCatalog.All, candidateNames);

            Assert.That(index, Is.EqualTo(-1));
        }
```

Add `using Wake.UI;` to the test file's using block if not already present (it is — confirmed line 6).

- [ ] **Step 2: Run the tests to verify they fail**

Use `mcp__UnityMCP__run_tests`, `test_names: ["Wake.Tests.OrpheusAudioRestorationTests.SelectClipIndex_PairsBothJulianSegmentsToDistinctClips", "Wake.Tests.OrpheusAudioRestorationTests.SelectClipIndex_ReturnsMinusOneWhenNoFileMatchesSpeaker"]`, `include_failed_tests: true`.
Expected: compile error, `SelectClipIndex` doesn't exist.

- [ ] **Step 3: Rewrite `ResourcesOrpheusAudioProvider`**

In `Assets/_Project/Code/UI/OrpheusAudioRestorationUIController.cs`, replace lines 13-25 (the whole `ResourcesOrpheusAudioProvider` class) with:

```csharp
    public sealed class ResourcesOrpheusAudioProvider : IOrpheusAudioProvider
    {
        private const string ResourceFolder = "SoundEffect/Dubbing/story_recording";

        public bool TryGetClip(string stableLineId, out AudioClip clip)
        {
            clip = null;
            if (!OrpheusRecordCatalog.TryGet(
                    stableLineId, out OrpheusRecordSegment segment))
            {
                return false;
            }

            AudioClip[] allClips = Resources.LoadAll<AudioClip>(ResourceFolder);
            int index = SelectClipIndex(
                segment,
                OrpheusRecordCatalog.All,
                allClips.Select(item => item.name).ToArray());
            if (index < 0)
            {
                return false;
            }

            clip = allClips[index];
            return true;
        }

        public static int SelectClipIndex(
            OrpheusRecordSegment segment,
            IReadOnlyList<OrpheusRecordSegment> allSegments,
            IReadOnlyList<string> candidateClipNames)
        {
            string speakerPrefix = segment.Speaker
                .Replace("_RECORD", string.Empty)
                .Replace("_MESSAGE", string.Empty)
                .ToUpperInvariant();
            int[] matchingIndexes = candidateClipNames
                .Select((name, index) => (name, index))
                .Where(item => item.name.ToUpperInvariant().Contains(speakerPrefix))
                .OrderBy(item => item.name, StringComparer.Ordinal)
                .Select(item => item.index)
                .ToArray();

            int sameSpeakerPosition = allSegments
                .Where(item => item.Speaker == segment.Speaker)
                .ToList()
                .IndexOf(segment);

            return sameSpeakerPosition >= 0 &&
                   sameSpeakerPosition < matchingIndexes.Length
                ? matchingIndexes[sameSpeakerPosition]
                : -1;
        }
    }
```

`System` and `System.Linq` are already imported at the top of this file (lines 1 and 3) — no using-directive changes needed.

- [ ] **Step 4: Run the tests to verify they pass**

Use `mcp__UnityMCP__run_tests`, `test_names: ["Wake.Tests.OrpheusAudioRestorationTests"]`.
Expected: all PASS (both new tests plus the pre-existing ones in this file, unaffected since `OrpheusRecordCatalog` itself wasn't touched).

- [ ] **Step 5: Commit**

```bash
git add Assets/_Project/Code/UI/OrpheusAudioRestorationUIController.cs Assets/_Project/Tests/EditMode/OrpheusAudioRestorationTests.cs
git commit -m "fix: point ResourcesOrpheusAudioProvider at the real story-recording folder"
```

---

### Task 6: Wire everything into `DialogueController.PresentPromptRecord`

**Files:**
- Modify: `Assets/_Project/Code/Narrative/DialogueController.cs`

**Interfaces:**
- Consumes: `VoiceBarkPlayer`/`ResourcesVoiceBarkClipProvider` (Task 2), `StoryRecordingCatalog.TryGet` (Task 3), `AudioManager.Instance.DuckMusic`/`PlaySfx` (Task 4 + existing), `DialogueSpeakerKind` (existing enum).
- Produces: nothing further downstream — last task in this plan.

No automated test — this is a MonoBehaviour integration point inside an already-large, untested-at-the-unit-level controller (matches the file's existing convention: `DialogueController` has zero EditMode tests of its own today; it's exercised via `PlayMode` fixtures and manual play). Verified by compile + a manual Play Mode pass.

- [ ] **Step 1: Add the bark player field and initialize it**

In `Assets/_Project/Code/Narrative/DialogueController.cs`, add a field near the existing `typewriterAudioSource`/`typewriterClip` fields (around line 56-59):

```csharp
        private AudioSource voiceBarkAudioSource;
        private VoiceBarkPlayer voiceBarkPlayer;
        private string lastBarkSpeakerId = string.Empty;
```

Immediately after `typewriterAudioSource.volume = AudioManager.Instance?.SfxVolume ?? 1f;` (`DialogueController.cs:134-135`, the end of the existing typewriter `AudioSource` setup block) and before the following `responsiveLayout = ...` line, add:

```csharp
            voiceBarkAudioSource = gameObject.AddComponent<AudioSource>();
            voiceBarkAudioSource.playOnAwake = false;
            voiceBarkAudioSource.loop = false;
            voiceBarkPlayer = new VoiceBarkPlayer(
                new ResourcesVoiceBarkClipProvider(), voiceBarkAudioSource);
```

- [ ] **Step 2: Hook playback into `PresentPromptRecord`**

In the same file, `PresentPromptRecord` (around lines 749-768), add the voice hook immediately after the existing `ShowPortrait(...)` call and before the closing brace:

```csharp
            if (record.VoiceRequired)
            {
                PlayVoiceForRecord(record, speaker);
            }
```

Then add a new private method right after `PresentPromptRecord`:

```csharp
        private void PlayVoiceForRecord(
            DialogueRecord record, DialogueSpeakerIdentity speaker)
        {
            if (speaker.Kind == DialogueSpeakerKind.RecordedVoice)
            {
                if (!StoryRecordingCatalog.TryGet(
                        record.StableLineId, out string resourcePath))
                {
                    return;
                }

                AudioClip clip = Resources.Load<AudioClip>(
                    $"SoundEffect/Dubbing/story_recording/{resourcePath}");
                if (clip == null)
                {
                    return;
                }

                AudioManager.Instance?.PlaySfx(clip);
                AudioManager.Instance?.DuckMusic(0.5f, clip.length);
                return;
            }

            if (speaker.Kind != DialogueSpeakerKind.Character &&
                speaker.Kind != DialogueSpeakerKind.Monologue)
            {
                return;
            }

            bool isNewSpeakerTurn = speaker.PortraitId != lastBarkSpeakerId;
            lastBarkSpeakerId = speaker.PortraitId;
            voiceBarkPlayer?.TryPlayBark(
                speaker.PortraitId,
                DialoguePresentationMap.GetEmotion(record.Emotion),
                isNewSpeakerTurn,
                Time.unscaledTime);
        }
```

- [ ] **Step 3: Reset speaker-turn tracking with the rest of the per-session state**

In `EndDialogue()` (`DialogueController.cs:1200-1220`), add `lastBarkSpeakerId = string.Empty;` immediately after the existing `pendingWorldCharacterId = string.Empty;` line, so a new dialogue session doesn't inherit "same speaker" from whatever scene played last.

- [ ] **Step 4: Compile check**

Use `mcp__UnityMCP__refresh_unity` (`mode: "force"`, `compile: "request"`, `wait_for_ready: true`), then `mcp__UnityMCP__read_console` (`types: ["error"]`).
Expected: zero errors.

- [ ] **Step 5: Manual Play Mode verification**

Enter Play Mode on `UI Basic Scene` (or via the `PuzzleQA` debug scene from the earlier session if faster to reach specific characters — not required, just convenient), advance through dialogue that includes Daniel, Claire, or Adrian lines. Confirm via `mcp__UnityMCP__read_console` that no errors/exceptions appear, and confirm audibly (or via `mcp__UnityMCP__execute_code` checking `voiceBarkAudioSource.isPlaying` right after a line renders) that barks fire on at least some lines. Advance to a D2-06 or D5-03 Daniel-chat line specifically and confirm the story recording plays instead of a generic bark (the two should sound different — one is the specific scripted recording, not a short reaction clip).

- [ ] **Step 6: Commit**

```bash
git add Assets/_Project/Code/Narrative/DialogueController.cs
git commit -m "feat: play character voice barks and story recordings when dialogue lines render"
```
