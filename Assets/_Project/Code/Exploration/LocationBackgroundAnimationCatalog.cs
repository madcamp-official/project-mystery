using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using UnityEngine;

namespace Wake.Exploration
{
    public enum LocationBackgroundEffectType
    {
        RadialLightPulse,
        RectangularScreenPulse,
        LinearSweep,
        DriftingMotes,
        DriftingSteam,
        OccasionalFlicker,
        OccasionalSpark,
        FullBackgroundDrift,
        FullBackgroundShake
    }

    /// <summary>
    /// Immutable, renderer-agnostic instructions for one UI background effect.
    /// Rect and anchor coordinates use a bottom-left (0, 0), top-right (1, 1)
    /// convention. NormalizedTravel is measured against the full background.
    /// FrequencyHz is cycles per second for loops and average events per second
    /// for occasional effects.
    /// </summary>
    public readonly struct LocationBackgroundEffectSpec
    {
        public LocationBackgroundEffectSpec(
            LocationBackgroundEffectType type,
            Rect normalizedRect,
            Vector2 normalizedAnchor,
            Color color,
            float intensity,
            float durationSeconds,
            float frequencyHz,
            int seed,
            int maxElementCount,
            Vector2 direction,
            float normalizedTravel)
        {
            float x = Mathf.Clamp01(normalizedRect.x);
            float y = Mathf.Clamp01(normalizedRect.y);
            float width = Mathf.Clamp(
                normalizedRect.width,
                0.001f,
                1f - x);
            float height = Mathf.Clamp(
                normalizedRect.height,
                0.001f,
                1f - y);

            Type = type;
            NormalizedRect = new Rect(x, y, width, height);
            NormalizedAnchor = new Vector2(
                Mathf.Clamp01(normalizedAnchor.x),
                Mathf.Clamp01(normalizedAnchor.y));
            Color = color;
            Intensity = Mathf.Clamp01(intensity);
            DurationSeconds = Mathf.Max(0.01f, durationSeconds);
            FrequencyHz = Mathf.Max(0.001f, frequencyHz);
            Seed = seed;
            MaxElementCount = Mathf.Max(1, maxElementCount);
            Direction = direction.sqrMagnitude > 0f
                ? direction.normalized
                : Vector2.zero;
            NormalizedTravel = Mathf.Clamp(normalizedTravel, 0f, 2f);
        }

        public LocationBackgroundEffectType Type { get; }
        public Rect NormalizedRect { get; }
        public Vector2 NormalizedAnchor { get; }
        public Color Color { get; }
        public float Intensity { get; }
        public float DurationSeconds { get; }
        public float FrequencyHz { get; }
        public int Seed { get; }
        public int MaxElementCount { get; }
        public Vector2 Direction { get; }
        public float NormalizedTravel { get; }
    }

    public sealed class LocationBackgroundAnimationProfile
    {
        private readonly ReadOnlyCollection<LocationBackgroundEffectSpec>
            effects;

        public LocationBackgroundAnimationProfile(
            string id,
            string sourceSpriteFileName,
            params LocationBackgroundEffectSpec[] effects)
        {
            Id = id?.Trim() ?? string.Empty;
            SourceSpriteFileName =
                sourceSpriteFileName?.Trim() ?? string.Empty;
            this.effects = Array.AsReadOnly(
                effects?.ToArray() ??
                Array.Empty<LocationBackgroundEffectSpec>());
        }

        public string Id { get; }
        public string SourceSpriteFileName { get; }
        public IReadOnlyList<LocationBackgroundEffectSpec> Effects => effects;
    }

    public readonly struct LocationBackgroundProfileBinding
    {
        public LocationBackgroundProfileBinding(
            string logicalLocationCode,
            string profileId)
        {
            LogicalLocationCode =
                logicalLocationCode?.Trim() ?? string.Empty;
            ProfileId = profileId?.Trim() ?? string.Empty;
        }

        public string LogicalLocationCode { get; }
        public string ProfileId { get; }
    }

