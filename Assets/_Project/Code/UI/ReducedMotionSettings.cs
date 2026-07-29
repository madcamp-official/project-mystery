using System;
using UnityEngine;

namespace Wake.UI
{
    public static class ReducedMotionSettings
    {
        private const string PlayerPrefsKey = "accessibility.reduced-motion";

        public static event Action<bool> Changed;

        public static bool Enabled
        {
            get => PlayerPrefs.GetInt(PlayerPrefsKey, 0) != 0;
            set
            {
                if (Enabled == value)
                    return;
                PlayerPrefs.SetInt(PlayerPrefsKey, value ? 1 : 0);
                PlayerPrefs.Save();
                Changed?.Invoke(value);
            }
        }
    }
}
