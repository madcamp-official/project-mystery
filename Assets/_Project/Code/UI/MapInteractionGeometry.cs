using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace Wake.UI
{
    [Serializable]
    public sealed class MapLocationNode
    {
        [SerializeField] private string locationCode = string.Empty;
        [SerializeField] private Vector2 normalizedPosition = new(.5f, .5f);
        [SerializeField] private string entryPointId = string.Empty;

        public string LocationCode => locationCode;
        public Vector2 NormalizedPosition => normalizedPosition;
        public string EntryPointId => entryPointId;

        public void SetAuthoringData(
            string code,
            Vector2 position,
            string authoredEntryPointId = "")
        {
            locationCode = code?.Trim().ToUpperInvariant() ?? string.Empty;
            normalizedPosition = position;
            entryPointId = authoredEntryPointId?.Trim() ?? string.Empty;
        }
    }

    [Serializable]
    public sealed class MapRoomMask
    {
        [SerializeField] private string locationCode = string.Empty;
        [SerializeField] private Vector2[] polygon = Array.Empty<Vector2>();
        [SerializeField] private bool correctiveArtworkRecommended;

        public string LocationCode => locationCode;
        public IReadOnlyList<Vector2> Polygon => polygon;
        public bool CorrectiveArtworkRecommended =>
            correctiveArtworkRecommended;

        public void SetAuthoringData(
            string code,
            IEnumerable<Vector2> points,
            bool artworkRecommended = false)
        {
            locationCode = code?.Trim().ToUpperInvariant() ?? string.Empty;
            polygon = points?.ToArray() ?? Array.Empty<Vector2>();
            correctiveArtworkRecommended = artworkRecommended;
        }
    }

    public abstract class MapGeometryAsset : ScriptableObject
    {
        [SerializeField] private int deck;
        [SerializeField] private Sprite sourceMap;
        [SerializeField] private string authoredSourceHash = string.Empty;

        public int Deck => deck;
        public Sprite SourceMap => sourceMap;
        public string AuthoredSourceHash => authoredSourceHash;

        public void SetSource(
            int authoredDeck,
            Sprite authoredSourceMap,
            string sourceHash)
        {
            deck = authoredDeck;
            sourceMap = authoredSourceMap;
            authoredSourceHash = sourceHash ?? string.Empty;
        }
    }

    public static class MapInteractionGeometryCatalog
    {
        private static readonly int[] ActiveDecks = { 0, 7, 8, 9, 10 };
        private static IReadOnlyList<MapNodesAsset> nodeAssets;
        private static IReadOnlyList<RoomMasksAsset> maskAssets;

        public static IReadOnlyList<MapNodesAsset> NodeAssets =>
            nodeAssets ??= LoadAssets<MapNodesAsset>("Maps/MapNodes");

        public static IReadOnlyList<RoomMasksAsset> MaskAssets =>
            maskAssets ??= LoadAssets<RoomMasksAsset>("Maps/RoomMasks");

        public static IReadOnlyList<MapLocationNode> NodesForDeck(int deck) =>
            NodeAssets.FirstOrDefault(asset => asset.Deck == deck)?.Nodes ??
            Array.Empty<MapLocationNode>();

        public static IReadOnlyList<MapRoomMask> MasksForDeck(int deck) =>
            MaskAssets.FirstOrDefault(asset => asset.Deck == deck)?.Masks ??
            Array.Empty<MapRoomMask>();

        public static bool TryGetNode(
            string locationCode,
            out MapLocationNode node)
        {
            string code = locationCode?.Trim() ?? string.Empty;
            node = NodeAssets
                .SelectMany(asset => asset.Nodes)
                .FirstOrDefault(item => string.Equals(
                    item.LocationCode,
                    code,
                    StringComparison.OrdinalIgnoreCase));
            return node != null;
        }

        public static bool TryGetMask(
            string locationCode,
            out MapRoomMask mask)
        {
            string code = locationCode?.Trim() ?? string.Empty;
            mask = MaskAssets
                .SelectMany(asset => asset.Masks)
                .FirstOrDefault(item => string.Equals(
                    item.LocationCode,
                    code,
                    StringComparison.OrdinalIgnoreCase));
            return mask != null;
        }

        public static bool Validate(out IReadOnlyList<string> diagnostics)
        {
            var errors = new List<string>();
            string[] activeCodes = MapDeckCatalog.All
                .Select(item => item.LocationCode)
                .OrderBy(code => code, StringComparer.Ordinal)
                .ToArray();
            string[] nodeCodes = NodeAssets
                .SelectMany(asset => asset.Nodes)
                .Select(item => item.LocationCode)
                .OrderBy(code => code, StringComparer.Ordinal)
                .ToArray();
            string[] maskCodes = MaskAssets
                .SelectMany(asset => asset.Masks)
                .Select(item => item.LocationCode)
                .OrderBy(code => code, StringComparer.Ordinal)
                .ToArray();

            if (!activeCodes.SequenceEqual(nodeCodes))
                errors.Add("활성 장소와 지도 노드 집합이 일치하지 않습니다.");
            if (!activeCodes.SequenceEqual(maskCodes))
                errors.Add("활성 장소와 방 클릭 영역 집합이 일치하지 않습니다.");

            foreach (int deck in ActiveDecks)
            {
                int nodeAssetCount = NodeAssets.Count(asset =>
                    asset.Deck == deck);
                int maskAssetCount = MaskAssets.Count(asset =>
                    asset.Deck == deck);
                if (nodeAssetCount != 1 || maskAssetCount != 1)
                {
                    errors.Add(
                        $"Deck {deck}: 노드/마스크 에셋이 각각 하나여야 합니다.");
                }
            }

            foreach (MapLocationPlacement placement in MapDeckCatalog.All)
            {
                if (!TryGetNode(placement.LocationCode, out MapLocationNode node) ||
                    !TryGetMask(placement.LocationCode, out MapRoomMask mask))
                {
                    continue;
                }
                if (nodeAssets.First(asset =>
                        asset.Nodes.Contains(node)).Deck != placement.Deck ||
                    maskAssets.First(asset =>
                        asset.Masks.Contains(mask)).Deck != placement.Deck)
                {
                    errors.Add($"{placement.LocationCode}: 층 정보가 다릅니다.");
                }
                if (!MapPolygonUtility.Contains(
                        mask.Polygon,
                        node.NormalizedPosition))
                {
                    errors.Add(
                        $"{placement.LocationCode}: 노드가 자기 방 영역 밖입니다.");
                }
                if (mask.Polygon.Count < 3 ||
                    mask.Polygon.Any(point =>
                        !MapPolygonUtility.IsNormalized(point)) ||
                    Mathf.Abs(MapPolygonUtility.SignedArea(mask.Polygon)) <
                    .0001f ||
                    MapPolygonUtility.SelfIntersects(mask.Polygon))
                {
                    errors.Add(
                        $"{placement.LocationCode}: 방 영역이 유효하지 않습니다.");
                }
            }

            foreach (RoomMasksAsset asset in MaskAssets)
            {
                for (int first = 0; first < asset.Masks.Count; first++)
                {
                    for (int second = first + 1;
                         second < asset.Masks.Count;
                         second++)
                    {
                        if (MapPolygonUtility.OverlapsInterior(
                                asset.Masks[first].Polygon,
                                asset.Masks[second].Polygon))
                        {
                            errors.Add(
                                $"Deck {asset.Deck}: " +
                                $"{asset.Masks[first].LocationCode}와 " +
                                $"{asset.Masks[second].LocationCode} 영역이 겹칩니다.");
                        }
                    }
                }
            }

            diagnostics = errors;
            return errors.Count == 0;
        }

        public static void InvalidateCache()
        {
            nodeAssets = null;
            maskAssets = null;
        }

        private static IReadOnlyList<T> LoadAssets<T>(string path)
            where T : MapGeometryAsset
        {
            return Resources.LoadAll<T>(path)
                .OrderBy(asset => asset.Deck)
                .ToArray();
        }
    }
}
