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
            GameStateManager state = GameStateManager.Instance;
            string nextScene = ProductionEndingCatalog.GetNextDialogueScene(
                result.EndingId,
                state?.HasCompletedScene(
                    ProductionEndingCatalog.ConfessionSceneId) == true,
                state?.HasCompletedScene(
                    ProductionEndingCatalog.EpilogueSceneId) == true);
            if (!string.IsNullOrEmpty(nextScene) &&
                DialogueController.Instance != null)
            {
                state?.UnlockProductionScene(nextScene);
                if (DialogueController.Instance.StartProductionScene(nextScene))
                {
                    Close();
                    return;
                }
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
            rect.anchorMin = Vector2.zero;
            rect.anchorMax = Vector2.one;
            rect.offsetMin = rect.offsetMax = Vector2.zero;
            RuntimeUiLayoutRegistry.CopyLayout(rect, "modal.ending");
            Image background = root.GetComponent<Image>();
            background.sprite =
                Resources.Load<Sprite>("UiOverhaul/ui_ending_background");
            background.preserveAspect = false;
            background.color = Color.white;

            MakePanel(new Vector2(0.065f, 0.40f), new Vector2(0.43f, 0.69f));
            MakePanel(new Vector2(0.065f, 0.14f), new Vector2(0.43f, 0.38f));
            MakeBorder();
            MakeLogo();

            routeText = MakeText("", 0.61f, 0.67f, 24f, 0.08f, 0.41f);
            titleText = MakeText("", 0.51f, 0.61f, 46f, 0.08f, 0.41f);
            epilogueText = MakeText("", 0.42f, 0.51f, 22f, 0.085f, 0.405f);
            reasonText = MakeText("", 0.19f, 0.34f, 19f, 0.085f, 0.405f);
            Button returnToTitle =
                MakeButton("타이틀로", 0.045f, 0.115f);
            returnToTitle.onClick.AddListener(ReturnToTitle);
            FeatureTypography.ApplyEnding(
                root.transform,
                routeText,
                titleText,
                epilogueText,
                reasonText);
            root.AddComponent<UiPanelEntranceAnimator>();
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
            float size,
            float minX = 0.08f,
            float maxX = 0.92f)
        {
            GameObject target = MakeObject(
                "Label",
                root.transform,
                typeof(TextMeshProUGUI));
            RectTransform rect = target.GetComponent<RectTransform>();
            rect.anchorMin = new Vector2(minX, minY);
            rect.anchorMax = new Vector2(maxX, maxY);
            rect.offsetMin = Vector2.zero;
            rect.offsetMax = Vector2.zero;
            TMP_Text text = target.GetComponent<TMP_Text>();
            TypographyService.Apply(text, TypographyRole.Body);
            text.fontSize = size;
            text.color = new Color32(239, 218, 171, 255);
            text.alignment = TextAlignmentOptions.Center;
            text.text = value;
            text.textWrappingMode = TextWrappingModes.Normal;
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
            rect.anchorMin = new Vector2(0.09f, minY);
            rect.anchorMax = new Vector2(0.29f, maxY);
            rect.offsetMin = Vector2.zero;
            rect.offsetMax = Vector2.zero;
            target.GetComponent<Image>().color =
                new Color(0.17f, 0.10f, 0.28f, 0.98f);
            Outline outline = target.AddComponent<Outline>();
            outline.effectColor = new Color32(201, 154, 72, 255);
            outline.effectDistance = new Vector2(2f, -2f);
            GameObject labelObject = MakeObject(
                "Label", target.transform, typeof(TextMeshProUGUI));
            RectTransform labelRect = labelObject.GetComponent<RectTransform>();
            labelRect.anchorMin = Vector2.zero;
            labelRect.anchorMax = Vector2.one;
            labelRect.offsetMin = labelRect.offsetMax = Vector2.zero;
            TMP_Text text = labelObject.GetComponent<TMP_Text>();
            TypographyService.Apply(text, TypographyRole.Body);
            text.text = label;
            text.fontSize = 24f;
            text.color = new Color32(239, 218, 171, 255);
            text.alignment = TextAlignmentOptions.Center;
            text.transform.SetParent(target.transform, false);
            text.raycastTarget = false;
            return target.GetComponent<Button>();
        }

        private void MakePanel(Vector2 anchorMin, Vector2 anchorMax)
        {
            GameObject panel = MakeObject(
                "Ending Panel", root.transform, typeof(Image));
            RectTransform rect = panel.GetComponent<RectTransform>();
            rect.anchorMin = anchorMin;
            rect.anchorMax = anchorMax;
            rect.offsetMin = rect.offsetMax = Vector2.zero;
            panel.GetComponent<Image>().color =
                new Color(0.018f, 0.022f, 0.052f, 0.91f);
            Outline outline = panel.AddComponent<Outline>();
            outline.effectColor = new Color32(180, 131, 56, 230);
            outline.effectDistance = new Vector2(2f, -2f);
        }

        private void MakeBorder()
        {
            GameObject border = MakeObject(
                "Ending Border", root.transform, typeof(Image));
            RectTransform rect = border.GetComponent<RectTransform>();
            rect.anchorMin = new Vector2(0.012f, 0.022f);
            rect.anchorMax = new Vector2(0.988f, 0.978f);
            rect.offsetMin = rect.offsetMax = Vector2.zero;
            Image image = border.GetComponent<Image>();
            image.color = Color.clear;
            image.raycastTarget = false;
            Outline outline = border.AddComponent<Outline>();
            outline.effectColor = new Color32(196, 150, 72, 220);
            outline.effectDistance = new Vector2(2f, -2f);
        }

        private void MakeLogo()
        {
            GameObject logoObject = MakeObject(
                "Under the Horizon Logo", root.transform, typeof(Image));
            RectTransform rect = logoObject.GetComponent<RectTransform>();
            rect.anchorMin = rect.anchorMax = new Vector2(0.225f, 0.835f);
            rect.sizeDelta = new Vector2(410f, 230f);
            Image image = logoObject.GetComponent<Image>();
            image.sprite =
                Resources.Load<Sprite>("UiOverhaul/logo_transparent");
            image.preserveAspect = true;
            image.raycastTarget = false;
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
