using System.Collections.Generic;
using System.IO;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

namespace SpaceChico.Editor
{
    /// <summary>
    /// Construtor mestre do projeto Space Chico (Unity 6).
    /// Executa através do menu: Tools ▸ Space Chico ▸ Build Everything.
    /// Gera tags, layers, ScriptableObjects, prefabs, interface e monta as cenas completas.
    /// </summary>
    public static class SpaceChicoBuilder
    {
        private const string RootDir = "Assets/_SpaceChico";
        private const string ArtDir = RootDir + "/Art";
        private const string DataDir = RootDir + "/Data/Items";
        private const string PrefabDir = RootDir + "/Prefabs";
        private const string SceneDir = RootDir + "/Scenes";

        [MenuItem("Tools/Space Chico/Build Everything (Complete Project & Scenes)", priority = 0)]
        public static void BuildEverything()
        {
            Debug.Log("<color=#00e5ff><b>[Space Chico] INICIANDO CONSTRUÇÃO COMPLETA DO JOGO...</b></color>");

            EnsureDirectories();
            SetupTagsAndLayers();
            SpriteProcessor.ProcessAllSprites();

            var items = CreateItemScriptableObjects();
            var prefabs = CreateAllPrefabs(items);

            BuildScene00_Intro();
            BuildScene01_Pantano(prefabs, items);

            SetupBuildSettings();

            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();

            Debug.Log("<color=#00ff88><b>[Space Chico] PROJETO CONSTRUÍDO COM SUCESSO! Abra a cena '00_Intro' ou '01_Pantano' e dê Play!</b></color>");
        }

        public static void BuildAllBatch()
        {
            BuildEverything();
            EditorApplication.Exit(0);
        }

        private static void EnsureDirectories()
        {
            string[] dirs = { RootDir, ArtDir, DataDir, PrefabDir, SceneDir, RootDir + "/Resources/VFX" };
            foreach (var d in dirs)
            {
                if (!Directory.Exists(d)) Directory.CreateDirectory(d);
            }
            AssetDatabase.Refresh();
        }

        // ====================================================================
        //  1. TAGS & LAYERS
        // ====================================================================
        [MenuItem("Tools/Space Chico/2. Setup Tags and Layers")]
        public static void SetupTagsAndLayers()
        {
            var tagManager = new SerializedObject(AssetDatabase.LoadAllAssetsAtPath("ProjectSettings/TagManager.asset")[0]);

            // Tags
            var tagsProp = tagManager.FindProperty("tags");
            AddTag(tagsProp, Layers.TagPlayer);
            AddTag(tagsProp, Layers.TagEnemy);
            AddTag(tagsProp, Layers.TagGround);

            // Layers (8 a 31)
            var layersProp = tagManager.FindProperty("layers");
            SetLayer(layersProp, 8, Layers.Ground);
            SetLayer(layersProp, 9, Layers.Player);
            SetLayer(layersProp, 10, Layers.Enemy);
            SetLayer(layersProp, 11, Layers.EnemyTrigger);
            SetLayer(layersProp, 12, Layers.Projectile);
            SetLayer(layersProp, 13, Layers.Pickup);
            SetLayer(layersProp, 14, Layers.Hazard);

            tagManager.ApplyModifiedProperties();
            Layers.ApplyCollisionMatrix();
            Debug.Log("[Space Chico] Tags e Layers configuradas.");
        }

        private static void AddTag(SerializedProperty tagsProp, string tag)
        {
            for (int i = 0; i < tagsProp.arraySize; i++)
            {
                if (tagsProp.GetArrayElementAtIndex(i).stringValue == tag) return;
            }
            tagsProp.InsertArrayElementAtIndex(tagsProp.arraySize);
            tagsProp.GetArrayElementAtIndex(tagsProp.arraySize - 1).stringValue = tag;
        }

        private static void SetLayer(SerializedProperty layersProp, int index, string layerName)
        {
            var sp = layersProp.GetArrayElementAtIndex(index);
            sp.stringValue = layerName;
        }

        // ====================================================================
        //  2. SCRIPTABLE OBJECTS (ITENS)
        // ====================================================================
        private struct ItemMap
        {
            public ItemData thruster;
            public ItemData fruit;
            public ItemData crystal;
            public ItemData oxygen;
            public ItemData shield;
            public ItemData doubleJump;
        }

        private static ItemMap CreateItemScriptableObjects()
        {
            ItemMap m = new ItemMap();

            m.thruster = CreateOrUpdateItem("Item_Thruster", "Propulsor Quântico",
                "Peça central da espaçonave de Chico. Converte energia plasmática em impulsão sub-luz.",
                "item_thruster", ItemCategory.ShipPart, PowerType.None, 0f, new Color(0.2f, 0.9f, 1f));

            m.fruit = CreateOrUpdateItem("Item_NeonFruit", "Fruto de Neon",
                "Espécime vegetal alienígena que restaura 1 ponto de vida ou oxigênio.",
                "item_neon_fruit", ItemCategory.Consumable, PowerType.None, 0f, new Color(0.2f, 1f, 0.5f));

            m.crystal = CreateOrUpdateItem("Item_Crystal", "Cristal Energético",
                "Formação geológica translúcida que soma pontuação galáctica ao Chico.",
                "item_crystal", ItemCategory.Consumable, PowerType.None, 0f, new Color(0.8f, 0.4f, 1f));

            m.oxygen = CreateOrUpdateItem("Item_Oxygen", "Cápsula de O₂",
                "Cilindro de ar puro que restaura 30% da reserva de oxigênio do traje.",
                "item_oxygen", ItemCategory.Consumable, PowerType.None, 0f, new Color(0.3f, 0.8f, 1f));

            m.shield = CreateOrUpdateItem("Item_Shield", "Escudo Hexagonal",
                "Gera uma cúpula de energia que absorve todo o dano recebido por 6 segundos.",
                "item_shield", ItemCategory.Power, PowerType.Shield, 6f, new Color(1f, 0.85f, 0.3f));

            m.doubleJump = CreateOrUpdateItem("Item_DoubleJump", "Botas de Plasma",
                "Permite efetuar um pulo duplo no ar durante 8 segundos.",
                "item_double_jump", ItemCategory.Power, PowerType.DoubleJump, 8f, new Color(0.4f, 1f, 0.8f));

            AssetDatabase.SaveAssets();
            return m;
        }

        private static ItemData CreateOrUpdateItem(string id, string displayName, string desc, string spriteName,
                                                  ItemCategory category, PowerType power, float duration, Color accent)
        {
            string path = $"{DataDir}/{id}.asset";
            var item = AssetDatabase.LoadAssetAtPath<ItemData>(path);
            if (item == null)
            {
                item = ScriptableObject.CreateInstance<ItemData>();
                AssetDatabase.CreateAsset(item, path);
            }

            item.id = id;
            item.displayName = displayName;
            item.description = desc;
            item.category = category;
            item.power = power;
            item.powerDuration = duration;
            item.accent = accent;

            var sprite = AssetDatabase.LoadAssetAtPath<Sprite>($"{ArtDir}/{spriteName}.png");
            if (sprite != null) item.icon = sprite;

            EditorUtility.SetDirty(item);
            return item;
        }

        // ====================================================================
        //  3. PREFABS
        // ====================================================================
        private struct PrefabMap
        {
            public GameObject player;
            public GameObject slime;
            public GameObject bossSwamp;
            public GameObject bossSpit;
            public GameObject projectile;
            public GameObject shipPart;
            public GameObject crystal;
            public GameObject oxygen;
            public GameObject fruit;
            public GameObject powerShield;
            public GameObject powerDoubleJump;
            public GameObject vine;
            public GameObject hazardWater;
            public GameObject hudCanvas;
        }

        private static PrefabMap CreateAllPrefabs(ItemMap items)
        {
            PrefabMap p = new PrefabMap();

            p.projectile = CreateProjectilePrefab();
            p.player = CreatePlayerPrefab(p.projectile);
            p.slime = CreateSlimePrefab();
            p.shipPart = CreateShipPartPrefab(items.thruster);
            p.bossSpit = CreateBossSpitPrefab();
            p.bossSwamp = CreateBossSwampPrefab(p.bossSpit, p.shipPart);
            p.crystal = CreatePickupPrefab("Pickup_Crystal", "item_crystal", PickupType.Crystal, 1, null, new Color(0.8f, 0.4f, 1f));
            p.oxygen = CreatePickupPrefab("Pickup_Oxygen", "item_oxygen", PickupType.Oxygen, 30, null, new Color(0.3f, 0.8f, 1f));
            p.fruit = CreatePickupPrefab("Pickup_NeonFruit", "item_neon_fruit", PickupType.NeonFruit, 1, null, new Color(0.2f, 1f, 0.4f));
            p.powerShield = CreatePickupPrefab("Pickup_PowerShield", "item_shield", PickupType.PowerUpShield, 1, items.shield, new Color(1f, 0.85f, 0.3f));
            p.powerDoubleJump = CreatePickupPrefab("Pickup_PowerDoubleJump", "item_double_jump", PickupType.PowerUpDoubleJump, 1, items.doubleJump, new Color(0.4f, 1f, 0.8f));
            p.vine = CreateVinePrefab();
            p.hazardWater = CreateHazardWaterPrefab();
            p.hudCanvas = CreateHudCanvasPrefab();

            return p;
        }

