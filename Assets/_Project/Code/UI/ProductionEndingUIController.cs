using System;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using Wake.Core;
using Wake.Narrative;

namespace Wake.UI
{
    [DisallowMultipleComponent]
    public sealed class ProductionEndingUIController :
        MonoBehaviour,
        IRuntimeModalController
    {
        private GameObject root;
        private TMP_Text routeText;
        private TMP_Text titleText;
        private TMP_Text epilogueText;
        private TMP_Text reasonText;

        public bool IsOpen => root != null && root.activeSelf;

        private void Awake()
        {
            BuildUi();
        }

        public void HandleSubmission(FinalAccusationSubmission submission)
        {
            if (!submission.Submitted || submission.Result == null)
            {
                return;
            }

            FinalAccusationResult result = submission.Result;
            if (FinalAccusationResolver.OpensD8Confession(result.EndingId) &&
                DialogueController.Instance != null &&
                DialogueController.Instance.StartProductionScene(
                    ProductionEndingCatalog.ConfessionSceneId))
            {
                Close();
                return;
            }

            Show(result.EndingId, result.Reason);
        }

        public void ShowStoredEnding()
        {
            GameStateManager state = GameStateManager.Instance;
            if (state == null || string.IsNullOrEmpty(state.FinalEndingId))
            {
                return;
            }

            Show(state.FinalEndingId, "저장된 최종 수사 결과입니다.");
        }

        public void ShowEpilogue()
        {
            GameStateManager state = GameStateManager.Instance;
            if (state != null)
            {
                Show(state.FinalEndingId, "귀항 후 사건 평가가 확정됐습니다.");
            }
        }

        public void Close()
        {
            root?.SetActive(false);
        }

        private void Show(string endingId, string reason)
        {
            if (root == null ||
                !ProductionEndingCatalog.TryGet(
                    endingId,
                    out ProductionEndingDefinition ending))
            {
                return;
            }

            routeText.text = ending.RouteLabel;
            titleText.text = ending.Title;
            epilogueText.text = ending.Epilogue;
            reasonText.text = reason ?? string.Empty;
            root.SetActive(true);
        }

        private void BuildUi()
        {
            Transform canvas = GameObject.Find("Canvas")?.transform;
            if (canvas == null)
            {
                return;
            }

            root = MakeObject("Production Ending", canvas, typeof(Image));
            RectTransform rect = root.GetComponent<RectTransform>();
            rect.anchorMin = new Vector2(0.5f, 0.5f);
            rect.anchorMax = new Vector2(0.5f, 0.5f);
            rect.sizeDelta = new Vector2(820f, 520f);
            root.GetComponent<Image>().color =
                new Color(0.025f, 0.04f, 0.08f, 0.99f);

            routeText = MakeText("", 0.84f, 0.95f, 24f);
            titleText = MakeText("", 0.69f, 0.84f, 38f);
            epilogueText = MakeText("", 0.34f, 0.67f, 23f);
            reasonText = MakeText("", 0.19f, 0.33f, 18f);
            Button returnToTitle =
                MakeButton("타이틀로 돌아가기", 0.06f, 0.16f);
            returnToTitle.onClick.AddListener(ReturnToTitle);
            root.SetActive(false);
        }

        private void ReturnToTitle()
        {
            Close();
            UIManager.Instance?.ShowStartScene();
        }

        private TMP_Text MakeText(
            string value,
            float minY,
            float maxY,
            float size)
        {
            GameObject target = MakeObject(
                "Label",
                root.transform,
                typeof(TextMeshProUGUI));
            RectTransform rect = target.GetComponent<RectTransform>();
            rect.anchorMin = new Vector2(0.08f, minY);
            rect.anchorMax = new Vector2(0.92f, maxY);
            rect.offsetMin = Vector2.zero;
            rect.offsetMax = Vector2.zero;
            TMP_Text text = target.GetComponent<TMP_Text>();
            text.font = StatusHUDController.RuntimeKoreanFont;
            text.fontSize = size;
            text.color = Color.white;
            text.alignment = TextAlignmentOptions.Center;
            text.text = value;
            return text;
        }

        private Button MakeButton(string label, float minY, float maxY)
        {
            GameObject target = MakeObject(
                label,
                root.transform,
                typeof(Image),
                typeof(Button));
            RectTransform rect = target.GetComponent<RectTransform>();
            rect.anchorMin = new Vector2(0.36f, minY);
            rect.anchorMax = new Vector2(0.64f, maxY);
            rect.offsetMin = Vector2.zero;
            rect.offsetMax = Vector2.zero;
            target.GetComponent<Image>().color =
                new Color(0.25f, 0.17f, 0.4f, 1f);
            TMP_Text text = MakeText(label, 0f, 1f, 21f);
            text.transform.SetParent(target.transform, false);
            text.raycastTarget = false;
            return target.GetComponent<Button>();
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
    }
}
