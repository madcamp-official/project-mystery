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

            yield return S(7, "CABIN_DANIEL", .142f, .443f,
                P(.067f, .337f), P(.226f, .337f),
                P(.226f, .548f), P(.066f, .548f),
                P(.056f, .516f), P(.053f, .444f),
                P(.058f, .378f));
            yield return S(7, "SERVICE7", .424f, .244f,
                P(.103f, .187f), P(.744f, .187f),
                P(.752f, .197f), P(.752f, .300f),
                P(.095f, .300f), P(.093f, .270f),
                P(.096f, .221f));
            yield return S(7, "ENGINE_CONTROL", .745f, .424f, true,
                P(.714f, .338f), P(.776f, .338f),
                P(.776f, .510f), P(.714f, .510f));
            yield return S(7, "BALLAST_CONTROL_ANNEX", .851f, .445f, true,
                P(.790f, .383f), P(.909f, .383f),
                P(.913f, .402f), P(.913f, .506f),
                P(.790f, .506f));
            yield return S(7, "CREW_STAIRS", .784f, .618f, true,
                P(.762f, .544f), P(.806f, .544f),
                P(.806f, .691f), P(.762f, .691f));
            yield return S(7, "SERVICE_RAIL", .821f, .277f, true,
                P(.752f, .188f), P(.779f, .195f),
                P(.831f, .221f), P(.879f, .263f),
                P(.906f, .306f), P(.915f, .341f),
                P(.795f, .341f), P(.786f, .325f),
                P(.781f, .306f), P(.752f, .306f));

            yield return S(8, "NEWS_LOUNGE", .208f, .328f,
                P(.141f, .210f), P(.355f, .207f),
                P(.355f, .407f), P(.346f, .425f),
                P(.332f, .439f), P(.329f, .444f),
                P(.075f, .444f),
                P(.055f, .424f), P(.050f, .380f),
                P(.052f, .298f), P(.070f, .250f),
                P(.100f, .222f));
            yield return S(8, "ATRIUM", .476f, .464f,
                P(.359f, .202f), P(.594f, .202f),
                P(.594f, .407f), P(.612f, .432f),
                P(.615f, .494f), P(.604f, .514f),
                P(.620f, .554f), P(.620f, .636f),
                P(.603f, .683f), P(.598f, .708f),
                P(.359f, .708f), P(.350f, .686f),
                P(.335f, .652f), P(.335f, .557f),
                P(.349f, .515f), P(.332f, .490f),
                P(.332f, .439f), P(.346f, .425f),
                P(.355f, .407f));
            yield return S(8, "MEDBAY", .718f, .324f,
                P(.598f, .202f), P(.835f, .203f),
                P(.836f, .447f), P(.620f, .447f),
                P(.620f, .432f), P(.598f, .407f));
            yield return S(8, "SECURITY", .665f, .474f, true,
                P(.620f, .452f), P(.710f, .452f),
                P(.710f, .496f), P(.620f, .496f));
            yield return S(8, "CABIN_CLAIRE", .728f, .605f,
                P(.620f, .500f), P(.837f, .500f),
                P(.838f, .675f), P(.827f, .699f),
                P(.804f, .712f), P(.620f, .712f));

            yield return S(9, "BALLROOM", .19f, .46f,
                P(.085f, .258f), P(.286f, .258f),
                P(.286f, .276f), P(.310f, .276f),
                P(.310f, .636f), P(.286f, .636f),
                P(.286f, .652f), P(.083f, .652f),
                P(.083f, .639f), P(.057f, .639f),
                P(.057f, .615f), P(.039f, .589f),
                P(.028f, .525f), P(.028f, .350f),
                P(.041f, .294f), P(.064f, .268f));
            yield return S(9, "DINING", .49f, .36f,
                P(.341f, .177f), P(.626f, .177f),
                P(.626f, .490f), P(.600f, .490f),
                P(.600f, .483f), P(.544f, .483f),
                P(.544f, .490f), P(.470f, .490f),
                P(.470f, .483f), P(.413f, .483f),
                P(.413f, .490f), P(.341f, .490f));
            yield return S(9, "PROMENADE", .50f, .785f, true,
                P(.143f, .760f), P(.200f, .760f),
                P(.232f, .773f), P(.773f, .773f),
                P(.812f, .760f), P(.834f, .736f),
                P(.844f, .746f), P(.840f, .777f),
                P(.814f, .798f), P(.226f, .800f),
                P(.190f, .790f), P(.157f, .788f),
                P(.143f, .778f));
            yield return S(9, "HORIZON", .85f, .44f,
                P(.784f, .243f), P(.827f, .226f),
                P(.891f, .242f), P(.934f, .278f),
                P(.960f, .333f), P(.971f, .414f),
                P(.968f, .523f), P(.949f, .578f),
                P(.909f, .623f), P(.857f, .646f),
                P(.811f, .630f), P(.778f, .595f),
                P(.760f, .552f), P(.760f, .323f),
                P(.770f, .270f));

            yield return S(10, "RICHARD_SUITE", .28f, .43f,
                P(.193f, .207f), P(.341f, .207f),
                P(.341f, .350f), P(.360f, .350f),
                P(.360f, .486f), P(.352f, .486f),
                P(.352f, .636f), P(.270f, .636f),
                P(.270f, .608f), P(.180f, .608f),
                P(.180f, .638f), P(.064f, .638f),
                P(.064f, .584f), P(.053f, .584f),
                P(.053f, .486f), P(.153f, .486f),
                P(.168f, .462f), P(.168f, .355f),
                P(.193f, .355f));
            yield return S(10, "VIP_LOUNGE", .13f, .34f, true,
                P(.087f, .225f), P(.185f, .225f),
                P(.185f, .350f), P(.168f, .350f),
                P(.168f, .458f), P(.153f, .476f),
                P(.105f, .476f), P(.070f, .450f),
                P(.054f, .397f), P(.052f, .305f),
                P(.066f, .258f));
            yield return S(10, "BRIDGE", .85f, .45f,
                P(.771f, .242f), P(.812f, .213f),
                P(.883f, .224f), P(.932f, .269f),
                P(.962f, .331f), P(.974f, .420f),
                P(.971f, .523f), P(.949f, .585f),
                P(.904f, .633f), P(.849f, .650f),
                P(.792f, .631f), P(.769f, .594f),
                P(.765f, .542f), P(.704f, .542f),
                P(.704f, .527f), P(.760f, .527f),
                P(.760f, .395f), P(.744f, .395f),
                P(.744f, .370f), P(.760f, .370f),
                P(.760f, .285f));
            yield return S(10, "VAULT", .18f, .71f,
                P(.135f, .655f), P(.230f, .655f),
                P(.230f, .769f), P(.157f, .769f),
                P(.135f, .742f));
            yield return S(10, "ARCHIVE", .43f, .28f,
                P(.374f, .207f), P(.502f, .207f),
                P(.502f, .351f), P(.484f, .351f),
                P(.484f, .345f), P(.376f, .345f),
                P(.376f, .326f), P(.371f, .326f),
                P(.371f, .225f), P(.374f, .225f));
            yield return S(10, "INTERVIEW", .63f, .28f,
                P(.592f, .207f), P(.671f, .207f),
                P(.671f, .354f), P(.652f, .354f),
                P(.652f, .347f), P(.595f, .347f),
                P(.595f, .354f), P(.587f, .354f),
                P(.587f, .221f), P(.592f, .221f));
            yield return S(10, "OPEN_DECK", .22f, .185f, true,
                P(.070f, .250f), P(.087f, .216f),
                P(.124f, .186f), P(.168f, .172f),
                P(.349f, .168f), P(.349f, .203f),
                P(.193f, .203f), P(.193f, .207f),
                P(.087f, .224f), P(.066f, .258f));
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
