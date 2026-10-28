using System;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEditor.Build.Reporting;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;
using OCC.Combat.Presentation;

namespace OCC.Combat.EditorTools
{
    public static class OccWindowsPlayerBuild
    {
        public static void Build()
        {
            string[] args=Environment.GetCommandLineArgs();string output=null;
            for(int i=0;i<args.Length-1;i++)if(args[i]=="-buildOutput")output=Path.GetFullPath(args[i+1]);
            BuildTo(output);
        }
        public static void BuildTo(string output)
        {
            string project=Path.GetFullPath(Path.Combine(Application.dataPath,".."));
            if(project.Replace('\\','/')!="E:/数据库/OCC_Codex/UnityProject"&&project.Replace('\\','/')!="D:/occart/OCC_latest/UnityProject")
                throw new InvalidOperationException("Build must use the current OCC main project.");
            for(int i=0;i<SceneManager.sceneCount;i++)
                if(SceneManager.GetSceneAt(i).isDirty)throw new InvalidOperationException("Unsaved scene; build aborted without saving it.");
            AetherBrandingSettings.Apply();
            AetherPaperImport.Apply();
            AetherWorkshopImport.Apply();
            if(string.IsNullOrEmpty(output)||!output.EndsWith(".exe",StringComparison.OrdinalIgnoreCase))
                throw new InvalidOperationException("Pass an explicit .exe -buildOutput.");
            if(File.Exists(output))throw new IOException("Use a new delivery directory; do not overwrite an earlier player.");
            Directory.CreateDirectory(Path.GetDirectoryName(output));
            var stripping=GraphicsSettings.GetRenderPipelineSettings<URPShaderStrippingSetting>();
            const string referencePath="Assets/Game/Resources/OccPlayerUnlitReference.mat";
            // Recover only the exact temporary reference left by the interrupted
            // first diagnostic build, whose recorded original stripping value was true.
            if(AssetDatabase.LoadAssetAtPath<Material>(referencePath)!=null)
            {
                stripping.stripUnusedVariants=true;
                AssetDatabase.DeleteAsset(referencePath);
            }
            bool oldPost=stripping.stripUnusedPostProcessingVariants;
            var pipeline=AssetDatabase.LoadAssetAtPath<UniversalRenderPipelineAsset>("Assets/Settings/UniversalRP.asset");
            var pipelineState=new SerializedObject(pipeline);
            var layers=pipelineState.FindProperty("m_SupportsLightLayers");bool oldLayers=layers.boolValue;
            var renderers=pipelineState.FindProperty("m_RendererDataList");
            var renderer=(ScriptableRendererData)renderers.GetArrayElementAtIndex(1).objectReferenceValue;
            var dependency=ScriptableObject.CreateInstance<OccRuntimeSsaoDependency>();
            dependency.name="OCC runtime SSAO build dependency";
            var dependencyState=new SerializedObject(dependency);
            dependencyState.FindProperty("m_Settings.Source").intValue=0;
            dependencyState.FindProperty("m_Settings.NormalSamples").intValue=2;
            dependencyState.FindProperty("m_Settings.AOMethod").intValue=1;
            dependencyState.FindProperty("m_Settings.Samples").intValue=0;
            dependencyState.ApplyModifiedPropertiesWithoutUndo();
            var unlit=Shader.Find("Universal Render Pipeline/Unlit");
            if(unlit==null)throw new InvalidOperationException("Map/battle Unlit shader unavailable.");
            BuildReport report;
            try
            {
                stripping.stripUnusedPostProcessingVariants=false;
                layers.boolValue=true;pipelineState.ApplyModifiedPropertiesWithoutUndo();
                AssetDatabase.AddObjectToAsset(dependency,renderer);
                renderer.rendererFeatures.Add(dependency);EditorUtility.SetDirty(renderer);AssetDatabase.SaveAssetIfDirty(renderer);
                // Shader.Find alone does not create a Player dependency. This build-only
                // reference includes Unlit without marking every Lit variant always included.
                AssetDatabase.CreateAsset(new Material(unlit),referencePath);
                report=BuildPipeline.BuildPlayer(new BuildPlayerOptions{
                scenes=new[]{"Assets/Scenes/CombatPrototype.unity"},locationPathName=output,
                target=BuildTarget.StandaloneWindows64,options=BuildOptions.CompressWithLz4HC});
            }
            finally
            {
                stripping.stripUnusedPostProcessingVariants=oldPost;
                renderer.rendererFeatures.Remove(dependency);UnityEngine.Object.DestroyImmediate(dependency,true);
                EditorUtility.SetDirty(renderer);AssetDatabase.SaveAssetIfDirty(renderer);
                pipelineState.Update();pipelineState.FindProperty("m_SupportsLightLayers").boolValue=oldLayers;
                pipelineState.ApplyModifiedPropertiesWithoutUndo();AssetDatabase.SaveAssetIfDirty(pipeline);
                foreach(string guid in AssetDatabase.FindAssets("t:UniversalRenderPipelineGlobalSettings"))
                {
                    var settings=AssetDatabase.LoadMainAssetAtPath(AssetDatabase.GUIDToAssetPath(guid));
                    EditorUtility.SetDirty(settings);AssetDatabase.SaveAssetIfDirty(settings);
                }
                AssetDatabase.DeleteAsset(referencePath);
            }
            var summary=report.summary;
            File.WriteAllText(Path.Combine(Path.GetDirectoryName(output),"build-report.json"),JsonUtility.ToJson(new Report{
                status=summary.result.ToString(),unityVersion=Application.unityVersion,
                sizeBytes=summary.totalSize.ToString(),durationSeconds=summary.totalTime.TotalSeconds,
                errors=summary.totalErrors,warnings=summary.totalWarnings,scene="Assets/Scenes/CombatPrototype.unity"},true));
            if(summary.result!=BuildResult.Succeeded)throw new InvalidOperationException("Windows player build failed: "+summary.result);
            AetherWindowsVersionInfo.Apply(output);
            foreach(string name in new[]{Path.GetFileName(output),"UnityPlayer.dll",Path.GetFileNameWithoutExtension(output)+"_Data"})
                if(!File.Exists(Path.Combine(Path.GetDirectoryName(output),name))&&!Directory.Exists(Path.Combine(Path.GetDirectoryName(output),name)))
                    throw new IOException("Build output incomplete: "+name);
            Debug.Log("OCC_WINDOWS_BUILD_SUCCEEDED "+output);
        }
        [Serializable] private sealed class Report
        {public string status,unityVersion,sizeBytes,scene;public double durationSeconds;public int errors,warnings;}
    }
}
