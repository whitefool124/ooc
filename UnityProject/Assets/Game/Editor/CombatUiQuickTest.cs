using OCC.Combat.Presentation;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace OCC.Combat.EditorTools
{
    /// <summary>
    /// One-command combat UI loop for presentation work. It opens the dedicated
    /// arena, enters Play Mode and starts a deterministic battle without the
    /// front end, save selection, map or combat-entry sequence.
    /// </summary>
    public static class CombatUiQuickTest
    {
        private const string ArenaScene = "Assets/Scenes/CombatTestArena.unity";
        private const string PendingScenarioKey = "OCC.CombatUiQuickTest.PendingScenario";
        private const string DefaultScenario = "arena_n01_flank";

        [MenuItem("OCC/Quick Test/Combat UI Default _F8")]
        public static void LaunchDefault()
        {
            Launch(DefaultScenario);
        }

        [MenuItem("OCC/Quick Test/Combat Arena Selector #F8")]
        public static void LaunchSelector()
        {
            Launch(string.Empty);
        }

        private static void Launch(string scenarioId)
        {
            if (EditorApplication.isPlayingOrWillChangePlaymode)
            {
                Debug.LogWarning("Quick Test: exit Play Mode before launching another combat UI session.");
                return;
            }

            Scene active = EditorSceneManager.GetActiveScene();
            if (active.IsValid() && active.isDirty)
            {
                Debug.LogWarning("Quick Test: current scene has unsaved changes. Save or discard them explicitly, then press F8 again.");
                return;
            }

            if (string.IsNullOrEmpty(scenarioId)) PlayerPrefs.DeleteKey(PendingScenarioKey);
            else PlayerPrefs.SetString(PendingScenarioKey, scenarioId);
            PlayerPrefs.Save();
            EditorSceneManager.OpenScene(ArenaScene, OpenSceneMode.Single);
            EditorApplication.EnterPlaymode();
        }
    }
}
