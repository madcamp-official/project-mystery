using System;
using System.Collections.Generic;
using System.Linq;

namespace Wake.Exploration
{
    public readonly struct SceneWorldCharacter
    {
        public SceneWorldCharacter(
            string characterId,
            SceneCharacterState state,
            bool isFocusParticipant,
            bool isOffCamera)
        {
            CharacterId = characterId ?? string.Empty;
            State = state;
            IsFocusParticipant = isFocusParticipant;
            IsOffCamera = isOffCamera;
        }

        public string CharacterId { get; }
        public SceneCharacterState State { get; }
        public bool IsFocusParticipant { get; }
        public bool IsOffCamera { get; }
    }

    /// <summary>
    /// Converts the workbook's logical occupancy into a safe world presentation.
    /// The detective remains the player's viewpoint, deceased characters never
    /// stand as an ambient actor, and crowded ensemble scenes retain their full
    /// logical cast while limiting the foreground to readable silhouettes.
    /// </summary>
    public static class ScenePresencePresentationPolicy
    {
        public const int DefaultVisibleLimit = 3;

        private static readonly IReadOnlyDictionary<string, string[]>
            ScenePriorities =
                new Dictionary<string, string[]>(StringComparer.Ordinal)
                {
                    ["P-01"] = C("DANIEL"),
                    ["P-02"] = C("DANIEL", "RICHARD", "EVELYN"),
                    ["P-03"] = C("RICHARD"),
                    ["D1-01"] = C("CLAIRE", "MARCUS", "HELENA", "OWEN"),
                    ["D1-02"] = C("DANIEL", "EVELYN"),
                    ["D1-03"] = C("DANIEL", "EVELYN", "RICHARD"),
                    ["D1-04"] = C(),
                    ["D1-05"] = C("RICHARD", "EVELYN"),
                    ["D1-06"] = C("HELENA"),
                    ["D1-07"] = C("THOMAS", "MARCUS", "HELENA"),
                    ["D2-01"] = C("MARCUS"),
                    ["D2-02"] = C("HELENA"),
                    ["D2-03"] = C("HELENA"),
                    ["D2-04"] = C("MARCUS"),
                    ["D2-05"] = C("OWEN"),
                    ["D2-06"] = C("EVELYN"),
                    ["D3-01"] = C("RICHARD", "EVELYN"),
                    ["D3-02"] = C("RICHARD"),
                    ["D3-03"] = C("THOMAS", "OWEN"),
                    ["D3-04"] = C("EVELYN", "MARCUS"),
                    ["D3-05"] = C("CLAIRE"),
                    ["D4-01"] = C("MARCUS"),
                    ["D4-02"] = C("MARCUS", "EVELYN", "HELENA"),
                    ["D4-03"] = C("OWEN"),
                    ["D4-04"] = C("HELENA", "MARCUS"),
                    ["D5-01"] = C("CLAIRE", "EVELYN"),
                    ["D5-02"] = C("CLAIRE", "EVELYN"),
                    ["D5-03"] = C("EVELYN"),
                    ["D5-04"] = C("HELENA"),
                    ["D6-01"] = C("THOMAS", "OWEN"),
                    ["D6-02"] = C("OWEN"),
                    ["D6-03"] = C("HELENA"),
                    ["D6-04"] = C("HELENA"),
                    ["D6-05"] = C("EVELYN"),
                    ["D7-01"] = C("EVELYN", "THOMAS"),
                    ["D7-02"] = C("HELENA"),
                    ["D7-03"] = C("RICHARD", "THOMAS", "OWEN"),
                    ["D7-04"] = C("EVELYN"),
                    ["D8-01"] = C("EVELYN", "RICHARD", "CLAIRE"),
                    ["D8-02"] = C("EVELYN", "MARCUS"),
                    ["D8-03"] = C("RICHARD", "CLAIRE", "MARCUS")
                };

        private static readonly IReadOnlyDictionary<string, string[]>
            SceneHiddenCharacters =
                new Dictionary<string, string[]>(StringComparer.Ordinal)
                {
                    // D1-04 keeps Daniel's last confirmed location as story
                    // data, while the player sees only the witness who remains.
                    ["D1-04"] = C("DANIEL")
                };

        public static IReadOnlyList<SceneWorldCharacter> Select(
            ScenePresenceRecord scene,
            string locationCode,
            int visibleLimit = DefaultVisibleLimit)
        {
            if (scene == null || visibleLimit <= 0)
                return Array.Empty<SceneWorldCharacter>();

            string location = Normalize(locationCode);
            IReadOnlyList<string> occupants =
                scene.GetCharactersAt(location);
            if (occupants.Count == 0)
                return Array.Empty<SceneWorldCharacter>();

            HashSet<string> localCharacters =
                new(occupants, StringComparer.Ordinal);
            IEnumerable<string> ordered =
                GetPriorities(scene.SceneId)
                    .Concat(ScenePresenceCatalog.MainCharacterIds)
                    .Where(character => localCharacters.Contains(character))
                    .Distinct(StringComparer.Ordinal)
                    .Where(character => IsPresentable(scene, character));

            bool focusLocation = string.Equals(
                scene.FocusLocation,
                location,
                StringComparison.Ordinal);
            return ordered
                .Select((character, index) =>
                    new SceneWorldCharacter(
                        character,
                        scene.GetState(character),
                        focusLocation &&
                        GetPriorities(scene.SceneId).Contains(
                            character,
                            StringComparer.Ordinal),
                        index >= visibleLimit))
                .ToArray();
        }

        public static IReadOnlyList<SceneWorldCharacter> SelectVisible(
            ScenePresenceRecord scene,
            string locationCode,
            int visibleLimit = DefaultVisibleLimit)
        {
            return Select(scene, locationCode, visibleLimit)
                .Where(character => !character.IsOffCamera)
                .ToArray();
        }

        private static bool IsPresentable(
            ScenePresenceRecord scene,
            string character)
        {
            if (SceneHiddenCharacters.TryGetValue(
                    scene.SceneId,
                    out string[] hidden) &&
                hidden.Contains(character, StringComparer.Ordinal))
            {
                return false;
            }

            if (string.Equals(
                    character,
                    "ADRIAN",
                    StringComparison.Ordinal))
            {
                return false;
            }

            return scene.GetState(character) !=
                   SceneCharacterState.Deceased;
        }

        private static IReadOnlyList<string> GetPriorities(string sceneId)
        {
            return ScenePriorities.TryGetValue(
                Normalize(sceneId),
                out string[] priorities)
                ? priorities
                : Array.Empty<string>();
        }

        private static string[] C(params string[] characters) => characters;

        private static string Normalize(string value) =>
            value?.Trim().ToUpperInvariant() ?? string.Empty;
    }
}
