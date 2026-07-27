using System.IO;
using NUnit.Framework;
using UnityEngine;
using Wake.UI;

namespace Wake.Tests
{
    public sealed class RuntimeUiLayoutRegistryTests
    {
        private GameObject root;

        [TearDown]
        public void TearDown()
        {
            if (root != null)
                Object.DestroyImmediate(root);
        }

        [Test]
        public void Attach_UsesEditableSlotAsRuntimeParent()
        {
            root = new GameObject(
                "Canvas",
                typeof(RectTransform),
                typeof(RuntimeUiLayoutRegistry));
            var slotObject = new GameObject(
                "Modal Slot",
                typeof(RectTransform),
                typeof(RuntimeUiLayoutSlot));
            slotObject.transform.SetParent(root.transform, false);
            slotObject.GetComponent<RuntimeUiLayoutSlot>().Configure(
                "modal.test",
                Color.cyan);
            var runtimeObject = new GameObject(
                "Runtime Modal",
                typeof(RectTransform));

            bool attached = RuntimeUiLayoutRegistry.Attach(
                runtimeObject.GetComponent<RectTransform>(),
                "modal.test");

            Assert.That(attached, Is.True);
            Assert.That(
                runtimeObject.transform.parent,
                Is.SameAs(slotObject.transform));
            RectTransform rect =
                runtimeObject.GetComponent<RectTransform>();
            Assert.That(rect.anchorMin, Is.EqualTo(Vector2.zero));
            Assert.That(rect.anchorMax, Is.EqualTo(Vector2.one));
            Object.DestroyImmediate(runtimeObject);
        }

        [Test]
        public void NormalizedRect_ReadsInspectorAnchors()
        {
            root = new GameObject(
                "Canvas",
                typeof(RectTransform),
                typeof(RuntimeUiLayoutRegistry));
            var slotObject = new GameObject(
                "Hotspot Slot",
                typeof(RectTransform),
                typeof(RuntimeUiLayoutSlot));
            slotObject.transform.SetParent(root.transform, false);
            RectTransform slotRect =
                slotObject.GetComponent<RectTransform>();
            slotRect.anchorMin = new Vector2(.2f, .3f);
            slotRect.anchorMax = new Vector2(.6f, .8f);
            slotObject.GetComponent<RuntimeUiLayoutSlot>().Configure(
                "location.test.evidence.c-01",
                Color.green);

            Assert.That(
                RuntimeUiLayoutRegistry.TryGetNormalizedRect(
                    "location.test.evidence.c-01",
                    out Rect rect),
                Is.True);
            Assert.That(rect.min, Is.EqualTo(new Vector2(.2f, .3f)));
            Assert.That(rect.max, Is.EqualTo(new Vector2(.6f, .8f)));
        }

        [Test]
        public void UiBasicScene_HasPlaceholdersWithoutGlobalProgressPanel()
        {
            string yaml = File.ReadAllText(
                "Assets/_Project/Scenes/UI/UI Basic Scene.unity");

            Assert.That(yaml, Does.Contain("m_Name: Runtime UI Layout"));
            Assert.That(yaml, Does.Contain("m_Name: Modal Slots"));
            Assert.That(yaml, Does.Contain("m_Name: Location Overlay Slots"));
            Assert.That(yaml, Does.Not.Contain(
                "m_Name: Investigation Progress"));
        }
    }
}
