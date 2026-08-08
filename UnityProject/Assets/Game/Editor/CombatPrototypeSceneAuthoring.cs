using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

namespace OCC.Combat.Presentation.Editor
{
    public static class CombatPrototypeSceneAuthoring
    {
        [MenuItem("OCC/Refresh Combat Prototype Preview")]
        private static void EnsureSceneVisuals()
        {
            if (EditorApplication.isPlayingOrWillChangePlaymode) return;
            if (UnityEngine.SceneManagement.SceneManager.GetActiveScene().path != "Assets/Scenes/CombatPrototype.unity") return;
            CombatPrototypeBootstrap manager = Object.FindAnyObjectByType<CombatPrototypeBootstrap>();
            if (manager == null) return;
            manager.EnsureEditorVisuals();
            // Keep editor previews transient; formal scene saves require explicit user approval.
        }
    }
}
