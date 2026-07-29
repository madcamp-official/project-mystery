using System.Collections;
using UnityEngine;
using UnityEngine.UI;
using Wake.Core;
using Wake.Exploration;
using Wake.Narrative;

namespace Wake.UI
{
    [DisallowMultipleComponent]
    public sealed class BodyDiscoveryPresenter : MonoBehaviour
    {
        public const string SceneId = "D1-06";
        public const string SeenFlag =
            "cinematic.d1_06_body_discovery_seen";

        private static readonly string[] FrameKeys =
        {
            "BodyDiscovery/discovery1",
            "BodyDiscovery/discovery2",
            "BodyDiscovery/discovery3",
            "BodyDiscovery/discovery4"
        };

        private CanvasGroup rootGroup;
        private RawImage frameImage;
        private RectTransform frameRect;
        private AudioSource stingerSource;
        private Texture2D[] frames;
        private Coroutine sequence;
        private bool inputSuppressed;

        public bool IsPlaying => sequence != null;
        public int LoadedFrameCount
        {
            get
            {
                int count = 0;
                foreach (Texture2D frame in frames ?? new Texture2D[0])
                {
                    if (frame != null)
                        count++;
                }
                return count;
            }
        }
        public bool HasStingerClip => stingerSource?.clip != null;

        private void OnEnable()
        {
            InvestigationEventHub.Published += HandleInvestigationEvent;
        }

        private void OnDisable()
        {
            InvestigationEventHub.Published -= HandleInvestigationEvent;
            InterruptSequence();
        }

        private void HandleInvestigationEvent(
            InvestigationEvent investigationEvent)
        {
            if (investigationEvent.Kind !=
                    InvestigationEventKind.SceneEntered ||
                !string.Equals(
                    investigationEvent.SubjectId,
                    SceneId,
                    System.StringComparison.OrdinalIgnoreCase) ||
                IsPlaying ||
                GameStateManager.Instance?.HasFlag(SeenFlag) == true)
            {
                return;
            }

            EnsureBuilt();
            if (LoadedFrameCount != FrameKeys.Length)
            {
                Debug.LogError(
                    "Body discovery cinematic is missing one or more frames.");
                return;
            }

            sequence = StartCoroutine(PlaySequence());
        }

        private void EnsureBuilt()
        {
            if (rootGroup != null)
                return;

            Transform canvasRoot = GameObject.Find("Canvas")?.transform;
            if (canvasRoot == null)
            {
                Debug.LogError(
                    "BodyDiscoveryPresenter requires an active Canvas root.");
                return;
            }

            GameObject root = new(
                "Body Discovery Cinematic",
                typeof(RectTransform),
                typeof(Canvas),
                typeof(CanvasGroup),
                typeof(GraphicRaycaster),
                typeof(Image));
            root.transform.SetParent(canvasRoot, false);
            RectTransform rootRect = root.GetComponent<RectTransform>();
            Stretch(rootRect);
            Canvas overlay = root.GetComponent<Canvas>();
            overlay.overrideSorting = true;
            overlay.sortingOrder = 175;
            Image black = root.GetComponent<Image>();
            black.color = Color.black;
            black.raycastTarget = true;
            rootGroup = root.GetComponent<CanvasGroup>();
            rootGroup.alpha = 0f;
            rootGroup.interactable = true;
            rootGroup.blocksRaycasts = true;

            GameObject imageObject = new(
                "Discovery Frame",
                typeof(RectTransform),
                typeof(CanvasRenderer),
                typeof(RawImage),
                typeof(AspectRatioFitter));
            imageObject.transform.SetParent(root.transform, false);
            frameRect = imageObject.GetComponent<RectTransform>();
            Stretch(frameRect);
            frameImage = imageObject.GetComponent<RawImage>();
            frameImage.raycastTarget = false;
            AspectRatioFitter fitter =
                imageObject.GetComponent<AspectRatioFitter>();
            fitter.aspectMode = AspectRatioFitter.AspectMode.EnvelopeParent;
            fitter.aspectRatio = 16f / 9f;

            frames = new Texture2D[FrameKeys.Length];
            for (int index = 0; index < FrameKeys.Length; index++)
                frames[index] = Resources.Load<Texture2D>(FrameKeys[index]);

            stingerSource = gameObject.AddComponent<AudioSource>();
            stingerSource.playOnAwake = false;
            stingerSource.loop = false;
            stingerSource.spatialBlend = 0f;
            stingerSource.clip =
                Resources.Load<AudioClip>("SoundEffect/Suspicious");
            if (stingerSource.clip == null)
            {
                Debug.LogError(
                    "Body discovery cinematic is missing Suspicious.mp3.");
            }
            root.SetActive(false);
        }

