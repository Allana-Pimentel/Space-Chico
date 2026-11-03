using UnityEngine;
using UnityEngine.SceneManagement;

namespace SpaceChico
{
    /// <summary>
    /// Expande a fase 01_Pantano adicionando 3 novos setores completos de desafios
    /// (Lago Ácido, Floresta de Cipós Duplos e Ponte Quebrada dos Guardiões)
    /// antes da Arena do Chefe, deixando a fase quase duas vezes mais longa.
    /// </summary>
    public static class LevelExpander
    {
        private const float ArenaShiftX = 90f;

        public static void ExpandIfPantano()
        {
            Scene active = SceneManager.GetActiveScene();
            if (active.name != "01_Pantano") return;

            // Evita expandir duas vezes na mesma instância da cena
            if (GameObject.Find("Ground_BossArena_Extended") != null) return;

            GameObject ground66 = GameObject.Find("Ground_66_126");
            GameObject platformTemplate = GameObject.Find("Platform_38_46");
            if (ground66 == null || platformTemplate == null) return;

            Transform envParent = ground66.transform.parent;

            GameObject vineTemplate = GameObject.Find("Prop_Vine");
            GameObject toxicTemplate = GameObject.Find("Hazard_ToxicWater");
            GameObject slimeTemplate = GameObject.Find("Enemy_SlimeCrawler");
            GameObject crystalTemplate = GameObject.Find("Pickup_Crystal");
            GameObject oxygenTemplate = GameObject.Find("Pickup_Oxygen");
            GameObject fruitTemplate = GameObject.Find("Pickup_NeonFruit");
            GameObject shieldTemplate = GameObject.Find("Pickup_PowerShield");
            GameObject mushroomTemplate = GameObject.Find("Prop_prop_mushrooms");
            GameObject flowerTemplate = GameObject.Find("Prop_prop_alien_flower");
            GameObject stumpTemplate = GameObject.Find("Prop_prop_tree_stump");

            // 1. Desloca a Arena do Chefe (+90 unidades para a direita: x = 176..216)
            ShiftBossArena(ArenaShiftX);

            // 2. Ajusta o piso Ground_66_126 para terminar em x = 84 (início do Lago Ácido)
            ResizeGround(ground66, 66f, 84f, -1.0f);

            // =========================================================================
            // SETOR 3 (x = 84 a 112): Lago Ácido Profundo com Plataformas Suspensas
            // =========================================================================
            CreateToxicWater(toxicTemplate, envParent, "Hazard_AcidLake_84_112", 84f, 112f, -1.2f);

            CreatePlatform(platformTemplate, envParent, "Platform_86_90", 88.0f, 0.7f, 4.2f);
            CreatePlatform(platformTemplate, envParent, "Platform_92_96", 94.0f, 2.1f, 4.5f);
            CreatePlatform(platformTemplate, envParent, "Platform_98_103", 100.5f, 3.5f, 5.0f);
            CreatePlatform(platformTemplate, envParent, "Platform_105_109", 107.0f, 1.8f, 4.5f);

            CreateClone(crystalTemplate, "Pickup_Crystal_S3_1", new Vector3(87.2f, 1.7f, 0f));
            CreateClone(crystalTemplate, "Pickup_Crystal_S3_2", new Vector3(88.8f, 1.7f, 0f));
            CreateClone(oxygenTemplate, "Pickup_Oxygen_S3", new Vector3(94.0f, 3.2f, 0f));
            CreateClone(crystalTemplate, "Pickup_Crystal_S3_3", new Vector3(99.3f, 4.5f, 0f));
            CreateClone(fruitTemplate, "Pickup_NeonFruit_S3", new Vector3(100.5f, 4.6f, 0f));
            CreateClone(crystalTemplate, "Pickup_Crystal_S3_4", new Vector3(101.7f, 4.5f, 0f));
            CreateClone(crystalTemplate, "Pickup_Crystal_S3_5", new Vector3(107.0f, 2.8f, 0f));

            // =========================================================================
            // SETOR 4 (x = 112 a 144): Floresta de Cogumelos e Cipós Duplos
            // =========================================================================
            CreateGround(ground66, envParent, "Ground_112_144", 112f, 144f, -1.0f);

            // Inimigos patrulhando o térreo da floresta
            CreateSlime(slimeTemplate, "Enemy_Slime_Forest1", 119.0f, -0.1f, 113.5f, 125.0f);
            CreateSlime(slimeTemplate, "Enemy_Slime_Forest2", 135.0f, -0.1f, 129.0f, 141.5f);

            // Plataforma alta 1 + Cipó 1
            CreatePlatform(platformTemplate, envParent, "Platform_118_126", 122.0f, 5.5f, 8.0f);
            CreateVine(vineTemplate, envParent, "Prop_Vine_Forest1", 119.5f, 2.1f, 1.08f);
            CreateClone(crystalTemplate, "Pickup_Crystal_S4_1", new Vector3(120.5f, 6.5f, 0f));
            CreateClone(crystalTemplate, "Pickup_Crystal_S4_2", new Vector3(122.2f, 6.5f, 0f));
            CreateClone(fruitTemplate, "Pickup_NeonFruit_S4_1", new Vector3(124.2f, 6.5f, 0f));

            // Plataforma alta 2 + Cipó 2
            CreatePlatform(platformTemplate, envParent, "Platform_132_140", 136.0f, 6.8f, 8.0f);
            CreateVine(vineTemplate, envParent, "Prop_Vine_Forest2", 133.5f, 2.75f, 1.28f);
            CreateClone(crystalTemplate, "Pickup_Crystal_S4_3", new Vector3(134.5f, 7.8f, 0f));
            CreateClone(shieldTemplate, "Pickup_PowerShield_S4", new Vector3(136.5f, 7.9f, 0f));
            CreateClone(crystalTemplate, "Pickup_Crystal_S4_4", new Vector3(138.5f, 7.8f, 0f));

            // Oxigênio e decoração no térreo da floresta
            CreateClone(oxygenTemplate, "Pickup_Oxygen_S4", new Vector3(127.5f, 0.5f, 0f));
            CreateClone(mushroomTemplate, "Prop_Mushrooms_S4_1", new Vector3(115.0f, -0.1f, 0f), envParent);
            CreateClone(stumpTemplate, "Prop_Stump_S4_1", new Vector3(123.5f, -0.1f, 0f), envParent);
            CreateClone(flowerTemplate, "Prop_Flower_S4_1", new Vector3(128.5f, -0.1f, 0f), envParent);
            CreateClone(mushroomTemplate, "Prop_Mushrooms_S4_2", new Vector3(138.0f, -0.1f, 0f), envParent);
            CreateClone(flowerTemplate, "Prop_Flower_S4_2", new Vector3(142.0f, -0.1f, 0f), envParent);

            // =========================================================================
            // SETOR 5 (x = 144 a 174): Ponte Quebrada dos Guardiões
            // =========================================================================
            // Fosso tóxico 1 (x = 144..152)
            CreateToxicWater(toxicTemplate, envParent, "Hazard_ToxicGap_144_152", 144f, 152f, -1.2f);
            CreatePlatform(platformTemplate, envParent, "Platform_146_150", 148.0f, 1.4f, 4.5f);
            CreateClone(crystalTemplate, "Pickup_Crystal_S5_1", new Vector3(147.2f, 2.4f, 0f));
            CreateClone(crystalTemplate, "Pickup_Crystal_S5_2", new Vector3(148.8f, 2.4f, 0f));

            // Ilha central dos Guardiões (x = 152..166)
            CreateGround(ground66, envParent, "Ground_152_166", 152f, 166f, -1.0f);
            CreateSlime(slimeTemplate, "Enemy_Slime_Bridge1", 155.5f, -0.1f, 152.8f, 158.8f);
            CreateSlime(slimeTemplate, "Enemy_Slime_Bridge2", 162.5f, -0.1f, 159.2f, 165.2f);

            // Plataforma de suprimentos antes do Boss
            CreatePlatform(platformTemplate, envParent, "Platform_156_162", 159.0f, 3.2f, 6.0f);
            CreateClone(oxygenTemplate, "Pickup_Oxygen_S5", new Vector3(157.5f, 4.3f, 0f));
            CreateClone(crystalTemplate, "Pickup_Crystal_S5_3", new Vector3(159.0f, 4.3f, 0f));
            CreateClone(fruitTemplate, "Pickup_NeonFruit_S5", new Vector3(160.5f, 4.3f, 0f));
            CreateClone(stumpTemplate, "Prop_Stump_S5", new Vector3(159.0f, -0.1f, 0f), envParent);

            // Fosso tóxico 2 (x = 166..174)
            CreateToxicWater(toxicTemplate, envParent, "Hazard_ToxicGap_166_174", 166f, 174f, -1.2f);
            CreatePlatform(platformTemplate, envParent, "Platform_168_172", 170.0f, 1.4f, 4.5f);
            CreateClone(crystalTemplate, "Pickup_Crystal_S5_4", new Vector3(169.2f, 2.4f, 0f));
            CreateClone(crystalTemplate, "Pickup_Crystal_S5_5", new Vector3(170.8f, 2.4f, 0f));

            // =========================================================================
            // SETOR 6 (x = 174 a 216): Piso da Arena do Chefe do Pântano
            // =========================================================================
            CreateGround(ground66, envParent, "Ground_BossArena_Extended", 174f, 216f, -1.0f);

            Physics2D.SyncTransforms();
        }

