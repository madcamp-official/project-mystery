# Ambient Room Particles Implementation Plan

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:subagent-driven-development (recommended) or superpowers:executing-plans to implement this plan task-by-task. Steps use checkbox (`- [ ]`) syntax for tracking.

**Goal:** Add soft, bright, slowly-drifting dust-mote particles over exploration room backgrounds, tinted per location, to add visual dynamism.

**Architecture:** A pure static class computes deterministic per-particle drift/twinkle from a seed + time + bounds (unit-testable, no scene needed). A thin `MonoBehaviour` overlay (same pattern as `AmbientCharacterHotspotOverlay`/`AmbientInspectableOverlay`) owns a fixed pool of small glow `Image`s parented to the room background's content rect, applies that pure math every frame, and is wired into `LocationLoader` exactly like the other ambient overlays.

**Tech Stack:** Unity 6000.3, C#, `UnityEngine.UI` (`Image`/`CanvasRenderer`), NUnit EditMode tests (`Wake.EditModeTests.asmdef`), runtime code in `Wake.Runtime.asmdef`.

## Global Constraints

- No 3D particle system / VFX Graph / Shader Graph — stay inside the existing 2D UI-overlay architecture (spec: Non-goals).
- No per-location on/off toggle — every location gets the effect via its tint (spec: Non-goals).
- No automatic color sampling from the background sprite — tint is a manually authored field, defaulted for every location (spec: Non-goals).
- No player-facing settings/accessibility toggle (spec: Non-goals).
- Not wired into dialogue/puzzle/`ProductionSceneDirector` scenes — exploration room background only (spec: Non-goals).
- Existing `LocationDefinition` assets must need zero migration — new field takes its C# default until an author changes it (spec: Goals).
- Drift/twinkle math must be a pure, static, seed+time+bounds function so it's EditMode-testable without a scene (spec: Goals).

---

### Task 1: `LocationDefinition.AmbientParticleTint` field

**Files:**
- Modify: `Assets/_Project/Code/Exploration/LocationDefinition.cs`
- Test: `Assets/_Project/Tests/EditMode/LocationDefinitionAmbientParticleTintTests.cs` (create)

**Interfaces:**
- Produces: `LocationDefinition.AmbientParticleTint` (`Color`, public get-only property), default `new Color(1f, 0.95f, 0.85f, 0.5f)`.

- [ ] **Step 1: Write the failing test**

Create `Assets/_Project/Tests/EditMode/LocationDefinitionAmbientParticleTintTests.cs`:

```csharp
using NUnit.Framework;
using UnityEngine;
using Wake.Exploration;

namespace Wake.Tests
{
    public sealed class LocationDefinitionAmbientParticleTintTests
    {
        [Test]
        public void AmbientParticleTint_DefaultsToWarmLowAlphaWhite()
        {
            LocationDefinition location =
                ScriptableObject.CreateInstance<LocationDefinition>();
            try
            {
                Assert.That(
                    location.AmbientParticleTint,
                    Is.EqualTo(new Color(1f, 0.95f, 0.85f, 0.5f)));
            }
            finally
            {
                Object.DestroyImmediate(location);
            }
        }
    }
}
```

- [ ] **Step 2: Run test to verify it fails**

Run via Unity Test Runner (EditMode) or `mcp__UnityMCP__run_tests` with mode `EditMode`, filtered to
`LocationDefinitionAmbientParticleTintTests`.
Expected: FAIL to compile — `AmbientParticleTint` does not exist on `LocationDefinition`.

- [ ] **Step 3: Add the field**

In `Assets/_Project/Code/Exploration/LocationDefinition.cs`, add the field next to the other
`[SerializeField]` fields (after `backgroundZoom`, line 16):

```csharp
        [SerializeField] private Color ambientParticleTint =
            new(1f, 0.95f, 0.85f, 0.5f);
```

Add the accessor next to `BackgroundZoom` (after line 32):

