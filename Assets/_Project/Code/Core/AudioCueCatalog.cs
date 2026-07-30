using System;
using System.Collections.Generic;

namespace Wake.Core
{
    public enum FootstepSurface
    {
        Normal,
        Wood,
        Metal,
        Stone
    }

    public readonly struct LocationAudioCue
    {
        public LocationAudioCue(
            string musicKey,
            float musicVolume,
            float crossfadeSeconds,
            string primaryAmbienceKey = "",
            float primaryAmbienceVolume = 0f,
            string secondaryAmbienceKey = "",
            float secondaryAmbienceVolume = 0f)
        {
            MusicKey = musicKey ?? string.Empty;
            MusicVolume = musicVolume;
            CrossfadeSeconds = crossfadeSeconds;
            PrimaryAmbienceKey = primaryAmbienceKey ?? string.Empty;
            PrimaryAmbienceVolume = primaryAmbienceVolume;
            SecondaryAmbienceKey = secondaryAmbienceKey ?? string.Empty;
            SecondaryAmbienceVolume = secondaryAmbienceVolume;
        }

        public string MusicKey { get; }
        public float MusicVolume { get; }
        public float CrossfadeSeconds { get; }
        public string PrimaryAmbienceKey { get; }
        public float PrimaryAmbienceVolume { get; }
        public string SecondaryAmbienceKey { get; }
        public float SecondaryAmbienceVolume { get; }
    }

    public static class AudioCueCatalog
    {
        public const float MapTravelFootstepSeconds = 1.5f;
        public const string ChapterTransitionMusicKey = "BGM/Mystery";

        public const string WaterSloshingKey =
            "SoundEffect/sound_of_water_sloshing";
        public const string WaterSplashOutKey =
            "SoundEffect/sound_of_waves";
        public const float WaterSplashOutStartOffset = 2f;
        public const float WaterSplashOutSeconds = 3f;
        public const float WaterSplashOutFadeOutSeconds = 0.4f;
        public const float UnderwaterMuffledCutoffHz = 300f;
        public const float UnderwaterClearCutoffHz = 22000f;

        public const string IronDoorKnockKey =
            "SoundEffect/The_sound_of_an_iron_door_knocking";
        public const string IronDoorToggleKey =
            "SoundEffect/The_sound_of_an_iron_door_opening_and_closing";

        // The former rough/metal footstep placeholder is intentionally routed
        // to the requested iron-door knock instead of Mountain Hiking Footsteps.
        public const string MetalFootstepReplacementKey = IronDoorKnockKey;
        public const string NormalFootstepKey =
            "SoundEffect/shoe_footsteps_sound_2";
        public const string RoughFootstepKey =
            "SoundEffect/Mountain Hiking Footsteps";

        private const string AdrianTheme =
            "BGM/Midnight_Latitude(Theme_Adrian_Vale)";
        private const string ThomasTheme =
            "BGM/The_Long_Watch(Theme_Thomas_Reed)";
        private const string MahoganyTheme =
            "BGM/The_Mahogany_Ledger(Theme_Richard Hawthorne_Evelyn_Shaw)";
        private const string PassageToPort = "BGM/Passage_to_Port";
        private const string HorizonRoom = "BGM/The_Horizon_Room";
        private const string LastWaltz = "BGM/Last_Waltz_on_the_Elysium";
        private const string Waves =
            "SoundEffect/Sound_of_waves_on_the_beach_1";
        private const string Wind = "SoundEffect/wind_noise";
        private const string Crowd = "SoundEffect/Cafe_buzzing_sound";
        private const string Engine = "SoundEffect/boat_engine_sound";
        private const string Fan = "SoundEffect/factory_exhaust_fan_sound";

        private static readonly IReadOnlyDictionary<string, LocationAudioCue>
            LocationCues =
                new Dictionary<string, LocationAudioCue>(
                    StringComparer.OrdinalIgnoreCase)
                {
                    ["PORT"] = Cue(PassageToPort, .55f, 2f, Waves, .4f, Wind, .28f),
                    ["GANGWAY"] = Cue(PassageToPort, .52f, 1.5f, Engine, .28f, Crowd, .24f),
                    ["RICHARD_SUITE"] = Cue(MahoganyTheme, .48f, 2f),
                    ["VIP_LOUNGE"] = Cue(AdrianTheme, .45f, 2f),
                    ["CABIN_CLAIRE"] = Cue(AdrianTheme, .45f, 2f),
                    ["OPEN_DECK"] = Cue(ThomasTheme, .5f, 2.5f, Wind, .43f, Waves, .34f),
                    ["BALLROOM"] = Cue(LastWaltz, .62f, 2f, Crowd, .34f),
                    ["DINING"] = Cue(LastWaltz, .52f, 2f, Crowd, .3f),
                    ["PROMENADE"] = Cue(AdrianTheme, .46f, 2f, Wind, .36f, Waves, .34f),
                    ["HORIZON"] = Cue(HorizonRoom, .56f, 2.2f),
                    ["ATRIUM"] = Cue(PassageToPort, .52f, 2f, Crowd, .34f),
                    ["NEWS_LOUNGE"] = Cue(AdrianTheme, .46f, 2f, Crowd, .22f),
                    ["CABIN_DANIEL"] = Cue(AdrianTheme, .46f, 2f),
                    ["SECURITY"] = Cue(AdrianTheme, .47f, 1.8f),
                    ["INTERVIEW"] = Cue(AdrianTheme, .47f, 1.8f),
                    ["SERVICE_RAIL"] = Cue(HorizonRoom, .54f, 1.8f, Fan, .3f),
                    ["MEDBAY"] = Cue(AdrianTheme, .42f, 1.8f),
                    ["BALLAST_CONTROL_ANNEX"] =
                        Cue(HorizonRoom, .57f, 1.6f, Fan, .34f, Engine, .12f),
                    ["ENGINE_CONTROL"] =
                        Cue(ThomasTheme, .48f, 2f, Fan, .32f, Engine, .22f),
                    ["BRIDGE"] =
                        Cue(ThomasTheme, .48f, 2f, Fan, .12f, Engine, .18f),
                    ["SERVICE7"] = Cue(AdrianTheme, .5f, 1.2f, Fan, .12f),
                    ["CREW_STAIRS"] = Cue(AdrianTheme, .5f, 1.2f, Fan, .12f),
                    ["VAULT"] = Cue(MahoganyTheme, .52f, 1.8f),
                    ["ARCHIVE"] = Cue(ThomasTheme, .48f, 2f),
                    ["LAUNDRY"] = Cue(AdrianTheme, .4f, 2f),
                    ["SERVICE_HUB"] = Cue(AdrianTheme, .44f, 1.8f),
                    ["STABILIZERS"] =
                        Cue(AdrianTheme, .48f, 1.8f, Fan, .25f, Engine, .15f),
                    ["BALLAST_TANKS"] =
                        Cue(HorizonRoom, .52f, 1.6f, Fan, .3f, Engine, .12f),
                    ["GENERATOR"] =
                        Cue(AdrianTheme, .48f, 1.6f, Fan, .38f, Engine, .1f),
                    ["WORKSHOP"] = Cue(AdrianTheme, .43f, 1.8f)
                };