    /// <summary>
    /// Animation plans for the 19 currently used pieces of location artwork.
    /// Twenty-three story-relevant logical locations bind to those visuals;
    /// four pairs intentionally share art and therefore share animation masks.
    /// This catalog contains no scene objects and can be consumed by any UI
    /// overlay implementation.
    /// </summary>
    public static class LocationBackgroundAnimationCatalog
    {
        private static readonly Color SunGold =
            new Color32(255, 198, 108, 178);
        private static readonly Color LampGold =
            new Color32(255, 218, 157, 150);
        private static readonly Color CandleAmber =
            new Color32(255, 158, 70, 174);
        private static readonly Color OceanGlint =
            new Color32(187, 229, 242, 120);
        private static readonly Color CoolScreen =
            new Color32(127, 226, 216, 138);
        private static readonly Color GreenScreen =
            new Color32(114, 232, 160, 148);
        private static readonly Color Dust =
            new Color32(255, 232, 190, 105);
        private static readonly Color Steam =
            new Color32(218, 236, 233, 112);
        private static readonly Color Spark =
            new Color32(255, 157, 63, 225);
        private static readonly Color NeutralMotion =
            new Color32(255, 255, 255, 255);

        private static readonly LocationBackgroundAnimationProfile[]
            ProfileEntries =
        {
            P(
                "PORT",
                "bg_location_port_evidence.png",
                E(LocationBackgroundEffectType.RadialLightPulse,
                    .79f, .72f, .31f, .35f, SunGold,
                    .22f, 5.8f, .17f, 101, 1),
                E(LocationBackgroundEffectType.LinearSweep,
                    .68f, .22f, .54f, .23f, OceanGlint,
                    .16f, 8.0f, .125f, 102, 2, 1f, .08f, .42f),
                E(LocationBackgroundEffectType.DriftingMotes,
                    .53f, .50f, .88f, .82f, Dust,
                    .15f, 10.0f, .10f, 103, 9, -.10f, .35f, .28f),
                E(LocationBackgroundEffectType.FullBackgroundDrift,
                    .50f, .50f, 1f, 1f, NeutralMotion,
                    .035f, 16.0f, .0625f, 104, 1, .18f, .04f, .012f)),

            P(
                "GANGWAY",
                "bg_location_gangway.png",
                E(LocationBackgroundEffectType.RadialLightPulse,
                    .69f, .73f, .22f, .30f, SunGold,
                    .18f, 6.6f, .15f, 201, 2),
                E(LocationBackgroundEffectType.LinearSweep,
                    .52f, .23f, .55f, .24f, OceanGlint,
                    .13f, 7.2f, .14f, 202, 1, -.85f, .16f, .34f),
                E(LocationBackgroundEffectType.DriftingMotes,
                    .45f, .55f, .70f, .72f, Dust,
                    .12f, 11.0f, .09f, 203, 7, .08f, .42f, .22f)),

            P(
                "RICHARD_SUITE",
                "bg_location_d10_1_richard_suite.png",
                E(LocationBackgroundEffectType.RadialLightPulse,
                    .70f, .67f, .28f, .42f, SunGold,
                    .16f, 7.5f, .13f, 301, 1),
                E(LocationBackgroundEffectType.RadialLightPulse,
                    .18f, .37f, .16f, .24f, LampGold,
                    .12f, 4.8f, .21f, 302, 2),
                E(LocationBackgroundEffectType.DriftingMotes,
                    .51f, .58f, .70f, .68f, Dust,
                    .14f, 12.0f, .083f, 303, 10, -.05f, .50f, .25f),
                E(LocationBackgroundEffectType.OccasionalFlicker,
                    .18f, .37f, .17f, .25f, CandleAmber,
                    .18f, .20f, .075f, 304, 1)),

            P(
                "ATRIUM",
                "bg_location_d8_1_atrium.png",
                E(LocationBackgroundEffectType.RadialLightPulse,
                    .52f, .70f, .28f, .35f, LampGold,
                    .15f, 6.2f, .16f, 401, 3),
                E(LocationBackgroundEffectType.LinearSweep,
                    .49f, .19f, .55f, .24f, LampGold,
                    .10f, 9.0f, .11f, 402, 1, .92f, .06f, .38f),
                E(LocationBackgroundEffectType.DriftingMotes,
                    .51f, .53f, .76f, .78f, Dust,
                    .13f, 12.5f, .08f, 403, 12, .04f, .55f, .30f),
                E(LocationBackgroundEffectType.FullBackgroundDrift,
                    .50f, .50f, 1f, 1f, NeutralMotion,
                    .025f, 18.0f, .056f, 404, 1, .10f, .02f, .008f)),

            P(
                "DINING",
                "bg_location_d9_2_dining.png",
                E(LocationBackgroundEffectType.RadialLightPulse,
                    .48f, .68f, .40f, .38f, LampGold,
                    .14f, 6.8f, .15f, 501, 4),
                E(LocationBackgroundEffectType.RadialLightPulse,
                    .57f, .33f, .42f, .30f, CandleAmber,
                    .18f, 3.9f, .26f, 502, 5),
                E(LocationBackgroundEffectType.OccasionalFlicker,
                    .57f, .33f, .45f, .32f, CandleAmber,
                    .20f, .16f, .09f, 503, 2),
                E(LocationBackgroundEffectType.DriftingMotes,
                    .50f, .54f, .78f, .72f, Dust,
                    .10f, 13.0f, .077f, 504, 8, -.04f, .42f, .20f)),

            P(
                "BALLROOM",
                "bg_location_d9_1_ballroom.png",
                E(LocationBackgroundEffectType.RadialLightPulse,
                    .49f, .79f, .38f, .34f, LampGold,
                    .19f, 5.9f, .17f, 601, 5),
                E(LocationBackgroundEffectType.LinearSweep,
                    .49f, .23f, .62f, .31f, LampGold,
                    .14f, 7.8f, .13f, 602, 2, .90f, .08f, .46f),
                E(LocationBackgroundEffectType.DriftingMotes,
                    .49f, .56f, .83f, .72f, Dust,
                    .16f, 11.5f, .087f, 603, 14, -.08f, .52f, .28f),
                E(LocationBackgroundEffectType.FullBackgroundDrift,
                    .50f, .50f, 1f, 1f, NeutralMotion,
                    .022f, 20.0f, .05f, 604, 1, .14f, .02f, .007f)),

            P(
                "SERVICE_STAIRS",
                "bg_location_d7_4_crew_stairs.png",
                E(LocationBackgroundEffectType.RadialLightPulse,
                    .30f, .69f, .28f, .38f, LampGold,
                    .17f, 5.4f, .185f, 701, 3),
                E(LocationBackgroundEffectType.DriftingSteam,
                    .63f, .38f, .47f, .60f, Steam,
                    .22f, 7.5f, .13f, 702, 5, -.18f, .82f, .42f),
                E(LocationBackgroundEffectType.OccasionalFlicker,
                    .30f, .68f, .30f, .42f, CandleAmber,
                    .24f, .13f, .065f, 703, 2),
                E(LocationBackgroundEffectType.FullBackgroundShake,
                    .50f, .50f, 1f, 1f, NeutralMotion,
                    .018f, .24f, .07f, 704, 1, 1f, 0f, .005f)),

            P(
                "HORIZON",
                "bg_location_d9_4_horizon_room_evidence.png",
                E(LocationBackgroundEffectType.RadialLightPulse,
                    .74f, .69f, .37f, .40f, SunGold,
                    .21f, 6.9f, .145f, 801, 1),
                E(LocationBackgroundEffectType.LinearSweep,
                    .72f, .32f, .42f, .22f, OceanGlint,
                    .13f, 8.8f, .115f, 802, 2, .94f, .04f, .36f),
                E(LocationBackgroundEffectType.DriftingMotes,
                    .50f, .54f, .78f, .70f, Dust,
                    .12f, 12.0f, .083f, 803, 8, -.10f, .38f, .24f),
                E(LocationBackgroundEffectType.FullBackgroundDrift,
                    .50f, .50f, 1f, 1f, NeutralMotion,
                    .028f, 17.0f, .059f, 804, 1, .16f, .02f, .009f)),

            P(
                "MEDBAY",
                "bg_location_d7_1_medbay_evidence.png",
                E(LocationBackgroundEffectType.RectangularScreenPulse,
                    .43f, .67f, .20f, .25f, CoolScreen,
                    .18f, 3.4f, .29f, 901, 2),
                E(LocationBackgroundEffectType.RadialLightPulse,
                    .54f, .78f, .42f, .24f, LampGold,
                    .11f, 7.2f, .14f, 902, 2),
                E(LocationBackgroundEffectType.LinearSweep,
                    .43f, .67f, .19f, .23f, CoolScreen,
                    .17f, 2.6f, .38f, 903, 1, 0f, -1f, .20f),
                E(LocationBackgroundEffectType.DriftingMotes,
                    .51f, .53f, .74f, .68f, Dust,
                    .09f, 14.0f, .071f, 904, 6, .04f, .35f, .18f)),

            P(
                "SECURITY_INTERVIEW",
                "bg_location_d8_3_security_evidence.png",
                E(LocationBackgroundEffectType.RectangularScreenPulse,
                    .30f, .58f, .39f, .39f, CoolScreen,
                    .22f, 2.8f, .36f, 1001, 4),
                E(LocationBackgroundEffectType.LinearSweep,
                    .30f, .58f, .40f, .38f, GreenScreen,
                    .19f, 2.2f, .45f, 1002, 2, 0f, -1f, .34f),
                E(LocationBackgroundEffectType.OccasionalFlicker,
                    .30f, .58f, .43f, .42f, CoolScreen,
                    .26f, .11f, .055f, 1003, 2),
                E(LocationBackgroundEffectType.RadialLightPulse,
                    .75f, .78f, .25f, .24f, LampGold,
                    .10f, 7.0f, .14f, 1004, 1)),

            P(
                "NEWS_DANIEL",
                "bg_location_d8_2_news_lounge_evidence.png",
                E(LocationBackgroundEffectType.RectangularScreenPulse,
                    .28f, .63f, .37f, .37f, CoolScreen,
                    .20f, 3.1f, .32f, 1101, 4),
                E(LocationBackgroundEffectType.LinearSweep,
                    .28f, .63f, .38f, .36f, GreenScreen,
                    .16f, 2.7f, .37f, 1102, 2, .05f, -1f, .31f),
                E(LocationBackgroundEffectType.RadialLightPulse,
                    .67f, .75f, .28f, .25f, LampGold,
                    .11f, 6.5f, .15f, 1103, 2),
                E(LocationBackgroundEffectType.DriftingMotes,
                    .55f, .52f, .70f, .65f, Dust,
                    .10f, 13.0f, .077f, 1104, 7, -.05f, .38f, .19f)),

            P(
                "ENGINE_BRIDGE",
                "bg_location_d7_3_engine_control_evidence.png",
                E(LocationBackgroundEffectType.RectangularScreenPulse,
                    .31f, .44f, .42f, .28f, GreenScreen,
                    .19f, 2.5f, .40f, 1201, 4),
                E(LocationBackgroundEffectType.RadialLightPulse,
                    .72f, .64f, .36f, .48f, GreenScreen,
                    .16f, 4.8f, .21f, 1202, 2),
                E(LocationBackgroundEffectType.DriftingSteam,
                    .65f, .48f, .57f, .72f, Steam,
                    .24f, 6.8f, .15f, 1203, 5, -.15f, .88f, .48f),
                E(LocationBackgroundEffectType.OccasionalSpark,
                    .82f, .45f, .24f, .48f, Spark,
                    .38f, .18f, .07f, 1204, 3, -.18f, -.98f, .18f),
                E(LocationBackgroundEffectType.FullBackgroundShake,
                    .50f, .50f, 1f, 1f, NeutralMotion,
                    .025f, .30f, .11f, 1205, 1, 1f, 0f, .007f)),

            P(
                "VAULT",
                "bg_location_d6_1_vault.png",
                E(LocationBackgroundEffectType.RectangularScreenPulse,
                    .34f, .43f, .36f, .25f, CoolScreen,
                    .18f, 3.0f, .33f, 1301, 3),
                E(LocationBackgroundEffectType.RadialLightPulse,
                    .62f, .53f, .22f, .34f, GreenScreen,
                    .14f, 4.0f, .25f, 1302, 1),
                E(LocationBackgroundEffectType.LinearSweep,
                    .53f, .53f, .47f, .58f, GreenScreen,
                    .14f, 4.6f, .22f, 1303, 1, 1f, .05f, .39f),
                E(LocationBackgroundEffectType.OccasionalFlicker,
                    .34f, .43f, .39f, .28f, CoolScreen,
                    .22f, .12f, .05f, 1304, 1)),

            P(
                "PROMENADE",
                "bg_location_d9_3_promenade_evidence.png",
                E(LocationBackgroundEffectType.RadialLightPulse,
                    .70f, .70f, .34f, .40f, SunGold,
                    .20f, 6.7f, .15f, 1401, 1),
                E(LocationBackgroundEffectType.LinearSweep,
                    .63f, .28f, .66f, .23f, OceanGlint,
                    .15f, 7.9f, .13f, 1402, 2, .96f, .05f, .48f),
                E(LocationBackgroundEffectType.DriftingMotes,
                    .55f, .55f, .80f, .72f, Dust,
                    .13f, 10.5f, .095f, 1403, 10, -.12f, .48f, .29f),
                E(LocationBackgroundEffectType.FullBackgroundDrift,
                    .50f, .50f, 1f, 1f, NeutralMotion,
                    .035f, 15.0f, .067f, 1404, 1, .18f, .03f, .011f)),

            P(
                "CABIN_CLAIRE",
                "bg_location_d10_2_vip_lounge.png",
                E(LocationBackgroundEffectType.RadialLightPulse,
                    .73f, .39f, .35f, .40f, CandleAmber,
                    .17f, 5.1f, .20f, 1501, 3),
                E(LocationBackgroundEffectType.RadialLightPulse,
                    .47f, .73f, .38f, .28f, LampGold,
                    .12f, 7.2f, .14f, 1502, 2),
                E(LocationBackgroundEffectType.DriftingMotes,
                    .50f, .55f, .78f, .70f, Dust,
                    .15f, 12.0f, .083f, 1503, 11, .03f, .46f, .24f),
                E(LocationBackgroundEffectType.OccasionalFlicker,
                    .73f, .39f, .37f, .43f, CandleAmber,
                    .21f, .14f, .075f, 1504, 2)),

            P(
                "SERVICE_RAIL",
                "bg_location_d8_4_service_rail.png",
                E(LocationBackgroundEffectType.RadialLightPulse,
                    .66f, .75f, .40f, .27f, LampGold,
                    .13f, 6.0f, .17f, 1601, 4),
                E(LocationBackgroundEffectType.LinearSweep,
                    .50f, .23f, .72f, .28f, CoolScreen,
                    .12f, 6.8f, .15f, 1602, 2, -.93f, .08f, .52f),
                E(LocationBackgroundEffectType.DriftingSteam,
                    .47f, .48f, .80f, .70f, Steam,
                    .20f, 7.0f, .14f, 1603, 4, -.12f, .90f, .44f),
                E(LocationBackgroundEffectType.OccasionalFlicker,
                    .66f, .75f, .42f, .30f, LampGold,
                    .19f, .12f, .06f, 1604, 2)),

            P(
                "BALLAST_CONTROL_ANNEX",
                "bg_location_d7_2_ballast_control_annex_evidence.png",
                E(LocationBackgroundEffectType.RectangularScreenPulse,
                    .33f, .49f, .45f, .30f, GreenScreen,
                    .20f, 2.7f, .37f, 1701, 4),
                E(LocationBackgroundEffectType.DriftingSteam,
                    .73f, .47f, .40f, .72f, Steam,
                    .25f, 6.1f, .16f, 1702, 5, -.16f, .91f, .50f),
                E(LocationBackgroundEffectType.OccasionalSpark,
                    .75f, .51f, .30f, .62f, Spark,
                    .34f, .16f, .065f, 1703, 3, -.20f, -.98f, .20f),
                E(LocationBackgroundEffectType.FullBackgroundShake,
                    .50f, .50f, 1f, 1f, NeutralMotion,
                    .022f, .28f, .09f, 1704, 1, 1f, 0f, .006f)),

            P(
                "ARCHIVE",
                "bg_location_d6_2_archive.png",
                E(LocationBackgroundEffectType.RectangularScreenPulse,
                    .50f, .34f, .36f, .24f, CoolScreen,
                    .18f, 3.2f, .31f, 1801, 3),
                E(LocationBackgroundEffectType.LinearSweep,
                    .50f, .34f, .35f, .23f, GreenScreen,
                    .14f, 2.8f, .36f, 1802, 1, 0f, -1f, .19f),
                E(LocationBackgroundEffectType.RadialLightPulse,
                    .69f, .73f, .32f, .28f, LampGold,
                    .11f, 7.5f, .13f, 1803, 2),
                E(LocationBackgroundEffectType.DriftingMotes,
                    .48f, .55f, .78f, .72f, Dust,
                    .17f, 13.5f, .074f, 1804, 13, -.04f, .48f, .25f)),

            P(
                "OPEN_DECK",
                "bg_location_d10_3_open_deck.png",
                E(LocationBackgroundEffectType.RadialLightPulse,
                    .27f, .70f, .42f, .43f, SunGold,
                    .25f, 6.2f, .16f, 1901, 1),
                E(LocationBackgroundEffectType.LinearSweep,
                    .35f, .25f, .68f, .23f, OceanGlint,
                    .17f, 7.4f, .14f, 1902, 2, .97f, .05f, .55f),
                E(LocationBackgroundEffectType.DriftingMotes,
                    .50f, .56f, .90f, .76f, Dust,
                    .16f, 9.8f, .10f, 1903, 12, -.18f, .55f, .34f),
                E(LocationBackgroundEffectType.FullBackgroundDrift,
                    .50f, .50f, 1f, 1f, NeutralMotion,
                    .045f, 13.0f, .077f, 1904, 1, .22f, .04f, .014f))
        };

