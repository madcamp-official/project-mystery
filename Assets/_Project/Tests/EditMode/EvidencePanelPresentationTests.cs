using System.Linq;
using NUnit.Framework;
using UnityEngine;
using Wake.Evidence;
namespace Wake.Tests
{
    public class EvidencePanelPresentationTests
    {
        private GameObject host;
        private EvidenceInventory inventory;
        [SetUp]
        public void SetUp()
        {
            host = new GameObject("EvidencePanelPresentationTests");
            inventory = host.AddComponent<EvidenceInventory>();
        }
        [TearDown]
        public void TearDown()
        {
            Object.DestroyImmediate(host);
        }
        [Test]
        public void EmptyInventory_ShowsNoUnknownSlots()
        {
            EvidencePanelViewModel view =
                EvidencePanelPresentation.Create(inventory, 100);
            Assert.That(view.Items, Is.Empty);
            Assert.That(view.CollectedCount, Is.Zero);
        }

        [Test]
        public void RestoredEvidence_KeepsAcquisitionOrderAndNoDuplicates()
        {
            inventory.RestoreFromIds(
                new[] { "C-09", "C-01", "c9", "C-16" });
            Assert.That(inventory.TryAddById("c9"), Is.False);
            EvidencePanelViewModel view =
                EvidencePanelPresentation.Create(inventory, 100);
            Assert.That(
                view.Items.Take(3).Select(item => item.Id),
                Is.EqualTo(new[] { "C-09", "C-01", "C-16" }));
            Assert.That(
                view.Items.Select(item => item.Id).Distinct().Count(),
                Is.EqualTo(3));
            Assert.That(view.CollectedCount, Is.EqualTo(3));
            Assert.That(
                view.Items.Take(3).All(item =>
                    item.State == EvidencePanelItemState.Collected),
                Is.True);
        }

        [Test]
        public void ZeroIntegrity_OnlyMarksCollectedDirectEvidenceUnreliable()
        {
            inventory.TryAddById("C-02");
            inventory.TryAddById("C-01");
            EvidencePanelViewModel view =
                EvidencePanelPresentation.Create(inventory, 0);
            Assert.That(view.Items[0].Id, Is.EqualTo("C-02"));
            Assert.That(
                view.Items[0].State,
                Is.EqualTo(EvidencePanelItemState.Unreliable));
            Assert.That(
                view.Items[1].State,
                Is.EqualTo(EvidencePanelItemState.Collected));
            Assert.That(view.UnreliableCount, Is.EqualTo(1));
            Assert.That(
                view.Items[0].Reliability,
                Does.Contain("신뢰성을 다시 확인"));
        }

        [Test]
        public void TextOnlyEvidence_ClearlyReportsNoImage()
        {
            inventory.TryAddById("C-18");
            EvidencePanelItem item =
                EvidencePanelPresentation.Create(inventory, 100).Items[0];
            Assert.That(item.Title, Does.Contain("수정 기사"));
            Assert.That(item.Detail, Does.Contain("피해자의 오판"));
            Assert.That(item.Detail, Does.Not.Contain("D8-03"));
            Assert.That(item.Title, Does.Not.Contain("C-18"));
            Assert.That(item.CarouselLabel, Does.Not.Contain("C-18"));
            Assert.That(item.AcquisitionPlace, Is.EqualTo("항구"));
            Assert.That(item.RelatedPeople, Does.Contain("리처드 호손"));
            Assert.That(item.Reliability, Is.Not.Empty);
            Assert.That(item.HasImage, Is.False);
        }

        [Test]
        public void RecordDetail_SeparatesStoryMetadataFromDescription()
        {
            inventory.TryAddById("C-15");
            EvidencePanelItem item =
                EvidencePanelPresentation.Create(inventory, 100).Items[0];

            Assert.That(item.AcquisitionPlace, Does.Contain("금고실"));
            Assert.That(item.AcquisitionPlace, Does.Contain("의무실"));
            Assert.That(item.RelatedPeople, Is.EqualTo("마커스 케인"));
            Assert.That(item.Detail, Does.Not.Contain(item.Id));
            Assert.That(item.Detail, Does.Not.Contain("총 단서"));
            Assert.That(item.Detail, Does.Not.Contain("수집률"));
        }

        [Test]
        public void MarcusAuthentication_IsOnlyGrantedByTypedInteraction()
        {
            Assert.That(
                CanonicalEvidenceCatalog.TryGet("C-15", out var entry),
                Is.True);
            Assert.That(
                entry.GrantMode,
                Is.EqualTo(CanonicalEvidenceGrantMode.Interaction));
            Assert.That(
                CanonicalEvidenceCatalog.GetGrantedEvidenceIds("d4_04_04"),
                Does.Not.Contain("C-15"));
        }
    }
}
