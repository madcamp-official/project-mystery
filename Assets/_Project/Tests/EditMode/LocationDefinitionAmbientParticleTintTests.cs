using NUnit.Framework;
using UnityEngine;
using Wake.Exploration;

namespace Wake.Tests
{
    public sealed class LocationDefinitionAmbientParticleTintTests
    {
        [Test]
        public void AmbientParticleTint_DefaultsToWarmLowAlphaWhite()
        {
            LocationDefinition location =
                ScriptableObject.CreateInstance<LocationDefinition>();
            try
            {
                Assert.That(
                    location.AmbientParticleTint,
                    Is.EqualTo(new Color(1f, 0.95f, 0.85f, 0.5f)));
            }
            finally
            {
                Object.DestroyImmediate(location);
            }
        }
    }
}
