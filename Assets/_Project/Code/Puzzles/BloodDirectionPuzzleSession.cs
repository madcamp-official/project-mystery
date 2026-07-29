using System;
using System.Collections.Generic;
using UnityEngine;
using Wake.Core;

namespace Wake.Puzzles
{
    public enum BloodDirectionStage
    {
        Reconstruct = 0,
        CompareBody = 1,
        ChooseConclusion = 2,
        Complete = 3
    }

    /// <summary>
    /// Pure state and validation for "피의 방향". UI coordinates are normalized
    /// so the puzzle remains deterministic at every supported resolution.
    /// </summary>
    public sealed class BloodDirectionPuzzleSession
    {
        public const int PieceCount = 9;
        public const float MinimumMarkerDistance = 0.18f;
        private const int GridSize = 3;

        private static readonly int[] InitialOrder = { 7, 2, 5, 1, 8, 0, 6, 3, 4 };
        private static readonly int[] InitialRotations = { 1, 3, 2, 1, 2, 3, 0, 2, 1 };

        private readonly int[] pieces = new int[PieceCount];
        private readonly int[] rotations = new int[PieceCount];

        public BloodDirectionPuzzleSession()
        {
            Array.Copy(InitialOrder, pieces, PieceCount);
            Array.Copy(InitialRotations, rotations, PieceCount);
        }

        public BloodDirectionStage Stage { get; private set; }
        public IReadOnlyList<int> Pieces => pieces;
        public IReadOnlyList<int> Rotations => rotations;
        public int SelectedPosture { get; private set; } = -1;
        public int IncorrectPostureAttempts { get; private set; }
        public Vector2? WoundMarker { get; private set; }
        public Vector2? PoolMarker { get; private set; }
        public bool ShouldEmphasizeTails => IncorrectPostureAttempts >= 3;
        public bool IsReconstructionCorrect =>
            TryGetSolvedGlobalRotation(out _);

        public void Swap(int firstSlot, int secondSlot)
        {
            if (Stage != BloodDirectionStage.Reconstruct ||
                firstSlot < 0 || firstSlot >= PieceCount ||
                secondSlot < 0 || secondSlot >= PieceCount)
            {
                return;
            }

            (pieces[firstSlot], pieces[secondSlot]) =
                (pieces[secondSlot], pieces[firstSlot]);
            (rotations[firstSlot], rotations[secondSlot]) =
                (rotations[secondSlot], rotations[firstSlot]);
            TryAdvanceFromReconstruction();
        }

        public void Rotate(int slot)
        {
            if (Stage != BloodDirectionStage.Reconstruct ||
                slot < 0 || slot >= PieceCount)
            {
                return;
            }

            rotations[slot] = (rotations[slot] + 1) % 4;
            TryAdvanceFromReconstruction();
        }

        public bool SelectPosture(int posture)
        {
            if (Stage != BloodDirectionStage.CompareBody || posture < 0 || posture > 2)
            {
                return false;
            }

            SelectedPosture = posture;
            IncorrectPostureAttempts++;
            return true;
        }

        public bool PlaceMarker(bool wound, Vector2 normalizedPosition)
        {
            if (Stage != BloodDirectionStage.CompareBody)
            {
                return false;
            }

            normalizedPosition = new Vector2(
                Mathf.Clamp01(normalizedPosition.x),
                Mathf.Clamp01(normalizedPosition.y));
            if (wound)
            {
                WoundMarker = normalizedPosition;
            }
            else
            {
                PoolMarker = normalizedPosition;
            }

            if (WoundMarker.HasValue && PoolMarker.HasValue &&
                Vector2.Distance(WoundMarker.Value, PoolMarker.Value) >=
                MinimumMarkerDistance)
            {
                Stage = BloodDirectionStage.ChooseConclusion;
            }
            return true;
        }

        public bool ChooseConclusion(int index)
        {
            if (Stage != BloodDirectionStage.ChooseConclusion || index != 2)
            {
                return false;
            }

            Stage = BloodDirectionStage.Complete;
            return true;
        }

        /// <summary>Test/accessibility affordance; gameplay still uses drag and rotate.</summary>
        public void SetSolvedReconstruction()
        {
            for (int index = 0; index < PieceCount; index++)
            {
                pieces[index] = index;
                rotations[index] = 0;
            }
            TryAdvanceFromReconstruction();
        }

        private void TryAdvanceFromReconstruction()
        {
            if (!TryGetSolvedGlobalRotation(out _))
            {
                return;
            }

            NormalizeReconstruction();
            Stage = BloodDirectionStage.CompareBody;
        }

        private bool TryGetSolvedGlobalRotation(out int quarterTurns)
        {
            for (int rotation = 0; rotation < 4; rotation++)
            {
                bool matches = true;
                for (int source = 0; source < PieceCount; source++)
                {
                    int destination = RotateSlot(source, rotation);
                    if (pieces[destination] != source ||
                        rotations[destination] != rotation)
                    {
                        matches = false;
                        break;
                    }
                }

                if (matches)
                {
                    quarterTurns = rotation;
                    return true;
                }
            }

            quarterTurns = -1;
            return false;
        }

        private static int RotateSlot(int source, int quarterTurns)
        {
            int row = source / GridSize;
            int column = source % GridSize;
            return quarterTurns switch
            {
                0 => source,
                1 => column * GridSize + (GridSize - 1 - row),
                2 => (GridSize - 1 - row) * GridSize +
                     (GridSize - 1 - column),
                _ => (GridSize - 1 - column) * GridSize + row
            };
        }

        private void NormalizeReconstruction()
        {
            for (int index = 0; index < PieceCount; index++)
            {
                pieces[index] = index;
                rotations[index] = 0;
            }
        }
    }
}
