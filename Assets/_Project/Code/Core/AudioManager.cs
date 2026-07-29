using System.Collections;
using UnityEngine;

namespace Wake.Core
{
    public class AudioManager : MonoBehaviour
    {
        private const string MusicVolumePreference = "audio.music.volume";
        private const string SfxVolumePreference = "audio.sfx.volume";
        private const float DefaultMusicVolume = 0.5f;
        private const float DefaultSfxVolume = 0.5f;
        private const float DefaultCrossfadeSeconds = 1.8f;

        public static AudioManager Instance { get; private set; }

        [SerializeField] private AudioSource musicSource;
        [SerializeField] private AudioSource sfxSource;
        [SerializeField, Min(0f)] private float defaultCrossfadeSeconds =
            DefaultCrossfadeSeconds;

        [Header("Music")]
        [SerializeField] private AudioClip startMenuTheme;
        [SerializeField] private string[] locationCodesWithThemes;
        [SerializeField] private AudioClip[] locationThemeClips;

        [Header("SFX")]
        [SerializeField] private AudioClip evidencePickupSfx;
        [SerializeField] private AudioClip badEndSfx;
        [SerializeField] private AudioClip buttonClickSfx;

        private GameStateManager state;
        private AudioSource musicSourceB;
        private AudioSource activeMusicSource;
        private AudioSource ambienceSourceA;
        private AudioSource ambienceSourceB;
        private Coroutine musicFade;
        private Coroutine ambienceFadeA;
        private Coroutine ambienceFadeB;
        private float currentMusicMix = 1f;
        private float currentAmbienceMixA;
        private float currentAmbienceMixB;

        public float MusicVolume { get; private set; } = DefaultMusicVolume;
        public float SfxVolume { get; private set; } = DefaultSfxVolume;

        private void OnDestroy()
        {
            // With "Enter Play Mode Options > Reload Domain" disabled,
            // static fields survive across Play sessions - without this,
            // Instance keeps pointing at this destroyed object into the
            // next session, and the first call through it (before the new
            // AudioManager's Awake reassigns Instance) throws
            // MissingReferenceException.
            if (Instance == this)
            {
                Instance = null;
            }
        }

        private void Awake()
        {
            Instance = this;
            MusicVolume = Mathf.Clamp01(PlayerPrefs.GetFloat(
                MusicVolumePreference,
                DefaultMusicVolume));
            SfxVolume = Mathf.Clamp01(PlayerPrefs.GetFloat(
                SfxVolumePreference,
                DefaultSfxVolume));
            EnsureRuntimeSources();
            ApplyVolumes();
        }

        private void Start()
        {
            PlayTitleTheme(false);
            BindState();
        }

        private void OnDisable()
        {
            UnbindState();
        }

        private void BindState()
        {
            state = GameStateManager.Instance;
            if (state != null)
            {
                state.BadEndTriggered += OnBadEnd;
            }
        }

        private void UnbindState()
        {
            if (state != null)
            {
                state.BadEndTriggered -= OnBadEnd;
                state = null;
            }
        }

        private void OnBadEnd(string message)
        {
            PlaySfx(badEndSfx);
        }

        public void PlayLocationTheme(string locationCode)
        {
            if (AudioCueCatalog.TryGetLocationCue(
                    locationCode,
                    out LocationAudioCue cue))
            {
                AudioClip clip = LoadClip(cue.MusicKey);
                if (clip != null)
                {
                    CrossfadeMusic(
                        clip,
                        cue.CrossfadeSeconds,
                        cue.MusicVolume);
                }
                else
                {
                    PlayLegacyLocationTheme(locationCode);
                }

                FadeAmbience(
                    0,
                    cue.PrimaryAmbienceKey,
                    cue.PrimaryAmbienceVolume,
                    cue.CrossfadeSeconds);
                FadeAmbience(
                    1,
                    cue.SecondaryAmbienceKey,
                    cue.SecondaryAmbienceVolume,
                    cue.CrossfadeSeconds);
                return;
            }

            PlayLegacyLocationTheme(locationCode);
        }

        private void PlayLegacyLocationTheme(string locationCode)
        {
            if (locationCodesWithThemes == null)
            {
                return;
            }

            for (int i = 0; i < locationCodesWithThemes.Length; i++)
            {
                if (string.Equals(
                        locationCodesWithThemes[i],
                        locationCode,
                        System.StringComparison.OrdinalIgnoreCase) &&
                    locationThemeClips != null &&
                    i < locationThemeClips.Length)
                {
                    PlayMusic(locationThemeClips[i]);
                    return;
                }
            }
        }

