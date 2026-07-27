using System;
using System.Collections.Generic;
using System.Linq;
using Wake.Core;
using Wake.Evidence;
namespace Wake.Narrative
{
    public enum PortraitEmotion
    {
        Neutral,
        Concerned,
        Angry,
        Positive
    }
    public enum DialogueSpeakerKind
    {
        Character,
        Monologue,
        RecordedVoice,
        Narration,
        System,
        NonPlayer
    }
    public readonly struct DialogueSpeakerIdentity
    {
        public string PortraitId { get; }
        public DialogueSpeakerKind Kind { get; }
        public DialogueSpeakerIdentity(string portraitId, DialogueSpeakerKind kind)
        {
            PortraitId = portraitId;
            Kind = kind;
        }
    }
    public static class DialoguePresentationMap
    {
        private static readonly Dictionary<string, PortraitEmotion> Emotions =
            new(StringComparer.Ordinal)
            {
                ["neutral"] = PortraitEmotion.Neutral,
                ["calm"] = PortraitEmotion.Neutral,
                ["controlled"] = PortraitEmotion.Neutral,
                ["clinical"] = PortraitEmotion.Neutral,
                ["cold"] = PortraitEmotion.Neutral,
                ["dry"] = PortraitEmotion.Neutral,
                ["observe"] = PortraitEmotion.Neutral,
                ["recorded"] = PortraitEmotion.Neutral,
                ["system"] = PortraitEmotion.Neutral,
                ["choice"] = PortraitEmotion.Neutral,
                ["confused"] = PortraitEmotion.Concerned,
                ["alarmed"] = PortraitEmotion.Concerned,
                ["ashamed"] = PortraitEmotion.Concerned,
                ["broken"] = PortraitEmotion.Concerned,
                ["defeated"] = PortraitEmotion.Concerned,
                ["fear"] = PortraitEmotion.Concerned,
                ["fake_fear"] = PortraitEmotion.Concerned,
                ["shaken"] = PortraitEmotion.Concerned,
                ["tense"] = PortraitEmotion.Concerned,
                ["accuse"] = PortraitEmotion.Angry,
                ["anger"] = PortraitEmotion.Angry,
                ["defensive"] = PortraitEmotion.Angry,
                ["defiant"] = PortraitEmotion.Angry,
                ["focused"] = PortraitEmotion.Angry,
                ["grim"] = PortraitEmotion.Angry,
                ["hard"] = PortraitEmotion.Angry,
                ["warning"] = PortraitEmotion.Angry,
                ["deduction"] = PortraitEmotion.Positive,
                ["final"] = PortraitEmotion.Positive
            };
        public static PortraitEmotion GetEmotion(string emotion)
        {
            return Emotions.TryGetValue(emotion ?? string.Empty, out PortraitEmotion mapped)
                ? mapped
                : PortraitEmotion.Neutral;
        }
        public static bool IsKnownEmotion(string emotion)
        {
            return Emotions.ContainsKey(emotion ?? string.Empty);
        }
        public static DialogueSpeakerIdentity GetSpeaker(string speaker)
        {
            return speaker switch
            {
                "ADRIAN_\uB3C5\uBC31" =>
                    new DialogueSpeakerIdentity("ADRIAN", DialogueSpeakerKind.Monologue),
                "CLAIRE(\uC120\uD0DD)" =>
                    new DialogueSpeakerIdentity("CLAIRE", DialogueSpeakerKind.Character),
                "EVELYN_RECORD" =>
                    new DialogueSpeakerIdentity("EVELYN", DialogueSpeakerKind.RecordedVoice),
                "JULIAN_RECORD" =>
                    new DialogueSpeakerIdentity("JULIAN", DialogueSpeakerKind.RecordedVoice),
                "NARRATION" =>
                    new DialogueSpeakerIdentity(string.Empty, DialogueSpeakerKind.Narration),
                "SYSTEM" =>
                    new DialogueSpeakerIdentity(string.Empty, DialogueSpeakerKind.System),
                "\uC0DD\uC874\uC790" or "\uC2B9\uBB34\uC6D0_NPC" or "\uC804\uC6D0" =>
                    new DialogueSpeakerIdentity("NPC", DialogueSpeakerKind.NonPlayer),
                _ => new DialogueSpeakerIdentity(speaker ?? string.Empty, DialogueSpeakerKind.Character)
            };
        }

