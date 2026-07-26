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

        public void Update(
            AccusedPerson accused,
            MurderLocation location,
            MurderMethod method,
            BodyTransport transport,
            DanielTargetBelief target,
            OrpheusEventDesign design,
            bool discloseCoverup)
        {
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
            if (Accusation.Accused == AccusedPerson.Unknown)
                messages.Add("범인을 선택하세요.");
            if (Accusation.Location == MurderLocation.Unknown)
                messages.Add("살해 장소를 선택하세요.");
            if (Accusation.Method == MurderMethod.Unknown)
                messages.Add("살해 방법을 선택하세요.");
            if (Accusation.Transport == BodyTransport.Unknown)
                messages.Add("시신 운반 경로를 선택하세요.");
            if (Accusation.DanielBelievedTarget == DanielTargetBelief.Unknown)
                messages.Add("Daniel이 믿은 표적을 선택하세요.");
            if (Accusation.OrpheusDesign == OrpheusEventDesign.Unknown)
                messages.Add("Orpheus 사건의 설계를 선택하세요.");

            string[] missing = CrimeDeductions
                .Where(id => !state.HasUnlockedDeduction(id))
                .ToArray();
            if (missing.Length > 0)
            {
                messages.Add($"핵심 논증이 부족합니다: {string.Join(", ", missing)}");
            }

            if (Accusation.DiscloseRichardCoverup &&
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
            IReadOnlyList<string> messages = GetPreSubmitMessages();
            bool missingAnswer = messages.Any(message => message.EndsWith("선택하세요."));
            bool missingCase = messages.Any(message => message.StartsWith("핵심 논증"));
            if (missingAnswer || missingCase)
            {
                return new FinalAccusationSubmission(false, null, messages);
            }

            FinalAccusationResult result = resolver.Resolve(Accusation);
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
                    $"coverup={(Accusation.DiscloseRichardCoverup ? 1 : 0)}"
                },
                step = 6,
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
        }

        private static T Read<T>(
            IReadOnlyDictionary<string, int> values,
            string key) where T : struct =>
            values.TryGetValue(key, out int value) &&
            Enum.IsDefined(typeof(T), value)
                ? (T)Enum.ToObject(typeof(T), value)
                : default;
    }

    public sealed class FinalAccusationUIController : MonoBehaviour
    {
        private sealed class ChoiceControl
        {
            private readonly string[] options;
            private readonly TMP_Text text;

            public ChoiceControl(string[] options, TMP_Text text)
            {
                this.options = options;
                this.text = text;
                Set(0);
            }

            public int Value { get; private set; }

            public void Advance() => Set((Value + 1) % options.Length);

            public void Set(int value)
            {
                Value = Mathf.Clamp(value, 0, options.Length - 1);
                text.text = options[Value];
            }
        }

        private GameObject panel;
        private ChoiceControl[] choices;
        private Toggle coverupToggle;
        private TMP_Text feedback;
        private FinalAccusationSession session;

        public void Open()
        {
            if (GameStateManager.Instance == null)
                return;
            Build();
            session = new FinalAccusationSession(GameStateManager.Instance);
            ApplySavedValues();
            feedback.text = "여섯 항목을 선택한 뒤 최종 논증을 제출하세요.";
            panel.SetActive(true);
        }

        private void Awake() => Build();

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
            CreateText("최종 논증", 30);

            choices = new[]
            {
                CreateChoice("범인", "선택", "Evelyn", "Richard"),
                CreateChoice("살해 장소", "선택", "Horizon Room", "Ballast Control Annex"),
                CreateChoice("살해 방법", "선택", "둔기", "질소 질식"),
                CreateChoice("시신 운반", "선택", "외부", "천장 서비스 레일"),
                CreateChoice("Daniel의 표적", "선택", "Evelyn", "Richard"),
                CreateChoice("Orpheus 설계", "선택", "사고", "보험 사기")
            };
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

        private ChoiceControl CreateChoice(string label, params string[] options)
        {
            CreateText(label, 18);
            var node = new GameObject(
                label, typeof(RectTransform), typeof(Image), typeof(Button));
            node.transform.SetParent(panel.transform, false);
            node.GetComponent<Image>().color = new Color(0.12f, 0.16f, 0.25f);
            TMP_Text text = CreateText(string.Empty, 18);
            text.transform.SetParent(node.transform, false);
            var control = new ChoiceControl(options, text);
            node.GetComponent<Button>().onClick.AddListener(control.Advance);
            return control;
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