```csharp
        public Color AmbientParticleTint => ambientParticleTint;
```

- [ ] **Step 4: Run test to verify it passes**

Run the same EditMode test filter.
Expected: PASS.

- [ ] **Step 5: Commit**

```bash
git add Assets/_Project/Code/Exploration/LocationDefinition.cs \
        Assets/_Project/Tests/EditMode/LocationDefinitionAmbientParticleTintTests.cs
git commit -m "feat: add ambient particle tint field to LocationDefinition"
```

---

### Task 2: `AmbientRoomParticleDrift` pure drift/twinkle math

**Files:**
- Create: `Assets/_Project/Code/Exploration/AmbientRoomParticleDrift.cs`
- Test: `Assets/_Project/Tests/EditMode/AmbientRoomParticleDriftTests.cs` (create)

**Interfaces:**
- Produces:
  - `readonly struct AmbientParticleState { Vector2 Position; float Alpha01; }` (public constructor `AmbientParticleState(Vector2 position, float alpha01)`)
  - `static class AmbientRoomParticleDrift { static AmbientParticleState Evaluate(int seed, float time, Rect bounds); }`
- Consumes: nothing (pure function, no dependencies on Task 1 or 3).

- [ ] **Step 1: Write the failing tests**

Create `Assets/_Project/Tests/EditMode/AmbientRoomParticleDriftTests.cs`:

```csharp
using NUnit.Framework;
using UnityEngine;
using Wake.Exploration;

namespace Wake.Tests
{
    public sealed class AmbientRoomParticleDriftTests
    {
        private static readonly Rect Bounds =
            new(-400f, -300f, 800f, 600f);

        [Test]
        public void Evaluate_PositionStaysWithinBounds()
        {
            for (int seed = 0; seed < 20; seed++)
            {
                for (float time = 0f; time < 30f; time += 3.7f)
                {
                    AmbientParticleState state =
                        AmbientRoomParticleDrift.Evaluate(seed, time, Bounds);

                    Assert.That(
                        state.Position.x,
                        Is.InRange(Bounds.xMin, Bounds.xMax));
                    Assert.That(
                        state.Position.y,
                        Is.InRange(Bounds.yMin, Bounds.yMax));
                }
            }
        }

        [Test]
        public void Evaluate_AlphaStaysWithinZeroToOne()
        {
            for (int seed = 0; seed < 20; seed++)
            {
                for (float time = 0f; time < 30f; time += 3.7f)
                {
                    AmbientParticleState state =
                        AmbientRoomParticleDrift.Evaluate(seed, time, Bounds);

                    Assert.That(state.Alpha01, Is.InRange(0f, 1f));
                }
            }
        }

        [Test]
        public void Evaluate_SameInputs_ReturnsIdenticalResult()
        {
            AmbientParticleState first =
                AmbientRoomParticleDrift.Evaluate(7, 12.5f, Bounds);
            AmbientParticleState second =
                AmbientRoomParticleDrift.Evaluate(7, 12.5f, Bounds);

            Assert.That(second.Position, Is.EqualTo(first.Position));
            Assert.That(second.Alpha01, Is.EqualTo(first.Alpha01));
        }

        [Test]
        public void Evaluate_DifferentSeeds_ProduceDifferentPositions()
        {
            AmbientParticleState particleA =
                AmbientRoomParticleDrift.Evaluate(1, 5f, Bounds);
            AmbientParticleState particleB =
                AmbientRoomParticleDrift.Evaluate(2, 5f, Bounds);

            Assert.That(
                particleA.Position,
                Is.Not.EqualTo(particleB.Position));
        }
    }
}
```

- [ ] **Step 2: Run tests to verify they fail**

Run via `mcp__UnityMCP__run_tests` (EditMode), filtered to `AmbientRoomParticleDriftTests`.
Expected: FAIL to compile — `AmbientRoomParticleDrift`/`AmbientParticleState` do not exist.