        private IEnumerator PlaySequence()
        {
            SetInputSuppression(true);
            rootGroup.gameObject.SetActive(true);
            rootGroup.gameObject.transform.SetAsLastSibling();
            rootGroup.alpha = 1f;
            frameImage.color = new Color(1f, 1f, 1f, 0f);
            if (stingerSource?.clip != null)
            {
                stingerSource.volume =
                    (AudioManager.Instance?.SfxVolume ?? 1f) * .88f;
                stingerSource.Play();
            }

            float[] holds = { .62f, .72f, .68f, 1.05f };
            for (int index = 0; index < frames.Length; index++)
            {
                yield return PresentFrame(frames[index], holds[index]);
                if (index < frames.Length - 1)
                    yield return FadeFrame(1f, 0f, .16f);
            }

            float elapsed = 0f;
            const float exitDuration = .5f;
            float audioStart =
                stingerSource != null ? stingerSource.volume : 0f;
            while (elapsed < exitDuration)
            {
                elapsed += Time.unscaledDeltaTime;
                float progress = Mathf.Clamp01(elapsed / exitDuration);
                rootGroup.alpha = 1f - progress;
                if (stingerSource != null)
                {
                    stingerSource.volume =
                        Mathf.Lerp(audioStart, 0f, progress);
                }
                yield return null;
            }

            CompleteSequence();
        }

        private IEnumerator PresentFrame(Texture2D texture, float hold)
        {
            frameImage.texture = texture;
            if (texture != null && texture.height > 0)
            {
                frameImage.GetComponent<AspectRatioFitter>().aspectRatio =
                    (float)texture.width / texture.height;
            }
            bool reducedMotion = ReducedMotionSettings.Enabled;
            frameRect.localScale =
                reducedMotion ? Vector3.one : Vector3.one * 1.035f;
            yield return FadeFrame(0f, 1f, reducedMotion ? .25f : .22f);

            float elapsed = 0f;
            while (elapsed < hold)
            {
                elapsed += Time.unscaledDeltaTime;
                if (!reducedMotion)
                {
                    float progress = Mathf.Clamp01(elapsed / hold);
                    frameRect.localScale = Vector3.one *
                        Mathf.Lerp(1.035f, 1f, progress);
                }
                yield return null;
            }
            frameRect.localScale = Vector3.one;
        }

        private IEnumerator FadeFrame(float from, float to, float duration)
        {
            float elapsed = 0f;
            while (elapsed < duration)
            {
                elapsed += Time.unscaledDeltaTime;
                float alpha = Mathf.Lerp(
                    from,
                    to,
                    Mathf.Clamp01(elapsed / duration));
                frameImage.color = new Color(1f, 1f, 1f, alpha);
                yield return null;
            }
            frameImage.color = new Color(1f, 1f, 1f, to);
        }

        private void CompleteSequence()
        {
            stingerSource?.Stop();
            rootGroup.alpha = 0f;
            rootGroup.gameObject.SetActive(false);
            frameImage.texture = null;
            sequence = null;
            GameStateManager.Instance?.AddFlagSilently(SeenFlag);
            SetInputSuppression(false);
        }

        private void InterruptSequence()
        {
            if (sequence != null)
            {
                StopCoroutine(sequence);
                sequence = null;
            }
            stingerSource?.Stop();
            if (rootGroup != null)
            {
                rootGroup.alpha = 0f;
                rootGroup.gameObject.SetActive(false);
            }
            if (inputSuppressed)
                SetInputSuppression(false);
        }

        private void SetInputSuppression(bool suppressed)
        {
            inputSuppressed = suppressed;
            UIManager.Instance?.SetCinematicOverlayActive(suppressed);
            DialogueController.Instance?.SetInputSuppressed(suppressed);
            LocationLoader.Instance?.SetWorldInteractionSuppressed(suppressed);
        }

        private static void Stretch(RectTransform rect)
        {
            rect.anchorMin = Vector2.zero;
            rect.anchorMax = Vector2.one;
            rect.offsetMin = Vector2.zero;
            rect.offsetMax = Vector2.zero;
        }
    }
}