        public void PlayEvidencePickup()
        {
            PlaySfx(evidencePickupSfx);
        }

        public void PlayButtonClick()
        {
            PlaySfx(buttonClickSfx);
        }

        public void PlayMetalFootstepReplacement()
        {
            PlaySfx(LoadClip(AudioCueCatalog.MetalFootstepReplacementKey));
        }

        public void PlayIronDoorKnock()
        {
            PlaySfx(LoadClip(AudioCueCatalog.IronDoorKnockKey));
        }

        public void PlayIronDoorToggle()
        {
            PlaySfx(LoadClip(AudioCueCatalog.IronDoorToggleKey));
        }

        public void PlayTitleTheme(bool fade = true)
        {
            StopAmbience(fade ? defaultCrossfadeSeconds : 0f);
            CrossfadeMusic(
                startMenuTheme,
                fade ? defaultCrossfadeSeconds : 0f,
                1f);
        }

        public void PlayMusic(AudioClip clip)
        {
            CrossfadeMusic(clip, defaultCrossfadeSeconds, 1f);
        }

        public void CrossfadeMusic(
            AudioClip clip,
            float duration,
            float mixVolume = 1f)
        {
            if (clip == null || musicSource == null)
            {
                return;
            }

            EnsureRuntimeSources();
            currentMusicMix = Mathf.Clamp01(mixVolume);
            AudioSource sameClipSource = FindPlayingMusicSource(clip);
            if (musicFade != null)
            {
                StopCoroutine(musicFade);
            }

            if (sameClipSource != null)
            {
                activeMusicSource = sameClipSource;
                AudioSource other = OtherMusicSource(sameClipSource);
                musicFade = StartCoroutine(FadeMusicSources(
                    sameClipSource,
                    other,
                    duration,
                    false));
                return;
            }

            AudioSource outgoing = activeMusicSource;
            AudioSource incoming = OtherMusicSource(outgoing);
            incoming.Stop();
            incoming.clip = clip;
            incoming.loop = true;
            incoming.volume = 0f;
            incoming.Play();
            activeMusicSource = incoming;
            musicFade = StartCoroutine(FadeMusicSources(
                incoming,
                outgoing,
                duration,
                true));
        }

        public void PlaySfx(AudioClip clip)
        {
            if (clip == null || sfxSource == null)
            {
                return;
            }

            sfxSource.PlayOneShot(clip);
        }

        public void SetMusicVolume(float value)
        {
            MusicVolume = Mathf.Clamp01(value);
            if (activeMusicSource != null)
                activeMusicSource.volume = MusicVolume * currentMusicMix;
            PlayerPrefs.SetFloat(MusicVolumePreference, MusicVolume);
        }

        public void SetSfxVolume(float value)
        {
            SfxVolume = Mathf.Clamp01(value);
            if (sfxSource != null)
                sfxSource.volume = SfxVolume;
            if (ambienceSourceA != null)
                ambienceSourceA.volume =
                    SfxVolume * currentAmbienceMixA;
            if (ambienceSourceB != null)
                ambienceSourceB.volume =
                    SfxVolume * currentAmbienceMixB;
            PlayerPrefs.SetFloat(SfxVolumePreference, SfxVolume);
        }

        private void ApplyVolumes()
        {
            if (activeMusicSource != null)
                activeMusicSource.volume = MusicVolume * currentMusicMix;
            if (sfxSource != null)
                sfxSource.volume = SfxVolume;
        }

        private void EnsureRuntimeSources()
        {
            if (musicSource == null)
            {
                musicSource = CreateSource("Music A", true);
            }

            if (musicSourceB == null)
            {
                musicSourceB = CreateSource("Music B", true, musicSource);
            }

            if (ambienceSourceA == null)
            {
                ambienceSourceA = CreateSource("Ambience A", true, musicSource);
            }

            if (ambienceSourceB == null)
            {
                ambienceSourceB = CreateSource("Ambience B", true, musicSource);
            }

            activeMusicSource ??= musicSource;
        }

        private AudioSource CreateSource(
            string sourceName,
            bool loop,
            AudioSource template = null)
        {
            GameObject sourceObject = new(sourceName, typeof(AudioSource));
            sourceObject.transform.SetParent(transform, false);
            AudioSource source = sourceObject.GetComponent<AudioSource>();
            source.playOnAwake = false;
            source.loop = loop;
            source.spatialBlend = 0f;
            if (template != null)
            {
                source.outputAudioMixerGroup = template.outputAudioMixerGroup;
                source.priority = template.priority;
            }
            return source;
        }

