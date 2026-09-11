using System;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEditor.Build.Reporting;
using UnityEngine;

internal static class OccPlayableBuild
{
    private const string MenuPath = "OCC/Build/Windows Playable";
    private const string OutputRoot = "Artifacts/Builds";

    [MenuItem(MenuPath)]
    private static void BuildWindowsPlayable()
    {
        // Keep the build entry point in a compiled editor assembly so transient Funplay
        // snippet assemblies are discarded by the script-domain reload before packaging.
        string[] scenes = EditorBuildSettings.scenes
            .Where(scene => scene.enabled)
            .Select(scene => scene.path)
            .ToArray();
        if (scenes.Length == 0) throw new InvalidOperationException("No enabled scenes are configured for the player build.");

        string projectRoot = Directory.GetParent(Application.dataPath).FullName;
        string outputDirectory = Path.Combine(projectRoot, OutputRoot, "OCC_Playable_" + DateTime.Now.ToString("yyyy-MM-dd"));
        Directory.CreateDirectory(outputDirectory);
        string executablePath = Path.Combine(outputDirectory, "OCC.exe");

        BuildReport report = BuildPipeline.BuildPlayer(new BuildPlayerOptions
        {
            scenes = scenes,
            locationPathName = executablePath,
            target = BuildTarget.StandaloneWindows64,
            options = BuildOptions.None
        });

        BuildSummary summary = report.summary;
        string result = $"OCC_BUILD result={summary.result}; errors={summary.totalErrors}; warnings={summary.totalWarnings}; " +
                        $"size={summary.totalSize}; time={summary.totalTime}; output={executablePath}";
        if (summary.result == BuildResult.Succeeded) Debug.Log(result);
        else throw new InvalidOperationException(result);
    }
}
