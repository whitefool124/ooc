using System;
using System.IO;
using System.Collections.Generic;
using OCC.Combat.Presentation;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.Rendering.Universal;

public static class SkyFurnaceAuthoring
{
    public const string ScenePath="Assets/Scenes/SkyFurnace.unity";
    [MenuItem("OCC/Sky Furnace/Inspect State")]
    public static void Inspect()
    {
        Debug.Log("SKY_FURNACE_STATE "+JsonUtility.ToJson(new State{dataPath=Application.dataPath,playing=Application.isPlaying,compiling=EditorApplication.isCompiling,scene=SceneManager.GetActiveScene().path,dirty=SceneManager.GetActiveScene().isDirty}));
    }
    [Serializable] private sealed class State {public string dataPath,scene;public bool playing,compiling,dirty;}
    [MenuItem("OCC/Sky Furnace/Create Dedicated Scene")]
    public static void Create()
    {
        if(Application.isPlaying)throw new InvalidOperationException("Stop Play before authoring.");
        if(Path.GetFullPath(Application.dataPath).Replace('\\','/')!="E:/数据库/OCC_Codex/UnityProject/Assets")throw new InvalidOperationException("Wrong OCC checkout.");
        for(int i=0;i<SceneManager.sceneCount;i++)if(SceneManager.GetSceneAt(i).isDirty)throw new InvalidOperationException("Unsaved scene: "+SceneManager.GetSceneAt(i).path);
        if(File.Exists(ScenePath))throw new InvalidOperationException("Dedicated scene already exists; inspect rather than overwrite.");
        var scene=EditorSceneManager.NewScene(NewSceneSetup.EmptyScene,NewSceneMode.Single);
        var obj=new GameObject("天穹熔炉导演");Undo.RegisterCreatedObjectUndo(obj,"Create Sky Furnace");
        var director=Undo.AddComponent<SkyFurnaceDirector>(obj);
        director.EnergyShader=Require("SkyFurnaceEnergy");director.SmokeShader=Require("SkyFurnaceCloud");director.HeatShader=Require("SkyFurnaceHeat");
        EditorSceneManager.SaveScene(scene,ScenePath);
        Debug.Log("SKY_FURNACE_SAVED "+ScenePath);
    }
    private static Shader Require(string name)
    {var s=AssetDatabase.LoadAssetAtPath<Shader>("Assets/Game/Runtime/Presentation/Shaders/"+name+".shader");if(s==null)throw new MissingReferenceException(name);return s;}
    [MenuItem("OCC/Sky Furnace/Apply Pixel Assets")]
    public static void ApplyPixelAssets()
    {
        if(Application.isPlaying||SceneManager.GetActiveScene().path!=ScenePath)throw new InvalidOperationException("Dedicated idle scene required");
        var d=UnityEngine.Object.FindFirstObjectByType<SkyFurnaceDirector>();if(d==null)throw new MissingReferenceException("Director missing");
        foreach(string name in new[]{"pyromancer_cast","smoke_wisp_v2","magic_card_outer","magic_card_sigils","magic_card_core","magic_card_caster"})
        {
            string path="Assets/Game/Resources/Art/SkyFurnace/"+name+".png";
            var importer=AssetImporter.GetAtPath(path) as TextureImporter;if(importer==null)throw new MissingReferenceException(path);
            importer.textureType=TextureImporterType.Default;importer.filterMode=FilterMode.Point;importer.wrapMode=TextureWrapMode.Clamp;
            importer.isReadable=true;importer.mipmapEnabled=false;importer.textureCompression=TextureImporterCompression.Uncompressed;importer.alphaSource=TextureImporterAlphaSource.FromInput;importer.alphaIsTransparency=true;importer.SaveAndReimport();
        }
        Undo.RecordObject(d,"Apply native32 Sky Furnace layers");d.PixelLayerShader=Require("SkyFurnacePixelLayer");EditorUtility.SetDirty(d);
        const string cutoutPath="Assets/Game/Resources/Art/SkyFurnace/sky_furnace_cutout.mat";
        var cutout=AssetDatabase.LoadAssetAtPath<Material>(cutoutPath);
        if(cutout==null)
        {
            var shader=AssetDatabase.LoadAssetAtPath<Shader>("Assets/Game/Resources/Shaders/Hd2DGroundedSprite.shader");
            if(shader==null)throw new MissingReferenceException("Grounded sprite shader missing");
            cutout=new Material(shader){name="Sky Furnace persistent alpha-test variant"};
            cutout.SetFloat("_Cutoff",.5f);cutout.SetFloat("_Cull",0);cutout.SetFloat("_ReceiveShadows",0);
            cutout.EnableKeyword("_ALPHATEST_ON");cutout.EnableKeyword("_RECEIVE_SHADOWS_OFF");
            AssetDatabase.CreateAsset(cutout,cutoutPath);
        }
        d.CutoutTemplate=cutout;AssetDatabase.SaveAssetIfDirty(cutout);
        const string litPath="Assets/Game/Resources/Art/SkyFurnace/sky_furnace_vfx_lit_card.mat";
        var lit=AssetDatabase.LoadAssetAtPath<Material>(litPath);
        if(lit==null){lit=new Material(Require("Hd2DVfxLitCard")){name="Sky Furnace shadow receiving pixel card"};AssetDatabase.CreateAsset(lit,litPath);}
        d.VfxLitCardTemplate=lit;
        const string recipePath="Assets/Game/Resources/Art/SkyFurnace/sky_furnace_fire_light.asset";
        var recipe=AssetDatabase.LoadAssetAtPath<Hd2DFireLightRecipe>(recipePath);
        if(recipe==null){recipe=ScriptableObject.CreateInstance<Hd2DFireLightRecipe>();AssetDatabase.CreateAsset(recipe,recipePath);}
        d.FireLightRecipe=recipe;EditorUtility.SetDirty(recipe);AssetDatabase.SaveAssetIfDirty(recipe);
        d.VoxelSurfaceShader=Require("Hd2DVfxVoxelSurface");
        var voxelTexture=AssetImporter.GetAtPath("Assets/Game/Resources/Art/SkyFurnace/quiet_energy_pixel.png") as TextureImporter;
        if(voxelTexture==null)throw new MissingReferenceException("Voxel material artwork missing");
        voxelTexture.textureType=TextureImporterType.Default;voxelTexture.filterMode=FilterMode.Point;voxelTexture.wrapMode=TextureWrapMode.Clamp;voxelTexture.mipmapEnabled=false;voxelTexture.textureCompression=TextureImporterCompression.Uncompressed;voxelTexture.SaveAndReimport();
        EditorSceneManager.MarkSceneDirty(SceneManager.GetActiveScene());EditorSceneManager.SaveScene(SceneManager.GetActiveScene());
        Debug.Log("SKY_FURNACE_PIXEL_ASSETS_APPLIED");
    }
    [MenuItem("OCC/Sky Furnace/Capture Key Frames")]
    public static void Capture()
    {
        var d=UnityEngine.Object.FindFirstObjectByType<SkyFurnaceDirector>();if(d==null||!Application.isPlaying)throw new InvalidOperationException("Sky Furnace must be running.");
        string root=Path.GetFullPath(Application.dataPath+"/../../Worldbuilding/归档/2026-10-15_天穹熔炉自审修正/fixed_frames");Directory.CreateDirectory(root);
        bool controls=d.ShowControls;d.ShowControls=false;
        foreach(float age in new[]{.8f,2.5f,4.5f,6.5f,7.35f,8.1f,10f,12.5f,15.8f})
        {d.Seek(age);Render(d.RenderCamera,1920,1080,Path.Combine(root,"frame_"+age.ToString("00.00",System.Globalization.CultureInfo.InvariantCulture)+".png"));}
        d.ShowControls=controls;d.ResetStage();Debug.Log("SKY_FURNACE_KEYFRAMES "+root);
    }
    [MenuItem("OCC/Sky Furnace/Capture Ignition Study")]
    public static void CaptureIgnitionStudy()
    {
        var d=UnityEngine.Object.FindFirstObjectByType<SkyFurnaceDirector>();if(d==null||!Application.isPlaying)throw new InvalidOperationException("Running furnace required");
        string root=Path.GetFullPath(Application.dataPath+"/../../Worldbuilding/归档/2026-10-15_天穹熔炉自审修正/ignition_study");Directory.CreateDirectory(root);var records=new List<object>();
        bool controls=d.ShowControls;d.ShowControls=false;
        foreach(float t in new[]{1.6f,3.4f,5.5f,6.9f}){d.Seek(t);Render(d.RenderCamera,1920,1080,Path.Combine(root,t.ToString("00.00",System.Globalization.CultureInfo.InvariantCulture)+".png"));records.Add(new{time=t,ignition=d.IgnitionAudit()});}
        File.WriteAllText(Path.Combine(root,"progression.json"),Newtonsoft.Json.JsonConvert.SerializeObject(records,Newtonsoft.Json.Formatting.Indented));d.ShowControls=controls;d.ResetStage();
    }
    [MenuItem("OCC/Sky Furnace/Capture Lighting Comparison")]
    public static void CaptureLighting()
    {
        var d=UnityEngine.Object.FindFirstObjectByType<SkyFurnaceDirector>();if(d==null||!Application.isPlaying)throw new InvalidOperationException("Running furnace required");
        string root=Path.GetFullPath(Application.dataPath+"/../../Worldbuilding/归档/2026-10-15_天穹熔炉自审修正");Directory.CreateDirectory(root);
        bool controls=d.ShowControls;d.ShowControls=false;
        foreach(float age in new[]{4.5f,6.5f,7.5f,8.1f,10f,12.5f})
        {
            string stamp=age.ToString("00.00",System.Globalization.CultureInfo.InvariantCulture);
            d.EnableEffectLighting=true;d.EnableActorShadow=true;d.Seek(age);Render(d.RenderCamera,1920,1080,Path.Combine(root,stamp+"_光影.png"));
            if(Mathf.Abs(age-8.1f)<.01f){Render(d.RenderCamera,1920,1080,Path.Combine(root,"fixed_comparison_1920x1080.png"));Render(d.RenderCamera,960,540,Path.Combine(root,"fixed_comparison_960x540.png"));}
            d.EnableActorShadow=false;d.Seek(age);Render(d.RenderCamera,1920,1080,Path.Combine(root,stamp+"_无角色影.png"));
            d.EnableEffectLighting=false;d.Seek(age);Render(d.RenderCamera,1920,1080,Path.Combine(root,stamp+"_无特效灯.png"));
        }
        d.EnableEffectLighting=true;d.EnableActorShadow=true;d.ShowControls=controls;d.ResetStage();
        Debug.Log("SKY_FURNACE_LIGHTING_COMPARISON "+root);
    }
    [MenuItem("OCC/Sky Furnace/Capture Static Model Views")]
    public static void CaptureModelViews()
    {
        var d=UnityEngine.Object.FindFirstObjectByType<SkyFurnaceDirector>();if(d==null||!Application.isPlaying)throw new InvalidOperationException("Running furnace required");
        string root=Path.GetFullPath(Application.dataPath+"/../../Worldbuilding/归档/2026-10-15_天穹熔炉自审修正/model_views");Directory.CreateDirectory(root);
        var node=new GameObject("临时静态模型观察相机");var camera=node.AddComponent<Camera>();camera.CopyFrom(d.RenderCamera);camera.enabled=false;
        var data=camera.GetUniversalAdditionalCameraData();data.SetRenderer(1);data.renderPostProcessing=true;data.volumeLayerMask=1<<29;data.requiresColorTexture=true;data.requiresDepthTexture=true;
        try
        {
            foreach(float age in new[]{4.5f,6.5f,8.1f})foreach(int yaw in new[]{0,90,180,270})
            {
                d.Seek(age);var rotation=Quaternion.Euler(45,yaw,0);camera.transform.SetPositionAndRotation(new Vector3(0,.35f,.3f)-rotation*Vector3.forward*13.2f,rotation);
                Render(camera,1920,1080,Path.Combine(root,age.ToString("00.00",System.Globalization.CultureInfo.InvariantCulture)+"_view_"+yaw+".png"));
                if(!d.CameraLocked)throw new InvalidOperationException("Main camera was modified by model inspection");
            }
            File.WriteAllText(Path.Combine(root,"ignition_checks.json"),Newtonsoft.Json.JsonConvert.SerializeObject(d.IgnitionAudit(),Newtonsoft.Json.Formatting.Indented));
            var evidence=d.PrimitiveAudit();string json=Newtonsoft.Json.JsonConvert.SerializeObject(evidence,Newtonsoft.Json.Formatting.Indented);
            File.WriteAllText(Path.Combine(root,"model_checks.json"),json);Debug.Log("SKY_FURNACE_PRIMITIVE_AUDIT "+json);
            if(!Newtonsoft.Json.Linq.JObject.Parse(json).Value<bool>("technicalPass"))throw new InvalidOperationException("Effect primitive contract failed");
        }
        finally{UnityEngine.Object.DestroyImmediate(node);d.ResetStage();}
    }
    [Serializable] private sealed class ModelEvidence{public int meshes,vertices,invalidTriangles,emptyMeshes,groundGlyphMeshes;public float worldPitch,maximumGridError,maximumUvDensityError;public bool mainCameraLocked,technicalPass;public string aestheticReview="PENDING",scope="Voxel models and ground glyphs, four auxiliary static views; playback camera never moved";}
    public static void Render(Camera camera,int width,int height,string path)
    {
        var previous=camera.targetTexture;var old=RenderTexture.active;
        var rt=RenderTexture.GetTemporary(width,height,24,RenderTextureFormat.ARGBHalf);
        var output=RenderTexture.GetTemporary(width,height,0,RenderTextureFormat.ARGB32,RenderTextureReadWrite.sRGB);
        var tex=new Texture2D(width,height,TextureFormat.RGB24,false,false);bool gamma=GL.sRGBWrite;
        try{camera.targetTexture=rt;camera.Render();GL.sRGBWrite=QualitySettings.activeColorSpace==ColorSpace.Linear;Graphics.Blit(rt,output);RenderTexture.active=output;tex.ReadPixels(new Rect(0,0,width,height),0,0);tex.Apply();File.WriteAllBytes(path,tex.EncodeToPNG());}
        finally{camera.targetTexture=previous;RenderTexture.active=old;GL.sRGBWrite=gamma;RenderTexture.ReleaseTemporary(rt);RenderTexture.ReleaseTemporary(output);UnityEngine.Object.DestroyImmediate(tex);}
    }
}
