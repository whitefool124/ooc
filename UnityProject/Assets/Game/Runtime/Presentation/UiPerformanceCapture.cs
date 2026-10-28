using System;
using System.Collections.Generic;
using System.IO;
using UnityEngine;
using UnityEngine.Profiling;

namespace OCC.Combat.Presentation
{
    // Opt-in standalone observation. No save writes, camera changes or UI input.
    public sealed class UiPerformanceCapture : MonoBehaviour
    {
        private string output;
        private readonly List<float> frameMs = new List<float>(8192);
        private readonly List<double> cpuMs = new List<double>(8192), gpuMs = new List<double>(8192);
        private readonly FrameTiming[] timing = new FrameTiming[1];
        private float began;
        private float duration;
        private long peakMemory;
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
        private static void Attach()
        {
            if (Application.isEditor) return;
            var args=Environment.GetCommandLineArgs();
            for(int i=0;i<args.Length-1;i++) if(args[i]=="-aether-ui-metrics")
            {
                var go=new GameObject("Explicit UI performance observation");DontDestroyOnLoad(go);
                var probe=go.AddComponent<UiPerformanceCapture>();probe.output=Path.GetFullPath(args[i+1]);
                for(int j=0;j<args.Length-1;j++)if(args[j]=="-aether-metrics-duration")
                    float.TryParse(args[j+1],System.Globalization.NumberStyles.Float,System.Globalization.CultureInfo.InvariantCulture,out probe.duration);
                probe.began=Time.realtimeSinceStartup;return;
            }
        }
        private void Update()
        {
            FrameTimingManager.CaptureFrameTimings();
            if(Time.realtimeSinceStartup-began<5f)return;
            frameMs.Add(Time.unscaledDeltaTime*1000f);
            if(FrameTimingManager.GetLatestTimings(1,timing)>0)
            {
                if(timing[0].cpuMainThreadFrameTime>0)cpuMs.Add(timing[0].cpuMainThreadFrameTime);
                if(timing[0].gpuFrameTime>0)gpuMs.Add(timing[0].gpuFrameTime);
            }
            if(Time.frameCount%60==0)peakMemory=Math.Max(peakMemory,Profiler.GetTotalAllocatedMemoryLong());
            if(duration>0&&Time.realtimeSinceStartup-began>=duration)Application.Quit();
        }
        private void OnApplicationQuit()
        {
            frameMs.Sort();cpuMs.Sort();gpuMs.Sort();
            Directory.CreateDirectory(Path.GetDirectoryName(output));
            File.WriteAllText(output,Newtonsoft.Json.JsonConvert.SerializeObject(new {
                environment=duration>0?"Standalone hidden batch Player; normal entry idle window after 5s warm-up; not an onscreen FPS certification":
                    "Standalone hidden batch Player; includes navigation and synchronous screenshot encoding; not an onscreen steady-state FPS certification",
                device=SystemInfo.graphicsDeviceName,cpu=SystemInfo.processorType,resolution=new[]{Screen.width,Screen.height},
                quality=QualitySettings.names[QualitySettings.GetQualityLevel()],frames=frameMs.Count,
                medianFrameMs=frameMs.Count==0?0:frameMs[frameMs.Count/2],
                p95FrameMs=frameMs.Count==0?0:frameMs[(int)((frameMs.Count-1)*.95f)],
                maxFrameMs=frameMs.Count==0?0:frameMs[frameMs.Count-1],
                cpuMainThreadMedianMs=cpuMs.Count==0?(double?)null:cpuMs[cpuMs.Count/2],
                gpuMedianMs=gpuMs.Count==0?(double?)null:gpuMs[gpuMs.Count/2],peakAllocatedMb=peakMemory/1048576.0,
                focusRequested=false,saveWrites=false
            },Newtonsoft.Json.Formatting.Indented));
        }
    }
}