        public static string GetSpeakerLabel(
            string sourceSpeaker,
            DialogueSpeakerIdentity identity)
        {
            string displayName =
                DialoguePortraitCatalog.GetDisplayName(identity.PortraitId);
            return identity.Kind switch
            {
                DialogueSpeakerKind.Monologue => $"{displayName} · 독백",
                DialogueSpeakerKind.RecordedVoice =>
                    $"{displayName} · 기록 음성",
                DialogueSpeakerKind.Narration => "내레이션",
                DialogueSpeakerKind.System => "시스템",
                DialogueSpeakerKind.NonPlayer => "승무원",
                _ => string.IsNullOrEmpty(displayName)
                    ? sourceSpeaker ?? string.Empty
                    : displayName
            };
        }
    }
    public sealed class DialogueTypedEffect
    {
        public string TargetCharacterId { get; set; } = string.Empty;
        public int TrustDelta { get; set; }
        public int AnxietyDelta { get; set; }
        public int IntegrityDelta { get; set; }
        public IReadOnlyList<string> AddFlags { get; set; } = Array.Empty<string>();
        public IReadOnlyList<string> RemoveFlags { get; set; } = Array.Empty<string>();
        public IReadOnlyList<string> CompleteObjectives { get; set; } =
            Array.Empty<string>();

        public void Apply(GameStateManager state)
        {
            if (state == null)
            {
                return;
            }
            state.ApplyChoiceEffects(TargetCharacterId, TrustDelta, AnxietyDelta, IntegrityDelta);
            foreach (string flag in AddFlags)
            {
                state.AddFlag(flag);
            }
            foreach (string flag in RemoveFlags)
            {
                state.RemoveFlag(flag);
            }
            foreach (string objectiveId in CompleteObjectives)
            {
                state.RecordCompletedObjective(objectiveId);
            }
        }
    }

    public readonly struct DialogueEffectDiagnostic
    {
        public DialogueEffectDiagnostic(DialogueRecord record, string message)
        {
            StableLineId = record?.StableLineId ?? string.Empty;
            SourceRow = record?.SourceRow ?? 0;
            Source = record?.NextOrEffect ?? string.Empty;
            Message = message ?? string.Empty;
        }

        public string StableLineId { get; }
        public int SourceRow { get; }
        public string Source { get; }
        public string Message { get; }
    }

    public static class DialogueEffectCatalog
    {
        private static readonly IReadOnlyDictionary<string, DialogueTypedEffect> ByEffectKey =
            new Dictionary<string, DialogueTypedEffect>(StringComparer.Ordinal)
            {
                ["\uBE44\uC11C\uC2E4 \uAD8C\uD55C \uD50C\uB798\uADF8"] = new()
                {
                    AddFlags = new[] { "secretary_access" }
                },
                ["\uCC9C\uC7A5 \uC870\uC0AC \uAC1C\uBC29"] = new()
                {
                    AddFlags = new[] { "ceiling_access" }
                },
                ["\uD654\uBB3C \uB808\uC77C \uC870\uC0AC \uAC1C\uBC29"] = new()
                {
                    AddFlags = new[] { "service_rail_access" }
                }
            };

        private static readonly IReadOnlyDictionary<string, DialogueTypedEffect> ByLineId =
            new Dictionary<string, DialogueTypedEffect>(StringComparer.Ordinal)
            {
                ["d1_01_04"] = F("interrogation_keywords"),
                ["d1_05_04"] = F("message_metadata"),
                ["d2_01_05"] = F("sealed_room_proposition"),
                ["d2_05_04"] = F("service_rail_foreshadowed"),
                ["d2_06_04"] = F("claire_theft_foreshadowed"),
                ["d3_03_04"] = F("vault_access_quest"),
                ["d3_04_04"] = F("marcus_pressure"),
                ["d3_05_04"] = F("evelyn_language_pattern"),
                ["d4_01_04"] = F("marcus_confession_promised"),
                ["d4_03_04"] = F("evelyn_unethical_only"),
                ["d4_04_04"] = F("vault_accomplice_connection"),
                ["d5_01_04"] = F("repeated_locked_room"),
                ["d5_02_05"] = F("daniel_tablet_recovered"),
                ["d5_03_04"] = F("informant_lure_confirmed"),
                ["d6_01_04"] = F("body_movement_confirmed"),
                ["d6_02_04"] = F("actual_murder_location_candidate"),
                ["d6_03_04"] = F("actual_scene_confirmed"),
                ["d6_04_04"] = F("evelyn_contact_evidence"),
                ["d6_05_04"] = F("final_interrogation_condition_1"),
                ["d7_01_04"] = F("evelyn_suspicion_confirmed"),
                ["d7_02_04"] = F("final_physical_evidence"),
                ["d7_03_04"] = F("past_culprit_confirmed"),
                ["d7_04_04"] = F("additional_confession_available")
            };

