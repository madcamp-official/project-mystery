using UnityEngine;
using Wake.Core;
using Wake.Evidence;

namespace Wake.Puzzles
{
    /// 기존 씬을 정식 C-03/C-04/C-05 추론 서비스에 연결하는 호환 어댑터.
    /// 추론 해금은 활성 가설 슬롯을 자동으로 차지하지 않는다.
    public class ExitPuzzle : MonoBehaviour
    {
        private const string SolvedFlag = "pz_exit_solved";

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

            var service = new CanonicalDeductionService(
                state,
                evidenceId => EvidenceInventory.Instance.Contains(evidenceId));
            if (!service.TryUnlock(CanonicalDeductionCatalog.SceneDenial))
            {
                return;
            }

            state.AddFlag(SolvedFlag, "현장 출입 부정 추론");
        }
    }
}
