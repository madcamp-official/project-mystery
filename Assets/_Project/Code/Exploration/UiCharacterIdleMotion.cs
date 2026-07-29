using System;
using UnityEngine;
using UnityEngine.UI;

namespace Wake.Exploration
{
    [Serializable]
    public struct UiCharacterIdleMotionSettings
    {
        [SerializeField]
        private float breathingPositionAmplitude;
        [SerializeField]
        private float breathingScaleAmplitude;
        [SerializeField]
        private float breathingCycleSeconds;
        [SerializeField]
        private float swayDegrees;
        [SerializeField]
        private float swayCycleSeconds;
        [SerializeField]
        private float startupBlendSeconds;

        public UiCharacterIdleMotionSettings(
            float breathingPositionAmplitude,
            float breathingScaleAmplitude,
            float breathingCycleSeconds,
            float swayDegrees,
            float swayCycleSeconds,
            float startupBlendSeconds)
        {
            this.breathingPositionAmplitude =
                breathingPositionAmplitude;
            this.breathingScaleAmplitude = breathingScaleAmplitude;
            this.breathingCycleSeconds = breathingCycleSeconds;
            this.swayDegrees = swayDegrees;
            this.swayCycleSeconds = swayCycleSeconds;
            this.startupBlendSeconds = startupBlendSeconds;
        }

        public static UiCharacterIdleMotionSettings Default =>
            new(
                breathingPositionAmplitude: 1.5f,
                breathingScaleAmplitude: 0.006f,
                breathingCycleSeconds: 3.6f,
                swayDegrees: 0.65f,
                swayCycleSeconds: 4.8f,
                startupBlendSeconds: 0.35f);

        public float BreathingPositionAmplitude =>
            breathingPositionAmplitude;
        public float BreathingScaleAmplitude =>
            breathingScaleAmplitude;
        public float BreathingCycleSeconds => breathingCycleSeconds;
        public float SwayDegrees => swayDegrees;
        public float SwayCycleSeconds => swayCycleSeconds;
        public float StartupBlendSeconds => startupBlendSeconds;
    }

    public readonly struct UiCharacterIdleMotionSample
    {
        public UiCharacterIdleMotionSample(
            Vector2 anchoredPositionOffset,
            Vector2 scaleMultiplier,
            float rotationDegrees)
        {
            AnchoredPositionOffset = anchoredPositionOffset;
            ScaleMultiplier = scaleMultiplier;
            RotationDegrees = rotationDegrees;
        }

        public Vector2 AnchoredPositionOffset { get; }
        public Vector2 ScaleMultiplier { get; }
        public float RotationDegrees { get; }
    }

    // Pure motion math shared by runtime playback and EditMode tests. It
    // only operates on value types, does not touch UnityEngine.Random, and
    // does not allocate.
    public static class UiCharacterIdleMotionEvaluator
    {
        public const float MinimumScaleYMultiplier = 0.98f;
        public const float MaximumScaleYMultiplier = 1.02f;

        private const float TwoPi = Mathf.PI * 2f;
        private const float MaximumPositionAmplitude = 8f;
        private const float MaximumBreathingScaleAmplitude = 0.02f;
        private const float MaximumSwayDegrees = 2f;
        private const float MinimumCycleSeconds = 0.5f;

        public static UiCharacterIdleMotionSample Evaluate(
            int seed,
            float timeSeconds)
        {
            return Evaluate(
                seed,
                timeSeconds,
                UiCharacterIdleMotionSettings.Default);
        }

