using UnityEngine;

namespace SpaceChico
{
    public static class VFX
    {
        static Font font;

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

        public static ParticleSystem Burst(Vector3 pos, Color color, int count = 12, float speed = 3.5f,
                                           float size = 0.2f, float life = 0.4f, float gravity = 0f,
                                           float radius = 0.1f)
        {
            var go = new GameObject("VFX_Burst");
            go.transform.position = new Vector3(pos.x, pos.y, 0f);
            var ps = go.AddComponent<ParticleSystem>();
            ps.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);

            var main = ps.main;
            main.duration = 0.25f;
            main.loop = false;
            main.startLifetime = life;
            main.startSpeed = speed;
            main.startSize = size;
            main.startColor = color;
            main.gravityModifier = gravity;
            main.stopAction = ParticleSystemStopAction.Destroy;

            var em = ps.emission;
            em.rateOverTime = 0f;
            em.SetBursts(new[] { new ParticleSystem.Burst(0f, (short)count) });

            var r = go.GetComponent<ParticleSystemRenderer>();
            r.material = new Material(Shader.Find("Sprites/Default"));
            r.sortingOrder = 60;

            ps.Play();
            return ps;
        }

        public static void PopText(Vector3 pos, string text, Color color, float scale = 1f)
        {
            var go = new GameObject("PopText");
            go.transform.position = pos;
            var tm = go.AddComponent<TextMesh>();
            tm.text = text;
            tm.font = UiFont;
            tm.fontSize = 48;
            tm.characterSize = 0.07f * scale;
            tm.anchor = TextAnchor.MiddleCenter;
            tm.color = color;

            var mr = go.GetComponent<MeshRenderer>();
            mr.sharedMaterial = UiFont.material;
            mr.sortingOrder = 80;

            Object.Destroy(go, 0.75f);
        }
    }
}
