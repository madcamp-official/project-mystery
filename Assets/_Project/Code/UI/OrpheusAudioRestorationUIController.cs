using System;
using System.Collections.Generic;
using System.Linq;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using Wake.Core;
using Wake.Narrative;
using Wake.Puzzles;

namespace Wake.UI
{
    public sealed class ResourcesOrpheusAudioProvider : IOrpheusAudioProvider
    {
        private const string ResourceFolder = "VoiceBarks/story_recording";

        public bool TryGetClip(string stableLineId, out AudioClip clip)
        {
            clip = null;
            if (!OrpheusRecordCatalog.TryGet(
                    stableLineId, out OrpheusRecordSegment segment))
            {
                return false;
            }

            AudioClip[] allClips = Resources.LoadAll<AudioClip>(ResourceFolder);
            int index = SelectClipIndex(
                segment,
                OrpheusRecordCatalog.All,
                allClips.Select(item => item.name).ToArray());
            if (index < 0)
            {
                return false;
            }

            clip = allClips[index];
            return true;
        }

        public static int SelectClipIndex(
            OrpheusRecordSegment segment,
            IReadOnlyList<OrpheusRecordSegment> allSegments,
            IReadOnlyList<string> candidateClipNames)
        {
            string speakerPrefix = segment.Speaker
                .Replace("_RECORD", string.Empty)
                .Replace("_MESSAGE", string.Empty)
                .ToUpperInvariant();
            int[] matchingIndexes = candidateClipNames
                .Select((name, index) => (name, index))
                .Where(item => item.name.ToUpperInvariant().Contains(speakerPrefix))
                .OrderBy(item => item.name, StringComparer.Ordinal)
                .Select(item => item.index)
                .ToArray();

            int sameSpeakerPosition = allSegments
                .Where(item => item.Speaker == segment.Speaker)
                .ToList()
                .IndexOf(segment);

            return sameSpeakerPosition >= 0 &&
                   sameSpeakerPosition < matchingIndexes.Length
                ? matchingIndexes[sameSpeakerPosition]
                : -1;
        }
    }

    public readonly struct OrpheusSegmentView
    {
        public OrpheusSegmentView(
            string lineId,
            string speaker,
            int position,
            bool selected)
        {
            LineId = lineId ?? string.Empty;
            Speaker = speaker ?? string.Empty;
            Position = position;
            Selected = selected;
        }

        public string LineId { get; }
        public string Speaker { get; }
        public int Position { get; }
        public bool Selected { get; }
        public bool IsPlaced => Position >= 0;
    }

    public static class OrpheusAudioPresentation
    {
        public static IReadOnlyList<OrpheusSegmentView> CreateSegments(
            IReadOnlyList<string> orderedLineIds,
            string selectedLineId)
        {
            return OrpheusRecordCatalog.All
                .Select(segment => new OrpheusSegmentView(
                    segment.LineId,
                    SpeakerLabel(segment.Speaker),
                    orderedLineIds?.ToList().IndexOf(segment.LineId) ?? -1,
                    segment.LineId == selectedLineId))
                .ToArray();
        }

        public static string SpeakerLabel(string speaker) => speaker switch
        {
            "JULIAN_RECORD" => "Julian 기록 음성",
            "EVELYN_RECORD" => "이블린 기록 음성",
            "RICHARD" => "Richard",
            _ => string.IsNullOrWhiteSpace(speaker) ? "알 수 없는 화자" : speaker
        };

        public static string PlaybackText(OrpheusPlaybackRequest request)
        {
            if (!request.Found)
            {
                return request.Warning;
            }

            string mode = request.UsesTranscriptFallback
                ? "[음성 없음 · 한국어 자막 재생]"
                : "[음성 및 한국어 자막 재생]";
            return string.IsNullOrEmpty(request.Warning)
                ? $"{mode}\n{request.Transcript}"
                : $"{mode}\n{request.Transcript}\n{request.Warning}";
        }
    }