        // Longer, gentler crossfades for specific location-to-location
        // transitions that would otherwise hard-swap between two
        // unrelated music cues at a narratively loaded moment (e.g.
        // straight from discovering Daniel's body into the medbay
        // aftermath) - keyed by (from, to) location code pair.
        private static readonly IReadOnlyDictionary<
            (string From, string To), float> TransitionCrossfadeOverrides =
            new Dictionary<(string, string), float>
            {
                [("HORIZON", "MEDBAY")] = 4.5f
            };

        private static readonly HashSet<string> WoodFootstepLocations =
            new(StringComparer.OrdinalIgnoreCase)
            {
                "GANGWAY",
                "OPEN_DECK",
                "PROMENADE"
            };

        private static readonly HashSet<string> MetalFootstepLocations =
            new(StringComparer.OrdinalIgnoreCase)
            {
                "SERVICE7",
                "CREW_STAIRS",
                "SERVICE_RAIL",
                "ENGINE_CONTROL",
                "BALLAST_CONTROL_ANNEX",
                "LAUNDRY",
                "SERVICE_HUB",
                "STABILIZERS",
                "BALLAST_TANKS",
                "GENERATOR",
                "WORKSHOP"
            };

        public static bool TryGetLocationCue(
            string locationCode,
            out LocationAudioCue cue)
        {
            string normalized = locationCode?.Trim() ?? string.Empty;
            return LocationCues.TryGetValue(normalized, out cue);
        }

        public static bool TryGetTransitionCrossfadeSeconds(
            string fromLocationCode,
            string toLocationCode,
            out float crossfadeSeconds)
        {
            string from = fromLocationCode?.Trim().ToUpperInvariant() ??
                          string.Empty;
            string to = toLocationCode?.Trim().ToUpperInvariant() ??
                        string.Empty;
            return TransitionCrossfadeOverrides.TryGetValue(
                (from, to),
                out crossfadeSeconds);
        }

        public static FootstepSurface FootstepSurfaceFor(
            string locationCode)
        {
            string normalized =
                locationCode?.Trim() ?? string.Empty;
            if (string.Equals(
                    normalized,
                    "PORT",
                    StringComparison.OrdinalIgnoreCase))
            {
                return FootstepSurface.Stone;
            }
            if (MetalFootstepLocations.Contains(normalized))
                return FootstepSurface.Metal;
            if (WoodFootstepLocations.Contains(normalized))
                return FootstepSurface.Wood;
            return FootstepSurface.Normal;
        }

        public static string FootstepKeyFor(FootstepSurface surface) =>
            surface switch
            {
                FootstepSurface.Metal => MetalFootstepReplacementKey,
                FootstepSurface.Wood => RoughFootstepKey,
                FootstepSurface.Stone => RoughFootstepKey,
                _ => NormalFootstepKey
            };

        public static float FootstepPitchFor(FootstepSurface surface) =>
            surface switch
            {
                FootstepSurface.Metal => 1.18f,
                FootstepSurface.Wood => 1.12f,
                FootstepSurface.Stone => 1.08f,
                _ => 1.2f
            };

        // Each source mp3 has its own lead-in silence before the actual
        // footstep audio starts (measured from each clip's waveform), so
        // playback starts partway in instead of during a silent gap.
        public static float FootstepStartOffsetFor(FootstepSurface surface) =>
            surface switch
            {
                FootstepSurface.Metal => 0.06f,
                FootstepSurface.Wood => 1.10f,
                FootstepSurface.Stone => 1.10f,
                _ => 0.41f
            };

        private static LocationAudioCue Cue(
            string music,
            float musicVolume,
            float crossfade,
            string ambienceA = "",
            float ambienceAVolume = 0f,
            string ambienceB = "",
            float ambienceBVolume = 0f) =>
            new(
                music,
                musicVolume,
                crossfade,
                ambienceA,
                ambienceAVolume,
                ambienceB,
                ambienceBVolume);
    }
}
