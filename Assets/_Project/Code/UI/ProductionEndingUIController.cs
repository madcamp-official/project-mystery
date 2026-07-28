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
        private static readonly Color32 Paper = new(239, 218, 171, 255);
        private static readonly Color32 Brass = new(201, 154, 72, 255);

        private GameObject root;
        private TMP_Text routeText;
        private TMP_Text titleText;
        private TMP_Text epilogueText;
        private TMP_Text reasonText;

        public bool IsOpen => root != null && root.activeSelf;

        private void Awake() => BuildUi();

        public void HandleSubmission(FinalAccusationSubmission submission)
        {
            if (!submission.Submitted || submission.Result == null)
                return;

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
                return;

            Show(state.FinalEndingId, "저장된 최종 수사 결과입니다.");
        }

        public void ShowEpilogue()
        {
            GameStateManager state = GameStateManager.Instance;
            if (state != null)
                Show(state.FinalEndingId, "귀항 후 사건 평가가 확정되었습니다.");
        }

        public void Close() => root?.SetActive(false);

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
            Canvas.ForceUpdateCanvases();
            ResetScrollPositions();
        }

        private void BuildUi()
        {
            Transform canvas = GameObject.Find("Canvas")?.transform;
            if (canvas == null)
                return;

            root = MakeObject("Production Ending", canvas, typeof(Image));
            RectTransform rootRect = root.GetComponent<RectTransform>();
            ScreenShellRuntimePresenter.Place(
                rootRect,
                ScreenShellSlotIds.EndingBackground,
                Vector2.zero,
                Vector2.one);
            Image background = root.GetComponent<Image>();
            background.sprite =
                Resources.Load<Sprite>("UiOverhaul/ui_ending_background");
            background.preserveAspect = false;
            background.color = Color.white;
            root.AddComponent<ScreenShellRuntimePresenter>()
                .Configure(ScreenShellType.Ending);

            MakeBorder();
            MakeLogo();
            routeText = MakeText(
                "Ending Route",
                ScreenShellSlotIds.EndingRoute,
                24f,
                new Vector2(.08f, .61f),
                new Vector2(.41f, .69f));
            titleText = MakeText(
                "Ending Title",
                ScreenShellSlotIds.EndingTitle,
                46f,
                new Vector2(.08f, .50f),
                new Vector2(.41f, .61f));
            epilogueText = MakeScrollableText(
                "Ending Epilogue",
                ScreenShellSlotIds.EndingEpilogue,
                22f,
                new Vector2(.075f, .37f),
                new Vector2(.415f, .50f));
            reasonText = MakeScrollableText(
                "Ending Reason",
                ScreenShellSlotIds.EndingReason,
                19f,
                new Vector2(.075f, .14f),
                new Vector2(.415f, .35f));

            Button returnToTitle = MakeButton(
                "타이틀로",
                ScreenShellSlotIds.EndingPrimary,
                new Vector2(.08f, .04f),
                new Vector2(.31f, .12f));
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
            string name,
            string slotId,
            float size,
            Vector2 fallbackMin,
            Vector2 fallbackMax)
        {
            GameObject target = MakeObject(
                name,
                root.transform,
                typeof(TextMeshProUGUI));
            ScreenShellRuntimePresenter.Place(
                target.GetComponent<RectTransform>(),
                slotId,
                fallbackMin,
                fallbackMax);
            TMP_Text text = ConfigureText(target, size);
            text.alignment = TextAlignmentOptions.Center;
            ScreenShellRuntimePresenter.PrepareReadableText(text);
            return text;
        }

        private TMP_Text MakeScrollableText(
            string name,
            string slotId,
            float size,
            Vector2 fallbackMin,
            Vector2 fallbackMax)
        {
            GameObject viewport = MakeObject(
                $"{name} Viewport",
                root.transform,
                typeof(Image),
                typeof(RectMask2D),
                typeof(ScrollRect));
            RectTransform viewportRect =
                viewport.GetComponent<RectTransform>();
            ScreenShellRuntimePresenter.Place(
                viewportRect,
                slotId,
                fallbackMin,
                fallbackMax);
            viewport.GetComponent<Image>().color =
                new Color(.018f, .022f, .052f, .91f);
            Outline outline = viewport.AddComponent<Outline>();
            outline.effectColor = Brass;
            outline.effectDistance = new Vector2(2f, -2f);

            GameObject content = MakeObject(
                name,
                viewport.transform,
                typeof(TextMeshProUGUI),
                typeof(ContentSizeFitter));
            RectTransform contentRect =
                content.GetComponent<RectTransform>();
            contentRect.anchorMin = new Vector2(0f, 1f);
            contentRect.anchorMax = new Vector2(1f, 1f);
            contentRect.pivot = new Vector2(.5f, 1f);
            contentRect.anchoredPosition = Vector2.zero;
            contentRect.sizeDelta = new Vector2(-36f, 0f);
            TMP_Text text = ConfigureText(content, size);
            text.alignment = TextAlignmentOptions.TopLeft;
            text.textWrappingMode = TextWrappingModes.Normal;
            text.overflowMode = TextOverflowModes.Overflow;
            text.margin = new Vector4(10f, 12f, 10f, 12f);
            ContentSizeFitter fitter =
                content.GetComponent<ContentSizeFitter>();
            fitter.horizontalFit = ContentSizeFitter.FitMode.Unconstrained;
            fitter.verticalFit = ContentSizeFitter.FitMode.PreferredSize;

            ScrollRect scroll = viewport.GetComponent<ScrollRect>();
            scroll.viewport = viewportRect;
            scroll.content = contentRect;
            scroll.horizontal = false;
            scroll.vertical = true;
            scroll.movementType = ScrollRect.MovementType.Clamped;
            scroll.scrollSensitivity = 36f;
            return text;
        }

        private static TMP_Text ConfigureText(GameObject target, float size)
        {
            TMP_Text text = target.GetComponent<TMP_Text>();
            TypographyService.Apply(text, TypographyRole.Body);
            text.fontSize = size;
            text.color = Paper;
            text.text = string.Empty;
            return text;
        }

        private Button MakeButton(
            string label,
            string slotId,
            Vector2 fallbackMin,
            Vector2 fallbackMax)
        {
            GameObject target = MakeObject(
                label,
                root.transform,
                typeof(Image),
                typeof(Button));
            ScreenShellRuntimePresenter.Place(
                target.GetComponent<RectTransform>(),
                slotId,
                fallbackMin,
                fallbackMax);
            target.GetComponent<Image>().color =
                new Color(.17f, .10f, .28f, .98f);
            Outline outline = target.AddComponent<Outline>();
            outline.effectColor = Brass;
            outline.effectDistance = new Vector2(2f, -2f);

            GameObject labelObject = MakeObject(
                "Label",
                target.transform,
                typeof(TextMeshProUGUI));
            Stretch(labelObject.GetComponent<RectTransform>());
            TMP_Text text = ConfigureText(labelObject, 24f);
            text.text = label;
            text.alignment = TextAlignmentOptions.Center;
            text.raycastTarget = false;

            Button button = target.GetComponent<Button>();
            ScreenShellRuntimePresenter.PrepareButton(button, 180f, 56f);
            return button;
        }

        private void MakeBorder()
        {
            GameObject border = MakeObject(
                "Ending Border",
                root.transform,
                typeof(Image));
            RectTransform rect = border.GetComponent<RectTransform>();
            Stretch(rect);
            rect.offsetMin = new Vector2(16f, 16f);
            rect.offsetMax = new Vector2(-16f, -16f);
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
                "Under the Horizon Logo",
                root.transform,
                typeof(Image));
            ScreenShellRuntimePresenter.Place(
                logoObject.GetComponent<RectTransform>(),
                ScreenShellSlotIds.EndingLogo,
                new Vector2(.07f, .70f),
                new Vector2(.38f, .95f));
            Image image = logoObject.GetComponent<Image>();
            image.sprite =
                Resources.Load<Sprite>("UiOverhaul/logo_transparent");
            image.preserveAspect = true;
            image.raycastTarget = false;
        }

        private void ResetScrollPositions()
        {
            foreach (ScrollRect scroll in
                     root.GetComponentsInChildren<ScrollRect>(true))
            {
                scroll.verticalNormalizedPosition = 1f;
            }
        }

        private static void Stretch(RectTransform rect)
        {
            rect.anchorMin = Vector2.zero;
            rect.anchorMax = Vector2.one;
            rect.anchoredPosition = Vector2.zero;
            rect.sizeDelta = Vector2.zero;
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
