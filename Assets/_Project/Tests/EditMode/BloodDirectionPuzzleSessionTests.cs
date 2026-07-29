using System.Linq;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;
using Wake.Puzzles;
using Wake.UI;

namespace Wake.Tests.EditMode
{
    public sealed class BloodDirectionPuzzleSessionTests
    {
        [Test]
        public void PieceView_RegistersCompleteDragEventContract()
        {
            var target = new GameObject(
                "Blood Piece",
                typeof(RectTransform),
                typeof(CanvasRenderer),
                typeof(Image));
            try
            {
                BloodPuzzlePieceView view =
                    target.AddComponent<BloodPuzzlePieceView>();

                Assert.That(view, Is.InstanceOf<IDragHandler>());
                Assert.That(
                    ExecuteEvents.GetEventHandler<IDragHandler>(target),
                    Is.SameAs(target));
            }
            finally
            {
                Object.DestroyImmediate(target);
            }
        }

        [Test]
        public void AnalysisTool_RegistersCompleteDragEventContract()
        {
            var target = new GameObject(
                "Analysis Tool",
                typeof(RectTransform),
                typeof(CanvasRenderer),
                typeof(Image));
            try
            {
                BloodAnalysisToolDrag view =
                    target.AddComponent<BloodAnalysisToolDrag>();

                Assert.That(view, Is.InstanceOf<IBeginDragHandler>());
                Assert.That(view, Is.InstanceOf<IDragHandler>());
                Assert.That(view, Is.InstanceOf<IEndDragHandler>());
                Assert.That(
                    ExecuteEvents.GetEventHandler<IDragHandler>(target),
                    Is.SameAs(target));
            }
            finally
            {
                Object.DestroyImmediate(target);
            }
        }

        [Test]
        public void Reconstruction_RequiresCorrectPieceAndRotation()
        {
            var session = new BloodDirectionPuzzleSession();

            Assert.That(session.IsReconstructionCorrect, Is.False);
            Assert.That(session.Stage, Is.EqualTo(BloodDirectionStage.Reconstruct));

            session.SetSolvedReconstruction();

            Assert.That(session.IsReconstructionCorrect, Is.True);
            Assert.That(session.Stage, Is.EqualTo(BloodDirectionStage.CompareBody));
        }

        [TestCase(0)]
        [TestCase(1)]
        [TestCase(2)]
        [TestCase(3)]
        public void Reconstruction_AcceptsEveryGlobalQuarterTurnAndNormalizes(
            int quarterTurns)
        {
            var session = new BloodDirectionPuzzleSession();

            ArrangeAsGlobalRotation(session, quarterTurns);

            Assert.That(
                session.Stage,
                Is.EqualTo(BloodDirectionStage.CompareBody));
            Assert.That(
                session.Pieces,
                Is.EqualTo(Enumerable.Range(
                    0,
                    BloodDirectionPuzzleSession.PieceCount)));
            Assert.That(
                session.Rotations,
                Is.All.EqualTo(0));
        }

        [Test]
        public void Markers_UnlockConclusionOnlyWhenSeparated()
        {
            var session = new BloodDirectionPuzzleSession();
            session.SetSolvedReconstruction();

            session.PlaceMarker(true, new Vector2(0.45f, 0.5f));
            session.PlaceMarker(false, new Vector2(0.5f, 0.52f));
            Assert.That(session.Stage, Is.EqualTo(BloodDirectionStage.CompareBody));

            session.PlaceMarker(false, new Vector2(0.75f, 0.75f));
            Assert.That(session.Stage, Is.EqualTo(BloodDirectionStage.ChooseConclusion));
        }

        [Test]
        public void ThirdPostureAttempt_EnablesDirectionTailHint()
        {
            var session = new BloodDirectionPuzzleSession();
            session.SetSolvedReconstruction();

            session.SelectPosture(0);
            session.SelectPosture(1);
            Assert.That(session.ShouldEmphasizeTails, Is.False);
            session.SelectPosture(2);

            Assert.That(session.ShouldEmphasizeTails, Is.True);
        }

        [Test]
        public void OnlyVerticalDropConclusion_CompletesPuzzle()
        {
            var session = new BloodDirectionPuzzleSession();
            session.SetSolvedReconstruction();
            session.PlaceMarker(true, new Vector2(0.2f, 0.2f));
            session.PlaceMarker(false, new Vector2(0.8f, 0.8f));

            Assert.That(session.ChooseConclusion(0), Is.False);
            Assert.That(session.Stage, Is.EqualTo(BloodDirectionStage.ChooseConclusion));
            Assert.That(session.ChooseConclusion(2), Is.True);
            Assert.That(session.Stage, Is.EqualTo(BloodDirectionStage.Complete));
        }

        private static void ArrangeAsGlobalRotation(
            BloodDirectionPuzzleSession session,
            int quarterTurns)
        {
            const int gridSize = 3;
            var targetPieces =
                new int[BloodDirectionPuzzleSession.PieceCount];
            for (int source = 0;
                 source < BloodDirectionPuzzleSession.PieceCount;
                 source++)
            {
                int row = source / gridSize;
                int column = source % gridSize;
                int destination = quarterTurns switch
                {
                    0 => source,
                    1 => column * gridSize + (gridSize - 1 - row),
                    2 => (gridSize - 1 - row) * gridSize +
                         (gridSize - 1 - column),
                    _ => (gridSize - 1 - column) * gridSize + row
                };
                targetPieces[destination] = source;
            }

            for (int slot = 0; slot < targetPieces.Length; slot++)
            {
                int desiredSource = targetPieces[slot];
                int currentSlot = session.Pieces
                    .Select((source, index) => (source, index))
                    .Single(item => item.source == desiredSource)
                    .index;
                session.Swap(slot, currentSlot);
            }

            for (int slot = 0; slot < targetPieces.Length; slot++)
            {
                int turns = (quarterTurns - session.Rotations[slot] + 4) % 4;
                for (int turn = 0; turn < turns; turn++)
                {
                    session.Rotate(slot);
                }
            }
        }
    }
}