    [DisallowMultipleComponent]
    public sealed class OrpheusAudioRestorationUIController :
        MonoBehaviour,
        IRuntimeModalController
    {
        private static readonly Color Panel = new(0.035f, 0.075f, 0.12f, 0.98f);
        private static readonly Color Available = new(0.16f, 0.20f, 0.26f, 1f);
        private static readonly Color Selected = new(0.24f, 0.48f, 0.56f, 1f);
        private static readonly Color Placed = new(0.12f, 0.30f, 0.28f, 1f);
        private static readonly Color Empty = new(0.10f, 0.12f, 0.16f, 1f);

        private readonly List<Button> segmentButtons = new();
        private readonly List<Button> positionButtons = new();
        private readonly IOrpheusAudioProvider audioProvider =
            new ResourcesOrpheusAudioProvider();
        private GameObject root;
        private TMP_Text hintText;
        private TMP_Text playbackText;
        private TMP_Text statusText;
        private AudioSource audioSource;
        private OrpheusAudioRestorationSession session;
        private string selectedLineId;

        public bool IsOpen => root != null && root.activeSelf;
        public OrpheusAudioRestorationSession Session => session;
        public string SelectedLineId => selectedLineId;

        private void Awake()
        {
            audioSource = GetComponent<AudioSource>();
            if (audioSource == null)
            {
                audioSource = gameObject.AddComponent<AudioSource>();
            }
            audioSource.volume = AudioManager.Instance?.SfxVolume ?? 1f;
            BuildUi();
        }

        public bool Open()
        {
            GameStateManager state = GameStateManager.Instance;
            if (root == null ||
                !ProductionSceneCompletionGate.CanStartInteraction(
                    state,
                    OrpheusRecordCatalog.SceneId,
                    OrpheusRecordCatalog.PuzzleId))
            {
                return false;
            }

            session = new OrpheusAudioRestorationSession(
                state,
                audioProvider);
            selectedLineId = null;
            playbackText.text =
                "[대기 중]\n기록 조각을 선택하면 음성 또는 한국어 자막을 재생합니다.";
            statusText.text = "기록 조각을 선택하고 복원 위치를 지정하세요.";
            RuntimeModalTransition.Open(root);
            Refresh();
            return true;
        }

        public void Close()
        {
            audioSource?.Stop();
            RuntimeModalTransition.Close(root);
        }

        public bool SelectSegment(string lineId)
        {
            if (session == null ||
                session.IsCompleted ||
                !OrpheusRecordCatalog.TryGet(lineId, out _))
            {
                return false;
            }

            selectedLineId = OrpheusRecordSegment.Normalize(lineId);
            statusText.text = "복원할 순서 위치를 선택하세요.";
            Play(selectedLineId);
            Refresh();
            return true;
        }

        public bool MoveSelected(int position)
        {
            if (session == null || string.IsNullOrEmpty(selectedLineId))
            {
                statusText.text = "먼저 기록 조각을 선택하세요.";
                return false;
            }

            bool moved = session.Move(selectedLineId, position);
            statusText.text = moved
                ? $"{position + 1}번째 위치로 이동했습니다."
                : "기록 조각을 이동하지 못했습니다.";
            if (moved)
            {
                selectedLineId = null;
            }
            Refresh();
            return moved;
        }

        public OrpheusPlaybackRequest Play(string lineId)
        {
            if (session == null)
            {
                return new OrpheusPlaybackRequest(
                    false,
                    null,
                    string.Empty,
                    "복원 세션을 시작하지 못했습니다.");
            }

            OrpheusPlaybackRequest request = session.RequestPlayback(lineId);
            playbackText.text = OrpheusAudioPresentation.PlaybackText(request);
            if (request.Clip != null)
            {
                audioSource.Stop();
                audioSource.clip = request.Clip;
                audioSource.volume = AudioManager.Instance?.SfxVolume ?? 1f;
                audioSource.Play();
            }
            return request;
        }

        public bool UseHint()
        {
            bool changed = session != null && session.UseHint();
            if (changed)
            {
                statusText.text = "복원 힌트를 갱신했습니다.";
                Refresh();
            }
            return changed;
        }

        public OrpheusCompletionResult Submit()
        {
            if (session == null)
            {
                return new OrpheusCompletionResult(
                    false,
                    new[] { "복원 세션을 시작하지 못했습니다." });
            }

            OrpheusCompletionResult result = session.TryComplete();
            statusText.text = result.Completed
                ? "Orpheus 기록 복원을 완료했습니다."
                : string.Join("\n", result.Diagnostics.Take(2));
            ToastController.Instance?.Show(statusText.text);
            if (result.Completed)
            {
                Close();
            }
            else
            {
                Refresh();
            }
            return result;
        }

        private void Refresh()
        {
            if (session == null)
            {
                return;
            }

            hintText.text = session.GetHint();
            IReadOnlyList<OrpheusSegmentView> views =
                OrpheusAudioPresentation.CreateSegments(
                    session.OrderedLineIds,
                    selectedLineId);
            for (int index = 0; index < segmentButtons.Count; index++)
            {
                OrpheusSegmentView view = views[index];
                Button button = segmentButtons[index];
                button.interactable = !session.IsCompleted;
                button.image.color =
                    view.IsPlaced ? Placed : view.Selected ? Selected : Available;
                string state = view.IsPlaced
                    ? $"복원 위치: {view.Position + 1}"
                    : view.Selected ? "선택됨" : "선택 가능";
                button.GetComponentInChildren<TMP_Text>().text =
                    $"[{state}] {view.LineId} · {view.Speaker}";
            }

            for (int index = 0; index < positionButtons.Count; index++)
            {
                Button button = positionButtons[index];
                string lineId = index < session.OrderedLineIds.Count
                    ? session.OrderedLineIds[index]
                    : string.Empty;
                button.interactable =
                    !session.IsCompleted && !string.IsNullOrEmpty(selectedLineId);
                button.image.color = string.IsNullOrEmpty(lineId) ? Empty : Placed;
                string label = OrpheusRecordCatalog.TryGet(lineId, out var segment)
                    ? OrpheusAudioPresentation.SpeakerLabel(segment.Speaker)
                    : "비어 있음";
                button.GetComponentInChildren<TMP_Text>().text =
                    $"{index + 1}. [{(string.IsNullOrEmpty(lineId) ? "빈 위치" : "복원됨")}] {label}";
            }
        }

        private void BuildUi()
        {
            Transform canvas = GameObject.Find("Canvas")?.transform;
            if (canvas == null)
            {
                return;
            }

            root = MakeObject("Orpheus Audio Restoration", canvas, typeof(Image));
            RectTransform rootRect = root.GetComponent<RectTransform>();
            ScreenShellRuntimePresenter.Place(
                rootRect,
                ScreenShellSlotIds.PuzzlePanel,
                new Vector2(.04f, .08f),
                new Vector2(.96f, .92f));
            root.GetComponent<Image>().color = Panel;
            root.AddComponent<ScreenShellRuntimePresenter>()
                .Configure(ScreenShellType.Puzzle);

            MakeText(
                root.transform,
                "Orpheus 기록 음성 복원",
                0.88f,
                0.98f,
                34f,
                0.06f,
                0.94f);
            MakeText(
                root.transform,
                "네 개의 기록 조각을 재생하고 원래 대화 순서로 복원하세요.",
                0.80f,
                0.88f,
                20f,
                0.06f,
                0.94f);

            for (int index = 0; index < OrpheusRecordCatalog.All.Count; index++)
            {
                int captured = index;
                float maxY = 0.75f - index * 0.09f;
                Button segment = MakeButton(
                    root.transform,
                    $"Segment {index + 1}",
                    maxY - 0.072f,
                    maxY,
                    string.Empty,
                    0.06f,
                    0.49f,
                    17f);
                segment.onClick.AddListener(() =>
                    SelectSegment(OrpheusRecordCatalog.All[captured].LineId));
                segmentButtons.Add(segment);
            }

            for (int index = 0; index < OrpheusRecordCatalog.All.Count; index++)
            {
                int captured = index;
                float maxY = 0.75f - index * 0.09f;
                Button position = MakeButton(
                    root.transform,
                    $"Position {index + 1}",
                    maxY - 0.072f,
                    maxY,
                    string.Empty,
                    0.53f,
                    0.94f,
                    17f);
                position.onClick.AddListener(() => MoveSelected(captured));
                positionButtons.Add(position);
            }

            playbackText = MakeText(
                root.transform,
                string.Empty,
                0.20f,
                0.38f,
                19f,
                0.06f,
                0.94f);
            hintText = MakeText(
                root.transform,
                string.Empty,
                0.13f,
                0.20f,
                17f,
                0.06f,
                0.94f);
            statusText = MakeText(
                root.transform,
                string.Empty,
                0.06f,
                0.13f,
                17f,
                0.06f,
                0.56f);
            Button hint = MakeButton(
                root.transform, "Hint", 0.015f, 0.07f, "힌트", 0.59f, 0.70f, 18f);
            hint.onClick.AddListener(() => UseHint());
            Button submit = MakeButton(
                root.transform, "Submit", 0.015f, 0.07f, "복원 확인", 0.72f, 0.84f, 18f);
            submit.onClick.AddListener(() => Submit());
            Button close = MakeButton(
                root.transform, "Close", 0.015f, 0.07f, "닫기", 0.86f, 0.94f, 18f);
            close.onClick.AddListener(Close);
            InteractionTypography.Apply(
                root.transform,
                playbackText,
                hintText,
                statusText);
            root.SetActive(false);
        }

        private static GameObject MakeObject(
            string name,
            Transform parent,
            params Type[] components)
        {
            Type[] all = new Type[components.Length + 2];
            all[0] = typeof(RectTransform);
            all[1] = typeof(CanvasRenderer);
            Array.Copy(components, 0, all, 2, components.Length);
            var target = new GameObject(name, all);
            target.transform.SetParent(parent, false);
            return target;
        }

        private static TMP_Text MakeText(
            Transform parent,
            string value,
            float minY,
            float maxY,
            float size,
            float minX,
            float maxX)
        {
            GameObject target = MakeObject("Label", parent, typeof(TextMeshProUGUI));
            RectTransform rect = target.GetComponent<RectTransform>();
            rect.anchorMin = new Vector2(minX, minY);
            rect.anchorMax = new Vector2(maxX, maxY);
            rect.offsetMin = Vector2.zero;
            rect.offsetMax = Vector2.zero;
            TMP_Text text = target.GetComponent<TMP_Text>();
            TypographyService.Apply(text, TypographyRole.Body);
            text.fontSize = size;
            text.alignment = TextAlignmentOptions.Center;
            text.color = Color.white;
            text.text = value;
            return text;
        }

        private static Button MakeButton(
            Transform parent,
            string name,
            float minY,
            float maxY,
            string label,
            float minX,
            float maxX,
            float size)
        {
            GameObject target = MakeObject(name, parent, typeof(Image), typeof(Button));
            RectTransform rect = target.GetComponent<RectTransform>();
            rect.anchorMin = new Vector2(minX, minY);
            rect.anchorMax = new Vector2(maxX, maxY);
            rect.offsetMin = Vector2.zero;
            rect.offsetMax = Vector2.zero;
            target.GetComponent<Image>().color = Available;
            TMP_Text text = MakeText(target.transform, label, 0f, 1f, size, 0f, 1f);
            text.raycastTarget = false;
            Button button = target.GetComponent<Button>();
            ScreenShellRuntimePresenter.PrepareButton(button);
            return button;
        }
    }

}
