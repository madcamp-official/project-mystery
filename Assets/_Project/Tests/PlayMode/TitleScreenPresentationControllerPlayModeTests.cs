using System.Collections;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;
using UnityEngine.UI;
using Wake.UI;

namespace Wake.Tests.PlayMode
{
    public sealed class TitleScreenPresentationControllerPlayModeTests
    {
        [UnityTest]
        public IEnumerator Awake_CreatesChildObjectsSynchronously()
        {
            // Arrange: Create a test GameObject with the expected child buttons
            var testHost = new GameObject("TestTitleScreenHost");
            var hostRect = testHost.AddComponent<RectTransform>();

            // Create Start Game Button
            var startButton = new GameObject("Start Game Btn", typeof(RectTransform), typeof(Button));
            startButton.transform.SetParent(testHost.transform, false);

            // Create Settings Button
            var settingsButton = new GameObject("Settings Btn", typeof(RectTransform), typeof(Button));
            settingsButton.transform.SetParent(testHost.transform, false);

            // Act: Add the controller component (should trigger Awake synchronously)
            TitleScreenPresentationController controller =
                testHost.AddComponent<TitleScreenPresentationController>();

            // Assert: The "Title Presentation" object should exist immediately after AddComponent
            // This verifies that Build() was called in Awake(), not deferred to Start()
            Transform titlePresentationTransform = testHost.transform.Find("Title Presentation");
            Assert.That(
                titlePresentationTransform,
                Is.Not.Null,
                "Title Presentation should be created synchronously in Awake, not deferred to Start");

            Assert.That(
                titlePresentationTransform.gameObject.name,
                Is.EqualTo("Title Presentation"));

            // Verify that the presentation object has the expected components
            Image backgroundImage = titlePresentationTransform.GetComponent<Image>();
            Assert.That(
                backgroundImage,
                Is.Not.Null,
                "Title Presentation should have an Image component");

            // Cleanup
            Object.Destroy(testHost);
            yield return null;
        }
    }
}
