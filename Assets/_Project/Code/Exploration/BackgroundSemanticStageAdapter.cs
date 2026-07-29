using System;
using System.Linq;
using UnityEngine;

namespace Wake.Exploration
{
    /// <summary>
    /// Adapts an approved semantic slot to the presentation data already used
    /// by ambient and main world-character renderers.
    /// </summary>
    public static class BackgroundSemanticStageAdapter
    {
        private const float DefaultShadowDistance = .018f;
        private const float DefaultGroundShadowScale = .62f;

        public static bool TryCreate(
            ApprovedBackgroundSemanticBinding binding,
            BackgroundSemanticSlot slot,
            out AmbientWorldStageProfile stageProfile)
        {
            stageProfile = default;
            BackgroundSemanticProfile profile = binding?.Profile;
            if (profile == null ||
                slot == null ||
                !profile.Slots.Any(candidate =>
                    candidate != null &&
                    string.Equals(
                        candidate.Id,
                        slot.Id,
                        StringComparison.OrdinalIgnoreCase)))
            {
                return false;
            }

            BackgroundSemanticSlotVisualGrade grade = null;
            binding.TryGetVisualGrade(slot.Id, out grade);
            return TryCreate(
                profile,
                slot,
                grade,
                out stageProfile);
        }

        public static bool TryCreate(
            BackgroundSemanticProfile profile,
            BackgroundSemanticSlot slot,
            out AmbientWorldStageProfile stageProfile)
        {
            return TryCreate(
                profile,
                slot,
                visualGrade: null,
                out stageProfile);
        }

        public static bool TryCreate(
            BackgroundSemanticProfile profile,
            BackgroundSemanticSlot slot,
            BackgroundSemanticSlotVisualGrade visualGrade,
            out AmbientWorldStageProfile stageProfile)
        {
            stageProfile = default;
            if (profile == null ||
                slot == null ||
                profile.IsUnused ||
                !IsFinite(slot.Anchor) ||
                !IsFinite(slot.NormalizedHeight))
            {
                return false;
            }

            BackgroundSemanticLight light = profile.Light;
            Color tint = light.Tint;
            float saturation = light.Saturation;
            float exposure = light.Exposure;
            float contrast = light.Contrast;
            float softness = light.Softness;
            float shadowOpacity = light.ShadowOpacity;
            float groundShadowScale = DefaultGroundShadowScale;
            float shadowDistance = DefaultShadowDistance;

            if (visualGrade != null)
            {
                if (!IsValid(visualGrade))
                    return false;

                tint = Multiply(
                    tint,
                    visualGrade.LightTintMultiplier);
                saturation *= visualGrade.SaturationMultiplier;
                exposure *= visualGrade.ExposureMultiplier;
                contrast *= visualGrade.ContrastMultiplier;
                softness += visualGrade.SoftnessOffset;
                shadowOpacity *=
                    visualGrade.ShadowOpacityMultiplier;
                groundShadowScale =
                    visualGrade.GroundShadowScale;
                shadowDistance = visualGrade.ShadowDistance;
            }

            Vector2 lightDirection = light.Direction;
            Vector2 shadowDirection =
                lightDirection.sqrMagnitude > .000001f
                    ? -lightDirection.normalized * shadowDistance
                    : new Vector2(0f, -shadowDistance);
            bool mirror = slot.Facing switch
            {
                BackgroundSemanticFacing.Left => true,
                BackgroundSemanticFacing.Right => false,
                _ => slot.Anchor.x >= .5f
            };

            stageProfile = new AmbientWorldStageProfile(
                slot.Anchor,
                slot.NormalizedHeight,
                mirror,
                tint,
                shadowDirection,
                shadowOpacity,
                groundShadowScale,
                saturation,
                exposure,
                contrast,
                softness);
            return true;
        }

        private static Color Multiply(Color first, Color second)
        {
            return new Color(
                Mathf.Clamp01(first.r * second.r),
                Mathf.Clamp01(first.g * second.g),
                Mathf.Clamp01(first.b * second.b),
                Mathf.Clamp01(first.a * second.a));
        }

        private static bool IsValid(
            BackgroundSemanticSlotVisualGrade grade)
        {
            Color tint = grade.LightTintMultiplier;
            return !string.IsNullOrEmpty(grade.SlotId) &&
                   IsFinite(tint.r) && tint.r >= 0f &&
                   IsFinite(tint.g) && tint.g >= 0f &&
                   IsFinite(tint.b) && tint.b >= 0f &&
                   IsFinite(tint.a) && tint.a >= 0f &&
                   IsFinite(grade.SaturationMultiplier) &&
                   grade.SaturationMultiplier >= 0f &&
                   IsFinite(grade.ExposureMultiplier) &&
                   grade.ExposureMultiplier >= 0f &&
                   IsFinite(grade.ContrastMultiplier) &&
                   grade.ContrastMultiplier >= 0f &&
                   IsFinite(grade.SoftnessOffset) &&
                   IsFinite(grade.ShadowOpacityMultiplier) &&
                   grade.ShadowOpacityMultiplier >= 0f &&
                   IsFinite(grade.GroundShadowScale) &&
                   grade.GroundShadowScale >= .25f &&
                   grade.GroundShadowScale <= 1.2f &&
                   IsFinite(grade.ShadowDistance) &&
                   grade.ShadowDistance >= 0f &&
                   grade.ShadowDistance <= .10f;
        }

        private static bool IsFinite(Vector2 value) =>
            IsFinite(value.x) && IsFinite(value.y);

        private static bool IsFinite(float value) =>
            !float.IsNaN(value) && !float.IsInfinity(value);
    }
}
