using System;
using UnityEngine;

namespace Wake.UI
{
    public static class RuntimeModalTransition
    {
        public static void Open(GameObject root, Action activated = null)
        {
            if (root == null)
                return;

            void Activate()
            {
                root.SetActive(true);
                root.transform.SetAsLastSibling();
                activated?.Invoke();
            }

            UIManager manager = UIManager.Instance;
            if (manager == null)
            {
                Activate();
                return;
            }
            manager.OpenRuntimeModalAnimated(root, Activate);
        }

        public static void Close(GameObject root, Action deactivated = null)
        {
            if (root == null)
            {
                deactivated?.Invoke();
                return;
            }

            void Deactivate() => root.SetActive(false);

            UIManager manager = UIManager.Instance;
            if (manager == null)
            {
                Deactivate();
                deactivated?.Invoke();
                return;
            }
            manager.CloseRuntimeModalAnimated(
                root,
                Deactivate,
                deactivated);
        }
    }
}
