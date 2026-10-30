#if UNITY_EDITOR
using UnityEditor;
using UnityEditor.Callbacks;
using UnityEngine;

namespace SpaceChico.Editor
{
    public static class AutoRebuilder
    {
        [DidReloadScripts]
        private static void OnScriptsReloaded()
        {
            EditorApplication.delayCall += () =>
            {
                if (EditorApplication.isPlayingOrWillChangePlaymode)
                    return;

                if (SessionState.GetBool("SpaceChico_RebuiltDone_v2", false))
                    return;

                SessionState.SetBool("SpaceChico_RebuiltDone_v2", true);
                Debug.Log("<color=#00e5ff><b>[Space Chico] Atualizando HUD e novo Background...</b></color>");
                SpaceChicoBuilder.BuildEverything();
                Debug.Log("<color=#00ff88><b>[Space Chico] HUD no canto e Background atualizados com sucesso!</b></color>");
            };
        }
    }
}
#endif
