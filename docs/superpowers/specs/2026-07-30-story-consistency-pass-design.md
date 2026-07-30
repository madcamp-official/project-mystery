# Story Consistency Pass (Round 1)

## Problem

User-requested audit of the scripted dialogue
(`Under_the_Horizon_Dialogue_KR.csv`, 1063 rows / 41 scenes) for three
things: a specific known contradiction about Daniel's coat color, any other
awkward/inconsistent story beats, and missing self-introductions for major
characters Adrian meets for the first time.

A full-script audit (via a research fork reading the entire CSV plus
`CanonicalEvidenceCatalog.cs`/`ScenePresenceCatalog.cs` for cross-checks)
found the story is largely tight — only a handful of concrete issues, not a
sweeping rewrite. All are confirmed and approved for this pass.

## Scope

Text-only edits to `Under_the_Horizon_Dialogue_KR.csv`. No new rows are
inserted (avoids renumbering every subsequent `order` value in an affected
scene) — self-introductions are prefixed onto each character's existing
first line as an additional sentence, and the other fixes are in-place text
corrections. No code changes.

## Findings and fixes

### 1. Daniel's coat color contradiction

`P-01_002` (opening narration) establishes his collar as teal:
"청록색 옷깃은 구겨져 있었고". `D1-04_003` has Adrian ask a crew member about
"검은 코트를 입은 기자" (a reporter in a *black* coat) — the only other
clothing-color reference to Daniel in the entire project. Fix: change
`D1-04_003` to reference the established teal collar instead of inventing a
black coat.

### 2. "Three vs. four exits" phrasing gap

`D2-01_002` (Marcus): "출입문 외 가능 경로는 세 곳입니다" (three routes
*besides* the door). `D2-01_024` (Adrian): "출구는 셋이 아니라 넷이었지만"
(exits were four, not three). The math is right (3 routes + 1 door = 4) but
nothing ever explicitly said "three total exits," so "넷이었지만" reads as
contradicting a claim nobody made. Fix: reword `D2-01_024` to explicitly
tie back to Marcus's three routes plus the door, so the "four" lands as a
sum instead of a correction.

### 3. CSV column-shift data bug

`D7-01_004` and `D7-04_008` (both `speaker=NARRATION` rows) have a location
code (`VAULT`, `PROMENADE` respectively) sitting in the `emotion` column,
and an empty `stage_direction` column — the two values are swapped/shifted
relative to every other row in the file. `DialoguePresentationMap.GetEmotion`
silently falls back to Neutral for an unrecognized emotion string, so this
hasn't crashed anything, but it's a genuine authoring defect. Fix: move the
location value to `stage_direction` and fill `emotion` with a tone matching
the surrounding narration rows (`observe`, matching the convention already
used for other narration beats in the same scenes).

### 4. Self-introductions for the seven characters Adrian meets fresh

Confirmed via the audit: none of RICHARD, EVELYN, CLAIRE, MARCUS, HELENA,
OWEN, or THOMAS ever states their own name in dialogue. User confirmed:
include all seven (including Richard, whom Adrian has been hired by but not
yet met face-to-face on screen; and Thomas, even though the preceding
narration already names him).

Each gets one short self-identifying sentence prefixed onto their existing
first line of dialogue with Adrian (verified exact insertion point per
character below), keeping their established voice:

| Character | Row edited | Existing first line (unchanged, kept after the new sentence) |
|---|---|---|
| RICHARD | `P-03_002` | "머서는 초대받지 않았소..." |
| EVELYN | `P-02_017` (her first line actually directed at Adrian, inside the `P-02_C1` interrogation branch — her earlier `P-02_002`/`004`/`006` lines are addressed to Daniel/Richard, not Adrian) | "네. 탑승 지연을 막기 위한 행정 처리였습니다..." |
| CLAIRE | `D1-01_007` | "아드리안 베일. 삼촌이 부른 탐정이군요..." |
| MARCUS | `D1-01_012` | "승객용 구역과 서비스 구역을 구분해 주십시오..." |
| HELENA | `D1-01_017` | "배멀미약이 필요하신가요..." |
| OWEN | `D1-01_022` | "유리 바닥 가운데 서지 마십시오..." |
| THOMAS | `D1-07_002` | "가까운 항구까지 열두 시간..." |

Proposed prefix sentences (prepended to the existing `text_ko`, same cell,
no new row):

- RICHARD: "리처드 호손이오."
- EVELYN: "이블린 쇼입니다. 회장 비서실 소속이고요."
- CLAIRE: "클레어 호손입니다."
- MARCUS: "보안 책임자 마커스 벨입니다."
- HELENA: "의무실 담당 헬레나 워드예요."
- OWEN: "기관사 오언 프라이스입니다."
- THOMAS: "선장 토머스 리드입니다."

### 5. Evelyn's 15-years-deep involvement vs. her "no personal relationship" line

Audit judged this as thin foreshadowing rather than a contradiction — she's
telling the truth about Daniel specifically, and the depth of her older
history with the Hawthorne family plausibly reads as intended late-story
misdirection in a mystery. User confirmed: leave as-is, no change.

## Testing

These are CSV text edits with no code changes, so there's no new unit
coverage to add. Verification is:
- `DialogueCsvParser`/`DialogueContentValidator`/`OfficialDialogueContractValidator`
  EditMode tests (already existing) must still pass — they validate the CSV
  parses cleanly and every referenced condition/effect is well-formed, which
  would catch a malformed edit.
- Full EditMode regression pass at the end, same as prior work on this
  project.
- Manual read-through of each edited line in context (via the CSV, not
  necessarily in-Editor) to confirm the prefixed sentence reads naturally
  and doesn't break the line's pagination/length assumptions.