        public static readonly IReadOnlyList<string> ProductionEffectKeys = new[]
        {
            "A/B/C 엔딩", "Claire 적대도 변화", "Claire 절도 복선",
            "Daniel 신뢰도 ±1", "Evelyn 언어 습관 복선", "Evelyn 의심 확정",
            "Evelyn 접촉 증거", "Evelyn의 비윤리성만 확정", "Helena 신뢰도",
            "Marcus 압박", "Richard 신뢰도 분기", "Richard 완전 자백 여부",
            "게임 본편 시작", "고백 약속", "과거 진범 확정", "금고 공범 연결",
            "금고 접근 퀘스트", "도덕적 결말 톤", "메시지 메타데이터 단서",
            "밀실 명제 등록", "반복 밀실", "발견 시점 변화", "비서실 권한 플래그",
            "승객 불안 수치", "시신 이동 확정", "시신 투입 가설 1",
            "실제 살해 장소 후보", "실제 현장 확정", "심문 키워드 개방",
            "엔딩 분기", "오판 위험", "제보자 유인 의도 확정", "천장 조사 개방",
            "초기 단서 우선순위", "최종 물증", "최종 심문 조건 1",
            "추가 자백 획득 가능", "태블릿 회수", "현장 보존도",
            "화물 레일 떡밥", "화물 레일 조사 개방"
        };

        public static bool TryResolve(string source, out DialogueTypedEffect effect)
        {
            return ByEffectKey.TryGetValue(source?.Trim() ?? string.Empty, out effect);
        }

        public static bool TryResolve(
            DialogueRecord record,
            out DialogueTypedEffect effect)
        {
            effect = null;
            if (record == null || string.IsNullOrWhiteSpace(record.NextOrEffect))
            {
                return false;
            }

            return ByLineId.TryGetValue(record.StableLineId, out effect) ||
                   TryResolve(record.NextOrEffect, out effect);
        }

        public static IReadOnlyList<DialogueEffectDiagnostic> GetDiagnostics(
            IEnumerable<DialogueRecord> records)
        {
            return (records ?? Array.Empty<DialogueRecord>())
                .Where(record => !string.IsNullOrWhiteSpace(record.NextOrEffect))
                .Where(record => !TryResolve(record, out _))
                .Select(record => new DialogueEffectDiagnostic(
                    record,
                    "확정되지 않은 자연어 효과는 실행하지 않았습니다."))
                .ToArray();
        }

        private static DialogueTypedEffect F(params string[] addFlags)
        {
            return new DialogueTypedEffect { AddFlags = addFlags };
        }
    }
    public sealed class ProductionDialogueFlow
    {
        public const int ChoiceCapacity = 8;
        private readonly Dictionary<string, List<DialogueRecord>> scenes;
        private readonly HashSet<string> completedScenes;
        private readonly GameStateManager state;
        private readonly Func<string, bool> tryGrantEvidence;
        private List<DialogueRecord> activeScene = new();
        private int index;
        public DialogueRecord Current { get; private set; }
        public IReadOnlyList<DialogueRecord> Choices { get; private set; } =
            Array.Empty<DialogueRecord>();
        public IReadOnlyList<string> Warnings => warnings;
        public IReadOnlyCollection<string> CompletedSceneIds => completedScenes;
        public bool IsAwaitingChoice => Choices.Count > 0;
        public bool IsComplete { get; private set; }
        public ProductionScenePhase Phase { get; private set; } =
            ProductionScenePhase.NotStarted;
        public string PendingInteractionId { get; private set; } = string.Empty;
        public string ActiveSceneId { get; private set; } = string.Empty;
        public int CurrentIndex => index;
        private readonly List<string> warnings = new();
        public ProductionDialogueFlow(
            IEnumerable<DialogueRecord> records,
            ISet<string> completed = null,
            GameStateManager state = null,
            Func<string, bool> tryGrantEvidence = null)
        {
            scenes = records
                .GroupBy(record => record.SceneId)
                .ToDictionary(
                    group => group.Key,
                    group => group.OrderBy(record => record.Order).ToList(),
                    StringComparer.Ordinal);
            completedScenes = completed as HashSet<string> ??
                new HashSet<string>(StringComparer.Ordinal);
            NormalizeCompletedScenes(completed);
            this.state = state;
            this.tryGrantEvidence = tryGrantEvidence;

            if (state != null)
            {
                completedScenes.UnionWith(state.CompletedProductionSceneIds);
            }
        }
        public bool StartScene(string sceneId)
        {
            sceneId = NormalizeSceneId(sceneId);
            warnings.Clear();
            Choices = Array.Empty<DialogueRecord>();
            IsComplete = false;
            Phase = ProductionScenePhase.NotStarted;
            PendingInteractionId = string.Empty;
            Current = null;
            if (!scenes.TryGetValue(sceneId, out activeScene) ||
                !PrerequisitesAreMet(activeScene))
            {
                return false;
            }
            ActiveSceneId = sceneId;
            index = 0;
            Phase = ProductionScenePhase.DialogueActive;
            PresentCurrent();
            return true;
        }

