using System;
using System.Collections.Generic;
using System.Linq;
using NUnit.Framework;
using UnityEditor;
using UnityEngine;
using Wake.UI;

namespace Wake.Tests
{
    public sealed class MapInteractionGeometryTests
    {
        private static readonly int[] ActiveDecks = { 0, 7, 8, 9, 10 };

        [SetUp]
        public void SetUp() =>
            MapInteractionGeometryCatalog.InvalidateCache();

        [Test]
        public void ActiveDeckRuntimeAssets_AreCommittedAndLoadable()
        {
            foreach (int deck in ActiveDecks)
            {
                string prefix = deck == 0 ? "Port" : $"Deck{deck:00}";
                string nodePath =
                    "Assets/_Project/Resources/Maps/MapNodes/" +
                    $"{prefix}_MapNodes.asset";
                string maskPath =
                    "Assets/_Project/Resources/Maps/RoomMasks/" +
                    $"{prefix}_RoomMasks.asset";
                Assert.That(
                    AssetDatabase.LoadAssetAtPath<MapNodesAsset>(nodePath),
                    Is.Not.Null,
                    nodePath);
                Assert.That(
                    AssetDatabase.LoadAssetAtPath<RoomMasksAsset>(maskPath),
                    Is.Not.Null,
                    maskPath);
            }

            Assert.That(
                Resources.LoadAll<MapNodesAsset>("Maps/MapNodes"),
                Has.Length.EqualTo(5));
            Assert.That(
                Resources.LoadAll<RoomMasksAsset>("Maps/RoomMasks"),
                Has.Length.EqualTo(5));
        }

        [Test]
        public void EveryPlayableLocation_HasExactlyOneNodeAndRoomMask()
        {
            string[] active = MapDeckCatalog.All
                .Select(item => item.LocationCode)
                .OrderBy(code => code, StringComparer.Ordinal)
                .ToArray();
            string[] nodes = MapInteractionGeometryCatalog.NodeAssets
                .SelectMany(asset => asset.Nodes)
                .Select(node => node.LocationCode)
                .OrderBy(code => code, StringComparer.Ordinal)
                .ToArray();
            string[] masks = MapInteractionGeometryCatalog.MaskAssets
                .SelectMany(asset => asset.Masks)
                .Select(mask => mask.LocationCode)
                .OrderBy(code => code, StringComparer.Ordinal)
                .ToArray();

            Assert.That(active, Has.Length.EqualTo(24));
            Assert.That(nodes, Is.EqualTo(active));
            Assert.That(masks, Is.EqualTo(active));
            Assert.That(nodes.Distinct().Count(), Is.EqualTo(nodes.Length));
            Assert.That(masks.Distinct().Count(), Is.EqualTo(masks.Length));

            string[] unused = MapDeckCatalog.Unused
                .Select(item => item.LocationCode)
                .ToArray();
            Assert.That(nodes.Intersect(unused), Is.Empty);
            Assert.That(masks.Intersect(unused), Is.Empty);
            Assert.That(
                MapInteractionGeometryCatalog.NodeAssets.Any(
                    asset => asset.Deck == 6),
                Is.False);
            Assert.That(
                MapInteractionGeometryCatalog.MaskAssets.Any(
                    asset => asset.Deck == 6),
                Is.False);
        }

        [TestCase(0, 2)]
        [TestCase(7, 6)]
        [TestCase(8, 5)]
        [TestCase(9, 4)]
        [TestCase(10, 7)]
        public void DeckGeometry_HasExpectedLocationCount(
            int deck,
            int expected)
        {
            Assert.That(
                MapInteractionGeometryCatalog.NodesForDeck(deck),
                Has.Count.EqualTo(expected));
            Assert.That(
                MapInteractionGeometryCatalog.MasksForDeck(deck),
                Has.Count.EqualTo(expected));
        }

