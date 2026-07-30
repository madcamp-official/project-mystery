# Character Relationship Notes: Progress-Gated Reveal

## Problem

The "인물 관계" (Character Relationships) tab
(`EvidenceNotebookTabsController.ShowCharacterDetail`) shows each character's
`CharacterRelationshipProfile.KnownNote` as soon as `IsDiscovered` is true —
a single static string gated by one "first met" flag/scene, with no further
refinement as the case progresses. Confirmed by re-checking every one of the
9 profiles in `CharacterRelationshipProfileCatalog.cs` against the full
scripted dialogue for when each stated fact is actually established:

- **DANIEL** (`discoverySceneId: "P-01"`): `KnownNote` calls him "협박 사건의
  핵심 피해자가 되었다" (became the central victim) — but P-01 is the
  opening, before Daniel has died or anything has happened to him. The
  murder isn't discovered until D1-06.
- **CLAIRE** (`discoveryFlag: "met_claire"`, set in D1-01): `KnownNote`
  states she "다니엘과 공개적으로 충돌했다" (publicly clashed with Daniel) —
  that clash is the D1-02 dining scene, one scene *after* D1-01.
- **HELENA** (`discoveryFlag: "met_helena"`, set in D1-01): `KnownNote`
  states she "의학적 판단의 독립성을 요구했다" (demanded independence of
  medical judgment) — that demand is part of the D1-07 investigation
  contract negotiation, five scenes after D1-01. D1-01 only establishes the
  sedative-prescription half of the note.

The other six (ADRIAN, RICHARD, EVELYN, THOMAS, MARCUS, OWEN) were checked
against their full arcs and every stated fact in their `KnownNote` is
established within their own discovery scene — no change needed for them.

## Scope

`CharacterRelationshipProfileCatalog.cs` and its one consumer,
`EvidenceNotebookTabsController.ShowCharacterDetail`. `Summary` and `Role`/
`Affiliation` are untouched — they're static backstory/title text, not
case-progress-dependent, and were checked clean during the audit.

## Design

Replace the single `KnownNote` string with an ordered list of tiers, each
carrying its own `flag`/`sceneId` gate (reusing the exact same
flag-or-scene-completion check `IsDiscovered` already uses). The tab shows
the *last* tier whose gate is satisfied — tiers must be authored in
ascending story order so "last satisfied" means "most current."

```csharp
public sealed class CharacterRelationshipNoteTier
{
    public string Text { get; }
    public string Flag { get; }
    public string SceneId { get; }
}
```

`CharacterRelationshipProfile.KnownNote` becomes a method:

```csharp
public string GetKnownNote(GameStateManager state)
```

that walks the tiers from last to first and returns the first one whose
gate passes (empty flag/sceneId = always-satisfied base tier, so every
profile still has a fallback). For the six already-clean characters this is
a single-tier list — identical behavior to today, just re-expressed. For
DANIEL, CLAIRE, and HELENA it's two tiers:

- DANIEL: base tier — "선내 위험을 경고하며 오르페우스 사고 기록을 조사하고
  있다." (matches his actual P-01 warning + established backstory, no death
  reference) — then a tier gated on `sceneId: "D1-06"` with the original
  "협박 사건의 핵심 피해자가 되었다" text.
- CLAIRE: base tier — "회사 비자금과 가족 문제에 예민하다." — then a tier
  gated on `sceneId: "D1-02"` with the original full text (both halves).
- HELENA: base tier — "다니엘에게 안정제를 처방했다." — then a tier gated on
  `sceneId: "D1-07"` with the original full text (both halves).

`ShowCharacterDetail` changes its one line from `profile.KnownNote` to
`profile.GetKnownNote(state)`.

## Testing

- Extend `CharacterRelationshipProfileCatalogTests.cs`: for DANIEL, CLAIRE,
  and HELENA, assert the note text at each gate boundary (before/after the
  relevant scene is completed) matches the intended tier, and that it never
  contains the later tier's spoiler phrase before that scene completes.
- For the six single-tier characters, assert `GetKnownNote` still returns
  the same text as before regardless of progress (regression check).
- Full EditMode regression pass at the end, same as prior work on this
  project.
