using System;
using System.Collections.Generic;
using System.Linq;

namespace Wake.Exploration
{
    public enum SceneCharacterState
    {
        Normal,
        Injured,
        Deceased,
        Detained
    }

    public sealed class ScenePresenceRecord
    {
        private readonly IReadOnlyDictionary<string, string> characterLocations;

        public ScenePresenceRecord(
            int order,
            string sceneId,
            string focusLocation,
            string contextBarkId,
            string contextSpeaker,
            IReadOnlyDictionary<string, string> characterLocations)
        {
            Order = order;
            SceneId = sceneId;
            FocusLocation = focusLocation;
            ContextBarkId = contextBarkId;
            ContextSpeaker = contextSpeaker;
            this.characterLocations = characterLocations;
        }

        public int Order { get; }
        public string SceneId { get; }
        public string FocusLocation { get; }
        public string ContextBarkId { get; }
        public string ContextSpeaker { get; }
        public IReadOnlyDictionary<string, string> CharacterLocations =>
            characterLocations;

        public string GetLocation(string characterId)
        {
            string normalized = Normalize(characterId);
            return characterLocations.TryGetValue(normalized, out string location)
                ? location
                : string.Empty;
        }

        public IReadOnlyList<string> GetCharactersAt(string locationCode)
        {
            string normalized = Normalize(locationCode);
            return ScenePresenceCatalog.MainCharacterIds
                .Where(character =>
                    string.Equals(
                        GetLocation(character),
                        normalized,
                        StringComparison.Ordinal))
                .ToArray();
        }

        public SceneCharacterState GetState(string characterId)
        {
            string character = Normalize(characterId);
            if (character == "DANIEL" && Order >= 7)
                return SceneCharacterState.Deceased;
            if (character == "MARCUS" && Order >= 22 && Order <= 28)
                return SceneCharacterState.Injured;
            if (character == "EVELYN" && Order >= 39)
                return SceneCharacterState.Detained;
            return SceneCharacterState.Normal;
        }

        private static string Normalize(string value) =>
            value?.Trim().ToUpperInvariant() ?? string.Empty;
    }

    public static class ScenePresenceCatalog
    {
        public static readonly IReadOnlyList<string> MainCharacterIds =
            new[]
            {
                "ADRIAN", "DANIEL", "RICHARD", "EVELYN", "CLAIRE",
                "THOMAS", "MARCUS", "HELENA", "OWEN"
            };

