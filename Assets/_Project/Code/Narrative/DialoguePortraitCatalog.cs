using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace Wake.Narrative
{
    public sealed class DialoguePortraitDefinition
    {
        public DialoguePortraitDefinition(
            string characterId,
            string displayName,
            string expressionSheet,
            string fallbackTexture,
            Rect fallbackCrop)
        {
            CharacterId = characterId ?? string.Empty;
            DisplayName = displayName ?? string.Empty;
            ExpressionSheet = expressionSheet ?? string.Empty;
            FallbackTexture = fallbackTexture ?? string.Empty;
            FallbackCrop = fallbackCrop;
        }

        public string CharacterId { get; }
        public string DisplayName { get; }
        public string ExpressionSheet { get; }
        public string FallbackTexture { get; }
        public Rect FallbackCrop { get; }
        public bool UsesExpressionSprites =>
            !FallbackTexture.StartsWith(
                "AmbientCharacters/",
                StringComparison.OrdinalIgnoreCase);
    }

    public readonly struct DialoguePortraitAsset
    {
        public DialoguePortraitAsset(
            bool found,
            Texture texture,
            Rect uvRect,
            float aspectRatio,
            string spriteName,
            bool usesExpression)
        {
            Found = found;
            Texture = texture;
            UvRect = uvRect;
            AspectRatio = aspectRatio;
            SpriteName = spriteName ?? string.Empty;
            UsesExpression = usesExpression;
        }

        public bool Found { get; }
        public Texture Texture { get; }
        public Rect UvRect { get; }
        public float AspectRatio { get; }
        public string SpriteName { get; }
        public bool UsesExpression { get; }
    }

    public static class DialoguePortraitCatalog
    {
        public const string ResourceFolder = "CharacterExpressions";

        private static readonly Rect StandardFallback =
            new(0.46f, 0f, 0.54f, 1f);
        private const string PublicSpecialists =
            "AmbientCharacters/world_atlas_public_specialists";
        private const string OperationsSpecialists =
            "AmbientCharacters/world_atlas_operations_specialists";
        private const string ServiceSpecialists =
            "AmbientCharacters/world_atlas_service_specialists";

        private static readonly DialoguePortraitDefinition[] Entries =
        {
            D("ADRIAN", "Adrian Vale", "adrian_vale"),
            D("CLAIRE", "Claire Hawthorne", "claire_hawthorne"),
            D("DANIEL", "Daniel Mercer", "daniel_mercer"),
            D("RICHARD", "Richard Hawthorne", "richard_hawthorne"),
            D("EVELYN", "Evelyn Shaw", "evelyn_shaw"),
            D("THOMAS", "Thomas Reed", "thomas_reed"),
            D("MARCUS", "Marcus Bell", "marcus_bell",
                "marcus_bell_and_helena_ward",
                new Rect(0.25f, 0f, 0.28f, 1f)),
            D("HELENA", "Helena Ward", "helena_ward",
                "marcus_bell_and_helena_ward",
                new Rect(0.70f, 0f, 0.30f, 1f)),
            D("OWEN", "Owen Price", "owen_price"),
            D("PASSENGER_A", "승객", "passenger_a",
                "AmbientCharacters/passenger_a"),
            D("PASSENGER_B", "승객", "passenger_b",
                "AmbientCharacters/passenger_b"),
            D("PASSENGER_C", "승객", "passenger_c",
                "AmbientCharacters/passenger_c"),
            D("PASSENGER_D", "승객", "passenger_d",
                "AmbientCharacters/passenger_d"),
            D("PASSENGER_E", "승객", "passenger_e",
                "AmbientCharacters/passenger_e"),
            D("PASSENGER_F", "승객", "passenger_f",
                "AmbientCharacters/passenger_f"),
            D("CREW_ATTENDANT", "객실 승무원", "crew_attendant",
                "AmbientCharacters/crew_attendant"),
            D("CREW_ENGINEER", "기관 승무원", "crew_engineer",
                "AmbientCharacters/crew_engineer"),
            D("CREW_SECURITY", "보안 승무원", "crew_security",
                "AmbientCharacters/crew_security"),
            W("DOCK_PORTER", "항만 운반원", PublicSpecialists, 0),
            W("VIP_HOST", "VIP 라운지 매니저", PublicSpecialists, 1),
            W("BALLROOM_MUSICIAN", "무도회장 바이올리니스트",
                PublicSpecialists, 2),
            W("DINING_SOMMELIER", "식당 소믈리에",
                PublicSpecialists, 3),
            W("ATRIUM_GUIDE", "중앙 홀 안내원", PublicSpecialists, 4),
            W("SECURITY_OPERATOR", "보안 관제원",
                OperationsSpecialists, 0),
            W("RAIL_TECHNICIAN", "서비스 레일 기술자",
                OperationsSpecialists, 1),
            W("SHIP_MEDIC", "선내 의무관", OperationsSpecialists, 2),
            W("BALLAST_CONTROLLER", "밸러스트 제어원",
                OperationsSpecialists, 3),
            W("CHIEF_ENGINEER", "수석 기관사",
                OperationsSpecialists, 4),
            W("SUITE_STEWARD", "스위트 전담 승무원",
                ServiceSpecialists, 0),
            W("ARCHIVIST", "선박 기록 보관관", ServiceSpecialists, 1),
            W("LAUNDRY_SUPERVISOR", "세탁실 감독관",
                ServiceSpecialists, 2),
            W("ROBOTICS_TECH", "로봇 정비사", ServiceSpecialists, 3),
            W("WORKSHOP_MACHINIST", "공작실 기계공",
                ServiceSpecialists, 4)
        };

        private static readonly IReadOnlyDictionary<string, DialoguePortraitDefinition>
            ById = Entries
                .Select(entry =>
                    new KeyValuePair<string, DialoguePortraitDefinition>(
                        entry.CharacterId,
                        entry))
                .Concat(Entries
                    .Where(entry =>
                        !string.IsNullOrWhiteSpace(entry.DisplayName))
                    .GroupBy(
                        entry => entry.DisplayName,
                        StringComparer.OrdinalIgnoreCase)
                    .Select(group =>
                        new KeyValuePair<string, DialoguePortraitDefinition>(
                            group.Key,
                            group.First())))
                .GroupBy(
                    pair => pair.Key,
                    StringComparer.OrdinalIgnoreCase)
                .ToDictionary(
                    group => group.Key,
                    group => group.First().Value,
                    StringComparer.OrdinalIgnoreCase);

        public static IReadOnlyList<DialoguePortraitDefinition> All => Entries;

        public static bool TryGet(
            string characterId,
            out DialoguePortraitDefinition definition)
        {
            string exact = string.IsNullOrWhiteSpace(characterId)
                ? string.Empty
                : characterId.Trim();
            return ById.TryGetValue(exact, out definition) ||
                   ById.TryGetValue(
                       NormalizeCharacterId(exact),
                       out definition);
        }

        public static string GetSpriteName(
            DialoguePortraitDefinition definition,
            PortraitEmotion emotion)
        {
            if (definition == null)
            {
                return string.Empty;
            }

            return $"portrait_{definition.ExpressionSheet}_{EmotionSuffix(emotion)}";
        }

        public static DialoguePortraitAsset Resolve(
            string characterId,
            PortraitEmotion emotion)
        {
            if (!TryGet(characterId, out DialoguePortraitDefinition definition))
            {
                return default;
            }

            string spriteName = GetSpriteName(definition, emotion);
            Sprite sprite = Resources
                .LoadAll<Sprite>(
                    $"{ResourceFolder}/portrait_{definition.ExpressionSheet}_expressions")
                .FirstOrDefault(item => item.name == spriteName);
            if (sprite != null && sprite.texture != null)
            {
                Rect pixels = sprite.rect;
                Texture2D texture = sprite.texture;
                Rect uv = new(
                    pixels.x / texture.width,
                    pixels.y / texture.height,
                    pixels.width / texture.width,
                    pixels.height / texture.height);
                return new DialoguePortraitAsset(
                    true,
                    texture,
                    uv,
                    pixels.width / pixels.height,
                    spriteName,
                    true);
            }

            string fallbackPath = definition.FallbackTexture.Contains("/")
                ? definition.FallbackTexture
                : $"Characters/{definition.FallbackTexture}";
            Texture2D fallback = Resources.Load<Texture2D>(fallbackPath);
            if (fallback == null)
            {
                return default;
            }

            Rect crop = definition.FallbackCrop;
            return new DialoguePortraitAsset(
                true,
                fallback,
                crop,
                fallback.width * crop.width / (fallback.height * crop.height),
                string.Empty,
                false);
        }

        public static string GetDisplayName(string characterId)
        {
            return TryGet(characterId, out DialoguePortraitDefinition definition)
                ? definition.DisplayName
                : characterId ?? string.Empty;
        }

        private static DialoguePortraitDefinition D(
            string id,
            string displayName,
            string sheet,
            string fallback = null,
            Rect? crop = null)
        {
            return new DialoguePortraitDefinition(
                id,
                displayName,
                sheet,
                fallback ?? sheet,
                crop ?? StandardFallback);
        }

        private static DialoguePortraitDefinition W(
            string id,
            string displayName,
            string atlas,
            int column)
        {
            const float cellWidth = 0.2f;
            return D(
                id,
                displayName,
                id.ToLowerInvariant(),
                atlas,
                new Rect(
                    column * cellWidth,
                    0.38f,
                    cellWidth,
                    0.62f));
        }

        private static string EmotionSuffix(PortraitEmotion emotion) => emotion switch
        {
            PortraitEmotion.Concerned => "concerned",
            PortraitEmotion.Angry => "angry",
            PortraitEmotion.Positive => "happy",
            _ => "neutral"
        };

        private static string NormalizeCharacterId(string value)
        {
            string lookup = string.IsNullOrWhiteSpace(value)
                ? string.Empty
                : value.Trim();
            int separator = lookup.IndexOf('_');
            return separator > 0 ? lookup.Substring(0, separator) : lookup;
        }
    }
}
