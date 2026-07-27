using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text.RegularExpressions;

namespace Wake.Narrative
{
    public enum ProductionEffectKind
    {
        Marker,
        Trust,
        Hostility,
        PublicAnxiety,
        EvidenceIntegrity,
        TimeBlock,
        WrongStrike,
        Flag,
        Evidence,
        SceneUnlock,
        Ending,
        Metadata
    }

    public sealed class ProductionEffectInstruction
    {
        public ProductionEffectInstruction(
            ProductionEffectKind kind,
            string key,
            string value,
            int? numericValue)
        {
            Kind = kind;
            Key = key;
            Value = value;
            NumericValue = numericValue;
        }

        public ProductionEffectKind Kind { get; }
        public string Key { get; }
        public string Value { get; }
        public int? NumericValue { get; }
        public bool HasNumericValue => NumericValue.HasValue;

        public IReadOnlyList<string> Values => Value
            .Split(new[] { ',' }, StringSplitOptions.RemoveEmptyEntries)
            .Select(item => item.Trim())
            .Where(item => item.Length > 0)
            .ToArray();
    }

    public sealed class ProductionEffectParseResult
    {
        public ProductionEffectParseResult(
            IReadOnlyList<ProductionEffectInstruction> instructions,
            IReadOnlyList<string> errors)
        {
            Instructions = instructions;
            Errors = errors;
        }

        public IReadOnlyList<ProductionEffectInstruction> Instructions { get; }
        public IReadOnlyList<string> Errors { get; }
        public bool Success => Errors.Count == 0;
    }

    public static class ProductionEffectParser
    {
        private static readonly Regex KeyPattern = new(
            @"^[A-Za-z][A-Za-z0-9_]*$",
            RegexOptions.Compiled | RegexOptions.CultureInvariant);
        private static readonly HashSet<string> NumericKeys =
            new(StringComparer.Ordinal)
            {
                "publicAnxiety",
                "evidenceIntegrity",
                "timeBlock",
                "wrong_strike"
            };

        public static ProductionEffectParseResult Parse(string source)
        {
            var instructions = new List<ProductionEffectInstruction>();
            var errors = new List<string>();
            if (string.IsNullOrWhiteSpace(source))
                return new ProductionEffectParseResult(instructions, errors);

            string[] tokens = source.Split(';');
            for (int index = 0; index < tokens.Length; index++)
            {
                string token = tokens[index].Trim();
                if (token.Length == 0)
                {
                    errors.Add($"Effect token {index + 1} is empty.");
                    continue;
                }
                int separator = token.IndexOf(':');
                string key = separator < 0
                    ? token
                    : token.Substring(0, separator).Trim();
                string value = separator < 0
                    ? string.Empty
                    : token.Substring(separator + 1).Trim();
                if (!KeyPattern.IsMatch(key))
                {
                    errors.Add(
                        $"Effect token {index + 1} has invalid key '{key}'.");
                    continue;
                }
                if (separator >= 0 && value.Length == 0)
                {
                    errors.Add(
                        $"Effect token {index + 1} '{key}' has no value.");
                    continue;
                }

                ProductionEffectKind kind = Classify(key, separator >= 0);
                int? numericValue = null;
                if (RequiresInteger(kind, key))
                {
                    if (!int.TryParse(
                            value,
                            NumberStyles.AllowLeadingSign,
                            CultureInfo.InvariantCulture,
                            out int parsed))
                    {
                        errors.Add(
                            $"Effect token {index + 1} '{key}' requires " +
                            $"a signed integer, found '{value}'.");
                        continue;
                    }
                    numericValue = parsed;
                }
                if ((kind == ProductionEffectKind.SceneUnlock ||
                     kind == ProductionEffectKind.Evidence) &&
                    SplitValues(value).Count == 0)
                {
                    errors.Add(
                        $"Effect token {index + 1} '{key}' has no IDs.");
                    continue;
                }
                instructions.Add(new ProductionEffectInstruction(
                    kind, key, value, numericValue));
            }
            return new ProductionEffectParseResult(instructions, errors);
        }

        private static ProductionEffectKind Classify(
            string key,
            bool hasValue)
        {
            if (key.StartsWith("trust_", StringComparison.Ordinal))
                return ProductionEffectKind.Trust;
            if (key.StartsWith("hostility_", StringComparison.Ordinal))
                return ProductionEffectKind.Hostility;
            return key switch
            {
                "publicAnxiety" => ProductionEffectKind.PublicAnxiety,
                "evidenceIntegrity" => ProductionEffectKind.EvidenceIntegrity,
                "timeBlock" => ProductionEffectKind.TimeBlock,
                "wrong_strike" => ProductionEffectKind.WrongStrike,
                "flag" => ProductionEffectKind.Flag,
                "evidence" => ProductionEffectKind.Evidence,
                "scene_unlock" => ProductionEffectKind.SceneUnlock,
                "ending" => ProductionEffectKind.Ending,
                _ => hasValue
                    ? ProductionEffectKind.Metadata
                    : ProductionEffectKind.Marker
            };
        }

        private static bool RequiresInteger(
            ProductionEffectKind kind,
            string key)
        {
            return kind == ProductionEffectKind.Trust ||
                   kind == ProductionEffectKind.Hostility ||
                   NumericKeys.Contains(key);
        }

        private static IReadOnlyList<string> SplitValues(string value)
        {
            return value.Split(
                    new[] { ',' },
                    StringSplitOptions.RemoveEmptyEntries)
                .Select(item => item.Trim())
                .Where(item => item.Length > 0)
                .ToArray();
        }
    }
}
