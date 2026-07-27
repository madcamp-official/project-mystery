using NUnit.Framework;
using UnityEngine;
using Wake.UI;

namespace Wake.Tests
{
    public class LobbyRevealSequenceTests
    {
        [Test]
        public void ComputeWorldHeight_MatchesCanvasHeightTimesScale()
        {
            var go = new GameObject("Canvas", typeof(RectTransform));
            try
            {
                RectTransform rect = go.GetComponent<RectTransform>();
                rect.sizeDelta = new Vector2(2880f, 1800f);
                rect.localScale = new Vector3(0.0056f, 0.0056f, 0.0056f);

                float height = LobbyRevealSequence.ComputeWorldHeight(rect);

                Assert.That(height, Is.EqualTo(1800f * 0.0056f).Within(0.0001f));
            }
            finally
            {
                Object.DestroyImmediate(go);
            }
        }
    }
}