- [ ] **Step 3: Implement the drift math**

Create `Assets/_Project/Code/Exploration/AmbientRoomParticleDrift.cs`:

```csharp
using UnityEngine;

namespace Wake.Exploration
{
    public readonly struct AmbientParticleState
    {
        public AmbientParticleState(Vector2 position, float alpha01)
        {
            Position = position;
            Alpha01 = alpha01;
        }

        public Vector2 Position { get; }
        public float Alpha01 { get; }
    }

    // Pure, deterministic drift/twinkle math for AmbientRoomParticleOverlay's
    // dust motes. Kept separate from the MonoBehaviour so it's unit-testable
    // without a scene, matching BackgroundCoverLayout's split from
    // BackgroundCoverPresenter.
    //
    // Position is computed as a base offset (from the seed) plus motion,
    // wrapped with Mathf.Repeat(_, length) every frame. Repeat always
    // returns a value in [0, length) regardless of how far out of range its
    // input is, so the particle is *always* inside bounds - there's no
    // separate "if it left the bounds, respawn it" branch to get wrong.
    public static class AmbientRoomParticleDrift
    {
        private const float MinAlpha = 0.15f;
        private const float MaxAlpha = 0.6f;

        public static AmbientParticleState Evaluate(
            int seed,
            float time,
            Rect bounds)
        {
            float verticalSpeed = Mathf.Lerp(4f, 10f, Hash01(seed, 0));
            float y = bounds.yMin + Mathf.Repeat(
                Hash01(seed, 1) * bounds.height + time * verticalSpeed,
                bounds.height);

            float swaySpeed = Mathf.Lerp(0.3f, 0.7f, Hash01(seed, 2));
            float swayPhase = Hash01(seed, 3) * Mathf.PI * 2f;
            float swayAmplitude =
                bounds.width * Mathf.Lerp(0.03f, 0.08f, Hash01(seed, 4));
            float x = bounds.xMin + Mathf.Repeat(
                Hash01(seed, 5) * bounds.width +
                Mathf.Sin(time * swaySpeed + swayPhase) * swayAmplitude,
                bounds.width);

            float twinkleSpeed = Mathf.Lerp(0.8f, 1.6f, Hash01(seed, 6));
            float twinklePhase = Hash01(seed, 7) * Mathf.PI * 2f;
            float twinkle01 =
                (Mathf.Sin(time * twinkleSpeed + twinklePhase) + 1f) * 0.5f;
            float alpha = Mathf.Lerp(MinAlpha, MaxAlpha, twinkle01);

            return new AmbientParticleState(new Vector2(x, y), alpha);
        }

        // Deterministic, allocation-free pseudo-random in [0, 1] for a given
        // seed + channel (channel is a distinct small index per constant we
        // derive, so one seed can drive several independent-looking values).
        private static float Hash01(int seed, int channel)
        {
            unchecked
            {
                int h = seed * 374761393 + channel * 668265263;
                h = (h ^ (h >> 13)) * 1274126177;
                h ^= h >> 16;
                return (h & 0x7FFFFFFF) / (float)int.MaxValue;
            }
        }
    }
}
```

- [ ] **Step 4: Run tests to verify they pass**

Run the same EditMode test filter.
Expected: PASS (all four tests).

- [ ] **Step 5: Commit**

```bash
git add Assets/_Project/Code/Exploration/AmbientRoomParticleDrift.cs \
        Assets/_Project/Tests/EditMode/AmbientRoomParticleDriftTests.cs
git commit -m "feat: add pure drift/twinkle math for ambient room particles"
```

---

### Task 3: `AmbientRoomParticleOverlay` MonoBehaviour

**Files:**
- Create: `Assets/_Project/Code/Exploration/AmbientRoomParticleOverlay.cs`
- Test: `Assets/_Project/Tests/EditMode/AmbientRoomParticleOverlayTests.cs` (create)

