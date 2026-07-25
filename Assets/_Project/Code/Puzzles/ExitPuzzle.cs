using UnityEngine;
using Wake.Core;
using Wake.Evidence;

namespace Wake.Puzzles
{
    /// PZ-EXIT: 발판(C-03) + 덕트(C-04) + 점검구(C-05)가 모두 모이면
    /// "세 출구 모두 사용되지 않았다" 결론을 내리고 그 이론을 활성화한다.
    public class ExitPuzzle : MonoBehaviour
    {
        private const string TheoryId = "출구 미사용";
        private const string SolvedFlag = "pz_exit_solved";

        private static readonly string[] RequiredEvidenceIds = { "C-03", "C-04", "C-05" };

        private void Start()
        {
            // Start() runs after every Awake/OnEnable in the scene, so
            // EvidenceInventory.Instance is guaranteed to exist here
            // (subscribing from OnEnable can race EvidenceInventory's own Awake).
            if (EvidenceInventory.Instance != null)
            {
                EvidenceInventory.Instance.EvidenceAdded += OnEvidenceAdded;
                TryEvaluate();
            }
        }

        private void OnDisable()
        {
            if (EvidenceInventory.Instance != null)
            {
                EvidenceInventory.Instance.EvidenceAdded -= OnEvidenceAdded;
            }
        }

        private void OnEvidenceAdded(EvidenceDefinition evidence)
        {
            TryEvaluate();
        }

        private void TryEvaluate()
        {
            GameStateManager state = GameStateManager.Instance;
            if (state == null || state.HasFlag(SolvedFlag))
            {
                return;
            }

            if (!HasAllRequiredEvidence())
            {
                return;
            }

            state.AddFlag(SolvedFlag, "출구 미사용 결론");
            state.ActivateTheory(TheoryId);
        }

        private bool HasAllRequiredEvidence()
        {
            var collected = EvidenceInventory.Instance.Collected;
            foreach (string requiredId in RequiredEvidenceIds)
            {
                bool found = false;
                foreach (EvidenceDefinition evidence in collected)
                {
                    if (evidence != null && evidence.EvidenceId == requiredId)
                    {
                        found = true;
                        break;
                    }
                }

                if (!found)
                {
                    return false;
                }
            }

            return true;
        }
    }
}
