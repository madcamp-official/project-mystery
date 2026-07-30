using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace Wake.Narrative
{
    public interface IVoiceBarkClipProvider
    {
        IReadOnlyList<AudioClip> GetClips(string characterId, string cueId);
    }

    public sealed class ResourcesVoiceBarkClipProvider : IVoiceBarkClipProvider
    {
        private static readonly IReadOnlyDictionary<string, string> FolderByCharacter =
            new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
            {
                ["DANIEL"] = "daniel",
                ["CLAIRE"] = "claire",
                ["HELENA"] = "helena",
                ["ADRIAN"] = "adrian",
                ["RICHARD"] = "richard",
                ["THOMAS"] = "thomas",
                ["OWEN"] = "owen",
                ["EVELYN"] = "evelyn",
                ["MARCUS"] = "marcus"
            };

        public IReadOnlyList<AudioClip> GetClips(string characterId, string cueId)
        {
            if (string.IsNullOrEmpty(characterId) ||
                !FolderByCharacter.TryGetValue(characterId, out string folder))
            {
                return Array.Empty<AudioClip>();
            }

            string upperCue = cueId.ToUpperInvariant();
            return Resources.LoadAll<AudioClip>($"VoiceBarks/{folder}")
                .Where(clip => clip.name.ToUpperInvariant().Contains(upperCue))
                .ToArray();
        }
    }

    public sealed class VoiceBarkPlayer
    {
        private const float CooldownSeconds = 1.4f;
        private const int RepeatGuardCount = 3;
        private const string GreetCue = "GREET";

        private readonly IVoiceBarkClipProvider clipProvider;
        private readonly AudioSource audioSource;
        private readonly Func<int, int> randomIndexBelow;
        private readonly Dictionary<string, Queue<string>> recentClipsByCharacter = new();
        private readonly Dictionary<string, float> lastPlayTimeByCharacter = new();
        private readonly Dictionary<string, string> lastCueByCharacter = new();

        public VoiceBarkPlayer(
            IVoiceBarkClipProvider clipProvider,
            AudioSource audioSource,
            Func<int, int> randomIndexBelow = null)
        {
            this.clipProvider = clipProvider;
            this.audioSource = audioSource;
            this.randomIndexBelow =
                randomIndexBelow ?? (max => UnityEngine.Random.Range(0, max));
        }

        public bool TryPlayBark(
            string characterId,
            PortraitEmotion emotion,
            bool isNewSpeakerTurn,
            float currentTime)
        {
            if (lastPlayTimeByCharacter.TryGetValue(characterId, out float lastPlayed) &&
                currentTime - lastPlayed < CooldownSeconds)
            {
                return false;
            }

            string cue = isNewSpeakerTurn
                ? GreetCue
                : PickCue(VoiceBarkCatalog.CandidateCues(emotion));
            if (cue == null)
            {
                return false;
            }

            IReadOnlyList<AudioClip> candidates = clipProvider.GetClips(characterId, cue);
            if (candidates.Count == 0 &&
                lastCueByCharacter.TryGetValue(characterId, out string previousCue))
            {
                candidates = clipProvider.GetClips(characterId, previousCue);
            }

            if (candidates.Count == 0)
            {
                return false;
            }

            AudioClip chosen = PickClip(characterId, candidates);
            audioSource.PlayOneShot(chosen);
            RecordPlay(characterId, chosen.name, cue, currentTime);
            return true;
        }

        private string PickCue(IReadOnlyList<string> candidates) =>
            candidates.Count == 0 ? null : candidates[randomIndexBelow(candidates.Count)];

        private AudioClip PickClip(string characterId, IReadOnlyList<AudioClip> candidates)
        {
            Queue<string> recent = recentClipsByCharacter.TryGetValue(
                characterId, out Queue<string> existing)
                ? existing
                : new Queue<string>();
            AudioClip[] filtered = candidates
                .Where(clip => !recent.Contains(clip.name))
                .ToArray();
            AudioClip[] pool = filtered.Length > 0
                ? filtered
                : candidates.ToArray();
            return pool[randomIndexBelow(pool.Length)];
        }

        private void RecordPlay(
            string characterId, string clipName, string cue, float currentTime)
        {
            lastPlayTimeByCharacter[characterId] = currentTime;
            lastCueByCharacter[characterId] = cue;
            Queue<string> recent = recentClipsByCharacter.TryGetValue(
                characterId, out Queue<string> existing)
                ? existing
                : recentClipsByCharacter[characterId] = new Queue<string>();
            recent.Enqueue(clipName);
            while (recent.Count > RepeatGuardCount)
            {
                recent.Dequeue();
            }
        }
    }
}
