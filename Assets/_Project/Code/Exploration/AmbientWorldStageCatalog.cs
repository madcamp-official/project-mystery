using System;
using System.Collections.Generic;
using UnityEngine;

namespace Wake.Exploration
{
    public readonly struct AmbientWorldStageProfile
    {
        public AmbientWorldStageProfile(
            Vector2 anchor,
            float normalizedHeight,
            bool mirror,
            Color lightTint,
            Vector2 shadowDirection,
            float shadowOpacity,
            float groundShadowScale)
        {
            Anchor = new Vector2(
                Mathf.Clamp01(anchor.x),
                Mathf.Clamp01(anchor.y));
            NormalizedHeight = Mathf.Clamp(
                normalizedHeight,
                0.2f,
                0.9f);
            Mirror = mirror;
            LightTint = lightTint;
            ShadowDirection = shadowDirection;
            ShadowOpacity = Mathf.Clamp01(shadowOpacity);
            GroundShadowScale = Mathf.Clamp(
                groundShadowScale,
                0.25f,
                1.2f);
        }

        public Vector2 Anchor { get; }
        public float NormalizedHeight { get; }
        public bool Mirror { get; }
        public Color LightTint { get; }
        public Vector2 ShadowDirection { get; }
        public float ShadowOpacity { get; }
        public float GroundShadowScale { get; }
    }

    public readonly struct AmbientWorldStageRecord
    {
        public AmbientWorldStageRecord(
            string location,
            string speaker,
            AmbientWorldStageProfile profile)
        {
            Location = location ?? string.Empty;
            Speaker = speaker ?? string.Empty;
            Profile = profile;
        }

        public string Location { get; }
        public string Speaker { get; }
        public AmbientWorldStageProfile Profile { get; }
    }

    public static class AmbientWorldStageCatalog
    {
        private static readonly Color Daylight =
            new Color32(255, 241, 218, 255);
        private static readonly Color WarmInterior =
            new Color32(255, 216, 172, 255);
        private static readonly Color AmberMachinery =
            new Color32(225, 174, 119, 255);
        private static readonly Color CoolMachinery =
            new Color32(175, 217, 218, 255);
        private static readonly Color LuxuryInterior =
            new Color32(231, 204, 214, 255);
        private static readonly Color NeutralInterior =
            new Color32(224, 225, 216, 255);