        private static void ShiftBossArena(float shiftX)
        {
            // Desloca plataformas, paredes, decorações e gatilhos que estavam em x >= 85.5
            Transform[] allTransforms = Object.FindObjectsByType<Transform>(FindObjectsInactive.Include, FindObjectsSortMode.None);
            foreach (Transform t in allTransforms)
            {
                if (t == null) continue;
                if (t.gameObject.name == "Ground_66_126") continue;
                if (t.GetComponentInParent<ParallaxLoop>() != null) continue;
                if (t.GetComponentInParent<Canvas>() != null) continue;

                // Só move o objeto raiz daquele elemento (evita mover filho duas vezes se o pai também estiver em x >= 85.5)
                bool parentAlsoInArena = t.parent != null && t.parent.position.x >= 85.5f;
                if (!parentAlsoInArena && t.position.x >= 85.5f)
                {
                    t.position += new Vector3(shiftX, 0f, 0f);
                }
            }

            // Atualiza limites da Arena no BossSwamp
            BossSwamp boss = Object.FindAnyObjectByType<BossSwamp>();
            if (boss != null)
            {
                boss.arenaMinX += shiftX;
                boss.arenaMaxX += shiftX;
            }

            // Atualiza limites da câmera no BossArenaTrigger
            BossArenaTrigger trigger = Object.FindAnyObjectByType<BossArenaTrigger>();
            if (trigger != null)
            {
                trigger.cameraMinX += shiftX;
                trigger.cameraMaxX += shiftX;
            }

            // Expande o limite máximo da câmera da fase
            CameraFollow camFollow = Object.FindAnyObjectByType<CameraFollow>();
            if (camFollow != null)
            {
                camFollow.maxX += shiftX;
            }
        }