        private AudioSource FindPlayingMusicSource(AudioClip clip)
        {
            if (musicSource != null &&
                musicSource.clip == clip &&
                musicSource.isPlaying)
            {
                return musicSource;
            }
            if (musicSourceB != null &&
                musicSourceB.clip == clip &&
                musicSourceB.isPlaying)
            {
                return musicSourceB;
            }
            return null;
        }

        private AudioSource OtherMusicSource(AudioSource source) =>
            source == musicSource ? musicSourceB : musicSource;

        private IEnumerator FadeMusicSources(
            AudioSource incoming,
            AudioSource outgoing,
            float duration,
            bool fadeIncomingFromZero)
        {
            float safeDuration = Mathf.Max(0f, duration);
            float elapsed = 0f;
            float incomingStart = fadeIncomingFromZero ? 0f : incoming.volume;
            float outgoingStart =
                outgoing != null && outgoing != incoming
                    ? outgoing.volume
                    : 0f;

            while (elapsed < safeDuration)
            {
                elapsed += Time.unscaledDeltaTime;
                float progress = Mathf.Clamp01(elapsed / safeDuration);
                incoming.volume = Mathf.Lerp(
                    incomingStart,
                    MusicVolume * currentMusicMix,
                    progress);
                if (outgoing != null && outgoing != incoming)
                {
                    outgoing.volume =
                        Mathf.Lerp(outgoingStart, 0f, progress);
                }
                yield return null;
            }

            incoming.volume = MusicVolume * currentMusicMix;
            if (outgoing != null && outgoing != incoming)
            {
                outgoing.Stop();
                outgoing.clip = null;
                outgoing.volume = 0f;
            }
            musicFade = null;
        }

        private void FadeAmbience(
            int layer,
            string resourceKey,
            float mixVolume,
            float duration)
        {
            EnsureRuntimeSources();
            AudioSource source =
                layer == 0 ? ambienceSourceA : ambienceSourceB;
            if (layer == 0)
                currentAmbienceMixA = Mathf.Clamp01(mixVolume);
            else
                currentAmbienceMixB = Mathf.Clamp01(mixVolume);
            Coroutine running =
                layer == 0 ? ambienceFadeA : ambienceFadeB;
            if (running != null)
            {
                StopCoroutine(running);
            }

            AudioClip clip = LoadClip(resourceKey);
            Coroutine next = StartCoroutine(FadeAmbienceSource(
                source,
                clip,
                Mathf.Clamp01(mixVolume),
                duration));
            if (layer == 0)
                ambienceFadeA = next;
            else
                ambienceFadeB = next;
        }

        private IEnumerator FadeAmbienceSource(
            AudioSource source,
            AudioClip clip,
            float mixVolume,
            float duration)
        {
            float safeDuration = Mathf.Max(0f, duration);
            bool sameClip =
                clip != null && source.clip == clip && source.isPlaying;
            if (sameClip)
            {
                float start = source.volume;
                float elapsed = 0f;
                while (elapsed < safeDuration)
                {
                    elapsed += Time.unscaledDeltaTime;
                    source.volume = Mathf.Lerp(
                        start,
                        SfxVolume * mixVolume,
                        Mathf.Clamp01(elapsed / safeDuration));
                    yield return null;
                }
                source.volume = SfxVolume * mixVolume;
                yield break;
            }

            float halfDuration = safeDuration * .5f;
            float outgoingStart = source.volume;
            float elapsedOut = 0f;
            while (source.isPlaying && elapsedOut < halfDuration)
            {
                elapsedOut += Time.unscaledDeltaTime;
                source.volume = Mathf.Lerp(
                    outgoingStart,
                    0f,
                    Mathf.Clamp01(elapsedOut / halfDuration));
                yield return null;
            }

            source.Stop();
            source.clip = clip;
            source.volume = 0f;
            if (clip == null)
            {
                yield break;
            }

            source.loop = true;
            source.Play();
            float elapsedIn = 0f;
            while (elapsedIn < halfDuration)
            {
                elapsedIn += Time.unscaledDeltaTime;
                source.volume = Mathf.Lerp(
                    0f,
                    SfxVolume * mixVolume,
                    Mathf.Clamp01(elapsedIn / halfDuration));
                yield return null;
            }
            source.volume = SfxVolume * mixVolume;
        }

        private void StopAmbience(float duration)
        {
            FadeAmbience(0, string.Empty, 0f, duration);
            FadeAmbience(1, string.Empty, 0f, duration);
        }

        private static AudioClip LoadClip(string resourceKey)
        {
            return string.IsNullOrWhiteSpace(resourceKey)
                ? null
                : Resources.Load<AudioClip>(resourceKey);
        }
    }
}