        [Test]
        public void Geometry_IsNormalizedNonOverlappingAndDeckAligned()
        {
            Assert.That(
                MapInteractionGeometryCatalog.Validate(
                    out IReadOnlyList<string> diagnostics),
                Is.True,
                string.Join(Environment.NewLine, diagnostics));

            foreach (MapNodesAsset asset in
                     MapInteractionGeometryCatalog.NodeAssets)
            {
                Sprite expected = Resources.Load<Sprite>(
                    MapDeckCatalog.ResourceKey(
                        asset.Deck,
                        MapLayerMode.Passenger));
                Assert.That(asset.SourceMap, Is.SameAs(expected));
                string sourcePath =
                    AssetDatabase.GetAssetPath(asset.SourceMap);
                Assert.That(
                    asset.AuthoredSourceHash,
                    Is.EqualTo(
                        AssetDatabase.GetAssetDependencyHash(sourcePath)
                            .ToString()),
                    sourcePath);
                foreach (MapLocationNode node in asset.Nodes)
                {
                    Assert.That(
                        MapPolygonUtility.IsNormalized(
                            node.NormalizedPosition),
                        Is.True,
                        node.LocationCode);
                    Assert.That(
                        MapDeckCatalog.Find(node.LocationCode).Deck,
                        Is.EqualTo(asset.Deck),
                        node.LocationCode);
                }
            }
        }

        [Test]
        public void CorrectiveArtworkFlags_AreLimitedToKnownMapMismatches()
        {
            string[] flagged = MapInteractionGeometryCatalog.MaskAssets
                .SelectMany(asset => asset.Masks)
                .Where(mask => mask.CorrectiveArtworkRecommended)
                .Select(mask => mask.LocationCode)
                .OrderBy(code => code, StringComparer.Ordinal)
                .ToArray();

            Assert.That(
                flagged,
                Is.EqualTo(new[]
                {
                    "BALLAST_CONTROL_ANNEX",
                    "CREW_STAIRS",
                    "ENGINE_CONTROL",
                    "OPEN_DECK",
                    "PROMENADE",
                    "SECURITY",
                    "SERVICE_RAIL",
                    "VIP_LOUNGE"
                }));
        }

        [Test]
        public void OpenDeckMask_FollowsThinExteriorDeckWithoutSeaOrCabin()
        {
            Assert.That(
                MapInteractionGeometryCatalog.TryGetMask(
                    "OPEN_DECK",
                    out MapRoomMask mask),
                Is.True);

            Assert.That(
                MapPolygonUtility.Contains(
                    mask.Polygon,
                    new Vector2(.22f, .82f)),
                Is.True,
                "The thin upper exterior deck must remain selectable.");
            Assert.That(
                MapPolygonUtility.Contains(
                    mask.Polygon,
                    new Vector2(.22f, .90f)),
                Is.False,
                "Open sea above the hull must not be selectable.");
            Assert.That(
                MapPolygonUtility.Contains(
                    mask.Polygon,
                    new Vector2(.105f, .835f)),
                Is.False,
                "The former rectangular hit area's stern-side sea sample " +
                "must remain outside the room polygon.");
            Assert.That(
                MapPolygonUtility.Contains(
                    mask.Polygon,
                    new Vector2(.22f, .60f)),
                Is.False,
                "The Richard Suite cabin below must not be selectable.");
        }

        [TestCase("BALLROOM")]
        [TestCase("NEWS_LOUNGE")]
        [TestCase("RICHARD_SUITE")]
        [TestCase("OPEN_DECK")]
        public void BlueprintRooms_UseWallTracingPolygons(
            string locationCode)
        {
            Assert.That(
                MapInteractionGeometryCatalog.TryGetMask(
                    locationCode,
                    out MapRoomMask mask),
                Is.True,
                locationCode);
            Assert.That(
                mask.Polygon.Count,
                Is.GreaterThan(4),
                $"{locationCode} must follow blueprint walls instead of " +
                "using a bounding rectangle.");
        }

        [Test]
        public void ConcaveRoomHitArea_RejectsBoundingBoxNotch()
        {
            var roomObject = new GameObject(
                "Room Hit Test",
                typeof(RectTransform),
                typeof(CanvasRenderer),
                typeof(MapRoomHitAreaGraphic));
            try
            {
                MapRoomHitAreaGraphic graphic =
                    roomObject.GetComponent<MapRoomHitAreaGraphic>();
                graphic.Configure(
                    new[]
                    {
                        new Vector2(.1f, .1f),
                        new Vector2(.9f, .1f),
                        new Vector2(.9f, .4f),
                        new Vector2(.4f, .4f),
                        new Vector2(.4f, .9f),
                        new Vector2(.1f, .9f)
                    },
                    false,
                    false,
                    false);

                Assert.That(
                    graphic.ContainsNormalized(new Vector2(.2f, .8f)),
                    Is.True);
                Assert.That(
                    graphic.ContainsNormalized(new Vector2(.8f, .8f)),
                    Is.False);
            }
            finally
            {
                UnityEngine.Object.DestroyImmediate(roomObject);
            }
        }

