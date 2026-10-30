using System.IO;
using UnityEditor;
using UnityEngine;

namespace SpaceChico.Editor
{
    /// <summary>
    /// Processa os arquivos de imagem gerados por IA em ArtSource,
    /// remove os fundos de chroma-key (verde e magenta), recorta os sprites individuais,
    /// e salva PNGs com transparência perfeita em Assets/_SpaceChico/Art/.
    /// </summary>
    public static class SpriteProcessor
    {
        private static readonly string SourceDir = "ArtSource";
        private static readonly string TargetDir = "Assets/_SpaceChico/Art";

        [MenuItem("Tools/Space Chico/1. Process All Sprites (Chroma Key & Slicing)")]
        public static void ProcessAllSprites()
        {
            if (!Directory.Exists(TargetDir))
            {
                Directory.CreateDirectory(TargetDir);
            }

            // 1. Chico Sprites (Chroma Verde #00FF00)
            ProcessChico();

            // 2. Slime Crawler (Chroma Magenta #FF00FF)
            ProcessSlime();

            // 3. Items (Chroma Magenta)
            ProcessItems();

            // 4. Props (Chroma Magenta)
            ProcessProps();

            // 5. Boss do Pântano (Chroma Magenta)
            ProcessBoss();

            // 6. Ground Tile & Background
            ProcessEnvironment();

            AssetDatabase.Refresh(ImportAssetOptions.ForceUpdate);
            ConfigureSpriteImporters();
            AssetDatabase.Refresh();

            Debug.Log("<color=#00ff88>[Space Chico]</color> Todos os sprites foram processados e configurados com sucesso em " + TargetDir);
        }

        private static void ProcessChico()
        {
            // Sheet A: Idle, Run1, Run2, Jump
            string pathA = Path.Combine(SourceDir, "chico_sheet_a.jpg");
            if (File.Exists(pathA))
            {
                Texture2D texA = LoadTexture(pathA);
                Texture2D cleanA = RemoveChromaKey(texA, ChromaType.Green);

                int w = cleanA.width;
                int h = cleanA.height;

                // 4 poses distribuídas horizontalmente
                SaveSubSprite(cleanA, new Rect(0f * w, 0, 0.25f * w, h), "chico_idle.png");
                SaveSubSprite(cleanA, new Rect(0.25f * w, 0, 0.25f * w, h), "chico_run_0.png");
                SaveSubSprite(cleanA, new Rect(0.50f * w, 0, 0.25f * w, h), "chico_run_1.png");
                SaveSubSprite(cleanA, new Rect(0.75f * w, 0, 0.25f * w, h), "chico_jump.png");
            }

            // Sheet B: Fall, Hurt, Climb, Celebrate
            string pathB = Path.Combine(SourceDir, "chico_sheet_b.jpg");
            if (File.Exists(pathB))
            {
                Texture2D texB = LoadTexture(pathB);
                Texture2D cleanB = RemoveChromaKey(texB, ChromaType.Green);

                int w = cleanB.width;
                int h = cleanB.height;

                SaveSubSprite(cleanB, new Rect(0f * w, 0, 0.25f * w, h), "chico_fall.png");
                SaveSubSprite(cleanB, new Rect(0.25f * w, 0, 0.25f * w, h), "chico_hurt.png");
                SaveSubSprite(cleanB, new Rect(0.50f * w, 0, 0.25f * w, h), "chico_climb.png");
                SaveSubSprite(cleanB, new Rect(0.75f * w, 0, 0.25f * w, h), "chico_celebrate.png");
            }

            // Sheet C (Backup / refinamento limpo de poses)
            string pathC = Path.Combine(SourceDir, "chico_sheet_c.jpg");
            if (File.Exists(pathC))
            {
                Texture2D texC = LoadTexture(pathC);
                Texture2D cleanC = RemoveChromaKey(texC, ChromaType.Green);
                int w = cleanC.width;
                int h = cleanC.height;

                SaveSubSprite(cleanC, new Rect(0.68f * w, 0, 0.32f * w, h), "chico_celebrate.png");
            }
        }

