using System;
using System.IO;
using System.Linq;
using Newtonsoft.Json;
using OCC.Combat.Presentation;
using UnityEditor;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;

namespace OCC.Combat.EditorTools
{
    public static class Hd2DGroundAttachmentProbe
    {
        private static bool busy;
        private static string Folder=>Path.GetFullPath(Path.Combine(Application.dataPath,"../../Worldbuilding/归档/2026-10-12_OCC战斗连续提升/ground_probe"));
        [MenuItem("OCC/Tools/Render ground attachment batch (no focus)")]
        public static void RunAll()
        {
            Guard();if(busy)throw new InvalidOperationException("Ground batch active");
            busy=true;int index=0;Directory.CreateDirectory(Folder);Write("running",0,null);
            EditorApplication.delayCall+=Step;
            void Step()
            {
                try
                {
                    Guard();var entry=Hd2DGroundAttachmentMaps.Entries[index];Run(entry);index++;
                    if(index<Hd2DGroundAttachmentMaps.Entries.Count){Write("running",index,null);EditorApplication.delayCall+=Step;return;}
                    Write("completed",index,null);busy=false;
                }
                catch(Exception e){Write("failed",index,e.ToString());busy=false;Debug.LogException(e);}
            }
        }
        private static void Write(string status,int count,string error)=>File.WriteAllText(Path.Combine(Folder,"batch_state.json"),JsonConvert.SerializeObject(new{status,count,error,dataPath=Application.dataPath,play=Application.isPlaying,dirty=SceneManager.GetActiveScene().isDirty},Formatting.Indented));
        private static void Guard()
        {
            if(Application.dataPath.Replace('\\','/')!="E:/数据库/OCC_Codex/UnityProject/Assets"||Application.isPlaying||EditorApplication.isCompiling||EditorApplication.isUpdating)
                throw new InvalidOperationException("Main idle Edit Mode required");
            for(int i=0;i<SceneManager.sceneCount;i++)if(SceneManager.GetSceneAt(i).isDirty)throw new InvalidOperationException("Preserve dirty scene");
        }
        private static void Configure(string resource,bool normal)
        {
            var importer=AssetImporter.GetAtPath("Assets/Game/Resources/"+resource+".png") as TextureImporter;
            if(importer==null)throw new InvalidOperationException("Missing numeric field importer: "+resource);
            importer.textureType=normal?TextureImporterType.NormalMap:TextureImporterType.Default;
            importer.sRGBTexture=false;importer.alphaIsTransparency=false;importer.mipmapEnabled=false;
            importer.filterMode=FilterMode.Point;importer.wrapMode=TextureWrapMode.Clamp;
            importer.npotScale=TextureImporterNPOTScale.None;importer.textureCompression=TextureImporterCompression.Uncompressed;
            if(normal){importer.convertToNormalmap=false;importer.flipGreenChannel=false;}
            importer.SaveAndReimport();
        }
        private static void Run(Hd2DGroundAttachmentMaps.Entry entry)
        {
            Configure(entry.MapRoot+"normal",true);Configure(entry.MapRoot+"surface",false);
            var albedo=Resources.Load<Texture2D>(entry.AlbedoResource);var shader=Shader.Find("OCC/HD2D/BakedArchitecture");
            if(albedo==null||shader==null)throw new InvalidOperationException("Missing registered source or shader: "+entry.Id);
            var material=new Material(shader){mainTexture=albedo,color=Color.white,hideFlags=HideFlags.HideAndDontSave};
            material.SetFloat("_AlphaClip",1);material.SetFloat("_Cutoff",.5f);material.EnableKeyword("_ALPHATEST_ON");
            var mesh=new Mesh{hideFlags=HideFlags.HideAndDontSave,vertices=new[]{new Vector3(-.5f,0,-.5f),new Vector3(-.5f,0,.5f),new Vector3(.5f,0,.5f),new Vector3(.5f,0,-.5f)},uv=new[]{Vector2.zero,Vector2.up,Vector2.one,Vector2.right},uv2=new[]{Vector2.right,Vector2.right,Vector2.right,Vector2.right},triangles=new[]{0,1,2,0,2,3}};
            mesh.RecalculateNormals();mesh.RecalculateTangents();mesh.RecalculateBounds();
            var surface=EditorUtility.CreateGameObjectWithHideFlags("Authored ground field probe",HideFlags.HideAndDontSave,typeof(MeshFilter),typeof(MeshRenderer));
            surface.GetComponent<MeshFilter>().sharedMesh=mesh;surface.GetComponent<MeshRenderer>().sharedMaterial=material;
            var preview=new PreviewRenderUtility();preview.AddSingleGO(surface);
            var variants=new ShaderVariantCollection();string folder=Path.Combine(Folder,entry.Id);Directory.CreateDirectory(folder);
            try
            {
                foreach(var keys in new[]{new[]{"_ALPHATEST_ON"},new[]{"_OCC_GROUND_DATA","_ALPHATEST_ON"},new[]{"_NORMALMAP","_OCC_GROUND_DATA","_ALPHATEST_ON"}})
                    variants.Add(new ShaderVariantCollection.ShaderVariant(shader,PassType.ScriptableRenderPipeline,keys));
                variants.WarmUp();preview.camera.cameraType=CameraType.Game;preview.camera.fieldOfView=30;
                preview.camera.nearClipPlane=.05f;preview.camera.farClipPlane=20;preview.camera.allowHDR=true;
                preview.camera.backgroundColor=new Color(.12f,.14f,.17f);preview.camera.clearFlags=CameraClearFlags.SolidColor;
                var data=preview.camera.GetUniversalAdditionalCameraData();data.SetRenderer(1);data.renderPostProcessing=false;data.volumeLayerMask=0;
                preview.camera.transform.position=new Vector3(0,1.8f,-1.8f);preview.camera.transform.LookAt(Vector3.zero);
                preview.ambientColor=new Color(.12f,.12f,.12f);preview.lights[0].intensity=2.6f;
                preview.lights[0].color=new Color(1,.94f,.84f);preview.lights[1].intensity=.6f;
                preview.lights[1].color=new Color(.7f,.82f,1);preview.lights[1].transform.rotation=Quaternion.Euler(35,150,0);
                foreach(var size in new[]{new Vector2Int(1920,1080),new Vector2Int(960,540)})
                    foreach(string state in new[]{"baseline","surface_only","full","moved_light"})
                    {
                        Hd2DGroundAttachmentMaps.Apply(material,albedo);
                        if(state=="baseline"){material.DisableKeyword("_OCC_GROUND_DATA");material.DisableKeyword("_NORMALMAP");}
                        if(state=="surface_only")material.DisableKeyword("_NORMALMAP");
                        preview.lights[0].transform.rotation=Quaternion.Euler(55,state=="moved_light"?110:65,0);
                        preview.BeginStaticPreview(new Rect(0,0,size.x,size.y));preview.Render(true,false);
                        var image=preview.EndStaticPreview();
                        try{File.WriteAllBytes(Path.Combine(folder,state+"_"+size.x+"x"+size.y+".png"),image.EncodeToPNG());}
                        finally{UnityEngine.Object.DestroyImmediate(image);}
                    }
                var messages=ShaderUtil.GetShaderMessages(shader);
                File.WriteAllText(Path.Combine(folder,"result.json"),JsonConvert.SerializeObject(new{status="rendered",entry.Id,nativeCanvas="32x32",worldPixelPitch=1f/32,noFocus=true,source="Owned offscreen material preview, not runtime/human approval",messages=messages.Select(m=>new{m.message,severity=m.severity.ToString()}).ToArray()},Formatting.Indented));
                if(messages.Any(m=>m.severity==UnityEditor.Rendering.ShaderCompilerMessageSeverity.Error))throw new InvalidOperationException("Ground shader compile failure");
            }
            finally{preview.Cleanup();UnityEngine.Object.DestroyImmediate(variants);UnityEngine.Object.DestroyImmediate(mesh);UnityEngine.Object.DestroyImmediate(material);}
        }
    }
}