        public bool RestoreScene(ProductionDialogueCheckpoint checkpoint)
        {
            if (checkpoint == null ||
                !StartScene(checkpoint.activeSceneId) ||
                checkpoint.lineIndex < 0 ||
                checkpoint.lineIndex > activeScene.Count)
            {
                return false;
            }

            index = checkpoint.lineIndex;
            Current = null;
            Choices = Array.Empty<DialogueRecord>();
            IsComplete = false;
            PresentCurrent();

            if (checkpoint.awaitingChoice != IsAwaitingChoice)
            {
                return false;
            }

            string expectedInteraction =
                ProductionSceneCompletionRequirement.NormalizeInteractionId(
                    checkpoint.pendingInteractionId);
            return string.IsNullOrEmpty(expectedInteraction) ||
                   (Phase == ProductionScenePhase.InteractionPending &&
                    string.Equals(
                        PendingInteractionId,
                        expectedInteraction,
                        StringComparison.Ordinal));
        }

        public bool IsSceneCompleted(string sceneId)
        {
            string normalized = NormalizeSceneId(sceneId);
            return completedScenes.Contains(normalized) ||
                   (state != null && state.HasCompletedScene(normalized));
        }

        public bool CompletePendingInteraction(string interactionId)
        {
            if (Phase != ProductionScenePhase.InteractionPending ||
                !ProductionSceneCompletionGate.TryComplete(
                    state,
                    ActiveSceneId,
                    interactionId))
            {
                return false;
            }

            completedScenes.Add(ActiveSceneId);
            PendingInteractionId = string.Empty;
            Phase = ProductionScenePhase.Completed;
            return true;
        }

        public IReadOnlyList<string> GetMissingPrerequisites(string sceneId)
        {
            sceneId = NormalizeSceneId(sceneId);
            if (!scenes.TryGetValue(sceneId, out List<DialogueRecord> records))
            {
                return new[] { sceneId };
            }

            return records
                .Select(record => record.Condition)
                .Where(IsPrerequisite)
                .Distinct(StringComparer.Ordinal)
                .Where(condition => !IsConditionMet(condition))
                .ToList();
        }

        public bool CanStartScene(string sceneId)
        {
            sceneId = NormalizeSceneId(sceneId);
            return scenes.ContainsKey(sceneId) &&
                   GetMissingPrerequisites(sceneId).Count == 0;
        }

        public void Advance()
        {
            if (Current == null || IsAwaitingChoice || IsComplete)
            {
                return;
            }
            ApplyEffect(Current);
            index++;
            PresentCurrent();
        }

        public bool SelectChoice(int choiceIndex)
        {
            if (!IsAwaitingChoice || choiceIndex < 0 || choiceIndex >= Choices.Count)
            {
                return false;
            }
            DialogueRecord selectedChoice = Choices[choiceIndex];
            state?.AddFlag(
                ProductionConditionEvaluator.ChoiceFlag(selectedChoice.ChoiceId));
            ApplyEffect(selectedChoice);
            InvestigationEventHub.Publish(
                InvestigationEventKind.ChoiceResolved,
                selectedChoice.StableLineId,
                ActiveSceneId);
            index += Choices.Count;
            Choices = Array.Empty<DialogueRecord>();
            PresentCurrent();
            return true;
        }