        private static GameObject CreateGround(GameObject template, Transform parent, string name, float minX, float maxX, float topY)
        {
            GameObject go = Object.Instantiate(template, parent);
            go.name = name;
            ResizeGround(go, minX, maxX, topY);
            return go;
        }

        private static void ResizeGround(GameObject go, float minX, float maxX, float topY)
        {
            float width = maxX - minX;
            go.transform.position = new Vector3((minX + maxX) * 0.5f, topY, 0f);

            var sr = go.GetComponent<SpriteRenderer>();
            if (sr != null)
            {
                sr.drawMode = SpriteDrawMode.Tiled;
                sr.size = new Vector2(width, 5f);
            }

            var box = go.GetComponent<BoxCollider2D>();
            if (box != null)
            {
                box.size = new Vector2(width, 5f);
                box.offset = new Vector2(0f, -2.5f);
            }
        }

        private static GameObject CreatePlatform(GameObject template, Transform parent, string name, float centerX, float topY, float width)
        {
            GameObject go = Object.Instantiate(template, parent);
            go.name = name;
            go.transform.position = new Vector3(centerX, topY, 0f);

            var sr = go.GetComponent<SpriteRenderer>();
            if (sr != null)
            {
                sr.drawMode = SpriteDrawMode.Tiled;
                sr.size = new Vector2(width, 0.9f);
            }

            var box = go.GetComponent<BoxCollider2D>();
            if (box != null)
            {
                box.size = new Vector2(width, 0.9f);
                box.offset = new Vector2(0f, -0.45f);
            }
            return go;
        }

        private static GameObject CreateToxicWater(GameObject template, Transform parent, string name, float minX, float maxX, float centerY)
        {
            if (template == null) return null;
            float width = maxX - minX;
            GameObject go = Object.Instantiate(template, parent);
            go.name = name;
            go.transform.position = new Vector3((minX + maxX) * 0.5f, centerY, 0f);

            var sr = go.GetComponent<SpriteRenderer>();
            if (sr != null)
            {
                sr.drawMode = SpriteDrawMode.Tiled;
                sr.size = new Vector2(width, 1.4f);
            }

            var box = go.GetComponent<BoxCollider2D>();
            if (box != null)
            {
                box.size = new Vector2(width, 1.4f);
            }
            return go;
        }

        private static GameObject CreateVine(GameObject template, Transform parent, string name, float x, float y, float scaleY)
        {
            if (template == null) return null;
            GameObject go = Object.Instantiate(template, parent);
            go.name = name;
            go.transform.position = new Vector3(x, y, 0f);
            go.transform.localScale = new Vector3(1f, scaleY, 1f);

            Physics2D.SyncTransforms();
            var vine = go.GetComponent<Vine>();
            if (vine != null) vine.CalculateBounds();
            return go;
        }

        private static GameObject CreateSlime(GameObject template, string name, float x, float y, float patrolMinX, float patrolMaxX)
        {
            if (template == null) return null;
            GameObject go = Object.Instantiate(template, new Vector3(x, y, 0f), Quaternion.identity);
            go.name = name;

            var slime = go.GetComponent<SlimeCrawler>();
            if (slime != null)
            {
                slime.SetPatrolRange(patrolMinX, patrolMaxX);
            }
            return go;
        }

        private static GameObject CreateClone(GameObject template, string name, Vector3 pos, Transform parent = null)
        {
            if (template == null) return null;
            GameObject go = parent != null
                ? Object.Instantiate(template, pos, Quaternion.identity, parent)
                : Object.Instantiate(template, pos, Quaternion.identity);
            go.name = name;
            return go;
        }
    }
}

