using UnityEngine;
using UnityEngine.SceneManagement;

namespace SpaceChico
{
    // Adiciona plataformas, cipós e inimigos extras para deixar a fase 01_Pantano mais longa
    public static class LevelExpander
    {
        private const float ArenaShiftX = 90f;

        public static void ExpandIfPantano()
        {
            if (SceneManager.GetActiveScene().name != "01_Pantano") return;
            if (GameObject.Find("Ground_BossArena_Extended") != null) return;

            GameObject ground66 = GameObject.Find("Ground_66_126");
            GameObject platformTemplate = GameObject.Find("Platform_38_46");
            if (ground66 == null || platformTemplate == null) return;

            Transform env = ground66.transform.parent;
            GameObject vineT = GameObject.Find("Prop_Vine");
            GameObject toxicT = GameObject.Find("Hazard_ToxicWater");
            GameObject slimeT = GameObject.Find("Enemy_SlimeCrawler");
            GameObject crystalT = GameObject.Find("Pickup_Crystal");
            GameObject fruitT = GameObject.Find("Pickup_NeonFruit");

            ShiftBossArena(ArenaShiftX);
            ResizeGround(ground66, 66f, 84f, -1.0f);

            // Setor 3: Lago Ácido com Plataformas
            CreateBox(toxicT, env, "Hazard_AcidLake_84_112", 84f, 112f, -1.2f, 1.4f, false);
            CreatePlatform(platformTemplate, env, "Platform_86_90", 88.0f, 0.7f, 4.2f);
            CreatePlatform(platformTemplate, env, "Platform_92_96", 94.0f, 2.1f, 4.5f);
            CreatePlatform(platformTemplate, env, "Platform_98_103", 100.5f, 3.5f, 5.0f);
            CreatePlatform(platformTemplate, env, "Platform_105_109", 107.0f, 1.8f, 4.5f);
            SpawnItem(crystalT, 88f, 1.7f);
            SpawnItem(fruitT, 100.5f, 4.6f);
            SpawnItem(crystalT, 107f, 2.8f);

            // Setor 4: Floresta de Cipós
            CreateBox(ground66, env, "Ground_112_144", 112f, 144f, -1.0f, 5f, true);
            CreateSlime(slimeT, 119f, -0.1f, 113.5f, 125f);
            CreateSlime(slimeT, 135f, -0.1f, 129f, 141.5f);
            CreatePlatform(platformTemplate, env, "Platform_118_126", 122f, 5.5f, 8f);
            CreateVine(vineT, env, 119.5f, 2.1f, 1.08f);
            CreatePlatform(platformTemplate, env, "Platform_132_140", 136f, 6.8f, 8f);
            CreateVine(vineT, env, 133.5f, 2.75f, 1.28f);
            SpawnItem(crystalT, 122f, 6.5f);
            SpawnItem(fruitT, 136.5f, 7.8f);

            // Setor 5: Ponte Quebrada antes do Chefe
            CreateBox(toxicT, env, "Hazard_ToxicGap_144_152", 144f, 152f, -1.2f, 1.4f, false);
            CreatePlatform(platformTemplate, env, "Platform_146_150", 148f, 1.4f, 4.5f);
            CreateBox(ground66, env, "Ground_152_166", 152f, 166f, -1.0f, 5f, true);
            CreateSlime(slimeT, 156f, -0.1f, 153f, 165f);
            CreatePlatform(platformTemplate, env, "Platform_156_162", 159f, 3.2f, 6f);
            SpawnItem(fruitT, 159f, 4.3f);
            CreateBox(toxicT, env, "Hazard_ToxicGap_166_174", 166f, 174f, -1.2f, 1.4f, false);
            CreatePlatform(platformTemplate, env, "Platform_168_172", 170f, 1.4f, 4.5f);

            // Setor 6: Piso da Arena do Chefe
            CreateBox(ground66, env, "Ground_BossArena_Extended", 174f, 216f, -1.0f, 5f, true);
            Physics2D.SyncTransforms();
        }

        private static void ShiftBossArena(float shiftX)
        {
            foreach (Transform t in Object.FindObjectsByType<Transform>(FindObjectsInactive.Include))
            {
                if (t == null || t.name == "Ground_66_126") continue;
                if (t.GetComponentInParent<ParallaxLoop>() != null || t.GetComponentInParent<Canvas>() != null) continue;
                if ((t.parent == null || t.parent.position.x < 85.5f) && t.position.x >= 85.5f)
                    t.position += new Vector3(shiftX, 0f, 0f);
            }

            var boss = Object.FindAnyObjectByType<BossSwamp>();
            if (boss != null) { boss.arenaMinX += shiftX; boss.arenaMaxX += shiftX; }

            var trigger = Object.FindAnyObjectByType<BossArenaTrigger>();
            if (trigger != null) { trigger.cameraMinX += shiftX; trigger.cameraMaxX += shiftX; }

            var cam = Object.FindAnyObjectByType<CameraFollow>();
            if (cam != null) cam.maxX += shiftX;
        }

        private static void ResizeGround(GameObject go, float minX, float maxX, float y)
        {
            float w = maxX - minX;
            go.transform.position = new Vector3((minX + maxX) * 0.5f, y, 0f);
            var sr = go.GetComponent<SpriteRenderer>();
            if (sr != null) { sr.drawMode = SpriteDrawMode.Tiled; sr.size = new Vector2(w, 5f); }
            var box = go.GetComponent<BoxCollider2D>();
            if (box != null) { box.size = new Vector2(w, 5f); box.offset = new Vector2(0f, -2.5f); }
        }

        private static void CreateBox(GameObject tpl, Transform parent, string name, float minX, float maxX, float y, float h, bool offsetDown)
        {
            if (tpl == null) return;
            GameObject go = Object.Instantiate(tpl, parent);
            go.name = name;
            float w = maxX - minX;
            go.transform.position = new Vector3((minX + maxX) * 0.5f, y, 0f);
            var sr = go.GetComponent<SpriteRenderer>();
            if (sr != null) { sr.drawMode = SpriteDrawMode.Tiled; sr.size = new Vector2(w, h); }
            var box = go.GetComponent<BoxCollider2D>();
            if (box != null) { box.size = new Vector2(w, h); if (offsetDown) box.offset = new Vector2(0f, -h * 0.5f); }
        }

        private static void CreatePlatform(GameObject tpl, Transform parent, string name, float x, float y, float w)
        {
            CreateBox(tpl, parent, name, x - w * 0.5f, x + w * 0.5f, y, 0.9f, true);
        }

        private static void CreateVine(GameObject tpl, Transform parent, float x, float y, float scaleY)
        {
            if (tpl == null) return;
            GameObject go = Object.Instantiate(tpl, new Vector3(x, y, 0f), Quaternion.identity, parent);
            go.transform.localScale = new Vector3(1f, scaleY, 1f);
            Physics2D.SyncTransforms();
            var v = go.GetComponent<Vine>();
            if (v != null) v.CalculateBounds();
        }

        private static void CreateSlime(GameObject tpl, float x, float y, float minX, float maxX)
        {
            if (tpl == null) return;
            GameObject go = Object.Instantiate(tpl, new Vector3(x, y, 0f), Quaternion.identity);
            var s = go.GetComponent<SlimeCrawler>();
            if (s != null) s.SetPatrolRange(minX, maxX);
        }

        private static void SpawnItem(GameObject tpl, float x, float y)
        {
            if (tpl != null) Object.Instantiate(tpl, new Vector3(x, y, 0f), Quaternion.identity);
        }
    }
}
