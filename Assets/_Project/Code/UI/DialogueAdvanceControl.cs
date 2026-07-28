using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace Wake.UI
{
    public enum DialogueAdvanceState
    {
        Hidden,
        RevealLine,
        AdvanceLine
    }

    [DisallowMultipleComponent]
    [RequireComponent(typeof(Button))]
    [RequireComponent(typeof(CanvasGroup))]
    public sealed class DialogueAdvanceControl : MonoBehaviour
    {
        private const string RevealHint = "문장 전체 보기";
        private const string AdvanceHint = "다음 대사";

        [SerializeField] private Button button;
        [SerializeField] private Image icon;
        [SerializeField] private TMP_Text legacyLabel;
        [SerializeField] private Sprite normalSprite;
        [SerializeField] private Sprite pressedSprite;
        [SerializeField, Range(0f, 1f)] private float revealAlpha = 0.72f;

        private CanvasGroup canvasGroup;

        public DialogueAdvanceState State { get; private set; } =
            DialogueAdvanceState.Hidden;
        public string AccessibleHint { get; private set; } = AdvanceHint;

        public void Initialize(Button targetButton = null)
        {
            button = targetButton != null
                ? targetButton
                : GetComponent<Button>();
            icon = button != null
                ? button.targetGraphic as Image ??
                  button.GetComponent<Image>()
                : GetComponent<Image>();
            legacyLabel = GetComponentInChildren<TMP_Text>(true);
            canvasGroup = GetComponent<CanvasGroup>() ??
                          gameObject.AddComponent<CanvasGroup>();

            ApplySprites();
            HideLegacyLabel();
            ApplyState();
        }

        public void SetSprites(Sprite normal, Sprite pressed)
        {
            normalSprite = normal;
            pressedSprite = pressed;
            ApplySprites();
        }

        public void SetState(DialogueAdvanceState state)
        {
            State = state;
            AccessibleHint = state == DialogueAdvanceState.RevealLine
                ? RevealHint
                : AdvanceHint;
            ApplyState();
        }

        private void Awake()
        {
            Initialize();
        }

        private void OnValidate()
        {
            if (button == null)
                button = GetComponent<Button>();
            if (icon == null && button != null)
                icon = button.targetGraphic as Image ??
                       button.GetComponent<Image>();
            if (legacyLabel == null)
                legacyLabel = GetComponentInChildren<TMP_Text>(true);

            ApplySprites();
            HideLegacyLabel();
        }

        private void ApplySprites()
        {
            if (button == null || icon == null)
                return;

            if (normalSprite != null)
                icon.sprite = normalSprite;
            icon.preserveAspect = true;
            button.targetGraphic = icon;
            button.transition = Selectable.Transition.SpriteSwap;

            SpriteState spriteState = button.spriteState;
            spriteState.highlightedSprite = normalSprite;
            spriteState.selectedSprite = normalSprite;
            spriteState.pressedSprite = pressedSprite;
            spriteState.disabledSprite = normalSprite;
            button.spriteState = spriteState;
        }

        private void ApplyState()
        {
            bool visible = State != DialogueAdvanceState.Hidden;
            if (gameObject.activeSelf != visible)
                gameObject.SetActive(visible);
            if (!visible)
                return;

            if (canvasGroup == null)
                canvasGroup = GetComponent<CanvasGroup>() ??
                              gameObject.AddComponent<CanvasGroup>();
            canvasGroup.alpha = State == DialogueAdvanceState.RevealLine
                ? revealAlpha
                : 1f;
            canvasGroup.interactable = true;
            canvasGroup.blocksRaycasts = true;
            if (button != null)
                button.interactable = true;
            gameObject.name = "Next";
        }

        private void HideLegacyLabel()
        {
            if (legacyLabel == null)
                return;

            legacyLabel.text = string.Empty;
            legacyLabel.gameObject.SetActive(false);
        }
    }
}
