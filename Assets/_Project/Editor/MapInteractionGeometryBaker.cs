using System;
using System.Collections.Generic;
using System.Linq;
using UnityEditor;
using UnityEngine;
using Wake.UI;

namespace Wake.Editor
{
    public static class MapInteractionGeometryBaker
    {
        private const string NodeFolder =
            "Assets/_Project/Resources/Maps/MapNodes";
        private const string MaskFolder =
            "Assets/_Project/Resources/Maps/RoomMasks";

        [MenuItem("Wake/Map/Bake Interaction Geometry")]
        public static void Bake()
        {
            EnsureFolder(NodeFolder);
            EnsureFolder(MaskFolder);

            foreach (IGrouping<int, Seed> deckGroup in Seeds()
                         .GroupBy(seed => seed.Deck)
                         .OrderBy(group => group.Key))
            {
                int deck = deckGroup.Key;
                string prefix = deck == 0 ? "Port" : $"Deck{deck:00}";
                Sprite source = Resources.Load<Sprite>(
                    MapDeckCatalog.ResourceKey(
                        deck,
                        MapLayerMode.Passenger));
                if (source == null)
                {
                    throw new InvalidOperationException(
                        $"{prefix} Passenger Base Sprite를 찾을 수 없습니다.");
                }

                string sourcePath = AssetDatabase.GetAssetPath(source);
                string sourceHash =
                    AssetDatabase.GetAssetDependencyHash(sourcePath).ToString();
                MapNodesAsset nodes = LoadOrCreate<MapNodesAsset>(
                    $"{NodeFolder}/{prefix}_MapNodes.asset");
                RoomMasksAsset masks = LoadOrCreate<RoomMasksAsset>(
                    $"{MaskFolder}/{prefix}_RoomMasks.asset");

                nodes.SetSource(deck, source, sourceHash);
                nodes.ReplaceAll(deckGroup
                    .OrderBy(seed => seed.Code, StringComparer.Ordinal)
                    .Select(seed =>
                    {
                        var node = new MapLocationNode();
                        node.SetAuthoringData(
                            seed.Code,
                            BottomLeft(seed.NodeTopLeft));
                        return node;
                    }));
                masks.SetSource(deck, source, sourceHash);
                masks.ReplaceAll(deckGroup
                    .OrderBy(seed => seed.Code, StringComparer.Ordinal)
                    .Select(seed =>
                    {
                        var mask = new MapRoomMask();
                        mask.SetAuthoringData(
                            seed.Code,
                            seed.TopLeftPolygon.Select(BottomLeft),
                            seed.CorrectiveArtworkRecommended);
                        return mask;
                    }));

                EditorUtility.SetDirty(nodes);
                EditorUtility.SetDirty(masks);
            }

            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
            MapInteractionGeometryCatalog.InvalidateCache();
            if (!MapInteractionGeometryCatalog.Validate(
                    out IReadOnlyList<string> diagnostics))
            {
                throw new InvalidOperationException(
                    string.Join(Environment.NewLine, diagnostics));
            }

            Debug.Log(
                "Map interaction geometry baked: " +
                $"{MapDeckCatalog.All.Count} active locations, Deck 6 excluded.");
        }

