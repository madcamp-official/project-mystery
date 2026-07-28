using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace Wake.UI
{
    /// <summary>
    /// Applies semantic visual tokens in edit mode and at runtime so authored
    /// placeholders preview the same appearance that players will see.
    /// </summary>
    [ExecuteAlways]
    [DisallowMultipleComponent]
    public sealed class UiThemeBinding : MonoBehaviour
    {
        [SerializeField] private bool bindSurface;
        [SerializeField] private UiSurfaceStyle surfaceStyle =
            UiSurfaceStyle.Panel;
        [SerializeField] private bool bindText;
        [SerializeField] private UiTextStyle textStyle = UiTextStyle.Body;
        [SerializeField] private bool bindButton;
        [SerializeField] private UiButtonStyle buttonStyle =
            UiButtonStyle.Secondary;

        private void OnEnable()
        {
            Apply();
        }

#if UNITY_EDITOR
        private void OnValidate()
        {
            Apply();
        }
#endif

        [ContextMenu("Apply Visual Theme")]
        public void Apply()
        {
            if (bindSurface)
            {
                UiVisualThemeService.ApplySurface(
                    GetComponent<Image>(),
                    surfaceStyle);
            }

            if (bindText)
            {
                UiVisualThemeService.ApplyText(
                    GetComponent<TMP_Text>(),
                    textStyle);
            }

            if (bindButton)
            {
                UiVisualThemeService.ApplyButton(
                    GetComponent<Button>(),
                    buttonStyle);
            }
        }
    }
}
