using System.Collections;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;
using Wake.UI;

namespace Wake.Tests
{
    public class LobbyRevealSequencePlayModeTests
    {
        [UnityTest]
        public IEnumerator Play_MovesTitleUpAndRevealGroupAndWaterIntoPlace()
        {
            var canvasGo = new GameObject("Canvas", typeof(RectTransform));
            RectTransform canvasRect = canvasGo.GetComponent<RectTransform>();
            canvasRect.sizeDelta = new Vector2(2880f, 1800f);
            canvasRect.localScale = new Vector3(0.0056f, 0.0056f, 0.0056f);

            var titleGo = new GameObject("Title", typeof(RectTransform));
            RectTransform title = titleGo.GetComponent<RectTransform>();
            title.SetParent(canvasRect, false);

            var revealGo = new GameObject("RevealGroup", typeof(RectTransform));
            RectTransform reveal = revealGo.GetComponent<RectTransform>();
            reveal.SetParent(canvasRect, false);
            reveal.anchoredPosition = new Vector2(0f, -1800f);

            var waterGo = new GameObject("Water");
            float waterStartY = waterGo.transform.position.y;

            var sequenceGo = new GameObject("Sequence");
            LobbyRevealSequence sequence =
                sequenceGo.AddComponent<LobbyRevealSequence>();
            sequence.Configure(title, reveal, waterGo.transform, canvasRect);
            sequence.Play();

            yield return new WaitForSeconds(1f);

            Assert.That(title.anchoredPosition.y, Is.EqualTo(1800f).Within(0.01f));
            Assert.That(reveal.anchoredPosition.y, Is.EqualTo(0f).Within(0.01f));
            Assert.That(
                waterGo.transform.position.y,
                Is.EqualTo(waterStartY + 1800f * 0.0056f).Within(0.01f));

            Object.Destroy(canvasGo);
            Object.Destroy(waterGo);
            Object.Destroy(sequenceGo);
        }
    }
}
