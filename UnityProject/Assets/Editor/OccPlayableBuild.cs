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

    [MenuItem("OCC/Build/Web Playable")]
    private static void BuildWebPlayable()
    {
        if (!BuildPipeline.IsBuildTargetSupported(BuildTargetGroup.WebGL, BuildTarget.WebGL))
            throw new InvalidOperationException("Install Web Build Support for the current Unity version first.");
        string[] scenes = EditorBuildSettings.scenes.Where(scene => scene.enabled).Select(scene => scene.path).ToArray();
        if (scenes.Length == 0) throw new InvalidOperationException("No enabled scenes are configured.");
        string projectRoot = Directory.GetParent(Application.dataPath).FullName;
        string output = Path.Combine(projectRoot, OutputRoot, "OCC_Web_" + DateTime.Now.ToString("yyyy-MM-dd"));
        PlayerSettings.WebGL.compressionFormat = WebGLCompressionFormat.Gzip;
        PlayerSettings.WebGL.decompressionFallback = true;
        PlayerSettings.WebGL.template = "APPLICATION:Default";
        BuildReport report = BuildPipeline.BuildPlayer(new BuildPlayerOptions
        {
            scenes = scenes, locationPathName = output, target = BuildTarget.WebGL, options = BuildOptions.None
        });
        if (report.summary.result != BuildResult.Succeeded)
            throw new InvalidOperationException("OCC_WEB_BUILD failed: " + report.summary.result + "; errors=" + report.summary.totalErrors);
        // Web players require a URL video source, rather than an embedded VideoClip.
        string sourceVideo = Path.Combine(Application.dataPath, "Game/Resources/Video/OccOpening45s_UnityWebm.webm");
        if (!File.Exists(sourceVideo)) throw new FileNotFoundException("Opening video is missing", sourceVideo);
        string videoDirectory = Path.Combine(output, "StreamingAssets");
        Directory.CreateDirectory(videoDirectory);
        File.Copy(sourceVideo, Path.Combine(videoDirectory, "OccOpening45s_UnityWebm.webm"), true);
        Debug.Log($"OCC_WEB_BUILD result=Succeeded; errors={report.summary.totalErrors}; warnings={report.summary.totalWarnings}; size={report.summary.totalSize}; output={output}");
    }

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
