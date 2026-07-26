using System;
using System.Collections.Generic;
using System.Linq;
using Wake.Core;
using Wake.Narrative;

namespace Wake.Puzzles
{
    public enum MarcusAnswer
    {
        No,
        Yes
    }

    public enum MarcusQuestionResult
    {
        Recorded,
        AlreadyAsked,
        UnknownQuestion,
        LimitReached,
        SessionCompleted
    }

    public enum MarcusAuthenticationResult
    {
        Unresolved,
        EvelynAuthenticationDenied,
        EvelynAuthenticationConfirmed
    }

    public sealed class MarcusQuestionDefinition
    {
        public MarcusQuestionDefinition(string id, string prompt,
            bool confirmsEvelynAuthentication = false)
        {
            Id = Normalize(id);
            Prompt = prompt?.Trim() ?? string.Empty;
            ConfirmsEvelynAuthentication = confirmsEvelynAuthentication;
        }

        public string Id { get; }
        public string Prompt { get; }
        public bool ConfirmsEvelynAuthentication { get; }

        public static string Normalize(string value) =>
            string.IsNullOrWhiteSpace(value)
                ? string.Empty
                : value.Trim().ToLowerInvariant().Replace(' ', '_');
    }

    public static class MarcusInterrogationCatalog
    {
        public const string SessionId = "marcus_interrogation";
        public const string SceneId = "D4-04";
        public const string AuthenticationQuestion = "evelyn_authentication";
        public const string AuthenticationEvidence = "C-15";
        public const string AuthenticationFlag = "evelyn_authentication_confirmed";

        private static readonly MarcusQuestionDefinition[] Questions =
        {
            new(
                AuthenticationQuestion,
                "Evelyn에게 금고 접근 인증을 제공했습니까?",
                true),
            new(
                "direct_request",
                "인증 수단을 요구한 사람은 Evelyn이었습니까?"),
            new(
                "cargo_access",
                "Daniel이 사라진 밤 화물 설비 접근을 승인했습니까?"),
            new(
                "confession_intent",
                "계단 추락 전에 인증 제공 사실을 고백하려 했습니까?"),
            new(
                "richard_order",
                "Richard의 지시로 인증 수단을 제공했습니까?")
        };

        private static readonly IReadOnlyDictionary<string, MarcusQuestionDefinition>
            ById = Questions.ToDictionary(item => item.Id, StringComparer.Ordinal);

        public static IReadOnlyList<MarcusQuestionDefinition> All => Questions;

        public static bool TryGet(
            string questionId,
            out MarcusQuestionDefinition definition) =>
            ById.TryGetValue(
                MarcusQuestionDefinition.Normalize(questionId),
                out definition);
    }

    public readonly struct MarcusAnswerRecord
    {
        public MarcusAnswerRecord(
            string questionId, MarcusAnswer answer)
        {
            QuestionId = MarcusQuestionDefinition.Normalize(questionId);
            Answer = answer;
        }

        public string QuestionId { get; }
        public MarcusAnswer Answer { get; }
    }
    public readonly struct MarcusInterrogationCompletion
    {
        public MarcusInterrogationCompletion(
            bool completed,
            MarcusAuthenticationResult authentication,
            string message)
        {
            Completed = completed;
            Authentication = authentication;
            Message = message ?? string.Empty;
        }

        public bool Completed { get; }
        public MarcusAuthenticationResult Authentication { get; }
        public string Message { get; }
    }

    public sealed class MarcusInterrogationSession
    {
        public const int MaximumQuestions = 5;

        private readonly GameStateManager state;
        private readonly Func<string, bool> tryGrantEvidence;
        private readonly IReadOnlyDictionary<string, MarcusQuestionDefinition> questions;
        private readonly List<MarcusAnswerRecord> answers = new();

