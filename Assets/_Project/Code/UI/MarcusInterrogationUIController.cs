using System;
using System.Collections.Generic;
using System.Linq;
using TMPro;
using UnityEngine;
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
        private static readonly Color Panel = new(0.035f, 0.075f, 0.12f, 0.98f);
        private static readonly Color Available = new(0.16f, 0.20f, 0.26f, 1f);
        private static readonly Color Selected = new(0.24f, 0.48f, 0.56f, 1f);
        private static readonly Color Asked = new(0.12f, 0.30f, 0.28f, 1f);

        private readonly List<Button> questionButtons = new();
        private GameObject root;
        private TMP_Text remainingText;
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
                tryGrantEvidence: evidenceId =>
                    EvidenceInventory.Instance != null &&
                    EvidenceInventory.Instance.TryAddById(evidenceId));
            selectedQuestionId = null;
            statusText.text = "질문을 선택한 뒤 Marcus의 답변을 기록하세요.";
            root.SetActive(true);
            Refresh();
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
                !MarcusInterrogationCatalog.TryGet(questionId, out _) ||
                session.Answers.Any(answer => answer.QuestionId == questionId))
            {
                return false;
            }

            selectedQuestionId = questionId;
            statusText.text = "답변을 예 또는 아니오로 기록하세요.";
            Refresh();
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
                MarcusQuestionResult.LimitReached => "질문 횟수를 모두 사용했습니다.",
                MarcusQuestionResult.SessionCompleted => "이미 종료된 심문입니다.",
                _ => "등록되지 않은 질문입니다."
            };
            if (result == MarcusQuestionResult.Recorded)
            {
                selectedQuestionId = null;
            }
            Refresh();
            return result;
        }

        public MarcusInterrogationCompletion Submit()
        {
            if (session == null)
            {
                return new MarcusInterrogationCompletion(
                    false,
                    MarcusAuthenticationResult.Unresolved,
                    "심문 세션을 시작하지 못했습니다.");
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

            remainingText.text =
                $"남은 질문 {session.RemainingQuestions}/{MarcusInterrogationSession.MaximumQuestions}";
            for (int index = 0; index < questionButtons.Count; index++)
            {
                MarcusQuestionDefinition definition =
                    MarcusInterrogationCatalog.All[index];
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
                    ? $"[질문 완료: {AnswerLabel(record.Value.Answer)}] {definition.Prompt}"
                    : $"{(selected ? "[선택됨]" : "[질문 가능]")} {definition.Prompt}";
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
            rootRect.anchorMin = new Vector2(0.5f, 0.5f);
            rootRect.anchorMax = new Vector2(0.5f, 0.5f);
            rootRect.sizeDelta = new Vector2(860f, 650f);
            root.GetComponent<Image>().color = Panel;

            MakeText(root.transform, "제한 심문: Marcus", 0.88f, 0.98f, 34f);
            MakeText(
                root.transform,
                "최대 다섯 번의 질문으로 Evelyn의 금고 인증 경로를 확인하세요.",
                0.80f,
                0.88f,
                20f);
            remainingText = MakeText(
                root.transform,
                string.Empty,
                0.73f,
                0.80f,
                22f);

            for (int index = 0; index < MarcusInterrogationCatalog.All.Count; index++)
            {
                int captured = index;
                Button button = MakeButton(
                    root.transform,
                    $"Question {index + 1}",
                    0.43f + (4 - index) * 0.058f,
                    0.485f + (4 - index) * 0.058f,
                    string.Empty,
                    0.08f,
                    0.92f);
                button.onClick.AddListener(() =>
                    SelectQuestion(MarcusInterrogationCatalog.All[captured].Id));
                questionButtons.Add(button);
            }

            statusText = MakeText(
                root.transform,
                string.Empty,
                0.29f,
                0.39f,
                20f);
            yesButton = MakeButton(
                root.transform, "Yes", 0.18f, 0.27f, "예", 0.17f, 0.39f);
            yesButton.onClick.AddListener(() => RecordAnswer(MarcusAnswer.Yes));
            noButton = MakeButton(
                root.transform, "No", 0.18f, 0.27f, "아니오", 0.42f, 0.64f);
            noButton.onClick.AddListener(() => RecordAnswer(MarcusAnswer.No));
            completeButton = MakeButton(
                root.transform, "Complete", 0.06f, 0.15f, "심문 종료", 0.56f, 0.79f);
            completeButton.onClick.AddListener(() => Submit());
            Button close = MakeButton(
                root.transform, "Close", 0.06f, 0.15f, "닫기", 0.21f, 0.44f);
            close.onClick.AddListener(Close);
            InteractionTypography.Apply(
                root.transform,
                remainingText,
                null,
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
            float size)
        {
            GameObject target = MakeObject("Label", parent, typeof(TextMeshProUGUI));
            RectTransform rect = target.GetComponent<RectTransform>();
            rect.anchorMin = new Vector2(0.08f, minY);
            rect.anchorMax = new Vector2(0.92f, maxY);
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
            float maxX)
        {
            GameObject target = MakeObject(name, parent, typeof(Image), typeof(Button));
            RectTransform rect = target.GetComponent<RectTransform>();
            rect.anchorMin = new Vector2(minX, minY);
            rect.anchorMax = new Vector2(maxX, maxY);
            rect.offsetMin = Vector2.zero;
            rect.offsetMax = Vector2.zero;
            target.GetComponent<Image>().color = Available;
            TMP_Text text = MakeText(target.transform, label, 0f, 1f, 19f);
            text.raycastTarget = false;
            return target.GetComponent<Button>();
        }

        private static string AnswerLabel(MarcusAnswer answer) =>
            answer == MarcusAnswer.Yes ? "예" : "아니오";
    }
}
