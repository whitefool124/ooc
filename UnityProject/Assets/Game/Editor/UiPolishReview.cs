using System;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Reflection;
using OCC.Combat.Presentation;
using OCC.Combat.Roguelite;
using Unity.Pipeline.Commands;
using UnityEditor;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace OCC.Combat.EditorTools
{
    // Explicit, local review commands. Never focus a window or save a scene.
    public static class UiPolishReview
    {
        private static IDisposable scope;
        private static UnityEditor.TestTools.TestRunner.Api.TestRunnerApi testApi;
        private sealed class TestReceipt : UnityEditor.TestTools.TestRunner.Api.ICallbacks
        {
            internal string Output;
            public void RunStarted(UnityEditor.TestTools.TestRunner.Api.ITestAdaptor tests) { }
            public void TestStarted(UnityEditor.TestTools.TestRunner.Api.ITestAdaptor test) { }
            public void TestFinished(UnityEditor.TestTools.TestRunner.Api.ITestResultAdaptor result) { }
            public void RunFinished(UnityEditor.TestTools.TestRunner.Api.ITestResultAdaptor result)
            {
                UnityEditor.TestTools.TestRunner.Api.TestRunnerApi.SaveResultToFile(result,Output);
                testApi.UnregisterCallbacks(this);UnityEngine.Object.DestroyImmediate(testApi);testApi=null;
            }
        }
        [CliCommand("occ_ui_review", "Inspect, measure or capture the current OCC UI without focusing a window.")]
        public static object Review(string action, string output = "")
        {
            if (Application.dataPath.Replace('\\', '/') != "E:/数据库/OCC_Codex/UnityProject/Assets")
                throw new InvalidOperationException("Wrong main project");
            if (action == "inspect") return new { Application.dataPath, Application.isPlaying, frame = Time.frameCount,
                scenes = Enumerable.Range(0, SceneManager.sceneCount).Select(i => new { SceneManager.GetSceneAt(i).path, SceneManager.GetSceneAt(i).isDirty }).ToArray() };
            if (action == "build") { OccWindowsPlayerBuild.BuildTo(output); return "built"; }
            if (action == "restore-build-serialization")
            {
                if(Application.isPlaying)throw new InvalidOperationException("Edit Mode required");
                var pipeline=AssetDatabase.LoadMainAssetAtPath("Assets/Settings/UniversalRP.asset");
                if(pipeline==null)throw new InvalidOperationException("Pipeline missing");
                var serialized=new SerializedObject(pipeline);
                if(serialized.FindProperty("m_SupportsLightLayers").boolValue)
                    throw new InvalidOperationException("Build override was not restored");
                if(EditorSettings.serializationMode!=SerializationMode.ForceText)
                    throw new InvalidOperationException("Unexpected project serialization policy");
                if(!string.IsNullOrEmpty(output))
                {
                    var before=Newtonsoft.Json.Linq.JObject.Parse(File.ReadAllText(output));
                    foreach(var value in ((Newtonsoft.Json.Linq.JObject)before["pipeline"]).Properties())
                        serialized.FindProperty(value.Name).intValue=(int)value.Value;
                    serialized.ApplyModifiedPropertiesWithoutUndo();
                    var global=AssetDatabase.LoadMainAssetAtPath("Assets/Settings/UniversalRenderPipelineGlobalSettings.asset");
                    var globalState=new SerializedObject(global);
                    var settings=globalState.FindProperty("m_Settings.m_SettingsList.m_List");
                    var objects=new System.Collections.Generic.Dictionary<long,object>();
                    for(int i=0;i<settings.arraySize;i++)
                    {var item=settings.GetArrayElementAtIndex(i);objects[item.managedReferenceId]=item.managedReferenceValue;}
                    var ids=before["runtimeIds"].Values<long>().ToArray();
                    if(ids.Any(id=>!objects.ContainsKey(id)))throw new InvalidOperationException("Original runtime setting reference missing");
                    var runtime=globalState.FindProperty("m_Settings.m_RuntimeSettings.m_List");runtime.arraySize=ids.Length;
                    for(int i=0;i<ids.Length;i++)runtime.GetArrayElementAtIndex(i).managedReferenceValue=objects[ids[i]];
                    globalState.ApplyModifiedPropertiesWithoutUndo();AssetDatabase.SaveAssetIfDirty(global);
                }
                // BuildPipeline temporarily writes binary data. Reserialize only
                // these two restored build inputs using the project's text policy.
                AssetDatabase.SaveAssetIfDirty(pipeline);
                AssetDatabase.ForceReserializeAssets(new[]{"Assets/Settings/UniversalRP.asset","Assets/Settings/UniversalRenderPipelineGlobalSettings.asset"},ForceReserializeAssetsOptions.ReserializeAssets);
                return "restored build inputs serialized with current text policy";
            }
            if (action == "tests")
            {
                if (Application.isPlaying || testApi != null) throw new InvalidOperationException("Idle Edit Mode required");
                testApi=ScriptableObject.CreateInstance<UnityEditor.TestTools.TestRunner.Api.TestRunnerApi>();
                testApi.RegisterCallbacks(new TestReceipt { Output=output });
                testApi.Execute(new UnityEditor.TestTools.TestRunner.Api.ExecutionSettings(new UnityEditor.TestTools.TestRunner.Api.Filter {
                    testMode=UnityEditor.TestTools.TestRunner.Api.TestMode.EditMode,
                    testNames=new[]{"OCC.Tests.UiReadingProjectionCacheTests","OCC.Tests.MainMenuDeskTransitionTests"}
                }));
                return "targeted tests started without opening a window";
            }
            if (!Application.isPlaying) throw new InvalidOperationException("Play mode required");
            Application.runInBackground = true;
            var flow = UnityEngine.Object.FindAnyObjectByType<FirstExperiencePrototypeController>();
            if (flow == null) throw new InvalidOperationException("Real scene entry missing");
            if (action == "landing") { flow.PreviewUiCaptureStage(FirstExperiencePrototypeController.FlowStage.Landing); return "landing"; }
            if (action == "settings") { flow.OpenSettings(); return "settings"; }
            if (action == "map")
            {
                if (scope == null) scope = FirstExperienceSaveRouting.BeginVerificationScope("ui-polish-" + DateTime.UtcNow.Ticks);
                FirstExperienceSaveRouting.SelectSlot(0);
                var run = RogueliteMapRun.CreateSubsequentAcademyRun(4311);
                run.ChooseOpeningGift(run.OpeningGift.CandidateIds.First(id => id != "LK-05" && id != "LK-06"));
                run.ConfirmAcademyDeparture();
                var gateway = new RogueliteSaveGateway(new PlayerPrefsRogueliteSaveStore());
                if (!gateway.SaveMapRun(run)) throw new InvalidOperationException(gateway.LastError);
                flow.enabled = false;
                if (!flow.GetComponent<CombatPrototypeBootstrap>().EnterFirstExperienceFromOpening(true)) throw new InvalidOperationException("Continue failed");
                return "isolated map restored through continue entry";
            }
            if (action == "loadout" || action == "spells" || action == "map-settings")
                return UnityEngine.Object.FindAnyObjectByType<FormalRogueliteUi>().PreviewUiCapturePage(action == "map-settings" ? "settings" : action);
            if (action == "capture")
            {
                Directory.CreateDirectory(Path.GetDirectoryName(output));
                var capture = typeof(CombatPrototypeBootstrap).Assembly.GetType("OCC.Combat.Presentation.OccPlayerRenderCapture").GetMethod("Capture", BindingFlags.Static | BindingFlags.NonPublic);
                capture.Invoke(null, new object[] { UnityEngine.Object.FindAnyObjectByType<Hd2DBattlefieldSample>(), 1920, 1080, output, true });
                capture.Invoke(null, new object[] { UnityEngine.Object.FindAnyObjectByType<Hd2DBattlefieldSample>(), 960, 540, Path.ChangeExtension(output, null) + "_960.png", true });
                return new { output, frame = Time.frameCount };
            }
            if (action == "measure")
            {
                var ui = UnityEngine.Object.FindAnyObjectByType<FormalFirstExperienceUi>();
                var update = (Action)Delegate.CreateDelegate(typeof(Action), ui, typeof(FormalFirstExperienceUi).GetMethod("Update", BindingFlags.Instance | BindingFlags.NonPublic));
                object Measure(Action work, int iterations)
                {
                    work();
                    long bytes = GC.GetAllocatedBytesForCurrentThread();
                    var clock = Stopwatch.StartNew();
                    for (int i = 0; i < iterations; i++) work();
                    clock.Stop();
                    return new { iterations, milliseconds = clock.Elapsed.TotalMilliseconds, bytes = GC.GetAllocatedBytesForCurrentThread() - bytes };
                }
                var reading = UnityEngine.Object.FindAnyObjectByType<Hd2DServiceReadingText>();
                var result = new { frame = Time.frameCount, stage = flow.DebugStage, environment = "Editor method microbenchmark, not player FPS",
                    frontUpdate = Measure(update, 2000), readingRefresh = reading == null ? null : Measure(reading.Refresh, 300),
                    textCount = UnityEngine.Object.FindObjectsByType<UnityEngine.UI.Text>().Length };
                if (!string.IsNullOrEmpty(output)) File.WriteAllText(output, Newtonsoft.Json.JsonConvert.SerializeObject(result, Newtonsoft.Json.Formatting.Indented));
                return result;
            }
            throw new ArgumentException("Unknown action: " + action);
        }
    }
}
