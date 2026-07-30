# Ambient / World-Line Dialogue Variety Implementation Plan

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:subagent-driven-development (recommended) or superpowers:executing-plans to implement this plan task-by-task. Steps use checkbox (`- [ ]`) syntax for tracking.

**Goal:** Make background NPC barks and main-character world-lines vary with story day so they stop reading as tonally stuck on Day 1 once the plot has moved on.

**Architecture:** Generalize `AmbientBarkCatalog`'s condition matcher into a small compound-clause evaluator (currently a chain of exact-string comparisons), then add Day 2-4 and Day 5+ tier lines for every location and every main character. `ConditionPriority` already ranks conditioned entries above the `"always"` Day-1 default, so no selection-logic changes are needed beyond the parser.

**Tech Stack:** C#, NUnit EditMode tests.

## Global Constraints

- Non-overlapping day ranges only (`chapter>=2 and chapter<=4`, `chapter>=5`) so at most one tier is ever eligible per NPC per day — never stack open-ended `chapter>=N` conditions for the same speaker.
- No changes to `SceneContextBarkCatalog` or the scripted dialogue CSV.
- Ambient lines must stay spoiler-safe: atmosphere/rumor level only, never naming a specific culprit before the story reveals one.
- Spec: `docs/superpowers/specs/2026-07-30-ambient-dialogue-variety-design.md`

---

## Task 1: Generalize `AmbientBarkCatalog.Matches`

**Files:**
- Modify: `Assets/_Project/Code/Narrative/AmbientBarkCatalog.cs:127-132` (the 3 legacy `chapter=DayN` strings), `:280-306` (`Matches`)
- Test: Create `Assets/_Project/Tests/EditMode/AmbientBarkCatalogTests.cs` (no test file for this catalog exists yet — confirmed via search)

**Interfaces:**
- Produces: `Matches` continues to have signature `(string condition, GameStateManager state, string sceneId) : bool`; now accepts compound `X and Y` clauses where each clause is `chapter=N`, `chapter>=N`, or `chapter<=N` (new), in addition to the pre-existing `publicAnxiety`/`flag:`/`scene=` forms.

Confirmed real `GameStateManager` API used below (`Assets/_Project/Code/Core/GameStateManager.cs`): `public void SetTime(int day, TimeBlock timeBlock)` (line 551), `public int Day => data.day;` (line 137). There is no `AdvanceDay` method — use `SetTime` directly.

- [ ] **Step 1: Write the failing test**

Create `Assets/_Project/Tests/EditMode/AmbientBarkCatalogTests.cs`:

```csharp
using System.Linq;
using NUnit.Framework;
using UnityEngine;
using Wake.Core;
using Wake.Narrative;

namespace Wake.Tests
{
    public sealed class AmbientBarkCatalogTests
    {
        private GameObject host;
        private GameStateManager state;

        [SetUp]
        public void SetUp()
        {
            host = new GameObject("AmbientBarkCatalogTestState");
            state = host.AddComponent<GameStateManager>();
            state.StartNewGame();
        }

        [TearDown]
        public void TearDown()
        {
            Object.DestroyImmediate(host);
        }

        [Test]
        public void ChapterGreaterOrEqual_UsesBareIntegerAfterNormalization()
        {
            state.SetTime(1, TimeBlock.AM);
            Assert.That(
                AmbientBarkCatalog
                    .GetAvailable("HORIZON", state, maximum: 10)
                    .Any(entry => entry.Id == "HORIZON_CLOSED"),
                Is.False);

            state.SetTime(2, TimeBlock.AM);
            Assert.That(
                AmbientBarkCatalog
                    .GetAvailable("HORIZON", state, maximum: 10)
                    .Any(entry => entry.Id == "HORIZON_CLOSED"),
                Is.True);
        }

        [Test]
        public void CompoundAnxietyBand_StillMatchesAsTwoClauses()
        {
            SetAnxiety(state, 50);
            Assert.That(
                AmbientBarkCatalog
                    .GetAvailable("MEDBAY", state, maximum: 10)
                    .Any(entry => entry.Id == "MEDBAY_SECURITY"),
                Is.True);

            SetAnxiety(state, 80);
            Assert.That(
                AmbientBarkCatalog
                    .GetAvailable("MEDBAY", state, maximum: 10)
                    .Any(entry => entry.Id == "MEDBAY_SECURITY"),
                Is.False);
        }

        private static void SetAnxiety(GameStateManager target, int value)
        {
            int delta = value - target.PublicAnxiety;
            if (delta != 0)
            {
                target.ChangePublicAnxiety(delta);
            }
        }
    }
}
```

(`PublicAnxiety` is read-only — `public int PublicAnxiety => data.publicAnxiety;`
at `GameStateManager.cs:139` — so it's changed via
`public void ChangePublicAnxiety(int delta)` at `GameStateManager.cs:239`,
confirmed above.)

- [ ] **Step 2: Run test to verify it fails**

