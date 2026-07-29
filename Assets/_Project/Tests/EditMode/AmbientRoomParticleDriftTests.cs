using NUnit.Framework;
using UnityEngine;
using Wake.Exploration;

namespace Wake.Tests
{
    public sealed class AmbientRoomParticleDriftTests
    {
        private static readonly Rect Bounds =
            new(-400f, -300f, 800f, 600f);

        [Test]
        public void Evaluate_PositionStaysWithinBounds()
        {
            for (int seed = 0; seed < 20; seed++)
            {
                for (float time = 0f; time < 30f; time += 3.7f)
                {
                    AmbientParticleState state =
                        AmbientRoomParticleDrift.Evaluate(seed, time, Bounds);

                    Assert.That(
                        state.Position.x,
                        Is.InRange(Bounds.xMin, Bounds.xMax));
                    Assert.That(
                        state.Position.y,
                        Is.InRange(Bounds.yMin, Bounds.yMax));
                }
            }
        }

        [Test]
        public void Evaluate_AlphaStaysWithinZeroToOne()
        {
            for (int seed = 0; seed < 20; seed++)
            {
                for (float time = 0f; time < 30f; time += 3.7f)
                {
                    AmbientParticleState state =
                        AmbientRoomParticleDrift.Evaluate(seed, time, Bounds);

                    Assert.That(state.Alpha01, Is.InRange(0f, 1f));
                }
            }
        }

        [Test]
        public void Evaluate_SameInputs_ReturnsIdenticalResult()
        {
            AmbientParticleState first =
                AmbientRoomParticleDrift.Evaluate(7, 12.5f, Bounds);
            AmbientParticleState second =
                AmbientRoomParticleDrift.Evaluate(7, 12.5f, Bounds);

            Assert.That(second.Position, Is.EqualTo(first.Position));
            Assert.That(second.Alpha01, Is.EqualTo(first.Alpha01));
        }

        [Test]
        public void Evaluate_DifferentSeeds_ProduceDifferentPositions()
        {
            AmbientParticleState particleA =
                AmbientRoomParticleDrift.Evaluate(1, 5f, Bounds);
            AmbientParticleState particleB =
                AmbientRoomParticleDrift.Evaluate(2, 5f, Bounds);

            Assert.That(
                particleA.Position,
                Is.Not.EqualTo(particleB.Position));
        }
    }
}