        private static GameObject CreateProjectilePrefab()
        {
            string path = $"{PrefabDir}/Projectile_Laser.prefab";
            var go = new GameObject("Projectile_Laser");
            go.layer = LayerMask.NameToLayer(Layers.Projectile);

            var sr = go.AddComponent<SpriteRenderer>();
            sr.sortingOrder = 50;

            // Cria sprite simples brilhante se necessário
            var laserTex = new Texture2D(32, 8, TextureFormat.RGBA32, false);
            for (int y = 0; y < 8; y++)
            {
                for (int x = 0; x < 32; x++)
                {
                    float d = Mathf.Abs(y - 3.5f) / 3.5f;
                    laserTex.SetPixel(x, y, new Color(0.3f, 0.9f, 1f, 1f - d * 0.7f));
                }
            }
            laserTex.Apply();
            var sprite = Sprite.Create(laserTex, new Rect(0, 0, 32, 8), new Vector2(0.5f, 0.5f), 32);
            sr.sprite = sprite;

            var rb = go.AddComponent<Rigidbody2D>();
            rb.gravityScale = 0f;
            rb.collisionDetectionMode = CollisionDetectionMode2D.Continuous;

            var col = go.AddComponent<BoxCollider2D>();
            col.isTrigger = true;
            col.size = new Vector2(0.8f, 0.25f);

            var proj = go.AddComponent<Projectile>();
            proj.speed = 15f;
            proj.damage = 1;

            var prefab = PrefabUtility.SaveAsPrefabAsset(go, path);
            Object.DestroyImmediate(go);
            return prefab;
        }

        private static GameObject CreatePlayerPrefab(GameObject projectilePrefab)
        {
            string path = $"{PrefabDir}/Player_Chico.prefab";
            var go = new GameObject("Player_Chico");
            go.tag = Layers.TagPlayer;
            go.layer = LayerMask.NameToLayer(Layers.Player);

            var sr = go.AddComponent<SpriteRenderer>();
            sr.sortingOrder = 30;

            // Atribui sprites do Chico
            sr.sprite = AssetDatabase.LoadAssetAtPath<Sprite>($"{ArtDir}/chico_idle.png");

            var rb = go.AddComponent<Rigidbody2D>();
            rb.collisionDetectionMode = CollisionDetectionMode2D.Continuous;
            rb.freezeRotation = true;
            rb.gravityScale = 3.2f;

            var col = go.AddComponent<CapsuleCollider2D>();
            col.size = new Vector2(0.85f, 1.4f);
            col.offset = new Vector2(0f, 0.7f);

            var stats = go.AddComponent<PlayerStats>();
            var inv = go.AddComponent<Inventory>();
            var shooter = go.AddComponent<PlayerShooter>();
            shooter.projectilePrefab = projectilePrefab;

            var powers = go.AddComponent<PlayerPowers>();

            var pc = go.AddComponent<PlayerController>();
            pc.spriteRenderer = sr;
            pc.spriteIdle = AssetDatabase.LoadAssetAtPath<Sprite>($"{ArtDir}/chico_idle.png");
            pc.spriteRunA = AssetDatabase.LoadAssetAtPath<Sprite>($"{ArtDir}/chico_run_0.png");
            pc.spriteRunB = AssetDatabase.LoadAssetAtPath<Sprite>($"{ArtDir}/chico_run_1.png");
            pc.spriteJump = AssetDatabase.LoadAssetAtPath<Sprite>($"{ArtDir}/chico_jump.png");
            pc.spriteFall = AssetDatabase.LoadAssetAtPath<Sprite>($"{ArtDir}/chico_fall.png");
            pc.spriteHurt = AssetDatabase.LoadAssetAtPath<Sprite>($"{ArtDir}/chico_hurt.png");
            pc.spriteClimb = AssetDatabase.LoadAssetAtPath<Sprite>($"{ArtDir}/chico_climb.png");
            pc.spriteCelebrate = AssetDatabase.LoadAssetAtPath<Sprite>($"{ArtDir}/chico_celebrate.png");

            // GroundCheck
            var gc = new GameObject("GroundCheck");
            gc.transform.SetParent(go.transform);
            gc.transform.localPosition = new Vector3(0f, 0.05f, 0f);
            pc.groundCheck = gc.transform;

            // FirePoint
            var fp = new GameObject("FirePoint");
            fp.transform.SetParent(go.transform);
            fp.transform.localPosition = new Vector3(0.6f, 0.7f, 0f);
            shooter.firePoint = fp.transform;

            // StompFoot (GDD Sprint 3.1)
            var sf = new GameObject("StompFoot");
            sf.transform.SetParent(go.transform);
            sf.transform.localPosition = new Vector3(0f, 0.1f, 0f);
            var sfCol = sf.AddComponent<BoxCollider2D>();
            sfCol.isTrigger = true;
            sfCol.size = new Vector2(0.7f, 0.25f);
            var footScript = sf.AddComponent<StompFoot>();
            footScript.player = pc;

            // Shield visual
            var shieldVis = new GameObject("ShieldBubble");
            shieldVis.transform.SetParent(go.transform);
            shieldVis.transform.localPosition = new Vector3(0f, 0.7f, 0f);
            var sSr = shieldVis.AddComponent<SpriteRenderer>();
            sSr.sortingOrder = 35;
            sSr.color = new Color(0.3f, 0.8f, 1f, 0.45f);
            var shieldItem = AssetDatabase.LoadAssetAtPath<Sprite>($"{ArtDir}/item_shield.png");
            if (shieldItem != null) sSr.sprite = shieldItem;
            shieldVis.AddComponent<GlowPulse>();
            shieldVis.SetActive(false);
            powers.shieldBubbleVisual = shieldVis;

            var prefab = PrefabUtility.SaveAsPrefabAsset(go, path);
            Object.DestroyImmediate(go);
            return prefab;
        }

        private static GameObject CreateSlimePrefab()
        {
            string path = $"{PrefabDir}/Enemy_SlimeCrawler.prefab";
            var go = new GameObject("Enemy_SlimeCrawler");
            go.tag = Layers.TagEnemy;
            go.layer = LayerMask.NameToLayer(Layers.Enemy);

            var sr = go.AddComponent<SpriteRenderer>();
            sr.sortingOrder = 25;
            sr.sprite = AssetDatabase.LoadAssetAtPath<Sprite>($"{ArtDir}/slime_crawl_0.png");

            var rb = go.AddComponent<Rigidbody2D>();
            rb.collisionDetectionMode = CollisionDetectionMode2D.Discrete;
            rb.freezeRotation = true;
            rb.gravityScale = 2.5f;

            var col = go.AddComponent<CapsuleCollider2D>();
            col.direction = CapsuleDirection2D.Horizontal;
            col.size = new Vector2(1.2f, 0.6f);
            col.offset = new Vector2(0f, 0.3f);

            var slime = go.AddComponent<SlimeCrawler>();
            slime.spriteRenderer = sr;
            slime.spriteMoveA = AssetDatabase.LoadAssetAtPath<Sprite>($"{ArtDir}/slime_crawl_0.png");
            slime.spriteMoveB = AssetDatabase.LoadAssetAtPath<Sprite>($"{ArtDir}/slime_crawl_1.png");
            slime.spriteSquished = AssetDatabase.LoadAssetAtPath<Sprite>($"{ArtDir}/slime_squished.png");

            // WeakPoint no topo (GDD Sprint 3.1)
            var wp = new GameObject("WeakPoint");
            wp.transform.SetParent(go.transform);
            wp.transform.localPosition = new Vector3(0f, 0.65f, 0f);
            wp.layer = LayerMask.NameToLayer(Layers.EnemyTrigger);
            var wpCol = wp.AddComponent<BoxCollider2D>();
            wpCol.isTrigger = true;
            wpCol.size = new Vector2(0.9f, 0.25f);
            var wpScript = wp.AddComponent<WeakPoint>();
            wpScript.parentEnemy = slime;

            var prefab = PrefabUtility.SaveAsPrefabAsset(go, path);
            Object.DestroyImmediate(go);
            return prefab;
        }

        private static GameObject CreateBossSpitPrefab()
        {
            string path = $"{PrefabDir}/Projectile_BossSpit.prefab";
            var go = new GameObject("Projectile_BossSpit");
            go.layer = LayerMask.NameToLayer(Layers.EnemyTrigger);

            var sr = go.AddComponent<SpriteRenderer>();
            sr.sortingOrder = 45;
            var spitSprite = AssetDatabase.LoadAssetAtPath<Sprite>($"{ArtDir}/boss_spit.png");
            if (spitSprite != null)
            {
                sr.sprite = spitSprite;
            }
            else
            {
                var tex = new Texture2D(24, 24, TextureFormat.RGBA32, false);
                for (int y = 0; y < 24; y++)
                {
                    for (int x = 0; x < 24; x++)
                    {
                        float dist = Vector2.Distance(new Vector2(x, y), new Vector2(11.5f, 11.5f));
                        float a = Mathf.Clamp01(1f - dist / 11f);
                        tex.SetPixel(x, y, new Color(0.2f, 1f, 0.3f, a));
                    }
                }
                tex.Apply();
                sr.sprite = Sprite.Create(tex, new Rect(0, 0, 24, 24), new Vector2(0.5f, 0.5f), 24);
            }

            var rb = go.AddComponent<Rigidbody2D>();
            rb.gravityScale = 1.2f;
            rb.collisionDetectionMode = CollisionDetectionMode2D.Continuous;

            var col = go.AddComponent<CircleCollider2D>();
            col.isTrigger = true;
            col.radius = 0.38f;

            var spit = go.AddComponent<BossSpitProjectile>();
            spit.speed = 11f;
            spit.damage = 1;

            var prefab = PrefabUtility.SaveAsPrefabAsset(go, path);
            Object.DestroyImmediate(go);
            return prefab;
        }

