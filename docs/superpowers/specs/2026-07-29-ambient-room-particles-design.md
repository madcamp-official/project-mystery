# Ambient Room Particles

## Background

Exploration rooms are not 3D scenes. `LocationLoader` shows each location as a
2D background sprite (`BackgroundCoverPresenter`'s "Cover Image") inside a
`Screen Space - Overlay` canvas, CSS-`background-size:cover`-style cropped/
panned via `BackgroundCoverLayout`. Character hotspots, evidence hotspots, and
inspectable hotspots are all UI overlays parented to that same cover image's
`RectTransform` (`ContentRect`), so they pan/scale together with the
background (`AmbientCharacterHotspotOverlay`, `EvidenceLocationHotspotOverlay`,
`AmbientInspectableOverlay`).

The game currently has no particle/VFX system anywhere in the codebase (no
`ParticleSystem`, no VFX Graph usage). The request is to add soft, bright,
slowly-drifting particles ("dust motes") floating over each room to add
visual dynamism, tinted to complement that room's background.

This follows the same "procedural overlay" pattern the codebase already uses
(and explicitly keeps procedural — see
`2026-07-28-ui-inspector-authoring-map-evidence-design.md`, Non-goals) for
content whose positions are inherently dynamic rather than fixed per-location
data.

## Goals

- A new `AmbientRoomParticleOverlay` component renders ~14-18 small, soft,
  glowing dust motes drifting slowly over the currently-shown room background.
- Particles gently sway/drift (sine-based motion, no physics) and pulse in
  brightness ("twinkle"), wrapping to a new random position when they drift
  out of the visible cover-image bounds, so the effect runs continuously.
- Particle color is `location.AmbientParticleTint` (new field), so each room
  can have its own accent (e.g. warm gold vs cool blue) without changing the
  particle logic.
- Every `LocationDefinition` gets a sensible default tint (warm, low-alpha
  white) so the effect is live everywhere immediately; individual locations
  can be re-tinted later purely by editing that one Inspector field.
- Particles pan/scale with the background exactly like the existing hotspot
  overlays (parented to the same `ContentRect`).
- Drift/twinkle math lives in a pure, static, seed+time-based class so it's
  unit-testable in EditMode, matching the codebase's existing split between
  testable static presentation logic (`BackgroundCoverLayout`,
  `NarrativeLocationHUDPresentation`, ...) and thin MonoBehaviour wiring.

## Non-goals

- No 3D particle system / VFX Graph / Shader Graph work — everything stays
  inside the existing 2D UI-overlay architecture.
- No per-location on/off toggle. All locations get the effect via the default
  tint; nothing in this spec adds a way to disable it for a specific room.
  (If a future room turns out to need it disabled, that's a small follow-up,
  not part of this spec.)
- No automatic color sampling from the background sprite. Tint is manually
  authored per location (or left at the default).
- No player-facing settings/accessibility toggle for the effect.
- Not wired into `ProductionSceneDirector`/dialogue/puzzle scenes — this is
  exploration-room-background only, same scope as the other ambient overlays.

## Design

### Component & wiring

`AmbientRoomParticleOverlay` (new, `Wake.Exploration` namespace) follows the
`AmbientCharacterHotspotOverlay` / `AmbientInspectableOverlay` pattern:

- `Initialize(RectTransform contentRect)` — called once from
  `LocationLoader.CreateBackgroundPresenter()`, alongside the other overlays,
  parented to `backgroundPresenter.ContentRect`. Builds the fixed pool of
  particle `Image` objects once (`raycastTarget = false`).
- `Show(string locationCode, Color tint)` — called from
  `LocationLoader.TryLoadLocation` and `RefreshInteractionOverlays`, alongside
  the other overlays' `Show(...)` calls. Re-tints the existing pool (no
  destroy/recreate) and ensures it's active.
- No `Hide()` beyond what the parent container's `SetActive(false)` already
  does when `LocationLoader.SetPresentationVisible(false)` is called — same as
  the other overlays.

### Particle pool & motion

- Fixed pool size (~14-18), created once in `Initialize`.
- Each particle: a small soft-glow `Image` (new sprite asset — see Assets
  below), random size in a small range (~6-18px), a random seed used to
  derive its drift phase/speed/spawn position so particles don't move in
  lockstep.
- Per-frame (`Update`/`LateUpdate`), for each particle: compute position via
  a static pure function `AmbientRoomParticleDrift.Evaluate(seed, time,
  bounds) -> (Vector2 position, float alpha01)` — sine-based horizontal sway
  + slow vertical drift, alpha pulsing between a low and high bound for the
  twinkle. When position exits `bounds` (the `ContentRect` rect), the function
  wraps to a new pseudo-random position derived from the same seed (so it's
  deterministic/testable) rather than using live `Random` calls in the hot
  path.
- MonoBehaviour applies the returned position/alpha to each pooled `Image`'s
  `RectTransform.anchoredPosition` and `color` (tint × alpha). No per-frame
  allocation.

### Color authoring

- `LocationDefinition` gets `[SerializeField] private Color
  ambientParticleTint = <warm low-alpha white default>;` and a public
  `AmbientParticleTint` accessor, following the exact pattern of
  `backgroundFocus`/`backgroundZoom`.
- Existing `LocationDefinition` assets need no migration — the new field
  silently takes its default value until an author changes it.

### Testing

- New EditMode tests for `AmbientRoomParticleDrift`: alpha stays within
  [0,1], returned position stays within `bounds` after wrap, same
  seed+time+bounds always returns the same result (determinism), different
  particles (different seeds) don't all return identical positions.
- New EditMode test for `LocationDefinition.AmbientParticleTint` default
  value.
- No PlayMode test planned for the MonoBehaviour itself — it's pure wiring
  over already-tested logic, consistent with how `BackgroundCoverPresenter`
  is treated.

### Performance

- ~15 pooled `Image` objects, reused across location switches (re-tinted, not
  recreated). Per-frame cost is a handful of `sin`/`cos` calls per particle —
  negligible next to the rest of the UI overlay work already happening per
  frame.

### Assets

- Needs one new soft radial-gradient circle sprite (no suitable existing
  asset found in `Assets/_Project`). Generated or authored as part of
  implementation, imported as a UI sprite.

## Open questions for implementation

- Exact default tint color value and exact drift speed/size/twinkle-rate
  constants are tuning details, not architectural — pick reasonable starting
  values during implementation and adjust by eye in the Editor rather than
  specifying exact numbers here.