        public static UiCharacterIdleMotionSample Evaluate(
            int seed,
            float timeSeconds,
            UiCharacterIdleMotionSettings settings)
        {
            float time = IsFinite(timeSeconds)
                ? Mathf.Max(0f, timeSeconds)
                : 0f;

            float startupSeconds = Mathf.Clamp(
                settings.StartupBlendSeconds,
                0f,
                2f);
            float startupBlend = startupSeconds <= Mathf.Epsilon
                ? 1f
                : SmoothStep01(time / startupSeconds);

            float breathingCycle = Mathf.Max(
                MinimumCycleSeconds,
                settings.BreathingCycleSeconds);
            breathingCycle *= Mathf.Lerp(
                0.88f,
                1.12f,
                Hash01(seed, 0));
            float breathingPhase = Hash01(seed, 1) * TwoPi;
            float breathingWave = Mathf.Sin(
                time * TwoPi / breathingCycle + breathingPhase);
            float breathingStrength = Mathf.Lerp(
                0.78f,
                1f,
                Hash01(seed, 2));
            float positionAmplitude = Mathf.Clamp(
                settings.BreathingPositionAmplitude,
                0f,
                MaximumPositionAmplitude);
            float positionY =
                breathingWave *
                breathingStrength *
                positionAmplitude *
                startupBlend;

            float scaleAmplitude = Mathf.Clamp(
                settings.BreathingScaleAmplitude,
                0f,
                MaximumBreathingScaleAmplitude);
            float breathingScaleY =
                1f +
                breathingWave *
                breathingStrength *
                scaleAmplitude *
                startupBlend;

            float swayCycle = Mathf.Max(
                MinimumCycleSeconds,
                settings.SwayCycleSeconds);
            swayCycle *= Mathf.Lerp(
                0.9f,
                1.1f,
                Hash01(seed, 3));
            float swayPhase = Hash01(seed, 4) * TwoPi;
            float swayStrength = Mathf.Lerp(
                0.76f,
                1f,
                Hash01(seed, 5));
            float swayAmplitude = Mathf.Clamp(
                settings.SwayDegrees,
                0f,
                MaximumSwayDegrees);
            float rotation = Mathf.Sin(
                    time * TwoPi / swayCycle + swayPhase) *
                swayStrength *
                swayAmplitude *
                startupBlend;

            float scaleY = Mathf.Clamp(
                breathingScaleY,
                MinimumScaleYMultiplier,
                MaximumScaleYMultiplier);

            return new UiCharacterIdleMotionSample(
                new Vector2(0f, positionY),
                new Vector2(1f, scaleY),
                rotation);
        }

        private static float SmoothStep01(float value)
        {
            float t = Mathf.Clamp01(value);
            return t * t * (3f - 2f * t);
        }

        private static bool IsFinite(float value)
        {
            return !float.IsNaN(value) && !float.IsInfinity(value);
        }

        private static float Hash01(int seed, int channel)
        {
            unchecked
            {
                int hash = seed * 374761393 + channel * 668265263;
                hash = (hash ^ (hash >> 13)) * 1274126177;
                hash ^= hash >> 16;
                return (hash & 0x7FFFFFFF) /
                    (float)int.MaxValue;
            }
        }
    }

    [DisallowMultipleComponent]
    [RequireComponent(typeof(RectTransform))]
    public sealed class UiCharacterIdleMotion : MonoBehaviour
    {
        [SerializeField]
        private Graphic targetGraphic;
        [SerializeField]
        private int seed;
        [SerializeField]
        private bool useUnscaledTime = true;
        [SerializeField]
        private UiCharacterIdleMotionSettings settings =
            UiCharacterIdleMotionSettings.Default;

        private RectTransform targetRect;
        private Vector2 authoredAnchoredPosition;
        private Vector3 authoredLocalScale;
        private Quaternion authoredLocalRotation;
        private Color authoredGraphicColor;
        private Vector2 lastAppliedAnchoredPosition;
        private Vector3 lastAppliedLocalScale;
        private Quaternion lastAppliedLocalRotation;
        private float elapsedTime;
        private bool hasAuthoredState;
        private bool hasAppliedTransformSample;

        public int Seed
        {
            get => seed;
            set => seed = value;
        }

        public bool UseUnscaledTime
        {
            get => useUnscaledTime;
            set => useUnscaledTime = value;
        }

        public float ElapsedTime => elapsedTime;