        private static GameObject CreateBossSwampPrefab(GameObject spitPrefab, GameObject shipPartPrefab)
        {
            string path = $"{PrefabDir}/Enemy_BossSwamp.prefab";
            var go = new GameObject("Enemy_BossSwamp");
            go.tag = Layers.TagEnemy;
            go.layer = LayerMask.NameToLayer(Layers.Enemy);

            var sr = go.AddComponent<SpriteRenderer>();
            sr.sortingOrder = 25;
            sr.sprite = AssetDatabase.LoadAssetAtPath<Sprite>($"{ArtDir}/boss_idle.png");

            var rb = go.AddComponent<Rigidbody2D>();
            rb.collisionDetectionMode = CollisionDetectionMode2D.Continuous;
            rb.freezeRotation = true;
            rb.gravityScale = 2.8f;

            // Colisor corporal do chefe (grande, 2.6 largura, 2.0 altura)
            var col = go.AddComponent<CapsuleCollider2D>();
            col.direction = CapsuleDirection2D.Horizontal;
            col.size = new Vector2(2.6f, 2.0f);
            col.offset = new Vector2(0f, 1.0f);

            var boss = go.AddComponent<BossSwamp>();
            boss.spriteRenderer = sr;
            boss.spriteIdle = AssetDatabase.LoadAssetAtPath<Sprite>($"{ArtDir}/boss_idle.png");
            boss.spriteAttack = AssetDatabase.LoadAssetAtPath<Sprite>($"{ArtDir}/boss_attack.png");
            boss.spriteHurt = AssetDatabase.LoadAssetAtPath<Sprite>($"{ArtDir}/boss_hurt.png");
            boss.maxHealth = 10;
            boss.currentHealth = 10;
            boss.contactDamage = 2; // GDD Seção 13: 2 de dano por colisão
            boss.spitProjectilePrefab = spitPrefab;
            boss.shipPartPrefab = shipPartPrefab;

            // Ponto de saída do cuspe
            var mouth = new GameObject("MouthPoint");
            mouth.transform.SetParent(go.transform);
            mouth.transform.localPosition = new Vector3(0.9f, 1.3f, 0f);
            boss.spitMouthPoint = mouth.transform;

            // WeakPoint no topo da cabeça (GDD Seção 13: 5 Pisadas para derrotar / 2 dano cada)
            var wp = new GameObject("WeakPoint");
            wp.transform.SetParent(go.transform);
            wp.transform.localPosition = new Vector3(0f, 2.1f, 0f);
            wp.layer = LayerMask.NameToLayer(Layers.EnemyTrigger);

            var wpCol = wp.AddComponent<BoxCollider2D>();
            wpCol.isTrigger = true;
            wpCol.size = new Vector2(1.8f, 0.4f);

            var wpScript = wp.AddComponent<WeakPoint>();
            wpScript.parentEnemy = boss;
            wpScript.stompDamage = 2;
            wpScript.bounceForce = 14.5f;

            boss.weakPoint = wpScript;

            var prefab = PrefabUtility.SaveAsPrefabAsset(go, path);
            Object.DestroyImmediate(go);
            return prefab;
        }

        private static GameObject CreateShipPartPrefab(ItemData thrusterItem)
        {
            string path = $"{PrefabDir}/Pickup_ShipPart.prefab";
            var go = new GameObject("Pickup_ShipPart");
            go.layer = LayerMask.NameToLayer(Layers.Pickup);

            var sr = go.AddComponent<SpriteRenderer>();
            sr.sortingOrder = 28;
            sr.sprite = AssetDatabase.LoadAssetAtPath<Sprite>($"{ArtDir}/item_thruster.png");

            var col = go.AddComponent<BoxCollider2D>();
            col.isTrigger = true;
            col.size = new Vector2(1.2f, 1.2f);

            var sp = go.AddComponent<ShipPart>();
            sp.itemData = thrusterItem;
            sp.visualObject = go;

            go.AddComponent<Bob>().amplitude = new Vector2(0f, 0.15f);

            var halo = new GameObject("HaloGlow");
            halo.transform.SetParent(go.transform);
            halo.transform.localPosition = Vector3.zero;
            var hSr = halo.AddComponent<SpriteRenderer>();
            hSr.sortingOrder = 27;
            hSr.sprite = AssetDatabase.LoadAssetAtPath<Sprite>($"{ArtDir}/item_thruster.png");
            hSr.color = new Color(0.2f, 0.9f, 1f, 0.4f);
            halo.transform.localScale = Vector3.one * 1.3f;
            halo.AddComponent<GlowPulse>();

            var prefab = PrefabUtility.SaveAsPrefabAsset(go, path);
            Object.DestroyImmediate(go);
            return prefab;
        }

        private static GameObject CreatePickupPrefab(string name, string spriteName, PickupType type, int val, ItemData powerData, Color color)
        {
            string path = $"{PrefabDir}/{name}.prefab";
            var go = new GameObject(name);
            go.layer = LayerMask.NameToLayer(Layers.Pickup);

            var sr = go.AddComponent<SpriteRenderer>();
            sr.sortingOrder = 28;
            sr.sprite = AssetDatabase.LoadAssetAtPath<Sprite>($"{ArtDir}/{spriteName}.png");

            var col = go.AddComponent<CircleCollider2D>();
            col.isTrigger = true;
            col.radius = 0.45f;

            var p = go.AddComponent<Pickup>();
            p.type = type;
            p.value = val;
            p.powerItemData = powerData;
            p.burstColor = color;

            go.AddComponent<Bob>().amplitude = new Vector2(0f, 0.12f);

            var prefab = PrefabUtility.SaveAsPrefabAsset(go, path);
            Object.DestroyImmediate(go);
            return prefab;
        }

        private static GameObject CreateVinePrefab()
        {
            string path = $"{PrefabDir}/Prop_Vine.prefab";
            var go = new GameObject("Prop_Vine");
            go.layer = LayerMask.NameToLayer(Layers.Pickup);

            var sr = go.AddComponent<SpriteRenderer>();
            sr.sortingOrder = 18;
            sr.sprite = AssetDatabase.LoadAssetAtPath<Sprite>($"{ArtDir}/prop_vine.png");
            sr.drawMode = SpriteDrawMode.Tiled;
            sr.size = new Vector2(0.8f, 6.2f);

            var col = go.AddComponent<BoxCollider2D>();
            col.isTrigger = true;
            col.size = new Vector2(0.8f, 6.2f);

            go.AddComponent<Vine>();

            var prefab = PrefabUtility.SaveAsPrefabAsset(go, path);
            Object.DestroyImmediate(go);
            return prefab;
        }

        private static GameObject CreateHazardWaterPrefab()
        {
            string path = $"{PrefabDir}/Hazard_ToxicWater.prefab";
            var go = new GameObject("Hazard_ToxicWater");
            go.layer = LayerMask.NameToLayer(Layers.Hazard);

            var sr = go.AddComponent<SpriteRenderer>();
            sr.sortingOrder = 22;

            // Textura verde tóxica brilhante
            var tex = new Texture2D(32, 32, TextureFormat.RGBA32, false);
            for (int y = 0; y < 32; y++)
            {
                for (int x = 0; x < 32; x++)
                {
                    tex.SetPixel(x, y, new Color(0.1f, 0.95f, 0.4f, 0.75f));
                }
            }
            tex.Apply();
            sr.sprite = Sprite.Create(tex, new Rect(0, 0, 32, 32), new Vector2(0.5f, 0.5f), 32);

            var col = go.AddComponent<BoxCollider2D>();
            col.isTrigger = true;
            col.size = new Vector2(1f, 1f);

            go.AddComponent<Hazard>();

            var prefab = PrefabUtility.SaveAsPrefabAsset(go, path);
            Object.DestroyImmediate(go);
            return prefab;
        }

