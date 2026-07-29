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
        private AudioSource travelFootstepSource;
        private AudioSource waterSplashSource;
        private AudioLowPassFilter musicLowPassA;
        private AudioLowPassFilter musicLowPassB;
        private Coroutine musicFade;
        private Coroutine ambienceFadeA;
        private Coroutine ambienceFadeB;
        private Coroutine travelFade;
        private Coroutine travelFootstepStop;
        private Coroutine waterSplashStop;
        private float currentMusicMix = 1f;
        private float currentAmbienceMixA;
        private float currentAmbienceMixB;
        private float currentTravelFootstepMix;
        private string pendingTravelLocationCode = string.Empty;
        private float pendingTravelFadeInSeconds;

        public float MusicVolume { get; private set; } = DefaultMusicVolume;
        public float SfxVolume { get; private set; } = DefaultSfxVolume;

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

        private void OnDestroy()
        {
            UnbindState();
            if (Instance == this)
            {
                Instance = null;
            }
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
                float transitionSeconds = cue.CrossfadeSeconds;
                if (string.Equals(
                        pendingTravelLocationCode,
                        locationCode?.Trim(),
                        System.StringComparison.OrdinalIgnoreCase))
                {
                    transitionSeconds = pendingTravelFadeInSeconds;
                    pendingTravelLocationCode = string.Empty;
                    pendingTravelFadeInSeconds = 0f;
                }
                AudioClip clip = LoadClip(cue.MusicKey);
                if (clip != null)
                {
                    CrossfadeMusic(
                        clip,
                        transitionSeconds,
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
                    transitionSeconds);
                FadeAmbience(
                    1,
                    cue.SecondaryAmbienceKey,
                    cue.SecondaryAmbienceVolume,
                    transitionSeconds);
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

        public void PlayWaterSloshing()
        {
            PlaySfx(LoadClip(AudioCueCatalog.WaterSloshingKey));
        }

        public void SetUnderwaterMuffle(float depth01)
        {
            EnsureRuntimeSources();
            float cutoff = Mathf.Lerp(
                AudioCueCatalog.UnderwaterClearCutoffHz,
                AudioCueCatalog.UnderwaterMuffledCutoffHz,
                Mathf.Clamp01(depth01));
            if (musicLowPassA != null)
            {
                musicLowPassA.cutoffFrequency = cutoff;
            }
            if (musicLowPassB != null)
            {
                musicLowPassB.cutoffFrequency = cutoff;
            }
        }

        public void PlayWaterSplashOut()
        {
            EnsureRuntimeSources();
            AudioClip clip = LoadClip(AudioCueCatalog.WaterSplashOutKey);
            if (clip == null || waterSplashSource == null)
            {
                return;
            }

            waterSplashSource.Stop();
            waterSplashSource.clip = clip;
            waterSplashSource.time = 0f;
            waterSplashSource.Play();
            if (waterSplashStop != null)
            {
                StopCoroutine(waterSplashStop);
            }
            waterSplashStop = StartCoroutine(
                StopWaterSplashAfter(AudioCueCatalog.WaterSplashOutSeconds));
        }

        public void BeginMapTravelAudio(
            string destinationLocationCode,
            float fadeOutSeconds,
            float fadeInSeconds)
        {
            EnsureRuntimeSources();
            StopTransitionFades();
            pendingTravelLocationCode =
                destinationLocationCode?.Trim() ?? string.Empty;
            pendingTravelFadeInSeconds = Mathf.Max(0f, fadeInSeconds);
            travelFade = StartCoroutine(
                FadeCurrentAudioToSilence(fadeOutSeconds));
            PlayTravelFootsteps(destinationLocationCode);
        }

        public void CompleteMapTravelFadeOut()
        {
            if (travelFade != null)
            {
                StopCoroutine(travelFade);
                travelFade = null;
            }
            SilenceTravelMix();
        }

        public void ResumeCurrentLocationIfTravelPending()
        {
            if (string.IsNullOrEmpty(pendingTravelLocationCode))
                return;

            string current =
                GameStateManager.Instance?.CurrentLocationCode;
            if (string.IsNullOrWhiteSpace(current))
            {
                pendingTravelLocationCode = string.Empty;
                pendingTravelFadeInSeconds = 0f;
                return;
            }

            pendingTravelLocationCode = current;
            PlayLocationTheme(current);
        }

        public void EndMapTravelAudio()
        {
            pendingTravelLocationCode = string.Empty;
            pendingTravelFadeInSeconds = 0f;
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
            if (travelFootstepSource != null)
                travelFootstepSource.volume =
                    SfxVolume * currentTravelFootstepMix;
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

            if (travelFootstepSource == null)
            {
                travelFootstepSource =
                    CreateSource("Travel Footsteps", true, sfxSource);
            }

            if (waterSplashSource == null)
            {
                waterSplashSource =
                    CreateSource("Water Splash", false, sfxSource);
            }

            musicLowPassA ??= EnsureLowPass(musicSource);
            musicLowPassB ??= EnsureLowPass(musicSourceB);

            activeMusicSource ??= musicSource;
        }

        private static AudioLowPassFilter EnsureLowPass(AudioSource source)
        {
            AudioLowPassFilter filter =
                source.GetComponent<AudioLowPassFilter>();
            if (filter == null)
            {
                filter = source.gameObject.AddComponent<AudioLowPassFilter>();
                filter.cutoffFrequency =
                    AudioCueCatalog.UnderwaterClearCutoffHz;
            }
            return filter;
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

        private void PlayTravelFootsteps(string locationCode)
        {
            FootstepSurface surface =
                AudioCueCatalog.FootstepSurfaceFor(locationCode);
            AudioClip clip = LoadClip(
                AudioCueCatalog.FootstepKeyFor(surface));
            if (clip == null || travelFootstepSource == null)
                return;

            currentTravelFootstepMix = surface switch
            {
                FootstepSurface.Metal => .62f,
                FootstepSurface.Wood => .72f,
                FootstepSurface.Stone => .68f,
                _ => .7f
            };
            travelFootstepSource.Stop();
            travelFootstepSource.clip = clip;
            travelFootstepSource.loop = true;
            travelFootstepSource.pitch =
                AudioCueCatalog.FootstepPitchFor(surface);
            travelFootstepSource.volume =
                SfxVolume * currentTravelFootstepMix;
            travelFootstepSource.time = Mathf.Clamp(
                AudioCueCatalog.FootstepStartOffsetFor(surface),
                0f,
                Mathf.Max(0f, clip.length - 0.05f));
            travelFootstepSource.Play();
            if (travelFootstepStop != null)
            {
                StopCoroutine(travelFootstepStop);
            }
            travelFootstepStop = StartCoroutine(
                StopTravelFootstepsAfter(
                    AudioCueCatalog.MapTravelFootstepSeconds));
        }

        private IEnumerator StopTravelFootstepsAfter(float duration)
        {
            float elapsed = 0f;
            float safeDuration = Mathf.Max(0f, duration);
            while (elapsed < safeDuration)
            {
                elapsed += Time.unscaledDeltaTime;
                yield return null;
            }
            if (travelFootstepSource != null)
            {
                travelFootstepSource.Stop();
                travelFootstepSource.clip = null;
            }
            travelFootstepStop = null;
        }

        private IEnumerator StopWaterSplashAfter(float duration)
        {
            float elapsed = 0f;
            float safeDuration = Mathf.Max(0f, duration);
            while (elapsed < safeDuration)
            {
                elapsed += Time.unscaledDeltaTime;
                yield return null;
            }
            if (waterSplashSource != null)
            {
                waterSplashSource.Stop();
                waterSplashSource.clip = null;
            }
            waterSplashStop = null;
        }

        private void StopTransitionFades()
        {
            if (musicFade != null)
            {
                StopCoroutine(musicFade);
                musicFade = null;
            }
            if (ambienceFadeA != null)
            {
                StopCoroutine(ambienceFadeA);
                ambienceFadeA = null;
            }
            if (ambienceFadeB != null)
            {
                StopCoroutine(ambienceFadeB);
                ambienceFadeB = null;
            }
            if (travelFade != null)
            {
                StopCoroutine(travelFade);
                travelFade = null;
            }
        }

        private IEnumerator FadeCurrentAudioToSilence(float duration)
        {
            float safeDuration = Mathf.Max(0f, duration);
            float elapsed = 0f;
            float musicAStart = musicSource != null ? musicSource.volume : 0f;
            float musicBStart = musicSourceB != null ? musicSourceB.volume : 0f;
            float ambienceAStart =
                ambienceSourceA != null ? ambienceSourceA.volume : 0f;
            float ambienceBStart =
                ambienceSourceB != null ? ambienceSourceB.volume : 0f;
            while (elapsed < safeDuration)
            {
                elapsed += Time.unscaledDeltaTime;
                float progress = Mathf.SmoothStep(
                    0f,
                    1f,
                    Mathf.Clamp01(elapsed / safeDuration));
                if (musicSource != null)
                    musicSource.volume =
                        Mathf.Lerp(musicAStart, 0f, progress);
                if (musicSourceB != null)
                    musicSourceB.volume =
                        Mathf.Lerp(musicBStart, 0f, progress);
                if (ambienceSourceA != null)
                    ambienceSourceA.volume =
                        Mathf.Lerp(ambienceAStart, 0f, progress);
                if (ambienceSourceB != null)
                    ambienceSourceB.volume =
                        Mathf.Lerp(ambienceBStart, 0f, progress);
                yield return null;
            }
            SilenceTravelMix();
            travelFade = null;
        }

        private void SilenceTravelMix()
        {
            if (musicSource != null)
                musicSource.volume = 0f;
            if (musicSourceB != null)
                musicSourceB.volume = 0f;
            StopAndClear(ambienceSourceA);
            StopAndClear(ambienceSourceB);
        }

        private static void StopAndClear(AudioSource source)
        {
            if (source == null)
                return;
            source.Stop();
            source.clip = null;
            source.volume = 0f;
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

            bool hasOutgoing = source.isPlaying;
            float outgoingDuration =
                hasOutgoing ? safeDuration * .5f : 0f;
            float incomingDuration =
                hasOutgoing ? safeDuration * .5f : safeDuration;
            float outgoingStart = source.volume;
            float elapsedOut = 0f;
            while (source.isPlaying && elapsedOut < outgoingDuration)
            {
                elapsedOut += Time.unscaledDeltaTime;
                source.volume = Mathf.Lerp(
                    outgoingStart,
                    0f,
                    Mathf.Clamp01(elapsedOut / outgoingDuration));
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
            while (elapsedIn < incomingDuration)
            {
                elapsedIn += Time.unscaledDeltaTime;
                source.volume = Mathf.Lerp(
                    0f,
                    SfxVolume * mixVolume,
                    Mathf.Clamp01(elapsedIn / incomingDuration));
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
