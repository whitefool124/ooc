using OCC.Combat.Presentation;
using UnityEditor.Build;
using UnityEditor.Build.Reporting;
using UnityEngine;
using UnityEngine.SceneManagement;

// Remove the editor-only component from the build's scene copy, preserving the source scene.
internal sealed class OccReleaseSceneProcessor : IProcessSceneWithReport
{
    public int callbackOrder => 0;
    public void OnProcessScene(Scene scene, BuildReport report)
    {
        if (report == null || (report.summary.options & UnityEditor.BuildOptions.Development) != 0) return;
        foreach (GameObject root in scene.GetRootGameObjects())
            foreach (DeveloperConsolePanel console in root.GetComponentsInChildren<DeveloperConsolePanel>(true))
                Object.DestroyImmediate(console);
    }
}