        [Test]
        public void LockedRoomHitArea_PreservesLockAndExactPolygonHitShape()
        {
            var roomObject = new GameObject(
                "Locked Room Hit Test",
                typeof(RectTransform),
                typeof(CanvasRenderer),
                typeof(MapRoomHitAreaGraphic));
            try
            {
                MapRoomHitAreaGraphic graphic =
                    roomObject.GetComponent<MapRoomHitAreaGraphic>();
                graphic.Configure(
                    new[]
                    {
                        new Vector2(.1f, .1f),
                        new Vector2(.9f, .1f),
                        new Vector2(.9f, .4f),
                        new Vector2(.4f, .4f),
                        new Vector2(.4f, .9f),
                        new Vector2(.1f, .9f)
                    },
                    true,
                    false,
                    false);

                Assert.That(graphic.IsLocked, Is.True);
                Assert.That(
                    graphic.ContainsNormalized(new Vector2(.2f, .8f)),
                    Is.True);
                Assert.That(
                    graphic.ContainsNormalized(new Vector2(.8f, .8f)),
                    Is.False,
                    "A locked room must not make its bounding-box notch " +
                    "clickable.");
            }
            finally
            {
                UnityEngine.Object.DestroyImmediate(roomObject);
            }
        }

        [Test]
        public void DayOneRedactions_CoverTechnicalAndInternalInformation()
        {
            string[] required =
            {
                "D7_INTERNAL_DANIEL_ID",
                "D7_NORTH_TECHNICAL",
                "D9_BALLROOM_SERVICE",
                "D9_INTERNAL_BALLROOM_ID",
                "D10_INTERNAL_SUITE_ID",
                "D10_SERVICE_ACCESS"
            };
            foreach (string id in required)
            {
                MapPassengerRedaction redaction =
                    MapPassengerRedactionCatalog.All.Single(
                        item => item.Id == id);
                Assert.That(
                    MapPassengerRedactionCatalog.ShouldRender(
                        redaction,
                        MapLayerMode.Passenger,
                        Array.Empty<string>()),
                    Is.True,
                    id);
            }

            MapPassengerRedaction technical =
                MapPassengerRedactionCatalog.All.Single(
                    item => item.Id == "D7_NORTH_TECHNICAL");
            Assert.That(
                MapPassengerRedactionCatalog.ShouldRender(
                    technical,
                    MapLayerMode.Technical,
                    new[] { MapDeckCatalog.TechnicalUnlockSceneId }),
                Is.False);
            MapPassengerRedaction permanent =
                MapPassengerRedactionCatalog.All.Single(
                    item => item.Id == "D9_INTERNAL_BALLROOM_ID");
            Assert.That(
                MapPassengerRedactionCatalog.ShouldRender(
                    permanent,
                    MapLayerMode.Technical,
                    new[] { MapDeckCatalog.TechnicalUnlockSceneId }),
                Is.True);
        }

        [Test]
        public void PassengerRedactions_OnlyCoverTextSizedRegions()
        {
            foreach (MapPassengerRedaction redaction in
                     MapPassengerRedactionCatalog.All)
            {
                float area = Mathf.Abs(
                    MapPolygonUtility.SignedArea(redaction.Polygon));
                Assert.That(
                    area,
                    Is.GreaterThan(0f).And.LessThan(.025f),
                    $"{redaction.Id} must not hide a rectangular room or " +
                    "large section of the blueprint.");
            }
        }

        [Test]
        public void TechnicalLayer_RequiresD602Completion_NotUnlock()
        {
            Assert.That(
                MapDeckCatalog.IsLayerUnlocked(
                    MapLayerMode.Technical,
                    Array.Empty<string>(),
                    Array.Empty<string>()),
                Is.False);
            Assert.That(
                MapDeckCatalog.IsLayerUnlocked(
                    MapLayerMode.Technical,
                    Array.Empty<string>(),
                    new[] { MapDeckCatalog.TechnicalUnlockSceneId }),
                Is.False);
            Assert.That(
                MapDeckCatalog.IsLayerUnlocked(
                    MapLayerMode.Technical,
                    new[] { "D6-01" },
                    new[] { MapDeckCatalog.TechnicalUnlockSceneId }),
                Is.False);
            Assert.That(
                MapDeckCatalog.IsLayerUnlocked(
                    MapLayerMode.Technical,
                    new[] { MapDeckCatalog.TechnicalUnlockSceneId },
                    Array.Empty<string>()),
                Is.True);
        }
    }
}