        private static void ProcessSlime()
        {
            string path = Path.Combine(SourceDir, "slime_sheet.jpg");
            if (!File.Exists(path)) return;

            Texture2D tex = LoadTexture(path);
            Texture2D clean = RemoveChromaKey(tex, ChromaType.Magenta);
            int w = clean.width;
            int h = clean.height;

            // 3 poses: Crawl 1, Crawl 2, Squished
            SaveSubSprite(clean, new Rect(0.02f * w, 0.1f * h, 0.33f * w, 0.8f * h), "slime_crawl_0.png");
            SaveSubSprite(clean, new Rect(0.35f * w, 0.1f * h, 0.33f * w, 0.8f * h), "slime_crawl_1.png");
            SaveSubSprite(clean, new Rect(0.68f * w, 0.1f * h, 0.30f * w, 0.8f * h), "slime_squished.png");
        }

        private static void ProcessItems()
        {
            string path = Path.Combine(SourceDir, "items_sheet.jpg");
            if (!File.Exists(path)) return;

            Texture2D tex = LoadTexture(path);
            Texture2D clean = RemoveChromaKey(tex, ChromaType.Magenta);
            int w = clean.width;
            int h = clean.height;

            // Linha superior: Thruster, Fruit, Crystal
            // Linha inferior: O2, Shield, DoubleJump
            SaveSubSprite(clean, new Rect(0.04f * w, 0.52f * h, 0.30f * w, 0.44f * h), "item_thruster.png");
            SaveSubSprite(clean, new Rect(0.38f * w, 0.52f * h, 0.28f * w, 0.44f * h), "item_neon_fruit.png");
            SaveSubSprite(clean, new Rect(0.70f * w, 0.52f * h, 0.28f * w, 0.44f * h), "item_crystal.png");

            SaveSubSprite(clean, new Rect(0.05f * w, 0.05f * h, 0.25f * w, 0.44f * h), "item_oxygen.png");
            SaveSubSprite(clean, new Rect(0.32f * w, 0.05f * h, 0.28f * w, 0.44f * h), "item_shield.png");
            SaveSubSprite(clean, new Rect(0.70f * w, 0.05f * h, 0.28f * w, 0.44f * h), "item_double_jump.png");
        }

        private static void ProcessProps()
        {
            string path = Path.Combine(SourceDir, "props_sheet.jpg");
            if (!File.Exists(path)) return;

            Texture2D tex = LoadTexture(path);
            Texture2D clean = RemoveChromaKey(tex, ChromaType.Magenta);
            int w = clean.width;
            int h = clean.height;

            // 5 adereços: Crashed Ship, Mushrooms, Vine, Stump, Flower
            SaveSubSprite(clean, new Rect(0.01f * w, 0.05f * h, 0.31f * w, 0.9f * h), "prop_crashed_ship.png");
            SaveSubSprite(clean, new Rect(0.35f * w, 0.05f * h, 0.20f * w, 0.9f * h), "prop_mushrooms.png");
            SaveSubSprite(clean, new Rect(0.53f * w, 0.05f * h, 0.12f * w, 0.9f * h), "prop_vine.png");
            SaveSubSprite(clean, new Rect(0.63f * w, 0.05f * h, 0.22f * w, 0.9f * h), "prop_tree_stump.png");
            SaveSubSprite(clean, new Rect(0.85f * w, 0.05f * h, 0.14f * w, 0.9f * h), "prop_alien_flower.png");
        }

