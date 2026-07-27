using System;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

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
            Stretch(rootRect);
            RuntimeUiLayoutRegistry.CopyLayout(
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
            frameRect.anchorMin = new Vector2(.16f, .14f);
            frameRect.anchorMax = new Vector2(.84f, .86f);
            frameRect.offsetMin = frameRect.offsetMax = Vector2.zero;
            Image frameImage = frame.GetComponent<Image>();
            frameImage.color = new Color32(13, 30, 48, 252);
            Outline outline = frame.GetComponent<Outline>();
            outline.effectColor = new Color32(205, 166, 96, 210);
            outline.effectDistance = new Vector2(2f, -2f);

            sectionLabel = CreateText(
                "Section",
                frame.transform,
                new Vector2(.08f, .79f),
                new Vector2(.92f, .93f),
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
                new Vector2(.08f, .61f),
                new Vector2(.92f, .82f),
                48f,
                TextAlignmentOptions.Left,
                new Color32(248, 235, 207, 255));
            TypographyService.Apply(
                titleText,
                TypographyRole.HeadingStrong);

            bodyText = CreateText(
                "Observation",
                frame.transform,
                new Vector2(.08f, .23f),
                new Vector2(.92f, .61f),
                34f,
                TextAlignmentOptions.TopLeft,
                new Color32(226, 228, 221, 255));
            bodyText.textWrappingMode = TextWrappingModes.Normal;
            bodyText.enableAutoSizing = true;
            bodyText.fontSizeMin = 24f;
            bodyText.fontSizeMax = 36f;
            bodyText.overflowMode = TextOverflowModes.Ellipsis;
            bodyText.lineSpacing = 18f;
            TypographyService.Apply(bodyText, TypographyRole.Body);

            GameObject buttonObject = CreateObject(
                "Action",
                frame.transform,
                typeof(Image),
                typeof(Button));
            RectTransform buttonRect =
                buttonObject.GetComponent<RectTransform>();
            buttonRect.anchorMin = new Vector2(.32f, .07f);
            buttonRect.anchorMax = new Vector2(.68f, .20f);
            buttonRect.offsetMin = buttonRect.offsetMax = Vector2.zero;
            Image buttonImage = buttonObject.GetComponent<Image>();
            buttonImage.color = new Color32(207, 169, 96, 255);
            actionButton = buttonObject.GetComponent<Button>();
            actionButton.targetGraphic = buttonImage;

            actionLabel = CreateText(
                "Label",
                buttonObject.transform,
                Vector2.zero,
                Vector2.one,
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
            bodyText.text = body ?? string.Empty;
            actionLabel.text = action ?? string.Empty;
            actionButton.onClick.RemoveAllListeners();
            actionButton.onClick.AddListener(() => callback?.Invoke());
            root.transform.SetAsLastSibling();
            root.SetActive(true);
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
            Vector2 anchorMin,
            Vector2 anchorMax,
            float fontSize,
            TextAlignmentOptions alignment,
            Color color)
        {
            GameObject textObject = CreateObject(
                name,
                parent,
                typeof(TextMeshProUGUI));
            RectTransform rect = textObject.GetComponent<RectTransform>();
            rect.anchorMin = anchorMin;
            rect.anchorMax = anchorMax;
            rect.offsetMin = rect.offsetMax = Vector2.zero;

            TextMeshProUGUI text =
                textObject.GetComponent<TextMeshProUGUI>();
            text.fontSize = fontSize;
            text.alignment = alignment;
            text.color = color;
            return text;
        }

        private static void Stretch(RectTransform rect)
        {
            rect.anchorMin = Vector2.zero;
            rect.anchorMax = Vector2.one;
            rect.offsetMin = rect.offsetMax = Vector2.zero;
        }
    }
}
