using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using Wake.Core;
using Wake.Narrative;

namespace Wake.Puzzles
{
    public sealed class OrpheusRecordSegment
    {
        public OrpheusRecordSegment(
            string lineId,
            string speaker,
            string transcript,
            bool voiceRequired)
        {
            LineId = Normalize(lineId);
            Speaker = speaker?.Trim() ?? string.Empty;
            Transcript = transcript?.Trim() ?? string.Empty;
            VoiceRequired = voiceRequired;
        }

        public string LineId { get; }
        public string Speaker { get; }
        public string Transcript { get; }
        public bool VoiceRequired { get; }

        public static string Normalize(string value) =>
            string.IsNullOrWhiteSpace(value)
                ? string.Empty
                : value.Trim().ToLowerInvariant();
    }

    public static class OrpheusRecordCatalog
    {
        public const string PuzzleId = "orpheus_audio_restoration";
        public const string SceneId = "D7-03";
        public const string EvidenceId = "C-17";

        private static readonly OrpheusRecordSegment[] Segments =
        {
            new(
                "d7_03_01",
                "JULIAN_RECORD",
                "아버지는 아무것도 몰라요. 지금 멈춰요.",
                true),
            new(
                "d7_03_02",
                "EVELYN_RECORD",
                "당신 아버지가 몰랐다고 해서 회사가 살아나는 건 아니에요.",
                true),
            new(
                "d7_03_03",
                "JULIAN_RECORD",
                "승객이 탔습니다. 이대로 밸브를 열면 사고가 아니라 살인이에요.",
                true),
            new(
                "d7_03_04",
                "RICHARD",
                "Julian… 내가 너를 범인으로 만들었구나.",
                true)
        };

        private static readonly IReadOnlyDictionary<string, OrpheusRecordSegment>
            ById = Segments.ToDictionary(item => item.LineId, StringComparer.Ordinal);

        public static IReadOnlyList<OrpheusRecordSegment> All => Segments;

        public static bool TryGet(string lineId, out OrpheusRecordSegment segment) =>
            ById.TryGetValue(OrpheusRecordSegment.Normalize(lineId), out segment);
    }

    public interface IOrpheusAudioProvider
    {
        bool TryGetClip(string stableLineId, out AudioClip clip);
    }

    public readonly struct OrpheusPlaybackRequest
    {
        public OrpheusPlaybackRequest(
            bool found,
            AudioClip clip,
            string transcript,
            string warning)
        {
            Found = found;
            Clip = clip;
            Transcript = transcript ?? string.Empty;
            Warning = warning ?? string.Empty;
        }

        public bool Found { get; }
        public AudioClip Clip { get; }
        public string Transcript { get; }
        public string Warning { get; }
        public bool UsesTranscriptFallback => Found && Clip == null;
    }

    public readonly struct OrpheusCompletionResult
    {
        public OrpheusCompletionResult(
            bool completed,
            IReadOnlyList<string> diagnostics)
        {
            Completed = completed;
            Diagnostics = diagnostics ?? Array.Empty<string>();
        }

        public bool Completed { get; }
        public IReadOnlyList<string> Diagnostics { get; }
    }

    public static class OrpheusRecordValidator
    {
        public static IReadOnlyList<string> Validate(
            IEnumerable<OrpheusRecordSegment> segments,
            IOrpheusAudioProvider audioProvider = null)
        {
            OrpheusRecordSegment[] items =
                (segments ?? Array.Empty<OrpheusRecordSegment>()).ToArray();
            var diagnostics = new List<string>();
            if (items.Length != 4)
            {
                diagnostics.Add($"D7-03 기록은 4개여야 합니다: {items.Length}개");
            }

            foreach (OrpheusRecordSegment item in items)
            {
                if (item == null || string.IsNullOrEmpty(item.LineId))
                {
                    diagnostics.Add("stable line ID가 비어 있는 기록이 있습니다.");
                    continue;
                }

                if (string.IsNullOrEmpty(item.Speaker))
                {
                    diagnostics.Add($"화자가 비어 있습니다: {item.LineId}");
                }

                if (string.IsNullOrEmpty(item.Transcript))
                {
                    diagnostics.Add($"자막이 비어 있습니다: {item.LineId}");
                }

                if (item.VoiceRequired &&
                    (audioProvider == null ||
                     !audioProvider.TryGetClip(item.LineId, out AudioClip clip) ||
                     clip == null))
                {
                    diagnostics.Add(
                        $"AudioClip 없음, 자막으로 대체합니다: {item.LineId}");
                }
            }

            foreach (string duplicate in items
                         .Where(item => item != null)
                         .GroupBy(item => item.LineId, StringComparer.Ordinal)
                         .Where(group => group.Count() > 1)
                         .Select(group => group.Key))
            {
                diagnostics.Add($"중복 stable line ID: {duplicate}");
            }

            return diagnostics;
        }
    }

