using UnityEngine;

namespace SpaceChico
{
    /// <summary>Efeito de paralaxe: a camada se move uma fração do movimento da câmera.</summary>
    [DefaultExecutionOrder(100)]
    public class Parallax : MonoBehaviour
    {
        public Transform cam;
        [Tooltip("0 = preso ao mundo, 1 = preso à câmera")]
        public Vector2 factor = new Vector2(0.9f, 0.95f);

        Vector3 startPos, camStart;
        bool init;

        void Start()
        {
            if (cam == null && Camera.main != null) cam = Camera.main.transform;
            if (cam == null) return;
            startPos = transform.position;
            camStart = cam.position;
            init = true;
        }

        void LateUpdate()
        {
            if (!init) return;
            Vector3 d = cam.position - camStart;
            transform.position = new Vector3(startPos.x + d.x * factor.x, startPos.y + d.y * factor.y, startPos.z);
        }
    }

    /// <summary>Flutuação senoidal suave (itens, esporos, névoa).</summary>
    public class Bob : MonoBehaviour
    {
        public Vector2 amplitude = new Vector2(0f, 0.15f);
        public float speed = 2f;
        public bool useUnscaledTime;
        Vector3 start;
        float phase;

        void Start() { start = transform.localPosition; phase = Random.value * 10f; }

        void Update()
        {
            float t = (useUnscaledTime ? Time.unscaledTime : Time.time) * speed + phase;
            transform.localPosition = start + new Vector3(Mathf.Sin(t * 0.7f) * amplitude.x, Mathf.Sin(t) * amplitude.y, 0f);
        }
    }

    /// <summary>Pulsação de brilho (alpha e escala), para halos de luz bioluminescente.</summary>
    public class GlowPulse : MonoBehaviour
    {
        public float speed = 1.5f;
        public float minAlpha = 0.35f, maxAlpha = 0.8f;
        public float scaleAmount = 0.08f;
        SpriteRenderer sr;
        Vector3 baseScale;
        float phase;

        void Start()
        {
            sr = GetComponent<SpriteRenderer>();
            baseScale = transform.localScale;
            phase = Random.value * 10f;
        }

        void Update()
        {
            float k = 0.5f + 0.5f * Mathf.Sin(Time.time * speed + phase);
            if (sr != null) { var c = sr.color; c.a = Mathf.Lerp(minAlpha, maxAlpha, k); sr.color = c; }
            transform.localScale = baseScale * (1f + scaleAmount * k);
        }
    }

    /// <summary>Névoa que desliza lentamente de um lado para o outro.</summary>
    public class DriftingFog : MonoBehaviour
    {
        public float distance = 1.5f, speed = 0.15f;
        Vector3 start;
        float phase;
        void Start() { start = transform.position; phase = Random.value * 10f; }
        void Update() => transform.position = start + Vector3.right * Mathf.Sin(Time.time * speed + phase) * distance;
    }

    /// <summary>Configura um Particle System ambiente (esporos cintilantes) que acompanha a câmera.</summary>
    [RequireComponent(typeof(ParticleSystem))]
    public class AmbientParticles : MonoBehaviour
    {
        public Color colorA = new Color(0.4f, 1f, 0.7f, 0.9f);
        public Color colorB = new Color(0.7f, 0.5f, 1f, 0.9f);
        public float rate = 14f;
        public Vector2 area = new Vector2(26f, 14f);
        public float size = 0.12f;
        public float riseSpeed = 0.35f;

        void Awake()
        {
            var ps = GetComponent<ParticleSystem>();
            ps.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);
            var main = ps.main;
            main.loop = true;
            main.duration = 5f;
            main.startLifetime = new ParticleSystem.MinMaxCurve(4f, 8f);
            main.startSpeed = 0f;
            main.startSize = new ParticleSystem.MinMaxCurve(size * 0.4f, size);
            main.startColor = new ParticleSystem.MinMaxGradient(colorA, colorB);
            main.simulationSpace = ParticleSystemSimulationSpace.World;
            main.maxParticles = 300;
            main.prewarm = true;

            var em = ps.emission;
            em.rateOverTime = rate;

            var sh = ps.shape;
            sh.shapeType = ParticleSystemShapeType.Box;
            sh.scale = new Vector3(area.x, area.y, 0.1f);

            var vel = ps.velocityOverLifetime;
            vel.enabled = true;
            vel.space = ParticleSystemSimulationSpace.World;
            vel.x = new ParticleSystem.MinMaxCurve(-0.25f, 0.25f);
            vel.y = new ParticleSystem.MinMaxCurve(riseSpeed * 0.3f, riseSpeed);
            vel.z = new ParticleSystem.MinMaxCurve(0f, 0f);

            var noise = ps.noise;
            noise.enabled = true;
            noise.strength = 0.35f;
            noise.frequency = 0.4f;

            var col = ps.colorOverLifetime;
            col.enabled = true;
            var g = new Gradient();
            g.SetKeys(new[] { new GradientColorKey(Color.white, 0f), new GradientColorKey(Color.white, 1f) },
                      new[] { new GradientAlphaKey(0f, 0f), new GradientAlphaKey(1f, 0.2f), new GradientAlphaKey(1f, 0.7f), new GradientAlphaKey(0f, 1f) });
            col.color = g;

            var r = GetComponent<ParticleSystemRenderer>();
            r.sharedMaterial = VFX.AdditiveMaterial;
            r.sortingOrder = 40;

            ps.Play();
        }
    }
}