Run: `Wake.Tests.AmbientBarkCatalogTests`
Expected: `ChapterGreaterOrEqual_UsesBareIntegerAfterNormalization` FAILs at
the day-2 assertion (today's condition string is `"chapter>=Day2"`, and
today's `Matches` *does* still recognize that literal string, so this may
actually pass already — that's fine, it becomes a locked-in regression
check for Step 3's rename). `CompoundAnxietyBand_StillMatchesAsTwoClauses`
should pass already too, since today's `Matches` already special-cases that
exact compound string. Both existing behaviors must still hold after Step 3
— proceed regardless of which of the two already pass before the rewrite.

- [ ] **Step 3: Rewrite `Matches` and normalize the 3 legacy strings**

In `Assets/_Project/Code/Narrative/AmbientBarkCatalog.cs`, change the three literal conditions:

```csharp
B("HORIZON_CLOSED", "CREW_ATTENDANT",
    "호라이즌 룸은 예약이 중단되었습니다. 다른 라운지를 이용해 주세요.",
    "professional", "chapter>=2", "HORIZON"),
```

```csharp
B("HORIZON_FINALE", "PASSENGER_E",
    "탐정이 모두를 이 방에 불렀대요. 드디어 끝나는 건가요?",
    "anxious", "scene=D8-01", "HORIZON"),
```
(unchanged — uses `scene=`, not `chapter=`)

```csharp
B("ATRIUM_MEDBAY_RUMOR", "PASSENGER_A",
    "파티가 끝나기도 전에 의무실로 사람이 실려 갔다더군요.",
    "whisper", "chapter=2", "ATRIUM"),
```

```csharp
B("VIP_ROBOT", "VIP_HOST",
    "서비스 로봇은 임시 중단되었습니다. 객실 서비스가 지연될 수 있습니다.",
    "professional", "chapter=5", "VIP_LOUNGE"),
```

Replace the `Matches` method body:

```csharp
private static bool Matches(
    string condition,
    GameStateManager state,
    string sceneId)
{
    if (string.IsNullOrWhiteSpace(condition))
    {
        return false;
    }

    if (condition.StartsWith("flag:") || condition.StartsWith("scene="))
    {
        return MatchesSingle(condition, state, sceneId);
    }

    return condition
        .Split(" and ", StringSplitOptions.RemoveEmptyEntries)
        .All(clause => MatchesSingle(clause.Trim(), state, sceneId));
}

private static bool MatchesSingle(
    string condition,
    GameStateManager state,
    string sceneId)
{
    int anxiety = state?.PublicAnxiety ?? 15;
    int day = state?.Day ?? 1;
    if (condition == "always") return true;
    if (condition == "publicAnxiety<40") return anxiety < 40;
    if (condition == "publicAnxiety>=40") return anxiety >= 40;
    if (condition == "publicAnxiety<70") return anxiety < 70;
    if (condition == "publicAnxiety>=70") return anxiety >= 70;
    if (condition.StartsWith("chapter>="))
        return int.TryParse(condition.Substring(9), out int gte) &&
               day >= gte;
    if (condition.StartsWith("chapter<="))
        return int.TryParse(condition.Substring(9), out int lte) &&
               day <= lte;
    if (condition.StartsWith("chapter="))
        return int.TryParse(condition.Substring(8), out int eq) &&
               day == eq;
    if (condition.StartsWith("flag:"))
        return state?.HasFlag(condition.Substring(5)) == true;
    if (condition.StartsWith("scene="))
    {
        string required = NormalizeSceneId(condition.Substring(6));
        return !string.IsNullOrEmpty(sceneId)
            ? required == sceneId
            : state?.HasCompletedScene(required) == true ||
              state?.IsProductionSceneUnlocked(required) == true;
    }
    return false;
}
```

This drops the old `"publicAnxiety>=40 and publicAnxiety<70"` single-string
case in favor of the compound splitter handling `"publicAnxiety>=40 and
publicAnxiety<70"` as two clauses (`publicAnxiety>=40`, `publicAnxiety<70`) —
verify every existing call site in this file still uses exactly that phrase
(grep `publicAnxiety>=40 and publicAnxiety<70` in the file; it's used by
`MEDBAY_SECURITY` and `SERVICE_HUB_ENGINEER` today) so the split produces
those two known clauses.

- [ ] **Step 4: Run tests to verify they pass**

Run: `Wake.Tests.AmbientBarkCatalogTests` (whole file)
Expected: PASS — including every pre-existing test in the file (this step
doubles as the regression check that the rewrite didn't change behavior for
`"always"`, `"publicAnxiety>=70"`, `"flag:..."`, `"scene=..."`, and the
compound anxiety-band condition).

- [ ] **Step 5: Commit**

```bash
git add Assets/_Project/Code/Narrative/AmbientBarkCatalog.cs Assets/_Project/Tests/EditMode/AmbientBarkCatalogTests.cs
git commit -m "feat: generalize ambient bark chapter conditions into a compound-clause parser"
```

---

## Task 2: Day-tier bark lines for every location

**Files:**
- Modify: `Assets/_Project/Code/Narrative/AmbientBarkCatalog.cs:47-204` (`BaselineEntries`)
- Test: `Assets/_Project/Tests/EditMode/AmbientBarkCatalogTests.cs`

**Interfaces:**
- Consumes: `Matches`/compound conditions from Task 1.
- Produces: no new API — data only.

- [ ] **Step 1: Write the failing test**

Append to `AmbientBarkCatalogTests.cs`:

```csharp
[Test]
public void EveryLocation_HasBarksAcrossAllThreeDayBands()
{
    GameObject host = new("AmbientBarkDayBandCoverage");
    try
    {
        GameStateManager state = host.AddComponent<GameStateManager>();
        state.StartNewGame();

        foreach (string location in AmbientBarkCatalog.SupportedLocations)
        {
            state.SetTime(1, TimeBlock.AM);
            Assert.That(
                AmbientBarkCatalog.GetAvailable(location, state, maximum: 10),
                Is.Not.Empty,
                $"{location} day 1");

            state.SetTime(3, TimeBlock.AM);
            Assert.That(
                AmbientBarkCatalog.GetAvailable(location, state, maximum: 10),
                Is.Not.Empty,
                $"{location} day 3");

            state.SetTime(7, TimeBlock.AM);
            Assert.That(
                AmbientBarkCatalog.GetAvailable(location, state, maximum: 10),
                Is.Not.Empty,
                $"{location} day 7");
        }
    }
    finally
    {
        Object.DestroyImmediate(host);
    }
}
```

Check `GameStateManager.SetTime`'s real signature first (grep `public void SetTime` in `Assets/_Project/Code/Core/GameStateManager.cs`) and adjust the call if it takes different parameters than `(int day, TimeBlock block)`.

- [ ] **Step 2: Run test to verify it fails**

Run: `Wake.Tests.AmbientBarkCatalogTests.EveryLocation_HasBarksAcrossAllThreeDayBands`
Expected: FAIL at day 3 and day 7 for every location — today only `"always"`
entries exist for most locations, so day 1 passes trivially (still, `"always"`
also covers days 3 and 7, so this specific assertion set actually already
passes everywhere *before* this task's data is added, since `"always"` never
stops matching). This test therefore doesn't prove tier coverage by itself —
its real job is Step 4 below. Treat "expected to fail" as **optional** here;
proceed to Step 3 regardless, and rely on Step 4's stronger assertion.

- [ ] **Step 3: Add the tier lines**

Add these entries to `BaselineEntries` in `Assets/_Project/Code/Narrative/AmbientBarkCatalog.cs`, each placed right after its location's existing baseline group. `_MID` = `"chapter>=2 and chapter<=4"`, `_LATE` = `"chapter>=5"`.

```csharp
            B("PORT_ATTENDANT_MID", "DOCK_PORTER",
                "선적 목록에 조사 협조 물품 표시가 늘었습니다. 하선 심사도 그만큼 길어지고 있어요.",
                "uneasy", "chapter>=2 and chapter<=4", "PORT"),
            B("PORT_ATTENDANT_LATE", "DOCK_PORTER",
                "이번 항차 이야기가 항구까지 퍼졌더군요. 손님들 질문이 짐 확인보다 그쪽에 더 많습니다.",
                "dry", "chapter>=5", "PORT"),

            B("GANGWAY_SECURITY_MID", "CREW_SECURITY",
                "출입 기록을 이중으로 대조하라는 지시가 내려왔습니다. 평소보다 시간이 걸릴 겁니다.",
                "firm", "chapter>=2 and chapter<=4", "GANGWAY"),
            B("GANGWAY_SECURITY_LATE", "PASSENGER_D",
                "이 통로 경비가 처음보다 갑절은 늘었어요. 다들 말은 안 해도 이유는 알죠.",
                "uneasy", "chapter>=5", "GANGWAY"),

            B("RICHARD_SUITE_MID", "SUITE_STEWARD",
                "회장님은 요즘 문을 걸어 잠그고 서류만 보십니다. 식사도 방으로 들여보내라 하셨어요.",
                "quiet", "chapter>=2 and chapter<=4", "RICHARD_SUITE"),
            B("RICHARD_SUITE_LATE", "SUITE_STEWARD",
                "기자분들 문의가 늘어 응대 인력을 더 붙였습니다. 회장님은 아직 공식 입장이 없으십니다.",
                "professional", "chapter>=5", "RICHARD_SUITE"),

            B("VIP_ATTENDANT_MID", "PASSENGER_B",
                "카드 테이블보다 요즘은 다들 그 이야기뿐이에요. 판돈 이야기가 쏙 들어갔죠.",
                "dry", "chapter>=2 and chapter<=4", "VIP_LOUNGE"),
            B("VIP_ATTENDANT_LATE", "VIP_HOST",
                "예약 취소가 이어지고 있습니다. 남으신 분들도 라운지보다 방을 더 찾으세요.",
                "professional", "chapter>=5", "VIP_LOUNGE"),

            B("OPEN_DECK_SECURITY_MID", "CREW_SECURITY",
                "야간 갑판 통제 구역을 넓혔습니다. 안내선 밖으로는 나가지 말아 주십시오.",
                "firm", "chapter>=2 and chapter<=4", "OPEN_DECK"),
            B("OPEN_DECK_NATURALIST_LATE", "PASSENGER_E",
                "돌고래는커녕 요즘은 순찰 도는 것만 보여요. 다들 여기서도 목소리를 낮추네요.",
                "uneasy", "chapter>=5", "OPEN_DECK"),

            B("BALLROOM_SINGER_MID", "BALLROOM_MUSICIAN",
                "무대는 다시 치웠습니다. 오늘은 음악보다 통제선이 먼저 보이네요.",
                "uneasy", "chapter>=2 and chapter<=4", "BALLROOM"),
            B("BALLROOM_SINGER_LATE", "CREW_ATTENDANT",
                "다들 이 방보다 회의실 쪽 소식을 더 궁금해합니다. 좌석표 확인하는 분도 줄었어요.",
                "uneasy", "chapter>=5", "BALLROOM"),

            B("DINING_ATTENDANT_MID", "DINING_SOMMELIER",
                "요즘은 식사 시간을 줄여 달라는 분이 많습니다. 자리 배치도 그래서 조용한 구석부터 채웁니다.",
                "professional", "chapter>=2 and chapter<=4", "DINING"),
            B("DINING_GUEST_LATE", "PASSENGER_C",
                "다들 식사보다 신문 얘기가 먼저예요. 와인 봉인 이야기는 이제 아무도 안 물어요.",
                "dry", "chapter>=5", "DINING"),

            B("PROMENADE_PHOTOGRAPHER_MID", "PASSENGER_A",
                "요즘은 사진보다 순찰대와 마주치는 일이 더 많아요. 복도가 예전만큼 한가하지 않습니다.",
                "uneasy", "chapter>=2 and chapter<=4", "PROMENADE"),
            B("PROMENADE_REPORTER_LATE", "PASSENGER_D",
                "그 금속 끌리는 소리는 이제 아무도 신경 안 써요. 다들 더 큰 소문에 정신이 팔렸죠.",
                "dry", "chapter>=5", "PROMENADE"),

            B("HORIZON_ATTENDANT_MID", "CREW_ATTENDANT",
                "호라이즌 룸 예약은 당분간 조사 우선으로 조정됩니다. 양해 부탁드립니다.",
                "professional", "chapter>=2 and chapter<=4", "HORIZON"),
            B("HORIZON_NATURALIST_LATE", "PASSENGER_E",
                "이 방에서 수평선을 보는 분보다 서류를 든 분이 더 많아졌어요.",
                "quiet", "chapter>=5", "HORIZON"),

            B("ATRIUM_PHOTOGRAPHER_MID", "PASSENGER_A",
                "유리 바닥 사진 찍는 분보다 안내원 붙잡고 묻는 분이 더 많아졌어요.",
                "curious", "chapter>=2 and chapter<=4", "ATRIUM"),
            B("ATRIUM_ATTENDANT_LATE", "ATRIUM_GUIDE",
                "행사 안내보다 문의 응대가 더 늘었습니다. 오늘 일정은 대부분 취소됐어요.",
                "professional", "chapter>=5", "ATRIUM"),

            B("NEWS_COFFEE_MID", "PASSENGER_F",
                "요즘은 커피보다 송고 단말기 앞 줄이 더 길어요. 다들 최신 기사부터 확인하죠.",
                "dry", "chapter>=2 and chapter<=4", "NEWS_LOUNGE"),
            B("NEWS_REPORTER_LATE", "PASSENGER_B",
                "다니엘 머서 이름으로 된 기사가 다시 인용되고 있어요. 편집부도 조심스러워합니다.",
                "uneasy", "chapter>=5", "NEWS_LOUNGE"),

            B("SECURITY_OFFICER_MID", "SECURITY_OPERATOR",
                "교대 인원을 늘렸습니다. 신원 미확인자는 이 구역에 오래 머물 수 없습니다.",
                "firm", "chapter>=2 and chapter<=4", "SECURITY"),
            B("SECURITY_OFFICER_LATE", "SECURITY_OPERATOR",
                "출입 기록을 매시간 대조합니다. 최근엔 사소한 불일치도 그냥 넘기지 않습니다.",
                "commanding", "chapter>=5", "SECURITY"),

            B("SERVICE_RAIL_ENGINEER_MID", "RAIL_TECHNICIAN",
                "레일 운행 기록을 전부 다시 남기고 있습니다. 예전보다 절차가 늘었어요.",
                "professional", "chapter>=2 and chapter<=4", "SERVICE_RAIL"),
            B("SERVICE_RAIL_ENGINEER_LATE", "RAIL_TECHNICIAN",
                "이 레일 얘기는 이제 저희끼리도 잘 안 꺼냅니다. 기록만 넘기고 맙니다.",
                "quiet", "chapter>=5", "SERVICE_RAIL"),

            B("MEDBAY_ATTENDANT_MID", "SHIP_MEDIC",
                "면회 제한이 더 엄격해졌습니다. 접수대에서 방문 사유부터 확인합니다.",
                "professional", "chapter>=2 and chapter<=4", "MEDBAY"),
            B("MEDBAY_ATTENDANT_LATE", "SHIP_MEDIC",
                "요즘은 진료보다 기록 요청이 더 많습니다. 승인 없이는 열람할 수 없다고 매번 안내드려요.",
                "clinical", "chapter>=5", "MEDBAY"),

            B("BALLAST_ANNEX_ENGINEER_MID", "BALLAST_CONTROLLER",
                "출입자 명단을 매번 새로 받고 있습니다. 절차가 늘어난 건 다들 이해해 주십니다.",
                "professional", "chapter>=2 and chapter<=4", "BALLAST_CONTROL_ANNEX"),
            B("BALLAST_ANNEX_ENGINEER_LATE", "BALLAST_CONTROLLER",
                "밸브 점검 기록을 다시 정리해 달라는 요청이 왔습니다. 예전 기록까지 전부요.",
                "focused", "chapter>=5", "BALLAST_CONTROL_ANNEX"),

            B("ENGINE_CONTROL_ENGINEER_MID", "CHIEF_ENGINEER",
                "출력 기록을 하루 단위로 제출하고 있습니다. 평소엔 없던 절차입니다.",
                "professional", "chapter>=2 and chapter<=4", "ENGINE_CONTROL"),
            B("ENGINE_CONTROL_ENGINEER_LATE", "CHIEF_ENGINEER",
                "기관실 얘기도 이젠 조사 대상이더군요. 저희도 기록만 성실히 남길 뿐입니다.",
                "matter_of_fact", "chapter>=5", "ENGINE_CONTROL"),

            B("CREW_STAIRS_SECURITY_MID", "CREW_SECURITY",
                "이 계단 출입 기록도 전부 대조 대상입니다. 허가증을 꼭 챙겨 주십시오.",
                "firm", "chapter>=2 and chapter<=4", "CREW_STAIRS"),
            B("CREW_STAIRS_SECURITY_LATE", "CREW_SECURITY",
                "이 계단 얘기는 저희끼리도 조심스럽습니다. 사고 이후로 다들 예민해졌어요.",
                "uneasy", "chapter>=5", "CREW_STAIRS"),

            B("VAULT_SECURITY_MID", "CREW_SECURITY",
                "이중 인증 기록을 매일 보안실로 넘기고 있습니다. 예전엔 주간 단위였습니다.",
                "firm", "chapter>=2 and chapter<=4", "VAULT"),
            B("VAULT_SECURITY_LATE", "CREW_SECURITY",
                "보관고 근처엔 이제 혼자 오는 분이 없습니다. 다들 둘씩 짝지어 다니시더군요.",
                "uneasy", "chapter>=5", "VAULT"),

            B("ARCHIVE_SECURITY_MID", "ARCHIVIST",
                "열람 신청서를 다시 받고 있습니다. 사유란을 비워 두시면 안내해 드릴 수 없어요.",
                "firm", "chapter>=2 and chapter<=4", "ARCHIVE"),
            B("ARCHIVE_SECURITY_LATE", "ARCHIVIST",
                "요즘은 옛날 기록 요청이 부쩍 늘었습니다. 다들 뭘 찾는지는 안 여쭤봅니다.",
                "dry", "chapter>=5", "ARCHIVE"),

            B("LAUNDRY_ATTENDANT_MID", "LAUNDRY_SUPERVISOR",
                "제복 세탁 라인에 표식 확인 절차가 하나 늘었습니다. 번거로워도 양해 부탁드려요.",
                "professional", "chapter>=2 and chapter<=4", "LAUNDRY"),
            B("LAUNDRY_ATTENDANT_LATE", "LAUNDRY_SUPERVISOR",
                "요즘은 세탁물보다 이야기가 더 많이 돌아요. 저는 투입구 번호만 볼 뿐입니다.",
                "dry", "chapter>=5", "LAUNDRY"),

            B("SERVICE_HUB_ATTENDANT_MID", "ROBOTICS_TECH",
                "충전 순번을 다시 정리하고 있습니다. 로봇 한 대가 아직 회수되지 않았거든요.",
                "focused", "chapter>=2 and chapter<=4", "SERVICE_HUB"),
            B("SERVICE_HUB_ATTENDANT_LATE", "ROBOTICS_TECH",
                "그 로봇 얘기는 이제 여기서도 꺼내기 조심스럽습니다. 점검만 계속하고 있어요.",
                "uneasy", "chapter>=5", "SERVICE_HUB"),

            B("STABILIZERS_ENGINEER_MID", "CREW_ENGINEER",
                "점검 주기를 반으로 줄였습니다. 흰 선 안쪽엔 여전히 서지 말아 주세요.",
                "professional", "chapter>=2 and chapter<=4", "STABILIZERS"),
            B("STABILIZERS_ENGINEER_LATE", "CREW_ENGINEER",
                "안정기는 정상입니다. 다만 요즘은 저희도 기록을 두 번씩 확인합니다.",
                "matter_of_fact", "chapter>=5", "STABILIZERS"),

            B("BALLAST_TANKS_ENGINEER_MID", "BALLAST_CONTROLLER",
                "수위 기록을 매 교대마다 남기고 있습니다. 예전보다 촘촘해졌어요.",
                "professional", "chapter>=2 and chapter<=4", "BALLAST_TANKS"),
            B("BALLAST_TANKS_ENGINEER_LATE", "BALLAST_CONTROLLER",
                "여기까지 조사가 내려올 줄은 몰랐습니다. 기록은 늘 그대로였는데 말이죠.",
                "uneasy", "chapter>=5", "BALLAST_TANKS"),

            B("GENERATOR_ENGINEER_MID", "CREW_ENGINEER",
                "출입 인원을 하나씩 기록하고 있습니다. 보호구 착용도 다시 한번 확인해 주세요.",
                "professional", "chapter>=2 and chapter<=4", "GENERATOR"),
            B("GENERATOR_ENGINEER_LATE", "CREW_ENGINEER",
                "발전기 자체는 문제없습니다. 다만 요즘은 누가 언제 들어왔는지가 더 중요해졌어요.",
                "matter_of_fact", "chapter>=5", "GENERATOR"),

            B("WORKSHOP_ENGINEER_MID", "WORKSHOP_MACHINIST",
                "공구 반출 장부를 매일 마감하고 있습니다. 이전엔 주 단위였어요.",
                "professional", "chapter>=2 and chapter<=4", "WORKSHOP"),
            B("WORKSHOP_ENGINEER_LATE", "WORKSHOP_MACHINIST",
                "안정기 도면은 아직 저희 작업대에 있습니다. 찾는 분이 있으면 저한테 먼저 말씀하세요.",
                "focused", "chapter>=5", "WORKSHOP")
```

- [ ] **Step 4: Run test to verify it passes**

Run: `Wake.Tests.AmbientBarkCatalogTests.EveryLocation_HasBarksAcrossAllThreeDayBands`
Expected: PASS. Then also run the full file to confirm no regressions:
`Wake.Tests.AmbientBarkCatalogTests`.

- [ ] **Step 5: Commit**

```bash
git add Assets/_Project/Code/Narrative/AmbientBarkCatalog.cs Assets/_Project/Tests/EditMode/AmbientBarkCatalogTests.cs
git commit -m "feat: add day 2-4 and day 5+ ambient bark tiers for every location"
```

---

## Task 3: Day-tier world-lines for main characters

**Files:**
- Modify: `Assets/_Project/Code/Exploration/MainCharacterWorldLineCatalog.cs`, `Assets/_Project/Code/Exploration/AmbientCharacterHotspotOverlay.cs:660-738` (the two call sites)
- Test: Create `Assets/_Project/Tests/EditMode/MainCharacterWorldLineCatalogTests.cs`

**Interfaces:**
- Produces: `MainCharacterWorldLineCatalog.Get(string characterId, SceneCharacterState state, int day) : string`, `GetCompleted(string characterId, SceneCharacterState state, int day) : string` (both gain the `day` parameter — signature change, both call sites must be updated in this same task).

- [ ] **Step 1: Write the failing tests**

Create `Assets/_Project/Tests/EditMode/MainCharacterWorldLineCatalogTests.cs`:

```csharp
using System.Linq;
using NUnit.Framework;
using Wake.Exploration;

namespace Wake.Tests
{
    public sealed class MainCharacterWorldLineCatalogTests
    {
        private static readonly string[] DayTieredCharacters =
        {
            "RICHARD", "EVELYN", "CLAIRE", "THOMAS",
            "MARCUS", "HELENA", "OWEN"
        };

        [Test]
        public void EveryDayTieredCharacter_HasThreeDistinctNormalStateLines()
        {
            foreach (string character in DayTieredCharacters)
            {
                string day1 = MainCharacterWorldLineCatalog.Get(
                    character, SceneCharacterState.Normal, 1);
                string day3 = MainCharacterWorldLineCatalog.Get(
                    character, SceneCharacterState.Normal, 3);
                string day7 = MainCharacterWorldLineCatalog.Get(
                    character, SceneCharacterState.Normal, 7);

                Assert.That(
                    new[] { day1, day3, day7 }.Distinct().Count(),
                    Is.EqualTo(3),
                    character);
            }
        }

        [Test]
        public void EveryDayTieredCharacter_HasThreeDistinctCompletedLines()
        {
            foreach (string character in DayTieredCharacters)
            {
                string day1 = MainCharacterWorldLineCatalog.GetCompleted(
                    character, SceneCharacterState.Normal, 1);
                string day3 = MainCharacterWorldLineCatalog.GetCompleted(
                    character, SceneCharacterState.Normal, 3);
                string day7 = MainCharacterWorldLineCatalog.GetCompleted(
                    character, SceneCharacterState.Normal, 7);

                Assert.That(
                    new[] { day1, day3, day7 }.Distinct().Count(),
                    Is.EqualTo(3),
                    character);
            }
        }

        [TestCase(1)]
        [TestCase(3)]
        [TestCase(7)]
        public void InjuredAndDetained_IgnoreDayAndOverrideNormalLine(
            int day)
        {
            Assert.That(
                MainCharacterWorldLineCatalog.Get(
                    "MARCUS", SceneCharacterState.Injured, day),
                Is.EqualTo("부상 부위가 아직 좋지 않습니다. 필요한 내용만 짧게 묻죠."));
            Assert.That(
                MainCharacterWorldLineCatalog.Get(
                    "EVELYN", SceneCharacterState.Detained, day),
                Is.EqualTo("경비가 지켜보는 자리군요. 정식 심문에서 같은 답을 드리겠습니다."));
        }

        [Test]
        public void Daniel_KeepsHisSingleLineRegardlessOfDay()
        {
            string day1 = MainCharacterWorldLineCatalog.Get(
                "DANIEL", SceneCharacterState.Normal, 1);
            string day3 = MainCharacterWorldLineCatalog.Get(
                "DANIEL", SceneCharacterState.Normal, 3);

            Assert.That(day1, Is.EqualTo(day3));
        }
    }
}
```

- [ ] **Step 2: Run tests to verify they fail**

Run: `Wake.Tests.MainCharacterWorldLineCatalogTests`
Expected: FAIL with a compile error (`Get`/`GetCompleted` don't accept a
third `int` argument yet).

- [ ] **Step 3: Add day tiers and update the signatures**

Replace the full contents of
`Assets/_Project/Code/Exploration/MainCharacterWorldLineCatalog.cs`:

```csharp
using System;
using System.Collections.Generic;

namespace Wake.Exploration
{
    public static class MainCharacterWorldLineCatalog
    {
        private sealed class DayTieredLines
        {
            public DayTieredLines(string early, string mid, string late)
            {
                Early = early;
                Mid = mid;
                Late = late;
            }

            public string Early { get; }
            public string Mid { get; }
            public string Late { get; }

            public string ForDay(int day) =>
                day <= 1 ? Early : day <= 4 ? Mid : Late;
        }

        private static readonly IReadOnlyDictionary<string, DayTieredLines>
            Lines = new Dictionary<string, DayTieredLines>(
                StringComparer.Ordinal)
            {
                ["DANIEL"] = new DayTieredLines(
                    "확인되지 않은 소문보다 기록을 보시죠. 제가 본 순서대로 말씀드리겠습니다.",
                    "확인되지 않은 소문보다 기록을 보시죠. 제가 본 순서대로 말씀드리겠습니다.",
                    "확인되지 않은 소문보다 기록을 보시죠. 제가 본 순서대로 말씀드리겠습니다."),
                ["RICHARD"] = new DayTieredLines(
                    "동선에 관해서라면 기록으로 답하겠네. 추측으로 일을 키우진 말게.",
                    "가문 이름이 걸린 일이야. 확인된 것 외엔 아무 말도 하지 않겠네.",
                    "이젠 숨길 것도 별로 없네. 다만 묻는 순서는 지켜 주게."),
                ["EVELYN"] = new DayTieredLines(
                    "지금 공개할 수 있는 범위는 여기까지예요. 정식 질문이라면 답하겠습니다.",
                    "회사와 가족, 둘 다 지켜야 할 입장이라는 것만 알아 두세요.",
                    "제가 뭘 감추고 있다고 생각하시는군요. 틀린 짐작은 아니겠죠."),
                ["CLAIRE"] = new DayTieredLines(
                    "사람들이 불안해하고 있어요. 필요한 질문이라면 조용한 곳에서 해 주세요.",
                    "제 방 얘기라면 이미 다 말씀드렸어요. 같은 질문 반복하지 말아 주세요.",
                    "다들 절 그렇게 보시는 거 알아요. 그래도 대답할 건 대답할게요."),
                ["THOMAS"] = new DayTieredLines(
                    "장비 상태부터 확인해야 합니다. 수치와 기록은 숨기지 않겠습니다.",
                    "기관 기록은 요청하시면 그대로 넘겨드립니다. 지어낼 이유가 없어요.",
                    "원본 기록 얘기는 이제 저도 조심스럽습니다. 아는 만큼만 말씀드리죠."),
                ["MARCUS"] = new DayTieredLines(
                    "통제 기록을 확인 중입니다. 경비 동선에 관한 질문은 정확히 해 주십시오.",
                    "인증 기록을 다시 정리하고 있습니다. 지금은 그것만 봐 주십시오.",
                    "저도 예전 같지 않다는 거 압니다. 그래도 확인할 건 확인하겠습니다."),
                ["HELENA"] = new DayTieredLines(
                    "환자와 현장 보존이 우선이에요. 의학적으로 확인된 사실만 말씀드리죠.",
                    "검시 소견은 아직 정리 중입니다. 성급한 결론은 원치 않아요.",
                    "제가 본 걸 다 말씀드렸다고 생각했는데, 또 여쭤보시는군요."),
                ["OWEN"] = new DayTieredLines(
                    "기계는 흔적을 남깁니다. 정비 기록과 실제 손상부터 대조해 보죠.",
                    "정비 기록은 매일 새로 남기고 있습니다. 궁금하신 부분 짚어 주세요.",
                    "기계는 거짓말 안 합니다. 사람 쪽 이야기는 제 몫이 아니고요.")
            };

        public static string Get(
            string characterId,
            SceneCharacterState state,
            int day)
        {
            if (state == SceneCharacterState.Injured)
            {
                return "부상 부위가 아직 좋지 않습니다. 필요한 내용만 짧게 묻죠.";
            }

            if (state == SceneCharacterState.Detained)
            {
                return "경비가 지켜보는 자리군요. 정식 심문에서 같은 답을 드리겠습니다.";
            }

            string key = characterId?.Trim().ToUpperInvariant() ?? "";
            return Lines.TryGetValue(key, out DayTieredLines lines)
                ? lines.ForDay(day)
                : "지금 확인 중인 내용이 있습니다. 정식 질문이라면 답하겠습니다.";
        }

        public static string GetEmotion(SceneCharacterState state)
        {
            return state switch
            {
                SceneCharacterState.Injured => "strained",
                SceneCharacterState.Detained => "guarded",
                _ => "neutral"
            };
        }

        public static string GetCompleted(
            string characterId,
            SceneCharacterState state,
            int day)
        {
            if (state == SceneCharacterState.Injured)
            {
                return "지금은 더 이야기하기 어렵습니다. 앞서 말씀드린 내용을 확인해 주세요.";
            }

            if (state == SceneCharacterState.Detained)
            {
                return "이미 진술을 마쳤습니다. 추가 내용은 정식 심문에서 말씀드리겠습니다.";
            }

            string key = characterId?.Trim().ToUpperInvariant() ?? string.Empty;
            int tier = day <= 1 ? 0 : day <= 4 ? 1 : 2;
            return (key, tier) switch
            {
                ("DANIEL", _) => "이미 말씀드릴 수 있는 건 전부 말씀드렸습니다.",
                ("RICHARD", 0) => "같은 질문에는 같은 답밖에 해 줄 수 없네.",
                ("RICHARD", 1) => "가문 이름을 걸고 이미 말했네. 더는 보탤 게 없어.",
                ("RICHARD", _) => "이제 와서 더 말한다고 달라질 게 있겠나.",
                ("EVELYN", 0) => "제 진술은 끝났습니다. 기록을 확인해 주세요.",
                ("EVELYN", 1) => "드릴 수 있는 답은 이미 다 드렸어요.",
                ("EVELYN", _) => "더 물으셔도 같은 답뿐이에요. 기록을 보세요.",
                ("CLAIRE", 0) => "조금 전 말씀드린 내용이 전부예요.",
                ("CLAIRE", 1) => "그 얘기는 이미 끝났잖아요. 다른 걸 물어봐 주세요.",
                ("CLAIRE", _) => "몇 번을 물으셔도 대답은 똑같아요.",
                ("THOMAS", 0) => "정비 기록 외에 덧붙일 내용은 없습니다.",
                ("THOMAS", 1) => "기록은 이미 넘겨드렸습니다. 그대로입니다.",
                ("THOMAS", _) => "제가 아는 건 이미 다 말씀드렸습니다.",
                ("MARCUS", 0) => "진술은 기록됐습니다. 추가 사항이 생기면 보고하겠습니다.",
                ("MARCUS", 1) => "인증 기록은 이미 제출했습니다. 확인해 보십시오.",
                ("MARCUS", _) => "더 드릴 말씀은 없습니다. 기록이 전부입니다.",
                ("HELENA", 0) => "검시 소견은 이미 전달했습니다. 기록을 확인해 주세요.",
                ("HELENA", 1) => "소견서에 적은 내용이 전부예요. 더는 추측하지 않겠습니다.",
                ("HELENA", _) => "몇 번을 여쭤보셔도 소견은 그대로예요.",
                ("OWEN", 0) => "기관 기록과 제 진술은 이미 제출했습니다.",
                ("OWEN", 1) => "정비 기록은 이미 넘겨드렸습니다. 달라질 게 없어요.",
                ("OWEN", _) => "기계는 그대로고, 제 답도 그대로입니다.",
                _ => "앞서 말씀드린 내용이 전부입니다."
            };
        }
    }
}
```

- [ ] **Step 4: Update the two call sites**

In `Assets/_Project/Code/Exploration/AmbientCharacterHotspotOverlay.cs`,
`StartMainCharacterDialogue` (`Wake.Core.GameStateManager state` is already
resolved at the top of the method), change every
`MainCharacterWorldLineCatalog.Get(...)` /
`MainCharacterWorldLineCatalog.GetCompleted(...)` call (there are 4: two
`GetCompleted` in the focus-participant early-return and fallback branches,
one `GetCompleted` in the non-focus already-completed branch, one `Get` in
the final `StartAmbientLine` call) to pass `state?.Day ?? 1` as the third
argument. Example for one of the four (apply the same pattern to all four):

```csharp
dialogue.StartAmbientLine(
    character.CharacterId,
    MainCharacterWorldLineCatalog.GetCompleted(
        character.CharacterId,
        character.State,
        state?.Day ?? 1),
    MainCharacterWorldLineCatalog.GetEmotion(
        character.State));
```

- [ ] **Step 5: Run tests to verify they pass**

Run: `Wake.Tests.MainCharacterWorldLineCatalogTests`
Expected: PASS.

- [ ] **Step 6: Verify the project compiles**

The 4 call sites in `AmbientCharacterHotspotOverlay.cs` are not covered by
an EditMode test (that class needs a live Canvas). Open Unity and confirm
the Console shows no compile errors for either modified file.

- [ ] **Step 7: Commit**

```bash
git add Assets/_Project/Code/Exploration/MainCharacterWorldLineCatalog.cs Assets/_Project/Code/Exploration/AmbientCharacterHotspotOverlay.cs Assets/_Project/Tests/EditMode/MainCharacterWorldLineCatalogTests.cs
git commit -m "feat: add day-tiered world-lines for main characters outside their focus scene"
```

---

## Task 4: Full regression pass

**Files:** none (verification only)

- [ ] **Step 1: Run the full EditMode suite**

Run: `Unity -batchmode -runTests -testPlatform EditMode` (or the
`mcp__UnityMCP__run_tests` MCP tool with `mode: "EditMode"`, no filter)
Expected: PASS, zero failures beyond any already-known pre-existing/
environment-specific failures unrelated to this branch (cross-check against
`main` if anything unexpected shows up, the same way prior work on this
project isolated the Typography-asset test noise before concluding it was
unrelated — don't assume a new failure is pre-existing without that check).

- [ ] **Step 2: Manual spot-check in the Unity Editor**

1. Enter Play mode, start a new game, and visit 2-3 locations on Day 1 —
   confirm the existing baseline barks still play.
2. Use debug/skip tooling (or `GameStateManager.SetTime`) to jump to Day 3
   and revisit the same locations — confirm the `_MID` tier lines now play
   instead of the Day-1 ones.
3. Jump to Day 7 and revisit again — confirm the `_LATE` tier lines play.
4. Click a non-focus main character (or a focus character after their scene
   is done) at Day 1, Day 3, and Day 7 — confirm the world-line text changes
   across the three visits.

If any step fails, stop and re-open the relevant task above rather than
patching ad hoc.
