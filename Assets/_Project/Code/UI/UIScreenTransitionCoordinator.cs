using System;
using System.Collections;
using UnityEngine;

namespace Wake.UI
{
    [DisallowMultipleComponent]
    public sealed class UIScreenTransitionCoordinator : MonoBehaviour
    {
        [SerializeField] private UiTransitionProfile defaultProfile;

        private RectTransform canvas;
        private ScreenCoverPresenter cover;
        private UiTransitionProfile runtimeDefault;
        private Coroutine transition;
        private bool reducedMotion;

        public bool IsTransitioning => transition != null;

        private void OnEnable()
        {
            reducedMotion = ReducedMotionSettings.Enabled;
            ReducedMotionSettings.Changed += SetReducedMotion;
        }

        public void Configure(RectTransform canvasRoot)
        {
            canvas = canvasRoot;
            if (canvas == null)
                return;
            cover = canvas.GetComponent<ScreenCoverPresenter>() ??
                    canvas.gameObject.AddComponent<ScreenCoverPresenter>();
        }

        public void SetReducedMotion(bool value)
        {
            reducedMotion = value;
        }

        public bool Run(
            GameObject outgoing,
            GameObject incoming,
            Action swap,
            Action completed = null,
            UiTransitionProfile profile = null)
        {
            if (transition != null ||
                (outgoing == null && incoming == null))
                return false;

            if (!Application.isPlaying)
            {
                swap?.Invoke();
                completed?.Invoke();
                return true;
            }

            UiTransitionProfile selected =
                profile != null ? profile : ResolveDefaultProfile();
            transition = StartCoroutine(
                TransitionRoutine(
                    outgoing,
                    incoming,
                    swap,
                    completed,
                    selected));
            return true;
        }

        private IEnumerator TransitionRoutine(
            GameObject outgoing,
            GameObject incoming,
            Action swap,
            Action completed,
            UiTransitionProfile profile)
        {
            if (cover != null)
                cover.SetInputBlocked(true);

            if (outgoing != null &&
                outgoing.activeInHierarchy &&
                outgoing != incoming)
            {
                UiPanelTransitionAnimator animator =
                    EnsureAnimator(outgoing);
                animator.SetReducedMotion(reducedMotion);
                bool finished = false;
                animator.PlayOut(profile, () => finished = true);
                while (!finished &&
                       outgoing != null &&
                       animator != null &&
                       animator.IsPlaying)
                    yield return null;
            }

            if (!reducedMotion &&
                cover != null &&
                profile != null &&
                profile.Cover == UiTransitionCover.Fade)
            {
                yield return cover.FadeTo(
                    1f,
                    profile.CoverDuration,
                    profile.CoverColor);
            }

            try
            {
                swap?.Invoke();
            }
            catch (Exception exception)
            {
                Debug.LogException(exception);
            }

            yield return null;

            if (!reducedMotion &&
                cover != null &&
                profile != null &&
                profile.Cover == UiTransitionCover.Fade)
            {
                yield return cover.FadeTo(
                    0f,
                    profile.CoverDuration,
                    profile.CoverColor);
            }

            if (incoming != null && incoming.activeInHierarchy)
            {
                UiPanelTransitionAnimator animator =
                    EnsureAnimator(incoming);
                animator.SetReducedMotion(reducedMotion);
                bool finished = false;
                animator.PlayIn(profile, () => finished = true);
                while (!finished &&
                       incoming != null &&
                       animator != null &&
                       animator.IsPlaying)
                    yield return null;
            }

            if (cover != null)
                cover.ResetCover();
            transition = null;
            completed?.Invoke();
        }

        private UiTransitionProfile ResolveDefaultProfile()
        {
            if (defaultProfile != null)
                return defaultProfile;
            runtimeDefault ??= UiTransitionProfile.CreateRuntimeDefault();
            return runtimeDefault;
        }

        private static UiPanelTransitionAnimator EnsureAnimator(
            GameObject panel)
        {
            UiPanelTransitionAnimator animator =
                panel.GetComponent<UiPanelTransitionAnimator>();
            return animator != null
                ? animator
                : panel.AddComponent<UiPanelTransitionAnimator>();
        }

        private void OnDisable()
        {
            ReducedMotionSettings.Changed -= SetReducedMotion;
            if (transition != null)
            {
                StopCoroutine(transition);
                transition = null;
            }
            if (cover != null)
                cover.ResetCover();
        }
    }
}
