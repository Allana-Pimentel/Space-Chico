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
                    var player = GameObject.Find("Player_Chico");
                    if (player != null) Selection.activeGameObject = player;
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
    }
}
#endif