        private static IEnumerable<Seed> Seeds()
        {
            yield return S(0, "PORT", .20f, .50f,
                P(.05f, .13f), P(.35f, .13f), P(.36f, .19f),
                P(.36f, .84f), P(.35f, .89f), P(.05f, .89f));
            yield return S(0, "GANGWAY", .58f, .49f,
                P(.38f, .57f), P(.77f, .36f),
                P(.80f, .41f), P(.40f, .65f));

            yield return S(7, "CABIN_DANIEL", .15f, .45f,
                P(.06f, .34f), P(.23f, .34f),
                P(.23f, .55f), P(.06f, .55f));
            yield return S(7, "SERVICE7", .42f, .25f,
                P(.10f, .18f), P(.73f, .18f), P(.78f, .22f),
                P(.74f, .31f), P(.11f, .31f), P(.08f, .26f));
            yield return S(7, "ENGINE_CONTROL", .77f, .46f,
                P(.70f, .36f), P(.85f, .36f),
                P(.85f, .55f), P(.70f, .55f));
            yield return S(7, "BALLAST_CONTROL_ANNEX", .90f, .46f,
                P(.86f, .36f), P(.95f, .40f),
                P(.95f, .54f), P(.86f, .56f));
            yield return S(7, "CREW_STAIRS", .78f, .66f,
                P(.73f, .57f), P(.84f, .57f),
                P(.84f, .75f), P(.73f, .75f));
            yield return S(7, "SERVICE_RAIL", .87f, .27f,
                P(.80f, .18f), P(.93f, .21f),
                P(.94f, .34f), P(.81f, .34f));

            yield return S(8, "ATRIUM", .48f, .48f,
                P(.36f, .20f), P(.59f, .20f), P(.59f, .45f),
                P(.59f, .52f), P(.59f, .70f), P(.36f, .70f),
                P(.33f, .58f), P(.36f, .45f));
            yield return S(8, "NEWS_LOUNGE", .21f, .33f,
                P(.07f, .20f), P(.35f, .20f),
                P(.35f, .45f), P(.07f, .45f));
            yield return S(8, "SECURITY", .71f, .47f, true,
                P(.59f, .45f), P(.84f, .45f),
                P(.84f, .49f), P(.59f, .49f));
            yield return S(8, "MEDBAY", .71f, .33f,
                P(.59f, .20f), P(.84f, .20f),
                P(.84f, .45f), P(.59f, .45f));
            yield return S(8, "CABIN_CLAIRE", .72f, .60f,
                P(.61f, .49f), P(.84f, .49f),
                P(.84f, .71f), P(.61f, .71f));

            yield return S(9, "BALLROOM", .19f, .46f,
                P(.05f, .24f), P(.32f, .24f),
                P(.32f, .66f), P(.06f, .66f));
            yield return S(9, "DINING", .49f, .39f,
                P(.34f, .18f), P(.63f, .18f),
                P(.63f, .51f), P(.34f, .51f));
            yield return S(9, "PROMENADE", .50f, .74f, true,
                P(.14f, .68f), P(.84f, .68f),
                P(.84f, .79f), P(.14f, .79f));
            yield return S(9, "HORIZON", .85f, .43f,
                P(.75f, .25f), P(.88f, .21f), P(.96f, .32f),
                P(.96f, .58f), P(.87f, .66f), P(.75f, .57f));

            yield return S(10, "RICHARD_SUITE", .27f, .43f,
                P(.17f, .20f), P(.35f, .20f), P(.35f, .64f),
                P(.08f, .64f), P(.08f, .43f), P(.17f, .43f));
            yield return S(10, "VIP_LOUNGE", .13f, .33f, true,
                P(.08f, .22f), P(.17f, .22f),
                P(.17f, .43f), P(.08f, .43f));
            yield return S(10, "BRIDGE", .85f, .46f,
                P(.78f, .30f), P(.91f, .34f), P(.95f, .46f),
                P(.94f, .62f), P(.79f, .62f));
            yield return S(10, "VAULT", .18f, .70f,
                P(.11f, .64f), P(.28f, .64f), P(.29f, .75f),
                P(.12f, .76f), P(.08f, .70f));
            yield return S(10, "ARCHIVE", .42f, .34f,
                P(.35f, .22f), P(.50f, .22f),
                P(.50f, .49f), P(.35f, .49f));
            yield return S(10, "INTERVIEW", .63f, .28f,
                P(.58f, .20f), P(.68f, .20f),
                P(.68f, .36f), P(.58f, .36f));
            yield return S(10, "OPEN_DECK", .22f, .18f, true,
                P(.10f, .16f), P(.34f, .16f),
                P(.34f, .20f), P(.10f, .20f));
        }

        private static T LoadOrCreate<T>(string path)
            where T : ScriptableObject
        {
            T asset = AssetDatabase.LoadAssetAtPath<T>(path);
            if (asset != null)
                return asset;

            asset = ScriptableObject.CreateInstance<T>();
            AssetDatabase.CreateAsset(asset, path);
            return asset;
        }

        private static void EnsureFolder(string path)
        {
            string current = "Assets";
            foreach (string part in path.Split('/').Skip(1))
            {
                string next = $"{current}/{part}";
                if (!AssetDatabase.IsValidFolder(next))
                    AssetDatabase.CreateFolder(current, part);
                current = next;
            }
        }

        private static Vector2 BottomLeft(Vector2 topLeft) =>
            new(topLeft.x, 1f - topLeft.y);

        private static Vector2 P(float x, float y) => new(x, y);

        private static Seed S(
            int deck,
            string code,
            float nodeX,
            float nodeY,
            params Vector2[] polygon) =>
            new(deck, code, new Vector2(nodeX, nodeY), false, polygon);

        private static Seed S(
            int deck,
            string code,
            float nodeX,
            float nodeY,
            bool artworkRecommended,
            params Vector2[] polygon) =>
            new(
                deck,
                code,
                new Vector2(nodeX, nodeY),
                artworkRecommended,
                polygon);

        private sealed class Seed
        {
            public Seed(
                int deck,
                string code,
                Vector2 nodeTopLeft,
                bool correctiveArtworkRecommended,
                Vector2[] topLeftPolygon)
            {
                Deck = deck;
                Code = code;
                NodeTopLeft = nodeTopLeft;
                CorrectiveArtworkRecommended =
                    correctiveArtworkRecommended;
                TopLeftPolygon = topLeftPolygon;
            }

            public int Deck { get; }
            public string Code { get; }
            public Vector2 NodeTopLeft { get; }
            public bool CorrectiveArtworkRecommended { get; }
            public IReadOnlyList<Vector2> TopLeftPolygon { get; }
        }
    }
}