        private static readonly ScenePresenceRecord[] Entries =
        {
            S("P-01", "PORT", "SCENE_P01_PORT", "DOCK_PORTER", "PORT", "PORT", "RICHARD_SUITE", "GANGWAY", "VIP_LOUNGE", "ENGINE_CONTROL", "SECURITY", "MEDBAY", "ENGINE_CONTROL"),
            S("P-02", "GANGWAY", "SCENE_P02_GANGWAY", "CREW_SECURITY", "GANGWAY", "GANGWAY", "GANGWAY", "GANGWAY", "VIP_LOUNGE", "ENGINE_CONTROL", "SECURITY", "MEDBAY", "ENGINE_CONTROL"),
            S("P-03", "RICHARD_SUITE", "SCENE_P03_SUITE", "SUITE_STEWARD", "RICHARD_SUITE", "NEWS_LOUNGE", "RICHARD_SUITE", "VIP_LOUNGE", "VIP_LOUNGE", "ENGINE_CONTROL", "SECURITY", "MEDBAY", "ENGINE_CONTROL"),
            S("D1-01", "ATRIUM", "SCENE_D101_ATRIUM", "ATRIUM_GUIDE", "ATRIUM", "NEWS_LOUNGE", "RICHARD_SUITE", "GANGWAY", "ATRIUM", "ENGINE_CONTROL", "ATRIUM", "ATRIUM", "ATRIUM"),
            S("D1-02", "DINING", "SCENE_D102_DINING", "DINING_SOMMELIER", "DINING", "DINING", "RICHARD_SUITE", "DINING", "DINING", "ENGINE_CONTROL", "SECURITY", "MEDBAY", "ENGINE_CONTROL"),
            S("D1-03", "BALLROOM", "SCENE_D103_BALLROOM", "BALLROOM_MUSICIAN", "BALLROOM", "BALLROOM", "BALLROOM", "BALLROOM", "BALLROOM", "BALLROOM", "BALLROOM", "BALLROOM", "BALLROOM"),
            S("D1-04", "CREW_STAIRS", "SCENE_D104_STAIRS", "CREW_SECURITY", "CREW_STAIRS", "CREW_STAIRS", "RICHARD_SUITE", "VIP_LOUNGE", "VIP_LOUNGE", "ENGINE_CONTROL", "SECURITY", "MEDBAY", "ENGINE_CONTROL"),
            S("D1-05", "BALLROOM", "SCENE_D105_BALLROOM", "CREW_ATTENDANT", "BALLROOM", "HORIZON", "BALLROOM", "BALLROOM", "BALLROOM", "ENGINE_CONTROL", "SECURITY", "MEDBAY", "ENGINE_CONTROL"),
            S("D1-06", "HORIZON", "SCENE_D106_HORIZON", "CREW_ATTENDANT", "HORIZON", "HORIZON", "HORIZON", "VIP_LOUNGE", "VIP_LOUNGE", "ENGINE_CONTROL", "SECURITY", "HORIZON", "ENGINE_CONTROL"),
            S("D1-07", "MEDBAY", "SCENE_D107_MEDBAY", "SHIP_MEDIC", "MEDBAY", "MEDBAY", "MEDBAY", "VIP_LOUNGE", "VIP_LOUNGE", "MEDBAY", "MEDBAY", "MEDBAY", "ENGINE_CONTROL"),
            S("D2-01", "HORIZON", "SCENE_D201_HORIZON", "CREW_SECURITY", "HORIZON", "MEDBAY", "RICHARD_SUITE", "VIP_LOUNGE", "VIP_LOUNGE", "ENGINE_CONTROL", "HORIZON", "MEDBAY", "ENGINE_CONTROL"),
            S("D2-02", "HORIZON", "SCENE_D202_HORIZON", "CREW_ATTENDANT", "HORIZON", "MEDBAY", "RICHARD_SUITE", "VIP_LOUNGE", "VIP_LOUNGE", "ENGINE_CONTROL", "SECURITY", "HORIZON", "ENGINE_CONTROL"),
            S("D2-03", "MEDBAY", "SCENE_D203_MEDBAY", "SHIP_MEDIC", "MEDBAY", "MEDBAY", "RICHARD_SUITE", "VIP_LOUNGE", "VIP_LOUNGE", "ENGINE_CONTROL", "SECURITY", "MEDBAY", "ENGINE_CONTROL"),
            S("D2-04", "SECURITY", "SCENE_D204_SECURITY", "SECURITY_OPERATOR", "SECURITY", "MEDBAY", "RICHARD_SUITE", "VIP_LOUNGE", "VIP_LOUNGE", "ENGINE_CONTROL", "SECURITY", "MEDBAY", "ENGINE_CONTROL"),
            S("D2-05", "HORIZON", "SCENE_D205_HORIZON", "CREW_ENGINEER", "HORIZON", "MEDBAY", "RICHARD_SUITE", "VIP_LOUNGE", "VIP_LOUNGE", "ENGINE_CONTROL", "SECURITY", "MEDBAY", "HORIZON"),
            S("D2-06", "NEWS_LOUNGE", "SCENE_D206_NEWS", "PASSENGER_D", "NEWS_LOUNGE", "MEDBAY", "RICHARD_SUITE", "VIP_LOUNGE", "NEWS_LOUNGE", "ENGINE_CONTROL", "SECURITY", "MEDBAY", "ENGINE_CONTROL"),
            S("D3-01", "NEWS_LOUNGE", "SCENE_D301_NEWS", "PASSENGER_D", "NEWS_LOUNGE", "MEDBAY", "NEWS_LOUNGE", "VIP_LOUNGE", "NEWS_LOUNGE", "ENGINE_CONTROL", "SECURITY", "MEDBAY", "ENGINE_CONTROL"),
            S("D3-02", "RICHARD_SUITE", "SCENE_D302_SUITE", "SUITE_STEWARD", "RICHARD_SUITE", "MEDBAY", "RICHARD_SUITE", "VIP_LOUNGE", "VIP_LOUNGE", "ENGINE_CONTROL", "SECURITY", "MEDBAY", "ENGINE_CONTROL"),
            S("D3-03", "ENGINE_CONTROL", "SCENE_D303_ENGINE", "CHIEF_ENGINEER", "ENGINE_CONTROL", "MEDBAY", "RICHARD_SUITE", "VIP_LOUNGE", "VIP_LOUNGE", "ENGINE_CONTROL", "SECURITY", "MEDBAY", "ENGINE_CONTROL"),
            S("D3-04", "VAULT", "SCENE_D304_VAULT", "VAULT_GUARD", "VAULT", "MEDBAY", "RICHARD_SUITE", "VAULT", "VIP_LOUNGE", "ENGINE_CONTROL", "VAULT", "MEDBAY", "ENGINE_CONTROL"),
            S("D3-05", "PROMENADE", "SCENE_D305_PROMENADE", "PASSENGER_D", "PROMENADE", "MEDBAY", "RICHARD_SUITE", "VIP_LOUNGE", "PROMENADE", "ENGINE_CONTROL", "SECURITY", "MEDBAY", "ENGINE_CONTROL"),
            S("D4-01", "SECURITY", "SCENE_D401_SECURITY", "SECURITY_OPERATOR", "SECURITY", "MEDBAY", "RICHARD_SUITE", "VIP_LOUNGE", "VIP_LOUNGE", "ENGINE_CONTROL", "SECURITY", "MEDBAY", "ENGINE_CONTROL"),
            S("D4-02", "CREW_STAIRS", "SCENE_D402_STAIRS", "CREW_SECURITY", "CREW_STAIRS", "MEDBAY", "RICHARD_SUITE", "CREW_STAIRS", "VIP_LOUNGE", "ENGINE_CONTROL", "CREW_STAIRS", "CREW_STAIRS", "ENGINE_CONTROL"),
            S("D4-03", "CREW_STAIRS", "SCENE_D403_STAIRS", "CREW_ENGINEER", "CREW_STAIRS", "MEDBAY", "RICHARD_SUITE", "VIP_LOUNGE", "VIP_LOUNGE", "ENGINE_CONTROL", "MEDBAY", "MEDBAY", "CREW_STAIRS"),
            S("D4-04", "MEDBAY", "SCENE_D404_MEDBAY", "SHIP_MEDIC", "MEDBAY", "MEDBAY", "RICHARD_SUITE", "VIP_LOUNGE", "VIP_LOUNGE", "ENGINE_CONTROL", "MEDBAY", "MEDBAY", "ENGINE_CONTROL"),
            S("D5-01", "VIP_LOUNGE", "SCENE_D501_VIP", "VIP_HOST", "VIP_LOUNGE", "MEDBAY", "RICHARD_SUITE", "VIP_LOUNGE", "VIP_LOUNGE", "ENGINE_CONTROL", "MEDBAY", "MEDBAY", "ENGINE_CONTROL"),
            S("D5-02", "VIP_LOUNGE", "SCENE_D502_VIP", "ROBOTICS_TECH", "VIP_LOUNGE", "MEDBAY", "RICHARD_SUITE", "VIP_LOUNGE", "VIP_LOUNGE", "ENGINE_CONTROL", "MEDBAY", "MEDBAY", "ENGINE_CONTROL"),
            S("D5-03", "SECURITY", "SCENE_D503_SECURITY", "SECURITY_OPERATOR", "SECURITY", "MEDBAY", "RICHARD_SUITE", "VIP_LOUNGE", "SECURITY", "ENGINE_CONTROL", "MEDBAY", "MEDBAY", "ENGINE_CONTROL"),
            S("D5-04", "HORIZON", "SCENE_D504_HORIZON", "CREW_ATTENDANT", "HORIZON", "MEDBAY", "RICHARD_SUITE", "VIP_LOUNGE", "VIP_LOUNGE", "ENGINE_CONTROL", "MEDBAY", "MEDBAY", "ENGINE_CONTROL"),
            S("D6-01", "ENGINE_CONTROL", "SCENE_D601_ENGINE", "CHIEF_ENGINEER", "ENGINE_CONTROL", "MEDBAY", "RICHARD_SUITE", "VIP_LOUNGE", "VIP_LOUNGE", "ENGINE_CONTROL", "SECURITY", "MEDBAY", "ENGINE_CONTROL"),
            S("D6-02", "SERVICE_RAIL", "SCENE_D602_RAIL", "RAIL_TECHNICIAN", "SERVICE_RAIL", "MEDBAY", "RICHARD_SUITE", "VIP_LOUNGE", "VIP_LOUNGE", "ENGINE_CONTROL", "SECURITY", "MEDBAY", "SERVICE_RAIL"),
            S("D6-03", "BALLAST_CONTROL_ANNEX", "SCENE_D603_BALLAST", "BALLAST_CONTROLLER", "BALLAST_CONTROL_ANNEX", "MEDBAY", "RICHARD_SUITE", "VIP_LOUNGE", "VIP_LOUNGE", "ENGINE_CONTROL", "SECURITY", "BALLAST_CONTROL_ANNEX", "ENGINE_CONTROL"),
            S("D6-04", "MEDBAY", "SCENE_D604_MEDBAY", "SHIP_MEDIC", "MEDBAY", "MEDBAY", "RICHARD_SUITE", "VIP_LOUNGE", "VIP_LOUNGE", "ENGINE_CONTROL", "SECURITY", "MEDBAY", "ENGINE_CONTROL"),
            S("D6-05", "NEWS_LOUNGE", "SCENE_D605_NEWS", "PASSENGER_D", "NEWS_LOUNGE", "MEDBAY", "RICHARD_SUITE", "VIP_LOUNGE", "VIP_LOUNGE", "ENGINE_CONTROL", "SECURITY", "MEDBAY", "ENGINE_CONTROL"),
            S("D7-01", "VAULT", "SCENE_D701_VAULT", "VAULT_GUARD", "VAULT", "MEDBAY", "RICHARD_SUITE", "VAULT", "VIP_LOUNGE", "VAULT", "SECURITY", "MEDBAY", "ENGINE_CONTROL"),
            S("D7-02", "MEDBAY", "SCENE_D702_MEDBAY", "SHIP_MEDIC", "MEDBAY", "MEDBAY", "RICHARD_SUITE", "VIP_LOUNGE", "VIP_LOUNGE", "ENGINE_CONTROL", "SECURITY", "MEDBAY", "ENGINE_CONTROL"),
            S("D7-03", "ARCHIVE", "SCENE_D703_ARCHIVE", "ARCHIVIST", "ARCHIVE", "MEDBAY", "ARCHIVE", "VIP_LOUNGE", "VIP_LOUNGE", "ARCHIVE", "SECURITY", "MEDBAY", "ARCHIVE"),
            S("D7-04", "PROMENADE", "SCENE_D704_PROMENADE", "CREW_ATTENDANT", "PROMENADE", "MEDBAY", "RICHARD_SUITE", "PROMENADE", "VIP_LOUNGE", "ENGINE_CONTROL", "SECURITY", "MEDBAY", "ENGINE_CONTROL"),
            S("D8-01", "HORIZON", "SCENE_D801_HORIZON", "CREW_SECURITY", "HORIZON", "MEDBAY", "HORIZON", "HORIZON", "HORIZON", "HORIZON", "HORIZON", "HORIZON", "HORIZON"),
            S("D8-02", "OPEN_DECK", "SCENE_D802_DECK", "CREW_SECURITY", "OPEN_DECK", "MEDBAY", "RICHARD_SUITE", "OPEN_DECK", "VIP_LOUNGE", "ENGINE_CONTROL", "OPEN_DECK", "MEDBAY", "ENGINE_CONTROL"),
            S("D8-03", "PORT", "SCENE_D803_PORT", "DOCK_PORTER", "PORT", "PORT", "PORT", "GANGWAY", "PORT", "PORT", "PORT", "PORT", "PORT")
        };

