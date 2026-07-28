using System;
using System.Collections.Generic;
using System.Linq;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;
using Wake.Core;
using Wake.Evidence;
using Wake.Narrative;
using Wake.Puzzles;

namespace Wake.UI
{
    [DisallowMultipleComponent]
    public sealed class MarcusInterrogationUIController :
        MonoBehaviour,
        IRuntimeModalController
    {
        private static readonly Color Dim = new(0.01f, 0.02f, 0.04f, 0.72f);
        private static readonly Color Panel = new(0.035f, 0.075f, 0.12f, 0.98f);
        private static readonly Color Available = new(0.16f, 0.20f, 0.26f, 1f);
        private static readonly Color Selected = new(0.24f, 0.48f, 0.56f, 1f);
        private static readonly Color Asked = new(0.12f, 0.30f, 0.28f, 1f);

        private readonly List<Button> questionButtons = new();
        private GameObject root;
        private TMP_Text relationshipText;
        private TMP_Text statusText;
        private Button yesButton;
        private Button noButton;
        private Button completeButton;
        private MarcusInterrogationSession session;
        private string selectedQuestionId;

        public bool IsOpen => root != null && root.activeSelf;
        public MarcusInterrogationSession Session => session;
        public string SelectedQuestionId => selectedQuestionId;

        private void Awake()
        {
            BuildUi();
        }

        public bool Open()
        {
            GameStateManager state = GameStateManager.Instance;
            if (root == null ||
                !ProductionSceneCompletionGate.CanStartInteraction(
                    state,
                    MarcusInterrogationCatalog.SceneId,
                    MarcusInterrogationCatalog.SessionId))
            {
                return false;
            }

            session = new MarcusInterrogationSession(
                state,
                MarcusInterrogationCatalog.Create(
                    DialogueDatabase.Instance?.Records.Values),
                tryGrantEvidence: evidenceId =>
                    EvidenceInventory.Instance != null &&
                    EvidenceInventory.Instance.TryAddById(evidenceId));
            IReadOnlyList<string> diagnostics =
                MarcusInterrogationValidator.Validate(session.Definitions);
            if (diagnostics.Count > 0)
            {
                Debug.LogError(string.Join("\n", diagnostics));
                session = null;
                return false;
            }

            selectedQuestionId = null;
            statusText.text =
                "확인할 질문을 고른 뒤 마커스의 답변을 기록하세요.";
            root.SetActive(true);
            root.transform.SetAsLastSibling();
            Refresh();
            FocusFirstAvailableQuestion();
            return true;
        }

        public void Close()
        {
            root?.SetActive(false);
        }

        public bool SelectQuestion(string questionId)
        {
            if (session == null ||
                session.IsCompleted ||
                !session.ContainsQuestion(questionId) ||
                session.Answers.Any(answer => answer.QuestionId == questionId))
            {
                return false;
            }

            selectedQuestionId = questionId;
            statusText.text = "마커스의 답변을 예 또는 아니요로 기록하세요.";
            Refresh();
            EventSystem.current?.SetSelectedGameObject(
                yesButton.gameObject);
            return true;
        }

        public MarcusQuestionResult RecordAnswer(MarcusAnswer answer)
        {
            if (session == null || string.IsNullOrEmpty(selectedQuestionId))
            {
                statusText.text = "먼저 질문을 선택하세요.";
                return MarcusQuestionResult.UnknownQuestion;
            }

            MarcusQuestionResult result = session.Ask(selectedQuestionId, answer);
            statusText.text = result switch
            {
                MarcusQuestionResult.Recorded => "답변을 기록했습니다.",
                MarcusQuestionResult.AlreadyAsked => "이미 확인한 질문입니다.",
                MarcusQuestionResult.LimitReached =>
                    "더 이상 질문할 수 없습니다.",
                MarcusQuestionResult.SessionCompleted =>
                    "이미 종료된 심문입니다.",
                _ => "기록할 수 없는 질문입니다."
            };
            if (result == MarcusQuestionResult.Recorded)
            {
                selectedQuestionId = null;
            }

            Refresh();
            FocusFirstAvailableQuestion();
            return result;
        }

        public MarcusInterrogationCompletion Submit()
        {
            if (session == null)
            {
                return new MarcusInterrogationCompletion(
                    false,
                    MarcusAuthenticationResult.Unresolved,
                    "심문을 시작하지 못했습니다.");
            }

            MarcusInterrogationCompletion result = session.Complete();
            statusText.text = result.Message;
            ToastController.Instance?.Show(result.Message);
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

            int trust = GameStateManager.Instance?.GetTrust("MARCUS") ?? 0;
            string relationship =
                InterrogationRelationshipPresentation.ResolveTrust(trust);
            string questionBudget =
                InterrogationRelationshipPresentation.ResolveQuestionBudget(
                    session.RemainingQuestions,
                    MarcusInterrogationSession.MaximumQuestions);
            relationshipText.text =
                $"마커스 · {relationship} · {questionBudget}";

            for (int index = 0; index < questionButtons.Count; index++)
            {
                MarcusQuestionDefinition definition = session.Definitions[index];
                Button button = questionButtons[index];
                MarcusAnswerRecord? record = session.Answers
                    .Cast<MarcusAnswerRecord?>()
                    .FirstOrDefault(answer =>
                        answer.HasValue &&
                        answer.Value.QuestionId == definition.Id);
                bool asked = record.HasValue;
                bool selected = selectedQuestionId == definition.Id;
                button.interactable =
                    !asked && !session.IsCompleted && session.RemainingQuestions > 0;
                button.image.color = asked ? Asked : selected ? Selected : Available;
                button.GetComponentInChildren<TMP_Text>().text = asked
                    ? $"확인됨 · {definition.Prompt}"
                    : selected
                        ? $"선택 · {definition.Prompt}"
                        : definition.Prompt;
            }

            bool canAnswer =
                !string.IsNullOrEmpty(selectedQuestionId) && !session.IsCompleted;
            yesButton.interactable = canAnswer;
            noButton.interactable = canAnswer;
            completeButton.interactable = !session.IsCompleted;
        }

        private void BuildUi()
        {
            Transform canvas = GameObject.Find("Canvas")?.transform;
            if (canvas == null)
            {
                return;
            }

            root = MakeObject(
                "Marcus Interrogation",
                canvas,
                typeof(Image));
            RectTransform rootRect = root.GetComponent<RectTransform>();
            RequireLayout(rootRect, "interrogation.dim");
            Image dim = root.GetComponent<Image>();
            dim.color = Dim;
            dim.raycastTarget = true;

            GameObject panel = MakeObject(
                "Interrogation Panel",
                root.transform,
                typeof(Image));
            RequireLayout(
                panel.GetComponent<RectTransform>(),
                "interrogation.panel");
            panel.GetComponent<Image>().color = Panel;

            MakeText(
                root.transform,
                "제한 심문 · 마커스",
                "interrogation.title",
                34f,
                TypographyRole.HeadingStrong);
            MakeText(
                root.transform,
                "질문 기회가 제한되어 있습니다. 답변의 모순과 인증 경로를 확인하세요.",
                "interrogation.guidance",
                20f,
                TypographyRole.BodyRegular);
            relationshipText = MakeText(
                root.transform,
                string.Empty,
                "interrogation.state",
                22f,
                TypographyRole.Heading);

            for (int index = 0;
                 index < MarcusInterrogationCatalog.OfficialQuestionCount;
                 index++)
            {
                int captured = index;
                Button button = MakeButton(
                    root.transform,
                    $"Question {index + 1}",
                    $"interrogation.question.{index + 1}",
                    string.Empty);
                button.onClick.AddListener(() => SelectQuestionAt(captured));
                questionButtons.Add(button);
            }

            statusText = MakeText(
                root.transform,
                string.Empty,
                "interrogation.feedback",
                20f,
                TypographyRole.Body);
            Button close = MakeButton(
                root.transform,
                "Back",
                "interrogation.back",
                "뒤로");
            close.onClick.AddListener(Close);
            yesButton = MakeButton(
                root.transform,
                "Yes",
                "interrogation.answer.yes",
                "예");
            yesButton.onClick.AddListener(() => RecordAnswer(MarcusAnswer.Yes));
            noButton = MakeButton(
                root.transform,
                "No",
                "interrogation.answer.no",
                "아니요");
            noButton.onClick.AddListener(() => RecordAnswer(MarcusAnswer.No));
            completeButton = MakeButton(
                root.transform,
                "Complete",
                "interrogation.submit",
                "심문 종료");
            completeButton.onClick.AddListener(() => Submit());
            root.SetActive(false);
        }

        private void SelectQuestionAt(int index)
        {
            if (session != null &&
                index >= 0 &&
                index < session.Definitions.Count)
            {
                SelectQuestion(session.Definitions[index].Id);
            }
        }

        private void FocusFirstAvailableQuestion()
        {
            Button target = questionButtons.FirstOrDefault(button =>
                button != null && button.interactable);
            EventSystem.current?.SetSelectedGameObject(
                target != null ? target.gameObject : completeButton?.gameObject);
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
            string slotId,
            float size,
            TypographyRole role)
        {
            GameObject target = MakeObject(
                slotId,
                parent,
                typeof(TextMeshProUGUI));
            RequireLayout(target.GetComponent<RectTransform>(), slotId);
            TMP_Text text = target.GetComponent<TMP_Text>();
            TypographyService.Apply(text, role);
            text.fontSize = size;
            text.enableAutoSizing = true;
            text.fontSizeMin = Mathf.Max(16f, size - 6f);
            text.fontSizeMax = size;
            text.alignment = TextAlignmentOptions.Center;
            text.overflowMode = TextOverflowModes.Truncate;
            text.color = Color.white;
            text.text = value;
            return text;
        }

        private static Button MakeButton(
            Transform parent,
            string name,
            string slotId,
            string label)
        {
            GameObject target = MakeObject(
                name,
                parent,
                typeof(Image),
                typeof(Button));
            RequireLayout(target.GetComponent<RectTransform>(), slotId);
            target.GetComponent<Image>().color = Available;

            GameObject labelObject = MakeObject(
                "Label",
                target.transform,
                typeof(TextMeshProUGUI));
            RectTransform labelRect = labelObject.GetComponent<RectTransform>();
            labelRect.anchorMin = Vector2.zero;
            labelRect.anchorMax = Vector2.one;
            labelRect.offsetMin = new Vector2(18f, 8f);
            labelRect.offsetMax = new Vector2(-18f, -8f);
            TMP_Text text = labelObject.GetComponent<TMP_Text>();
            TypographyService.Apply(text, TypographyRole.Body);
            text.fontSize = 19f;
            text.enableAutoSizing = true;
            text.fontSizeMin = 15f;
            text.fontSizeMax = 19f;
            text.alignment = TextAlignmentOptions.Center;
            text.overflowMode = TextOverflowModes.Truncate;
            text.raycastTarget = false;
            text.color = Color.white;
            text.text = label;
            return target.GetComponent<Button>();
        }

        private static void RequireLayout(
            RectTransform rect,
            string slotId)
        {
            if (!RuntimeUiLayoutRegistry.CopyWorldLayout(rect, slotId))
            {
                Debug.LogError(
                    $"MarcusInterrogationUIController requires '{slotId}'.");
            }
        }
    }
}
