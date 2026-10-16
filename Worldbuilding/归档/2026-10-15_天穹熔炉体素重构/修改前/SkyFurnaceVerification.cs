using System;
using System.Collections.Generic;
using System.IO;
using Newtonsoft.Json;
using OCC.Combat.Presentation;
using UnityEditor;
using UnityEngine;
using UnityEngine.Rendering;

// Offscreen local checks and cinematic export. Never opens or focuses a window.
[InitializeOnLoad]
public static class SkyFurnaceVerification
{
    private static SkyFurnaceDirector director;
    private static string mode;
    private static int phase,frame,resources;
    private static float pausedAge;
    private static double deadline,waitAt;
    private static readonly List<object> evidence=new List<object>();
    private static string Folder=>Path.GetFullPath(Application.dataPath+"/../../Worldbuilding/归档/2026-10-14_天穹熔炉演出方案/光影迭代v2");
    static SkyFurnaceVerification(){EditorApplication.update+=Tick;}
    private static SkyFurnaceDirector Require()
    {
        if(!Application.isPlaying||Application.dataPath.Replace('\\','/')!="E:/数据库/OCC_Codex/UnityProject/Assets")throw new InvalidOperationException("Main OCC Play Mode required");
        var d=UnityEngine.Object.FindFirstObjectByType<SkyFurnaceDirector>();if(d==null||d.RenderCamera==null)throw new MissingReferenceException("Running Sky Furnace missing");return d;
    }
    [MenuItem("OCC/Sky Furnace/Verify Complete Playback")]
    public static void BeginChecks()
    {
        if(mode!=null)throw new InvalidOperationException("A Sky Furnace operation is already active");
        director=Require();resources=director.OwnedResourceCount;phase=0;evidence.Clear();Directory.CreateDirectory(Folder);
        director.Replay();mode="verify";deadline=EditorApplication.timeSinceStartup+60;
        Debug.Log("SKY_FURNACE_VERIFY_STARTED normal16s fixed-camera runtime playback");
    }
    [MenuItem("OCC/Sky Furnace/Export Continuous Film")]
    public static void BeginFilm()
    {
        if(mode!=null)throw new InvalidOperationException("A Sky Furnace operation is already active");
        director=Require();director.ShowControls=false;frame=0;mode="film";
        Directory.CreateDirectory(Path.Combine(Folder,"fixed_film_frames"));deadline=EditorApplication.timeSinceStartup+600;
        Debug.Log("SKY_FURNACE_FILM_STARTED 1920x1080 30fps timeline export");
    }
    [MenuItem("OCC/Sky Furnace/Operation Status")]
    public static void Status()
    {Debug.Log("SKY_FURNACE_OPERATION "+JsonConvert.SerializeObject(new{mode,phase,frame,age=director!=null?director.Age:0}));}
    private static void Assert(bool value,string message){if(!value)throw new InvalidOperationException(message);}
    private static void Tick()
    {
        if(mode==null)return;
        try
        {
            if(director==null||!Application.isPlaying)throw new InvalidOperationException("Playback interrupted");
            if(EditorApplication.timeSinceStartup>deadline)throw new TimeoutException("Sky Furnace operation exceeded its local deadline");
            EditorApplication.QueuePlayerLoopUpdate();
            if(mode=="film")
            {
                // Fixed timeline sampling of the real runtime scene, with real
                // occlusion, lighting and post effects. This is an engine film
                // export; it is not an FPS benchmark or a human-input recording.
                director.Seek(frame/30f);
                SkyFurnaceAuthoring.Render(director.RenderCamera,1920,1080,Path.Combine(Folder,"fixed_film_frames",frame.ToString("0000")+".png"));
                frame++;
                if(frame>Mathf.RoundToInt(SkyFurnaceDirector.Duration*30))
                {
                    File.WriteAllText(Path.Combine(Folder,"fixed_film_capture.json"),JsonConvert.SerializeObject(new{schema="occ-cinematic-capture-v1",status="COMPLETE",frames=frame,fps=30,resolution=new[]{1920,1080},duration=SkyFurnaceDirector.Duration,
                        source="Unity Play Mode camera rendered at every fixed timeline frame; offscreen engine film export",focusRequested=false,performanceBenchmark=false,audio="synchronized16-second music and SFX stems"},Formatting.Indented));
                    director.ShowControls=true;director.ResetStage();mode=null;Debug.Log("SKY_FURNACE_FILM_COMPLETE "+frame);
                }
                return;
            }
            float age=director.Age;
            if(phase==0)
            {
                int sample=evidence.Count;
                float[] times={.8f,4.5f,6.5f,7.35f,8.1f,12.5f,15.8f};
                if(sample<times.Length&&age>=times[sample])
                {
                    string path=Path.Combine(Folder,"fixed_live_"+sample+".png");
                    SkyFurnaceAuthoring.Render(director.RenderCamera,1920,1080,path);
                    evidence.Add(new{age,director.BrokenPieces,director.PlayingAudioCount,path});
                }
                Assert(director.CameraLocked,"Camera moved during playback");
                if(age<SkyFurnaceDirector.Duration)return;
                Assert(director.BrokenPieces==0,"Unexpected destruction in spell-only playback");
                Assert(director.PlayingAudioCount==0,"Audio remains after normal ending");
                director.Replay();phase=1;return;
            }
            if(phase==1)
            {
                if(age<.7f)return;
                pausedAge=age;director.SetPaused(true);waitAt=EditorApplication.timeSinceStartup+.35;phase=2;return;
            }
            if(phase==2)
            {
                if(EditorApplication.timeSinceStartup<waitAt)return;
                Assert(Mathf.Abs(director.Age-pausedAge)<.0001f,"Pause did not freeze stage clock");
                Assert(director.PlayingAudioCount==0,"Pause did not pause audio");
                director.SetPaused(false);deadline=EditorApplication.timeSinceStartup+10;phase=3;return;
            }
            if(phase==3)
            {
                if(director.Age<pausedAge+.25f)return;
                for(int i=0;i<3;i++){director.Replay();director.Seek(10);}
                Assert(director.OwnedResourceCount==resources,"Replay grew owned resources");
                director.Skip();Assert(director.Age==SkyFurnaceDirector.Duration&&director.BrokenPieces==0&&director.CameraLocked&&director.PlayingAudioCount==0,"Skip did not produce a silent completed stage");
                director.ResetStage();Assert(director.Age==0&&director.BrokenPieces==0&&director.PlayingAudioCount==0&&director.EffectLightIntensity<.0001f,"Reset left effect state");
                var ownerPipeline=QualitySettings.renderPipeline;
                director.enabled=false;Assert(!director.RenderCamera.gameObject.activeInHierarchy&&director.PlayingAudioCount==0,"Disable left camera or sound active");
                Assert(QualitySettings.renderPipeline!=ownerPipeline,"Disable left scene-owned pipeline active");
                director.enabled=true;Assert(director.OwnedResourceCount==resources,"Re-enable grew resources");
                director.Seek(8.1f);SkyFurnaceAuthoring.Render(director.RenderCamera,960,540,Path.Combine(Folder,"fixed_comparison_960x540.png"));
                SkyFurnaceAuthoring.Render(director.RenderCamera,1920,1080,Path.Combine(Folder,"fixed_comparison_1920x1080.png"));director.ResetStage();
                File.WriteAllText(Path.Combine(Folder,"fixed_runtime_checks.json"),JsonConvert.SerializeObject(new{schema="occ-cinematic-local-checks-v1",status="PASS",duration=SkyFurnaceDirector.Duration,stageCells=new[]{7,7},actor="pyromancer",
                    normalCompletePlayback=true,fixedCamera=true,terrainDestruction=false,pauseResume=true,replayThreeTimes=true,skipEnd=true,reset=true,disableReenable=true,pipelineRestored=true,ownedResources=resources,resourceGrowth=0,evidence,
                    scope="One dedicated cinematic, normal scene initialization plus public playback controls; no full game journey or performance audit",humanAestheticReview="PENDING",focusRequested=false},Formatting.Indented));
                mode=null;Debug.Log("SKY_FURNACE_VERIFY_PASS");
            }
        }
        catch(Exception error)
        {
            string operation=mode;mode=null;
            File.WriteAllText(Path.Combine(Folder,"operation_failure.json"),JsonConvert.SerializeObject(new{operation,error=error.ToString()},Formatting.Indented));Debug.LogException(error);
        }
    }
}