    public sealed class OrpheusAudioRestorationSession
    {
        private readonly GameStateManager state;
        private readonly IOrpheusAudioProvider audioProvider;
        private readonly List<string> orderedLineIds = new();

        public OrpheusAudioRestorationSession(
            GameStateManager state,
            IOrpheusAudioProvider audioProvider = null)
        {
            this.state = state ?? throw new ArgumentNullException(nameof(state));
            this.audioProvider = audioProvider;
            if (state.TryGetPuzzleSession(
                    OrpheusRecordCatalog.PuzzleId,
                    out PuzzleSessionState saved))
            {
                orderedLineIds.AddRange((saved.selectedIds ?? new List<string>())
                    .Where(id => OrpheusRecordCatalog.TryGet(id, out _))
                    .Distinct());
                HintLevel = saved.hintLevel;
                IsCompleted = saved.completed;
            }
        }

        public IReadOnlyList<string> OrderedLineIds => orderedLineIds;
        public int HintLevel { get; private set; }
        public bool IsCompleted { get; private set; }

        public bool Move(string lineId, int index)
        {
            string normalized = OrpheusRecordSegment.Normalize(lineId);
            if (IsCompleted ||
                !OrpheusRecordCatalog.TryGet(normalized, out _) ||
                index < 0 ||
                index >= OrpheusRecordCatalog.All.Count)
            {
                return false;
            }

            orderedLineIds.Remove(normalized);
            orderedLineIds.Insert(Math.Min(index, orderedLineIds.Count), normalized);
            Save();
            return true;
        }

        public bool UseHint()
        {
            if (IsCompleted || HintLevel >= 3)
            {
                return false;
            }

            HintLevel++;
            Save();
            return true;
        }

        public string GetHint() => HintLevel switch
        {
            1 => "Julian의 첫 경고부터 복원을 시작하세요.",
            2 => "Evelyn의 답변 뒤에 밸브와 승객에 관한 경고가 이어집니다.",
            3 => "stable line ID의 01 → 02 → 03 → 04 순서를 확인하세요.",
            _ => "기록 조각을 재생하고 대화의 흐름을 복원하세요."
        };

        public OrpheusPlaybackRequest RequestPlayback(string lineId)
        {
            if (!OrpheusRecordCatalog.TryGet(lineId, out OrpheusRecordSegment segment))
            {
                return new OrpheusPlaybackRequest(
                    false, null, string.Empty, "알 수 없는 기록 조각입니다.");
            }

            AudioClip clip = null;
            audioProvider?.TryGetClip(segment.LineId, out clip);
            return new OrpheusPlaybackRequest(
                true,
                clip,
                segment.Transcript,
                clip == null
                    ? $"AudioClip 없음, 자막으로 대체합니다: {segment.LineId}"
                    : string.Empty);
        }

        public OrpheusCompletionResult TryComplete()
        {
            var diagnostics = new List<string>();
            string[] expected =
                OrpheusRecordCatalog.All.Select(item => item.LineId).ToArray();
            if (!orderedLineIds.SequenceEqual(expected))
            {
                diagnostics.Add("기록 조각의 순서가 원본 D7-03과 일치하지 않습니다.");
            }

            if (IsCompleted ||
                diagnostics.Count > 0 ||
                !ProductionSceneCompletionGate.CanStartInteraction(
                    state,
                    OrpheusRecordCatalog.SceneId,
                    OrpheusRecordCatalog.PuzzleId))
            {
                return new OrpheusCompletionResult(false, diagnostics);
            }

            using IDisposable batch = state.BeginStateBatch();
            IsCompleted = true;
            Save();
            state.RecordEvidenceCollected(OrpheusRecordCatalog.EvidenceId);
            state.AddFlag("past_culprit_confirmed", "과거 진범 확정");
            if (ProductionSceneCompletionGate.TryComplete(
                state,
                OrpheusRecordCatalog.SceneId,
                OrpheusRecordCatalog.PuzzleId,
                out bool newlyCompleted,
                out _) &&
                newlyCompleted)
            {
                InvestigationEventHub.Publish(
                    InvestigationEventKind.TheoryCompleted,
                    "past_culprit_evelyn",
                    OrpheusRecordCatalog.SceneId);
            }
            return new OrpheusCompletionResult(true, Array.Empty<string>());
        }

        private void Save()
        {
            state.SavePuzzleSession(new PuzzleSessionState
            {
                puzzleId = OrpheusRecordCatalog.PuzzleId,
                selectedIds = orderedLineIds.ToList(),
                step = orderedLineIds.Count,
                hintLevel = HintLevel,
                completed = IsCompleted
            });
        }
    }
}
