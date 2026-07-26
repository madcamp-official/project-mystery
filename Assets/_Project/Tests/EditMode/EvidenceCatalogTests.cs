using System.Collections.Generic;
using System.Linq;
using NUnit.Framework;
using UnityEditor;
using UnityEngine;
using Wake.Evidence;
using Wake.Narrative;

namespace Wake.Tests
{
    public class EvidenceCatalogTests
    {
        private const string DialoguePath =
            "Assets/_Project/Content/Dialogue/The_Wake_Without_Footprints_Dialogue_KR.csv";
        private readonly List<Object> createdObjects = new();
        private List<DialogueRecord> records;

        [OneTimeSetUp]
        public void LoadDialogue()
        {
            TextAsset csv = AssetDatabase.LoadAssetAtPath<TextAsset>(DialoguePath);
            records = DialogueCsvParser.Parse(csv.text).Records.ToList();
        }

        [TearDown]
        public void TearDown()
        {
            foreach (Object created in createdObjects)
            {
                if (created != null)
                {
                    Object.DestroyImmediate(created);
                }
            }

            createdObjects.Clear();
        }

        [Test]
        public void Catalog_ContainsSequentialC01ThroughC18()
        {
            string[] expected = Enumerable.Range(1, 18)
                .Select(number => $"C-{number:00}")
                .ToArray();

            Assert.That(CanonicalEvidenceCatalog.All.Count, Is.EqualTo(18));
            Assert.That(
                CanonicalEvidenceCatalog.All.Select(entry => entry.Id),
                Is.EqualTo(expected));
            Assert.That(
                CanonicalEvidenceCatalog.All.Select(entry => entry.Id).Distinct().Count(),
                Is.EqualTo(18));
        }

        [Test]
        public void Catalog_UsesReadableKoreanAndCompleteMetadata()
        {
            foreach (CanonicalEvidenceEntry entry in CanonicalEvidenceCatalog.All)
            {
                Assert.That(entry.DisplayName, Is.Not.Empty, entry.Id);
                Assert.That(entry.Description, Is.Not.Empty, entry.Id);
                Assert.That(entry.Category, Is.Not.Empty, entry.Id);
                Assert.That(entry.DisplayName.IndexOf('\uFFFD'), Is.EqualTo(-1), entry.Id);
                Assert.That(entry.Description.IndexOf('\uFFFD'), Is.EqualTo(-1), entry.Id);
                Assert.That(entry.DisplayName, Does.Not.Contain("???"), entry.Id);
                Assert.That(entry.Description, Does.Not.Contain("???"), entry.Id);
                Assert.That(entry.GrantLineIds, Is.Not.Empty, entry.Id);
            }
        }

        [Test]
        public void Catalog_GrantMappingsReferenceProductionLines()
        {
            HashSet<string> stableLineIds = records
                .Select(record => record.StableLineId)
                .ToHashSet();

            foreach (CanonicalEvidenceEntry entry in CanonicalEvidenceCatalog.All)
            {
                foreach (string lineId in entry.GrantLineIds)
                {
                    Assert.That(
                        stableLineIds,
                        Contains.Item(lineId),
                        $"{entry.Id} maps to missing line {lineId}.");
                }
            }
        }

        [Test]
        public void Catalog_NormalizesSupportedEvidenceIdForms()
        {
            Assert.That(CanonicalEvidenceCatalog.NormalizeId(" c1 "), Is.EqualTo("C-01"));
            Assert.That(CanonicalEvidenceCatalog.NormalizeId("c_03"), Is.EqualTo("C-03"));
            Assert.That(CanonicalEvidenceCatalog.NormalizeId("C-18"), Is.EqualTo("C-18"));
            Assert.That(CanonicalEvidenceCatalog.NormalizeId(null), Is.Empty);
            Assert.That(CanonicalEvidenceCatalog.TryGet(" c6 ", out var entry), Is.True);
            Assert.That(entry.Id, Is.EqualTo("C-06"));
            Assert.That(CanonicalEvidenceCatalog.TryGet("C-99", out _), Is.False);
        }

