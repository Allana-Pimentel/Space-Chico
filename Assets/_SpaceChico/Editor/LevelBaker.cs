#if UNITY_EDITOR
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

namespace SpaceChico.Editor
{
    public static class LevelBaker
    {
        [MenuItem("Space Chico/4. Gerar Fase Completa na Cena (Bake Permanente) %3", priority = 4)]
        public static void BakeLevelIntoScene()
        {
            var scene = EditorSceneManager.OpenScene("Assets/_SpaceChico/Scenes/01_Pantano.unity");
            if (!scene.IsValid()) return;

            if (GameObject.Find("Ground_BossArena_Extended") != null)
            {
                EditorUtility.DisplayDialog("Space Chico", "A fase ja esta completamente gerada na cena!", "OK");
                return;
            }

            GameObject ground66 = GameObject.Find("Ground_66_126");
            GameObject platT = GameObject.Find("Platform_38_46");
            if (ground66 == null || platT == null) return;

            Transform env = ground66.transform.parent;
            GameObject vineT = GameObject.Find("Prop_Vine");
            GameObject toxicT = GameObject.Find("Hazard_ToxicWater");
            GameObject slimeT = GameObject.Find("Enemy_SlimeCrawler");
            GameObject crystalT = GameObject.Find("Pickup_Crystal");
            GameObject fruitT = GameObject.Find("Pickup_NeonFruit");

            // Shift Boss Arena by 90 units
            foreach (Transform t in Object.FindObjectsByType<Transform>(FindObjectsInactive.Include))
            {
                if (t == null || t.name == "Ground_66_126") continue;
                if (t.GetComponentInParent<ParallaxLoop>() != null || t.GetComponentInParent<Canvas>() != null) continue;
                if ((t.parent == null || t.parent.position.x < 85.5f) && t.position.x >= 85.5f)
                    t.position += new Vector3(90f, 0f, 0f);
            }

            var boss = Object.FindAnyObjectByType<BossSwamp>();
            if (boss != null) { boss.arenaMinX += 90f; boss.arenaMaxX += 90f; }
            var trigger = Object.FindAnyObjectByType<BossArenaTrigger>();
            if (trigger != null) { trigger.cameraMinX += 90f; trigger.cameraMaxX += 90f; }
            var cam = Object.FindAnyObjectByType<CameraFollow>();
            if (cam != null) cam.maxX += 90f;

            ResizeGround(ground66, 66f, 84f, -1.0f);

            Spawn(fruitT, 30.0f, 0.5f, env);
            Spawn(crystalT, 68.0f, 0.5f, env);

            CreateBox(toxicT, env, "Hazard_AcidLake_84_112", 84f, 112f, -1.2f, 1.4f, false);
            CreatePlat(platT, env, 88.0f, 0.7f, 4.2f);
            CreatePlat(platT, env, 94.0f, 2.1f, 4.5f);
            CreatePlat(platT, env, 100.5f, 3.5f, 5.0f);
            CreatePlat(platT, env, 107.0f, 1.8f, 4.5f);
            Spawn(crystalT, 88f, 1.7f, env);
            Spawn(crystalT, 94f, 3.1f, env);
            Spawn(fruitT, 100.5f, 4.6f, env);
            Spawn(crystalT, 107f, 2.8f, env);

            CreateBox(ground66, env, "Ground_112_144", 112f, 144f, -1.0f, 5f, true);
            CreateSlime(slimeT, 119f, -0.1f, 113.5f, 125f);
            CreateSlime(slimeT, 135f, -0.1f, 129f, 141.5f);
            CreatePlat(platT, env, 122f, 5.5f, 8f);
            CreateVine(vineT, env, 119.5f, 2.1f, 1.08f);
            CreatePlat(platT, env, 136f, 6.8f, 8f);
            CreateVine(vineT, env, 133.5f, 2.75f, 1.28f);
            Spawn(crystalT, 122f, 6.5f, env);
            Spawn(fruitT, 136.5f, 7.8f, env);

            CreateBox(toxicT, env, "Hazard_ToxicGap_144_152", 144f, 152f, -1.2f, 1.4f, false);
            CreatePlat(platT, env, 148f, 1.4f, 4.5f);
            CreateBox(ground66, env, "Ground_152_166", 152f, 166f, -1.0f, 5f, true);
            CreateSlime(slimeT, 156f, -0.1f, 153f, 165f);
            CreatePlat(platT, env, 159f, 3.2f, 6f);
            Spawn(fruitT, 159f, 4.3f, env);
            CreateBox(toxicT, env, "Hazard_ToxicGap_166_174", 166f, 174f, -1.2f, 1.4f, false);
            CreatePlat(platT, env, 170f, 1.4f, 4.5f);
            CreateBox(ground66, env, "Ground_BossArena_Extended", 174f, 216f, -1.0f, 5f, true);

            EditorSceneManager.MarkSceneDirty(scene);
            EditorSceneManager.SaveScene(scene);

            SceneOpener.FocarNoChico();
            EditorUtility.DisplayDialog("Space Chico", "Fase gerada com sucesso e salva na cena!\nAgora todos os objetos aparecem no Editor mesmo sem dar Play.", "OK");
        }

