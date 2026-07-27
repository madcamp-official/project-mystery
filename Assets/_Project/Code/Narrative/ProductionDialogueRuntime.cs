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
                ["ARCHIVE"] = PortraitEmotion.Neutral,
                ["CABIN_DANIEL"] = PortraitEmotion.Neutral,
                ["PORT"] = PortraitEmotion.Neutral,
                ["PROMENADE"] = PortraitEmotion.Neutral,
                ["STERN"] = PortraitEmotion.Neutral,
                ["VAULT"] = PortraitEmotion.Neutral,
                ["afraid"] = PortraitEmotion.Concerned,
                ["alarmed"] = PortraitEmotion.Concerned,
                ["alert"] = PortraitEmotion.Neutral,
                ["angry"] = PortraitEmotion.Angry,
                ["authoritative"] = PortraitEmotion.Angry,
                ["bitter"] = PortraitEmotion.Angry,
                ["breathless"] = PortraitEmotion.Concerned,
                ["broken"] = PortraitEmotion.Concerned,
                ["businesslike"] = PortraitEmotion.Neutral,
                ["neutral"] = PortraitEmotion.Neutral,
                ["calm"] = PortraitEmotion.Neutral,
                ["cautious"] = PortraitEmotion.Concerned,
                ["challenging"] = PortraitEmotion.Angry,
                ["choice"] = PortraitEmotion.Neutral,
                ["controlled"] = PortraitEmotion.Neutral,
                ["clinical"] = PortraitEmotion.Neutral,
                ["cold"] = PortraitEmotion.Neutral,
                ["commanding"] = PortraitEmotion.Angry,
                ["concerned"] = PortraitEmotion.Concerned,
                ["confident"] = PortraitEmotion.Positive,
                ["conflicted"] = PortraitEmotion.Concerned,
                ["cool"] = PortraitEmotion.Neutral,
                ["corrective"] = PortraitEmotion.Angry,
                ["curious"] = PortraitEmotion.Positive,
                ["decisive"] = PortraitEmotion.Positive,
                ["defeated"] = PortraitEmotion.Concerned,
                ["defensive"] = PortraitEmotion.Angry,
                ["defiant"] = PortraitEmotion.Angry,
                ["deflect"] = PortraitEmotion.Angry,
                ["desperate"] = PortraitEmotion.Concerned,
                ["direct"] = PortraitEmotion.Positive,
                ["disappointed"] = PortraitEmotion.Concerned,
                ["drunk_irritated"] = PortraitEmotion.Angry,
                ["dry"] = PortraitEmotion.Neutral,
                ["emotional"] = PortraitEmotion.Concerned,
                ["ending:A_complete"] = PortraitEmotion.Positive,
                ["ending:B_complete"] = PortraitEmotion.Neutral,
                ["ending:C_complete"] = PortraitEmotion.Concerned,
                ["ending:bad_complete"] = PortraitEmotion.Concerned,
                ["fading"] = PortraitEmotion.Concerned,
                ["fearful"] = PortraitEmotion.Concerned,
                ["firm"] = PortraitEmotion.Angry,
                ["observe"] = PortraitEmotion.Neutral,
                ["focused"] = PortraitEmotion.Angry,
                ["formal"] = PortraitEmotion.Neutral,
                ["frightened"] = PortraitEmotion.Concerned,
                ["furious"] = PortraitEmotion.Angry,
                ["gentle"] = PortraitEmotion.Positive,
                ["grave"] = PortraitEmotion.Angry,
                ["grim"] = PortraitEmotion.Angry,
                ["gruff"] = PortraitEmotion.Angry,
                ["guarded"] = PortraitEmotion.Angry,
                ["guilty"] = PortraitEmotion.Concerned,
                ["hard"] = PortraitEmotion.Angry,
                ["helpful"] = PortraitEmotion.Positive,
                ["hint1"] = PortraitEmotion.Neutral,
                ["hint2"] = PortraitEmotion.Neutral,
                ["hint3"] = PortraitEmotion.Neutral,
                ["horrified"] = PortraitEmotion.Concerned,
                ["hurt"] = PortraitEmotion.Concerned,
                ["insistent"] = PortraitEmotion.Angry,
                ["intense"] = PortraitEmotion.Angry,
                ["internal"] = PortraitEmotion.Neutral,
                ["irritated"] = PortraitEmotion.Angry,
                ["light"] = PortraitEmotion.Positive,
                ["low"] = PortraitEmotion.Concerned,
                ["lying"] = PortraitEmotion.Angry,
                ["matter_of_fact"] = PortraitEmotion.Neutral,
                ["measured"] = PortraitEmotion.Neutral,
                ["nervous"] = PortraitEmotion.Concerned,
                ["offended"] = PortraitEmotion.Angry,
                ["pained"] = PortraitEmotion.Concerned,
                ["panicked"] = PortraitEmotion.Concerned,
                ["pleading"] = PortraitEmotion.Concerned,
                ["polite"] = PortraitEmotion.Neutral,
                ["press"] = PortraitEmotion.Angry,
                ["professional"] = PortraitEmotion.Neutral,
                ["provoking"] = PortraitEmotion.Angry,
                ["realization"] = PortraitEmotion.Positive,
                ["recorded"] = PortraitEmotion.Neutral,
                ["system"] = PortraitEmotion.Neutral,
                ["relieved"] = PortraitEmotion.Positive,
                ["reluctant"] = PortraitEmotion.Concerned,
                ["resentful"] = PortraitEmotion.Angry,
                ["resolved"] = PortraitEmotion.Positive,
                ["sad"] = PortraitEmotion.Concerned,
                ["serious"] = PortraitEmotion.Angry,
                ["severe"] = PortraitEmotion.Angry,
                ["shaken"] = PortraitEmotion.Concerned,
                ["sharp"] = PortraitEmotion.Angry,
                ["skeptical"] = PortraitEmotion.Angry,
                ["smile"] = PortraitEmotion.Positive,
                ["startled"] = PortraitEmotion.Concerned,
                ["subdued"] = PortraitEmotion.Concerned,
                ["surprised"] = PortraitEmotion.Concerned,
                ["suspicious"] = PortraitEmotion.Angry,
                ["tutorial"] = PortraitEmotion.Neutral,
                ["uncertain"] = PortraitEmotion.Concerned,
                ["uneasy"] = PortraitEmotion.Concerned,
                ["urgent"] = PortraitEmotion.Concerned,
                ["warm"] = PortraitEmotion.Positive,
                ["warning"] = PortraitEmotion.Angry,
                ["weary"] = PortraitEmotion.Concerned,
                ["wry"] = PortraitEmotion.Positive,
                ["confused"] = PortraitEmotion.Concerned,
                ["ashamed"] = PortraitEmotion.Concerned,
                ["fear"] = PortraitEmotion.Concerned,
                ["fake_fear"] = PortraitEmotion.Concerned,
                ["tense"] = PortraitEmotion.Concerned,
                ["accuse"] = PortraitEmotion.Angry,
                ["anger"] = PortraitEmotion.Angry,
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

        public static DialogueSpeakerIdentity GetSpeaker(
            string speaker,
            string lineType)
        {
            DialogueSpeakerIdentity identity = GetSpeaker(speaker);
            if (string.Equals(
                    lineType,
                    "monologue",
                    StringComparison.OrdinalIgnoreCase))
            {
                return new DialogueSpeakerIdentity(
                    identity.PortraitId,
                    DialogueSpeakerKind.Monologue);
            }

            return identity;
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
    public sealed class ProductionDialogueFlow
    {
        public const int ChoiceCapacity = 8;
        private readonly Dictionary<string, List<DialogueRecord>> scenes;
        private readonly HashSet<string> completedScenes;
        private readonly GameStateManager state;
        private readonly Func<string, bool> tryGrantEvidence;
        private readonly HashSet<string> resolvedChoiceIds =
            new(StringComparer.OrdinalIgnoreCase);
        private List<DialogueRecord> activeScene = new();
        private int index;
        private int presentedChoiceBlockEnd = -1;
        private int repeatableChoiceStart = -1;
        private int repeatableChoiceEnd = -1;
        private string repeatableChoiceGroup = string.Empty;
        private string activeRepeatableCondition = string.Empty;
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
            resolvedChoiceIds.Clear();
            presentedChoiceBlockEnd = -1;
            ClearRepeatableChoiceContext();
            if (!scenes.TryGetValue(sceneId, out activeScene) ||
                IsSceneCompleted(sceneId) ||
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
                   !IsSceneCompleted(sceneId) &&
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
            resolvedChoiceIds.Add(selectedChoice.ChoiceId);
            state?.AddFlag(
                ProductionConditionEvaluator.ChoiceFlag(selectedChoice.ChoiceId));
            ApplyEffect(selectedChoice);
            InvestigationEventHub.Publish(
                InvestigationEventKind.ChoiceResolved,
                selectedChoice.StableLineId,
                ActiveSceneId);
            if (repeatableChoiceStart >= 0)
            {
                activeRepeatableCondition = ChoiceCondition(selectedChoice.ChoiceId);
                index = presentedChoiceBlockEnd;
                while (index < repeatableChoiceEnd &&
                       !string.Equals(
                           activeScene[index].Condition,
                           activeRepeatableCondition,
                           StringComparison.OrdinalIgnoreCase))
                {
                    index++;
                }
            }
            else
            {
                index = presentedChoiceBlockEnd;
            }
            Choices = Array.Empty<DialogueRecord>();
            PresentCurrent();
            return true;
        }

        private void PresentCurrent()
        {
            RestoreRepeatableChoiceContext();
            ReturnToRepeatableChoicesAfterBranch();

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
                List<DialogueRecord> choiceBlock = activeScene
                    .Skip(index)
                    .TakeWhile(record => record.Speaker == "PLAYER_CHOICE")
                    .ToList();
                presentedChoiceBlockEnd = index + choiceBlock.Count;
                ConfigureRepeatableChoiceContext(choiceBlock);
                List<DialogueRecord> available = repeatableChoiceStart >= 0
                    ? choiceBlock.Where(record => !IsChoiceResolved(record)).ToList()
                    : choiceBlock;
                if (available.Count == 0 && repeatableChoiceStart >= 0)
                {
                    index = repeatableChoiceEnd;
                    ClearRepeatableChoiceContext();
                    PresentCurrent();
                    return;
                }
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

        private void ConfigureRepeatableChoiceContext(
            IReadOnlyList<DialogueRecord> choiceBlock)
        {
            if (choiceBlock.Count == 0 ||
                !IsRepeatableChoiceGroup(choiceBlock[0].BranchGroup) ||
                choiceBlock.Any(record => !string.Equals(
                    record.BranchGroup,
                    choiceBlock[0].BranchGroup,
                    StringComparison.OrdinalIgnoreCase)))
            {
                ClearRepeatableChoiceContext();
                return;
            }

            repeatableChoiceStart = index;
            repeatableChoiceGroup = choiceBlock[0].BranchGroup;
            repeatableChoiceEnd = presentedChoiceBlockEnd;
            while (repeatableChoiceEnd < activeScene.Count &&
                   string.Equals(
                       activeScene[repeatableChoiceEnd].BranchGroup,
                       repeatableChoiceGroup,
                       StringComparison.OrdinalIgnoreCase))
            {
                repeatableChoiceEnd++;
            }
        }

        private void RestoreRepeatableChoiceContext()
        {
            if (repeatableChoiceStart >= 0 ||
                index < 0 ||
                index >= activeScene.Count ||
                !IsRepeatableChoiceGroup(activeScene[index].BranchGroup))
            {
                return;
            }

            string group = activeScene[index].BranchGroup;
            int choiceStart = index;
            while (choiceStart > 0 &&
                   string.Equals(
                       activeScene[choiceStart - 1].BranchGroup,
                       group,
                       StringComparison.OrdinalIgnoreCase))
            {
                choiceStart--;
            }
            while (choiceStart < activeScene.Count &&
                   activeScene[choiceStart].Speaker != "PLAYER_CHOICE")
            {
                choiceStart++;
            }
            if (choiceStart >= activeScene.Count)
            {
                return;
            }

            int choiceEnd = choiceStart;
            while (choiceEnd < activeScene.Count &&
                   activeScene[choiceEnd].Speaker == "PLAYER_CHOICE" &&
                   string.Equals(
                       activeScene[choiceEnd].BranchGroup,
                       group,
                       StringComparison.OrdinalIgnoreCase))
            {
                choiceEnd++;
            }

            repeatableChoiceStart = choiceStart;
            repeatableChoiceGroup = group;
            repeatableChoiceEnd = choiceEnd;
            while (repeatableChoiceEnd < activeScene.Count &&
                   string.Equals(
                       activeScene[repeatableChoiceEnd].BranchGroup,
                       group,
                       StringComparison.OrdinalIgnoreCase))
            {
                repeatableChoiceEnd++;
            }
            presentedChoiceBlockEnd = choiceEnd;
            activeRepeatableCondition =
                index >= choiceEnd ? activeScene[index].Condition : string.Empty;
        }

        private void ReturnToRepeatableChoicesAfterBranch()
        {
            if (repeatableChoiceStart < 0 ||
                string.IsNullOrEmpty(activeRepeatableCondition) ||
                (index < repeatableChoiceEnd &&
                 string.Equals(
                     activeScene[index].Condition,
                     activeRepeatableCondition,
                     StringComparison.OrdinalIgnoreCase)))
            {
                return;
            }

            activeRepeatableCondition = string.Empty;
            bool hasRemainingChoice = activeScene
                .Skip(repeatableChoiceStart)
                .Take(presentedChoiceBlockEnd - repeatableChoiceStart)
                .Any(record => !IsChoiceResolved(record));
            if (hasRemainingChoice)
            {
                index = repeatableChoiceStart;
                return;
            }

            index = repeatableChoiceEnd;
            ClearRepeatableChoiceContext();
        }

        private bool IsChoiceResolved(DialogueRecord record)
        {
            return resolvedChoiceIds.Contains(record.ChoiceId) ||
                   (state != null &&
                    state.HasFlag(
                        ProductionConditionEvaluator.ChoiceFlag(record.ChoiceId)));
        }

        private static bool IsRepeatableChoiceGroup(string branchGroup)
        {
            return !string.IsNullOrWhiteSpace(branchGroup) &&
                   branchGroup.Trim().EndsWith(
                       "_FREE",
                       StringComparison.OrdinalIgnoreCase);
        }

        private static string ChoiceCondition(string choiceId) =>
            $"choice({choiceId})";

        private void ClearRepeatableChoiceContext()
        {
            repeatableChoiceStart = -1;
            repeatableChoiceEnd = -1;
            repeatableChoiceGroup = string.Empty;
            activeRepeatableCondition = string.Empty;
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

                if (condition ==
                    ProductionSceneCatalog.FinalAccusationPrerequisite)
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

            if (state == null)
            {
                return false;
            }
            if (condition ==
                ProductionSceneCatalog.FinalAccusationPrerequisite)
            {
                return FinalAccusationResolver.OpensD8Confession(
                    state.FinalEndingId);
            }
            if (condition == ProductionSceneCatalog.EpiloguePrerequisite)
            {
                string route = FinalAccusationResolver.ToOfficialRoute(
                    state.FinalEndingId);
                return IsSceneCompleted("D8-02") ||
                       route == "C" ||
                       route == "Bad";
            }
            return false;
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
                   (string.Equals(
                        value.Trim(),
                        ProductionSceneCatalog.FinalAccusationPrerequisite,
                        StringComparison.Ordinal) ||
                    string.Equals(
                        value.Trim(),
                        ProductionSceneCatalog.EpiloguePrerequisite,
                        StringComparison.Ordinal) ||
                    System.Text.RegularExpressions.Regex.IsMatch(
                        value.Trim(),
                        @"^(P|D\d+)-\d+$"));
        }

        private static string NormalizeSceneId(string value)
        {
            return string.IsNullOrWhiteSpace(value)
                ? string.Empty
                : value.Trim().ToUpperInvariant();
        }

        private void ApplyEffect(DialogueRecord record)
        {
            string[] evidenceIds =
                CanonicalEvidenceCatalog.GetGrantedEvidenceIds(
                        record.StableLineId)
                    .ToArray();
            bool hasTextEffect =
                !string.IsNullOrWhiteSpace(record.NextOrEffect);
            if (evidenceIds.Length == 0 && !hasTextEffect)
            {
                return;
            }

            if (state != null &&
                state.HasAppliedDialogueEffect(record.StableLineId))
            {
                return;
            }

            ProductionEffectParseResult parsed = hasTextEffect
                ? ProductionEffectParser.Parse(record.NextOrEffect)
                : null;
            if (hasTextEffect &&
                (parsed == null ||
                 !parsed.Success ||
                 parsed.Instructions.Count == 0))
            {
                warnings.Add(
                    $"{record.StableLineId} (CSV row {record.SourceRow}): " +
                    $"invalid official effect '{record.NextOrEffect}' was not executed.");
                return;
            }

            using (state?.BeginStateBatch())
            {
                foreach (string evidenceId in evidenceIds)
                    tryGrantEvidence?.Invoke(evidenceId);

                if (hasTextEffect)
                {
                    var executor =
                        new ProductionEffectExecutor(state, tryGrantEvidence);
                    ProductionEffectExecutionResult result =
                        executor.Execute(record.NextOrEffect);
                    foreach (string warning in result.Warnings)
                    {
                        warnings.Add(
                            $"{record.StableLineId} (CSV row {record.SourceRow}): {warning}");
                    }
                }

                state?.RecordAppliedDialogueEffect(record.StableLineId);
            }
        }
    }
}
