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
            float visibleBottomMargin)
        {
            ResourcePath = resourcePath ?? string.Empty;
            UvRect = uvRect;
            CellAspectRatio = Mathf.Max(0.01f, cellAspectRatio);
            VisibleBottomMargin = Mathf.Clamp(
                visibleBottomMargin,
                0f,
                0.25f);
        }

        public string ResourcePath { get; }
        public Rect UvRect { get; }
        public float CellAspectRatio { get; }
        public float VisibleBottomMargin { get; }
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
                    Specialist(ServiceSpecialists, 4)
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
            return new AmbientWorldCharacterAsset(
                AtlasA,
                new Rect(column * width, 0f, width, 1f),
                0.4f,
                bottomMargins[column]);
        }

        private static AmbientWorldCharacterAsset B(int column)
        {
            const float width = 0.25f;
            float[] bottomMargins =
                { 0.0000f, 0.0688f, 0.0575f, 0.0609f };
            return new AmbientWorldCharacterAsset(
                AtlasB,
                new Rect(column * width, 0f, width, 1f),
                0.5f,
                bottomMargins[column]);
        }

        private static AmbientWorldCharacterAsset Specialist(
            string atlas,
            int column)
        {
            const float width = 0.2f;
            float visibleBottomMargin = BottomMargin(atlas, column);
            return new AmbientWorldCharacterAsset(
                atlas,
                new Rect(column * width, 0f, width, 1f),
                0.4f,
                visibleBottomMargin);
        }

        private static float BottomMargin(string atlas, int column)
        {
            float[] values = atlas == PublicSpecialists
                ? new[] { .1094f, .1105f, .1026f, .1060f, .1127f }
                : atlas == OperationsSpecialists
                    ? new[] { .1330f, .1319f, .1206f, .1229f, .1184f }
                    : new[] { .0992f, .1094f, .1094f, .1003f, .0936f };
            return values[column];
        }
    }
}