        private static GameObject CreateHudCanvasPrefab()
        {
            string path = $"{PrefabDir}/HUD_Canvas.prefab";
            var go = new GameObject("HUD_Canvas");
            var canvas = go.AddComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            var cs = go.AddComponent<CanvasScaler>();
            cs.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            cs.referenceResolution = new Vector2(1920, 1080);
            go.AddComponent<GraphicRaycaster>();

            Font russoFont = AssetDatabase.LoadAssetAtPath<Font>("Assets/_SpaceChico/Resources/Fonts/RussoOne-Regular.ttf");
            Font nunitoFont = AssetDatabase.LoadAssetAtPath<Font>("Assets/_SpaceChico/Resources/Fonts/Nunito.ttf");

            // --- HUD CONTAINER ---
            var hudObj = new GameObject("HUD_Panel", typeof(RectTransform));
            hudObj.transform.SetParent(go.transform, false);
            var hud = hudObj.AddComponent<HUD>();

            // Hearts container (top-left)
            var heartsBox = new GameObject("HeartsContainer", typeof(RectTransform), typeof(HorizontalLayoutGroup));
            heartsBox.transform.SetParent(hudObj.transform, false);
            var rtHearts = heartsBox.GetComponent<RectTransform>();
            rtHearts.anchorMin = new Vector2(0, 1);
            rtHearts.anchorMax = new Vector2(0, 1);
            rtHearts.pivot = new Vector2(0, 1);
            rtHearts.anchoredPosition = new Vector2(30, -30);
            rtHearts.sizeDelta = new Vector2(250, 48);
            hud.heartsContainer = heartsBox.transform;

            // Oxygen Bar
            var o2Box = new GameObject("OxygenBar", typeof(RectTransform), typeof(Image));
            o2Box.transform.SetParent(hudObj.transform, false);
            var rtO2 = o2Box.GetComponent<RectTransform>();
            rtO2.anchorMin = new Vector2(0, 1);
            rtO2.anchorMax = new Vector2(0, 1);
            rtO2.pivot = new Vector2(0, 1);
            rtO2.anchoredPosition = new Vector2(30, -85);
            rtO2.sizeDelta = new Vector2(240, 22);
            o2Box.GetComponent<Image>().color = new Color(0.1f, 0.1f, 0.15f, 0.75f);

            var o2Fill = new GameObject("Fill", typeof(RectTransform), typeof(Image));
            o2Fill.transform.SetParent(o2Box.transform, false);
            var rtFill = o2Fill.GetComponent<RectTransform>();
            rtFill.anchorMin = Vector2.zero;
            rtFill.anchorMax = Vector2.one;
            rtFill.sizeDelta = Vector2.zero;
            var imgFill = o2Fill.GetComponent<Image>();
            imgFill.type = Image.Type.Filled;
            imgFill.fillMethod = Image.FillMethod.Horizontal;
            imgFill.color = new Color(0.2f, 0.8f, 1f);
            hud.oxygenFillImage = imgFill;

            var o2Txt = new GameObject("O2Text", typeof(RectTransform), typeof(Text));
            o2Txt.transform.SetParent(o2Box.transform, false);
            var rtO2Txt = o2Txt.GetComponent<RectTransform>();
            rtO2Txt.anchorMin = Vector2.zero;
            rtO2Txt.anchorMax = Vector2.one;
            rtO2Txt.sizeDelta = Vector2.zero;
            var txtO2 = o2Txt.GetComponent<Text>();
            txtO2.font = russoFont;
            txtO2.fontSize = 15;
            txtO2.alignment = TextAnchor.MiddleCenter;
            txtO2.color = Color.white;
            txtO2.text = "O₂ 100%";
            hud.oxygenText = txtO2;

            // Crystals Count
            var crysBox = new GameObject("Crystals", typeof(RectTransform), typeof(Text));
            crysBox.transform.SetParent(hudObj.transform, false);
            var rtCrys = crysBox.GetComponent<RectTransform>();
            rtCrys.anchorMin = new Vector2(0, 1);
            rtCrys.anchorMax = new Vector2(0, 1);
            rtCrys.pivot = new Vector2(0, 1);
            rtCrys.anchoredPosition = new Vector2(30, -118);
            rtCrys.sizeDelta = new Vector2(200, 30);
            var txtCrys = crysBox.GetComponent<Text>();
            txtCrys.font = russoFont;
            txtCrys.fontSize = 20;
            txtCrys.color = new Color(0.8f, 0.5f, 1f);
            txtCrys.text = "◆ 0";
            hud.crystalsText = txtCrys;

            // Power Icon (top-right)
            var pwrBox = new GameObject("PowerStatus", typeof(RectTransform), typeof(Image));
            pwrBox.transform.SetParent(hudObj.transform, false);
            var rtPwr = pwrBox.GetComponent<RectTransform>();
            rtPwr.anchorMin = new Vector2(1, 1);
            rtPwr.anchorMax = new Vector2(1, 1);
            rtPwr.pivot = new Vector2(1, 1);
            rtPwr.anchoredPosition = new Vector2(-30, -30);
            rtPwr.sizeDelta = new Vector2(64, 64);
            pwrBox.GetComponent<Image>().color = new Color(0.12f, 0.12f, 0.2f, 0.85f);

            var pwrIcon = new GameObject("Icon", typeof(RectTransform), typeof(Image));
            pwrIcon.transform.SetParent(pwrBox.transform, false);
            var rtIcon = pwrIcon.GetComponent<RectTransform>();
            rtIcon.sizeDelta = new Vector2(50, 50);
            hud.powerIcon = pwrIcon.GetComponent<Image>();

            var pwrLabel = new GameObject("Label", typeof(RectTransform), typeof(Text));
            pwrLabel.transform.SetParent(pwrBox.transform, false);
            var rtLbl = pwrLabel.GetComponent<RectTransform>();
            rtLbl.anchoredPosition = new Vector2(0, -50);
            rtLbl.sizeDelta = new Vector2(140, 40);
            var txtLbl = pwrLabel.GetComponent<Text>();
            txtLbl.font = nunitoFont;
            txtLbl.fontSize = 14;
            txtLbl.alignment = TextAnchor.UpperCenter;
            txtLbl.color = Color.white;
            txtLbl.text = "Poder [F]";
            hud.powerLabel = txtLbl;

            // Control Guide (bottom-right)
            var controls = new GameObject("ControlsGuide", typeof(RectTransform), typeof(Text));
            controls.transform.SetParent(hudObj.transform, false);
            var rtCtl = controls.GetComponent<RectTransform>();
            rtCtl.anchorMin = new Vector2(1, 0);
            rtCtl.anchorMax = new Vector2(1, 0);
            rtCtl.pivot = new Vector2(1, 0);
            rtCtl.anchoredPosition = new Vector2(-25, 20);
            rtCtl.sizeDelta = new Vector2(350, 90);
            var txtCtl = controls.GetComponent<Text>();
            txtCtl.font = nunitoFont;
            txtCtl.fontSize = 15;
            txtCtl.alignment = TextAnchor.LowerRight;
            txtCtl.color = new Color(1f, 1f, 1f, 0.65f);
            txtCtl.text = "Setas: Mover / Escalar Vinhas\nEspaço: Pular / Esmagar\n[E] Atirar  |  [F] Poder  |  [B] Mochila";

            // --- BOSS HEALTH BAR (Top-Center) ---
            var bossBarObj = new GameObject("BossHealthBar", typeof(RectTransform), typeof(CanvasGroup));
            bossBarObj.transform.SetParent(hudObj.transform, false);
            var rtBoss = bossBarObj.GetComponent<RectTransform>();
            rtBoss.anchorMin = new Vector2(0.5f, 1f);
            rtBoss.anchorMax = new Vector2(0.5f, 1f);
            rtBoss.pivot = new Vector2(0.5f, 1f);
            rtBoss.anchoredPosition = new Vector2(0, -25);
            rtBoss.sizeDelta = new Vector2(620, 80);

            var bossUI = bossBarObj.AddComponent<BossHealthBarUI>();
            bossUI.canvasGroup = bossBarObj.GetComponent<CanvasGroup>();

            // Nome do Chefe
            var bossNameObj = new GameObject("BossName", typeof(RectTransform), typeof(Text));
            bossNameObj.transform.SetParent(bossBarObj.transform, false);
            var rtBossName = bossNameObj.GetComponent<RectTransform>();
            rtBossName.anchorMin = new Vector2(0.5f, 1f);
            rtBossName.anchorMax = new Vector2(0.5f, 1f);
            rtBossName.pivot = new Vector2(0.5f, 1f);
            rtBossName.anchoredPosition = new Vector2(0, 0);
            rtBossName.sizeDelta = new Vector2(500, 28);
            var txtBossName = bossNameObj.GetComponent<Text>();
            txtBossName.font = russoFont;
            txtBossName.fontSize = 20;
            txtBossName.alignment = TextAnchor.MiddleCenter;
            txtBossName.color = new Color(0.9f, 0.4f, 1f);
            txtBossName.text = "GUARDIÃO DO PÂNTANO";
            bossUI.bossNameText = txtBossName;

            // Subtítulo de Fase
            var phaseObj = new GameObject("PhaseSubtitle", typeof(RectTransform), typeof(Text));
            phaseObj.transform.SetParent(bossBarObj.transform, false);
            var rtPhase = phaseObj.GetComponent<RectTransform>();
            rtPhase.anchorMin = new Vector2(0.5f, 1f);
            rtPhase.anchorMax = new Vector2(0.5f, 1f);
            rtPhase.pivot = new Vector2(0.5f, 1f);
            rtPhase.anchoredPosition = new Vector2(0, -26);
            rtPhase.sizeDelta = new Vector2(400, 20);
            var txtPhase = phaseObj.GetComponent<Text>();
            txtPhase.font = nunitoFont;
            txtPhase.fontSize = 13;
            txtPhase.alignment = TextAnchor.MiddleCenter;
            txtPhase.color = new Color(0.8f, 1f, 0.8f, 0.85f);
            txtPhase.text = "Bioluminescent Colossus • Fase 1";
            bossUI.phaseText = txtPhase;

            // Fundo da Barra
            var barBgObj = new GameObject("BarBackground", typeof(RectTransform), typeof(Image));
            barBgObj.transform.SetParent(bossBarObj.transform, false);
            var rtBarBg = barBgObj.GetComponent<RectTransform>();
            rtBarBg.anchorMin = new Vector2(0.5f, 0f);
            rtBarBg.anchorMax = new Vector2(0.5f, 0f);
            rtBarBg.pivot = new Vector2(0.5f, 0f);
            rtBarBg.anchoredPosition = new Vector2(0, 6);
            rtBarBg.sizeDelta = new Vector2(580, 22);
            barBgObj.GetComponent<Image>().color = new Color(0.08f, 0.08f, 0.12f, 0.9f);

            // Preenchimento da Barra
            var barFillObj = new GameObject("BarFill", typeof(RectTransform), typeof(Image));
            barFillObj.transform.SetParent(barBgObj.transform, false);
            var rtBarFill = barFillObj.GetComponent<RectTransform>();
            rtBarFill.anchorMin = Vector2.zero;
            rtBarFill.anchorMax = Vector2.one;
            rtBarFill.sizeDelta = Vector2.zero;
            var imgBarFill = barFillObj.GetComponent<Image>();
            imgBarFill.type = Image.Type.Filled;
            imgBarFill.fillMethod = Image.FillMethod.Horizontal;
            imgBarFill.color = new Color(0.2f, 0.9f, 0.4f);
            bossUI.fillImage = imgBarFill;

            // Texto de HP numérico
            var hpTextObj = new GameObject("HPText", typeof(RectTransform), typeof(Text));
            hpTextObj.transform.SetParent(barBgObj.transform, false);
            var rtHpText = hpTextObj.GetComponent<RectTransform>();
            rtHpText.anchorMin = Vector2.zero;
            rtHpText.anchorMax = Vector2.one;
            rtHpText.sizeDelta = Vector2.zero;
            var txtHp = hpTextObj.GetComponent<Text>();
            txtHp.font = russoFont;
            txtHp.fontSize = 14;
            txtHp.alignment = TextAnchor.MiddleCenter;
            txtHp.color = Color.white;
            txtHp.text = "10 / 10 HP";
            bossUI.hpText = txtHp;

            // --- BACKPACK UI PANEL ---
            var bpObj = new GameObject("BackpackPanel", typeof(RectTransform), typeof(Image));
            bpObj.transform.SetParent(go.transform, false);
            var rtBp = bpObj.GetComponent<RectTransform>();
            rtBp.anchorMin = Vector2.zero;
            rtBp.anchorMax = Vector2.one;
            rtBp.sizeDelta = Vector2.zero;
            bpObj.GetComponent<Image>().color = new Color(0.04f, 0.04f, 0.08f, 0.92f);

            var bp = bpObj.AddComponent<BackpackUI>();
            bp.panelRoot = bpObj;

            var bpTitle = new GameObject("Title", typeof(RectTransform), typeof(Text));
            bpTitle.transform.SetParent(bpObj.transform, false);
            var rtBpT = bpTitle.GetComponent<RectTransform>();
            rtBpT.anchorMin = new Vector2(0.5f, 1f);
            rtBpT.anchorMax = new Vector2(0.5f, 1f);
            rtBpT.pivot = new Vector2(0.5f, 1f);
            rtBpT.anchoredPosition = new Vector2(0, -60);
            rtBpT.sizeDelta = new Vector2(600, 60);
            var txtBpT = bpTitle.GetComponent<Text>();
            txtBpT.font = russoFont;
            txtBpT.fontSize = 38;
            txtBpT.alignment = TextAnchor.MiddleCenter;
            txtBpT.color = new Color(0.3f, 0.9f, 1f);
            txtBpT.text = "MOCHILA DE SOBREVIVÊNCIA [B]";

            // Slots Grid
            var gridObj = new GameObject("SlotsGrid", typeof(RectTransform), typeof(GridLayoutGroup));
            gridObj.transform.SetParent(bpObj.transform, false);
            var rtGrid = gridObj.GetComponent<RectTransform>();
            rtGrid.anchorMin = new Vector2(0.1f, 0.2f);
            rtGrid.anchorMax = new Vector2(0.5f, 0.75f);
            rtGrid.sizeDelta = Vector2.zero;
            var glg = gridObj.GetComponent<GridLayoutGroup>();
            glg.cellSize = new Vector2(90, 90);
            glg.spacing = new Vector2(16, 16);
            bp.slotsContainer = gridObj.transform;

            // Details panel
            var detObj = new GameObject("DetailsPanel", typeof(RectTransform), typeof(Image));
            detObj.transform.SetParent(bpObj.transform, false);
            var rtDet = detObj.GetComponent<RectTransform>();
            rtDet.anchorMin = new Vector2(0.55f, 0.2f);
            rtDet.anchorMax = new Vector2(0.9f, 0.75f);
            rtDet.sizeDelta = Vector2.zero;
            detObj.GetComponent<Image>().color = new Color(0.1f, 0.12f, 0.18f, 0.85f);

            var detIcon = new GameObject("Icon", typeof(RectTransform), typeof(Image));
            detIcon.transform.SetParent(detObj.transform, false);
            var rtDI = detIcon.GetComponent<RectTransform>();
            rtDI.anchoredPosition = new Vector2(0, 110);
            rtDI.sizeDelta = new Vector2(96, 96);
            bp.detailIcon = detIcon.GetComponent<Image>();

            var detName = new GameObject("Name", typeof(RectTransform), typeof(Text));
            detName.transform.SetParent(detObj.transform, false);
            var rtDN = detName.GetComponent<RectTransform>();
            rtDN.anchoredPosition = new Vector2(0, 35);
            rtDN.sizeDelta = new Vector2(340, 40);
            var txtDN = detName.GetComponent<Text>();
            txtDN.font = russoFont;
            txtDN.fontSize = 22;
            txtDN.alignment = TextAnchor.MiddleCenter;
            txtDN.color = Color.white;
            bp.detailName = txtDN;

            var detCat = new GameObject("Category", typeof(RectTransform), typeof(Text));
            detCat.transform.SetParent(detObj.transform, false);
            var rtDC = detCat.GetComponent<RectTransform>();
            rtDC.anchoredPosition = new Vector2(0, 5);
            rtDC.sizeDelta = new Vector2(340, 30);
            var txtDC = detCat.GetComponent<Text>();
            txtDC.font = nunitoFont;
            txtDC.fontSize = 16;
            txtDC.alignment = TextAnchor.MiddleCenter;
            txtDC.color = new Color(0.3f, 0.9f, 1f);
            bp.detailCategory = txtDC;

            var detDesc = new GameObject("Desc", typeof(RectTransform), typeof(Text));
            detDesc.transform.SetParent(detObj.transform, false);
            var rtDD = detDesc.GetComponent<RectTransform>();
            rtDD.anchoredPosition = new Vector2(0, -60);
            rtDD.sizeDelta = new Vector2(320, 90);
            var txtDD = detDesc.GetComponent<Text>();
            txtDD.font = nunitoFont;
            txtDD.fontSize = 16;
            txtDD.alignment = TextAnchor.UpperCenter;
            txtDD.color = new Color(0.85f, 0.85f, 0.9f);
            bp.detailDescription = txtDD;

            var detCloseBtn = new GameObject("CloseButton", typeof(RectTransform), typeof(Image), typeof(Button));
            detCloseBtn.transform.SetParent(bpObj.transform, false);
            var rtCls = detCloseBtn.GetComponent<RectTransform>();
            rtCls.anchoredPosition = new Vector2(0, -420);
            rtCls.sizeDelta = new Vector2(220, 50);
            detCloseBtn.GetComponent<Image>().color = new Color(0.2f, 0.6f, 0.9f);
            bp.closeButton = detCloseBtn.GetComponent<Button>();

            var txtCls = new GameObject("Text", typeof(RectTransform), typeof(Text));
            txtCls.transform.SetParent(detCloseBtn.transform, false);
            var rtTxtCls = txtCls.GetComponent<RectTransform>();
            rtTxtCls.sizeDelta = new Vector2(220, 50);
            var tClose = txtCls.GetComponent<Text>();
            tClose.font = russoFont;
            tClose.fontSize = 18;
            tClose.alignment = TextAnchor.MiddleCenter;
            tClose.color = Color.white;
            tClose.text = "FECHAR [B]";

            bpObj.SetActive(false);

            // --- PAUSE MENU UI ---
            var pauseObj = new GameObject("PausePanel", typeof(RectTransform), typeof(Image));
            pauseObj.transform.SetParent(go.transform, false);
            var rtP = pauseObj.GetComponent<RectTransform>();
            rtP.anchorMin = Vector2.zero;
            rtP.anchorMax = Vector2.one;
            rtP.sizeDelta = Vector2.zero;
            pauseObj.GetComponent<Image>().color = new Color(0.02f, 0.02f, 0.06f, 0.88f);

            var pm = pauseObj.AddComponent<PauseMenuUI>();
            pm.panelRoot = pauseObj;

            var pTitle = new GameObject("Title", typeof(RectTransform), typeof(Text));
            pTitle.transform.SetParent(pauseObj.transform, false);
            var rtPT = pTitle.GetComponent<RectTransform>();
            rtPT.anchoredPosition = new Vector2(0, 150);
            rtPT.sizeDelta = new Vector2(400, 60);
            var txtPT = pTitle.GetComponent<Text>();
            txtPT.font = russoFont;
            txtPT.fontSize = 42;
            txtPT.alignment = TextAnchor.MiddleCenter;
            txtPT.color = Color.white;
            txtPT.text = "JOGO PAUSADO";

            pm.resumeButton = CreateSimpleButton(pauseObj.transform, new Vector2(0, 40), "CONTINUAR", russoFont);
            pm.restartButton = CreateSimpleButton(pauseObj.transform, new Vector2(0, -30), "REINICIAR FASE", russoFont);
            pm.quitButton = CreateSimpleButton(pauseObj.transform, new Vector2(0, -100), "DESISTIR (MENU)", russoFont);

            pauseObj.SetActive(false);

            // --- GAME OVER UI ---
            var overObj = new GameObject("GameOverPanel", typeof(RectTransform), typeof(Image));
            overObj.transform.SetParent(go.transform, false);
            var rtGo = overObj.GetComponent<RectTransform>();
            rtGo.anchorMin = Vector2.zero;
            rtGo.anchorMax = Vector2.one;
            rtGo.sizeDelta = Vector2.zero;
            overObj.GetComponent<Image>().color = new Color(0.12f, 0.02f, 0.04f, 0.95f);

            var gom = overObj.AddComponent<GameOverUI>();
            gom.panelRoot = overObj;

            var goTitle = new GameObject("Title", typeof(RectTransform), typeof(Text));
            goTitle.transform.SetParent(overObj.transform, false);
            var rtGoT = goTitle.GetComponent<RectTransform>();
            rtGoT.anchoredPosition = new Vector2(0, 130);
            rtGoT.sizeDelta = new Vector2(500, 70);
            var txtGoT = goTitle.GetComponent<Text>();
            txtGoT.font = russoFont;
            txtGoT.fontSize = 54;
            txtGoT.alignment = TextAnchor.MiddleCenter;
            txtGoT.color = new Color(1f, 0.25f, 0.35f);
            txtGoT.text = "GAME OVER";

            var goReason = new GameObject("Reason", typeof(RectTransform), typeof(Text));
            goReason.transform.SetParent(overObj.transform, false);
            var rtGoR = goReason.GetComponent<RectTransform>();
            rtGoR.anchoredPosition = new Vector2(0, 40);
            rtGoR.sizeDelta = new Vector2(600, 50);
            var txtGoR = goReason.GetComponent<Text>();
            txtGoR.font = nunitoFont;
            txtGoR.fontSize = 20;
            txtGoR.alignment = TextAnchor.MiddleCenter;
            txtGoR.color = Color.white;
            gom.reasonText = txtGoR;

            gom.restartButton = CreateSimpleButton(overObj.transform, new Vector2(0, -40), "TENTAR NOVAMENTE", russoFont);
            gom.quitButton = CreateSimpleButton(overObj.transform, new Vector2(0, -110), "MENU PRINCIPAL", russoFont);

            overObj.SetActive(false);

            // --- LEVEL COMPLETE UI ---
            var winObj = new GameObject("LevelCompletePanel", typeof(RectTransform), typeof(Image));
            winObj.transform.SetParent(go.transform, false);
            var rtW = winObj.GetComponent<RectTransform>();
            rtW.anchorMin = Vector2.zero;
            rtW.anchorMax = Vector2.one;
            rtW.sizeDelta = Vector2.zero;
            winObj.GetComponent<Image>().color = new Color(0.02f, 0.08f, 0.06f, 0.95f);

            var lcm = winObj.AddComponent<LevelCompleteUI>();
            lcm.panelRoot = winObj;

            var winTitle = new GameObject("Title", typeof(RectTransform), typeof(Text));
            winTitle.transform.SetParent(winObj.transform, false);
            var rtWT = winTitle.GetComponent<RectTransform>();
            rtWT.anchoredPosition = new Vector2(0, 160);
            rtWT.sizeDelta = new Vector2(700, 70);
            var txtWT = winTitle.GetComponent<Text>();
            txtWT.font = russoFont;
            txtWT.fontSize = 46;
            txtWT.alignment = TextAnchor.MiddleCenter;
            txtWT.color = new Color(0.3f, 1f, 0.6f);
            lcm.titleText = txtWT;

            var winPart = new GameObject("Part", typeof(RectTransform), typeof(Text));
            winPart.transform.SetParent(winObj.transform, false);
            var rtWP = winPart.GetComponent<RectTransform>();
            rtWP.anchoredPosition = new Vector2(0, 95);
            rtWP.sizeDelta = new Vector2(700, 40);
            var txtWP = winPart.GetComponent<Text>();
            txtWP.font = russoFont;
            txtWP.fontSize = 24;
            txtWP.alignment = TextAnchor.MiddleCenter;
            txtWP.color = new Color(0.3f, 0.9f, 1f);
            lcm.partRecoveredText = txtWP;

            var winStats = new GameObject("Stats", typeof(RectTransform), typeof(Text));
            winStats.transform.SetParent(winObj.transform, false);
            var rtWS = winStats.GetComponent<RectTransform>();
            rtWS.anchoredPosition = new Vector2(0, 20);
            rtWS.sizeDelta = new Vector2(600, 60);
            var txtWS = winStats.GetComponent<Text>();
            txtWS.font = nunitoFont;
            txtWS.fontSize = 20;
            txtWS.alignment = TextAnchor.MiddleCenter;
            txtWS.color = Color.white;
            lcm.statsSummaryText = txtWS;

            var winBonus = new GameObject("Bonus", typeof(RectTransform), typeof(Text));
            winBonus.transform.SetParent(winObj.transform, false);
            var rtWB = winBonus.GetComponent<RectTransform>();
            rtWB.anchoredPosition = new Vector2(0, -45);
            rtWB.sizeDelta = new Vector2(600, 40);
            var txtWB = winBonus.GetComponent<Text>();
            txtWB.font = russoFont;
            txtWB.fontSize = 20;
            txtWB.alignment = TextAnchor.MiddleCenter;
            txtWB.color = new Color(1f, 0.85f, 0.3f);
            lcm.bonusLifeText = txtWB;

            lcm.continueButton = CreateSimpleButton(winObj.transform, new Vector2(0, -130), "CONTINUAR JORNADA", russoFont);

            winObj.SetActive(false);

            var prefab = PrefabUtility.SaveAsPrefabAsset(go, path);
            Object.DestroyImmediate(go);
            return prefab;
        }

