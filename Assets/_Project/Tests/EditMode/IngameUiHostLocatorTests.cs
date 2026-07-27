using NUnit.Framework;
using Wake.UI;

namespace Wake.Tests
{
    public class IngameUiHostLocatorTests
    {
        private sealed class FakeHost : IIngameUiHost
        {
            public bool IsShowingIngamePanel { get; set; }
            public bool IsSettingsOpen { get; set; }
            public int OpenRuntimeModalCount { get; set; }
            public int ShowIngameCalls { get; private set; }
            public void ShowIngame() => ShowIngameCalls++;
            public void ShowEvidence() { }
            public void ShowEvidence(string evidenceId) { }
            public void CloseSettings() { }
        }

        [Test]
        public void Current_ReturnsNull_WhenNoHostRegistered()
        {
            IngameUi.Register(null);
            Assert.That(IngameUi.Current, Is.Null);
        }

        [Test]
        public void Current_ReturnsRegisteredHost()
        {
            var host = new FakeHost();
            IngameUi.Register(host);
            Assert.That(IngameUi.Current, Is.SameAs(host));
            IngameUi.Register(null);
        }
    }
}
