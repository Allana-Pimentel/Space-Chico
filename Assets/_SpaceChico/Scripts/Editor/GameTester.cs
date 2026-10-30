using System.Collections;
using System.IO;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

namespace SpaceChico.Editor
{
    /// <summary>
    /// Utilitário para captura de tela automática da cena para validação visual.
    /// </summary>
    public static class GameTester
    {
        private const string ScreenshotDir = "Screenshots";

        [MenuItem("Tools/Space Chico/5. Capture Scene Screenshot")]
        public static void CaptureScreenshot()
        {
            if (!Directory.Exists(ScreenshotDir)) Directory.CreateDirectory(ScreenshotDir);

            // Abre a cena 01_Pantano
            EditorSceneManager.OpenScene("Assets/_SpaceChico/Scenes/01_Pantano.unity");

            var cam = Camera.main;
            if (cam == null) cam = Object.FindAnyObjectByType<Camera>();
            if (cam == null)
            {
                Debug.LogError("Câmera não encontrada na cena!");
                return;
            }

            cam.transform.position = new Vector3(0f, 1.8f, -10f);

            int width = 1280;
            int height = 720;
            RenderTexture rt = new RenderTexture(width, height, 24);
            cam.targetTexture = rt;
            Texture2D screenShot = new Texture2D(width, height, TextureFormat.RGB24, false);

            cam.Render();
            RenderTexture.active = rt;
            screenShot.ReadPixels(new Rect(0, 0, width, height), 0, 0);
            screenShot.Apply();

            cam.targetTexture = null;
            RenderTexture.active = null;
            Object.DestroyImmediate(rt);

            byte[] bytes = screenShot.EncodeToPNG();
            string filename = Path.Combine(ScreenshotDir, "screenshot_pantano.png");
            File.WriteAllBytes(filename, bytes);

            Debug.Log($"<color=#00ff88>[Space Chico]</color> Captura de tela salva em: {filename}");
        }

        [MenuItem("Tools/Space Chico/6. Capture Boss Arena Screenshot")]
        public static void CaptureBossArena()
        {
            if (!Directory.Exists(ScreenshotDir)) Directory.CreateDirectory(ScreenshotDir);

            EditorSceneManager.OpenScene("Assets/_SpaceChico/Scenes/01_Pantano.unity");

            var cam = Camera.main;
            if (cam == null) cam = Object.FindAnyObjectByType<Camera>();
            if (cam == null) return;

            cam.transform.position = new Vector3(104f, 2.8f, -10f);

            int width = 1280;
            int height = 720;
            RenderTexture rt = new RenderTexture(width, height, 24);
            cam.targetTexture = rt;
            Texture2D screenShot = new Texture2D(width, height, TextureFormat.RGB24, false);

            cam.Render();
            RenderTexture.active = rt;
            screenShot.ReadPixels(new Rect(0, 0, width, height), 0, 0);
            screenShot.Apply();

            cam.targetTexture = null;
            RenderTexture.active = null;
            Object.DestroyImmediate(rt);

            byte[] bytes = screenShot.EncodeToPNG();
            string filename = Path.Combine(ScreenshotDir, "screenshot_boss_arena.png");
            File.WriteAllBytes(filename, bytes);

            Debug.Log($"<color=#00ff88>[Space Chico]</color> Captura da arena salva em: {filename}");
        }

        public static void CaptureScreenshotBatch()
        {
            CaptureScreenshot();
            CaptureBossArena();
            EditorApplication.Exit(0);
        }
    }
}