        private static readonly LocationBackgroundProfileBinding[]
            BindingEntries =
        {
            B("PORT", "PORT"),
            B("GANGWAY", "GANGWAY"),
            B("RICHARD_SUITE", "RICHARD_SUITE"),
            B("ATRIUM", "ATRIUM"),
            B("DINING", "DINING"),
            B("BALLROOM", "BALLROOM"),
            B("SERVICE7", "SERVICE_STAIRS"),
            B("CREW_STAIRS", "SERVICE_STAIRS"),
            B("HORIZON", "HORIZON"),
            B("MEDBAY", "MEDBAY"),
            B("SECURITY", "SECURITY_INTERVIEW"),
            B("INTERVIEW", "SECURITY_INTERVIEW"),
            B("NEWS_LOUNGE", "NEWS_DANIEL"),
            B("CABIN_DANIEL", "NEWS_DANIEL"),
            B("ENGINE_CONTROL", "ENGINE_BRIDGE"),
            B("BRIDGE", "ENGINE_BRIDGE"),
            B("VAULT", "VAULT"),
            B("PROMENADE", "PROMENADE"),
            B("CABIN_CLAIRE", "CABIN_CLAIRE"),
            B("SERVICE_RAIL", "SERVICE_RAIL"),
            B("BALLAST_CONTROL_ANNEX", "BALLAST_CONTROL_ANNEX"),
            B("ARCHIVE", "ARCHIVE"),
            B("OPEN_DECK", "OPEN_DECK")
        };