        private static Button CreateSimpleButton(Transform parent, Vector2 pos, string text, Font font)
        {
            var btnObj = new GameObject("Btn_" + text.Replace(" ", ""), typeof(RectTransform), typeof(Image), typeof(Button));
            btnObj.transform.SetParent(parent, false);
            var rt = btnObj.GetComponent<RectTransform>();
            rt.anchoredPosition = pos;
            rt.sizeDelta = new Vector2(260, 48);
            btnObj.GetComponent<Image>().color = new Color(0.2f, 0.35f, 0.6f);

            var txtObj = new GameObject("Text", typeof(RectTransform), typeof(Text));
            txtObj.transform.SetParent(btnObj.transform, false);
            var rtT = txtObj.GetComponent<RectTransform>();
            rtT.sizeDelta = new Vector2(260, 48);
            var t = txtObj.GetComponent<Text>();
            t.font = font;
            t.fontSize = 18;
            t.alignment = TextAnchor.MiddleCenter;
            t.color = Color.white;
            t.text = text;

            return btnObj.GetComponent<Button>();
        }

        // ====================================================================
        //  4. MONTAGEM DAS CENAS
        // ====================================================================
        [MenuItem("Tools/Space Chico/3. Build Scene 00_Intro")]
        public static void BuildScene00_Intro()
        {
            var scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);

