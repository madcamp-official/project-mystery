using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using Wake.Exploration;

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
        private static readonly DialoguePortraitDefinition[] Entries =
        {
            D("ADRIAN", "아드리안 베일", "adrian_vale"),
            D("CLAIRE", "클레어 호손", "claire_hawthorne"),
            D("DANIEL", "다니엘 머서", "daniel_mercer"),
            D("RICHARD", "리처드 호손", "richard_hawthorne"),
            D("EVELYN", "이블린 쇼", "evelyn_shaw"),
            D("THOMAS", "토머스 리드", "thomas_reed"),
            D("MARCUS", "마커스 벨", "marcus_bell",
                "marcus_bell_and_helena_ward",
                new Rect(0.25f, 0f, 0.28f, 1f)),
            D("HELENA", "헬레나 워드", "helena_ward",
                "marcus_bell_and_helena_ward",
                new Rect(0.70f, 0f, 0.30f, 1f)),
            D("OWEN", "오언 프라이스", "owen_price"),
            D("PASSENGER_A", "승객", "passenger_a",
                "AmbientCharacters/passenger_a_expressions"),
            D("PASSENGER_ATRIUM", "승객", "passenger_a",
                "AmbientCharacters/passenger_a_expressions"),
            D("PASSENGER_PROMENADE_2", "승객", "passenger_a",
                "AmbientCharacters/passenger_a_expressions"),
            D("PASSENGER_B", "승객", "passenger_b",
                "AmbientCharacters/passenger_b_expressions"),
            D("PASSENGER_C", "승객", "passenger_c",
                "AmbientCharacters/passenger_c_expressions"),
            D("PASSENGER_D", "승객", "passenger_d",
                "AmbientCharacters/passenger_d_expressions"),
            D("PASSENGER_NEWS", "승객", "passenger_d",
                "AmbientCharacters/passenger_d_expressions"),
            D("PASSENGER_PROMENADE", "승객", "passenger_d",
                "AmbientCharacters/passenger_d_expressions"),
            D("PASSENGER_E", "승객", "passenger_e",
                "AmbientCharacters/passenger_e_expressions"),
            D("PASSENGER_DECK", "승객", "passenger_e",
                "AmbientCharacters/passenger_e_expressions"),
            D("PASSENGER_F", "승객", "passenger_f",
                "AmbientCharacters/passenger_f_expressions"),
            D("CREW_ATTENDANT", "객실 승무원", "crew_attendant",
                "AmbientCharacters/crew_attendant_expressions"),
            D("CREW_ATTENDANT_BALLROOM", "객실 승무원", "crew_attendant",
                "AmbientCharacters/crew_attendant_expressions"),
            D("CREW_ENGINEER", "기관 승무원", "crew_engineer",
                "AmbientCharacters/crew_engineer_expressions"),
            D("CREW_ENGINEER_GENERATOR", "기관 승무원", "crew_engineer",
                "AmbientCharacters/crew_engineer_expressions"),
            D("CREW_SECURITY", "보안 승무원", "crew_security",
                "AmbientCharacters/crew_security_expressions"),
            D("CREW_SECURITY_DECK", "보안 승무원", "crew_security",
                "AmbientCharacters/crew_security_expressions"),
            D("CREW_SECURITY_STAIRS", "보안 승무원", "crew_security",
                "AmbientCharacters/crew_security_expressions"),
            D("CREW_SECURITY_VAULT", "보안 승무원", "crew_security",
                "AmbientCharacters/crew_security_expressions"),
            W("DOCK_PORTER", "항만 운반원", "dock_porter"),
            W("VIP_HOST", "VIP 라운지 매니저", "VIP_host"),
            W("BALLROOM_MUSICIAN", "무도회장 바이올리니스트",
                "ballroom_musician"),
            W("DINING_SOMMELIER", "식당 소믈리에",
                "dining_sommelier"),
            W("ATRIUM_GUIDE", "중앙 홀 안내원", "atrium_guide"),
            W("SECURITY_OPERATOR", "보안 관제원",
                "security_operator"),
            W("RAIL_TECHNICIAN", "서비스 레일 기술자",
                "rail_technician"),
            W("SHIP_MEDIC", "선내 의무관", "ship_medic"),
            W("BALLAST_CONTROLLER_TANKS", "밸러스트 제어원",
                "ballast_controller"),
            W("BALLAST_CONTROLLER", "밸러스트 제어원",
                "ballast_controller"),
            W("CHIEF_ENGINEER", "수석 기관사",
                "chief_engineer"),
            W("SUITE_STEWARD", "스위트 전담 승무원",
                "suite_steward"),
            W("ARCHIVIST", "선박 기록 보관관", "archivist"),
            W("LAUNDRY_SUPERVISOR", "세탁실 감독관",
                "suite_steward"),
            W("ROBOTICS_TECH", "로봇 정비사", "robotics_tech"),
            W("WORKSHOP_MACHINIST", "공작실 기계공",
                "chief_engineer")
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

            Rect crop = definition.FallbackTexture.EndsWith(
                "_expressions",
                StringComparison.OrdinalIgnoreCase)
                ? SpecialistExpressionCrop(emotion)
                : definition.FallbackCrop;
            return new DialoguePortraitAsset(
                true,
                fallback,
                crop,
                fallback.width * crop.width / (fallback.height * crop.height),
                string.Empty,
                false);
        }

        public static DialoguePortraitAsset ResolveWorldFigure(
            string characterId)
        {
            if (!TryGet(characterId, out DialoguePortraitDefinition definition))
                return default;

            bool useIsolatedAttendantFigure =
                string.Equals(
                    characterId,
                    "CREW_ATTENDANT",
                    StringComparison.OrdinalIgnoreCase) ||
                string.Equals(
                    characterId,
                    "CREW_ATTENDANT_BALLROOM",
                    StringComparison.OrdinalIgnoreCase);
            if (useIsolatedAttendantFigure &&
                AmbientWorldCharacterCatalog.TryGetAsset(
                    characterId,
                    out AmbientWorldCharacterAsset worldAsset))
            {
                Texture2D worldFigure = Resources.Load<Texture2D>(
                    worldAsset.ResourcePath);
                if (worldFigure != null)
                    return FullFigure(worldFigure, worldAsset.UvRect);
            }

            Texture2D mainFigure = Resources.Load<Texture2D>(
                $"WorldMainCharacters/{definition.ExpressionSheet}");
            if (mainFigure != null)
            {
                return FullFigure(
                    mainFigure,
                    new Rect(0f, 0f, 1f, 1f));
            }

            string fallbackPath = definition.FallbackTexture.Contains("/")
                ? definition.FallbackTexture
                : $"Characters/{definition.FallbackTexture}";
            Texture2D fallback = Resources.Load<Texture2D>(fallbackPath);
            if (fallback == null)
                return default;

            Rect uv;
            if (fallbackPath.EndsWith(
                    "_expressions",
                    StringComparison.OrdinalIgnoreCase))
            {
                uv = new Rect(0f, 0f, 0.25f, 1f);
            }
            else if (fallbackPath.Contains(
                         "world_atlas_",
                         StringComparison.OrdinalIgnoreCase))
            {
                uv = new Rect(
                    definition.FallbackCrop.x,
                    0f,
                    definition.FallbackCrop.width,
                    1f);
            }
            else
            {
                uv = new Rect(0f, 0f, 1f, 1f);
            }
            return FullFigure(fallback, uv);
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
            string resourceName)
        {
            return D(
                id,
                displayName,
                id.ToLowerInvariant(),
                $"AmbientCharacters/{resourceName}_expressions",
                new Rect(0.25f, 0f, 0.25f, 1f));
        }

        private static Rect SpecialistExpressionCrop(PortraitEmotion emotion)
        {
            int column = emotion switch
            {
                PortraitEmotion.Concerned => 2,
                PortraitEmotion.Angry => 3,
                _ => 1
            };
            return new Rect(column * 0.25f, 0f, 0.25f, 1f);
        }

        private static DialoguePortraitAsset FullFigure(
            Texture2D texture,
            Rect uv)
        {
            return new DialoguePortraitAsset(
                true,
                texture,
                uv,
                texture.width * uv.width /
                (texture.height * uv.height),
                string.Empty,
                false);
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
