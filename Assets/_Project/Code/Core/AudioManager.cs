using UnityEngine;

namespace Wake.Core
{
    public class AudioManager : MonoBehaviour
    {
        private const string MusicVolumePreference = "audio.music.volume";
        private const string SfxVolumePreference = "audio.sfx.volume";
        private const float DefaultMusicVolume = 0.5f;
        private const float DefaultSfxVolume = 0.5f;

        public static AudioManager Instance { get; private set; }

        [SerializeField] private AudioSource musicSource;
        [SerializeField] private AudioSource sfxSource;

        [Header("Music")]
        [SerializeField] private AudioClip startMenuTheme;
        [SerializeField] private string[] locationCodesWithThemes;
        [SerializeField] private AudioClip[] locationThemeClips;

        [Header("SFX")]
        [SerializeField] private AudioClip evidencePickupSfx;
        [SerializeField] private AudioClip badEndSfx;

        private GameStateManager state;

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
            ApplyVolumes();
        }

        private void Start()
        {
            PlayMusic(startMenuTheme);
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
            if (locationCodesWithThemes == null)
            {
                return;
            }

            for (int i = 0; i < locationCodesWithThemes.Length; i++)
            {
                if (locationCodesWithThemes[i] == locationCode)
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

        public void PlayMusic(AudioClip clip)
        {
            if (clip == null || musicSource == null)
            {
                return;
            }

            if (musicSource.clip == clip && musicSource.isPlaying)
            {
                return;
            }

            musicSource.clip = clip;
            musicSource.loop = true;
            musicSource.Play();
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
            if (musicSource != null)
                musicSource.volume = MusicVolume;
            PlayerPrefs.SetFloat(MusicVolumePreference, MusicVolume);
        }

        public void SetSfxVolume(float value)
        {
            SfxVolume = Mathf.Clamp01(value);
            if (sfxSource != null)
                sfxSource.volume = SfxVolume;
            PlayerPrefs.SetFloat(SfxVolumePreference, SfxVolume);
        }

        private void ApplyVolumes()
        {
            if (musicSource != null)
                musicSource.volume = MusicVolume;
            if (sfxSource != null)
                sfxSource.volume = SfxVolume;
        }
    }
}