        static void ResizeGround(GameObject go, float minX, float maxX, float y) => CreateBox(go, null, go.name, minX, maxX, y, 5f, true, false);

        static void CreateBox(GameObject tpl, Transform parent, string name, float minX, float maxX, float y, float h, bool offsetDown, bool clone = true)
        {
            if (tpl == null) return;
            GameObject go = clone ? (GameObject)PrefabUtility.InstantiatePrefab(PrefabUtility.GetCorrespondingObjectFromSource(tpl) ?? tpl, parent) : tpl;
            if (go == null) go = clone ? Object.Instantiate(tpl, parent) : tpl;
            go.name = name;
            float w = maxX - minX;
            go.transform.position = new Vector3((minX + maxX) * 0.5f, y, 0f);
            var sr = go.GetComponent<SpriteRenderer>();
            if (sr != null)
            {
                if (sr.sprite != null) { sr.drawMode = SpriteDrawMode.Tiled; sr.size = new Vector2(w, h); }
                else { sr.enabled = false; }
            }
            var box = go.GetComponent<BoxCollider2D>();
            if (box != null) { box.size = new Vector2(w, h); if (offsetDown) box.offset = new Vector2(0f, -h * 0.5f); }
        }

        static void CreatePlat(GameObject tpl, Transform p, float x, float y, float w) => CreateBox(tpl, p, "Platform_" + Mathf.RoundToInt(x - w * 0.5f) + "_" + Mathf.RoundToInt(x + w * 0.5f), x - w * 0.5f, x + w * 0.5f, y, 0.9f, true);

        static void CreateVine(GameObject tpl, Transform p, float x, float y, float sy)
        {
            if (tpl == null) return;
            var src = PrefabUtility.GetCorrespondingObjectFromSource(tpl) ?? tpl;
            var go = (GameObject)PrefabUtility.InstantiatePrefab(src, p);
            if (go == null) go = Object.Instantiate(tpl, p);
            go.transform.position = new Vector3(x, y, 0f);
            go.transform.localScale = new Vector3(1f, sy, 1f);
            go.GetComponent<Vine>()?.CalculateBounds();
        }

        static void CreateSlime(GameObject tpl, float x, float y, float minX, float maxX)
        {
            if (tpl == null) return;
            var src = PrefabUtility.GetCorrespondingObjectFromSource(tpl) ?? tpl;
            var go = (GameObject)PrefabUtility.InstantiatePrefab(src);
            if (go == null) go = Object.Instantiate(tpl);
            go.transform.position = new Vector3(x, y, 0f);
            go.GetComponent<SlimeCrawler>()?.SetPatrolRange(minX, maxX);
        }

        static void Spawn(GameObject tpl, float x, float y, Transform parent)
        {
            if (tpl == null) return;
            var src = PrefabUtility.GetCorrespondingObjectFromSource(tpl) ?? tpl;
            var go = (GameObject)PrefabUtility.InstantiatePrefab(src, parent);
            if (go == null) go = Object.Instantiate(tpl, parent);
            go.transform.position = new Vector3(x, y, 0f);
        }
    }
}
#endif

