using UnityEngine;

namespace Wake.Core
{
    public class AudioManager : MonoBehaviour
    {
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

        private void Awake()
        {
            Instance = this;
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
    }
}
