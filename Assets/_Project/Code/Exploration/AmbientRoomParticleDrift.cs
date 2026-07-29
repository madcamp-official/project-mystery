using UnityEngine;

namespace Wake.Exploration
{
    public readonly struct AmbientParticleState
    {
        public AmbientParticleState(Vector2 position, float alpha01)
        {
            Position = position;
            Alpha01 = alpha01;
        }

        public Vector2 Position { get; }
        public float Alpha01 { get; }
    }

    // Pure, deterministic drift/twinkle math for AmbientRoomParticleOverlay's
    // dust motes. Kept separate from the MonoBehaviour so it's unit-testable
    // without a scene, matching BackgroundCoverLayout's split from
    // BackgroundCoverPresenter.
    //
    // Position is computed as a base offset (from the seed) plus motion,
    // wrapped with Mathf.Repeat(_, length) every frame. Repeat always
    // returns a value in [0, length) regardless of how far out of range its
    // input is, so the particle is *always* inside bounds - there's no
    // separate "if it left the bounds, respawn it" branch to get wrong.
    public static class AmbientRoomParticleDrift
    {
        private const float MinAlpha = 0.15f;
        private const float MaxAlpha = 0.6f;

        public static AmbientParticleState Evaluate(
            int seed,
            float time,
            Rect bounds)
        {
            float verticalSpeed = Mathf.Lerp(4f, 10f, Hash01(seed, 0));
            float y = bounds.yMin + Mathf.Repeat(
                Hash01(seed, 1) * bounds.height + time * verticalSpeed,
                bounds.height);

            float swaySpeed = Mathf.Lerp(0.3f, 0.7f, Hash01(seed, 2));
            float swayPhase = Hash01(seed, 3) * Mathf.PI * 2f;
            float swayAmplitude =
                bounds.width * Mathf.Lerp(0.03f, 0.08f, Hash01(seed, 4));
            float x = bounds.xMin + Mathf.Repeat(
                Hash01(seed, 5) * bounds.width +
                Mathf.Sin(time * swaySpeed + swayPhase) * swayAmplitude,
                bounds.width);

            float twinkleSpeed = Mathf.Lerp(0.8f, 1.6f, Hash01(seed, 6));
            float twinklePhase = Hash01(seed, 7) * Mathf.PI * 2f;
            float twinkle01 =
                (Mathf.Sin(time * twinkleSpeed + twinklePhase) + 1f) * 0.5f;
            float alpha = Mathf.Lerp(MinAlpha, MaxAlpha, twinkle01);

            return new AmbientParticleState(new Vector2(x, y), alpha);
        }

        // Deterministic, allocation-free pseudo-random in [0, 1] for a given
        // seed + channel (channel is a distinct small index per constant we
        // derive, so one seed can drive several independent-looking values).
        private static float Hash01(int seed, int channel)
        {
            unchecked
            {
                int h = seed * 374761393 + channel * 668265263;
                h = (h ^ (h >> 13)) * 1274126177;
                h ^= h >> 16;
                return (h & 0x7FFFFFFF) / (float)int.MaxValue;
            }
        }
    }
}