        private static readonly AmbientWorldStageRecord[] Entries =
        {
            S("PORT", "DOCK_PORTER", .20f, .035f, .61f, false,
                Daylight, .020f, -.010f, .30f, .70f),
            S("PORT", "PASSENGER_A", .77f, .035f, .58f, true,
                Daylight, .018f, -.008f, .27f, .62f),

            S("GANGWAY", "CREW_SECURITY", .70f, .035f, .56f, true,
                Daylight, -.018f, -.008f, .30f, .62f),
            S("GANGWAY", "PASSENGER_D", .29f, .035f, .53f, false,
                Daylight, -.016f, -.008f, .25f, .58f),

            S("RICHARD_SUITE", "SUITE_STEWARD", .72f, .035f, .61f, true,
                LuxuryInterior, -.018f, -.010f, .36f, .68f),

            S("VIP_LOUNGE", "VIP_HOST", .26f, .035f, .60f, false,
                LuxuryInterior, .018f, -.010f, .34f, .66f),
            S("VIP_LOUNGE", "PASSENGER_B", .76f, .035f, .57f, true,
                LuxuryInterior, -.018f, -.010f, .32f, .62f),

            S("OPEN_DECK", "PASSENGER_E", .25f, .035f, .59f, false,
                Daylight, .020f, -.008f, .25f, .64f),
            S("OPEN_DECK", "CREW_SECURITY", .74f, .035f, .60f, true,
                Daylight, .020f, -.008f, .28f, .66f),

            S("BALLROOM", "BALLROOM_MUSICIAN", .29f, .035f, .61f, false,
                LuxuryInterior, .018f, -.010f, .35f, .65f),
            S("BALLROOM", "CREW_ATTENDANT", .73f, .035f, .58f, true,
                LuxuryInterior, -.016f, -.010f, .34f, .62f),

            S("DINING", "DINING_SOMMELIER", .35f, .035f, .54f, false,
                WarmInterior, .018f, -.010f, .38f, .58f),
            S("DINING", "PASSENGER_C", .68f, .035f, .52f, true,
                WarmInterior, -.018f, -.010f, .36f, .56f),

            S("PROMENADE", "PASSENGER_A", .58f, .035f, .51f, false,
                Daylight, -.018f, -.008f, .26f, .55f),
            S("PROMENADE", "PASSENGER_D", .79f, .035f, .54f, true,
                Daylight, -.020f, -.008f, .28f, .58f),

            S("HORIZON", "PASSENGER_E", .27f, .035f, .57f, false,
                Daylight, .018f, -.008f, .26f, .60f),
            S("HORIZON", "CREW_ATTENDANT", .74f, .035f, .59f, true,
                Daylight, -.018f, -.008f, .28f, .62f),

            S("ATRIUM", "PASSENGER_A", .25f, .035f, .57f, false,
                WarmInterior, .018f, -.010f, .32f, .60f),
            S("ATRIUM", "ATRIUM_GUIDE", .75f, .035f, .59f, true,
                WarmInterior, -.018f, -.010f, .34f, .63f),

            S("NEWS_LOUNGE", "PASSENGER_F", .20f, .035f, .52f, false,
                NeutralInterior, .015f, -.010f, .30f, .56f),
            S("NEWS_LOUNGE", "PASSENGER_D", .50f, .035f, .54f, false,
                NeutralInterior, 0f, -.010f, .30f, .58f),
            S("NEWS_LOUNGE", "PASSENGER_B", .80f, .035f, .52f, true,
                NeutralInterior, -.015f, -.010f, .30f, .56f),

            S("SECURITY", "SECURITY_OPERATOR", .73f, .035f, .58f, true,
                CoolMachinery, -.014f, -.010f, .42f, .64f),
            S("SERVICE_RAIL", "RAIL_TECHNICIAN", .72f, .035f, .54f, true,
                CoolMachinery, -.016f, -.010f, .44f, .60f),

            S("MEDBAY", "SHIP_MEDIC", .70f, .035f, .58f, true,
                NeutralInterior, -.016f, -.010f, .34f, .62f),
            S("MEDBAY", "CREW_SECURITY", .25f, .035f, .55f, false,
                NeutralInterior, .016f, -.010f, .34f, .59f),

            S("BALLAST_CONTROL_ANNEX", "BALLAST_CONTROLLER",
                .73f, .035f, .56f, true,
                CoolMachinery, -.018f, -.010f, .45f, .62f),
            S("ENGINE_CONTROL", "CHIEF_ENGINEER", .22f, .035f, .55f, false,
                AmberMachinery, .018f, -.010f, .46f, .62f),
            S("CREW_STAIRS", "CREW_SECURITY", .71f, .035f, .52f, true,
                CoolMachinery, -.016f, -.010f, .42f, .58f),
            S("VAULT", "CREW_SECURITY", .74f, .035f, .57f, true,
                NeutralInterior, -.014f, -.010f, .45f, .62f),
            S("ARCHIVE", "ARCHIVIST", .68f, .035f, .56f, true,
                WarmInterior, -.018f, -.010f, .40f, .62f),
            S("LAUNDRY", "LAUNDRY_SUPERVISOR", .25f, .035f, .52f, false,
                NeutralInterior, .016f, -.010f, .38f, .58f),
            S("SERVICE_HUB", "ROBOTICS_TECH", .72f, .035f, .55f, true,
                CoolMachinery, -.018f, -.010f, .44f, .61f),
            S("STABILIZERS", "CREW_ENGINEER", .77f, .035f, .55f, true,
                CoolMachinery, -.020f, -.010f, .46f, .62f),
            S("BALLAST_TANKS", "BALLAST_CONTROLLER", .58f, .035f, .51f, true,
                CoolMachinery, -.018f, -.010f, .48f, .58f),
            S("GENERATOR", "CREW_ENGINEER", .78f, .035f, .54f, true,
                AmberMachinery, -.020f, -.010f, .48f, .61f),
            S("WORKSHOP", "WORKSHOP_MACHINIST", .19f, .035f, .53f, false,
                AmberMachinery, .018f, -.010f, .45f, .60f)
        };

        private static readonly IReadOnlyDictionary<string, AmbientWorldStageProfile>
            ByLocationAndSpeaker = BuildLookup();

        public static IReadOnlyList<AmbientWorldStageRecord> All => Entries;

        public static bool TryGet(
            string location,
            string speaker,
            out AmbientWorldStageProfile profile)
        {
            return ByLocationAndSpeaker.TryGetValue(
                Key(location, speaker),
                out profile);
        }

        private static IReadOnlyDictionary<string, AmbientWorldStageProfile>
            BuildLookup()
        {
            var result = new Dictionary<string, AmbientWorldStageProfile>(
                StringComparer.OrdinalIgnoreCase);
            foreach (AmbientWorldStageRecord entry in Entries)
                result[Key(entry.Location, entry.Speaker)] = entry.Profile;
            return result;
        }

        private static string Key(string location, string speaker)
        {
            return $"{location?.Trim()}|{speaker?.Trim()}";
        }

        private static AmbientWorldStageRecord S(
            string location,
            string speaker,
            float x,
            float y,
            float height,
            bool mirror,
            Color lightTint,
            float shadowX,
            float shadowY,
            float shadowOpacity,
            float groundShadowScale)
        {
            return new AmbientWorldStageRecord(
                location,
                speaker,
                new AmbientWorldStageProfile(
                    new Vector2(x, y),
                    height,
                    mirror,
                    lightTint,
                    new Vector2(shadowX, shadowY),
                    shadowOpacity,
                    groundShadowScale));
        }
    }
}
