using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using UnityEngine;

namespace OCC.Combat.Presentation
{
    /// <summary>Only armed by the explicit offline delivery-check flag.</summary>
    public sealed class SkyFurnacePlayerProbe : MonoBehaviour
    {
        private SkyFurnaceDirector director;
        private string folder;
        private readonly List<string> errors=new List<string>();
        private Report report=new Report();
        [Serializable] private sealed class Report
        {
            public string status="RUNNING",scope="Standalone dedicated cinematic normal entry and public playback controls",device,version,dataPath;
            public bool fixedCamera,terrainDestruction=false,normalCompletePlayback,pauseResume,replay,skip,reset,resourceStable,focusRequested=false;
            public int brokenPieces,ownedResources;public string[] errors;
        }
        public static void TryAttach(SkyFurnaceDirector owner)
        {
            string[] args=Environment.GetCommandLineArgs();
            for(int i=0;i<args.Length-1;i++)if(args[i]=="-occ-furnace-check")
            {
                var probe=owner.gameObject.AddComponent<SkyFurnacePlayerProbe>();probe.director=owner;probe.folder=Path.GetFullPath(args[i+1]);
                Directory.CreateDirectory(probe.folder);return;
            }
        }
        private IEnumerator Start()
        {
            Application.logMessageReceived+=OnLog;
            report.device=SystemInfo.graphicsDeviceName;report.version=Application.unityVersion;report.dataPath=Application.dataPath;
            int initial=director.OwnedResourceCount;
            yield return new WaitForSecondsRealtime(.8f);
            Capture("player_start.png");
            float stopAt=Time.realtimeSinceStartup+35;
            while(director.Age<SkyFurnaceDirector.Duration&&Time.realtimeSinceStartup<stopAt)yield return null;
            report.normalCompletePlayback=director.Age>=SkyFurnaceDirector.Duration;report.fixedCamera=director.CameraLocked;report.brokenPieces=director.BrokenPieces;
            Capture("player_final.png");
            director.Replay();yield return new WaitForSecondsRealtime(.7f);
            director.SetPaused(true);float age=director.Age;
            yield return new WaitForSecondsRealtime(.35f);
            bool paused=Mathf.Abs(age-director.Age)<.001f&&director.PlayingAudioCount==0;
            director.SetPaused(false);yield return new WaitForSecondsRealtime(.3f);
            report.pauseResume=paused&&director.Age>age;
            director.Seek(8.1f);Capture("player_impact.png");
            for(int i=0;i<3;i++)director.Replay();
            report.replay=director.Age==0&&director.BrokenPieces==0;
            director.Skip();report.skip=director.Age==SkyFurnaceDirector.Duration&&director.BrokenPieces==0&&director.PlayingAudioCount==0;
            director.ResetStage();report.reset=director.Age==0&&director.BrokenPieces==0&&director.EffectLightIntensity<.001f&&director.PlayingAudioCount==0;
            report.ownedResources=director.OwnedResourceCount;report.resourceStable=initial==report.ownedResources;
            report.errors=errors.ToArray();report.status=report.fixedCamera&&report.normalCompletePlayback&&report.brokenPieces==0&&report.pauseResume&&report.replay&&report.skip&&report.reset&&report.resourceStable&&errors.Count==0?"PASS":"FAIL";
            File.WriteAllText(Path.Combine(folder,"player_checks.json"),JsonUtility.ToJson(report,true));
            Debug.Log("SKY_FURNACE_PLAYER_CHECK "+report.status);Application.logMessageReceived-=OnLog;Application.Quit(report.status=="PASS"?0:1);
        }
        private void OnLog(string message,string trace,LogType type)
        {if((type==LogType.Error||type==LogType.Exception||type==LogType.Assert)&&errors.Count<20)errors.Add(message+"\n"+trace);}
        private void Capture(string name)
        {
            var camera=director.RenderCamera;var previous=camera.targetTexture;var active=RenderTexture.active;bool gamma=GL.sRGBWrite;
            var rt=RenderTexture.GetTemporary(1920,1080,24,RenderTextureFormat.ARGBHalf);
            var output=RenderTexture.GetTemporary(1920,1080,0,RenderTextureFormat.ARGB32,RenderTextureReadWrite.sRGB);
            var texture=new Texture2D(1920,1080,TextureFormat.RGB24,false,false);
            try{camera.targetTexture=rt;camera.Render();GL.sRGBWrite=QualitySettings.activeColorSpace==ColorSpace.Linear;Graphics.Blit(rt,output);RenderTexture.active=output;texture.ReadPixels(new Rect(0,0,1920,1080),0,0);texture.Apply();File.WriteAllBytes(Path.Combine(folder,name),texture.EncodeToPNG());}
            finally{camera.targetTexture=previous;RenderTexture.active=active;GL.sRGBWrite=gamma;RenderTexture.ReleaseTemporary(rt);RenderTexture.ReleaseTemporary(output);Destroy(texture);}
        }
    }
}