        private static readonly IReadOnlyList<
            LocationBackgroundAnimationProfile> ReadOnlyProfiles =
                Array.AsReadOnly(ProfileEntries);
        private static readonly IReadOnlyList<
            LocationBackgroundProfileBinding> ReadOnlyBindings =
                Array.AsReadOnly(BindingEntries);
        private static readonly IReadOnlyDictionary<
            string,
            LocationBackgroundAnimationProfile> ProfilesById =
                BuildProfileLookup();
        private static readonly IReadOnlyDictionary<
            string,
            LocationBackgroundAnimationProfile> ProfilesByLocation =
                BuildLocationLookup();

        public static IReadOnlyList<LocationBackgroundAnimationProfile> All =>
            ReadOnlyProfiles;
        public static IReadOnlyList<LocationBackgroundProfileBinding>
            Bindings => ReadOnlyBindings;

        public static bool TryGet(
            string logicalLocationCode,
            out LocationBackgroundAnimationProfile profile)
        {
            string normalized =
                logicalLocationCode?.Trim().ToUpperInvariant() ??
                string.Empty;
            CanonicalLocationSpec canonical =
                CanonicalLocationCatalog.FindSpec(normalized);
            string canonicalCode = canonical?.Code ?? normalized;
            return ProfilesByLocation.TryGetValue(
                canonicalCode,
                out profile);
        }