        private static readonly IReadOnlyDictionary<string, ScenePresenceRecord>
            ByScene = Entries.ToDictionary(
                entry => entry.SceneId,
                StringComparer.Ordinal);

        public static IReadOnlyList<ScenePresenceRecord> All => Entries;

        public static bool TryGet(
            string sceneId,
            out ScenePresenceRecord record)
        {
            return ByScene.TryGetValue(
                sceneId?.Trim().ToUpperInvariant() ?? string.Empty,
                out record);
        }

        private static ScenePresenceRecord S(
            string sceneId,
            string focusLocation,
            string contextBarkId,
            string contextSpeaker,
            params string[] locations)
        {
            if (locations.Length != MainCharacterIds.Count)
            {
                throw new ArgumentException(
                    $"Scene {sceneId} must place every main character.");
            }

            return new ScenePresenceRecord(
                EntriesInitializedCount(),
                sceneId,
                focusLocation,
                contextBarkId,
                contextSpeaker,
                MainCharacterIds
                    .Select((character, index) =>
                        new KeyValuePair<string, string>(
                            character,
                            locations[index]))
                    .ToDictionary(
                        pair => pair.Key,
                        pair => pair.Value,
                        StringComparer.Ordinal));
        }

        private static int entriesInitialized;

        private static int EntriesInitializedCount() => entriesInitialized++;
    }
}
