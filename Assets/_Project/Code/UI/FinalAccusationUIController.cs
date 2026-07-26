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
    public readonly struct FinalAccusationSubmission
    {
        public FinalAccusationSubmission(
            bool submitted,
            FinalAccusationResult result,
            IReadOnlyList<string> messages)
        {
            Submitted = submitted;
            Result = result;
            Messages = messages ?? Array.Empty<string>();
        }

        public bool Submitted { get; }
        public FinalAccusationResult Result { get; }
        public IReadOnlyList<string> Messages { get; }
    }

    public sealed class FinalAccusationSession
    {
        public const string SessionId =
            ProductionSceneCompletionCatalog.FinalAccusationInteraction;

        private static readonly string[] CrimeDeductions =
        {
            CanonicalDeductionCatalog.SceneDenial,
            CanonicalDeductionCatalog.BodyInsertion,
            CanonicalDeductionCatalog.TransportRoute,
            CanonicalDeductionCatalog.ActualMurder,
            CanonicalDeductionCatalog.CulpritLink
        };

        private readonly GameStateManager state;
        private readonly FinalAccusationResolver resolver;

        public FinalAccusationSession(GameStateManager state)
        {
            this.state = state ?? throw new ArgumentNullException(nameof(state));
            resolver = new FinalAccusationResolver(state);
            Accusation = new FinalAccusation();
            Restore();
        }

        public FinalAccusation Accusation { get; }
        public bool IsCompleted { get; private set; }
        public int CompletedStageCount { get; private set; }
        public int WrongStrikeCount => state.WrongStrikeCount;
        public FinalAccusationStage? CurrentStage =>
            CompletedStageCount < FinalAccusationStageCatalog.All.Count
                ? FinalAccusationStageCatalog.All[CompletedStageCount].Stage
                : null;

        public void Update(
            AccusedPerson accused,
            MurderLocation location,
            MurderMethod method,
            BodyTransport transport,
            DanielTargetBelief target,
            OrpheusEventDesign design,
            bool discloseCoverup)
        {
            if (IsCompleted)
            {
                return;
            }

            Accusation.Accused = accused;
            Accusation.Location = location;
            Accusation.Method = method;
            Accusation.Transport = transport;
            Accusation.DanielBelievedTarget = target;
            Accusation.OrpheusDesign = design;
            Accusation.DiscloseRichardCoverup = discloseCoverup;
            Save(false);
        }

        public IReadOnlyList<string> GetPreSubmitMessages()
        {
            var messages = new List<string>();
            if (CurrentStage.HasValue && GetCurrentAnswerValue() == 0)
            {
                FinalAccusationStageDefinition stage =
                    FinalAccusationStageCatalog.All[CompletedStageCount];
                messages.Add($"{stage.Prompt} 답을 선택하세요.");
            }

            string[] missing = CrimeDeductions
                .Where(id => !state.HasUnlockedDeduction(id))
                .ToArray();
            if (missing.Length > 0)
            {
                messages.Add($"핵심 논증이 부족합니다: {string.Join(", ", missing)}");
            }

            if (!CurrentStage.HasValue &&
                Accusation.DiscloseRichardCoverup &&
                !state.HasUnlockedDeduction(CanonicalDeductionCatalog.PastEvent))
            {
                messages.Add("Richard의 은폐를 공개하려면 과거 사건 논증이 필요합니다.");
            }

            if (state.PublicAnxiety >= GameStateManager.MaxPercent)
                messages.Add("승객 불안 100: 제출하면 공황 배드 엔딩으로 판정됩니다.");
            if (state.EvidenceIntegrity <= 0)
                messages.Add("현장 보존도 0: 직접 증거를 사용할 수 없습니다.");
            return messages;
        }

        public FinalAccusationSubmission Submit()
        {
            if (IsCompleted)
            {
                ProductionSceneCompletionGate.TryComplete(
                    state,
                    "D8-01",
                    SessionId);
                FinalAccusationResult restored = resolver.Resolve(Accusation);
                return new FinalAccusationSubmission(
                    false,
                    restored,
                    new[] { "이미 확정한 최종 논증은 다시 제출할 수 없습니다." });
            }

            IReadOnlyList<string> messages = GetPreSubmitMessages();
            if ((CurrentStage.HasValue && GetCurrentAnswerValue() == 0) ||
                messages.Any(message => message.StartsWith("핵심 논증")) ||
                messages.Any(message => message.StartsWith("Richard의 은폐")))
            {
                return new FinalAccusationSubmission(false, null, messages);
            }

            if (!ProductionSceneCompletionGate.CanStartInteraction(
                    state,
                    "D8-01",
                    SessionId))
            {
                return new FinalAccusationSubmission(
                    false,
                    null,
                    new[] { "이미 완료된 최종 지목은 다시 제출할 수 없습니다." });
            }

            using IDisposable batch = state.BeginStateBatch();
            if (CurrentStage.HasValue)
            {
                FinalAccusationStageDefinition stage =
                    FinalAccusationStageCatalog.All[CompletedStageCount];
                int answer = GetCurrentAnswerValue();
                FinalAccusationOptionDefinition option = stage.Options
                    .Single(item => item.EnumValue == answer);
                if (!option.IsCorrect)
                {
                    int strikeCount = state.ChangeRuntimeCounter(
                        "wrong_strike",
                        1);
                    if (strikeCount < 3)
                    {
                        ClearCurrentAnswer();
                        Save(false);
                        return new FinalAccusationSubmission(
                            false,
                            null,
                            messages.Concat(new[]
                            {
                                $"오답입니다. 같은 단계를 다시 선택하세요. " +
                                $"오류 {strikeCount}/3"
                            }).ToArray());
                    }

                    FinalAccusationResult failed = resolver.Resolve(Accusation);
                    IsCompleted = true;
                    Save(true);
                    ProductionSceneCompletionGate.TryComplete(
                        state,
                        "D8-01",
                        SessionId);
                    return new FinalAccusationSubmission(
                        true,
                        failed,
                        messages.Concat(new[]
                        {
                            "핵심 지목 오류가 3회 누적되었습니다.",
                            failed.Reason
                        }).ToArray());
                }

                CompletedStageCount++;
                Save(false);
                if (CurrentStage.HasValue)
                {
                    FinalAccusationStageDefinition next =
                        FinalAccusationStageCatalog.All[CompletedStageCount];
                    return new FinalAccusationSubmission(
                        false,
                        null,
                        messages.Concat(new[]
                        {
                            $"{CompletedStageCount}단계 정답입니다. " +
                            $"다음 질문: {next.Prompt}"
                        }).ToArray());
                }

                return new FinalAccusationSubmission(
                    false,
                    null,
                    messages.Concat(new[]
                    {
                        "여섯 단계가 모두 정답입니다. " +
                        "Richard의 은폐 공개 여부를 결정하세요."
                    }).ToArray());
            }

            FinalAccusationResult result = resolver.Resolve(Accusation);
            IsCompleted = true;
            Save(true);
            ProductionSceneCompletionGate.TryComplete(
                state,
                "D8-01",
                SessionId);
            return new FinalAccusationSubmission(
                true,
                result,
                messages.Concat(new[] { result.Reason }).ToArray());
        }

        private void Save(bool completed)
        {
            state.SavePuzzleSession(new PuzzleSessionState
            {
                puzzleId = SessionId,
                selectedIds = new List<string>
                {
                    $"accused={(int)Accusation.Accused}",
                    $"location={(int)Accusation.Location}",
                    $"method={(int)Accusation.Method}",
                    $"transport={(int)Accusation.Transport}",
                    $"target={(int)Accusation.DanielBelievedTarget}",
                    $"design={(int)Accusation.OrpheusDesign}",
                    $"coverup={(Accusation.DiscloseRichardCoverup ? 1 : 0)}",
                    "flow=2"
                },
                step = CompletedStageCount,
                completed = completed
            });
        }

        private void Restore()
        {
            if (!state.TryGetPuzzleSession(SessionId, out PuzzleSessionState saved))
                return;

            var values = (saved.selectedIds ?? new List<string>())
                .Select(value => value.Split('='))
                .Where(parts => parts.Length == 2 && int.TryParse(parts[1], out _))
                .ToDictionary(parts => parts[0], parts => int.Parse(parts[1]));
            Accusation.Accused = Read<AccusedPerson>(values, "accused");
            Accusation.Location = Read<MurderLocation>(values, "location");
            Accusation.Method = Read<MurderMethod>(values, "method");
            Accusation.Transport = Read<BodyTransport>(values, "transport");
            Accusation.DanielBelievedTarget =
                Read<DanielTargetBelief>(values, "target");
            Accusation.OrpheusDesign =
                Read<OrpheusEventDesign>(values, "design");
            Accusation.DiscloseRichardCoverup =
                values.TryGetValue("coverup", out int coverup) && coverup == 1;
            CompletedStageCount =
                values.TryGetValue("flow", out int flow) && flow == 2
                    ? Mathf.Clamp(
                        saved.step,
                        0,
                        FinalAccusationStageCatalog.All.Count)
                    : InferCompletedStageCount();
            IsCompleted = saved.completed;
        }

        private int InferCompletedStageCount()
        {
            int[] answers =
            {
                (int)Accusation.Accused,
                (int)Accusation.Location,
                (int)Accusation.Method,
                (int)Accusation.Transport,
                (int)Accusation.DanielBelievedTarget,
                (int)Accusation.OrpheusDesign
            };
            int firstMissing = Array.FindIndex(answers, answer => answer == 0);
            return firstMissing >= 0 ? firstMissing : answers.Length;
        }

        private int GetCurrentAnswerValue() =>
            CurrentStage switch
            {
                FinalAccusationStage.Culprit => (int)Accusation.Accused,
                FinalAccusationStage.MurderLocation => (int)Accusation.Location,
                FinalAccusationStage.CauseOfDeath => (int)Accusation.Method,
                FinalAccusationStage.BodyTransport => (int)Accusation.Transport,
                FinalAccusationStage.MurderMotive =>
                    (int)Accusation.DanielBelievedTarget,
                FinalAccusationStage.OrpheusMastermind =>
                    (int)Accusation.OrpheusDesign,
                _ => 0
            };

        private void ClearCurrentAnswer()
        {
            switch (CurrentStage)
            {
                case FinalAccusationStage.Culprit:
                    Accusation.Accused = AccusedPerson.Unknown;
                    break;
                case FinalAccusationStage.MurderLocation:
                    Accusation.Location = MurderLocation.Unknown;
                    break;
                case FinalAccusationStage.CauseOfDeath:
                    Accusation.Method = MurderMethod.Unknown;
                    break;
                case FinalAccusationStage.BodyTransport:
                    Accusation.Transport = BodyTransport.Unknown;
                    break;
                case FinalAccusationStage.MurderMotive:
                    Accusation.DanielBelievedTarget = DanielTargetBelief.Unknown;
                    break;
                case FinalAccusationStage.OrpheusMastermind:
                    Accusation.OrpheusDesign = OrpheusEventDesign.Unknown;
                    break;
            }
        }

        private static T Read<T>(
            IReadOnlyDictionary<string, int> values,
            string key) where T : struct =>
            values.TryGetValue(key, out int value) &&
            Enum.IsDefined(typeof(T), value)
                ? (T)Enum.ToObject(typeof(T), value)
                : default;
    }

    [DisallowMultipleComponent]
    public sealed class FinalAccusationUIController :
        MonoBehaviour,
        IRuntimeModalController
    {
        private sealed class ChoiceControl
        {
            private readonly string[] options;
            private readonly int[] values;
            private readonly TMP_Text text;
            private readonly GameObject promptNode;
            private readonly GameObject choiceNode;
            private int index;

            public ChoiceControl(
                string[] options,
                int[] values,
                TMP_Text text,
                GameObject promptNode,
                GameObject choiceNode)
            {
                this.options = options;
                this.values = values;
                this.text = text;
                this.promptNode = promptNode;
                this.choiceNode = choiceNode;
                Set(0);
            }

            public int Value => values[index];

            public void Advance()
            {
                index = (index + 1) % options.Length;
                text.text = options[index];
            }

            public void Set(int value)
            {
                int matchingIndex = Array.IndexOf(values, value);
                index = matchingIndex >= 0 ? matchingIndex : 0;
                text.text = options[index];
            }

            public void SetVisible(bool visible)
            {
                promptNode.SetActive(visible);
                choiceNode.SetActive(visible);
            }
        }

        private GameObject panel;
        private ChoiceControl[] choices;
        private Toggle coverupToggle;
        private TMP_Text feedback;
        private FinalAccusationSession session;

        public bool IsOpen => panel != null && panel.activeSelf;

        public void Open()
        {
            GameStateManager state = GameStateManager.Instance;
            if (!ProductionSceneCompletionGate.CanStartInteraction(
                    state,
                    "D8-01",
                    FinalAccusationSession.SessionId))
                return;
            Build();
            session = new FinalAccusationSession(state);
            ApplySavedValues();
            RefreshStageVisibility();
            feedback.text = "여섯 항목을 선택하고 최종 논증을 제출하세요.";
            panel.SetActive(true);
        }

        private void Awake() => Build();

        public void Close()
        {
            panel?.SetActive(false);
        }

        private void Build()
        {
            if (panel != null)
                return;

            panel = new GameObject("Final Accusation", typeof(RectTransform), typeof(Image));
            panel.transform.SetParent(transform, false);
            RectTransform rect = panel.GetComponent<RectTransform>();
            rect.anchorMin = new Vector2(0.2f, 0.08f);
            rect.anchorMax = new Vector2(0.8f, 0.92f);
            rect.offsetMin = rect.offsetMax = Vector2.zero;
            panel.GetComponent<Image>().color = new Color(0.025f, 0.04f, 0.08f, 0.97f);

            var layout = panel.AddComponent<VerticalLayoutGroup>();
            layout.padding = new RectOffset(32, 32, 24, 24);
            layout.spacing = 10;
            CreateText("최종 지목", 30);

            choices = FinalAccusationStageCatalog.All
                .Select(CreateStageChoice)
                .ToArray();
            coverupToggle = CreateToggle("Richard의 과거 은폐도 공개");
            feedback = CreateText(string.Empty, 18);
            CreateButton("최종 논증 제출", Submit);
            CreateButton("닫기", () => panel.SetActive(false));
            panel.SetActive(false);
        }

        private void ApplySavedValues()
        {
            FinalAccusation value = session.Accusation;
            choices[0].Set((int)value.Accused);
            choices[1].Set((int)value.Location);
            choices[2].Set((int)value.Method);
            choices[3].Set((int)value.Transport);
            choices[4].Set((int)value.DanielBelievedTarget);
            choices[5].Set((int)value.OrpheusDesign);
            coverupToggle.isOn = value.DiscloseRichardCoverup;
        }

        private void Submit()
        {
            session.Update(
                (AccusedPerson)choices[0].Value,
                (MurderLocation)choices[1].Value,
                (MurderMethod)choices[2].Value,
                (BodyTransport)choices[3].Value,
                (DanielTargetBelief)choices[4].Value,
                (OrpheusEventDesign)choices[5].Value,
                coverupToggle.isOn);
            FinalAccusationSubmission result = session.Submit();
            feedback.text = string.Join("\n", result.Messages);
            if (result.Submitted)
            {
                panel.SetActive(false);
                FindFirstObjectByType<ProductionEndingUIController>()
                    ?.HandleSubmission(result);
            }
            else
            {
                ApplySavedValues();
                RefreshStageVisibility();
            }
        }

        private TMP_Text CreateText(string value, int size)
        {
            var node = new GameObject("Text", typeof(RectTransform), typeof(TextMeshProUGUI));
            node.transform.SetParent(panel.transform, false);
            TMP_Text text = node.GetComponent<TMP_Text>();
            text.text = value;
            text.fontSize = size;
            text.color = Color.white;
            return text;
        }

        private ChoiceControl CreateStageChoice(
            FinalAccusationStageDefinition stage)
        {
            string[] options = new[] { "선택" }
                .Concat(stage.Options.Select(option => option.Label))
                .ToArray();
            int[] values = new[] { 0 }
                .Concat(stage.Options.Select(option => option.EnumValue))
                .ToArray();

            TMP_Text prompt = CreateText(stage.Prompt, 18);
            var node = new GameObject(
                stage.Stage.ToString(),
                typeof(RectTransform),
                typeof(Image),
                typeof(Button));
            node.transform.SetParent(panel.transform, false);
            node.GetComponent<Image>().color = new Color(0.12f, 0.16f, 0.25f);
            TMP_Text text = CreateText(string.Empty, 18);
            text.transform.SetParent(node.transform, false);
            var control = new ChoiceControl(
                options,
                values,
                text,
                prompt.gameObject,
                node);
            node.GetComponent<Button>().onClick.AddListener(control.Advance);
            return control;
        }

        private void RefreshStageVisibility()
        {
            int currentIndex = session.CompletedStageCount;
            for (int index = 0; index < choices.Length; index++)
            {
                choices[index].SetVisible(index == currentIndex);
            }
            coverupToggle.gameObject.SetActive(!session.CurrentStage.HasValue);
        }

        private Toggle CreateToggle(string label)
        {
            var node = new GameObject(label, typeof(RectTransform), typeof(Toggle));
            node.transform.SetParent(panel.transform, false);
            CreateText(label, 18).transform.SetParent(node.transform, false);
            return node.GetComponent<Toggle>();
        }

        private void CreateButton(string label, UnityEngine.Events.UnityAction action)
        {
            var node = new GameObject(label, typeof(RectTransform), typeof(Image), typeof(Button));
            node.transform.SetParent(panel.transform, false);
            node.GetComponent<Image>().color = new Color(0.25f, 0.17f, 0.4f);
            node.GetComponent<Button>().onClick.AddListener(action);
            CreateText(label, 20).transform.SetParent(node.transform, false);
        }
    }
}
