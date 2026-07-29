using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

namespace Wake.Exploration
{
    public readonly struct LocationBackgroundElementState
    {
        public LocationBackgroundElementState(
            Vector2 normalizedPosition,
            float alphaMultiplier,
            float scaleMultiplier,
            float rotationDegrees)
        {
            NormalizedPosition = normalizedPosition;
            AlphaMultiplier = Mathf.Clamp01(alphaMultiplier);
            ScaleMultiplier = Mathf.Max(0f, scaleMultiplier);
            RotationDegrees = rotationDegrees;
        }

        public Vector2 NormalizedPosition { get; }
        public float AlphaMultiplier { get; }
        public float ScaleMultiplier { get; }
        public float RotationDegrees { get; }
    }

    public readonly struct LocationBackgroundMotionState
    {
        public LocationBackgroundMotionState(
            Vector2 normalizedOffset,
            float rotationDegrees,
            float scaleMultiplier)
        {
            NormalizedOffset = normalizedOffset;
            RotationDegrees = rotationDegrees;
            ScaleMultiplier = Mathf.Max(1f, scaleMultiplier);
        }

        public Vector2 NormalizedOffset { get; }
        public float RotationDegrees { get; }
        public float ScaleMultiplier { get; }
    }

    /// <summary>
    /// Pure, deterministic motion math for the location animation overlay.
    /// Every returned position uses full-background normalized coordinates.
    /// </summary>
    public static class LocationBackgroundAnimationEvaluator
    {
        private const float TwoPi = Mathf.PI * 2f;

        public static LocationBackgroundElementState EvaluateElement(
            LocationBackgroundEffectSpec effect,
            int elementIndex,
            float timeSeconds)
        {
            int index = Mathf.Max(0, elementIndex);
            float time = IsFinite(timeSeconds)
                ? Mathf.Max(0f, timeSeconds)
                : 0f;
            Vector2 position = GridPosition(effect, index);
            float alpha = 0f;
            float scale = 1f;
            float rotation = 0f;
            float phase = Hash01(effect.Seed, index * 17) * TwoPi;

            switch (effect.Type)
            {
                case LocationBackgroundEffectType.RadialLightPulse:
                {
                    float wave = Sine01(
                        time * effect.FrequencyHz * TwoPi + phase);
                    alpha = effect.Intensity * Mathf.Lerp(.48f, 1f, wave);
                    scale = Mathf.Lerp(.96f, 1.045f, wave);
                    break;
                }
                case LocationBackgroundEffectType.RectangularScreenPulse:
                {
                    float primary = Sine01(
                        time * effect.FrequencyHz * TwoPi + phase);
                    float secondary = Sine01(
                        time * effect.FrequencyHz * 2.37f * TwoPi -
                        phase * .43f);
                    float wave = Mathf.Clamp01(
                        primary * .72f + secondary * .28f);
                    alpha = effect.Intensity * Mathf.Lerp(.42f, 1f, wave);
                    scale = Mathf.Lerp(.992f, 1.008f, wave);
                    break;
                }
                case LocationBackgroundEffectType.LinearSweep:
                {
                    float cycle = Mathf.Repeat(
                        time / effect.DurationSeconds +
                        index /
                        (float)Mathf.Max(1, effect.MaxElementCount) +
                        Hash01(effect.Seed, index * 17 + 1) * .2f,
                        1f);
                    position = effect.NormalizedAnchor +
                        effect.Direction *
                        Mathf.Lerp(
                            -effect.NormalizedTravel * .5f,
                            effect.NormalizedTravel * .5f,
                            cycle);
                    position = ClampToRect(
                        position,
                        effect.NormalizedRect);
                    float envelope = Mathf.Sin(cycle * Mathf.PI);
                    alpha = effect.Intensity * envelope * envelope;
                    rotation = Mathf.Atan2(
                        effect.Direction.y,
                        effect.Direction.x) * Mathf.Rad2Deg;
                    break;
                }
                case LocationBackgroundEffectType.DriftingMotes:
                case LocationBackgroundEffectType.DriftingSteam:
                {
                    position = EvaluateDriftPosition(
                        effect,
                        index,
                        time);
                    float twinkle = Sine01(
                        time * effect.FrequencyHz * TwoPi + phase);
                    alpha = effect.Intensity *
                        (effect.Type ==
                         LocationBackgroundEffectType.DriftingSteam
                            ? Mathf.Lerp(.28f, .82f, twinkle)
                            : Mathf.Lerp(.36f, 1f, twinkle));
                    scale = effect.Type ==
                            LocationBackgroundEffectType.DriftingSteam
                        ? Mathf.Lerp(.82f, 1.22f, twinkle)
                        : Mathf.Lerp(.8f, 1.15f, twinkle);
                    rotation = Mathf.Sin(
                        time * effect.FrequencyHz * 1.7f * TwoPi +
                        phase) * 12f;
                    break;
                }
                case LocationBackgroundEffectType.OccasionalFlicker:
                {
                    float envelope = EvaluateEventEnvelope(
                        effect,
                        index,
                        time,
                        out _);
                    alpha = effect.Intensity * envelope;
                    scale = Mathf.Lerp(1.04f, .96f, envelope);
                    break;
                }
                case LocationBackgroundEffectType.OccasionalSpark:
                {
                    float envelope = EvaluateEventEnvelope(
                        effect,
                        index,
                        time,
                        out float eventProgress);
                    Vector2 start = RandomPositionInRect(
                        effect,
                        index,
                        41);
                    position = ClampToRect(
                        start +
                        effect.Direction *
                        effect.NormalizedTravel *
                        eventProgress,
                        effect.NormalizedRect);
                    alpha = effect.Intensity * envelope;
                    scale = Mathf.Lerp(.65f, 1.3f, envelope);
                    rotation = Mathf.Atan2(
                        effect.Direction.y,
                        effect.Direction.x) * Mathf.Rad2Deg -
                        90f;
                    break;
                }
            }

            return new LocationBackgroundElementState(
                position,
                alpha,
                scale,
                rotation);
        }

