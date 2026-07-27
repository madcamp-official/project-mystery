using System;
using System.Text.RegularExpressions;
using Wake.Core;

namespace Wake.Narrative
{
    public readonly struct ProductionConditionResult
    {
        public ProductionConditionResult(bool isMet, string diagnostic = "")
        {
            IsMet = isMet;
            Diagnostic = diagnostic ?? string.Empty;
        }

        public bool IsMet { get; }
        public string Diagnostic { get; }
    }

    public sealed class ProductionConditionEvaluator
    {
        private static readonly Regex ChoicePattern = new(
            @"^choice\((?<id>[^)]+)\)$",
            RegexOptions.Compiled | RegexOptions.CultureInvariant);
        private static readonly Regex FlagPattern = new(
            @"^flag\((?<id>[^)]+)\)$",
            RegexOptions.Compiled | RegexOptions.CultureInvariant);
        private static readonly Regex AllFlagsPattern = new(
            @"^all_flags\((?<ids>[^)]+)\)$",
            RegexOptions.Compiled | RegexOptions.CultureInvariant);
        private static readonly Regex ComparisonPattern = new(
            @"^(?<key>[A-Za-z][A-Za-z0-9_]*)(?<op>>=|<=|=|>|<)(?<value>.+)$",
            RegexOptions.Compiled | RegexOptions.CultureInvariant);

        private readonly GameStateManager state;

        public ProductionConditionEvaluator(GameStateManager state)
        {
            this.state = state;
        }

        public ProductionConditionResult Evaluate(string source)
        {
            if (string.IsNullOrWhiteSpace(source) || source.Trim() == "없음")
            {
                return new ProductionConditionResult(true);
            }
            if (state == null)
            {
                return new ProductionConditionResult(
                    false,
                    "Game state is required to evaluate a production condition.");
            }
            return EvaluateExpression(source.Trim());
        }

        public static string ChoiceFlag(string choiceId) =>
            $"choice_{Normalize(choiceId)}";

        private ProductionConditionResult EvaluateExpression(string expression)
        {
            int orIndex = FindTopLevelOperator(expression, " or ");
            if (orIndex >= 0)
            {
                ProductionConditionResult left =
                    EvaluateExpression(expression.Substring(0, orIndex));
                return left.IsMet
                    ? left
                    : EvaluateExpression(expression.Substring(orIndex + 4));
            }

            int andIndex = FindTopLevelOperator(expression, " and ");
            if (andIndex >= 0)
            {
                ProductionConditionResult left =
                    EvaluateExpression(expression.Substring(0, andIndex));
                return !left.IsMet
                    ? left
                    : EvaluateExpression(expression.Substring(andIndex + 5));
            }

            return EvaluateAtomic(expression.Trim());
        }

        private ProductionConditionResult EvaluateAtomic(string expression)
        {
            if (expression.StartsWith("not ", StringComparison.Ordinal))
            {
                ProductionConditionResult inner =
                    EvaluateAtomic(expression.Substring(4).Trim());
                return new ProductionConditionResult(!inner.IsMet, inner.Diagnostic);
            }

            Match match = ChoicePattern.Match(expression);
            if (match.Success)
            {
                return FromFlag(ChoiceFlag(match.Groups["id"].Value));
            }
            match = FlagPattern.Match(expression);
            if (match.Success)
            {
                return FromFlag(match.Groups["id"].Value);
            }
            match = AllFlagsPattern.Match(expression);
            if (match.Success)
            {
                foreach (string flag in match.Groups["ids"].Value.Split(','))
                {
                    if (!state.HasFlag(flag.Trim()))
                    {
                        return new ProductionConditionResult(false);
                    }
                }
                return new ProductionConditionResult(true);
            }

            if (expression == "all_accusations_correct")
            {
                for (int index = 1; index <= 6; index++)
                {
                    if (!state.HasFlag($"accusation{index}_correct"))
                    {
                        return new ProductionConditionResult(false);
                    }
                }
                return new ProductionConditionResult(true);
            }
            if (expression.StartsWith("ending:", StringComparison.Ordinal))
            {
                return new ProductionConditionResult(
                    string.Equals(
                        state.FinalEndingId,
                        expression.Substring(7),
                        StringComparison.OrdinalIgnoreCase));
            }
            if (expression.StartsWith("flag:", StringComparison.Ordinal))
            {
                return FromFlag(expression.Substring(5));
            }
            if (LooksLikeSceneId(expression))
            {
                return new ProductionConditionResult(state.HasCompletedScene(expression));
            }

            match = ComparisonPattern.Match(expression);
            if (match.Success)
            {
                return EvaluateComparison(
                    match.Groups["key"].Value,
                    match.Groups["op"].Value,
                    match.Groups["value"].Value);
            }

            int separator = expression.IndexOf(':');
            if (separator > 0)
            {
                return FromFlag(
                    expression.Substring(0, separator) + "_" +
                    expression.Substring(separator + 1));
            }
            return FromFlag(expression);
        }

        private ProductionConditionResult EvaluateComparison(
            string key,
            string operation,
            string expectedText)
        {
            if (!int.TryParse(expectedText, out int expected))
            {
                return FromFlag($"{key}_{expectedText}");
            }

            int actual = key switch
            {
                "publicAnxiety" => state.PublicAnxiety,
                "evidenceIntegrity" => state.EvidenceIntegrity,
                _ when key.StartsWith("trust_", StringComparison.Ordinal) =>
                    state.GetTrust(key.Substring(6)),
                _ => state.GetRuntimeCounter(key)
            };
            bool isMet = operation switch
            {
                ">=" => actual >= expected,
                "<=" => actual <= expected,
                ">" => actual > expected,
                "<" => actual < expected,
                "=" => actual == expected,
                _ => false
            };
            return new ProductionConditionResult(isMet);
        }

        private ProductionConditionResult FromFlag(string flag) =>
            new(state.HasFlag(Normalize(flag)));

        private static string Normalize(string value) =>
            value?.Trim().ToLowerInvariant() ?? string.Empty;

        private static bool LooksLikeSceneId(string value) =>
            !string.IsNullOrWhiteSpace(value) &&
            Regex.IsMatch(value, @"^(P|D\d+)-\d+$", RegexOptions.CultureInvariant);

        private static int FindTopLevelOperator(string value, string operation)
        {
            int depth = 0;
            for (int index = 0; index <= value.Length - operation.Length; index++)
            {
                if (value[index] == '(')
                {
                    depth++;
                }
                else if (value[index] == ')')
                {
                    depth = Math.Max(0, depth - 1);
                }
                else if (depth == 0 &&
                         string.CompareOrdinal(
                             value, index, operation, 0, operation.Length) == 0)
                {
                    return index;
                }
            }
            return -1;
        }
    }
}