        public MarcusInterrogationSession(GameStateManager state,
            IEnumerable<MarcusQuestionDefinition> definitions = null,
            Func<string, bool> tryGrantEvidence = null)
        {
            this.state = state ?? throw new ArgumentNullException(nameof(state));
            this.tryGrantEvidence = tryGrantEvidence ?? (_ => false);
            questions = (definitions ?? MarcusInterrogationCatalog.All)
                .Where(item => item != null && !string.IsNullOrEmpty(item.Id))
                .GroupBy(item => item.Id, StringComparer.Ordinal)
                .ToDictionary(group => group.Key, group => group.First(), StringComparer.Ordinal);

            if (state.TryGetPuzzleSession(
                    MarcusInterrogationCatalog.SessionId,
                    out PuzzleSessionState saved))
            {
                Restore(saved);
            }
        }

        public IReadOnlyList<MarcusAnswerRecord> Answers => answers;
        public int RemainingQuestions => Math.Max(0, MaximumQuestions - answers.Count);
        public bool IsCompleted { get; private set; }

        public MarcusQuestionResult Ask(string questionId, MarcusAnswer answer)
        {
            if (IsCompleted)
            {
                return MarcusQuestionResult.SessionCompleted;
            }

            string normalized = MarcusQuestionDefinition.Normalize(questionId);
            if (!questions.ContainsKey(normalized))
            {
                return MarcusQuestionResult.UnknownQuestion;
            }

            if (answers.Any(item => item.QuestionId == normalized))
            {
                return MarcusQuestionResult.AlreadyAsked;
            }

            if (answers.Count >= MaximumQuestions)
            {
                return MarcusQuestionResult.LimitReached;
            }

            answers.Add(new MarcusAnswerRecord(normalized, answer));
            Save();
            return MarcusQuestionResult.Recorded;
        }

        public MarcusInterrogationCompletion Complete()
        {
            if (IsCompleted)
            {
                return new MarcusInterrogationCompletion(
                    false,
                    ResolveAuthentication(),
                    "이미 종료된 심문입니다.");
            }

            MarcusAuthenticationResult authentication = ResolveAuthentication();
            if (authentication == MarcusAuthenticationResult.Unresolved)
            {
                return new MarcusInterrogationCompletion(
                    false,
                    authentication,
                    "Evelyn 인증 제공 여부를 먼저 확인하세요.");
            }

            if (!ProductionSceneCompletionGate.CanStartInteraction(
                    state,
                    MarcusInterrogationCatalog.SceneId,
                    MarcusInterrogationCatalog.SessionId))
            {
                return new MarcusInterrogationCompletion(
                    false,
                    authentication,
                    "이미 완료된 심문은 다시 종료할 수 없습니다.");
            }

            using IDisposable batch = state.BeginStateBatch();
            if (authentication ==
                    MarcusAuthenticationResult.EvelynAuthenticationConfirmed &&
                !tryGrantEvidence(MarcusInterrogationCatalog.AuthenticationEvidence))
            {
                return new MarcusInterrogationCompletion(
                    false,
                    authentication,
                    "증거 인벤토리에 C-15를 등록하지 못했습니다.");
            }

            IsCompleted = true;
            Save();
            if (authentication ==
                MarcusAuthenticationResult.EvelynAuthenticationConfirmed)
            {
                state.AddFlag(
                    MarcusInterrogationCatalog.AuthenticationFlag,
                    "Evelyn 인증 제공 확인");
            }

            if (ProductionSceneCompletionGate.TryComplete(
                    state,
                    MarcusInterrogationCatalog.SceneId,
                    MarcusInterrogationCatalog.SessionId,
                    out bool newlyCompleted,
                    out _) &&
                newlyCompleted)
            {
                InvestigationEventHub.Publish(
                    InvestigationEventKind.TheoryCompleted,
                    "vault_accomplice_connection",
                    MarcusInterrogationCatalog.SceneId);
            }
            return new MarcusInterrogationCompletion(
                true,
                authentication,
                authentication ==
                MarcusAuthenticationResult.EvelynAuthenticationConfirmed
                    ? "Marcus가 Evelyn에게 인증을 제공한 사실을 확인했습니다."
                    : "Marcus는 Evelyn에게 인증을 제공하지 않았다고 답했습니다.");
        }

