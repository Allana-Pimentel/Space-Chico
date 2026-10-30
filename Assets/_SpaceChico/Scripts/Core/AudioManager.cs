using System.Collections.Generic;
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

    /// <summary>
    /// Gera todos os efeitos sonoros e a música ambiente proceduralmente
    /// (síntese simples de ondas), então o projeto não depende de arquivos de
    /// áudio. Para usar sons próprios, preencha a lista "overrides" no Inspector.
    /// </summary>
    public class AudioManager : MonoBehaviour
    {
        [System.Serializable]
        public struct SfxOverride { public Sfx sfx; public AudioClip clip; }

        public List<SfxOverride> overrides = new List<SfxOverride>();
        public AudioClip musicOverride;
        [Range(0, 1)] public float sfxVolume = 0.8f;
        [Range(0, 1)] public float musicVolume = 0.35f;

        static AudioManager instance;
        AudioSource sfxSource;
        AudioSource musicSource;
        readonly Dictionary<Sfx, AudioClip> clips = new Dictionary<Sfx, AudioClip>();

        const int SR = 44100;

        public static AudioManager Instance
        {
            get
            {
                if (instance == null)
                {
                    instance = FindAnyObjectByType<AudioManager>();
                    if (instance == null) instance = new GameObject("AudioManager").AddComponent<AudioManager>();
                }
                return instance;
            }
        }

        void Awake()
        {
            if (instance != null && instance != this) { Destroy(gameObject); return; }
            instance = this;
            DontDestroyOnLoad(gameObject);

            sfxSource = gameObject.AddComponent<AudioSource>();
            sfxSource.playOnAwake = false;
            sfxSource.ignoreListenerPause = true;

            musicSource = gameObject.AddComponent<AudioSource>();
            musicSource.loop = true;
            musicSource.playOnAwake = false;
            musicSource.ignoreListenerPause = true;

            BuildClips();
            foreach (var o in overrides) if (o.clip != null) clips[o.sfx] = o.clip;

            musicSource.clip = musicOverride != null ? musicOverride : BuildAmbientMusic();
            musicSource.volume = musicVolume;
            musicSource.Play();
        }

        public static void Play(Sfx s, float volume = 1f, float pitchVariation = 0.04f)
        {
            var am = Instance;
            if (am == null || am.sfxSource == null) return;
            if (!am.clips.TryGetValue(s, out var clip) || clip == null) return;
            am.sfxSource.pitch = 1f + Random.Range(-pitchVariation, pitchVariation);
            am.sfxSource.PlayOneShot(clip, volume * am.sfxVolume);
        }

        // ================================================================
        //  Síntese
        // ================================================================
        enum Wave { Sine, Square, Triangle, Saw, Noise }

        static float Osc(Wave w, float phase)
        {
            float p = phase - Mathf.Floor(phase);
            switch (w)
            {
                case Wave.Square: return p < 0.5f ? 1f : -1f;
                case Wave.Triangle: return 4f * Mathf.Abs(p - 0.5f) - 1f;
                case Wave.Saw: return 2f * p - 1f;
                case Wave.Noise: return Random.value * 2f - 1f;
                default: return Mathf.Sin(p * Mathf.PI * 2f);
            }
        }

        /// <summary>Tom com varredura de frequência (exponencial) e envelope ataque/decaimento.</summary>
        static float[] Sweep(Wave w, float f0, float f1, float dur, float vol, float attack = 0.005f,
                             float vibratoHz = 0f, float vibratoDepth = 0f, float noise = 0f)
        {
            int n = Mathf.CeilToInt(dur * SR);
            var data = new float[n];
            float phase = 0f;
            float lp = 0f;
            for (int i = 0; i < n; i++)
            {
                float t = i / (float)SR;
                float k = t / dur;
                float f = f0 * Mathf.Pow(f1 / f0, k);
                if (vibratoHz > 0) f *= 1f + vibratoDepth * Mathf.Sin(t * vibratoHz * Mathf.PI * 2f);
                phase += f / SR;
                float s = Osc(w, phase);
                if (noise > 0)
                {
                    lp += ((Random.value * 2f - 1f) - lp) * 0.35f;
                    s = Mathf.Lerp(s, lp * 2f, noise);
                }
                float env = Mathf.Min(1f, t / attack) * Mathf.Pow(1f - k, 1.6f);
                data[i] = s * env * vol;
            }
            return data;
        }

        static float[] Arp(Wave w, float[] notes, float noteDur, float vol, float tail = 0.25f)
        {
            int per = Mathf.CeilToInt(noteDur * SR);
            int tailN = Mathf.CeilToInt(tail * SR);
            var data = new float[per * notes.Length + tailN];
            for (int ni = 0; ni < notes.Length; ni++)
            {
                bool last = ni == notes.Length - 1;
                int len = last ? per + tailN : per * 2;
                float phase = 0f;
                for (int i = 0; i < len && ni * per + i < data.Length; i++)
                {
                    float t = i / (float)SR;
                    phase += notes[ni] / SR;
                    float env = Mathf.Min(1f, t / 0.004f) * Mathf.Exp(-t * (last ? 5f : 14f));
                    data[ni * per + i] += Osc(w, phase) * env * vol;
                }
            }
            return data;
        }

        static float[] Mix(params float[][] parts)
        {
            int n = 0;
            foreach (var p in parts) n = Mathf.Max(n, p.Length);
            var d = new float[n];
            foreach (var p in parts) for (int i = 0; i < p.Length; i++) d[i] += p[i];
            return d;
        }

        static AudioClip Clip(string name, float[] data)
        {
            var c = AudioClip.Create(name, data.Length, 1, SR, false);
            c.SetData(data, 0);
            return c;
        }

        void BuildClips()
        {
            clips[Sfx.Jump] = Clip("jump", Sweep(Wave.Square, 280, 720, 0.16f, 0.16f));
            clips[Sfx.Land] = Clip("land", Sweep(Wave.Noise, 200, 80, 0.07f, 0.12f, noise: 1f));
            clips[Sfx.Shoot] = Clip("shoot", Mix(Sweep(Wave.Saw, 1500, 260, 0.14f, 0.16f), Sweep(Wave.Sine, 2400, 900, 0.08f, 0.1f)));
            clips[Sfx.Empty] = Clip("empty", Sweep(Wave.Square, 160, 120, 0.07f, 0.12f));
            clips[Sfx.Stomp] = Clip("stomp", Mix(Sweep(Wave.Sine, 220, 55, 0.22f, 0.5f), Sweep(Wave.Noise, 300, 100, 0.12f, 0.25f, noise: 0.9f)));
            clips[Sfx.Hurt] = Clip("hurt", Sweep(Wave.Square, 420, 110, 0.32f, 0.18f, vibratoHz: 28f, vibratoDepth: 0.15f));
            clips[Sfx.EnemyHit] = Clip("enemyhit", Sweep(Wave.Square, 640, 380, 0.07f, 0.14f));
            clips[Sfx.EnemyDie] = Clip("enemydie", Mix(Sweep(Wave.Sine, 340, 45, 0.38f, 0.4f), Sweep(Wave.Noise, 500, 100, 0.25f, 0.2f, noise: 1f)));
            clips[Sfx.Crystal] = Clip("crystal", Arp(Wave.Sine, new[] { 1318.5f, 1760f, 2637f }, 0.05f, 0.22f, 0.3f));
            clips[Sfx.Pickup] = Clip("pickup", Arp(Wave.Triangle, new[] { 660f, 990f }, 0.06f, 0.3f));
            clips[Sfx.Oxygen] = Clip("oxygen", Mix(Arp(Wave.Triangle, new[] { 523f, 784f, 1046f }, 0.06f, 0.28f),
                                                    Sweep(Wave.Sine, 300, 900, 0.3f, 0.12f, vibratoHz: 18f, vibratoDepth: 0.2f)));
            clips[Sfx.PowerUp] = Clip("powerup", Arp(Wave.Square, new[] { 523f, 659f, 784f, 1046f, 1318f }, 0.055f, 0.12f));
            clips[Sfx.PowerActivate] = Clip("poweract", Sweep(Wave.Saw, 180, 1400, 0.45f, 0.14f, attack: 0.05f, vibratoHz: 12f, vibratoDepth: 0.08f));
            clips[Sfx.ShipPart] = Clip("shippart", Mix(Arp(Wave.Square, new[] { 392f, 523f, 659f, 784f, 1046f }, 0.09f, 0.11f, 0.8f),
                                                        Arp(Wave.Sine, new[] { 784f, 1046f, 1318f, 1568f, 2093f }, 0.09f, 0.12f, 0.8f)));
            clips[Sfx.UiMove] = Clip("uimove", Sweep(Wave.Sine, 900, 880, 0.045f, 0.2f));
            clips[Sfx.UiSelect] = Clip("uiselect", Arp(Wave.Triangle, new[] { 880f, 1320f }, 0.05f, 0.3f, 0.12f));
            clips[Sfx.UiOpen] = Clip("uiopen", Sweep(Wave.Triangle, 480, 960, 0.1f, 0.3f));
            clips[Sfx.UiClose] = Clip("uiclose", Sweep(Wave.Triangle, 960, 480, 0.1f, 0.3f));
            clips[Sfx.LevelComplete] = Clip("levelcomplete", Mix(
                Arp(Wave.Square, new[] { 523f, 659f, 784f, 1046f, 784f, 1046f, 1318f }, 0.12f, 0.1f, 1.2f),
                Arp(Wave.Sine, new[] { 261f, 329f, 392f, 523f, 392f, 523f, 659f }, 0.12f, 0.2f, 1.2f)));
            clips[Sfx.GameOver] = Clip("gameover", Arp(Wave.Triangle, new[] { 392f, 330f, 262f, 196f }, 0.22f, 0.35f, 1f));
            clips[Sfx.Alert] = Clip("alert", Sweep(Wave.Square, 900, 1300, 0.09f, 0.1f));
            clips[Sfx.BossRoar] = Clip("bossroar", Mix(
                Sweep(Wave.Saw, 160, 45, 0.75f, 0.35f, vibratoHz: 35f, vibratoDepth: 0.3f),
                Sweep(Wave.Noise, 400, 60, 0.65f, 0.25f, noise: 0.9f)
            ));
            clips[Sfx.BossSlam] = Clip("bossslam", Mix(
                Sweep(Wave.Sine, 140, 30, 0.45f, 0.6f),
                Sweep(Wave.Noise, 600, 40, 0.35f, 0.45f, noise: 1f)
            ));
            clips[Sfx.BossSpit] = Clip("bossspit", Mix(
                Sweep(Wave.Saw, 520, 160, 0.18f, 0.25f, vibratoHz: 22f, vibratoDepth: 0.2f),
                Sweep(Wave.Noise, 800, 200, 0.12f, 0.18f, noise: 0.7f)
            ));
        }

        /// <summary>Pad ambiente místico com gotas d'água ("plinks") e um drone grave: clima do pântano.</summary>
        static AudioClip BuildAmbientMusic()
        {
            const int sr = 22050;
            const float chordDur = 4f;
            float[][] chords =
            {
                new[] { 220.00f, 261.63f, 329.63f },  // Am
                new[] { 174.61f, 220.00f, 261.63f },  // F
                new[] { 130.81f, 196.00f, 261.63f },  // C
                new[] { 196.00f, 246.94f, 293.66f },  // G
            };
            int n = (int)(sr * chordDur * chords.Length);
            var d = new float[n];
            var rng = new System.Random(7);

            for (int c = 0; c < chords.Length; c++)
            {
                int start = (int)(c * chordDur * sr);
                int len = (int)(chordDur * sr * 1.25f);
                foreach (var f in chords[c])
                {
                    for (int v = 0; v < 2; v++)
                    {
                        float det = v == 0 ? 0.997f : 1.003f;
                        for (int i = 0; i < len; i++)
                        {
                            int idx = (start + i) % n;
                            float t = i / (float)sr;
                            float env = Mathf.Clamp01(t / 1.2f) * Mathf.Clamp01((chordDur * 1.25f - t) / 1.5f);
                            d[idx] += Mathf.Sin(2f * Mathf.PI * f * det * t) * env * 0.045f;
                            d[idx] += Mathf.Sin(2f * Mathf.PI * f * 2f * det * t) * env * 0.012f;
                        }
                    }
                }
            }
            // drone grave com LFO
            for (int i = 0; i < n; i++)
            {
                float t = i / (float)sr;
                float lfo = 0.6f + 0.4f * Mathf.Sin(2f * Mathf.PI * t / 8f);
                d[i] += Mathf.Sin(2f * Mathf.PI * 55f * t) * 0.05f * lfo;
            }
            // gotas d'água
            for (int k = 0; k < 22; k++)
            {
                int start = rng.Next(0, n - sr);
                float f = 900f + (float)rng.NextDouble() * 1400f;
                for (int i = 0; i < sr / 3; i++)
                {
                    float t = i / (float)sr;
                    float ff = f * (1f + 0.6f * Mathf.Exp(-t * 40f));
                    d[start + i] += Mathf.Sin(2f * Mathf.PI * ff * t) * Mathf.Exp(-t * 18f) * 0.06f;
                }
            }
            var clip = AudioClip.Create("ambient_swamp", n, 1, sr, false);
            clip.SetData(d, 0);
            return clip;
        }
    }
}
