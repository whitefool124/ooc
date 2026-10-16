using System;
using System.IO;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using OCC.Combat.Presentation;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace OCC.Combat.EditorTools
{
    // Three requested stills, without opening/focusing Game View or running QA suites.
    [InitializeOnLoad]
    public static class Hd2DManualBattleScreenshots
    {
        private const string Key="OCC.ManualBattleScreenshots20261013";
        private static string Folder=>Path.GetFullPath(Path.Combine(Application.dataPath,
            "../../Worldbuilding/归档/2026-10-13_OCC战场层次提升/"+SessionState.GetString(Key+".label","manual")));
        static Hd2DManualBattleScreenshots()
        {
            EditorApplication.update+=Tick;
            EditorApplication.playModeStateChanged+=state=>{if(state==PlayModeStateChange.EnteredEditMode&&Hd2DGroundMaterialReview.Current!=null)Hd2DGroundMaterialReview.Current.Dispose();};
        }
        [MenuItem("OCC/Tools/Manual screenshots/Before layout")]
        public static void Before()=>Begin("before_layout");
        [MenuItem("OCC/Tools/Manual screenshots/After layout")]
        public static void After()=>Begin("after_layout");
        [MenuItem("OCC/Tools/Manual screenshots/Latest")]
        public static void Latest()=>Begin("manual_final");
        [MenuItem("OCC/Tools/Manual screenshots/Masonry")]
        public static void Masonry()=>Begin("masonry_r1");
        [MenuItem("OCC/Tools/Manual screenshots/Masonry and surrounds")]
        public static void MasonrySurrounds()=>Begin("masonry_surrounds");
        [MenuItem("OCC/Tools/Manual screenshots/Wall cap cell edges")]
        public static void WallCapEdges()=>Begin("wall_cap_cell_edges");
        [MenuItem("OCC/Tools/Manual screenshots/Readability and architecture")]
        public static void ReadabilityArchitecture()=>Begin("readability_architecture");
        [MenuItem("OCC/Tools/Manual screenshots/Garden composition")]
        public static void GardenComposition()=>Begin("garden_composition");
        [MenuItem("OCC/Tools/Manual screenshots/Garden composition final")]
        public static void GardenCompositionFinal()=>Begin("garden_composition_final");
        [MenuItem("OCC/Tools/Manual screenshots/Whole battlefield look")]
        public static void WholeBattlefieldLook()=>Begin("whole_battlefield_look");
        [MenuItem("OCC/Tools/Manual screenshots/Stable scene ambient")]
        public static void StableSceneAmbient()=>Begin("stable_scene_ambient");
        [MenuItem("OCC/Tools/Manual screenshots/Ground family candidates")]
        public static void GroundFamilyCandidates()=>Begin("ground_family_candidates");
        [MenuItem("OCC/Tools/Manual screenshots/Quiet ground family candidates")]
        public static void QuietGroundFamilyCandidates()=>Begin("quiet_ground_family_candidates");
        [MenuItem("OCC/Tools/Manual screenshots/Vegetation materials")]
        public static void VegetationMaterials()=>Begin("vegetation_materials");
        [MenuItem("OCC/Tools/Manual screenshots/Exterior air scattering")]
        public static void ExteriorAirScattering()=>Begin("exterior_air_scattering");
        [MenuItem("OCC/Tools/Manual screenshots/Key and fill balance")]
        public static void KeyFillBalance()=>Begin("key_fill_balance");
        [MenuItem("OCC/Tools/Manual screenshots/Pixel emission colours")]
        public static void PixelEmissionColours()=>Begin("pixel_emission_colours");
        [MenuItem("OCC/Tools/Manual screenshots/Emission clusters and staff")]
        public static void EmissionClusters()=>Begin("emission_clusters");
        [MenuItem("OCC/Tools/Manual screenshots/Crystal facet emission")]
        public static void CrystalFacetEmission()=>Begin("crystal_facet_emission");
        [MenuItem("OCC/Tools/Manual screenshots/Visible crystal facets")]
        public static void VisibleCrystalFacets()=>Begin("crystal_facet_emission_r2");
        [MenuItem("OCC/Tools/Manual screenshots/Native pixels and tactical glow")]
        public static void NativePixelsAndTactics()=>Begin("native_pixel_glow");
        [MenuItem("OCC/Tools/Manual screenshots/Crystal radiance")]
        public static void CrystalRadiance()=>Begin("crystal_radiance");
        [MenuItem("OCC/Tools/Manual screenshots/Contact soft shadows")]
        public static void ContactSoftShadows()=>Begin("contact_soft_shadows");

        public static void Begin(string label)
        {
            if(Application.dataPath.Replace('\\','/')!="E:/数据库/OCC_Codex/UnityProject/Assets"||!Application.isBatchMode||
                EditorApplication.isPlayingOrWillChangePlaymode||EditorApplication.isCompiling||SessionState.GetBool(Key,false))
                throw new InvalidOperationException("Idle main OCC background Editor required");
            if(label!="before_layout"&&label!="after_layout"&&label!="manual_final"&&label!="masonry_r1"&&label!="masonry_surrounds"&&label!="wall_cap_cell_edges"&&label!="readability_architecture"&&label!="garden_composition"&&label!="garden_composition_final"&&label!="whole_battlefield_look"&&label!="stable_scene_ambient"&&label!="ground_family_candidates"&&label!="quiet_ground_family_candidates"&&label!="vegetation_materials"&&label!="exterior_air_scattering"&&label!="key_fill_balance"&&label!="pixel_emission_colours"&&label!="emission_clusters"&&label!="crystal_facet_emission"&&label!="crystal_facet_emission_r2"&&label!="native_pixel_glow"&&label!="crystal_radiance"&&label!="contact_soft_shadows")throw new ArgumentException("Unknown screenshot set");
            for(int i=0;i<SceneManager.sceneCount;i++)if(SceneManager.GetSceneAt(i).isDirty)
                throw new InvalidOperationException("Preserve dirty scene");
            EditorSceneManager.OpenScene("Assets/Scenes/CombatPrototype.unity");
            SessionState.SetString(Key+".label",label);SessionState.SetInt(Key+".phase",-1);
            SessionState.SetInt(Key+".lightingUpdates",0);
            SessionState.SetBool(Key+".entrySettled",false);
            SessionState.SetBool(Key+".pixelEmissionInstalled",false);
            SessionState.SetFloat(Key+".deadline",(float)EditorApplication.timeSinceStartup+90);
            Directory.CreateDirectory(Folder);SessionState.SetBool(Key,true);EditorApplication.EnterPlaymode();
        }

        private static void Tick()
        {
            if(!SessionState.GetBool(Key,false)||EditorApplication.isCompiling||EditorApplication.isUpdating)return;
            try
            {
                if(EditorApplication.timeSinceStartup>SessionState.GetFloat(Key+".deadline",0))throw new TimeoutException("Requested screenshots timed out");
                if(!Application.isPlaying)return;
                EditorApplication.QueuePlayerLoopUpdate();
                var host=UnityEngine.Object.FindAnyObjectByType<CombatPrototypeBootstrap>();if(host==null)return;
                int phase=SessionState.GetInt(Key+".phase",-1);
                if(phase==-1)
                {
                    string error=host.PreviewUiCaptureRun(false,"B3");if(error!=null)throw new InvalidOperationException(error);
                    string label=SessionState.GetString(Key+".label","");
                    if(label=="ground_family_candidates"||label=="quiet_ground_family_candidates")LoadGroundCandidates();
                    host.StartMapNodeCombat("B3");SessionState.SetInt(Key+".phase",0);Wait(6);return;
                }
                if(host.IsCombatEntryBlocking)return;
                if(phase==0&&!SessionState.GetBool(Key+".entrySettled",false))
                {SessionState.SetBool(Key+".entrySettled",true);Wait(4);return;}
                if(EditorApplication.timeSinceStartup<SessionState.GetFloat(Key+".ready",0)||Time.unscaledTime<SessionState.GetFloat(Key+".runtimeReady",0))return;
                var sample=UnityEngine.Object.FindAnyObjectByType<Hd2DBattlefieldSample>();if(sample==null)return;
                if((SessionState.GetString(Key+".label","")=="native_pixel_glow"||SessionState.GetString(Key+".label","")=="crystal_radiance")&&!SessionState.GetBool(Key+".pixelEmissionInstalled",false))
                {InstallPixelEmission();host.SelectHudAction("移动");SessionState.SetBool(Key+".pixelEmissionInstalled",true);Wait(4);return;}
                if((SessionState.GetString(Key+".label","")=="pixel_emission_colours"||SessionState.GetString(Key+".label","")=="emission_clusters"||SessionState.GetString(Key+".label","").StartsWith("crystal_facet_emission"))&&!SessionState.GetBool(Key+".pixelEmissionInstalled",false))
                {InstallPixelEmission();SessionState.SetBool(Key+".pixelEmissionInstalled",true);Wait(4);return;}
                var bindings=new System.Collections.Generic.List<object>();
                foreach(var emission in UnityEngine.Object.FindObjectsByType<Hd2DPixelEmission>(FindObjectsSortMode.None))
                {
                    emission.RefreshBinding();
                    bindings.Add(new{renderer=emission.name,pixels=emission.PixelCount,lights=emission.LightCount,
                        enabled=emission.GetComponent<Renderer>().sharedMaterial.IsKeywordEnabled("_OCC_PIXEL_EMISSION")});
                }
                sample.RenderCamera.Render();sample.PumpLocalLightingReview();
                if(sample.LocalDiffuseUpdates<=SessionState.GetInt(Key+".lightingUpdates",0))return;
                string name=phase==0?"day":phase==1?"sunset":"rain_night";
                if((SessionState.GetString(Key+".label","")=="native_pixel_glow"||SessionState.GetString(Key+".label","")=="crystal_radiance"))
                {
                    int cells=0;foreach(var model in sample.FrameModels.Values)if(model!=null&&model.MoveMarker!=BattlefieldCellMarker.None)cells++;
                    File.WriteAllText(Path.Combine(Folder,name+"_tactical.json"),JsonConvert.SerializeObject(new{action=host.SelectedAction,movePreviewCells=cells,glowGraphics=UnityEngine.Object.FindObjectsByType<Hd2DTacticalGlow>(FindObjectsSortMode.None).Length,emissionUsesOriginalPixels=true,emissionTexture=false},Formatting.Indented));
                }
                if(SessionState.GetString(Key+".label","")=="emission_clusters"||SessionState.GetString(Key+".label","").StartsWith("crystal_facet_emission"))
                    File.WriteAllText(Path.Combine(Folder,name+"_emission_bindings.json"),JsonConvert.SerializeObject(bindings,Formatting.Indented));
                Hd2DBattleCanvasCapture.Capture(sample,1920,1080,Path.Combine(Folder,name+".png"));
                if(SessionState.GetString(Key+".label","")=="contact_soft_shadows")
                {
                    try
                    {
                        sample.SetContactSoftShadows(false);sample.RenderCamera.Render();
                        Hd2DBattleCanvasCapture.Capture(sample,1920,1080,Path.Combine(Folder,name+"_original_shadow.png"));
                    }
                    finally{sample.SetContactSoftShadows(true);}
                }
                if(phase==2&&(SessionState.GetString(Key+".label","")=="crystal_facet_emission_r2"||(SessionState.GetString(Key+".label","")=="native_pixel_glow"||SessionState.GetString(Key+".label","")=="crystal_radiance")))
                {
                    var emitters=UnityEngine.Object.FindObjectsByType<Hd2DPixelEmission>(FindObjectsSortMode.None);
                    try
                    {
                        foreach(var emitter in emitters)if(emitter.name.StartsWith("物件_")){emitter.EmissionEnabled=false;emitter.RefreshBinding();}
                        sample.RenderCamera.Render();
                        Hd2DBattleCanvasCapture.Capture(sample,1920,1080,Path.Combine(Folder,"rain_night_off.png"));
                    }
                    finally{foreach(var emitter in emitters)if(emitter.name.StartsWith("物件_")){emitter.EmissionEnabled=true;emitter.RefreshBinding();}}
                }
                File.WriteAllText(Path.Combine(Folder,name+"_lighting.json"),JsonConvert.SerializeObject(new{
                    diffuseUpdates=sample.LocalDiffuseUpdates,diffuseStatus=sample.LocalDiffuseStatus,
                    diffuseSurfaces=sample.LocalDiffuseSurfaces,reflectionCaptures=sample.ReflectionCaptures,
                    sourceRadianceRange=sample.LocalCapturedRadianceRange},Formatting.Indented));
                if(SessionState.GetString(Key+".label","")=="contact_soft_shadows")
                {
                    var enabled=new System.Collections.Generic.HashSet<Material>();
                    foreach(var renderer in sample.RenderCamera.transform.parent.GetComponentsInChildren<MeshRenderer>())
                        foreach(var material in renderer.sharedMaterials)if(material!=null&&material.IsKeywordEnabled("_OCC_CONTACT_SOFT_SHADOWS"))enabled.Add(material);
                    File.WriteAllText(Path.Combine(Folder,name+"_shadow.json"),JsonConvert.SerializeObject(new{enabledMaterials=enabled.Count,
                        shaderDependencies=AssetDatabase.GetAssetDependencyHash("Assets/Game/Resources/Shaders/Hd2DBakedArchitecture.shader").ToString(),
                        kernel="12 blocker + 25 weighted comparison samples; max4 atlas texels"},Formatting.Indented));
                }
                bool capEdgesOnly=SessionState.GetString(Key+".label","")=="wall_cap_cell_edges";
                if(phase==2||capEdgesOnly)
                {
                    File.WriteAllText(Path.Combine(Folder,"capture.json"),JsonConvert.SerializeObject(new{
                        status="CAPTURED_FOR_MANUAL_REVIEW",dataPath=Application.dataPath,level=host.CurrentLevelId,
                        source="Unity Editor Play Mode runtime battlefield and full HUD, isolated preview run; not a standalone Player or full journey acceptance",
                        resolution=new[]{1920,1080},captures=capEdgesOnly?new[]{"day.png"}:new[]{"day.png","sunset.png","rain_night.png"},focusRequested=false,
                        humanReview="PENDING",suitesRun=false},Formatting.Indented));
                    SessionState.SetBool(Key,false);EditorApplication.ExitPlaymode();return;
                }
                if(!sample.TrySetAtmosphere(phase==0?Hd2DBattleTimeOfDay.Sunset:Hd2DBattleTimeOfDay.Night,
                    phase==0?Hd2DBattleWeather.Clear:Hd2DBattleWeather.Rain,0))throw new InvalidOperationException("Atmosphere unavailable");
                SessionState.SetInt(Key+".lightingUpdates",sample.LocalDiffuseUpdates);
                SessionState.SetInt(Key+".phase",phase+1);Wait(4);
            }
            catch(Exception e)
            {
                File.WriteAllText(Path.Combine(Folder,"failure.json"),JsonConvert.SerializeObject(new{error=e.ToString()},Formatting.Indented));
                SessionState.SetBool(Key,false);if(EditorApplication.isPlayingOrWillChangePlaymode)EditorApplication.ExitPlaymode();Debug.LogException(e);
            }
        }
        private static void InstallPixelEmission()
        {
            var source=Resources.Load<Texture2D>("Art/FormalAcademyCombat32/academy_aether_crystal_intact");
            if(source==null||source.width!=96||source.height!=64)throw new InvalidOperationException("Reviewed crystal source changed");
            MeshRenderer selected=null;
            foreach(var renderer in UnityEngine.Object.FindObjectsByType<MeshRenderer>(FindObjectsSortMode.None))
                if(renderer.enabled&&renderer.shadowCastingMode!=UnityEngine.Rendering.ShadowCastingMode.ShadowsOnly&&
                    renderer.name.StartsWith("物件_")&&renderer.sharedMaterial!=null&&renderer.sharedMaterial.mainTexture==source&&Hd2DPixelEmission.Supports(renderer.sharedMaterial))
                {selected=renderer;break;}
            if(selected==null)throw new InvalidOperationException("Actual crystal card not found");
            if((SessionState.GetString(Key+".label","")=="crystal_facet_emission_r2"||(SessionState.GetString(Key+".label","")=="native_pixel_glow"||SessionState.GetString(Key+".label","")=="crystal_radiance")))
            {
                var active=selected.GetComponent<Hd2DPixelEmission>();
                if(active==null)throw new InvalidOperationException("Actual runtime crystal emission missing");
                File.WriteAllText(Path.Combine(Folder,"pixel_emission.json"),JsonConvert.SerializeObject(new{status="QA_PENDING",renderer=selected.name,pixels=active.PixelCount,lights=active.LightCount,source="Actual runtime binding; same-frame on/off stills"},Formatting.Indented));return;
            }
            var emitter=selected.gameObject.AddComponent<Hd2DPixelEmission>();
            bool clusters=SessionState.GetString(Key+".label","")=="emission_clusters";
            bool facets=SessionState.GetString(Key+".label","").StartsWith("crystal_facet_emission");
            var pixels=new System.Collections.Generic.List<Hd2DPixelEmission.Pixel>();
            if(facets)pixels.AddRange(Hd2DPixelEmissionPresets.CrystalFacets(source));
            else if(clusters)
            {
                pixels.AddRange(Hd2DPixelEmissionPresets.Cluster(source,new RectInt(22,24,7,9),new Vector3(.02f,2.6f,5.5f)));
                pixels.AddRange(Hd2DPixelEmissionPresets.Cluster(source,new RectInt(37,33,7,7),new Vector3(5f,1.4f,.02f)));
                pixels.AddRange(Hd2DPixelEmissionPresets.Cluster(source,new RectInt(58,22,7,7),new Vector3(2.8f,.02f,5f)));
            }
            else pixels.AddRange(new[]{
                new Hd2DPixelEmission.Pixel(25,28,new Vector3(.05f,2f,4f)),
                new Hd2DPixelEmission.Pixel(40,36,new Vector3(3f,.65f,.04f)),
                new Hd2DPixelEmission.Pixel(61,25,new Vector3(2f,.04f,3f))});
            emitter.Initialize(selected,source,pixels.ToArray());
            if(facets)emitter.AddLightGroup(new Vector3(0,1,-.15f),new Color(.15f,.66f,1f),.45f,1.6f);
            else
            {
            emitter.AddLightGroup(new Vector3((25.5f-48)/32f,28.5f/32f,-.15f),new Color(.08f,.7f,1f),.65f,1.6f);
            emitter.AddLightGroup(new Vector3((40.5f-48)/32f,36.5f/32f,-.15f),new Color(1f,.55f,.1f),.4f,1.6f);
            emitter.AddLightGroup(new Vector3((61.5f-48)/32f,25.5f/32f,-.15f),new Color(.8f,.12f,1f),.55f,1.6f);
            }
            File.WriteAllText(Path.Combine(Folder,"pixel_emission.json"),JsonConvert.SerializeObject(new{
                status="QA_PENDING",source=source.name,renderer=selected.name,pixelCount=emitter.PixelCount,lightGroups=emitter.LightCount,
                coordinates=facets?"Three manually traced native crystal facets; top-origin vertices in Hd2DPixelEmissionPresets.CrystalFacets":clusters?"source bottom-left; crystal regions (22,24,7,9), (37,33,7,7), (58,22,7,7); original alpha clips silhouette":"source bottom-left; selected opaque texels (25,28),(40,36),(61,25)",
                albedoChanged=false,formalReplacement=false,scope="One runtime crystal sample; other unit/model bindings not runtime reviewed"},Formatting.Indented));
        }
        private static void Wait(float seconds)
        {
            SessionState.SetFloat(Key+".ready",(float)EditorApplication.timeSinceStartup+seconds);
            SessionState.SetFloat(Key+".runtimeReady",Time.unscaledTime+seconds);
        }
        private static void LoadGroundCandidates()
        {
            string root=Path.GetFullPath(Path.Combine(Application.dataPath,"../../ArtSource/hd2d_ground_family_2026-10-13"));
            if(SessionState.GetString(Key+".label","")=="quiet_ground_family_candidates")root=Path.Combine(root,"sets/quiet");
            var owned=new System.Collections.Generic.List<Texture2D>();
            Texture2D Read(string file,bool linear)
            {
                var t=new Texture2D(2,2,TextureFormat.RGBA32,false,linear){name=Path.GetFileNameWithoutExtension(file),filterMode=FilterMode.Point,wrapMode=TextureWrapMode.Clamp};owned.Add(t);
                if(!t.LoadImage(File.ReadAllBytes(Path.Combine(root,file)),false)||t.width!=32||t.height!=32)throw new InvalidOperationException("Processed native32 review field required: "+file);
                return t;
            }
            try
            {
                var colours=new Texture2D[3];int index=0;
                foreach(char v in new[]{'a','b','c'})
                {
                    string id="academy_court_limestone_"+v;
                    var check=JObject.Parse(File.ReadAllText(Path.Combine(root,"diagnostics/"+id+"_asset_check.json")));
                    if(((JArray)check["errors"]).Count!=0)throw new InvalidOperationException("Candidate admission errors: "+id);
                    var manifest=JObject.Parse(File.ReadAllText(Path.Combine(root,"delivery/"+id+".occ-art-manifest-v1.json")));
                    byte[] data=File.ReadAllBytes(Path.Combine(root,"delivery/"+id+".png"));
                    using(var hasher=System.Security.Cryptography.SHA256.Create())
                        if(BitConverter.ToString(hasher.ComputeHash(data)).Replace("-","").ToLowerInvariant()!=manifest["delivery"]["output_sha256"].Value<string>())
                            throw new InvalidOperationException("Candidate delivery fingerprint changed: "+id);
                    colours[index++]=Read("delivery/"+id+".png",false);
                }
                Hd2DGroundMaterialReview.Current=new Hd2DGroundMaterialReview(colours,Read("material_data/normal.png",true),Read("material_data/roughness.png",true));
            }
            catch{foreach(var t in owned)if(t!=null)UnityEngine.Object.Destroy(t);throw;}
        }
    }
}