            // Câmera
            var camObj = new GameObject("Main Camera", typeof(Camera), typeof(AudioListener));
            camObj.transform.position = new Vector3(0f, 0f, -10f);
            var cam = camObj.GetComponent<Camera>();
            cam.orthographic = true;
            cam.backgroundColor = new Color(0.04f, 0.02f, 0.08f);
            cam.clearFlags = CameraClearFlags.SolidColor;

            // Audio Manager
            new GameObject("AudioManager", typeof(AudioManager));

            // Fundo espacial
            var bgObj = new GameObject("Background_Far", typeof(SpriteRenderer));
            var srBg = bgObj.GetComponent<SpriteRenderer>();
            srBg.sprite = AssetDatabase.LoadAssetAtPath<Sprite>($"{ArtDir}/bg_swamp_far.jpg");
            srBg.sortingOrder = -50;
            bgObj.transform.position = new Vector3(0f, 0f, 5f);
            bgObj.transform.localScale = Vector3.one * 1.5f;

            // Canvas de Intro Crawl
            var canvasObj = new GameObject("Canvas_Intro", typeof(Canvas), typeof(CanvasScaler), typeof(GraphicRaycaster));
            var canvas = canvasObj.GetComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            var cs = canvasObj.GetComponent<CanvasScaler>();
            cs.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            cs.referenceResolution = new Vector2(1920, 1080);

            var intro = canvasObj.AddComponent<IntroCrawl>();

            Font russoFont = AssetDatabase.LoadAssetAtPath<Font>("Assets/_SpaceChico/Resources/Fonts/RussoOne-Regular.ttf");
            Font nunitoFont = AssetDatabase.LoadAssetAtPath<Font>("Assets/_SpaceChico/Resources/Fonts/Nunito.ttf");

            // Texto Crawl que sobe
            var crawlObj = new GameObject("CrawlText", typeof(RectTransform), typeof(Text));
            crawlObj.transform.SetParent(canvasObj.transform, false);
            var rt = crawlObj.GetComponent<RectTransform>();
            rt.anchorMin = new Vector2(0.5f, 0.5f);
            rt.anchorMax = new Vector2(0.5f, 0.5f);
            rt.pivot = new Vector2(0.5f, 1f);
            rt.anchoredPosition = new Vector2(0, -540);
            rt.sizeDelta = new Vector2(900, 1200);

            var text = crawlObj.GetComponent<Text>();
            text.font = nunitoFont;
            text.fontSize = 28;
            text.lineSpacing = 1.35f;
            text.alignment = TextAnchor.UpperCenter;
            text.color = new Color(1f, 0.9f, 0.4f);
            text.text =
                "SPACE CHICO\n\n" +
                "— Capítulo I: O Pântano Bioluminescente —\n\n\n" +
                "Uma falha catastrófica no motor de dobra atirou a nave\n" +
                "do destemido felino interestelar CHICO diretamente em um\n" +
                "planeta selvagem e inexplorado.\n\n" +
                "Com o impacto brutal, os componentes essenciais de sua\n" +
                "espaçonave foram dispersados por biomas hostis.\n\n" +
                "Desperto entre musgos fluorescentes e vapores tóxicos,\n" +
                "Chico precisa empunhar seu blaster laser, enfrentar as\n" +
                "ameaças rastejantes e recuperar o PROPULSOR QUÂNTICO\n" +
                "para restaurar sua nave e retomar a jornada galáctica!\n\n\n" +
                "Boa sorte, Space Chico.";

            intro.crawlTransform = rt;

            // Prompt pular
            var skipObj = new GameObject("PromptSkip", typeof(RectTransform), typeof(Text));
            skipObj.transform.SetParent(canvasObj.transform, false);
            var rtSkip = skipObj.GetComponent<RectTransform>();
            rtSkip.anchorMin = new Vector2(0.5f, 0f);
            rtSkip.anchorMax = new Vector2(0.5f, 0f);
            rtSkip.pivot = new Vector2(0.5f, 0f);
            rtSkip.anchoredPosition = new Vector2(0, 40);
            rtSkip.sizeDelta = new Vector2(600, 40);

            var txtSkip = skipObj.GetComponent<Text>();
            txtSkip.font = russoFont;
            txtSkip.fontSize = 20;
            txtSkip.alignment = TextAnchor.MiddleCenter;
            txtSkip.color = Color.white;
            txtSkip.text = "Pressione [ESPAÇO] ou [ENTER] para Iniciar";
            intro.promptText = txtSkip;

            // EventSystem
            new GameObject("EventSystem", typeof(UnityEngine.EventSystems.EventSystem), typeof(UnityEngine.EventSystems.StandaloneInputModule));

            EditorSceneManager.SaveScene(scene, $"{SceneDir}/00_Intro.unity");
            Debug.Log("[Space Chico] Cena '00_Intro' montada e salva.");
        }

        [MenuItem("Tools/Space Chico/4. Build Scene 01_Pantano")]
        public static void BuildScene01_Pantano()
        {
            var items = CreateItemScriptableObjects();
            var prefabs = CreateAllPrefabs(items);
            BuildScene01_Pantano(prefabs, items);
        }

        private static void BuildScene01_Pantano(PrefabMap prefabs, ItemMap items)
        {
            var scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);