        public static bool TryGetById(
            string profileId,
            out LocationBackgroundAnimationProfile profile)
        {
            return ProfilesById.TryGetValue(
                profileId?.Trim() ?? string.Empty,
                out profile);
        }

        private static IReadOnlyDictionary<
            string,
            LocationBackgroundAnimationProfile> BuildProfileLookup()
        {
            return ProfileEntries.ToDictionary(
                profile => profile.Id,
                StringComparer.Ordinal);
        }

        private static IReadOnlyDictionary<
            string,
            LocationBackgroundAnimationProfile> BuildLocationLookup()
        {
            var result =
                new Dictionary<
                    string,
                    LocationBackgroundAnimationProfile>(
                    StringComparer.Ordinal);
            foreach (LocationBackgroundProfileBinding binding in
                     BindingEntries)
            {
                result.Add(
                    binding.LogicalLocationCode,
                    ProfilesById[binding.ProfileId]);
            }

            return result;
        }

        private static LocationBackgroundAnimationProfile P(
            string id,
            string sourceSpriteFileName,
            params LocationBackgroundEffectSpec[] effects)
        {
            return new LocationBackgroundAnimationProfile(
                id,
                sourceSpriteFileName,
                effects);
        }

        private static LocationBackgroundProfileBinding B(
            string location,
            string profile)
        {
            return new LocationBackgroundProfileBinding(location, profile);
        }

        private static LocationBackgroundEffectSpec E(
            LocationBackgroundEffectType type,
            float anchorX,
            float anchorY,
            float width,
            float height,
            Color color,
            float intensity,
            float duration,
            float frequency,
            int seed,
            int maxElements,
            float directionX = 0f,
            float directionY = 0f,
            float travel = 0f)
        {
            var anchor = new Vector2(anchorX, anchorY);
            var rect = new Rect(
                anchorX - width * .5f,
                anchorY - height * .5f,
                width,
                height);
            return new LocationBackgroundEffectSpec(
                type,
                rect,
                anchor,
                color,
                intensity,
                duration,
                frequency,
                seed,
                maxElements,
                new Vector2(directionX, directionY),
                travel);
        }
    }
}