        [Test]
        public void EveryCanonicalAsset_MatchesCatalogAndKeepsMeta()
        {
            foreach (CanonicalEvidenceEntry entry in CanonicalEvidenceCatalog.All)
            {
                string suffix = entry.Id.Replace("-", string.Empty);
                string assetPath =
                    $"Assets/_Project/Content/Evidence/EvidenceDefinition_{suffix}.asset";
                EvidenceDefinition definition =
                    AssetDatabase.LoadAssetAtPath<EvidenceDefinition>(assetPath);

                Assert.That(definition, Is.Not.Null, assetPath);
                Assert.That(definition.EvidenceId, Is.EqualTo(entry.Id), assetPath);
                Assert.That(definition.DisplayName, Is.EqualTo(entry.DisplayName), assetPath);
                Assert.That(definition.Description, Is.EqualTo(entry.Description), assetPath);
                Assert.That(definition.Category, Is.EqualTo(entry.Category), assetPath);
                Assert.That(definition.IsDirect, Is.EqualTo(entry.IsDirect), assetPath);
                Assert.That(
                    AssetDatabase.AssetPathToGUID(assetPath),
                    Is.Not.Null.And.Not.Empty,
                    $"{assetPath}.meta is missing.");
            }
        }

        [Test]
        public void Inventory_RejectsDifferentObjectsWithSameEvidenceId()
        {
            EvidenceInventory inventory = CreateInventory();
            EvidenceDefinition first = CreateDefinition("C-03");
            EvidenceDefinition duplicate = CreateDefinition("c3");

            Assert.That(inventory.Add(first), Is.True);
            Assert.That(inventory.Add(duplicate), Is.False);
            Assert.That(inventory.Contains(" C-03 "), Is.True);
            Assert.That(inventory.Collected, Is.EqualTo(new[] { first }));
        }

        [Test]
        public void Inventory_AddsCanonicalEvidenceByIdWithoutSceneReference()
        {
            EvidenceInventory inventory = CreateInventory();

            Assert.That(inventory.TryAddById(" c16 "), Is.True);
            Assert.That(inventory.TryAddById("C-16"), Is.False);
            Assert.That(inventory.TryAddById("C-99"), Is.False);
            Assert.That(inventory.Collected.Single().DisplayName, Is.EqualTo("보호면 DNA"));
        }

        [Test]
        public void Inventory_RestoresCanonicalIdsAndWarnsForUnknownIds()
        {
            EvidenceInventory inventory = CreateInventory();

            inventory.RestoreFromIds(new[] { " c1 ", "C-01", "C-17", "unknown" });

            Assert.That(
                inventory.Collected.Select(item => item.EvidenceId),
                Is.EqualTo(new[] { "C-01", "C-17" }));
            Assert.That(inventory.Warnings.Count, Is.EqualTo(1));
            Assert.That(inventory.Warnings[0], Does.Contain("unknown"));
        }

        [Test]
        public void DialogueFlow_DoesNotGrantInteractionEvidence()
        {
            var granted = new List<string>();
            var completed = new HashSet<string> { "D1-07" };
            var flow = new ProductionDialogueFlow(
                records,
                completed,
                null,
                evidenceId =>
                {
                    granted.Add(evidenceId);
                    return true;
                });

            CompleteScene(flow, "D2-01");

            Assert.That(granted, Is.Empty);
            Assert.That(
                CanonicalEvidenceCatalog.All
                    .Where(entry => entry.Id is "C-03" or "C-04" or "C-05")
                    .All(entry =>
                        entry.GrantMode == CanonicalEvidenceGrantMode.Interaction),
                Is.True);
        }

        private EvidenceInventory CreateInventory()
        {
            var host = new GameObject("EvidenceCatalogTests");
            createdObjects.Add(host);
            return host.AddComponent<EvidenceInventory>();
        }

        private EvidenceDefinition CreateDefinition(string evidenceId)
        {
            Assert.That(CanonicalEvidenceCatalog.TryGet(evidenceId, out var entry), Is.True);
            EvidenceDefinition definition =
                CanonicalEvidenceCatalog.CreateRuntimeDefinition(entry.Id);
            createdObjects.Add(definition);
            return definition;
        }

        private static void CompleteScene(ProductionDialogueFlow flow, string sceneId)
        {
            Assert.That(flow.StartScene(sceneId), Is.True, string.Join("\n", flow.Warnings));
            while (!flow.IsComplete)
            {
                if (flow.IsAwaitingChoice)
                {
                    flow.SelectChoice(0);
                }
                else
                {
                    flow.Advance();
                }
            }
        }
    }
}
