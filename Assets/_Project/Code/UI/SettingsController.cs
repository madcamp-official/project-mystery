using UnityEngine;
using UnityEngine.UI;

namespace Seat0A.UI
{
    public class SettingsController : MonoBehaviour
    {
        private Slider musicSlider;
        private Slider sfxSlider;
        private Button closeButton;
        private Button exitButton;
        private Button creditButton;

        private void Awake()
        {
            Transform canvas = GameObject.Find("Canvas").transform;
            Transform settingsRoot = canvas.Find("Settings Popup");

            musicSlider = settingsRoot.Find("Settings/Sound").GetComponent<Slider>();
            sfxSlider = settingsRoot.Find("Settings/Sound (1)").GetComponent<Slider>();
            creditButton = settingsRoot.Find("Settings/Credit").GetComponent<Button>();
            closeButton = settingsRoot.Find("Close").GetComponent<Button>();
            exitButton = settingsRoot.Find("Exit Btn").GetComponent<Button>();

            closeButton.onClick.AddListener(() => UIManager.Instance.CloseSettings());
            exitButton.onClick.AddListener(OnExitClicked);
            creditButton.onClick.AddListener(OnCreditClicked);

            musicSlider.onValueChanged.AddListener(OnMusicChanged);
            sfxSlider.onValueChanged.AddListener(OnSfxChanged);
        }

        private void OnMusicChanged(float value)
        {
            Debug.Log($"Music volume set to {value:0.00}");
        }

        private void OnSfxChanged(float value)
        {
            Debug.Log($"SFX volume set to {value:0.00}");
        }

        private void OnCreditClicked()
        {
            Debug.Log("Credit button clicked (placeholder - no credit screen yet).");
        }

        private void OnExitClicked()
        {
            Debug.Log("Exit requested.");
#if UNITY_EDITOR
            UnityEditor.EditorApplication.isPlaying = false;
#else
            Application.Quit();
#endif
        }
    }
}
