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
            float visibleTopMargin)
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
        }

        public string ResourcePath { get; }
        public Rect UvRect { get; }
        public float CellAspectRatio { get; }
        public float VisibleBottomMargin { get; }
        public float VisibleTopMargin { get; }
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
        private static readonly IReadOnlyDictionary<string, AmbientWorldCharacterAsset>
            Assets = new Dictionary<string, AmbientWorldCharacterAsset>(
                StringComparer.OrdinalIgnoreCase)
            {
                ["CREW_ATTENDANT"] = ExpressionFigure(
                    "crew_attendant", 0.6483f, 0.1309f, 0.0270f),
                ["CREW_ENGINEER"] = ExpressionFigure(
                    "crew_engineer", 0.6255f, 0.0416f, 0.0391f),
                ["CREW_SECURITY"] = ExpressionFigure(
                    "crew_security", 0.7500f, 0.0442f, 0.0262f),
                ["PASSENGER_A"] = ExpressionFigure(
                    "passenger_a", 0.6255f, 0.0000f, 0.0328f),
                ["PASSENGER_B"] = ExpressionFigure(
                    "passenger_b", 0.7500f, 0.0000f, 0.0166f),
                ["PASSENGER_C"] = ExpressionFigure(
                    "passenger_c", 0.6255f, 0.0277f, 0.0214f),
                ["PASSENGER_D"] = ExpressionFigure(
                    "passenger_d", 0.7500f, 0.0470f, 0.0193f),
                ["PASSENGER_E"] = ExpressionFigure(
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
                ["CHIEF_ENGINEER"] = Specialist("chief_engineer"),
                ["SUITE_STEWARD"] = Specialist("suite_steward"),
                ["ARCHIVIST"] = Specialist("archivist"),
                ["LAUNDRY_SUPERVISOR"] = Specialist("suite_steward"),
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
            return new AmbientWorldCharacterAsset(
                $"AmbientCharacters/{resourceName}_expressions",
                new Rect(0f, 0f, 0.25f, 1f),
                aspectRatio,
                bottomMargin,
                topMargin);
        }

        private static AmbientWorldCharacterAsset Specialist(string resourceName)
        {
            return new AmbientWorldCharacterAsset(
                $"AmbientCharacters/{resourceName}",
                new Rect(0f, 0f, 1f, 1f),
                0.7f,
                0.02f,
                0.02f);
        }

        private static AmbientWorldCharacterAsset Main(
            string resourceName,
            float aspectRatio,
            float bottomMargin,
            float topMargin)
        {
            return new AmbientWorldCharacterAsset(
                $"WorldMainCharacters/{resourceName}",
                new Rect(0f, 0f, 1f, 1f),
                aspectRatio,
                bottomMargin,
                topMargin);
        }

    }
}
