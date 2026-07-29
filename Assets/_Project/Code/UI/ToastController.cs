using UnityEngine;

namespace Wake.UI
{
    public enum ToastTypographyStyle
    {
        Normal,
        Alert
    }

    // Kept as a no-op compatibility surface while legacy callers migrate
    // to contextual UI. The former top-center toast must not be recreated.
    public class ToastController : MonoBehaviour
    {
        public static ToastController Instance { get; private set; }
        public static bool RuntimeSurfaceEnabled => false;

        private void Awake()
        {
            Instance = this;
        }

        private void OnDestroy()
        {
            if (Instance == this)
            {
                Instance = null;
            }
        }

        public void Show(string message)
        {
        }

        public void ShowAlert(string message)
        {
        }

        public void Show(
            string message,
            ToastTypographyStyle style)
        {
        }

        public static TypographyRole ResolveRole(
            ToastTypographyStyle style)
        {
            return style == ToastTypographyStyle.Alert
                ? TypographyRole.SpecialAlert
                : TypographyRole.Body;
        }
    }
}
