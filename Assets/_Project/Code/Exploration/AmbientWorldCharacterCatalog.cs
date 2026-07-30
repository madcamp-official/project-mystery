using System;
using System.Collections.Generic;
using UnityEngine;

namespace Wake.Exploration
{
    public readonly struct AmbientWorldCharacterAsset
    {
        public AmbientWorldCharacterAsset(
            string resourcePath,
            Rect uvRect,
            float cellAspectRatio,
            float visibleBottomMargin,
            float visibleTopMargin,
            float visibleLeftMargin = 0f,
            float visibleRightMargin = 0f)
        {
            ResourcePath = resourcePath ?? string.Empty;
            UvRect = uvRect;
            CellAspectRatio = Mathf.Max(0.01f, cellAspectRatio);
            VisibleBottomMargin = Mathf.Clamp(
                visibleBottomMargin,
                0f,
                0.25f);
            VisibleTopMargin = Mathf.Clamp(
                visibleTopMargin,
                0f,
                0.25f);
            VisibleLeftMargin = Mathf.Clamp(
                visibleLeftMargin,
                0f,
                0.45f);
            VisibleRightMargin = Mathf.Clamp(
                visibleRightMargin,
                0f,
                0.45f);
        }

        public string ResourcePath { get; }
        public Rect UvRect { get; }
        public float CellAspectRatio { get; }
        public float VisibleBottomMargin { get; }
        public float VisibleTopMargin { get; }
        public float VisibleLeftMargin { get; }
        public float VisibleRightMargin { get; }
        public float VisibleHorizontalSpan =>
            Mathf.Max(
                0.1f,
                1f - VisibleLeftMargin - VisibleRightMargin);
        public float VisibleVerticalSpan =>
            Mathf.Max(
                0.5f,
                1f - VisibleBottomMargin - VisibleTopMargin);
    }

    public readonly struct AmbientWorldPlacement
    {
        public AmbientWorldPlacement(
            Vector2 anchor,
            float normalizedHeight,
            bool mirror)
        {
            Anchor = new Vector2(
                Mathf.Clamp01(anchor.x),
                Mathf.Clamp01(anchor.y));
            NormalizedHeight = Mathf.Clamp(normalizedHeight, 0.2f, 0.9f);
            Mirror = mirror;
        }

        public Vector2 Anchor { get; }
        public float NormalizedHeight { get; }
        public bool Mirror { get; }
    }

    public static class AmbientWorldCharacterCatalog
    {
        private static readonly IReadOnlyDictionary<string, Vector2>
            ExpressionVisibleHorizontalMargins =
                new Dictionary<string, Vector2>(
                    StringComparer.OrdinalIgnoreCase)
                {
                    ["crew_engineer"] = new(.018f, .186f),
                    ["crew_security"] = new(.071f, .123f),
                    ["passenger_a"] = new(.038f, .061f),
                    ["passenger_b"] = new(.287f, .313f),
                    ["passenger_c"] = new(.026f, .089f),
                    ["passenger_d"] = new(.126f, .042f),
                    ["passenger_e"] = Vector2.zero,
                    ["passenger_f"] = new(.078f, .134f)
                };

        private static readonly IReadOnlyDictionary<string, Vector2>
            SpecialistVisibleHorizontalMargins =
                new Dictionary<string, Vector2>(
                    StringComparer.OrdinalIgnoreCase)
                {
                    ["dock_porter"] = new(.092f, .261f),
                    ["VIP_host"] = new(.109f, .196f),
                    ["ballroom_musician"] = new(.296f, .270f),
                    ["dining_sommelier"] = new(.200f, .261f),
                    ["atrium_guide"] = new(.158f, .262f),
                    ["security_operator"] = new(.207f, .344f),
                    ["rail_technician"] = new(.200f, 0f),
                    ["ship_medic"] = new(.189f, .395f),
                    ["ballast_controller"] = new(.174f, .329f),
                    ["chief_engineer"] = new(.240f, .384f),
                    ["suite_steward"] = new(.200f, .307f),
                    ["archivist"] = new(.178f, .340f),
                    ["robotics_tech"] = new(.199f, .332f)
                };

        private static readonly IReadOnlyDictionary<string, Vector2>
            MainVisibleHorizontalMargins =
                new Dictionary<string, Vector2>(
                    StringComparer.OrdinalIgnoreCase)
                {
                    ["adrian_vale"] = new(.225f, .283f),
                    ["claire_hawthorne"] = new(.273f, .324f),
                    ["daniel_mercer"] = new(.258f, .321f),
                    ["evelyn_shaw"] = new(.219f, .288f),
                    ["helena_ward"] = new(.321f, .341f),
                    ["marcus_bell"] = new(.234f, .222f),
                    ["owen_price"] = new(.237f, .296f),
                    ["richard_hawthorne"] = new(.173f, .293f),
                    ["thomas_reed"] = new(.202f, .291f)
                };