        public MarcusAuthenticationResult ResolveAuthentication()
        {
            MarcusAnswerRecord? record = answers
                .Where(answer =>
                    questions.TryGetValue(answer.QuestionId, out var definition) &&
                    definition.ConfirmsEvelynAuthentication)
                .Cast<MarcusAnswerRecord?>()
                .FirstOrDefault();
            if (!record.HasValue)
            {
                return MarcusAuthenticationResult.Unresolved;
            }

            return record.Value.Answer == MarcusAnswer.Yes
                ? MarcusAuthenticationResult.EvelynAuthenticationConfirmed
                : MarcusAuthenticationResult.EvelynAuthenticationDenied;
        }

        private void Restore(PuzzleSessionState saved)
        {
            foreach (string encoded in saved.selectedIds ?? new List<string>())
            {
                if (!TryDecode(encoded, out MarcusAnswerRecord record) ||
                    !questions.ContainsKey(record.QuestionId) ||
                    answers.Any(item => item.QuestionId == record.QuestionId) ||
                    answers.Count >= MaximumQuestions)
                {
                    continue;
                }

                answers.Add(record);
            }

            IsCompleted = saved.completed;
        }

        private void Save()
        {
            state.SavePuzzleSession(new PuzzleSessionState
            {
                puzzleId = MarcusInterrogationCatalog.SessionId,
                selectedIds = answers.Select(Encode).ToList(),
                step = answers.Count,
                completed = IsCompleted
            });
        }

        private static string Encode(MarcusAnswerRecord record) =>
            $"{record.QuestionId}={record.Answer.ToString().ToLowerInvariant()}";

        private static bool TryDecode(
            string encoded,
            out MarcusAnswerRecord record)
        {
            record = default;
            string[] parts = (encoded ?? string.Empty).Split('=');
            if (parts.Length != 2)
            {
                return false;
            }

            MarcusAnswer answer;
            if (string.Equals(parts[1], "yes", StringComparison.OrdinalIgnoreCase))
            {
                answer = MarcusAnswer.Yes;
            }
            else if (string.Equals(parts[1], "no", StringComparison.OrdinalIgnoreCase))
            {
                answer = MarcusAnswer.No;
            }
            else
            {
                return false;
            }

            record = new MarcusAnswerRecord(parts[0], answer);
            return !string.IsNullOrEmpty(record.QuestionId);
        }
    }

    public static class MarcusInterrogationValidator
    {
        public static IReadOnlyList<string> Validate(
            IEnumerable<MarcusQuestionDefinition> definitions)
        {
            var warnings = new List<string>();
            MarcusQuestionDefinition[] items =
                (definitions ?? Array.Empty<MarcusQuestionDefinition>()).ToArray();
            if (items.Length == 0)
            {
                warnings.Add("심문 질문이 정의되지 않았습니다.");
                return warnings;
            }

            foreach (MarcusQuestionDefinition item in items)
            {
                if (item == null || string.IsNullOrEmpty(item.Id))
                {
                    warnings.Add("ID가 비어 있는 심문 질문이 있습니다.");
                }
                else if (string.IsNullOrEmpty(item.Prompt))
                {
                    warnings.Add($"질문 문구가 비어 있습니다: {item.Id}");
                }
            }

            foreach (IGrouping<string, MarcusQuestionDefinition> duplicate in items
                         .Where(item => item != null && !string.IsNullOrEmpty(item.Id))
                         .GroupBy(item => item.Id, StringComparer.Ordinal)
                         .Where(group => group.Count() > 1))
            {
                warnings.Add($"중복 심문 질문 ID: {duplicate.Key}");
            }

            int authenticationCount = items.Count(item =>
                item != null && item.ConfirmsEvelynAuthentication);
            if (authenticationCount != 1)
            {
                warnings.Add(
                    $"Evelyn 인증 판정 질문은 정확히 1개여야 합니다: {authenticationCount}개");
            }

            if (items.Length > MarcusInterrogationSession.MaximumQuestions)
            {
                warnings.Add(
                    $"심문 질문은 최대 {MarcusInterrogationSession.MaximumQuestions}개입니다.");
            }

            return warnings;
        }
    }
}
