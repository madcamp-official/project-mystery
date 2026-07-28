using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace Wake.UI
{
    /// <summary>
    /// Connects runtime-created screens to the Inspector-authored shell slots.
    /// Controllers keep ownership of data and interactions while the scene owns
    /// fixed placement, safe areas and gameplay chrome policy.
    /// </summary>
    [DisallowMultipleComponent]
    public sealed class ScreenShellRuntimePresenter : MonoBehaviour
    {
        private ScreenShellType shellType;
        private ExplorationNavigationController navigation;
        private GameObject navigationRoot;
        private bool navigationWasEnabled;
        private bool navigationWasVisible;
        private GameObject statusHud;
        private bool statusHudWasVisible;
        private bool policyApplied;

        public void Configure(ScreenShellType type)
        {
            shellType = type;
            if (isActiveAndEnabled)
                ApplyPolicy();
        }

        private void OnEnable() => ApplyPolicy();

        private void OnDisable() => RestorePolicy();

        private void OnDestroy() => RestorePolicy();

        private void ApplyPolicy()
        {
            RestorePolicy();
            if (shellType != ScreenShellType.Ending)
                return;

            navigation = FindFirstObjectByType<ExplorationNavigationController>(
                FindObjectsInactive.Include);
            if (navigation != null)
            {
                navigationWasEnabled = navigation.enabled;
                navigationRoot = navigation.Root;
                navigationWasVisible =
                    navigationRoot != null && navigationRoot.activeSelf;
                navigation.enabled = false;
                navigationRoot?.SetActive(false);
            }

            statusHud = GameObject.Find("Canvas/Status HUD");
            if (statusHud != null)
            {
                statusHudWasVisible = statusHud.activeSelf;
                statusHud.SetActive(false);
            }

            policyApplied = true;
        }

        private void RestorePolicy()
        {
            if (!policyApplied)
                return;

            if (navigation != null)
            {
                navigation.enabled = navigationWasEnabled;
                navigationRoot?.SetActive(navigationWasVisible);
                if (navigationWasEnabled)
                    navigation.Refresh(true);
            }

            statusHud?.SetActive(statusHudWasVisible);
            navigation = null;
            navigationRoot = null;
            statusHud = null;
            policyApplied = false;
        }

        public static bool Place(
            RectTransform target,
            string slotId,
            Vector2 fallbackMin,
            Vector2 fallbackMax)
        {
            if (target == null)
                return false;

            if (RuntimeUiLayoutRegistry.CopyWorldLayout(target, slotId))
                return true;

            target.anchorMin = fallbackMin;
            target.anchorMax = fallbackMax;
            target.anchoredPosition = Vector2.zero;
            target.sizeDelta = Vector2.zero;
            return false;
        }

        public static void PrepareButton(
            Button button,
            float minimumWidth = 96f,
            float minimumHeight = 52f)
        {
            if (button == null)
                return;

            Navigation navigation = button.navigation;
            navigation.mode = Navigation.Mode.Automatic;
            button.navigation = navigation;

            LayoutElement layout =
                button.GetComponent<LayoutElement>() ??
                button.gameObject.AddComponent<LayoutElement>();
            layout.minWidth = minimumWidth;
            layout.minHeight = minimumHeight;
        }

        public static void PrepareReadableText(
            TMP_Text text,
            float minimumSize = 18f)
        {
            if (text == null)
                return;

            text.textWrappingMode = TextWrappingModes.Normal;
            text.overflowMode = TextOverflowModes.Overflow;
            text.enableAutoSizing = true;
            text.fontSizeMin = minimumSize;
            text.fontSizeMax = Mathf.Max(text.fontSize, minimumSize);
        }
    }
}
