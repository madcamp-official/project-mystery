using System.Collections;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;
using UnityEngine.UI;
using Wake.UI;

namespace Wake.Tests.PlayMode
{
    public sealed class UiScreenTransitionPlayModeTests
    {
        private GameObject canvasObject;
        private GameObject host;

        [TearDown]
        public void TearDown()
        {
            if (host != null)
                Object.DestroyImmediate(host);
            if (canvasObject != null)
                Object.DestroyImmediate(canvasObject);
        }

        [UnityTest]
        public IEnumerator Run_ExitsSwapsAndEnters_WhileRejectingDuplicates()
        {
            canvasObject = new GameObject(
                "Transition Test Canvas",
                typeof(RectTransform),
                typeof(Canvas),
                typeof(CanvasScaler),
                typeof(GraphicRaycaster));
            Canvas canvas = canvasObject.GetComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            RectTransform canvasRect =
                canvasObject.GetComponent<RectTransform>();

            GameObject outgoing = CreatePanel(canvasRect, "Outgoing");
            GameObject incoming = CreatePanel(canvasRect, "Incoming");
            incoming.SetActive(false);

            host = new GameObject("Transition Coordinator");
            UIScreenTransitionCoordinator coordinator =
                host.AddComponent<UIScreenTransitionCoordinator>();
            coordinator.Configure(canvasRect);
            coordinator.SetReducedMotion(true);

            bool swapped = false;
            bool completed = false;
            bool accepted = coordinator.Run(
                outgoing,
                incoming,
                () =>
                {
                    outgoing.SetActive(false);
                    incoming.SetActive(true);
                    swapped = true;
                },
                () => completed = true);

            Assert.That(accepted, Is.True);
            Assert.That(coordinator.IsTransitioning, Is.True);
            Assert.That(
                coordinator.Run(outgoing, incoming, null),
                Is.False,
                "A second request must be rejected while transitioning.");
            Assert.That(outgoing.activeSelf, Is.True);
            Assert.That(incoming.activeSelf, Is.False);

            yield return new WaitForSecondsRealtime(.25f);

            Assert.That(swapped, Is.True);
            Assert.That(outgoing.activeSelf, Is.False);
            Assert.That(incoming.activeSelf, Is.True);

            yield return new WaitForSecondsRealtime(.25f);

            Assert.That(completed, Is.True);
            Assert.That(coordinator.IsTransitioning, Is.False);
            RectTransform element =
                incoming.transform.GetChild(0) as RectTransform;
            Assert.That(element.anchoredPosition, Is.EqualTo(Vector2.zero));
            Assert.That(
                element.GetComponent<CanvasGroup>().alpha,
                Is.EqualTo(1f).Within(.001f));

            bool closed = false;
            Assert.That(
                coordinator.Run(
                    incoming,
                    null,
                    () =>
                    {
                        incoming.SetActive(false);
                        closed = true;
                    }),
                Is.True);

            yield return new WaitForSecondsRealtime(.25f);

            Assert.That(closed, Is.True);
            Assert.That(incoming.activeSelf, Is.False);
            Assert.That(coordinator.IsTransitioning, Is.False);
        }

        private static GameObject CreatePanel(
            RectTransform parent,
            string name)
        {
            GameObject panel = new(name, typeof(RectTransform));
            panel.transform.SetParent(parent, false);
            RectTransform panelRect =
                panel.GetComponent<RectTransform>();
            panelRect.anchorMin = Vector2.zero;
            panelRect.anchorMax = Vector2.one;
            panelRect.offsetMin = Vector2.zero;
            panelRect.offsetMax = Vector2.zero;

            GameObject element = new(
                "Element",
                typeof(RectTransform),
                typeof(CanvasRenderer),
                typeof(Image));
            element.transform.SetParent(panel.transform, false);
            RectTransform elementRect =
                element.GetComponent<RectTransform>();
            elementRect.sizeDelta = new Vector2(200f, 80f);
            panel.AddComponent<UiPanelTransitionAnimator>();
            return panel;
        }
    }
}
