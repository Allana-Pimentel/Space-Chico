#if UNITY_EDITOR
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

namespace SpaceChico.Editor
{
    [InitializeOnLoad]
    public static class SceneOpener
    {
        [MenuItem("Space Chico/1. Abrir Fase do Jogo (Pantano) %1", priority = 1)]
        public static void OpenPantano()
        {
            if (EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo())
            {
                var scene = EditorSceneManager.OpenScene("Assets/_SpaceChico/Scenes/01_Pantano.unity");
                if (scene.IsValid())
                {
                    FocarNoChico();
                }
            }
        }

        [MenuItem("Space Chico/2. Abrir Menu Inicial (Intro) %2", priority = 2)]
        public static void OpenIntro()
        {
            if (EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo())
            {
                EditorSceneManager.OpenScene("Assets/_SpaceChico/Scenes/00_Intro.unity");
            }
        }

        [MenuItem("Space Chico/3. Centralizar Scene View no Chico (F)", priority = 3)]
        public static void FocarNoChico()
        {
            var player = GameObject.Find("Player_Chico");
            if (player != null)
            {
                Selection.activeGameObject = player;
                var sv = SceneView.lastActiveSceneView ?? (SceneView.sceneViews.Count > 0 ? (SceneView)SceneView.sceneViews[0] : null);
                if (sv != null)
                {
                    sv.in2DMode = true;
                    sv.LookAt(player.transform.position, Quaternion.identity, 8f);
                    sv.Repaint();
                }
            }
        }
    }
}
#endif