        private void PresentCurrent()
        {
            var conditions = new ProductionConditionEvaluator(state);
            while (index < activeScene.Count &&
                   !conditions.Evaluate(activeScene[index].Condition).IsMet)
            {
                index++;
            }

            if (index >= activeScene.Count)
            {
                Current = null;
                IsComplete = true;
                if (ProductionSceneCompletionCatalog.TryGet(
                        ActiveSceneId,
                        out ProductionSceneCompletionRequirement requirement) &&
                    !IsSceneCompleted(ActiveSceneId))
                {
                    PendingInteractionId = requirement.InteractionId;
                    Phase = ProductionScenePhase.InteractionPending;
                    return;
                }

                completedScenes.Add(ActiveSceneId);
                state?.RecordCompletedScene(ActiveSceneId);
                Phase = ProductionScenePhase.Completed;
                return;
            }
            if (activeScene[index].Speaker == "PLAYER_CHOICE")
            {
                List<DialogueRecord> available = activeScene
                    .Skip(index)
                    .TakeWhile(record => record.Speaker == "PLAYER_CHOICE")
                    .ToList();
                if (available.Count > ChoiceCapacity)
                {
                    warnings.Add(
                        $"Scene '{ActiveSceneId}' has {available.Count} contiguous " +
                        $"choices; only {ChoiceCapacity} can be presented.");
                }
                Choices = available.Take(ChoiceCapacity).ToList();
                Current = null;
                return;
            }
            Current = activeScene[index];
        }

        private bool PrerequisitesAreMet(IEnumerable<DialogueRecord> records)
        {
            foreach (string condition in records
                .Select(record => record.Condition)
                .Where(IsPrerequisite)
                .Distinct())
            {
                if (IsConditionMet(condition))
                {
                    continue;
                }

                if (condition == "D8-01 정답")
                {
                    warnings.Add(
                        $"Typed prerequisite '{condition}' requires ending A or B.");
                    return false;
                }

                if (scenes.ContainsKey(condition))
                {
                    return false;
                }
                else
                {
                    warnings.Add($"Unknown prerequisite '{condition}' was not evaluated.");
                    return false;
                }
            }
            return true;
        }

        private bool IsConditionMet(string condition)
        {
            if (scenes.ContainsKey(condition))
            {
                return IsSceneCompleted(condition);
            }

            return condition == "D8-01 정답" &&
                   state != null &&
                   FinalAccusationResolver.OpensD8Confession(state.FinalEndingId);
        }

        private void NormalizeCompletedScenes(IEnumerable<string> source)
        {
            if (source == null)
            {
                return;
            }

            string[] values = source.ToArray();
            completedScenes.Clear();
            foreach (string value in values)
            {
                string normalized = NormalizeSceneId(value);
                if (!string.IsNullOrEmpty(normalized))
                {
                    completedScenes.Add(normalized);
                }
            }
        }

        private static bool IsPrerequisite(string value)
        {
            return !string.IsNullOrWhiteSpace(value) &&
                   System.Text.RegularExpressions.Regex.IsMatch(
                       value.Trim(),
                       @"^(P|D\d+)-\d+$");
        }

        private static string NormalizeSceneId(string value)
        {
            return string.IsNullOrWhiteSpace(value)
                ? string.Empty
                : value.Trim().ToUpperInvariant();
        }

        private void ApplyEffect(DialogueRecord record)
        {
            foreach (string evidenceId in
                     CanonicalEvidenceCatalog.GetGrantedEvidenceIds(record.StableLineId))
            {
                tryGrantEvidence?.Invoke(evidenceId);
            }

            if (string.IsNullOrWhiteSpace(record.NextOrEffect))
            {
                return;
            }
            ProductionEffectParseResult parsed =
                ProductionEffectParser.Parse(record.NextOrEffect);
            if (parsed.Success && parsed.Instructions.Count > 0)
            {
                var executor = new ProductionEffectExecutor(state, tryGrantEvidence);
                ProductionEffectExecutionResult result =
                    executor.Execute(record.NextOrEffect);
                foreach (string warning in result.Warnings)
                {
                    warnings.Add(
                        $"{record.StableLineId} (CSV row {record.SourceRow}): {warning}");
                }
            }
            else if (DialogueEffectCatalog.TryResolve(record, out DialogueTypedEffect effect))
            {
                effect.Apply(state);
            }
            else
            {
                warnings.Add(
                    $"{record.StableLineId} (CSV row {record.SourceRow}): " +
                    $"unconfirmed effect '{record.NextOrEffect}' was not executed.");
            }
        }
    }
}
