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
        private const string AtlasA =
            "AmbientCharacters/world_atlas_crew_passengers_ab";
        private const string AtlasB =
            "AmbientCharacters/world_atlas_passengers_cdef";
        private const string PublicSpecialists =
            "AmbientCharacters/world_atlas_public_specialists";
        private const string OperationsSpecialists =
            "AmbientCharacters/world_atlas_operations_specialists";
        private const string ServiceSpecialists =
            "AmbientCharacters/world_atlas_service_specialists";

        private static readonly IReadOnlyDictionary<string, AmbientWorldCharacterAsset>
            Assets = new Dictionary<string, AmbientWorldCharacterAsset>(
                StringComparer.OrdinalIgnoreCase)
            {
                ["CREW_ATTENDANT"] = A(0),
                ["CREW_ENGINEER"] = A(1),
                ["CREW_SECURITY"] = A(2),
                ["PASSENGER_A"] = A(3),
                ["PASSENGER_B"] = A(4),
                ["PASSENGER_C"] = B(0),
                ["PASSENGER_D"] = B(1),
                ["PASSENGER_E"] = B(2),
                ["PASSENGER_F"] = B(3),
                ["DOCK_PORTER"] = Specialist(PublicSpecialists, 0),
                ["VIP_HOST"] = Specialist(PublicSpecialists, 1),
                ["BALLROOM_MUSICIAN"] = Specialist(PublicSpecialists, 2),
                ["DINING_SOMMELIER"] = Specialist(PublicSpecialists, 3),
                ["ATRIUM_GUIDE"] = Specialist(PublicSpecialists, 4),
                ["SECURITY_OPERATOR"] =
                    Specialist(OperationsSpecialists, 0),
                ["RAIL_TECHNICIAN"] =
                    Specialist(OperationsSpecialists, 1),
                ["SHIP_MEDIC"] = Specialist(OperationsSpecialists, 2),
                ["BALLAST_CONTROLLER"] =
                    Specialist(OperationsSpecialists, 3),
                ["CHIEF_ENGINEER"] =
                    Specialist(OperationsSpecialists, 4),
                ["SUITE_STEWARD"] = Specialist(ServiceSpecialists, 0),
                ["ARCHIVIST"] = Specialist(ServiceSpecialists, 1),
                ["LAUNDRY_SUPERVISOR"] =
                    Specialist(ServiceSpecialists, 2),
                ["ROBOTICS_TECH"] = Specialist(ServiceSpecialists, 3),
                ["WORKSHOP_MACHINIST"] =
                    Specialist(ServiceSpecialists, 4),
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

        private static AmbientWorldCharacterAsset A(int column)
        {
            const float width = 0.2f;
            float[] bottomMargins =
                { 0.1229f, 0.1387f, 0.1048f, 0.1251f, 0.0800f };
            float[] topMargins =
                { 0.0586f, 0.0857f, 0.0496f, 0.0958f, 0.0609f };
            return new AmbientWorldCharacterAsset(
                AtlasA,
                new Rect(column * width, 0f, width, 1f),
                0.4f,
                bottomMargins[column],
                topMargins[column]);
        }

        private static AmbientWorldCharacterAsset B(int column)
        {
            const float width = 0.25f;
            float[] bottomMargins =
                { 0.0000f, 0.0688f, 0.0575f, 0.0609f };
            float[] topMargins =
                { 0.0304f, 0.0361f, 0.0406f, 0.0428f };
            return new AmbientWorldCharacterAsset(
                AtlasB,
                new Rect(column * width, 0f, width, 1f),
                0.5f,
                bottomMargins[column],
                topMargins[column]);
        }

        private static AmbientWorldCharacterAsset Specialist(
            string atlas,
            int column)
        {
            const float width = 0.2f;
            Vector2 visibleMargins = VisibleMargins(atlas, column);
            return new AmbientWorldCharacterAsset(
                atlas,
                new Rect(column * width, 0f, width, 1f),
                0.4f,
                visibleMargins.x,
                visibleMargins.y);
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

        private static Vector2 VisibleMargins(string atlas, int column)
        {
            float[] bottom = atlas == PublicSpecialists
                ? new[] { .1094f, .1105f, .1026f, .1060f, .1127f }
                : atlas == OperationsSpecialists
                    ? new[] { .1330f, .1319f, .1206f, .1229f, .1184f }
                    : new[] { .0992f, .1094f, .1094f, .1003f, .0936f };
            float[] top = atlas == PublicSpecialists
                ? new[] { .0902f, .0992f, .0710f, .0913f, .1037f }
                : atlas == OperationsSpecialists
                    ? new[] { .0902f, .0924f, .1184f, .0879f, .0800f }
                    : new[] { .0710f, .0958f, .0643f, .0496f, .0586f };
            return new Vector2(bottom[column], top[column]);
        }
    }
}