            // Câmera
            var camObj = new GameObject("Main Camera", typeof(Camera), typeof(AudioListener));
            camObj.tag = "MainCamera";
            camObj.transform.position = new Vector3(0f, 1.8f, -10f);
            var cam = camObj.GetComponent<Camera>();
            cam.orthographic = true;
            cam.orthographicSize = 6.0f;
            cam.backgroundColor = new Color(0.05f, 0.03f, 0.09f);
            cam.clearFlags = CameraClearFlags.SolidColor;
            var follow = camObj.AddComponent<CameraFollow>();

            // AudioManager
            new GameObject("AudioManager", typeof(AudioManager));

            // Fundo com Parallax em Loop Infinito (3 painéis que formam uma esteira contínua)
            var bgParent = new GameObject("--- BACKGROUND PARALLAX ---");
            bgParent.transform.position = new Vector3(0f, 4.5f, 5f);
            var plxLoop = bgParent.AddComponent<ParallaxLoop>();
            plxLoop.cam = camObj.transform;
            plxLoop.parallaxFactor = 0.65f;

            Sprite bgSprite = AssetDatabase.LoadAssetAtPath<Sprite>($"{ArtDir}/bg_swamp_far.jpg");
            float bgWidth = (bgSprite != null ? bgSprite.rect.width / bgSprite.pixelsPerUnit : 13.76f) * 1.6f;

            var bgPanels = new SpriteRenderer[3];
            for (int i = 0; i < 3; i++)
            {
                var bgObj = new GameObject($"Background_Panel_{i}", typeof(SpriteRenderer));
                bgObj.transform.SetParent(bgParent.transform);
                bgObj.transform.localScale = Vector3.one * 1.6f;
                // Posiciona os 3 painéis lado a lado: -largura, 0, +largura
                bgObj.transform.position = new Vector3((i - 1) * bgWidth, 4.5f, 5f);

                var srBg = bgObj.GetComponent<SpriteRenderer>();
                srBg.sprite = bgSprite;
                srBg.sortingOrder = -50;
                bgPanels[i] = srBg;
            }
            plxLoop.panels = bgPanels;

            // Esporos no ar (Partículas)
            var spores = new GameObject("AmbientSpores", typeof(ParticleSystem), typeof(AmbientParticles));
            spores.transform.position = new Vector3(40f, 5f, 0f);

            // Level Root
            var levelRoot = new GameObject("--- LEVEL GEOMETRY ---");

            // Plataformas e chão
            Sprite groundSprite = AssetDatabase.LoadAssetAtPath<Sprite>($"{ArtDir}/ground_block.jpg");

            // Chão inicial (X: -8 a 22)
            CreateGroundSection(levelRoot.transform, -8f, 22f, -1f, groundSprite);

            // Poça tóxica 1 (X: 22 a 28)
            CreateHazardPool(levelRoot.transform, 22f, 28f, -1.2f, prefabs.hazardWater);

            // Plataforma flutuante sobre poça 1 (X: 23.5 a 26.5, Y: 1.5)
            CreateFloatingPlatform(levelRoot.transform, 23.5f, 26.5f, 1.5f, groundSprite);

            // Chão intermediário (X: 28 a 52)
            CreateGroundSection(levelRoot.transform, 28f, 52f, -1f, groundSprite);

            // Vinha 1 (X: 42, Y: 0 a 6.2 - alcança com folga o topo da plataforma em Y=5.5)
            var v1 = (GameObject)PrefabUtility.InstantiatePrefab(prefabs.vine, levelRoot.transform);
            v1.transform.position = new Vector3(42f, 3.1f, 0f);

            // Plataforma alta 1 com Cristal e Pulo Duplo (X: 38 a 46, Y: 5.5)
            CreateFloatingPlatform(levelRoot.transform, 38f, 46f, 5.5f, groundSprite);
            SpawnPickup(prefabs.crystal, new Vector3(39.5f, 6.5f, 0f));
            SpawnPickup(prefabs.powerDoubleJump, new Vector3(44.5f, 6.5f, 0f));

            // Poça tóxica 2 maior (X: 52 a 66)
            CreateHazardPool(levelRoot.transform, 52f, 66f, -1.2f, prefabs.hazardWater);

            // Plataformas de salto estilo Mario (X: 54, 58, 62, Y: 1.2, 2.4, 1.8)
            CreateFloatingPlatform(levelRoot.transform, 54f, 56.5f, 1.4f, groundSprite);
            CreateFloatingPlatform(levelRoot.transform, 58f, 60.5f, 2.8f, groundSprite);
            CreateFloatingPlatform(levelRoot.transform, 62f, 64.5f, 1.6f, groundSprite);
            SpawnPickup(prefabs.crystal, new Vector3(55.2f, 2.4f, 0f));
            SpawnPickup(prefabs.oxygen, new Vector3(59.2f, 3.8f, 0f));
            SpawnPickup(prefabs.crystal, new Vector3(63.2f, 2.6f, 0f));

            // Chão do Pântano e Arena do Chefe (X: 66 a 126)
            CreateGroundSection(levelRoot.transform, 66f, 126f, -1f, groundSprite);

            // Parede direita limite da Arena (X: 123 a 127, Y: 10 descendo até -4)
            CreateGroundSection(levelRoot.transform, 123f, 127f, 10f, groundSprite, 14f);

            // Vinha 2 (X: 74, Y: 0 a 7.7 - alcança com folga o topo da plataforma em Y=6.8)
            var v2 = (GameObject)PrefabUtility.InstantiatePrefab(prefabs.vine, levelRoot.transform);
            v2.transform.position = new Vector3(74f, 3.8f, 0f);
            var v2Col = v2.GetComponent<BoxCollider2D>();
            if (v2Col != null) v2Col.size = new Vector2(0.8f, 7.8f);
            var v2Sr = v2.GetComponent<SpriteRenderer>();
            if (v2Sr != null) { v2Sr.drawMode = SpriteDrawMode.Tiled; v2Sr.size = new Vector2(0.8f, 7.8f); }

            // Plataforma secreta alta com Fruto de Neon e Escudo (X: 72 a 80, Y: 6.8)
            CreateFloatingPlatform(levelRoot.transform, 72f, 80f, 6.8f, groundSprite);
            SpawnPickup(prefabs.fruit, new Vector3(73.5f, 7.8f, 0f));
            SpawnPickup(prefabs.powerShield, new Vector3(78.5f, 7.8f, 0f));

            // Inimigo 4 guardando a entrada da arena (X: 77, patrulha entre 72 e 82)
            SpawnSlime(prefabs.slime, 77f, -0.1f, 72f, 83f);

            // --- ARENA DO CHEFE DO PÂNTANO (X: 86 a 125) ---

            // Barreira de espinhos que fecha a arena quando a batalha começa (X: 86.5)
            var barrierGo = new GameObject("ArenaBarrier", typeof(BoxCollider2D), typeof(SpriteRenderer));
            barrierGo.transform.SetParent(levelRoot.transform);
            barrierGo.transform.position = new Vector3(86.5f, 3.5f, 0f);
            barrierGo.layer = LayerMask.NameToLayer(Layers.Ground);
            barrierGo.tag = Layers.TagGround;
            var barCol = barrierGo.GetComponent<BoxCollider2D>();
            barCol.size = new Vector2(1.2f, 9.0f);
            var barSr = barrierGo.GetComponent<SpriteRenderer>();
            barSr.sprite = AssetDatabase.LoadAssetAtPath<Sprite>($"{ArtDir}/prop_vine.png");
            barSr.drawMode = SpriteDrawMode.Tiled;
            barSr.size = new Vector2(1.4f, 9.0f);
            barSr.sortingOrder = 22;
            barrierGo.SetActive(false); // Inicia aberta até o gatilho ser acionado

            // Plataformas táticas de salto na Arena (para alcançar o Ponto Fraco no topo do chefe)
            // Plataforma Baixa Esquerda (X: 90 a 95, Y: 3.2)
            CreateFloatingPlatform(levelRoot.transform, 90f, 95f, 3.2f, groundSprite);

            // Altar Central elevado (X: 99 a 105, Y: 1.8)
            CreateFloatingPlatform(levelRoot.transform, 99f, 105f, 1.8f, groundSprite);

            // Plataforma Alta Central para pulo esmagador supremo (X: 100 a 104, Y: 5.2)
            CreateFloatingPlatform(levelRoot.transform, 100f, 104f, 5.2f, groundSprite);
            SpawnPickup(prefabs.oxygen, new Vector3(102f, 6.2f, 0f)); // Cápsula de O2 para suporte na batalha

            // Plataforma Baixa Direita (X: 109 a 114, Y: 3.2)
            CreateFloatingPlatform(levelRoot.transform, 109f, 114f, 3.2f, groundSprite);

            // Ponto de spawn triunfal da peça no altar central ao derrotar o chefe
            var partSpawnPoint = new GameObject("ShipPartRewardSpawnPoint");
            partSpawnPoint.transform.SetParent(levelRoot.transform);
            partSpawnPoint.transform.position = new Vector3(102f, 3.2f, 0f);

            // Instancia o Chefe do Pântano (X: 108, Y: -0.1)
            var bossObj = (GameObject)PrefabUtility.InstantiatePrefab(prefabs.bossSwamp);
            bossObj.transform.position = new Vector3(108f, -0.1f, 0f);
            var bossSwamp = bossObj.GetComponent<BossSwamp>();
            bossSwamp.arenaMinX = 87.5f;
            bossSwamp.arenaMaxX = 124f;
            bossSwamp.arenaBarrier = barrierGo;
            bossSwamp.shipPartPrefab = prefabs.shipPart;
            bossSwamp.shipPartSpawnPoint = partSpawnPoint.transform;

