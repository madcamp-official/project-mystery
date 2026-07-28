using UnityEngine;
using UnityEngine.UI;

namespace Wake.UI
{
    public class SettingsController : MonoBehaviour
    {
        private Slider musicSlider;
        private Slider sfxSlider;
        private Button closeButton;

        private void Awake()
        {
            RectTransform canvas = GameObject.Find("Canvas")
                .transform as RectTransform;
            Transform settingsRoot = canvas.Find("Settings Popup");

            musicSlider = settingsRoot.Find("Settings/Sound").GetComponent<Slider>();
            sfxSlider = settingsRoot.Find("Settings/Sound (1)").GetComponent<Slider>();
            closeButton = settingsRoot.Find("Close").GetComponent<Button>();
            Transform credit = settingsRoot.Find("Settings/Credit");
            if (credit != null)
                credit.gameObject.SetActive(false);
            Transform exit = settingsRoot.Find("Exit Btn");
            if (exit != null)
                exit.gameObject.SetActive(false);
            FitPopupInsideCanvas(
                settingsRoot as RectTransform,
                canvas);

            closeButton.onClick.AddListener(() => UIManager.Instance.CloseSettings());

            musicSlider.onValueChanged.AddListener(OnMusicChanged);
            sfxSlider.onValueChanged.AddListener(OnSfxChanged);
        }

        internal static void FitPopupInsideCanvas(
            RectTransform popup,
            RectTransform canvas)
        {
            if (popup == null || canvas == null)
                return;

            popup.anchorMin = popup.anchorMax = new Vector2(0.5f, 0.5f);
            popup.pivot = new Vector2(0.5f, 0.5f);
            popup.anchoredPosition = Vector2.zero;

            // The legacy popup artwork extends beyond its root rect.
            // These bounds describe the complete authored panel and close
            // action while preserving the source artwork's safe margin.
            const float authoredWidth = 356.5f;
            const float authoredHeight = 484.6f;
            Canvas rootCanvas = canvas.GetComponentInParent<Canvas>();
            float canvasScale = rootCanvas != null
                ? Mathf.Max(0.01f, rootCanvas.scaleFactor)
                : 1f;
            float maxWidth =
                Screen.safeArea.width / canvasScale * 0.88f;
            float maxHeight =
                Screen.safeArea.height / canvasScale * 0.86f;
            float scale = Mathf.Min(
                3f,
                maxWidth / authoredWidth,
                maxHeight / authoredHeight);
            popup.localScale = Vector3.one * Mathf.Max(1f, scale);
        }

        private void OnMusicChanged(float value)
        {
            Debug.Log($"Music volume set to {value:0.00}");
        }

        private void OnSfxChanged(float value)
        {
            Debug.Log($"SFX volume set to {value:0.00}");
        }
    }
}
