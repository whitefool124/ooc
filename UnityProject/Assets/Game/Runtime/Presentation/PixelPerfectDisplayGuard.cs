using UnityEngine;
using UnityEngine.SceneManagement;

namespace OCC.Combat.Presentation
{
    /// <summary>
    /// First-stage display guard for the OCC pixel pipeline.
    /// It removes renderer-level features that introduce blended pixels and
    /// applies the same policy to cameras created by runtime presentations.
    /// </summary>
    internal static class PixelPerfectDisplayGuard
    {
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
        private static void Install()
        {
            QualitySettings.antiAliasing = 0;
            SceneManager.sceneLoaded -= OnSceneLoaded;
            SceneManager.sceneLoaded += OnSceneLoaded;
            ApplyToCameras();
        }

        private static void OnSceneLoaded(Scene scene, LoadSceneMode mode) => ApplyToCameras();

        private static void ApplyToCameras()
        {
            Camera[] cameras = Object.FindObjectsByType<Camera>(FindObjectsInactive.Include, FindObjectsSortMode.None);
            foreach (Camera camera in cameras)
            {
                if (camera == null) continue;
                camera.allowMSAA = false;
                camera.allowHDR = false;
            }
        }
    }
}