        private static void ProcessBoss()
        {
            string path = Path.Combine(SourceDir, "boss_sheet.jpg");
            if (!File.Exists(path)) return;

            Texture2D tex = LoadTexture(path);
            Texture2D clean = RemoveChromaKey(tex, ChromaType.Magenta);
            int w = clean.width;
            int h = clean.height;

            // 1. Pose Idle (Top-Left): postura imponente
            SaveSubSprite(clean, new Rect(0.02f * w, 0.58f * h, 0.54f * w, 0.40f * h), "boss_idle.png");

            // 2. Pose Ataque / Rugido (Middle-Right)
            SaveSubSprite(clean, new Rect(0.42f * w, 0.30f * h, 0.48f * w, 0.40f * h), "boss_attack.png");

            // 3. Pose Dano / Salto Enfurecido (Bottom-Left)
            SaveSubSprite(clean, new Rect(0.08f * w, 0.02f * h, 0.50f * w, 0.42f * h), "boss_hurt.png");

            // 4. Projétil de Lodo Tóxico (Orb verde cuspido à direita da boca)
            SaveSubSprite(clean, new Rect(0.80f * w, 0.46f * h, 0.18f * w, 0.20f * h), "boss_spit.png");
        }

        private static void ProcessEnvironment()
        {
            // Fundo
            string bgPath = Path.Combine(SourceDir, "bg_far.jpg");
            if (File.Exists(bgPath))
            {
                byte[] bgBytes = File.ReadAllBytes(bgPath);
                File.WriteAllBytes(Path.Combine(TargetDir, "bg_swamp_far.jpg"), bgBytes);
            }

            // Bloco de chão
            string tilePath = Path.Combine(SourceDir, "ground_tile.jpg");
            if (File.Exists(tilePath))
            {
                byte[] tileBytes = File.ReadAllBytes(tilePath);
                File.WriteAllBytes(Path.Combine(TargetDir, "ground_block.jpg"), tileBytes);
            }
        }

        private enum ChromaType { Green, Magenta }

        private static Texture2D RemoveChromaKey(Texture2D src, ChromaType type)
        {
            int w = src.width;
            int h = src.height;
            var pixels = src.GetPixels();
            var outPixels = new Color[pixels.Length];

            for (int i = 0; i < pixels.Length; i++)
            {
                Color c = pixels[i];
                bool isKey = false;
                float alpha = 1f;

                if (type == ChromaType.Green)
                {
                    // Fundo verde puro (#00FF00) com tolerância a artefatos JPG
                    float greenDominance = c.g - Mathf.Max(c.r, c.b);
                    if (greenDominance > 0.18f || (c.g > 0.55f && c.r < 0.35f && c.b < 0.35f))
                    {
                        isKey = true;
                    }
                    else if (greenDominance > 0.05f)
                    {
                        alpha = Mathf.Clamp01(1f - (greenDominance - 0.05f) / 0.13f);
                        // Despill: remove tom esverdeado nas bordas
                        c.g = Mathf.Max(c.r, c.b);
                    }
                }
                else if (type == ChromaType.Magenta)
                {
                    // Fundo magenta puro (#FF00FF)
                    float magentaScore = (c.r + c.b) * 0.5f - c.g;
                    if (magentaScore > 0.22f && c.r > 0.5f && c.b > 0.5f)
                    {
                        isKey = true;
                    }
                    else if (magentaScore > 0.12f && c.r > 0.4f && c.b > 0.4f)
                    {
                        alpha = Mathf.Clamp01(1f - (magentaScore - 0.12f) / 0.10f);
                        // Despill magenta
                        c.r = Mathf.Min(c.r, c.g + 0.1f);
                        c.b = Mathf.Min(c.b, c.g + 0.1f);
                    }
                }

                outPixels[i] = isKey ? Color.clear : new Color(c.r, c.g, c.b, alpha);
            }

            Texture2D res = new Texture2D(w, h, TextureFormat.RGBA32, false);
            res.SetPixels(outPixels);
            res.Apply();
            return res;
        }

