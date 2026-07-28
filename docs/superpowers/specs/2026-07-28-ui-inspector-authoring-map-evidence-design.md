# UI Inspector-Authoring: Map + Evidence (Phase A)

## Background

Large parts of this project's UI are assembled at runtime in C# instead of being
authored as real Unity hierarchy/prefab content. Controllers call
`new GameObject(...)` and set `anchorMin`/`anchorMax`/`anchoredPosition`/
`sizeDelta`/`offsetMin`/`offsetMax` in code every time a panel initializes,
which means the Inspector values on those RectTransforms are irrelevant (code
overwrites them on `Awake`/`Start`) and layout changes require editing C#
instead of dragging things in the Scene view.

A scan of `Assets/_Project/Code` found this pattern (`new GameObject(...)`,
`.sizeDelta = new Vector2(...)`, `.anchoredPosition = new Vector2(...)`,
`.anchorMin = new Vector2(...)`, `referenceResolution`) in **28 files, 152
occurrences**. This is too large for one spec/plan, so the work is split into
phases. This spec covers **Phase A only**: `MapController` and
`EvidencePanelController`, the two panels originally flagged as painful to
edit.

## Goals (Phase A)

- `MapController` and `EvidencePanelController` stop calling any RectTransform
  layout API (`anchorMin`/`anchorMax`/`pivot`/`anchoredPosition`/`sizeDelta`/
  `offsetMin`/`offsetMax`/`localScale`) at runtime, for elements whose count is
  fixed.
- Every UI element these two controllers currently build with
  `new GameObject(...)` becomes a real, hand-authored child in
  `UI Basic Scene.unity`, with the exact same visual result as today (values
  copied 1:1 from the current code into the Inspector — this is a mechanical
  migration, not a redesign).
- Map's 24 location nodes (a fixed set — see Design) become 24 authored
  GameObjects instead of being instantiated from scratch per `RefreshMap()`
  call.
- Code is reduced to: `transform.Find(...)` to get references, then setting
  *data* (text, sprite, color-by-state, active/inactive, button listeners) —
  never geometry.

## Non-goals (Phase A)

- Groups B–F (see Roadmap) are out of scope for this spec. Each gets its own
  spec/plan later.
- Group E (`AmbientInspectableOverlay`, `AmbientCharacterHotspotOverlay`,
  `AmbientInteractionPresentation`, `BackgroundCoverLayout`, `LocationLoader`)
  is excluded permanently from this initiative: their positions come from
  per-location *data* (hotspot coordinates that differ per location), not
  arbitrary hardcoding. Converting them to fixed Inspector values isn't
  possible without redesigning how location content is authored.
- No visual redesign. Every authored value is copied from the current
  runtime-computed value, so the game should look pixel-identical before and
  after.
- Evidence carousel items (`RebuildCarousel`/`PositionCarouselItems`) are
  *not* converted — their count genuinely varies at runtime (however much
  evidence the player has collected), so the existing template-instantiate +
  index-based-position pattern is the correct, sanctioned exception and stays
  as-is.

## Roadmap (not part of this spec)

Order agreed for future phases, each to be brainstormed/planned separately:

1. **Phase A** (this spec): `MapController`, `EvidencePanelController`
2. **Phase B**: `StatusHUDController`, `ObjectiveMapHUDController`,
   `ToastController` (highest reuse — other panels depend on these)
3. **Phase C**: `DialogueController` (portrait), `ResponsiveDialogueLayout`,
   `DialoguePresentationView`, `InvestigationDialogueUIController`,
   `NarrativeLocationHUDController`
4. **Phase D**: 9 puzzle UI controllers (`CameraBlindSpotUIController`,
   `BloodDirectionPuzzleUIController`, `ProductionPuzzleUIController`,
   `OrpheusAudioRestorationUIController`, `MarcusInterrogationUIController`,
   `TimelinePuzzleUIController`, `ExitInspectionUIController`,
   `FinalAccusationUIController`, `ProductionEndingUIController`) — near-
   identical pattern, expected to go fast once Phase A/B establish the
   template
5. **Phase F**: `RuntimeUiOverhaulController` (canvas scaler config stays in
   code — that's a legitimate runtime setting, not hardcoded layout; only the
   title-screen visual assembly in `TitleScreenPresentationController` moves
   to authored content), `RuntimeUiLayoutRegistry`

