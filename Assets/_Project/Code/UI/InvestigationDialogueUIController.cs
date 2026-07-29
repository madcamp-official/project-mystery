using System;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using Wake.Narrative;

namespace Wake.UI
{
    [DisallowMultipleComponent]
    public sealed class InvestigationDialogueUIController : MonoBehaviour
    {
        private GameObject root;
        private TMP_Text sectionLabel;
        private TMP_Text titleText;
        private TMP_Text bodyText;
        private TMP_Text actionLabel;
        private Button actionButton;
        private IReadOnlyList<string> pages = Array.Empty<string>();
        private int pageIndex;
        private string finalActionLabel = string.Empty;
        private Action completion;

        public bool IsOpen => root != null && root.activeSelf;

        public void Initialize(Transform canvas)
        {
            if (root != null || canvas == null)
                return;

            root = CreateObject(
                "Investigation Dialogue UI",
                canvas,
                typeof(Image));
            RectTransform rootRect = root.GetComponent<RectTransform>();
            RuntimeUiLayoutRegistry.CopyWorldLayout(
                rootRect,
                "dialogue.investigation");
            Image dimmer = root.GetComponent<Image>();
            dimmer.color = new Color32(4, 10, 19, 226);

            GameObject frame = CreateObject(
                "Investigation Frame",
                root.transform,
                typeof(Image),
                typeof(Outline));
            RectTransform frameRect = frame.GetComponent<RectTransform>();
            RuntimeUiLayoutRegistry.CopyWorldLayout(
                frameRect,
                "dialogue.investigation.frame");
            Image frameImage = frame.GetComponent<Image>();
            frameImage.color = new Color32(13, 30, 48, 252);
            Outline outline = frame.GetComponent<Outline>();
            outline.effectColor = new Color32(205, 166, 96, 210);
            outline.effectDistance = new Vector2(2f, -2f);

            sectionLabel = CreateText(
                "Section",
                frame.transform,
                "dialogue.investigation.section",
                26f,
                TextAlignmentOptions.Left,
                new Color32(217, 180, 105, 255));
            sectionLabel.text = "현장 조사";
            TypographyService.Apply(
                sectionLabel,
                TypographyRole.TechnicalStrong);

            titleText = CreateText(
                "Title",
                frame.transform,
                "dialogue.investigation.title",
                48f,
                TextAlignmentOptions.Left,
                new Color32(248, 235, 207, 255));
            TypographyService.Apply(
                titleText,
                TypographyRole.HeadingStrong);

            bodyText = CreateText(
                "Observation",
                frame.transform,
                "dialogue.investigation.body",
                34f,
                TextAlignmentOptions.TopLeft,
                new Color32(226, 228, 221, 255));
            bodyText.textWrappingMode = TextWrappingModes.Normal;
            bodyText.enableAutoSizing = true;
            bodyText.fontSizeMin = 24f;
            bodyText.fontSizeMax = 36f;
            bodyText.overflowMode = TextOverflowModes.Truncate;
            bodyText.lineSpacing = 18f;
            TypographyService.Apply(bodyText, TypographyRole.Body);

            GameObject buttonObject = CreateObject(
                "Action",
                frame.transform,
                typeof(Image),
                typeof(Button));
            RectTransform buttonRect =
                buttonObject.GetComponent<RectTransform>();
            RuntimeUiLayoutRegistry.CopyWorldLayout(
                buttonRect,
                "dialogue.investigation.action");
            Image buttonImage = buttonObject.GetComponent<Image>();
            buttonImage.color = new Color32(207, 169, 96, 255);
            actionButton = buttonObject.GetComponent<Button>();
            actionButton.targetGraphic = buttonImage;

            actionLabel = CreateContainedText(
                "Label",
                buttonObject.transform,
                28f,
                TextAlignmentOptions.Center,
                new Color32(12, 22, 34, 255));
            TypographyService.Apply(
                actionLabel,
                TypographyRole.Choice);

            root.SetActive(false);
        }

        public void ShowTarget(
            string title,
            Action inspectAction)
        {
            Show(
                "현장 조사",
                title,
                "대상을 자세히 조사해 흔적과 기록을 확인합니다.",
                "조사하기",
                inspectAction);
        }

        public void ShowObservation(
            string section,
            string title,
            string observation,
            Action continueAction)
        {
            Show(
                section,
                title,
                observation,
                "계속",
                continueAction);
        }

        public void Hide()
        {
            actionButton?.onClick.RemoveAllListeners();
            pages = Array.Empty<string>();
            completion = null;
            root?.SetActive(false);
        }

        private void Show(
            string section,
            string title,
            string body,
            string action,
            Action callback)
        {
            if (root == null)
                return;

            sectionLabel.text = section ?? string.Empty;
            titleText.text = title ?? string.Empty;
            Canvas.ForceUpdateCanvases();
            pages = DialogueTextPaginator.SplitToFit(
                body,
                bodyText,
                DialogueTypographyMetrics.LineMinimum,
                120);
            pageIndex = 0;
            finalActionLabel = action ?? string.Empty;
            completion = callback;
            actionButton.onClick.RemoveAllListeners();
            actionButton.onClick.AddListener(AdvancePage);
            PresentPage();
            root.transform.SetAsLastSibling();
            root.SetActive(true);
        }

        private void AdvancePage()
        {
            if (pageIndex + 1 < pages.Count)
            {
                pageIndex++;
                PresentPage();
                return;
            }

            Action callback = completion;
            completion = null;
            callback?.Invoke();
        }

        private void PresentPage()
        {
            bodyText.text = pages.Count == 0
                ? string.Empty
                : pages[Mathf.Clamp(pageIndex, 0, pages.Count - 1)];
            actionLabel.text = pageIndex + 1 < pages.Count
                ? "계속"
                : finalActionLabel;
        }

        private static GameObject CreateObject(
            string name,
            Transform parent,
            params Type[] components)
        {
            Type[] required = new Type[components.Length + 2];
            required[0] = typeof(RectTransform);
            required[1] = typeof(CanvasRenderer);
            Array.Copy(components, 0, required, 2, components.Length);
            var result = new GameObject(name, required);
            result.transform.SetParent(parent, false);
            return result;
        }

        private static TMP_Text CreateText(
            string name,
            Transform parent,
            string slotId,
            float fontSize,
            TextAlignmentOptions alignment,
            Color color)
        {
            GameObject textObject = CreateObject(
                name,
                parent,
                typeof(TextMeshProUGUI));
            RectTransform rect = textObject.GetComponent<RectTransform>();
            RuntimeUiLayoutRegistry.CopyWorldLayout(rect, slotId);

            TextMeshProUGUI text =
                textObject.GetComponent<TextMeshProUGUI>();
            text.fontSize = fontSize;
            text.alignment = alignment;
            text.color = color;
            return text;
        }

        private static TMP_Text CreateContainedText(
            string name,
            Transform parent,
            float fontSize,
            TextAlignmentOptions alignment,
            Color color)
        {
            GameObject textObject = CreateObject(
                name,
                parent,
                typeof(TextMeshProUGUI));
            RectTransform rect = textObject.GetComponent<RectTransform>();
            rect.anchorMin = Vector2.zero;
            rect.anchorMax = Vector2.one;
            rect.offsetMin = rect.offsetMax = Vector2.zero;

            TextMeshProUGUI text =
                textObject.GetComponent<TextMeshProUGUI>();
            text.fontSize = fontSize;
            text.alignment = alignment;
            text.color = color;
            return text;
        }
    }
}