**Interfaces:**
- Consumes: `AmbientRoomParticleDrift.Evaluate(int, float, Rect) -> AmbientParticleState` (Task 2).
- Produces:
  - `AmbientRoomParticleOverlay.Initialize(RectTransform backgroundContentRect)` — builds the particle pool as children of `backgroundContentRect`.
  - `AmbientRoomParticleOverlay.Show(Color tint)` — sets the tint applied to every particle.

- [ ] **Step 1: Write the failing test**

Create `Assets/_Project/Tests/EditMode/AmbientRoomParticleOverlayTests.cs`:

```csharp
using NUnit.Framework;
using UnityEngine;
using UnityEngine.UI;
using Wake.Exploration;

namespace Wake.Tests
{
    public sealed class AmbientRoomParticleOverlayTests
    {
        [Test]
        public void Initialize_CreatesSixteenNonInteractiveGlowParticles()
        {
            GameObject contentObject =
                new("Content", typeof(RectTransform));
            GameObject overlayObject =
                new("Overlay", typeof(AmbientRoomParticleOverlay));
            try
            {
                AmbientRoomParticleOverlay overlay =
                    overlayObject.GetComponent<AmbientRoomParticleOverlay>();
                overlay.Initialize(
                    contentObject.GetComponent<RectTransform>());

                Image[] images =
                    contentObject.GetComponentsInChildren<Image>(true);
                Assert.That(images.Length, Is.EqualTo(16));
                foreach (Image image in images)
                {
                    Assert.That(image.raycastTarget, Is.False);
                    Assert.That(image.sprite, Is.Not.Null);
                }
            }
            finally
            {
                Object.DestroyImmediate(overlayObject);
                Object.DestroyImmediate(contentObject);
            }
        }

        [Test]
        public void Show_TintsEveryParticleImage()
        {
            GameObject contentObject =
                new("Content", typeof(RectTransform));
            GameObject overlayObject =
                new("Overlay", typeof(AmbientRoomParticleOverlay));
            try
            {
                AmbientRoomParticleOverlay overlay =
                    overlayObject.GetComponent<AmbientRoomParticleOverlay>();
                overlay.Initialize(
                    contentObject.GetComponent<RectTransform>());
                Color tint = new(0.2f, 0.4f, 0.9f, 0.5f);

                overlay.Show(tint);

                Image[] images =
                    contentObject.GetComponentsInChildren<Image>(true);
                foreach (Image image in images)
                {
                    Assert.That(image.color.r, Is.EqualTo(tint.r).Within(0.001f));
                    Assert.That(image.color.g, Is.EqualTo(tint.g).Within(0.001f));
                    Assert.That(image.color.b, Is.EqualTo(tint.b).Within(0.001f));
                }
            }
            finally
            {
                Object.DestroyImmediate(overlayObject);
                Object.DestroyImmediate(contentObject);
            }
        }
    }
}
```

- [ ] **Step 2: Run tests to verify they fail**

Run via `mcp__UnityMCP__run_tests` (EditMode), filtered to `AmbientRoomParticleOverlayTests`.
Expected: FAIL to compile — `AmbientRoomParticleOverlay` does not exist.

- [ ] **Step 3: Implement the overlay**

Create `Assets/_Project/Code/Exploration/AmbientRoomParticleOverlay.cs`:

```csharp
using UnityEngine;
using UnityEngine.UI;

namespace Wake.Exploration
{
    [DisallowMultipleComponent]
    public sealed class AmbientRoomParticleOverlay : MonoBehaviour
    {
        private const int ParticleCount = 16;
        private const float MinSizePx = 6f;
        private const float MaxSizePx = 18f;
        private const int GlowTextureSize = 32;

        private static Sprite glowSprite;

        private readonly RectTransform[] particleRects =
            new RectTransform[ParticleCount];
        private readonly Image[] particleImages =
            new Image[ParticleCount];
        private readonly int[] particleSeeds =
            new int[ParticleCount];
        private RectTransform contentRect;
        private Color tint = new(1f, 0.95f, 0.85f, 0.5f);

        public void Initialize(RectTransform backgroundContentRect)
        {
            contentRect = backgroundContentRect;
            EnsureGlowSprite();

            for (int index = 0; index < ParticleCount; index++)
            {
                GameObject particle = new(
                    $"AmbientParticle_{index}",
                    typeof(RectTransform),
                    typeof(CanvasRenderer),
                    typeof(Image));
                particle.transform.SetParent(contentRect, false);

                RectTransform rect = particle.GetComponent<RectTransform>();
                rect.anchorMin = rect.anchorMax = new Vector2(0.5f, 0.5f);
                rect.pivot = new Vector2(0.5f, 0.5f);
                float size = Mathf.Lerp(
                    MinSizePx,
                    MaxSizePx,
                    (index + 0.5f) / ParticleCount);
                rect.sizeDelta = new Vector2(size, size);

                Image image = particle.GetComponent<Image>();
                image.sprite = glowSprite;
                image.raycastTarget = false;
                image.color = tint;

                particleRects[index] = rect;
                particleImages[index] = image;
                particleSeeds[index] = index * 7919 + 104729;
            }
        }

        public void Show(Color locationTint)
        {
            tint = locationTint;
            for (int index = 0; index < particleImages.Length; index++)
            {
                if (particleImages[index] != null)
                {
                    particleImages[index].color = tint;
                }
            }
        }

        private void Update()
        {
            if (contentRect == null || particleRects[0] == null)
            {
                return;
            }

            Rect bounds = contentRect.rect;
            if (bounds.width <= 0f || bounds.height <= 0f)
            {
                return;
            }

            float time = Time.time;
            for (int index = 0; index < ParticleCount; index++)
            {
                AmbientParticleState state = AmbientRoomParticleDrift.Evaluate(
                    particleSeeds[index],
                    time,
                    bounds);
                particleRects[index].anchoredPosition = state.Position;
                Color color = tint;
                color.a = tint.a * state.Alpha01;
                particleImages[index].color = color;
            }
        }

        private static void EnsureGlowSprite()
        {
            if (glowSprite != null)
            {
                return;
            }

            var texture = new Texture2D(
                GlowTextureSize,
                GlowTextureSize,
                TextureFormat.RGBA32,
                false)
            {
                name = "Ambient Particle Glow",
                wrapMode = TextureWrapMode.Clamp,
                filterMode = FilterMode.Bilinear,
                hideFlags = HideFlags.HideAndDontSave
            };
            var pixels = new Color32[GlowTextureSize * GlowTextureSize];
            for (int y = 0; y < GlowTextureSize; y++)
            {
                float ny = (y + 0.5f) / GlowTextureSize * 2f - 1f;
                for (int x = 0; x < GlowTextureSize; x++)
                {
                    float nx = (x + 0.5f) / GlowTextureSize * 2f - 1f;
                    float distance = nx * nx + ny * ny;
                    float falloff = Mathf.Clamp01(1f - distance);
                    byte alpha = (byte)Mathf.RoundToInt(
                        falloff * falloff * 255f);
                    pixels[y * GlowTextureSize + x] =
                        new Color32(255, 255, 255, alpha);
                }
            }
            texture.SetPixels32(pixels);
            texture.Apply(false, true);

            glowSprite = Sprite.Create(
                texture,
                new Rect(0f, 0f, GlowTextureSize, GlowTextureSize),
                new Vector2(0.5f, 0.5f));
            glowSprite.name = "Ambient Particle Glow Sprite";
        }
    }
}
```

Note: the glow texture generation mirrors
`AmbientCharacterHotspotOverlay.GetGroundShadowTexture()` (same squared
radial-falloff technique) — this project generates soft circular UI
textures procedurally rather than importing sprite assets, so no new image
asset is needed.

- [ ] **Step 4: Run tests to verify they pass**

