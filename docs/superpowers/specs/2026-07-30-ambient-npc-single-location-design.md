# Ambient NPC Single-Location Exclusivity

## Problem

`AmbientBarkCatalog` reuses a handful of generic "archetype" speaker ids
(`PASSENGER_A`..`PASSENGER_F`, `CREW_SECURITY`, `CREW_ATTENDANT`,
`CREW_ENGINEER`, `BALLAST_CONTROLLER`) across multiple locations. Nothing
stops the same archetype id from being simultaneously eligible at two or
more different locations within the same story period — e.g. `CREW_SECURITY`
is eligible at GANGWAY, OPEN_DECK, CREW_STAIRS, *and* VAULT at once on Day 1.
A player bouncing between locations sees what reads as the same guard
standing post in four different rooms at the same time.

The 9 main characters don't have this problem — `ScenePresenceCatalog`
already places each of them at exactly one location per scene. This is the
same discipline, applied to the ambient/flavor NPC roster.

Confirmed via analysis of the current catalog (`AmbientBarkCatalog.cs`,
96 entries across the Day-1 / Day-2-4 / Day-5+ tiers from the ambient
dialogue variety work): 7 archetypes are double- or triple-booked within at
least one tier —

| Archetype | Conflicting locations (by tier) |
|---|---|
| `CREW_SECURITY` | D1/MID: GANGWAY, OPEN_DECK, CREW_STAIRS, VAULT — LATE: CREW_STAIRS, VAULT |
| `PASSENGER_A` | D1: ATRIUM, PORT, PROMENADE — MID: ATRIUM, PROMENADE |
| `PASSENGER_D` | D1: GANGWAY, NEWS_LOUNGE, PROMENADE — LATE: GANGWAY, PROMENADE |
| `PASSENGER_E` | D1/LATE: HORIZON, OPEN_DECK |
| `CREW_ATTENDANT` | D1: BALLROOM, HORIZON |
| `CREW_ENGINEER` | D1/MID/LATE: GENERATOR, STABILIZERS |
| `BALLAST_CONTROLLER` | D1/MID/LATE: BALLAST_CONTROL_ANNEX, BALLAST_TANKS |

## Scope

`AmbientBarkCatalog` only. `SceneContextBarkCatalog` and the main-character
systems (`ScenePresenceCatalog`, `MainCharacterWorldLineCatalog`) are
already exclusive and untouched.

## Design

### 1. Exclusivity is enforced per day-tier, not globally

An archetype may still appear at *different* locations across *different*
tiers (that's a person's post changing over the course of the story, same
as how main characters move scene to scene) — the rule is only: within a
single tier (Day 1 / Day 2-4 / Day 5+), an archetype id may back at most one
location's bark entries.

### 2. Lock the invariant with a test first

Add an EditMode test that buckets `AmbientBarkCatalog.All` by (tier,
speaker) — reusing the same tier classification the ambient-variety work's
condition parser already implies (`"always"` → Day 1, `chapter>=2 and
chapter<=4` → Mid, `chapter>=5` → Late; anxiety-gated and `flag:`/`scene=`
entries are excluded from the check, since those are conditional overlays on
top of a day tier, not a separate tier of their own) — and asserts each
bucket maps to exactly one location. This test is the authority for
"done": every conflict in the table above must disappear, and no future
edit can reintroduce one without the test catching it immediately.

### 3. Resolve conflicts by giving each archetype one home location and aliasing the rest

For each conflicting archetype, keep the original id at one "home" location
and introduce a new archetype id — reusing the exact same sprite/portrait
asset as the original (same uniform, different specific person; ships
routinely have more than one security officer or steward) — for every other
location it currently double-books. Each new id needs exactly two aliasing
entries, both trivial (same resource, new key):

- `AmbientWorldCharacterCatalog.Assets`: reuse the same
  `ExpressionFigure`/`Specialist` call the original id uses.
- `DialoguePortraitCatalog`: reuse the same `D(...)`/`W(...)` call the
  original id uses (same display name, same sheet/fallback/crop).

Concrete assignment (derived from the table above; a location keeps its
existing bark *text* — only the `speaker` id changes for the non-home
locations, so no dialogue content is rewritten):

| Archetype | Home | New alias id(s) for the rest |
|---|---|---|
| `CREW_SECURITY` | GANGWAY | `CREW_SECURITY_DECK` (OPEN_DECK), `CREW_SECURITY_STAIRS` (CREW_STAIRS), `CREW_SECURITY_VAULT` (VAULT) |
| `PASSENGER_A` | PORT | `PASSENGER_ATRIUM` (ATRIUM), `PASSENGER_PROMENADE_2` (PROMENADE photographer line) |
| `PASSENGER_D` | GANGWAY | `PASSENGER_NEWS` (NEWS_LOUNGE, Day-1 only), `PASSENGER_PROMENADE` (PROMENADE reporter line) |
| `PASSENGER_E` | HORIZON | `PASSENGER_DECK` (OPEN_DECK) |
| `CREW_ATTENDANT` | HORIZON | `CREW_ATTENDANT_BALLROOM` (BALLROOM, Day-1 only — Mid/Late already don't conflict) |
| `CREW_ENGINEER` | STABILIZERS | `CREW_ENGINEER_GENERATOR` (GENERATOR) |
| `BALLAST_CONTROLLER` | BALLAST_CONTROL_ANNEX | `BALLAST_CONTROLLER_TANKS` (BALLAST_TANKS) |

11 new archetype ids total. This table is the starting plan; the invariant
test from Step 2 is what actually confirms correctness tier-by-tier as the
data changes are made, so small adjustments during implementation (e.g. if
a tier's exact entry list differs slightly from this table once re-checked
against the live file) are expected and fine as long as the test ends green.

## Testing

- The new invariant test itself (Task above) is the primary coverage.
- Re-run the existing `AmbientBarkCatalogTests.EveryLocation_HasBarksAcrossAllThreeDayBands`
  and the `AmbientContentCatalogTests`/`SceneContextBarkCatalogTests` baseline-count
  tests to confirm no location loses all its barks and the total entry count
  updates correctly (adding 11 new speaker aliases doesn't add new
  `AmbientBarkRecord` rows — it only renames the `speaker` field on 11
  existing rows — so `AmbientBarkCatalog.All.Count` stays at 96).
- Full EditMode regression pass at the end, same as prior work on this
  project.
