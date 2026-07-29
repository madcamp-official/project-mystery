using System;
using System.Linq;
using NUnit.Framework;
using UnityEditor;
using UnityEngine;
using Wake.UI;

namespace Wake.Tests
{
    public sealed class MapAreaCatalogTests
    {
        private const string LayerRoot =
            "Assets/_Project/Resources/Maps/DeckLayers";

        [Test]
        public void ActiveDecks_HaveValidNormalizedAreaShapes()
        {
            foreach (int deck in new[] { 7, 8, 9, 10 })
            {
                MapAreaShape[] areas =
                    MapAreaCatalog.ForDeck(deck).ToArray();
                Assert.That(areas, Is.Not.Empty, $"Deck {deck}");
                foreach (MapAreaShape area in areas)
                {
                    Assert.That(
                        MapAreaCatalog.IsValid(area, out string error),
                        Is.True,
                        $"{area.AreaId}: {error}");
                }
            }
        }

        [Test]
        public void EveryActiveRouteOnlyRoom_HasAnAreaShapeAndEntrance()
        {
            MapLocationPlacement[] restrictedRooms = MapDeckCatalog.All
                .Where(item =>
                    item.Deck > 0 &&
                    item.TravelTier == MapTravelTier.RouteOnly)
                .ToArray();
            foreach (MapLocationPlacement room in restrictedRooms)
            {
                MapAreaShape area = MapAreaCatalog.Find(room.LocationCode);
                Assert.That(area, Is.Not.Null, room.LocationCode);
                Assert.That(area.Deck, Is.EqualTo(room.Deck), room.LocationCode);
                Assert.That(area.EntranceAnchor.x, Is.InRange(0f, 1f));
                Assert.That(area.EntranceAnchor.y, Is.InRange(0f, 1f));
            }
        }

        [Test]
        public void AreaState_RequiresRevealAndClearsWhenAccessible()
        {
            MapAreaShape vault = MapAreaCatalog.Find("VAULT");
            Assert.That(
                MapAreaCatalog.ResolveState(
                    vault,
                    false,
                    true,
                    false,
                    false),
                Is.EqualTo(MapAreaVisualState.Hidden));
            Assert.That(
                MapAreaCatalog.ResolveState(
                    vault,
                    true,
                    true,
                    false,
                    false),
                Is.EqualTo(MapAreaVisualState.Restricted));
            Assert.That(
                MapAreaCatalog.ResolveState(
                    vault,
                    true,
                    true,
                    true,
                    false),
                Is.EqualTo(MapAreaVisualState.Accessible));
            Assert.That(
                MapAreaCatalog.ResolveState(
                    vault,
                    true,
                    true,
                    true,
                    true),
                Is.EqualTo(MapAreaVisualState.TemporarilyClosed));
        }

        [Test]
        public void ReplacementLayers_MatchCanvasAndImportContract()
        {
            foreach (int deck in new[] { 7, 8, 9, 10 })
            {
                TextureImporter baseline = null;
                foreach (string suffix in
                         new[] { "Base", "Restricted", "Technical" })
                {
                    string path =
                        $"{LayerRoot}/Deck{deck:00}_{suffix}.png";
                    Texture2D texture =
                        AssetDatabase.LoadAssetAtPath<Texture2D>(path);
                    TextureImporter importer =
                        AssetImporter.GetAtPath(path) as TextureImporter;
                    Assert.That(texture, Is.Not.Null, path);
                    Assert.That(texture.width, Is.EqualTo(1448), path);
                    Assert.That(texture.height, Is.EqualTo(1086), path);
                    Assert.That(importer, Is.Not.Null, path);
                    Assert.That(
                        importer.textureType,
                        Is.EqualTo(TextureImporterType.Sprite),
                        path);
                    Assert.That(importer.mipmapEnabled, Is.False, path);
                    Assert.That(importer.alphaIsTransparency, Is.True, path);
                    Assert.That(
                        importer.wrapMode,
                        Is.EqualTo(TextureWrapMode.Clamp),
                        path);
                    if (baseline != null)
                    {
                        Assert.That(
                            importer.spritePixelsPerUnit,
                            Is.EqualTo(baseline.spritePixelsPerUnit),
                            path);
                        Assert.That(
                            importer.spritePivot,
                            Is.EqualTo(baseline.spritePivot),
                            path);
                    }
                    baseline = importer;
                }
            }
        }

        [Test]
        public void SelfIntersection_IsRejected()
        {
            var shape = new MapAreaShape();
            shape.SetAuthoringData(
                "BROKEN",
                "교차 영역",
                8,
                new[]
                {
                    new Vector2(.2f, .2f),
                    new Vector2(.8f, .8f),
                    new Vector2(.2f, .8f),
                    new Vector2(.8f, .2f)
                },
                new Vector2(.5f, .5f),
                new Vector2(.5f, .5f),
                string.Empty,
                string.Empty,
                MapAreaVisualState.Restricted);

            Assert.That(
                MapAreaCatalog.IsValid(shape, out string error),
                Is.False);
            Assert.That(error, Does.Contain("자기 교차"));
        }
    }
}
