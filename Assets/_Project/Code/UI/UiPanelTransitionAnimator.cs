using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace Wake.UI
{
    [DisallowMultipleComponent]
    public class UiPanelTransitionAnimator : MonoBehaviour
    {
        private sealed class ElementPose
        {
            public RectTransform Rect;
            public CanvasGroup Group;
            public Vector2 Position;
            public Vector3 Scale;
            public float Alpha;
            public UiTransitionDirection Direction;
            public int Order;
            public float DistanceMultiplier;
        }

        [SerializeField] private UiTransitionProfile profile;
        [SerializeField] private bool playInOnEnable = true;
        [SerializeField] private bool excludeDialoguePanel;

        private readonly List<ElementPose> poses = new();
        private Coroutine animationRoutine;
        private bool reducedMotion;

        public bool IsPlaying => animationRoutine != null;
        public bool ExcludeDialoguePanel
        {
            get => excludeDialoguePanel;
            set => excludeDialoguePanel = value;
        }

        protected virtual void OnEnable()
        {
            if (Application.isPlaying && playInOnEnable)
            {
                animationRoutine = StartCoroutine(PlayInNextFrame());
            }
        }

        public void CaptureRestPose()
        {
            RestoreRestPose();
            poses.Clear();
            int visibleIndex = 0;
            foreach (Transform child in transform)
            {
                if (!child.gameObject.activeInHierarchy ||
                    IsBackground(child) ||
                    (excludeDialoguePanel && IsDialogue(child)) ||
                    child is not RectTransform rect)
                {
                    continue;
                }

                TransitionElementTag tag =
                    child.GetComponent<TransitionElementTag>();
                if (tag != null && tag.Exclude)
                    continue;

                CanvasGroup group = child.GetComponent<CanvasGroup>();
                if (group == null)
                    group = child.gameObject.AddComponent<CanvasGroup>();
                if (group == null)
                    continue;
                poses.Add(new ElementPose
                {
                    Rect = rect,
                    Group = group,
                    Position = rect.anchoredPosition,
                    Scale = rect.localScale,
                    Alpha = group.alpha,
                    Direction = tag != null
                        ? tag.Direction
                        : UiTransitionDirection.Auto,
                    Order = tag != null ? tag.Order : visibleIndex,
                    DistanceMultiplier =
                        tag != null ? tag.DistanceMultiplier : 1f
                });
                visibleIndex++;
            }
            poses.Sort((left, right) => left.Order.CompareTo(right.Order));
        }

        public void RestoreRestPose()
        {
            foreach (ElementPose pose in poses)
            {
                if (pose.Rect != null)
                {
                    pose.Rect.anchoredPosition = pose.Position;
                    pose.Rect.localScale = pose.Scale;
                }
                if (pose.Group != null)
                    pose.Group.alpha = pose.Alpha;
            }
        }

        public void SetReducedMotion(bool value)
        {
            reducedMotion = value;
        }

        public void PlayOut(
            UiTransitionProfile overrideProfile = null,
            Action completed = null)
        {
            StartAnimation(false, overrideProfile, completed);
        }

        public void PlayIn(
            UiTransitionProfile overrideProfile = null,
            Action completed = null)
        {
            StartAnimation(true, overrideProfile, completed);
        }

        private IEnumerator PlayInNextFrame()
        {
            yield return null;
            animationRoutine = null;
            if (isActiveAndEnabled)
                PlayIn();
        }

        private void StartAnimation(
            bool entering,
            UiTransitionProfile overrideProfile,
            Action completed)
        {
            StopAnimation();
            if (!isActiveAndEnabled || !Application.isPlaying)
            {
                RestoreRestPose();
                completed?.Invoke();
                return;
            }

            CaptureRestPose();
            if (poses.Count == 0)
            {
                completed?.Invoke();
                return;
            }

            UiTransitionProfile selected =
                overrideProfile != null ? overrideProfile : profile;
            animationRoutine = StartCoroutine(
                Animate(entering, selected, completed));
        }

        private IEnumerator Animate(
            bool entering,
            UiTransitionProfile selected,
            Action completed)
        {
            float duration = reducedMotion
                ? .18f
                : entering
                    ? selected != null ? selected.InDuration : .34f
                    : selected != null ? selected.OutDuration : .24f;
            float stagger = reducedMotion
                ? 0f
                : selected != null ? selected.Stagger : .03f;

            for (int index = 0; index < poses.Count; index++)
            {
                ElementPose pose = poses[index];
                Vector2 offset = reducedMotion
                    ? Vector2.zero
                    : ResolveOffset(pose, selected, index);
                if (pose.Rect != null)
                {
                    pose.Rect.anchoredPosition =
                        entering ? pose.Position + offset : pose.Position;
                    pose.Rect.localScale =
                        entering && IsScaleTransition(pose, selected)
                            ? pose.Scale * .96f
                            : pose.Scale;
                }
                if (pose.Group != null)
                    pose.Group.alpha = entering ? 0f : pose.Alpha;
            }

            float totalDuration =
                duration + stagger * Mathf.Max(0, poses.Count - 1);
            float elapsed = 0f;
            while (elapsed < totalDuration)
            {
                elapsed += Time.unscaledDeltaTime;
                for (int index = 0; index < poses.Count; index++)
                {
                    ElementPose pose = poses[index];
                    if (pose.Rect == null || pose.Group == null)
                        continue;

                    float local = Mathf.Clamp01(
                        (elapsed - stagger * index) /
                        Mathf.Max(.0001f, duration));
                    float eased = entering
                        ? EaseOutQuint(local)
                        : EaseInCubic(local);
                    Vector2 offset = reducedMotion
                        ? Vector2.zero
                        : ResolveOffset(pose, selected, index);
                    pose.Rect.anchoredPosition = entering
                        ? Vector2.LerpUnclamped(
                            pose.Position + offset,
                            pose.Position,
                            eased)
                        : Vector2.LerpUnclamped(
                            pose.Position,
                            pose.Position + offset,
                            eased);
                    if (IsScaleTransition(pose, selected))
                    {
                        pose.Rect.localScale = entering
                            ? Vector3.LerpUnclamped(
                                pose.Scale * .96f,
                                pose.Scale,
                                eased)
                            : Vector3.LerpUnclamped(
                                pose.Scale,
                                pose.Scale * .96f,
                                eased);
                    }
                    pose.Group.alpha = entering
                        ? Mathf.Lerp(0f, pose.Alpha, eased)
                        : Mathf.Lerp(pose.Alpha, 0f, eased);
                }
                yield return null;
            }

            if (entering)
                RestoreRestPose();
            animationRoutine = null;
            completed?.Invoke();
        }

        private Vector2 ResolveOffset(
            ElementPose pose,
            UiTransitionProfile selected,
            int index)
        {
            UiTransitionDirection direction = pose.Direction;
            if (direction == UiTransitionDirection.Auto)
            {
                direction = selected != null
                    ? selected.DefaultDirection
                    : UiTransitionDirection.Auto;
            }
            if (direction == UiTransitionDirection.Auto)
            {
                direction = index % 2 == 0
                    ? UiTransitionDirection.Left
                    : UiTransitionDirection.Right;
            }

            RectTransform panel = transform as RectTransform;
            float panelWidth = panel != null ? panel.rect.width : Screen.width;
            float panelHeight = panel != null ? panel.rect.height : Screen.height;
            float minimum = selected != null
                ? selected.MinimumTravel
                : 72f;
            float horizontal = Mathf.Max(
                minimum,
                panelWidth * .5f + pose.Rect.rect.width * .5f);
            float vertical = Mathf.Max(
                minimum,
                panelHeight * .5f + pose.Rect.rect.height * .5f);
            float multiplier = pose.DistanceMultiplier;

            return direction switch
            {
                UiTransitionDirection.Left =>
                    Vector2.left * horizontal * multiplier,
                UiTransitionDirection.Right =>
                    Vector2.right * horizontal * multiplier,
                UiTransitionDirection.Up =>
                    Vector2.up * vertical * multiplier,
                UiTransitionDirection.Down =>
                    Vector2.down * vertical * multiplier,
                UiTransitionDirection.Scale => Vector2.zero,
                _ => Vector2.zero
            };
        }

        private static bool IsScaleTransition(
            ElementPose pose,
            UiTransitionProfile selected)
        {
            if (pose.Direction == UiTransitionDirection.Scale)
                return true;
            return pose.Direction == UiTransitionDirection.Auto &&
                   selected != null &&
                   selected.DefaultDirection ==
                   UiTransitionDirection.Scale;
        }

        private static bool IsBackground(Transform target)
        {
            string value = target.name.ToLowerInvariant();
            return value.Contains("background") ||
                   value == "image" ||
                   value.Contains("backdrop") ||
                   value.Contains("title presentation");
        }

        private static bool IsDialogue(Transform target)
        {
            string value = target.name.ToLowerInvariant();
            return value.Contains("line panel") ||
                   value.Contains("dialogue");
        }

        private static float EaseInCubic(float value) =>
            value * value * value;

        private static float EaseOutQuint(float value) =>
            1f - Mathf.Pow(1f - value, 5f);

        private void StopAnimation()
        {
            if (animationRoutine == null)
                return;
            StopCoroutine(animationRoutine);
            animationRoutine = null;
        }

        protected virtual void OnDisable()
        {
            StopAnimation();
            RestoreRestPose();
        }
    }
}