        public UiCharacterIdleMotionSettings Settings
        {
            get => settings;
            set => settings = value;
        }

        public Graphic TargetGraphic => targetGraphic;

        public void Configure(
            int deterministicSeed,
            Graphic graphic = null,
            bool unscaledTime = true)
        {
            RestoreAuthoredState();
            ResolveReferences();
            seed = deterministicSeed;
            useUnscaledTime = unscaledTime;
            targetGraphic = graphic != null
                ? graphic
                : GetComponent<Graphic>();
            CaptureCurrentAsAuthoredState();
        }

        public void Restart()
        {
            elapsedTime = 0f;
            if (hasAuthoredState)
                ApplyCurrentSample();
        }

        public void StopAndRestore()
        {
            enabled = false;
            RestoreAuthoredState();
        }

        // This hook makes the time-source choice directly testable and also
        // allows a parent presenter to drive several characters in lockstep.
        // Runtime Update supplies Time.deltaTime and Time.unscaledDeltaTime.
        public void Advance(
            float scaledDeltaTime,
            float unscaledDeltaTime)
        {
            if (!hasAuthoredState)
                CaptureCurrentAsAuthoredState();

            float delta = useUnscaledTime
                ? unscaledDeltaTime
                : scaledDeltaTime;
            if (!IsFinite(delta) || delta <= 0f)
                return;

            elapsedTime += delta;
            ApplyCurrentSample();
        }

        public void ApplyAtTime(float timeSeconds)
        {
            if (!hasAuthoredState)
                CaptureCurrentAsAuthoredState();

            elapsedTime = IsFinite(timeSeconds)
                ? Mathf.Max(0f, timeSeconds)
                : 0f;
            ApplyCurrentSample();
        }

        // Call after an owning layout/presentation system has assigned all
        // authored values. The captured state becomes the exact restore
        // target for the next disable or destruction.
        public void CaptureCurrentAsAuthoredState()
        {
            ResolveReferences();
            if (targetRect == null)
                return;

            authoredAnchoredPosition = targetRect.anchoredPosition;
            authoredLocalScale = targetRect.localScale;
            authoredLocalRotation = targetRect.localRotation;
            if (targetGraphic != null)
                authoredGraphicColor = targetGraphic.color;

            elapsedTime = 0f;
            hasAuthoredState = true;
            hasAppliedTransformSample = false;
        }

        // Rebase is safe to call after a layout/tint pass, even while a
        // motion sample is active. Values overwritten by that pass become
        // the new authored baseline; values still equal to the last sample
        // are first restored so an animated scale/rotation is never
        // captured and applied a second time.
        public void CaptureAuthoredLayout()
        {
            ResolveReferences();
            if (targetRect == null)
                return;
            if (!hasAuthoredState)
            {
                CaptureCurrentAsAuthoredState();
                return;
            }

            Vector2 livePosition = targetRect.anchoredPosition;
            Vector3 liveScale = targetRect.localScale;
            Quaternion liveRotation = targetRect.localRotation;
            Color liveColor = targetGraphic != null
                ? targetGraphic.color
                : authoredGraphicColor;
            float retainedTime = elapsedTime;

            bool positionWasReauthored =
                !hasAppliedTransformSample ||
                !Approximately(
                    livePosition,
                    lastAppliedAnchoredPosition);
            bool scaleWasReauthored =
                !hasAppliedTransformSample ||
                !Approximately(
                    liveScale,
                    lastAppliedLocalScale);
            bool rotationWasReauthored =
                !hasAppliedTransformSample ||
                !Approximately(
                    liveRotation,
                    lastAppliedLocalRotation);
            Vector2 previousPosition = authoredAnchoredPosition;
            Vector3 previousScale = authoredLocalScale;
            Quaternion previousRotation = authoredLocalRotation;
            RestoreAuthoredState(resetElapsedTime: false);

            authoredAnchoredPosition = positionWasReauthored
                ? livePosition
                : previousPosition;
            authoredLocalScale = scaleWasReauthored
                ? liveScale
                : previousScale;
            authoredLocalRotation = rotationWasReauthored
                ? liveRotation
                : previousRotation;
            authoredGraphicColor = liveColor;
            targetRect.anchoredPosition =
                authoredAnchoredPosition;
            targetRect.localScale = authoredLocalScale;
            targetRect.localRotation = authoredLocalRotation;
            if (targetGraphic != null)
                targetGraphic.color = authoredGraphicColor;

            elapsedTime = retainedTime;
            hasAuthoredState = true;
            ApplyCurrentSample();
        }