Excluded permanently: Group E (see Non-goals).

## Design

### Principle

- Code must never set `anchorMin`/`anchorMax`/`pivot`/`anchoredPosition`/
  `sizeDelta`/`offsetMin`/`offsetMax`/`localScale` on a fixed-count UI
  element. Those values live on the GameObject in the Editor.
- Code may set: text, sprite, color (state-driven), active/inactive, and
  button listeners — pure data/behavior, never geometry.
- Exception: a runtime list whose *item count* genuinely varies (Evidence
  carousel) keeps its template + `Instantiate` + index-based positioning.
  Map's 24 nodes don't qualify — the set is fixed
  (`CruiseMapLayoutCatalog` has exactly 24 hardcoded location codes; only
  each node's *status* — locked/available/completed — varies per
  playthrough), so they become 24 authored GameObjects, not instantiated
  ones.
- Since the UI was never authored in the Inspector to begin with, the
  *initial* Inspector values/hierarchy are derived directly from the current
  code's computed values (copied 1:1), not redesigned. This is a mechanical
  migration.

### MapController changes

- `ConfigureFullscreenPanel()` deleted. Authored directly in the scene
  instead:
  - `Canvas/Map` (the panel returned by `roomsContainer.parent`): stretch
    anchors (0,0)-(1,1), zero offsets, `localScale` (1,1,1).
  - `Canvas/Map/Rooms`: stretch anchors (0,0)-(1,1), `offsetMin` (24, 24),
    `offsetMax` (-24, -88), `localScale` (1,1,1).
  - `Canvas/Map/Back Btn`: anchor (0,1)/(0,1), pivot (0,1), anchored
    position (24, -150), size (164, 54), scale (1,1,1).
  - `Canvas/Map/Image` (legacy decoration): `SetActive(false)` authored
    directly (inactive by default in the scene).
  - New authored child `Canvas/Map/Map Screen Title` (TMP text): anchor
    (0,1)-(1,1), pivot (0.5,1), offsetMin (210,-205), offsetMax (-210,-140),
    text "MV ELYSIUM · 장소 선택", font size 34, bold, center-aligned, color
    `#F4D696`, raycast target off. `MapTypography.ApplyLocation(title)` still
    runs in code (typography is a data/style concern already centralized
    there, not a one-off hardcoded rect).
  - `EnsureInitialized()` becomes: find `Map/Rooms`, disable any active
    legacy buttons under it (unchanged), find the pre-authored map nodes —
    no geometry code left.
- `CreateMapSurface()` deleted. Authored directly in the scene instead:
  - `Canvas/Map/Rooms/Dynamic Location Viewport`: `RectTransform` (stretch,
    zero offsets), `Image` (color `(0.015, 0.025, 0.045, 1)`), `RectMask2D`,
    `ScrollRect` (horizontal off, vertical on, Elastic, inertia on,
    scrollSensitivity 42, `verticalNormalizedPosition` 1). `viewport`/
    `content` references wired once in the Inspector.
  - `.../Dynamic Location Viewport/Dynamic Location Content`: anchor (0,1)-
    (1,1), pivot (0.5,1), anchored position (0,0), size delta (0, 1480).
  - `.../Dynamic Location Content/MV Elysium Cutaway`: `Image` stretch,
    `preserveAspect` false, `raycastTarget` false, sprite left empty in the
    Editor (assigned from `cruiseMapSprite` in code, since that's Inspector-
    driven *content*, not layout — the field already exists and is wired).
  - `EnsureInitialized()` finds `dynamicContent`/`deckScroll` via `Find()` +
    `GetComponent`, assigns `cruiseMapSprite` to the found Image. If
    `cruiseMapSprite` is null, keep the existing dark-fallback-color +
    `Debug.LogError` behavior (still valid — that's data validation, not
    layout).
- `CreateLocationNode()` deleted entirely. Authored directly in the scene
  instead: 24 children under `Dynamic Location Content`, one per
  `CruiseMapLayoutCatalog` entry, named `Map Node {CODE}` (e.g.
  `Map Node PORT`), each with:
  - `RectTransform`: `anchorMin`/`anchorMax` both set to that location's
    catalog position (e.g. PORT → (0.045, 0.15)), pivot (0.5, 0.5), size
    delta (154, 58).
  - `Image` (`type` Sliced if `mapNodeSprite` assigned, else Simple; base
    color the "locked" look `#30353E` @ 235 alpha — code overwrites per
    status anyway), `Button`, `Outline` (base "locked" outline color
    `#0F1218` @ 230 alpha).
  - Child `Label` (TMP text): stretch anchors, offsetMin (8,4), offsetMax
    (-8,-4), center-aligned, auto-sizing 12–21, `raycastTarget` false,
    `MapTypography.ApplyLocation(label)` applied once (typography, not
    layout).