        private static void SaveSubSprite(Texture2D src, Rect rect, string filename)
        {
            int rx = Mathf.Clamp((int)rect.x, 0, src.width - 1);
            int ry = Mathf.Clamp((int)rect.y, 0, src.height - 1);
            int rw = Mathf.Clamp((int)rect.width, 1, src.width - rx);
            int rh = Mathf.Clamp((int)rect.height, 1, src.height - ry);

            Color[] rawPixels = src.GetPixels(rx, ry, rw, rh);

            // Corta excesso de transparência (Crop to non-empty bounds)
            int minX = rw, maxX = 0, minY = rh, maxY = 0;
            for (int y = 0; y < rh; y++)
            {
                for (int x = 0; x < rw; x++)
                {
                    if (rawPixels[y * rw + x].a > 0.05f)
                    {
                        if (x < minX) minX = x;
                        if (x > maxX) maxX = x;
                        if (y < minY) minY = y;
                        if (y > maxY) maxY = y;
                    }
                }
            }

            // Margem de segurança de 4 pixels
            int pad = 4;
            minX = Mathf.Max(0, minX - pad);
            minY = Mathf.Max(0, minY - pad);
            maxX = Mathf.Min(rw - 1, maxX + pad);
            maxY = Mathf.Min(rh - 1, maxY + pad);

            int finalW = Mathf.Max(1, maxX - minX + 1);
            int finalH = Mathf.Max(1, maxY - minY + 1);

            Color[] cropped = new Color[finalW * finalH];
            for (int cy = 0; cy < finalH; cy++)
            {
                for (int cx = 0; cx < finalW; cx++)
                {
                    cropped[cy * finalW + cx] = rawPixels[(minY + cy) * rw + (minX + cx)];
                }
            }

            Texture2D outTex = new Texture2D(finalW, finalH, TextureFormat.RGBA32, false);
            outTex.SetPixels(cropped);
            outTex.Apply();

            byte[] pngData = outTex.EncodeToPNG();
            string fullPath = Path.Combine(TargetDir, filename);
            File.WriteAllBytes(fullPath, pngData);
        }

        private static void ConfigureSpriteImporters()
        {
            string[] files = Directory.GetFiles(TargetDir);
            foreach (var f in files)
            {
                if (f.EndsWith(".meta")) continue;

                string assetPath = f.Replace('\\', '/');
                var importer = AssetImporter.GetAtPath(assetPath) as TextureImporter;
                if (importer == null) continue;

                importer.textureType = TextureImporterType.Sprite;
                importer.spriteImportMode = SpriteImportMode.Single;
                importer.spritePixelsPerUnit = 100;
                importer.mipmapEnabled = false;
                importer.alphaIsTransparency = true;
                importer.filterMode = FilterMode.Bilinear;

                var settings = new TextureImporterSettings();
                importer.ReadTextureSettings(settings);
                if (assetPath.Contains("chico") || assetPath.Contains("slime"))
                {
                    settings.spriteAlignment = (int)SpriteAlignment.Custom;
                    settings.spritePivot = new Vector2(0.5f, 0.1f); // Pé do personagem
                }
                else if (assetPath.Contains("boss_spit"))
                {
                    settings.spriteAlignment = (int)SpriteAlignment.Center;
                    settings.spritePivot = new Vector2(0.5f, 0.5f);
                }
                else if (assetPath.Contains("boss"))
                {
                    settings.spriteAlignment = (int)SpriteAlignment.Custom;
                    settings.spritePivot = new Vector2(0.5f, 0.12f); // Base do corpo do Boss
                }
                else if (assetPath.Contains("ground_block"))
                {
                    importer.spritePixelsPerUnit = 300;
                    settings.spriteAlignment = (int)SpriteAlignment.TopCenter;
                    settings.spritePivot = new Vector2(0.5f, 1.0f); // Topo da plataforma
                }
                else
                {
                    settings.spriteAlignment = (int)SpriteAlignment.Center;
                    settings.spritePivot = new Vector2(0.5f, 0.5f); // Centro
                }
                if (assetPath.Contains("ground") || assetPath.Contains("water") || assetPath.Contains("hazard"))
                {
                    importer.wrapMode = TextureWrapMode.Repeat;
                }
                importer.SetTextureSettings(settings);

                importer.SaveAndReimport();
            }
        }

        private static Texture2D LoadTexture(string path)
        {
            byte[] bytes = File.ReadAllBytes(path);
            Texture2D tex = new Texture2D(2, 2);
            tex.LoadImage(bytes);
            return tex;
        }
    }
}
