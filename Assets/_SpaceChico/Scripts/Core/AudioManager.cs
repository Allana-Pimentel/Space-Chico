using UnityEngine;

namespace SpaceChico
{
    public enum Sfx
    {
        Jump, Land, Shoot, Empty, Stomp, Hurt, EnemyHit, EnemyDie,
        Crystal, Pickup, Oxygen, PowerUp, PowerActivate, ShipPart,
        UiMove, UiSelect, UiOpen, UiClose, LevelComplete, GameOver, Alert,
        BossRoar, BossSlam, BossSpit
    }

    public class AudioManager : MonoBehaviour
    {
        public static AudioManager Instance;

        [Range(0f, 1f)] public float sfxVolume = 0.8f;
        [Range(0f, 1f)] public float musicVolume = 0.35f;

        private AudioSource sfxSource;
        private AudioSource musicSource;

        void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Destroy(gameObject);
                return;
            }
            Instance = this;
            DontDestroyOnLoad(gameObject);

            sfxSource = gameObject.AddComponent<AudioSource>();
            sfxSource.playOnAwake = false;

            musicSource = gameObject.AddComponent<AudioSource>();
            musicSource.loop = true;
            musicSource.volume = musicVolume;

            AudioClip musica = Resources.Load<AudioClip>("Audio/AmbientMusic");
            if (musica != null)
            {
                musicSource.clip = musica;
                musicSource.Play();
            }
        }

        public static void Play(Sfx som, float volume = 1f)
        {
            if (Instance == null || Instance.sfxSource == null) return;

            AudioClip clip = Resources.Load<AudioClip>("Audio/" + som.ToString());
            if (clip != null)
            {
                Instance.sfxSource.PlayOneShot(clip, volume * Instance.sfxVolume);
            }
        }
    }
}
