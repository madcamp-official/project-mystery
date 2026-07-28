using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace Wake.UI
{
    public enum DialogueChoiceDirection
    {
        Left,
        Right,
        Up,
        Down
    }

    public static class DialogueChoiceNavigationPolicy
    {
        public static int FindNeighbor(
            int index,
            int count,
            int columns,
            DialogueChoiceDirection direction)
        {
            if (index < 0 || index >= count || count <= 0)
                return -1;

            columns = Mathf.Clamp(columns, 1, count);
            int row = index / columns;
            int column = index % columns;
            int candidate = direction switch
            {
                DialogueChoiceDirection.Left =>
                    column > 0 ? index - 1 : -1,
                DialogueChoiceDirection.Right =>
                    column + 1 < columns && index + 1 < count
                        ? index + 1
                        : -1,
                DialogueChoiceDirection.Up =>
                    row > 0 ? index - columns : -1,
                DialogueChoiceDirection.Down =>
                    index + columns < count ? index + columns : -1,
                _ => -1
            };
            return candidate;
        }
    }

    [DisallowMultipleComponent]
    public sealed class DialogueChoicePresentation : MonoBehaviour
    {
        private const float EntranceDuration = 0.16f;
        private const float StaggerDelay = 0.035f;
        private const float EntranceScale = 0.94f;

        [SerializeField] private RectTransform container;
        [SerializeField] private Button[] buttons;

        private Coroutine entranceRoutine;
        private readonly Dictionary<Graphic, float> baseGraphicAlphas =
            new();

        public int ActiveCount { get; private set; }

        public void Initialize(
            RectTransform targetContainer,
            IReadOnlyList<Button> targetButtons)
        {
            container = targetContainer;
            buttons = targetButtons?.Where(button => button != null).ToArray()
                      ?? System.Array.Empty<Button>();
            baseGraphicAlphas.Clear();
            foreach (Button button in buttons)
            {
                if (button.GetComponent<UiHoverFeedback>() == null)
                    button.gameObject.AddComponent<UiHoverFeedback>();
                foreach (Graphic graphic in
                         button.GetComponentsInChildren<Graphic>(true))
                {
                    baseGraphicAlphas[graphic] = graphic.color.a;
                }
            }
        }

        public void Show()
        {
            if (container == null || buttons == null)
                return;

            container.gameObject.SetActive(true);
            Button[] active = buttons
                .Where(button =>
                    button != null && button.gameObject.activeSelf)
                .ToArray();
            ActiveCount = active.Length;
            ConfigureNavigation(active);
            StopEntrance();

            if (!Application.isPlaying)
            {
                foreach (Button button in active)
                    ApplyVisual(button, 1f, 1f);
                FocusFirst(active);
                return;
            }

            foreach (Button button in active)
                ApplyVisual(button, 0f, EntranceScale);
            entranceRoutine = StartCoroutine(AnimateEntrance(active));
        }

        public void Hide()
        {
            StopEntrance();
            ActiveCount = 0;
            if (container == null)
                return;

            if (EventSystem.current != null)
            {
                GameObject selected =
                    EventSystem.current.currentSelectedGameObject;
                if (selected != null &&
                    selected.transform.IsChildOf(container))
                {
                    EventSystem.current.SetSelectedGameObject(null);
                }
            }
            container.gameObject.SetActive(false);
        }

        private IEnumerator AnimateEntrance(IReadOnlyList<Button> active)
        {
            float elapsed = 0f;
            float totalDuration =
                EntranceDuration +
                Mathf.Max(0, active.Count - 1) * StaggerDelay;
            while (elapsed < totalDuration)
            {
                elapsed += Time.unscaledDeltaTime;
                for (int i = 0; i < active.Count; i++)
                {
                    float localTime = elapsed - i * StaggerDelay;
                    float progress = Mathf.Clamp01(
                        localTime / EntranceDuration);
                    float eased = 1f - Mathf.Pow(1f - progress, 3f);
                    ApplyVisual(
                        active[i],
                        eased,
                        Mathf.Lerp(EntranceScale, 1f, eased));
                }
                yield return null;
            }

            foreach (Button button in active)
                ApplyVisual(button, 1f, 1f);
            FocusFirst(active);
            entranceRoutine = null;
        }

        private void ConfigureNavigation(IReadOnlyList<Button> active)
        {
            int columns = 1;
            GridLayoutGroup grid =
                container.GetComponent<GridLayoutGroup>();
            if (grid != null)
                columns = Mathf.Max(1, grid.constraintCount);

            for (int i = 0; i < active.Count; i++)
            {
                Navigation navigation = active[i].navigation;
                navigation.mode = Navigation.Mode.Explicit;
                navigation.selectOnLeft =
                    Neighbor(active, i, columns,
                        DialogueChoiceDirection.Left);
                navigation.selectOnRight =
                    Neighbor(active, i, columns,
                        DialogueChoiceDirection.Right);
                navigation.selectOnUp =
                    Neighbor(active, i, columns,
                        DialogueChoiceDirection.Up);
                navigation.selectOnDown =
                    Neighbor(active, i, columns,
                        DialogueChoiceDirection.Down);
                active[i].navigation = navigation;
            }
        }

        private static Selectable Neighbor(
            IReadOnlyList<Button> active,
            int index,
            int columns,
            DialogueChoiceDirection direction)
        {
            int neighbor = DialogueChoiceNavigationPolicy.FindNeighbor(
                index,
                active.Count,
                columns,
                direction);
            return neighbor >= 0 ? active[neighbor] : null;
        }

        private static void FocusFirst(IReadOnlyList<Button> active)
        {
            if (active.Count == 0 || EventSystem.current == null)
                return;
            EventSystem.current.SetSelectedGameObject(
                active[0].gameObject);
        }

        private void ApplyVisual(
            Button button,
            float alpha,
            float scale)
        {
            foreach (Graphic graphic in
                     button.GetComponentsInChildren<Graphic>(true))
            {
                float baseAlpha = baseGraphicAlphas.TryGetValue(
                    graphic,
                    out float stored)
                        ? stored
                        : 1f;
                Color color = graphic.color;
                color.a = baseAlpha * alpha;
                graphic.color = color;
            }
            button.transform.localScale = Vector3.one * scale;
        }

        private void StopEntrance()
        {
            if (entranceRoutine == null)
                return;
            StopCoroutine(entranceRoutine);
            entranceRoutine = null;
        }

        private void OnDisable()
        {
            StopEntrance();
            if (buttons == null)
                return;
            foreach (Button button in buttons)
            {
                if (button != null)
                    ApplyVisual(button, 1f, 1f);
            }
        }
    }
}