        private static readonly IReadOnlyDictionary<string, AmbientWorldCharacterAsset>
            Assets = new Dictionary<string, AmbientWorldCharacterAsset>(
                StringComparer.OrdinalIgnoreCase)
            {
                ["CREW_ATTENDANT"] = AtlasFigure(
                    "world_atlas_crew_passengers_ab",
                    textureWidth: 1774,
                    textureHeight: 887,
                    pixelRect: new RectInt(96, 108, 213, 728)),
                ["CREW_ATTENDANT_BALLROOM"] = AtlasFigure(
                    "world_atlas_crew_passengers_ab",
                    textureWidth: 1774,
                    textureHeight: 887,
                    pixelRect: new RectInt(96, 108, 213, 728)),
                ["CREW_ENGINEER"] = ExpressionFigure(
                    "crew_engineer", 0.6255f, 0.0416f, 0.0391f),
                ["CREW_ENGINEER_GENERATOR"] = ExpressionFigure(
                    "crew_engineer", 0.6255f, 0.0416f, 0.0391f),
                ["CREW_SECURITY"] = ExpressionFigure(
                    "crew_security", 0.7500f, 0.0442f, 0.0262f),
                ["CREW_SECURITY_DECK"] = ExpressionFigure(
                    "crew_security", 0.7500f, 0.0442f, 0.0262f),
                ["CREW_SECURITY_STAIRS"] = ExpressionFigure(
                    "crew_security", 0.7500f, 0.0442f, 0.0262f),
                ["CREW_SECURITY_VAULT"] = ExpressionFigure(
                    "crew_security", 0.7500f, 0.0442f, 0.0262f),
                ["VAULT_GUARD"] = ExpressionFigure(
                    "crew_security", 0.7500f, 0.0442f, 0.0262f),
                ["PASSENGER_A"] = ExpressionFigure(
                    "passenger_a", 0.6255f, 0.0000f, 0.0328f),
                ["PASSENGER_ATRIUM"] = ExpressionFigure(
                    "passenger_a", 0.6255f, 0.0000f, 0.0328f),
                ["PASSENGER_PROMENADE_2"] = ExpressionFigure(
                    "passenger_a", 0.6255f, 0.0000f, 0.0328f),
                ["PASSENGER_B"] = ExpressionFigure(
                    "passenger_b", 0.7500f, 0.0000f, 0.0200f),
                ["PASSENGER_C"] = ExpressionFigure(
                    "passenger_c", 0.6255f, 0.0277f, 0.0214f),
                ["PASSENGER_D"] = ExpressionFigure(
                    "passenger_d", 0.7500f, 0.0470f, 0.0193f),
                ["PASSENGER_NEWS"] = ExpressionFigure(
                    "passenger_d", 0.7500f, 0.0470f, 0.0193f),
                ["PASSENGER_PROMENADE"] = ExpressionFigure(
                    "passenger_d", 0.7500f, 0.0470f, 0.0193f),
                ["PASSENGER_E"] = ExpressionFigure(
                    "passenger_e", 0.6255f, 0.0328f, 0.0227f),
                ["PASSENGER_DECK"] = ExpressionFigure(
                    "passenger_e", 0.6255f, 0.0328f, 0.0227f),
                ["PASSENGER_F"] = ExpressionFigure(
                    "passenger_f", 0.7500f, 0.0055f, 0.0276f),
                ["DOCK_PORTER"] = Specialist("dock_porter"),
                ["VIP_HOST"] = Specialist("VIP_host"),
                ["BALLROOM_MUSICIAN"] = Specialist("ballroom_musician"),
                ["DINING_SOMMELIER"] = Specialist("dining_sommelier"),
                ["ATRIUM_GUIDE"] = Specialist("atrium_guide"),
                ["SECURITY_OPERATOR"] = Specialist("security_operator"),
                ["RAIL_TECHNICIAN"] = Specialist("rail_technician"),
                ["SHIP_MEDIC"] = Specialist("ship_medic"),
                ["BALLAST_CONTROLLER"] = Specialist("ballast_controller"),
                ["BALLAST_CONTROLLER_TANKS"] = Specialist("ballast_controller"),
                ["CHIEF_ENGINEER"] = Specialist("chief_engineer"),
                ["SUITE_STEWARD"] = Specialist("suite_steward"),
                ["ARCHIVIST"] = Specialist("archivist"),
                ["LAUNDRY_SUPERVISOR"] = Specialist(
                    "suite_steward", 0.08f, 0.08f),
                ["ROBOTICS_TECH"] = Specialist("robotics_tech"),
                ["WORKSHOP_MACHINIST"] = Specialist("chief_engineer"),
                ["ADRIAN"] = Main(
                    "adrian_vale", .706f, .098f, .025f),
                ["CLAIRE"] = Main(
                    "claire_hawthorne", .632f, .073f, .027f),
                ["DANIEL"] = Main(
                    "daniel_mercer", .839f, .047f, .024f),
                ["EVELYN"] = Main(
                    "evelyn_shaw", .666f, .053f, .031f),
                ["HELENA"] = Main(
                    "helena_ward", .666f, .036f, .033f),
                ["MARCUS"] = Main(
                    "marcus_bell", .663f, .052f, .046f),
                ["OWEN"] = Main(
                    "owen_price", .598f, .128f, .029f),
                ["RICHARD"] = Main(
                    "richard_hawthorne", .593f, .075f, .021f),
                ["THOMAS"] = Main(
                    "thomas_reed", .604f, .093f, .015f)
            };