- `RefreshMap()` rewritten: still builds `CurrentViewModel` the same way, but
  instead of `CreateLocationNode(entry)` per entry, it does
  `dynamicContent.Find($"Map Node {entry.Spec.Code}")` and sets: `Image.color`
  and `Outline.effectColor` (by `entry.Status`), `Button.interactable`,
  `Button.onClick` listener (`SelectEntry`), `Label.text`
  (`{DisplayName}\n{StatusLabel}`), `Label.color` (by locked state). No
  destroy/recreate loop needed since nodes are permanent now — any location
  the current `LocationGraph` doesn't produce an entry for this run just gets
  `SetActive(false)` (mirrors current behavior of not creating a node for it).
- Everything else (`SelectEntry`, `SelectLocation`, `TryTravelToScene`,
  `TryEnterDialogueOnlyScene`, `ShowTravelFeedback`,
  `TryLoadAllowedDestination`, `CreateTravelCoordinator`) is unchanged — none
  of it touches geometry.

### EvidencePanelController changes

- `LayoutRects()` deleted entirely. Authored directly in the scene instead
  (values copied from the current code):
  - `Canvas/Evidence`: stretch anchors, zero offsets, scale (1,1,1), `Image`
    background color `#081222` (already-existing component, just stop
    re-coloring it in code — set the color once in the Inspector).
  - `.../Image` (`detailImage`): anchored position (-360, 70), size
    (520, 340).
  - `.../Text (TMP)` (`titleText`): anchored position (280, 185), size
    (650, 70), font size 38, top-left aligned, color `#F2DEB4`.
  - `.../Description` (`detailText`, renamed from `Image/Evidence` — the
    reparent-and-rename in `Awake()` stays in code since it's structural
    cleanup of a legacy nested object, not a position/size change): anchored
    position (280, -30), size (650, 340), color `#DED3BE`.
  - `.../Evidences` (`carouselContainer`): anchored position (0, -300), size
    (1260, 190).
  - `.../Turn` (`turnLeftButton`): anchored position (-160, -135).
  - `.../Turn (1)` (`turnRightButton`): anchored position (-160, 75).
  - `.../Next (1)` (`prevButton`): anchored position (-720, -300).
  - `.../Next` (`nextButton`): anchored position (720, -300).
  - `.../Back Btn` (`backButton`): anchored position (790, -455), size
    (190, 62).
  - `.../Turn (2)` (`theoryBoardButton`): anchored position (330, 110), size
    (180, 58), starts active (currently forced active in code — author it
    active directly since Phase A doesn't change *when* it's shown, only
    *how* its rect gets set).
- `ConfigureTheoryBoardButton()` keeps only the non-geometry lines: label
  text "가설 보드" + center alignment. The `SetRect` call is deleted.
- `Awake()` keeps all `Find()`/`GetComponent()` wiring, the mask/grid-layout
  cleanup, the template-destroy loop, the button listener wiring, and
  `EvidenceTypography.ApplySurface(...)` (typography, not layout) — only the
  `LayoutRects()` call and the `SetRect(backButton...)` call are removed.
- Carousel behavior (`Refresh`, `RebuildCarousel`, `PositionCarouselItems`,
  `Advance`, `SelectIndex`, `Rotate`, `ApplySelection`, `ApplyView`) is
  unchanged — sanctioned runtime-list exception (see Principle).

### Testing

- Existing EditMode/PlayMode tests targeting `UI Basic Scene` must still
  pass unchanged (no behavior change intended, only *where* the values live).
- Manual verification in Play Mode: Map panel shows all 24 nodes in their
  current positions with correct locked/available/completed styling; clicking
  an available node travels correctly; Evidence panel shows carousel/detail
  panel in the same visual position as before; theory board button still
  opens.
- Since this is a pure migration (no visual change intended), the main risk
  is a copy-paste value mismatch between what the code used to compute and
  what gets authored — verify by comparing a screenshot of the panels before
  and after the change.
