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
    }
    public sealed class DialogueTypedEffect
    {
        public string TargetCharacterId { get; set; } = string.Empty;
        public int TrustDelta { get; set; }
        public int AnxietyDelta { get; set; }
        public int IntegrityDelta { get; set; }
        public IReadOnlyList<string> AddFlags { get; set; } = Array.Empty<string>();
        public IReadOnlyList<string> RemoveFlags { get; set; } = Array.Empty<string>();
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
        }
    }
    public static class DialogueEffectCatalog
    {
        private static readonly IReadOnlyDictionary<string, DialogueTypedEffect> Confirmed =
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
        public static bool TryResolve(string source, out DialogueTypedEffect effect)
        {
            return Confirmed.TryGetValue(source?.Trim() ?? string.Empty, out effect);
        }
    }
    public sealed class ProductionDialogueFlow
    {
        public const int ChoiceCapacity = 4;
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
        public string ActiveSceneId { get; private set; } = string.Empty;
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
            Current = null;
            if (!scenes.TryGetValue(sceneId, out activeScene) ||
                !PrerequisitesAreMet(activeScene))
            {
                return false;
            }
            ActiveSceneId = sceneId;
            index = 0;
            PresentCurrent();
            return true;
        }

        public bool IsSceneCompleted(string sceneId)
        {
            return completedScenes.Contains(NormalizeSceneId(sceneId));
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
            ApplyEffect(Choices[choiceIndex]);
            index += Choices.Count;
            Choices = Array.Empty<DialogueRecord>();
            PresentCurrent();
            return true;
        }

        private void PresentCurrent()
        {
            if (index >= activeScene.Count)
            {
                completedScenes.Add(ActiveSceneId);
                state?.RecordCompletedScene(ActiveSceneId);
                Current = null;
                IsComplete = true;
                return;
            }
            if (activeScene[index].Speaker == "PLAYER_CHOICE")
            {
                Choices = activeScene
                    .Skip(index)
                    .TakeWhile(record => record.Speaker == "PLAYER_CHOICE")
                    .Take(ChoiceCapacity)
                    .ToList();
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
                return completedScenes.Contains(condition);
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
            return !string.IsNullOrWhiteSpace(value) && value != "\uC5C6\uC74C";
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
            if (DialogueEffectCatalog.TryResolve(record.NextOrEffect, out DialogueTypedEffect effect))
            {
                effect.Apply(state);
            }
            else
            {
                warnings.Add(
                    $"{record.StableLineId}: unconfirmed effect '{record.NextOrEffect}' was not executed.");
            }
        }
    }
}
