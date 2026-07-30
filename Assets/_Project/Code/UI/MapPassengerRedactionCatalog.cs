using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace Wake.UI
{
    public sealed class MapPassengerRedaction
    {
        public MapPassengerRedaction(
            string id,
            int deck,
            string releaseSceneId,
            MapLayerMode minimumRevealLayer,
            IEnumerable<Vector2> polygon)
        {
            Id = id;
            Deck = deck;
            ReleaseSceneId = releaseSceneId ?? string.Empty;
            MinimumRevealLayer = minimumRevealLayer;
            Polygon = polygon?.ToArray() ?? Array.Empty<Vector2>();
        }

        public string Id { get; }
        public int Deck { get; }
        public string ReleaseSceneId { get; }
        public MapLayerMode MinimumRevealLayer { get; }
        public IReadOnlyList<Vector2> Polygon { get; }
        public bool IsPermanent => string.IsNullOrEmpty(ReleaseSceneId);
    }

    public static class MapPassengerRedactionCatalog
    {
        private static readonly MapPassengerRedaction[] Redactions =
        {
            R("D7_INTERNAL_DANIEL_ID", 7),
            R("D7_NORTH_TECHNICAL", 7, "D6-02", MapLayerMode.Technical,
                V(.390f, .202f), V(.555f, .202f),
                V(.555f, .232f), V(.390f, .232f)),

            R("D8_CLAIRE_NAME", 8, "D5-01", MapLayerMode.Passenger),
            R("D8_WRONG_PROMENADE_TOP", 8),
            R("D8_WRONG_PROMENADE_BOTTOM", 8),

            R("D9_BALLROOM_SERVICE", 9, "D6-02", MapLayerMode.Technical,
                V(.110f, .220f), V(.302f, .220f),
                V(.302f, .252f), V(.110f, .252f)),
            R("D9_INTERNAL_BALLROOM_ID", 9),
            R("D9_INTERNAL_DINING_ID", 9),
            R("D9_INTERNAL_HORIZON_ID", 9),
            R("D9_ATRIUM_CONNECTION_TEXT", 9),
            R("D9_KITCHEN", 9, "D1-04", MapLayerMode.Investigation,
                V(.490f, .222f), V(.620f, .222f),
                V(.620f, .254f), V(.490f, .254f)),
            R("D9_STAFF_AREAS", 9, "D1-04", MapLayerMode.Investigation,
                V(.665f, .490f), V(.770f, .490f),
                V(.770f, .522f), V(.665f, .522f)),

            R("D10_INTERNAL_SUITE_ID", 10),
            R("D10_ATRIUM_CONNECTION_TEXT", 10),
            R("D10_SERVICE_ACCESS", 10, "D6-02", MapLayerMode.Technical,
                V(.490f, .650f), V(.625f, .650f),
                V(.625f, .682f), V(.490f, .682f)),
            R("D10_BRIDGE_NAME", 10, "D3-03", MapLayerMode.Passenger),
            R("D10_INTERVIEW_NAME", 10, "D5-03", MapLayerMode.Passenger),
            R("D10_ARCHIVE_NAME", 10, "D7-03", MapLayerMode.Passenger)
        };

        public static IReadOnlyList<MapPassengerRedaction> All => Redactions;

        public static IReadOnlyList<MapPassengerRedaction> ForDeck(int deck) =>
            Redactions.Where(item => item.Deck == deck).ToArray();

        public static bool ShouldRender(
            MapPassengerRedaction redaction,
            MapLayerMode layer,
            IEnumerable<string> completedSceneIds)
        {
            if (redaction == null)
                return false;
            if (redaction.IsPermanent)
                return true;
            bool released = (completedSceneIds ?? Array.Empty<string>())
                .Contains(
                    redaction.ReleaseSceneId,
                    StringComparer.OrdinalIgnoreCase);
            return !released || layer < redaction.MinimumRevealLayer;
        }

        private static MapPassengerRedaction R(
            string id,
            int deck,
            string releaseSceneId = "",
            MapLayerMode revealLayer = MapLayerMode.Technical,
            params Vector2[] topLeftPolygon)
        {
            Vector2[] polygon = topLeftPolygon.Length > 0
                ? topLeftPolygon.Select(FlipY).ToArray()
                : Preset(id).Select(FlipY).ToArray();
            return new MapPassengerRedaction(
                id,
                deck,
                releaseSceneId,
                revealLayer,
                polygon);
        }

        private static IReadOnlyList<Vector2> Preset(string id) =>
            id switch
            {
                "D7_INTERNAL_DANIEL_ID" => Rect(.132f, .456f, .225f, .489f),
                "D7_NEUTRAL_DECK_TITLE" => Rect(.20f, .04f, .82f, .105f),
                "D8_CLAIRE_NAME" => Rect(.697f, .611f, .814f, .650f),
                "D8_WRONG_PROMENADE_TOP" => Rect(.456f, .163f, .542f, .193f),
                "D8_WRONG_PROMENADE_BOTTOM" => Rect(.458f, .704f, .545f, .735f),
                "D8_NEUTRAL_DECK_TITLE" => Rect(.234f, .045f, .784f, .105f),
                "D9_INTERNAL_BALLROOM_ID" => Rect(.159f, .425f, .253f, .461f),
                "D9_INTERNAL_DINING_ID" => Rect(.447f, .398f, .531f, .432f),
                "D9_INTERNAL_HORIZON_ID" => Rect(.817f, .439f, .888f, .475f),
                "D9_ATRIUM_CONNECTION_TEXT" =>
                    Rect(.445f, .560f, .525f, .630f),
                "D10_INTERNAL_SUITE_ID" => Rect(.218f, .394f, .300f, .431f),
                "D10_ATRIUM_CONNECTION_TEXT" =>
                    Rect(.470f, .400f, .550f, .470f),
                "D10_BRIDGE_NAME" => Rect(.790f, .405f, .846f, .445f),
                "D10_INTERVIEW_NAME" => Rect(.596f, .210f, .675f, .298f),
                "D10_ARCHIVE_NAME" => Rect(.405f, .225f, .478f, .275f),
                _ => Array.Empty<Vector2>()
            };

        private static Vector2[] Rect(
            float left,
            float top,
            float right,
            float bottom) =>
            new[]
            {
                V(left, top),
                V(right, top),
                V(right, bottom),
                V(left, bottom)
            };

        private static Vector2 V(float x, float y) => new(x, y);
        private static Vector2 FlipY(Vector2 point) =>
            new(point.x, 1f - point.y);
    }
}
