using UnityEngine;

namespace SpaceChico
{
    /// <summary>
    /// Fábrica de efeitos de partículas (GDD Sprint 5.2). Cria Particle Systems
    /// configurados por código para impactos, mortes e coletas.
    /// </summary>
    public static class VFX
    {
        static Material additive;
        static Font font;

        public static Material AdditiveMaterial
        {
            get
            {
                if (additive == null)
                {
                    additive = Resources.Load<Material>("VFX/Additive");
                    if (additive == null) additive = new Material(Shader.Find("Sprites/Default"));
                }
                return additive;
            }
        }

        public static Font UiFont
        {
            get
            {
                if (font == null)
                {
                    font = Resources.Load<Font>("Fonts/RussoOne-Regular");
                    if (font == null) font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
                }
                return font;
            }
        }

        public static ParticleSystem Burst(Vector3 pos, Color color, int count = 16, float speed = 4f,
                                           float size = 0.25f, float life = 0.5f, float gravity = 0f,
                                           float radius = 0.1f)
        {
            var go = new GameObject("VFX_Burst");
            go.transform.position = new Vector3(pos.x, pos.y, 0f);
            var ps = go.AddComponent<ParticleSystem>();
            ps.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);

            var main = ps.main;
            main.duration = 0.25f;
            main.loop = false;
            main.playOnAwake = false;
            main.startLifetime = new ParticleSystem.MinMaxCurve(life * 0.5f, life);
            main.startSpeed = new ParticleSystem.MinMaxCurve(speed * 0.3f, speed);
            main.startSize = new ParticleSystem.MinMaxCurve(size * 0.5f, size);
            main.startColor = color;
            main.gravityModifier = gravity;
            main.simulationSpace = ParticleSystemSimulationSpace.World;
            main.maxParticles = Mathf.Max(8, count * 2);
            main.stopAction = ParticleSystemStopAction.Destroy;

            var em = ps.emission;
            em.rateOverTime = 0f;
            em.SetBursts(new[] { new ParticleSystem.Burst(0f, (short)count) });

            var sh = ps.shape;
            sh.shapeType = ParticleSystemShapeType.Circle;
            sh.radius = radius;

            var col = ps.colorOverLifetime;
            col.enabled = true;
            var g = new Gradient();
            g.SetKeys(new[] { new GradientColorKey(Color.white, 0f), new GradientColorKey(Color.white, 1f) },
                      new[] { new GradientAlphaKey(1f, 0f), new GradientAlphaKey(0.8f, 0.5f), new GradientAlphaKey(0f, 1f) });
            col.color = g;

            var sz = ps.sizeOverLifetime;
            sz.enabled = true;
            sz.size = new ParticleSystem.MinMaxCurve(1f, AnimationCurve.Linear(0f, 1f, 1f, 0.1f));

            var r = go.GetComponent<ParticleSystemRenderer>();
            r.material = AdditiveMaterial;
            r.sortingOrder = 60;

            ps.Play();
            return ps;
        }

        /// <summary>Texto flutuante no mundo ("+1", "-1"), que sobe e desaparece.</summary>
        public static void PopText(Vector3 pos, string text, Color color, float scale = 1f)
        {
            var go = new GameObject("PopText");
            go.transform.position = pos;
            var tm = go.AddComponent<TextMesh>();
            tm.text = text;
            tm.font = UiFont;
            tm.fontSize = 64;
            tm.characterSize = 0.06f * scale;
            tm.anchor = TextAnchor.MiddleCenter;
            tm.alignment = TextAlignment.Center;
            tm.color = color;
            var mr = go.GetComponent<MeshRenderer>();
            mr.sharedMaterial = UiFont.material;
            mr.sortingOrder = 80;
            go.AddComponent<FloatAndFade>();
        }
    }

    /// <summary>Sobe e desaparece (usado pelo PopText).</summary>
    public class FloatAndFade : MonoBehaviour
    {
        public float speed = 1.6f, life = 0.9f;
        float t;
        TextMesh tm;
        Color c0;

        void Awake() { tm = GetComponent<TextMesh>(); if (tm) c0 = tm.color; }

        void Update()
        {
            t += Time.deltaTime;
            transform.position += Vector3.up * speed * Time.deltaTime * (1f - t / life);
            if (tm) tm.color = new Color(c0.r, c0.g, c0.b, Mathf.Clamp01(1f - t / life) * c0.a);
            if (t >= life) Destroy(gameObject);
        }
    }
}