            // Gatilho de Entrada da Arena do Chefe (X: 88.5, Y: 2.5)
            var triggerObj = new GameObject("Trigger_BossArena", typeof(BoxCollider2D), typeof(BossArenaTrigger));
            triggerObj.transform.SetParent(levelRoot.transform);
            triggerObj.transform.position = new Vector3(88.5f, 2.5f, 0f);
            var trigCol = triggerObj.GetComponent<BoxCollider2D>();
            trigCol.isTrigger = true;
            trigCol.size = new Vector2(1.5f, 7.0f);
            var arenaTrigger = triggerObj.GetComponent<BossArenaTrigger>();
            arenaTrigger.boss = bossSwamp;
            arenaTrigger.arenaBarrier = barrierGo;
            arenaTrigger.cameraFollow = follow;
            arenaTrigger.cameraMinX = 86f;
            arenaTrigger.cameraMaxX = 118f;

            // Adereço Nave Caída no início (X: -3.5, Y: -0.2)
            var shipCrash = new GameObject("Prop_CrashedShip", typeof(SpriteRenderer));
            shipCrash.transform.SetParent(levelRoot.transform);
            var srCrash = shipCrash.GetComponent<SpriteRenderer>();
            srCrash.sprite = AssetDatabase.LoadAssetAtPath<Sprite>($"{ArtDir}/prop_crashed_ship.png");
            srCrash.sortingOrder = 15;
            shipCrash.transform.position = new Vector3(-3.5f, -0.1f, 0f);

            // Cogumelos e vegetação decorativa
            SpawnProp("prop_mushrooms.png", new Vector3(8f, -0.1f, 0f), 15);
            SpawnProp("prop_tree_stump.png", new Vector3(32f, -0.1f, 0f), 15);
            SpawnProp("prop_alien_flower.png", new Vector3(48f, -0.1f, 0f), 15);
            SpawnProp("prop_mushrooms.png", new Vector3(70f, -0.1f, 0f), 15);
            SpawnProp("prop_tree_stump.png", new Vector3(90f, -0.1f, 0f), 15);
            SpawnProp("prop_alien_flower.png", new Vector3(115f, -0.1f, 0f), 15);

            // Inimigos: Rastejantes de Gosma
            // Inimigo 1 (X: 12, patrulha entre 10 e 17)
            SpawnSlime(prefabs.slime, 12f, -0.1f, 9.5f, 18.5f);

            // Inimigo 2 (X: 36, patrulha entre 34 e 41)
            SpawnSlime(prefabs.slime, 36f, -0.1f, 33f, 41.5f);

            // Inimigo 3 (X: 47, patrulha entre 44 e 51)
            SpawnSlime(prefabs.slime, 47f, -0.1f, 44f, 50.5f);

            // Cristais e Pickups pelo chão
            SpawnPickup(prefabs.crystal, new Vector3(4f, 0.4f, 0f));
            SpawnPickup(prefabs.crystal, new Vector3(15f, 0.4f, 0f));
            SpawnPickup(prefabs.oxygen, new Vector3(30f, 0.4f, 0f));
            SpawnPickup(prefabs.crystal, new Vector3(50f, 0.4f, 0f));
            SpawnPickup(prefabs.fruit, new Vector3(68f, 0.4f, 0f));
            SpawnPickup(prefabs.crystal, new Vector3(85f, 0.4f, 0f));

            // Instancia o Jogador: Chico no início (X: 0, Y: 0.5)
            var playerObj = (GameObject)PrefabUtility.InstantiatePrefab(prefabs.player);
            playerObj.transform.position = new Vector3(0f, 0.5f, 0f);
            var playerController = playerObj.GetComponent<PlayerController>();

            bossSwamp.playerTransform = playerObj.transform;

            // Conecta Câmera ao Chico
            follow.target = playerObj.transform;

            // Instancia o Canvas de UI
            var hudCanvasObj = (GameObject)PrefabUtility.InstantiatePrefab(prefabs.hudCanvas);
            var hud = hudCanvasObj.GetComponentInChildren<HUD>();
            var backpackUI = hudCanvasObj.GetComponentInChildren<BackpackUI>();
            var pauseUI = hudCanvasObj.GetComponentInChildren<PauseMenuUI>();
            var gameOverUI = hudCanvasObj.GetComponentInChildren<GameOverUI>();
            var levelCompleteUI = hudCanvasObj.GetComponentInChildren<LevelCompleteUI>();
            var bossHealthUI = hudCanvasObj.GetComponentInChildren<BossHealthBarUI>();
            arenaTrigger.bossHealthBar = bossHealthUI;
            if (bossHealthUI != null) bossHealthUI.boss = bossSwamp;

            // Cria GameManager
            var gmObj = new GameObject("GameManager");
            var gm = gmObj.AddComponent<GameManager>();
            gm.levelName = "Pântano Bioluminescente";
            gm.player = playerController;
            gm.hud = hud;
            gm.backpackUI = backpackUI;
            gm.pauseMenuUI = pauseUI;
            gm.gameOverUI = gameOverUI;
            gm.levelCompleteUI = levelCompleteUI;

            // EventSystem
            new GameObject("EventSystem", typeof(UnityEngine.EventSystems.EventSystem), typeof(UnityEngine.EventSystems.StandaloneInputModule));

            EditorSceneManager.SaveScene(scene, $"{SceneDir}/01_Pantano.unity");
            Debug.Log("[Space Chico] Cena '01_Pantano' montada e salva com sucesso!");
        }

        private static void CreateGroundSection(Transform parent, float minX, float maxX, float topY, Sprite sprite, float height = 5.0f)
        {
            var go = new GameObject($"Ground_{minX:F0}_{maxX:F0}");
            go.transform.SetParent(parent);
            go.tag = Layers.TagGround;
            go.layer = LayerMask.NameToLayer(Layers.Ground);

            float width = maxX - minX;
            float centerX = (minX + maxX) * 0.5f;

            go.transform.position = new Vector3(centerX, topY, 0f);

            var col = go.AddComponent<BoxCollider2D>();
            col.size = new Vector2(width, height);
            col.offset = new Vector2(0f, -height * 0.5f);
            col.usedByEffector = true;

            // Plataforma semi-sólida (permite subir de baixo para cima e pisar no topo)
            var effector = go.AddComponent<PlatformEffector2D>();
            effector.useOneWay = true;
            effector.surfaceArc = 170f;

            var sr = go.AddComponent<SpriteRenderer>();
            sr.sprite = sprite;
            sr.drawMode = SpriteDrawMode.Tiled;
            sr.size = new Vector2(width, height);
            sr.tileMode = SpriteTileMode.Continuous;
            sr.sortingOrder = 20;
        }

        private static void CreateFloatingPlatform(Transform parent, float minX, float maxX, float y, Sprite sprite)
        {
            var go = new GameObject($"Platform_{minX:F0}_{maxX:F0}");
            go.transform.SetParent(parent);
            go.tag = Layers.TagGround;
            go.layer = LayerMask.NameToLayer(Layers.Ground);

            float width = maxX - minX;
            float height = 0.9f;
            float centerX = (minX + maxX) * 0.5f;

            go.transform.position = new Vector3(centerX, y, 0f);

            var col = go.AddComponent<BoxCollider2D>();
            col.size = new Vector2(width, height);
            col.offset = new Vector2(0f, -height * 0.5f);
            col.usedByEffector = true;

            // Plataforma semi-sólida (permite subir de baixo para cima e pisar no topo)
            var effector = go.AddComponent<PlatformEffector2D>();
            effector.useOneWay = true;
            effector.surfaceArc = 170f;

            var sr = go.AddComponent<SpriteRenderer>();
            sr.sprite = sprite;
            sr.drawMode = SpriteDrawMode.Tiled;
            sr.size = new Vector2(width, height);
            sr.tileMode = SpriteTileMode.Continuous;
            sr.sortingOrder = 20;
        }

        private static void CreateHazardPool(Transform parent, float minX, float maxX, float y, GameObject hazardPrefab)
        {
            var go = (GameObject)PrefabUtility.InstantiatePrefab(hazardPrefab, parent);
            float width = maxX - minX;
            go.transform.position = new Vector3((minX + maxX) * 0.5f, y, 0f);
            var col = go.GetComponent<BoxCollider2D>();
            col.size = new Vector2(width, 1.4f);
            var sr = go.GetComponent<SpriteRenderer>();
            sr.size = new Vector2(width, 1.4f);
            sr.drawMode = SpriteDrawMode.Tiled;
        }

        private static void SpawnSlime(GameObject prefab, float x, float y, float patrolMinX, float patrolMaxX)
        {
            var go = (GameObject)PrefabUtility.InstantiatePrefab(prefab);
            go.transform.position = new Vector3(x, y, 0f);

            var wpA = new GameObject("Waypoint_A");
            wpA.transform.position = new Vector3(patrolMinX, y, 0f);
            var wpB = new GameObject("Waypoint_B");
            wpB.transform.position = new Vector3(patrolMaxX, y, 0f);

            var slime = go.GetComponent<SlimeCrawler>();
            slime.pointA = wpA.transform;
            slime.pointB = wpB.transform;
        }

        private static void SpawnPickup(GameObject prefab, Vector3 pos)
        {
            var go = (GameObject)PrefabUtility.InstantiatePrefab(prefab);
            go.transform.position = pos;
        }

        private static void SpawnProp(string spriteName, Vector3 pos, int sortingOrder)
        {
            var go = new GameObject("Prop_" + Path.GetFileNameWithoutExtension(spriteName), typeof(SpriteRenderer));
            var sr = go.GetComponent<SpriteRenderer>();
            sr.sprite = AssetDatabase.LoadAssetAtPath<Sprite>($"{ArtDir}/{spriteName}");
            sr.sortingOrder = sortingOrder;
            go.transform.position = pos;
        }

        // ====================================================================
        //  5. BUILD SETTINGS
        // ====================================================================
        private static void SetupBuildSettings()
        {
            var scenes = new EditorBuildSettingsScene[]
            {
                new EditorBuildSettingsScene($"{SceneDir}/00_Intro.unity", true),
                new EditorBuildSettingsScene($"{SceneDir}/01_Pantano.unity", true)
            };
            EditorBuildSettings.scenes = scenes;
            Debug.Log("[Space Chico] Build Settings configurados com '00_Intro' e '01_Pantano'.");
        }
    }
}