Run the same EditMode test filter.
Expected: PASS (both tests).

- [ ] **Step 5: Commit**

```bash
git add Assets/_Project/Code/Exploration/AmbientRoomParticleOverlay.cs \
        Assets/_Project/Tests/EditMode/AmbientRoomParticleOverlayTests.cs
git commit -m "feat: add AmbientRoomParticleOverlay pool + rendering"
```

---

### Task 4: Wire into `LocationLoader`

**Files:**
- Modify: `Assets/_Project/Code/Exploration/LocationLoader.cs:26-31` (field declarations), `:83-88` and `:95-106` (`TryLoadLocation`), `:122-132` (`RefreshInteractionOverlays`), `:134-165` (`CreateBackgroundPresenter`)

**Interfaces:**
- Consumes: `AmbientRoomParticleOverlay.Initialize(RectTransform)` and `.Show(Color)` (Task 3); `LocationDefinition.AmbientParticleTint` (Task 1).
- Produces: nothing new consumed by later tasks (this is the final wiring task).

- [ ] **Step 1: Add the field**

In `Assets/_Project/Code/Exploration/LocationLoader.cs`, add a field next to the other overlay
fields (after line 31, `private AmbientInspectableOverlay ambientInspectables;`):

```csharp
        private AmbientRoomParticleOverlay ambientParticles;
```

- [ ] **Step 2: Create it in `CreateBackgroundPresenter`**

In `CreateBackgroundPresenter()` (around line 162-164), add, right after the existing
`ambientInspectables` setup block:

```csharp
            ambientParticles =
                presenterObject.AddComponent<AmbientRoomParticleOverlay>();
            ambientParticles.Initialize(backgroundPresenter.ContentRect);
```

- [ ] **Step 3: Call `Show` alongside the other overlays**

In `TryLoadLocation` (around line 102-106), add after the existing `ambientInspectables?.Show(...)`
line:

```csharp
            ambientParticles?.Show(location.AmbientParticleTint);
```

In `RefreshInteractionOverlays` (around line 127-131), add after the existing
`ambientInspectables?.Show(...)` line:

```csharp
            ambientParticles?.Show(CurrentLocation.AmbientParticleTint);
```

- [ ] **Step 4: Compile check**

Run `mcp__UnityMCP__read_console` (or check the Unity Editor console) after Unity recompiles.
Expected: no compile errors in `LocationLoader.cs`.

- [ ] **Step 5: Manual verification in the Editor**

1. Open Unity, enter Play mode (`mcp__UnityMCP__manage_editor` action `play`) with a scene/flow
   that reaches exploration and loads at least one location (or call
   `LocationLoader.Instance.LoadLocation(...)` with any `LocationDefinition` asset directly via
   `mcp__UnityMCP__execute_code` if there's no quick way to reach exploration through normal play).
2. Confirm ~16 small soft glow dots are visible over the room background, drifting slowly and
   twinkling, tinted warm-white (the default).
3. Exit Play mode.

Expected: particles are visible, moving, and don't error in the console
(`mcp__UnityMCP__read_console`, types `error`).

- [ ] **Step 6: Commit**

```bash
git add Assets/_Project/Code/Exploration/LocationLoader.cs
git commit -m "feat: wire ambient room particles into LocationLoader"
```

---

### Task 5: Run full test suite

**Files:** none (verification-only task).

- [ ] **Step 1: Run the full EditMode suite**

Run `mcp__UnityMCP__run_tests` with mode `EditMode` (no filter — full suite).
Expected: all tests pass, including the three new test files from Tasks 1-3 and everything that
existed before this plan.

- [ ] **Step 2: If anything unrelated is failing**

Stop and report it rather than fixing it as part of this plan — this plan only touches
`LocationDefinition.cs`, `LocationLoader.cs`, and the two new `AmbientRoomParticle*` files, so a
failure outside those should be investigated separately, not patched over here.
