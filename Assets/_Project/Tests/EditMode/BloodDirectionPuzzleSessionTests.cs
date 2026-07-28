using NUnit.Framework;
using UnityEngine;
using Wake.Puzzles;

namespace Wake.Tests.EditMode
{
    public sealed class BloodDirectionPuzzleSessionTests
    {
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
    }
}
