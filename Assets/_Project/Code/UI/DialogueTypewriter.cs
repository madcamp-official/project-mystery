using System;
using System.Collections;
using TMPro;
using UnityEngine;

namespace Wake.UI
{
    public sealed class DialogueTypewriterProgress
    {
        private float fractionalCharacters;

        public DialogueTypewriterProgress(float charactersPerSecond)
        {
            CharactersPerSecond = charactersPerSecond;
        }

        public float CharactersPerSecond
        {
            get => charactersPerSecond;
            set => charactersPerSecond = Mathf.Clamp(
                value,
                DialogueTypewriter.MinimumCharactersPerSecond,
                DialogueTypewriter.MaximumCharactersPerSecond);
        }

        public int VisibleCharacters { get; private set; }
        public int TotalCharacters { get; private set; }
        public bool IsRevealing =>
            TotalCharacters > 0 &&
            VisibleCharacters < TotalCharacters;

        private float charactersPerSecond;

        public void Begin(int totalCharacters)
        {
            TotalCharacters = Mathf.Max(0, totalCharacters);
            VisibleCharacters = 0;
            fractionalCharacters = 0f;
        }

        public int Advance(float unscaledDeltaTime)
        {
            unscaledDeltaTime = Mathf.Max(0f, unscaledDeltaTime);
            if (!IsRevealing || unscaledDeltaTime == 0f)
            {
                return VisibleCharacters;
            }

            fractionalCharacters +=
                unscaledDeltaTime * CharactersPerSecond;
            int newlyVisible = Mathf.FloorToInt(fractionalCharacters);
            if (newlyVisible <= 0)
            {
                return VisibleCharacters;
            }

            fractionalCharacters -= newlyVisible;
            VisibleCharacters = Mathf.Min(
                TotalCharacters,
                VisibleCharacters + newlyVisible);
            return VisibleCharacters;
        }

        public bool Complete()
        {
            bool wasRevealing = IsRevealing;
            VisibleCharacters = TotalCharacters;
            fractionalCharacters = 0f;
            return wasRevealing;
        }

    }

    [DisallowMultipleComponent]
    public sealed class DialogueTypewriter : MonoBehaviour
    {
        public const float DefaultCharactersPerSecond = 50f;
        public const float MinimumCharactersPerSecond = 20f;
        public const float MaximumCharactersPerSecond = 120f;

        [SerializeField]
        [Range(MinimumCharactersPerSecond, MaximumCharactersPerSecond)]
        private float charactersPerSecond = DefaultCharactersPerSecond;

        private TMP_Text target;
        private Coroutine revealRoutine;
        private DialogueTypewriterProgress progress;

        public bool IsRevealing =>
            progress != null && progress.IsRevealing;
        public int VisibleCharacters =>
            progress?.VisibleCharacters ?? 0;
        public int TotalCharacters =>
            progress?.TotalCharacters ?? 0;
        public float CharactersPerSecond
        {
            get => charactersPerSecond;
            set
            {
                charactersPerSecond = Mathf.Clamp(
                    value,
                    MinimumCharactersPerSecond,
                    MaximumCharactersPerSecond);
                if (progress != null)
                {
                    progress.CharactersPerSecond =
                        charactersPerSecond;
                }
            }
        }

        public void Initialize(
            TMP_Text targetText,
            float requestedCharactersPerSecond =
                DefaultCharactersPerSecond)
        {
            target = targetText;
            CharactersPerSecond = requestedCharactersPerSecond;
            progress = new DialogueTypewriterProgress(
                charactersPerSecond);
            CancelAndShowAll();
        }

        public void Play(string text)
        {
            EnsureInitialized();
            StopRevealRoutine();

            target.text = text ?? string.Empty;
            target.ForceMeshUpdate(
                ignoreActiveState: true,
                forceTextReparsing: true);
            progress.Begin(target.textInfo.characterCount);
            target.maxVisibleCharacters = progress.VisibleCharacters;

            if (!progress.IsRevealing)
            {
                target.maxVisibleCharacters = int.MaxValue;
                return;
            }
            if (Application.isPlaying && isActiveAndEnabled)
            {
                revealRoutine = StartCoroutine(Reveal());
            }
        }

        public bool CompleteImmediately()
        {
            if (progress == null || !progress.Complete())
            {
                return false;
            }

            StopRevealRoutine();
            if (target != null)
            {
                target.maxVisibleCharacters = int.MaxValue;
            }
            return true;
        }

        public void CancelAndShowAll()
        {
            StopRevealRoutine();
            progress?.Complete();
            if (target != null)
            {
                target.maxVisibleCharacters = int.MaxValue;
            }
        }

        private IEnumerator Reveal()
        {
            while (progress.IsRevealing)
            {
                progress.Advance(Time.unscaledDeltaTime);
                target.maxVisibleCharacters =
                    progress.VisibleCharacters;
                yield return null;
            }

            target.maxVisibleCharacters = int.MaxValue;
            revealRoutine = null;
        }

        private void EnsureInitialized()
        {
            if (target == null)
            {
                target = GetComponent<TMP_Text>();
            }
            if (target == null)
            {
                throw new InvalidOperationException(
                    "DialogueTypewriter requires a TMP_Text target.");
            }
            if (progress == null)
            {
                progress = new DialogueTypewriterProgress(
                    charactersPerSecond);
            }
        }

        private void StopRevealRoutine()
        {
            if (revealRoutine == null)
            {
                return;
            }
            StopCoroutine(revealRoutine);
            revealRoutine = null;
        }

        private void OnDisable()
        {
            CancelAndShowAll();
        }

    }
}