        public void Rebase()
        {
            CaptureAuthoredLayout();
        }

        // Presentation systems can update completion/lighting tint without
        // recapturing a transform that currently contains motion.
        public void SetAuthoredGraphicColor(Color color)
        {
            ResolveReferences();
            if (targetGraphic == null)
                return;
            if (!hasAuthoredState)
                CaptureCurrentAsAuthoredState();

            authoredGraphicColor = color;
            targetGraphic.color = color;
        }

        private void Awake()
        {
            ResolveReferences();
        }

        private void OnEnable()
        {
            ResolveReferences();
            CaptureCurrentAsAuthoredState();
        }

        private void Update()
        {
            Advance(Time.deltaTime, Time.unscaledDeltaTime);
        }

        private void OnDisable()
        {
            RestoreAuthoredState();
        }

        private void OnDestroy()
        {
            RestoreAuthoredState();
        }

        private void ResolveReferences()
        {
            if (targetRect == null)
                targetRect = GetComponent<RectTransform>();
            if (targetGraphic == null)
                targetGraphic = GetComponent<Graphic>();
        }

        private void ApplyCurrentSample()
        {
            if (targetRect == null)
                return;

            UiCharacterIdleMotionSample sample =
                UiCharacterIdleMotionEvaluator.Evaluate(
                    seed,
                    elapsedTime,
                    settings);
            ApplyTransformSample(sample);
        }

        private void ApplyTransformSample(
            UiCharacterIdleMotionSample sample)
        {
            targetRect.anchoredPosition =
                authoredAnchoredPosition +
                sample.AnchoredPositionOffset;
            targetRect.localScale = new Vector3(
                authoredLocalScale.x * sample.ScaleMultiplier.x,
                authoredLocalScale.y * sample.ScaleMultiplier.y,
                authoredLocalScale.z);
            targetRect.localRotation =
                authoredLocalRotation *
                Quaternion.Euler(
                    0f,
                    0f,
                    sample.RotationDegrees);
            lastAppliedAnchoredPosition =
                targetRect.anchoredPosition;
            lastAppliedLocalScale = targetRect.localScale;
            lastAppliedLocalRotation = targetRect.localRotation;
            hasAppliedTransformSample = true;
        }

        private void RestoreAuthoredState(
            bool resetElapsedTime = true)
        {
            if (!hasAuthoredState)
                return;

            if (targetRect != null)
            {
                targetRect.anchoredPosition =
                    authoredAnchoredPosition;
                targetRect.localScale = authoredLocalScale;
                targetRect.localRotation = authoredLocalRotation;
            }

            if (targetGraphic != null)
                targetGraphic.color = authoredGraphicColor;

            if (resetElapsedTime)
                elapsedTime = 0f;
            hasAuthoredState = false;
            hasAppliedTransformSample = false;
        }

        private static bool Approximately(
            Vector2 first,
            Vector2 second)
        {
            return (first - second).sqrMagnitude <= 0.000001f;
        }

        private static bool Approximately(
            Vector3 first,
            Vector3 second)
        {
            return (first - second).sqrMagnitude <= 0.000001f;
        }

        private static bool Approximately(
            Quaternion first,
            Quaternion second)
        {
            return Mathf.Abs(Quaternion.Dot(first, second)) >=
                0.999999f;
        }

        private static bool IsFinite(float value)
        {
            return !float.IsNaN(value) && !float.IsInfinity(value);
        }
    }
}