        private static readonly HashSet<string> RightSideSingleLocations =
            new(StringComparer.OrdinalIgnoreCase)
            {
                "RICHARD_SUITE", "SECURITY", "SERVICE_RAIL",
                "BALLAST_CONTROL_ANNEX", "ENGINE_CONTROL", "CREW_STAIRS",
                "VAULT", "ARCHIVE", "SERVICE_HUB", "STABILIZERS",
                "BALLAST_TANKS", "GENERATOR", "WORKSHOP"
            };

        private static readonly HashSet<string> CompactLocations =
            new(StringComparer.OrdinalIgnoreCase)
            {
                "GANGWAY", "PROMENADE", "CREW_STAIRS", "LAUNDRY",
                "SERVICE_RAIL", "BALLAST_TANKS"
            };

        public static bool TryGetAsset(
            string speaker,
            out AmbientWorldCharacterAsset asset)
        {
            string key = speaker?.Trim() ?? string.Empty;
            return Assets.TryGetValue(key, out asset);
        }

        public static AmbientWorldPlacement GetPlacement(
            string locationCode,
            int index,
            int count)
        {
            string location = locationCode?.Trim().ToUpperInvariant() ?? "";
            int safeCount = Mathf.Max(1, count);
            int safeIndex = Mathf.Clamp(index, 0, safeCount - 1);
            float height = CompactLocations.Contains(location) ? 0.56f : 0.64f;

            if (safeCount == 1)
            {
                bool onRight = RightSideSingleLocations.Contains(location);
                return new AmbientWorldPlacement(
                    new Vector2(onRight ? 0.76f : 0.27f, 0.035f),
                    height,
                    mirror: onRight);
            }

            if (safeIndex == 0)
            {
                return new AmbientWorldPlacement(
                    new Vector2(0.23f, 0.035f),
                    height * 0.94f,
                    mirror: false);
            }

            if (safeIndex == safeCount - 1)
            {
                return new AmbientWorldPlacement(
                    new Vector2(0.77f, 0.035f),
                    height,
                    mirror: true);
            }

            return new AmbientWorldPlacement(
                new Vector2(0.51f, 0.045f),
                height * 0.88f,
                mirror: safeIndex % 2 == 0);
        }

        private static AmbientWorldCharacterAsset ExpressionFigure(
            string resourceName,
            float aspectRatio,
            float bottomMargin,
            float topMargin)
        {
            ExpressionVisibleHorizontalMargins.TryGetValue(
                resourceName,
                out Vector2 horizontalMargins);
            return new AmbientWorldCharacterAsset(
                $"AmbientCharacters/{resourceName}_expressions",
                new Rect(0f, 0f, 0.25f, 1f),
                aspectRatio,
                bottomMargin,
                topMargin,
                horizontalMargins.x,
                horizontalMargins.y);
        }

        private static AmbientWorldCharacterAsset AtlasFigure(
            string resourceName,
            int textureWidth,
            int textureHeight,
            RectInt pixelRect)
        {
            float safeWidth = Mathf.Max(1, textureWidth);
            float safeHeight = Mathf.Max(1, textureHeight);
            Rect uv = new(
                pixelRect.x / safeWidth,
                pixelRect.y / safeHeight,
                pixelRect.width / safeWidth,
                pixelRect.height / safeHeight);
            return new AmbientWorldCharacterAsset(
                $"AmbientCharacters/{resourceName}",
                uv,
                pixelRect.width / Mathf.Max(1f, pixelRect.height),
                visibleBottomMargin: 0f,
                visibleTopMargin: 0f);
        }

        private static AmbientWorldCharacterAsset Specialist(
            string resourceName,
            float bottomMargin = 0.02f,
            float topMargin = 0.02f)
        {
            SpecialistVisibleHorizontalMargins.TryGetValue(
                resourceName,
                out Vector2 horizontalMargins);
            return new AmbientWorldCharacterAsset(
                $"AmbientCharacters/{resourceName}",
                new Rect(0f, 0f, 1f, 1f),
                0.7f,
                bottomMargin,
                topMargin,
                horizontalMargins.x,
                horizontalMargins.y);
        }

        private static AmbientWorldCharacterAsset Main(
            string resourceName,
            float aspectRatio,
            float bottomMargin,
            float topMargin)
        {
            MainVisibleHorizontalMargins.TryGetValue(
                resourceName,
                out Vector2 horizontalMargins);
            return new AmbientWorldCharacterAsset(
                $"WorldMainCharacters/{resourceName}",
                new Rect(0f, 0f, 1f, 1f),
                aspectRatio,
                bottomMargin,
                topMargin,
                horizontalMargins.x,
                horizontalMargins.y);
        }

    }
}
