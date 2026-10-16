using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Security.Cryptography;
using Newtonsoft.Json;
using OCC.Combat.Presentation;
using UnityEditor;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;

namespace OCC.Combat.EditorTools
{
    // Local runtime evidence only. Source texture pixels are not inferred here.
    public static class Hd2DBattleLocalEvidence
    {
        public static void CaptureCrystalStateFixture(Hd2DBattlefieldSample sample,CombatPrototypeBootstrap host,string folder)
        {
            var cells=host.CurrentState.Map.PositionsWith(t=>t.IsAetherCrystal&&!t.IsDestroyed).ToArray();
            if(cells.Length==0)return;
            var health=cells.Select(p=>host.CurrentState.Map.GetTile(p).Durability).ToArray();
            try
            {
                foreach(var cell in cells)
                {
                    var tile=host.CurrentState.Map.GetTile(cell);tile.Durability=tile.IsPressureCrystal?16:8;
                }
                host.UiPresentationVersions.Mark(UiPresentationArea.Combat);sample.SendMessage("LateUpdate",SendMessageOptions.DontRequireReceiver);
                Hd2DBattleCanvasCapture.Capture(sample,1920,1080,Path.Combine(folder,"constructed_damage_fixture_1920x1080.png"));
                Hd2DBattleCanvasCapture.Capture(sample,960,540,Path.Combine(folder,"constructed_damage_fixture_960x540.png"));
                File.WriteAllText(Path.Combine(folder,"constructed_damage_fixture.json"),JsonConvert.SerializeObject(new{status="LOCAL_RENDER_FIXTURE",source="Disposable in-memory preview run: durability thresholds explicitly set to test damaged texture selection. Not a player action, gameplay result or normal-entry proof",cells=cells.Select((p,i)=>new{x=p.X,y=p.Y,before=health[i],fixture=host.CurrentState.Map.GetTile(p).Durability}).ToArray(),
                    mappedCrystalAlbedos=sample.RenderCamera.transform.parent.GetComponentsInChildren<MeshRenderer>(false).Where(r=>r.sharedMaterial!=null&&r.sharedMaterial.IsKeywordEnabled("_OCC_CRYSTAL_REGIONS")).Select(r=>r.sharedMaterial.mainTexture.name).Distinct().ToArray(),focusRequested=false},Formatting.Indented));
            }
            finally
            {
                for(int i=0;i<cells.Length;i++)host.CurrentState.Map.GetTile(cells[i]).Durability=health[i];
                host.UiPresentationVersions.Mark(UiPresentationArea.Combat);sample.SendMessage("LateUpdate",SendMessageOptions.DontRequireReceiver);
            }
        }
        private static string BindHash(string path)=>File.Exists(path)?BitConverter.ToString(SHA256.Create().ComputeHash(File.ReadAllBytes(path))).Replace("-","").ToLowerInvariant():null;
        private static Texture2D ResolveSource(Hd2DBattlefieldSample sample,Texture2D texture)
        {
            if(!string.IsNullOrEmpty(AssetDatabase.GetAssetPath(texture)))return texture;
            const System.Reflection.BindingFlags flags=System.Reflection.BindingFlags.Instance|System.Reflection.BindingFlags.NonPublic;
            if(ReferenceEquals(typeof(Hd2DBattlefieldSample).GetField("masonryTexture",flags)?.GetValue(sample),texture))
                return Resources.Load<Texture2D>("Art/FormalAcademyIndependentFloors32/academy_block_court_b");
            var environment=sample.RenderCamera.transform.parent.GetComponent<Hd2DAcademyEnvironment>();
            if(environment!=null)
            {
                var type=typeof(Hd2DAcademyEnvironment);
                if(ReferenceEquals(type.GetField("masonryTexture",flags)?.GetValue(environment),texture))
                    return type.GetField("masonrySourceTexture",flags)?.GetValue(environment) as Texture2D;
                if(ReferenceEquals(type.GetField("earthTexture",flags)?.GetValue(environment),texture))
                    return Resources.Load<Texture2D>("Art/FormalAcademyIndependentFloors32/academy_block_earth_b");
            }
            return null;
        }
        public static void Write(Hd2DBattlefieldSample sample,string folder,bool motion)
        {
            if(Application.dataPath.Replace('\\','/')!="E:/数据库/OCC_Codex/UnityProject/Assets"||!Application.isPlaying)
                throw new InvalidOperationException("Main OCC live local evidence required");
            var camera=sample.RenderCamera;var stage=camera.transform.parent;
            var rows=new List<object>();var unresolved=new List<string>();var pitches=new List<float>();var planes=GeometryUtility.CalculateFrustumPlanes(camera);
            foreach(var renderer in stage.GetComponentsInChildren<MeshRenderer>(false))
            {
                if(!renderer.enabled||renderer.shadowCastingMode==ShadowCastingMode.ShadowsOnly||!GeometryUtility.TestPlanesAABB(planes,renderer.bounds))continue;
                var filter=renderer.GetComponent<MeshFilter>();var material=renderer.sharedMaterial;
                if(filter==null||filter.sharedMesh==null||material==null)continue;
                // Texture-free weather geometry has no pixel-source inventory entry.
                if(!material.HasProperty("_BaseMap")&&!material.HasProperty("_MainTex"))continue;
                if(!(material.mainTexture is Texture2D texture)){unresolved.Add(renderer.name+": no colour texture");continue;}
                var sourceTexture=ResolveSource(sample,texture);
                if(sourceTexture==null){unresolved.Add(renderer.name+": unresolved runtime texture source");continue;}
                string assetPath=AssetDatabase.GetAssetPath(sourceTexture);
                var mesh=filter.sharedMesh;var uv=mesh.uv;var vertices=mesh.vertices;var triangles=mesh.triangles;
                var textureScale=material.mainTextureScale;var local=new List<float>();
                for(int i=0;i+2<triangles.Length;i+=3)
                {
                    int a=triangles[i],b=triangles[i+1],c=triangles[i+2];if(uv.Length<=Math.Max(a,Math.Max(b,c)))continue;
                    Vector3 e1=renderer.transform.TransformVector(vertices[b]-vertices[a]),e2=renderer.transform.TransformVector(vertices[c]-vertices[a]);
                    Vector2 u1=Vector2.Scale(uv[b]-uv[a],new Vector2(texture.width*textureScale.x,texture.height*textureScale.y));
                    Vector2 u2=Vector2.Scale(uv[c]-uv[a],new Vector2(texture.width*textureScale.x,texture.height*textureScale.y));
                    float det=u1.x*u2.y-u1.y*u2.x;if(Mathf.Abs(det)<1e-6f)continue;
                    local.Add(((e1*u2.y-e2*u1.y)/det).magnitude);local.Add(((e2*u1.x-e1*u2.x)/det).magnitude);
                }
                bool water=material.shader.name=="OCC/HD2D/CourtyardWater";
                if(water){local.Clear();local.Add(1f/32);local.Add(1f/32);}
                pitches.AddRange(local);
                string sampling=water?"Shader quantizes world XZ at floor(p*32), then samples native32 textures; refraction changes sample location, not lattice pitch":"Actual mesh world distance per source texel from UV derivatives";
                rows.Add(new{objectName=renderer.name,texture=texture.name,path="UnityProject/"+assetPath,sha256=BindHash(Path.GetFullPath(Path.Combine(Application.dataPath,"..",assetPath))),
                    texture.width,texture.height,filter=texture.filterMode.ToString(),vertices=mesh.vertexCount,
                    construction=mesh.vertexCount==4?"pixel_card":"simple_polygonal_mesh",
                    sourceTexelWorldPitchMin=local.Count==0?(float?)null:local.Min(),sourceTexelWorldPitchMax=local.Count==0?(float?)null:local.Max(),
                    runtimeCopyResolved=!ReferenceEquals(sourceTexture,texture),shader=material.shader.name,sampling,
                    worldBounds=new[]{renderer.bounds.size.x,renderer.bounds.size.y,renderer.bounds.size.z},uvDensityScope="Source macro-pixel lattice and style still require real review"});
                if(water&&material.GetTexture("_WaterPattern") is Texture2D pattern)
                {
                    string patternPath=AssetDatabase.GetAssetPath(pattern);
                    rows.Add(new{objectName=renderer.name+"/WaterPattern",texture=pattern.name,path="UnityProject/"+patternPath,sha256=BindHash(Path.GetFullPath(Path.Combine(Application.dataPath,"..",patternPath))),
                        pattern.width,pattern.height,filter=pattern.filterMode.ToString(),vertices=mesh.vertexCount,construction="pixel_card",sourceTexelWorldPitchMin=1f/32,sourceTexelWorldPitchMax=1f/32,
                        runtimeCopyResolved=false,shader=material.shader.name,sampling,worldBounds=new[]{renderer.bounds.size.x,renderer.bounds.size.y,renderer.bounds.size.z},uvDensityScope="Source macro-pixel lattice and style still require real review"});
                }
            }
            var data=camera.GetUniversalAdditionalCameraData();
            File.WriteAllText(Path.Combine(folder,"local_density_configuration.json"),JsonConvert.SerializeObject(new{status="QA_PENDING",source="Current live battle geometry/material readback, visible source-textured meshes only",camera.fieldOfView,camera.allowHDR,
                renderer=data.scriptableRenderer.GetType().Name,postProcessing=data.renderPostProcessing,
                colourSpace=QualitySettings.activeColorSpace.ToString(),quality=QualitySettings.names[QualitySettings.GetQualityLevel()],
                expectedWorldPixelPitch=1f/32,sourceTexelWorldPitchMin=pitches.Count==0?(float?)null:pitches.Min(),sourceTexelWorldPitchMax=pitches.Count==0?(float?)null:pitches.Max(),
                sourceMacroPixelReview="PENDING",humanReview="PENDING",visibleTextures=rows,unresolvedColourSources=unresolved,
                lights=stage.GetComponentsInChildren<Light>(false).Select(l=>new{l.name,type=l.type.ToString(),l.intensity,l.range,colour=new[]{l.color.r,l.color.g,l.color.b,l.color.a},shadows=l.shadows.ToString(),l.cullingMask}).ToArray(),
                limitations=new[]{"Inventory now resolves registered runtime copies and water pattern; source-granularity and human approval remain pending","Quad classification is a geometry diagnostic, not human construction review"}},Formatting.Indented));
            Hd2DBattleConfigurationEvidence.Write(sample,folder);
            if(!motion)return;
            var oldPosition=camera.transform.position;float oldFov=camera.fieldOfView;var samples=new List<object>();
            try
            {
                var pointer=new PointerEventData(EventSystem.current){button=PointerEventData.InputButton.Left};
                foreach(var size in new[]{new Vector2Int(1920,1080),new Vector2Int(960,540)})
                {
                    camera.transform.position=oldPosition;camera.fieldOfView=oldFov;
                    pointer.scrollDelta=new Vector2(0,20);sample.OnScroll(pointer);
                    Hd2DBattleCanvasCapture.Capture(sample,size.x,size.y,Path.Combine(folder,"closest_legal_zoom_"+size.x+"x"+size.y+".png"));
                    pointer.delta=new Vector2(24,8);sample.OnDrag(pointer);
                    Hd2DBattleCanvasCapture.Capture(sample,size.x,size.y,Path.Combine(folder,"legal_pan_"+size.x+"x"+size.y+".png"));
                    var rt=camera.targetTexture;var target=RenderTexture.GetTemporary(Math.Max(32,Mathf.RoundToInt(rt.width*size.x/1920f)),Math.Max(32,Mathf.RoundToInt(rt.height*size.y/1080f)),rt.depth,rt.format);
                    var targetSize=new[]{target.width,target.height};var times=new List<double>();
                    try
                    {
                        camera.targetTexture=target;
                        for(int i=0;i<26;i++)
                        {
                            var watch=System.Diagnostics.Stopwatch.StartNew();camera.Render();watch.Stop();
                            if(i>=6)times.Add(watch.Elapsed.TotalMilliseconds);
                        }
                    }
                    finally{camera.targetTexture=rt;RenderTexture.ReleaseTemporary(target);}
                    times.Sort();samples.Add(new{resolution=new[]{size.x,size.y},offscreenBattleResolution=targetSize,sampleCount=times.Count,
                        cameraRenderCpuSubmissionMedianMs=times[times.Count/2],cameraRenderCpuSubmissionP95Ms=times[(int)Math.Ceiling(times.Count*.95)-1],
                        fov=camera.fieldOfView,cameraPosition=new[]{camera.transform.position.x,camera.transform.position.y,camera.transform.position.z},
                        scope="Synchronous Editor Camera.Render submission only; excludes full UI/game frame, GPU frame timing and Player performance",budgetPass=(bool?)null});
                }
                File.WriteAllText(Path.Combine(folder,"local_motion_render_cost.json"),JsonConvert.SerializeObject(new{status="DIAGNOSTIC_ONLY",device=SystemInfo.graphicsDeviceName,cpu=SystemInfo.processorType,quality=QualitySettings.names[QualitySettings.GetQualityLevel()],noFocus=true,samples,
                    fullFramePerformance="NOT_MEASURED",motionHumanReview="PENDING"},Formatting.Indented));
            }
            finally{camera.transform.position=oldPosition;camera.fieldOfView=oldFov;}
        }
    }
}