        public static LocationBackgroundMotionState EvaluateMotion(
            LocationBackgroundEffectSpec effect,
            float timeSeconds)
        {
            float time = IsFinite(timeSeconds)
                ? Mathf.Max(0f, timeSeconds)
                : 0f;
            if (effect.Type ==
                LocationBackgroundEffectType.FullBackgroundDrift)
            {
                float phase = Hash01(effect.Seed, 73) * TwoPi;
                float wave = Mathf.Sin(
                    time * effect.FrequencyHz * TwoPi + phase);
                Vector2 offset =
                    effect.Direction *
                    effect.NormalizedTravel *
                    wave;
                float rotation =
                    wave * effect.Intensity * .55f;
                float overscan =
                    1f + effect.NormalizedTravel * 2.5f;
                return new LocationBackgroundMotionState(
                    offset,
                    rotation,
                    overscan);
            }

            if (effect.Type ==
                LocationBackgroundEffectType.FullBackgroundShake)
            {
                float envelope = EvaluateEventEnvelope(
                    effect,
                    0,
                    time,
                    out _);
                float x = Mathf.Sin(
                    time * 67.3f + effect.Seed * .013f);
                float y = Mathf.Sin(
                    time * 91.7f + effect.Seed * .019f);
                Vector2 offset = new Vector2(x, y) *
                    effect.NormalizedTravel *
                    envelope;
                float rotation = Mathf.Sin(
                        time * 113.1f + effect.Seed * .007f) *
                    effect.Intensity *
                    envelope;
                float overscan = 1f +
                    effect.NormalizedTravel *
                    envelope *
                    2.5f;
                return new LocationBackgroundMotionState(
                    offset,
                    rotation,
                    overscan);
            }

            return new LocationBackgroundMotionState(
                Vector2.zero,
                0f,
                1f);
        }

