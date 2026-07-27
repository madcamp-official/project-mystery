using NUnit.Framework;
using UnityEngine;
using Wake.UI;

namespace Wake.Tests
{
    public class RuntimeUiOverhaulControllerTests
    {
        [Test]
        public void ReferenceResolution_Is2880x1800()
        {
            Assert.That(
                RuntimeUiOverhaulController.ReferenceResolution,
                Is.EqualTo(new Vector2(2880f, 1800f)));
        }
    }
}
