using System;
using System.IO;
using System.Linq;
using Newtonsoft.Json;
using OCC.Combat.Presentation;
using UnityEditor;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.Rendering.Universal;

namespace OCC.Combat.EditorTools
{
    // One real material rendered offscreen. Does not enter Play, create a window,
    // save a scene, drive UI, or substitute for an in-game scene approval.
    public static class Hd2DStoneMaterialProbe
    {
        [MenuItem("OCC/Tools/Render stone material probe (no focus)")]
        public static void Run()
        {
            if(Application.dataPath.Replace('\\','/')!="E:/数据库/OCC_Codex/UnityProject/Assets" || Application.isPlaying)
                throw new InvalidOperationException("Main OCC Edit Mode required");
            if(EditorApplication.isCompiling || EditorApplication.isUpdating)throw new InvalidOperationException("Editor busy");
            for(int i=0;i<SceneManager.sceneCount;i++)if(SceneManager.GetSceneAt(i).isDirty)throw new InvalidOperationException("Preserve dirty scene");
            string active=SceneManager.GetActiveScene().path;
            Configure(Hd2DCourtyardStoneMaps.NormalResource,true);Configure(Hd2DCourtyardStoneMaps.RoughnessResource,false);
            var albedo=Resources.Load<Texture2D>(Hd2DCourtyardStoneMaps.AlbedoResource);
            var shader=Shader.Find("OCC/HD2D/BakedArchitecture");
            if(albedo==null || shader==null)throw new InvalidOperationException("Missing stone material resources");
            var material=new Material(shader){mainTexture=albedo,color=new Color(.82f,.84f,.86f),hideFlags=HideFlags.HideAndDontSave};
            material.SetFloat("_Smoothness",0);
            var mesh=new Mesh{hideFlags=HideFlags.HideAndDontSave,vertices=new[]{new Vector3(-.5f,0,-.5f),new Vector3(-.5f,0,.5f),new Vector3(.5f,0,.5f),new Vector3(.5f,0,-.5f)},
                uv=new[]{Vector2.zero,Vector2.up,Vector2.one,Vector2.right},uv2=new[]{Vector2.right,Vector2.right,Vector2.right,Vector2.right},triangles=new[]{0,1,2,0,2,3}};
            mesh.RecalculateNormals();mesh.RecalculateTangents();mesh.RecalculateBounds();
            var variants=new ShaderVariantCollection();
            var preview=new PreviewRenderUtility();
            var surface=EditorUtility.CreateGameObjectWithHideFlags("Registered stone material probe",HideFlags.HideAndDontSave,typeof(MeshFilter),typeof(MeshRenderer));
            surface.GetComponent<MeshFilter>().sharedMesh=mesh;surface.GetComponent<MeshRenderer>().sharedMaterial=material;
            preview.AddSingleGO(surface);
            string folder=Path.GetFullPath(Path.Combine(Application.dataPath,"../../Worldbuilding/归档/2026-10-12_OCC石材配套贴图/probe_v4"));
            Directory.CreateDirectory(folder);
            try
            {
                foreach(var keys in new[]{Array.Empty<string>(),new[]{"_NORMALMAP"},new[]{"_OCC_ROUGHNESS_MAP"},new[]{"_NORMALMAP","_OCC_ROUGHNESS_MAP"}})
                    variants.Add(new ShaderVariantCollection.ShaderVariant(shader,UnityEngine.Rendering.PassType.ScriptableRenderPipeline,keys));
                variants.WarmUp();
                preview.camera.fieldOfView=30;preview.camera.nearClipPlane=.05f;preview.camera.farClipPlane=20;
                preview.camera.allowHDR=true;preview.camera.clearFlags=CameraClearFlags.SolidColor;
                preview.camera.cullingMask=-1;
                // URP ignores AdditionalCameraData for Preview cameras and takes
                // this project's default 2D renderer. This remains an offscreen
                // camera in the owned preview scene, with no Game View or Play.
                preview.camera.cameraType=CameraType.Game;
                // The project's default renderer is 2D; the real HD2D battlefield
                // uses renderer1. Use that same 3D renderer for the mesh probe.
                var cameraData=preview.camera.GetUniversalAdditionalCameraData();
                cameraData.SetRenderer(1);cameraData.renderPostProcessing=false;cameraData.volumeLayerMask=0;
                preview.camera.backgroundColor=new Color(.12f,.14f,.17f);
                preview.camera.transform.position=new Vector3(0,1.8f,-1.8f);preview.camera.transform.LookAt(Vector3.zero);
                preview.ambientColor=new Color(.12f,.12f,.12f);
                preview.lights[0].color=new Color(1,.94f,.84f);preview.lights[0].intensity=2.6f;
                preview.lights[1].color=new Color(.7f,.82f,1);preview.lights[1].intensity=.6f;
                preview.lights[1].transform.rotation=Quaternion.Euler(35,150,0);
                Capture("baseline_1080",false,false,1920,1080,50,-25);
                Capture("normal_only_1080",true,false,1920,1080,50,-25);
                Capture("roughness_only_1080",false,true,1920,1080,50,-25);
                Capture("full_1080",true,true,1920,1080,50,-25);
                Capture("rotated_light_1080",true,true,1920,1080,50,65);
                preview.lights[0].color=Color.white;preview.lights[1].color=Color.white;
                Capture("neutral_light_1080",true,true,1920,1080,50,-25);
                preview.lights[0].color=new Color(1,.94f,.84f);preview.lights[1].color=new Color(.7f,.82f,1);
                Capture("full_540",true,true,960,540,50,-25);
            }
            finally
            {
                preview.Cleanup();UnityEngine.Object.DestroyImmediate(material);UnityEngine.Object.DestroyImmediate(mesh);UnityEngine.Object.DestroyImmediate(variants);
            }
            var messages=ShaderUtil.GetShaderMessages(shader);
            var maps=new[]{Hd2DCourtyardStoneMaps.NormalResource,Hd2DCourtyardStoneMaps.RoughnessResource}.Select(p=>
            {
                var path="Assets/Game/Resources/"+p+".png";var importer=(TextureImporter)AssetImporter.GetAtPath(path);var t=AssetDatabase.LoadAssetAtPath<Texture2D>(path);
                return new{path,guid=AssetDatabase.AssetPathToGUID(path),t.width,t.height,filter=t.filterMode.ToString(),type=importer.textureType.ToString(),importer.sRGBTexture,importer.mipmapEnabled,compression=importer.textureCompression.ToString()};
            }).ToArray();
            File.WriteAllText(Path.Combine(folder,"result.json"),JsonConvert.SerializeObject(new{
                scope="single native32 material probe, actual URP offscreen rendering; not normal-entry game/UI or performance",dataPath=Application.dataPath,
                activeScenePreserved=SceneManager.GetActiveScene().path==active,dirty=SceneManager.GetActiveScene().isDirty,play=Application.isPlaying,
                normalStrength=Hd2DCourtyardStoneMaps.NormalStrength,maps,
                captures=Directory.GetFiles(folder,"*.png").Select(Path.GetFileName).ToArray(),
                messages=messages.Select(m=>new{m.message,severity=m.severity.ToString()}).ToArray()},Formatting.Indented));
            if(messages.Any(m=>m.severity==UnityEditor.Rendering.ShaderCompilerMessageSeverity.Error))throw new InvalidOperationException("Stone shader errors");

            void Capture(string name,bool normal,bool roughness,int width,int height,float pitch,float yaw)
            {
                Hd2DCourtyardStoneMaps.Apply(material,albedo);
                if(!normal)material.DisableKeyword("_NORMALMAP");
                if(!roughness)material.DisableKeyword("_OCC_ROUGHNESS_MAP");
                preview.lights[0].transform.rotation=Quaternion.Euler(pitch,yaw,0);
                preview.BeginStaticPreview(new Rect(0,0,width,height));
                preview.Render(true,false);
                var capture=preview.EndStaticPreview();
                try{File.WriteAllBytes(Path.Combine(folder,name+".png"),capture.EncodeToPNG());}
                finally{UnityEngine.Object.DestroyImmediate(capture);}
            }
        }
        private static void Configure(string resource,bool normal)
        {
            string path="Assets/Game/Resources/"+resource+".png";
            var importer=AssetImporter.GetAtPath(path) as TextureImporter;
            if(importer==null)throw new InvalidOperationException("Map not imported: "+path);
            importer.textureType=normal?TextureImporterType.NormalMap:TextureImporterType.Default;
            importer.sRGBTexture=false;importer.alphaIsTransparency=false;importer.mipmapEnabled=false;
            importer.filterMode=FilterMode.Point;importer.wrapMode=TextureWrapMode.Repeat;importer.npotScale=TextureImporterNPOTScale.None;
            importer.textureCompression=TextureImporterCompression.Uncompressed;
            // Observed source rim: top G65, bottom G186. Convert its image-down Y
            // to Unity tangent V; preserve the authored PNG and X channel.
            if(normal){importer.convertToNormalmap=false;importer.flipGreenChannel=true;}
            importer.SaveAndReimport();
        }
    }
}