        private static Vector2 EvaluateDriftPosition(
            LocationBackgroundEffectSpec effect,
            int index,
            float time)
        {
            Rect rect = effect.NormalizedRect;
            Vector2 start = RandomPositionInRect(
                effect,
                index,
                19);
            float speed =
                effect.NormalizedTravel /
                effect.DurationSeconds;
            Vector2 position =
                start + effect.Direction * speed * time;
            Vector2 perpendicular =
                new(-effect.Direction.y, effect.Direction.x);
            float sway = Mathf.Sin(
                    time * effect.FrequencyHz * TwoPi +
                    Hash01(effect.Seed, index * 17 + 23) * TwoPi) *
                Mathf.Min(rect.width, rect.height) *
                .045f;
            position += perpendicular * sway;
            return new Vector2(
                rect.xMin + Mathf.Repeat(
                    position.x - rect.xMin,
                    rect.width),
                rect.yMin + Mathf.Repeat(
                    position.y - rect.yMin,
                    rect.height));
        }

        private static float EvaluateEventEnvelope(
            LocationBackgroundEffectSpec effect,
            int index,
            float time,
            out float eventProgress)
        {
            float period = 1f / effect.FrequencyHz;
            int window = Mathf.FloorToInt(time / period);
            float localTime = time - window * period;
            float duration = Mathf.Min(
                effect.DurationSeconds,
                period * .85f);
            float availableStart = Mathf.Max(0f, period - duration);
            int channel = unchecked(
                index * 97 + window * 193 + 59);
            float start = Hash01(
                effect.Seed,
                channel) * availableStart;
            if (localTime < start ||
                localTime > start + duration)
            {
                eventProgress = 0f;
                return 0f;
            }

            eventProgress = Mathf.Clamp01(
                (localTime - start) /
                Mathf.Max(duration, .001f));
            float pulse = Mathf.Sin(eventProgress * Mathf.PI);
            return pulse * pulse;
        }

        private static Vector2 GridPosition(
            LocationBackgroundEffectSpec effect,
            int index)
        {
            if (effect.MaxElementCount <= 1)
                return effect.NormalizedAnchor;

            int columns = Mathf.CeilToInt(
                Mathf.Sqrt(effect.MaxElementCount));
            int rows = Mathf.CeilToInt(
                effect.MaxElementCount /
                (float)columns);
            int column = index % columns;
            int row = Mathf.Clamp(
                index / columns,
                0,
                rows - 1);
            float jitterX =
                (Hash01(effect.Seed, index * 17 + 3) - .5f) *
                .18f;
            float jitterY =
                (Hash01(effect.Seed, index * 17 + 5) - .5f) *
                .18f;
            Vector2 local = new(
                (column + .5f + jitterX) / columns,
                (row + .5f + jitterY) / rows);
            return effect.NormalizedRect.min +
                Vector2.Scale(
                    effect.NormalizedRect.size,
                    local);
        }

        private static Vector2 RandomPositionInRect(
            LocationBackgroundEffectSpec effect,
            int index,
            int channelOffset)
        {
            Vector2 local = new(
                Hash01(
                    effect.Seed,
                    index * 17 + channelOffset),
                Hash01(
                    effect.Seed,
                    index * 17 + channelOffset + 1));
            return effect.NormalizedRect.min +
                Vector2.Scale(
                    effect.NormalizedRect.size,
                    local);
        }

        private static Vector2 ClampToRect(
            Vector2 value,
            Rect rect)
        {
            return new Vector2(
                Mathf.Clamp(value.x, rect.xMin, rect.xMax),
                Mathf.Clamp(value.y, rect.yMin, rect.yMax));
        }

        private static float Sine01(float radians)
        {
            return (Mathf.Sin(radians) + 1f) * .5f;
        }

        private static float Hash01(int seed, int channel)
        {
            unchecked
            {
                int hash =
                    seed * 374761393 +
                    channel * 668265263;
                hash = (hash ^ (hash >> 13)) * 1274126177;
                hash ^= hash >> 16;
                return (hash & 0x7FFFFFFF) /
                    (float)int.MaxValue;
            }
        }

        private static bool IsFinite(float value)
        {
            return !float.IsNaN(value) &&
                   !float.IsInfinity(value);
        }
    }

    [DisallowMultipleComponent]
    public sealed class LocationBackgroundAnimationOverlay :
        MonoBehaviour
    {
        private sealed class ElementView
        {
            public LocationBackgroundEffectSpec Effect;
            public int Index;
            public RectTransform Rect;
            public Image Image;
            public Vector2 NormalizedSize;
        }

        private const int TextureSize = 64;

        private static Sprite radialGlowSprite;
        private static Sprite softRectangleSprite;
        private static Material additiveMaterial;

        private readonly List<ElementView> elements = new();
        private RectTransform contentRect;
        private RectTransform motionRect;
        private RectTransform overlayRoot;
        private LocationBackgroundAnimationProfile activeProfile;
        private Vector2 lastContentSize;
        private Vector2 authoredMotionPosition;
        private Vector3 authoredMotionScale = Vector3.one;
        private Quaternion authoredMotionRotation = Quaternion.identity;
        private float playbackTime;
        private bool paused;
        private bool reducedMotion;

        public string ActiveProfileId =>
            activeProfile?.Id ?? string.Empty;
        public int ActiveElementCount => elements.Count;
        public bool IsPaused => paused || reducedMotion;
        public float PlaybackTime => playbackTime;

        public Color ResolveAmbientParticleTint(Color fallback)
        {
            if (activeProfile == null)
                return fallback;

            foreach (LocationBackgroundEffectSpec effect in
                     activeProfile.Effects)
            {
                if (effect.Type !=
                    LocationBackgroundEffectType.DriftingMotes)
                {
                    continue;
                }

                return new Color(
                    effect.Color.r,
                    effect.Color.g,
                    effect.Color.b,
                    fallback.a);
            }

            return fallback;
        }

        public void Initialize(
            RectTransform backgroundContentRect,
            RectTransform backgroundMotionRect = null)
        {
            contentRect = backgroundContentRect;
            motionRect =
                backgroundMotionRect ?? backgroundContentRect;
            CaptureAuthoredMotion();
            EnsureSprites();
            EnsureMaterial();
            BuildRoot();
        }

        public void Show(string locationCode)
        {
            if (!LocationBackgroundAnimationCatalog.TryGet(
                    locationCode,
                    out LocationBackgroundAnimationProfile profile))
            {
                Hide();
                return;
            }

            if (activeProfile != null &&
                string.Equals(
                    activeProfile.Id,
                    profile.Id,
                    StringComparison.Ordinal))
            {
                if (overlayRoot != null)
                    overlayRoot.SetAsFirstSibling();
                return;
            }

            ClearElements();
            ResetMotion();
            activeProfile = profile;
            playbackTime = 0f;
            paused = false;
            if (overlayRoot == null)
                BuildRoot();
            if (overlayRoot == null)
                return;

            overlayRoot.gameObject.SetActive(true);
            overlayRoot.SetAsFirstSibling();
            foreach (LocationBackgroundEffectSpec effect in
                     profile.Effects)
            {
                if (IsBackgroundMotion(effect.Type))
                    continue;
                // The existing bloom-backed AmbientRoomParticleOverlay owns
                // room dust. This catalog entry supplies its per-location
                // tint; drawing another mote pool here would double density.
                if (effect.Type ==
                    LocationBackgroundEffectType.DriftingMotes)
                {
                    continue;
                }

                int count = Mathf.Clamp(
                    effect.MaxElementCount,
                    1,
                    32);
                for (int index = 0; index < count; index++)
                    CreateElement(effect, index);
            }

            lastContentSize = Vector2.zero;
            ApplyAtTime(0f);
        }

        public void Hide()
        {
            ClearElements();
            activeProfile = null;
            playbackTime = 0f;
            paused = false;
            if (overlayRoot != null)
                overlayRoot.gameObject.SetActive(false);
            ResetMotion();
        }

        public void SetPaused(bool value)
        {
            paused = value;
        }

        public void SetReducedMotion(bool value)
        {
            if (reducedMotion == value)
            {
                // Show() can rebuild/apply a new profile while reduced motion
                // is already enabled. Reassert the authored background pose
                // even when the policy value itself did not change.
                if (value)
                    ResetMotion();
                return;
            }

            reducedMotion = value;
            if (reducedMotion)
            {
                playbackTime = 0f;
                ApplyAtTime(0f);
                ResetMotion();
            }
        }

        public void Advance(float unscaledDeltaTime)
        {
            if (paused ||
                reducedMotion ||
                activeProfile == null ||
                !IsFinitePositive(unscaledDeltaTime))
            {
                return;
            }

            playbackTime += unscaledDeltaTime;
            ApplyAtTime(playbackTime);
        }

        public void ApplyAtTime(float timeSeconds)
        {
            if (activeProfile == null ||
                contentRect == null)
            {
                return;
            }

            playbackTime =
                float.IsNaN(timeSeconds) ||
                float.IsInfinity(timeSeconds)
                    ? 0f
                    : Mathf.Max(0f, timeSeconds);
            RefreshElementSizes();
            foreach (ElementView element in elements)
            {
                LocationBackgroundElementState state =
                    LocationBackgroundAnimationEvaluator.EvaluateElement(
                        element.Effect,
                        element.Index,
                        playbackTime);
                element.Rect.anchorMin =
                    element.Rect.anchorMax =
                        new Vector2(.5f, .5f);
                element.Rect.anchoredPosition = new Vector2(
                    (state.NormalizedPosition.x - .5f) *
                    lastContentSize.x,
                    (state.NormalizedPosition.y - .5f) *
                    lastContentSize.y);
                element.Rect.localScale =
                    Vector3.one * state.ScaleMultiplier;
                element.Rect.localRotation =
                    Quaternion.Euler(
                        0f,
                        0f,
                        state.RotationDegrees);
                Color color = element.Effect.Color;
                color.a *= state.AlphaMultiplier;
                element.Image.color = color;
            }

            ApplyBackgroundMotion();
        }

        private void LateUpdate()
        {
            Advance(Time.unscaledDeltaTime);
        }

        private void OnDisable()
        {
            ResetMotion();
        }

        private void OnDestroy()
        {
            ResetMotion();
        }

        private void BuildRoot()
        {
            if (contentRect == null ||
                overlayRoot != null)
            {
                return;
            }

            GameObject root = new(
                "Location Background Animation",
                typeof(RectTransform));
            root.transform.SetParent(contentRect, false);
            overlayRoot = root.GetComponent<RectTransform>();
            overlayRoot.anchorMin = Vector2.zero;
            overlayRoot.anchorMax = Vector2.one;
            overlayRoot.offsetMin = Vector2.zero;
            overlayRoot.offsetMax = Vector2.zero;
            overlayRoot.SetAsFirstSibling();
        }

        private void CreateElement(
            LocationBackgroundEffectSpec effect,
            int index)
        {
            GameObject target = new(
                $"{effect.Type}_{index}",
                typeof(RectTransform),
                typeof(CanvasRenderer),
                typeof(Image));
            target.transform.SetParent(overlayRoot, false);

            Image image = target.GetComponent<Image>();
            image.sprite = SelectSprite(effect.Type);
            image.raycastTarget = false;
            image.color = effect.Color;
            image.material = additiveMaterial;

            elements.Add(new ElementView
            {
                Effect = effect,
                Index = index,
                Rect = target.GetComponent<RectTransform>(),
                Image = image,
                NormalizedSize =
                    ResolveNormalizedSize(effect, index)
            });
        }

        private void RefreshElementSizes()
        {
            Vector2 size = contentRect.rect.size;
            if (size.x <= 0f || size.y <= 0f)
                return;
            if (size == lastContentSize)
                return;

            foreach (ElementView element in elements)
            {
                element.Rect.sizeDelta = new Vector2(
                    element.NormalizedSize.x * size.x,
                    element.NormalizedSize.y * size.y);
            }
            lastContentSize = size;
        }

        private void ApplyBackgroundMotion()
        {
            if (motionRect == null ||
                activeProfile == null ||
                reducedMotion)
            {
                if (reducedMotion)
                    ResetMotion();
                return;
            }

            Vector2 normalizedOffset = Vector2.zero;
            float rotation = 0f;
            float scale = 1f;
            foreach (LocationBackgroundEffectSpec effect in
                     activeProfile.Effects)
            {
                if (!IsBackgroundMotion(effect.Type))
                    continue;

                LocationBackgroundMotionState state =
                    LocationBackgroundAnimationEvaluator.EvaluateMotion(
                        effect,
                        playbackTime);
                normalizedOffset += state.NormalizedOffset;
                rotation += state.RotationDegrees;
                scale = Mathf.Max(
                    scale,
                    state.ScaleMultiplier);
            }

            Vector2 viewportSize =
                motionRect.parent is RectTransform parent
                    ? parent.rect.size
                    : lastContentSize;
            motionRect.anchoredPosition =
                authoredMotionPosition +
                Vector2.Scale(
                    normalizedOffset,
                    viewportSize);
            motionRect.localRotation =
                authoredMotionRotation *
                Quaternion.Euler(0f, 0f, rotation);
            motionRect.localScale = Vector3.Scale(
                authoredMotionScale,
                new Vector3(scale, scale, 1f));
        }

        private void CaptureAuthoredMotion()
        {
            if (motionRect == null)
                return;

            authoredMotionPosition =
                motionRect.anchoredPosition;
            authoredMotionScale =
                motionRect.localScale;
            authoredMotionRotation =
                motionRect.localRotation;
        }

        private void ResetMotion()
        {
            if (motionRect == null)
                return;

            motionRect.anchoredPosition =
                authoredMotionPosition;
            motionRect.localScale =
                authoredMotionScale;
            motionRect.localRotation =
                authoredMotionRotation;
        }

        private void ClearElements()
        {
            foreach (ElementView element in elements)
            {
                if (element?.Rect == null)
                    continue;

                if (Application.isPlaying)
                    Destroy(element.Rect.gameObject);
                else
                    DestroyImmediate(element.Rect.gameObject);
            }
            elements.Clear();
            lastContentSize = Vector2.zero;
        }

        private static bool IsBackgroundMotion(
            LocationBackgroundEffectType type)
        {
            return type ==
                    LocationBackgroundEffectType.FullBackgroundDrift ||
                   type ==
                    LocationBackgroundEffectType.FullBackgroundShake;
        }

        private static Sprite SelectSprite(
            LocationBackgroundEffectType type)
        {
            return type switch
            {
                LocationBackgroundEffectType.RectangularScreenPulse =>
                    softRectangleSprite,
                LocationBackgroundEffectType.LinearSweep =>
                    softRectangleSprite,
                LocationBackgroundEffectType.OccasionalFlicker =>
                    softRectangleSprite,
                LocationBackgroundEffectType.OccasionalSpark =>
                    softRectangleSprite,
                _ => radialGlowSprite
            };
        }

        private static Vector2 ResolveNormalizedSize(
            LocationBackgroundEffectSpec effect,
            int index)
        {
            Rect region = effect.NormalizedRect;
            int count = Mathf.Max(
                1,
                effect.MaxElementCount);
            int columns = Mathf.CeilToInt(Mathf.Sqrt(count));
            int rows = Mathf.CeilToInt(
                count / (float)columns);
            Vector2 cell = new(
                region.width / columns,
                region.height / rows);
            float variation = Mathf.Lerp(
                .82f,
                1.12f,
                SizeHash01(effect.Seed, index));
            float size01 = Mathf.InverseLerp(
                .82f,
                1.12f,
                variation);

            return effect.Type switch
            {
                LocationBackgroundEffectType.DriftingMotes =>
                    Vector2.one *
                    Mathf.Lerp(.005f, .014f, size01),
                LocationBackgroundEffectType.DriftingSteam =>
                    Vector2.one *
                    Mathf.Lerp(.035f, .082f, size01),
                LocationBackgroundEffectType.LinearSweep =>
                    new Vector2(
                        region.width * .13f,
                        region.height * .82f),
                LocationBackgroundEffectType.OccasionalSpark =>
                    new Vector2(.007f, .032f) * variation,
                LocationBackgroundEffectType.RectangularScreenPulse =>
                    Vector2.Scale(cell, new Vector2(.84f, .78f)),
                LocationBackgroundEffectType.OccasionalFlicker =>
                    Vector2.Scale(cell, new Vector2(.92f, .86f)),
                _ => Vector2.Scale(cell, new Vector2(.96f, .96f))
            };
        }

        private static float SizeHash01(int seed, int index)
        {
            unchecked
            {
                int hash =
                    seed * 374761393 +
                    (index * 17 + 211) * 668265263;
                hash = (hash ^ (hash >> 13)) * 1274126177;
                hash ^= hash >> 16;
                return (hash & 0x7FFFFFFF) /
                    (float)int.MaxValue;
            }
        }

        private static bool IsFinitePositive(float value)
        {
            return !float.IsNaN(value) &&
                   !float.IsInfinity(value) &&
                   value > 0f;
        }

        private static void EnsureMaterial()
        {
            if (additiveMaterial != null)
                return;

            Shader shader = Resources.Load<Shader>(
                "Shaders/UIAdditiveGlow");
            if (shader == null)
                return;

            additiveMaterial = new Material(shader)
            {
                name = "Location Background Additive (Runtime)",
                hideFlags = HideFlags.HideAndDontSave
            };
        }

        private static void EnsureSprites()
        {
            // Use Unity's overloaded null check so stale static wrappers are
            // rebuilt when entering play mode with domain reload disabled.
            if (radialGlowSprite == null)
            {
                radialGlowSprite = CreateProceduralSprite(
                    "Location Radial Glow",
                    RadialAlpha);
            }
            if (softRectangleSprite == null)
            {
                softRectangleSprite = CreateProceduralSprite(
                    "Location Soft Rectangle",
                    SoftRectangleAlpha);
            }
        }

        private static Sprite CreateProceduralSprite(
            string name,
            Func<float, float, float> alpha)
        {
            var texture = new Texture2D(
                TextureSize,
                TextureSize,
                TextureFormat.RGBA32,
                false)
            {
                name = name,
                wrapMode = TextureWrapMode.Clamp,
                filterMode = FilterMode.Bilinear,
                hideFlags = HideFlags.HideAndDontSave
            };
            var pixels =
                new Color32[TextureSize * TextureSize];
            for (int y = 0; y < TextureSize; y++)
            {
                float v =
                    (y + .5f) / TextureSize;
                for (int x = 0; x < TextureSize; x++)
                {
                    float u =
                        (x + .5f) / TextureSize;
                    byte a = (byte)Mathf.RoundToInt(
                        Mathf.Clamp01(alpha(u, v)) *
                        255f);
                    pixels[y * TextureSize + x] =
                        new Color32(255, 255, 255, a);
                }
            }
            texture.SetPixels32(pixels);
            texture.Apply(false, true);

            Sprite sprite = Sprite.Create(
                texture,
                new Rect(
                    0f,
                    0f,
                    TextureSize,
                    TextureSize),
                new Vector2(.5f, .5f));
            sprite.name = name;
            return sprite;
        }

        private static float RadialAlpha(
            float u,
            float v)
        {
            float x = (u - .5f) * 2f;
            float y = (v - .5f) * 2f;
            float fade = Mathf.Clamp01(
                1f - Mathf.Sqrt(x * x + y * y));
            return fade * fade;
        }

        private static float SoftRectangleAlpha(
            float u,
            float v)
        {
            float edge = Mathf.Min(
                Mathf.Min(u, 1f - u),
                Mathf.Min(v, 1f - v));
            float fade = Mathf.Clamp01(edge / .18f);
            return fade * fade *
                (3f - 2f * fade);
        }
    }
}
