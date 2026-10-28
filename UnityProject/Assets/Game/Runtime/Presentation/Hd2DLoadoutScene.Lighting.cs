using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;

namespace OCC.Combat.Presentation
{
    internal sealed partial class Hd2DLoadoutScene
    {
        private Hd2DDeskReflectionRefresh reflectionRefresh;
        private void RequestDeskReflection()
        {
            if(reflectionRefresh!=null)reflectionRefresh.RequestRefresh();
        }
        private void ConfigureRealisticLighting(MainMenuHd2DStage stage)
        {
            reflectionRefresh=structure.Lighting.RefreshEnvironment(stage);
            StartCoroutine(ConfigureReadingFocusWhenReady(stage));
        }
        private IEnumerator ConfigureReadingFocusWhenReady(MainMenuHd2DStage stage)
        {
            yield return null;yield return null;
            if(stage==null||sceneRoot==null)yield break;
            var profile=stage.sceneVolume.profile;
            // Far-only focus boundary includes hidden spell cards; no research bokeh values.
            if(profile.TryGet<DepthOfField>(out var depth))
            {
                float readingFar=0;
                var camera=stage.sceneCamera;
                var forward=camera.transform.forward;
                var absoluteForward=new Vector3(Mathf.Abs(forward.x),Mathf.Abs(forward.y),Mathf.Abs(forward.z));
                foreach(var renderer in sceneRoot.GetComponentsInChildren<MeshRenderer>(true))
                {
                    var bounds=renderer.bounds;
                    readingFar=Mathf.Max(readingFar,Vector3.Dot(bounds.center-camera.transform.position,forward)+Vector3.Dot(bounds.extents,absoluteForward));
                }
                depth.active=readingFar>0;
                depth.mode.Override(DepthOfFieldMode.Gaussian);
                depth.gaussianStart.Override(readingFar+.6f);
                depth.gaussianEnd.Override(readingFar+2.6f);
                depth.gaussianMaxRadius.Override(.8f);
                depth.highQualitySampling.Override(true);
            }
        }
    }
    // Only the desk renderer is cloned. Authored renderer/pipeline assets are never edited.
    internal sealed class Hd2DDeskContactLighting : MonoBehaviour
    {
        private static Hd2DDeskContactLighting active;
        internal static void ReleaseActive(){if(active!=null)active.Release();}
        private RenderPipelineAsset originalQuality;
        private UniversalRenderPipelineAsset localPipeline;
        private ScriptableRendererData localRenderer;
        private ScreenSpaceAmbientOcclusion occlusion;
        private bool initialized;
        internal void Initialize()
        {
            initialized=true;
            if(localPipeline!=null)return;
            // A desk must never copy a battle pipeline whose renderer is about
            // to be disposed during the same page transition.
            Hd2DBattleLighting.ReleaseActive();
            var pipeline=GraphicsSettings.currentRenderPipeline as UniversalRenderPipelineAsset;
            if(pipeline==null)return;
            var flags=System.Reflection.BindingFlags.Instance|System.Reflection.BindingFlags.NonPublic|System.Reflection.BindingFlags.Public;
            var listField=typeof(UniversalRenderPipelineAsset).GetField("m_RendererDataList",flags);
            var source=listField?.GetValue(pipeline) as ScriptableRendererData[];
            if(source==null||source.Length<2||source[1]==null)return;
            // Page replacement may build the next stage before deferred destruction.
            // Release the previous lease first; never clone a soon-to-die pipeline.
            if(active!=null)
            {
                active.Release();
                pipeline=GraphicsSettings.currentRenderPipeline as UniversalRenderPipelineAsset;
                source=listField.GetValue(pipeline) as ScriptableRendererData[];
            }
            active=this;
            originalQuality=QualitySettings.renderPipeline;
            localPipeline=Instantiate(pipeline);localPipeline.name="OCC desk contact lighting runtime";
            var list=(ScriptableRendererData[])source.Clone();
            localRenderer=Instantiate(source[1]);localRenderer.name="OCC desk contact renderer runtime";
            // Instantiate makes a new list but shares feature objects. Preserve existing
            // features; our new AO is the only feature owned by this component.
            occlusion=ScriptableObject.CreateInstance<ScreenSpaceAmbientOcclusion>();
            occlusion.name="Desk indirect contact occlusion";
            var settingsField=typeof(ScreenSpaceAmbientOcclusion).GetField("m_Settings",flags);
            var settings=settingsField.GetValue(occlusion);
            void Set(string name,object value)
            {
                var field=settings.GetType().GetField(name,flags);
                field.SetValue(settings,field.FieldType.IsEnum?System.Enum.ToObject(field.FieldType,value):value);
            }
            Set("Source",0);Set("NormalSamples",2);Set("AOMethod",1);
            Set("Samples",0);Set("Downsample",false);Set("AfterOpaque",false);
            Set("Intensity",.8f);Set("Radius",.18f);Set("DirectLightingStrength",0f);
            Set("Falloff",25f);
            occlusion.Create();localRenderer.rendererFeatures.Add(occlusion);
            list[1]=localRenderer;listField.SetValue(localPipeline,list);
            QualitySettings.renderPipeline=localPipeline;
        }
        private void OnEnable(){if(initialized)Initialize();}
        private void OnDisable()=>Release();
        private void OnDestroy()=>Release();
        private void Release()
        {
            if(localPipeline!=null&&QualitySettings.renderPipeline==localPipeline)
                QualitySettings.renderPipeline=originalQuality;
            if(localPipeline!=null)Destroy(localPipeline);
            if(localRenderer!=null)Destroy(localRenderer);
            if(occlusion!=null)Destroy(occlusion);
            localPipeline=null;localRenderer=null;occlusion=null;
            if(active==this)active=null;
        }
    }
    // One local probe; collapse slider events and wait for the previous capture.
    internal sealed class Hd2DDeskReflectionRefresh : MonoBehaviour
    {
        private ReflectionProbe probe;
        private bool dirty;
        private float requestedAt,lastCapture=-10f;
        private int renderId=-1;
        private int projectedId=-1;
        private bool readbackPending;
        private Transform stageRoot;
        private readonly Dictionary<Renderer,ProbeState> surfaces=new Dictionary<Renderer,ProbeState>();
        private sealed class ProbeState
        {
            internal LightProbeUsage Usage;
            internal MaterialPropertyBlock Original;
        }
        public int CaptureRequests {get;private set;}
        public int DiffuseUpdates {get;private set;}
        public int BoundSurfaces=>surfaces.Count;
        public string DiffuseStatus {get;private set;}="waiting_for_capture";
        internal void Initialize(ReflectionProbe value,Transform stage)
        {
            stageRoot=stage;probe=value;RequestRefresh();StartCoroutine(CaptureWhenSettled());
        }
        private void RegisterSurfaces()
        {
            if(stageRoot==null)return;
            // Only this page's OCC lit surfaces receive local irradiance. No global
            // ambient, authored material, texture, pixel pitch or battle shader changes.
            foreach(var renderer in stageRoot.GetComponentsInChildren<Renderer>(true))
            {
                if(surfaces.ContainsKey(renderer))continue;
                bool lit=false;
                foreach(var material in renderer.sharedMaterials)
                    if(material!=null&&material.shader!=null&&
                        (material.shader.name=="OCC/HD2D/Surface"||material.shader.name=="OCC/HD2D/DeskSurface"))lit=true;
                if(!lit)continue;
                var original=new MaterialPropertyBlock();renderer.GetPropertyBlock(original);
                surfaces.Add(renderer,new ProbeState{Usage=renderer.lightProbeUsage,Original=original});
            }
        }
        internal void RequestRefresh(){dirty=true;requestedAt=Time.unscaledTime;}
        private IEnumerator CaptureWhenSettled()
        {
            yield return null;yield return null;
            while(probe!=null)
            {
                if(renderId>=0 && renderId!=projectedId && probe.IsFinishedRendering(renderId) && !readbackPending)
                {
                    projectedId=renderId;
                    if(SystemInfo.supportsAsyncGPUReadback&&probe.texture!=null)
                    {
                        readbackPending=true;
                        // A 16x16 mip on the 256 cube is sufficient for diffuse SH.
                        // One asynchronous read per settled capture, never a per-frame read.
                        AsyncGPUReadback.Request(probe.texture,4,TextureFormat.RGBAFloat,ProjectEnvironment);
                    }
                    else DiffuseStatus="unavailable_keep_existing_ambient";
                }
                if(dirty && Time.unscaledTime-requestedAt>=.15f && Time.unscaledTime-lastCapture>=.4f &&
                    (renderId<0||probe.IsFinishedRendering(renderId)))
                {
                    RegisterSurfaces();
                    renderId=probe.RenderProbe();lastCapture=Time.unscaledTime;
                    CaptureRequests++;dirty=false;
                }
                yield return null;
            }
        }
        private static Vector3 CubeDirection(int face,float u,float v)
        {
            switch(face)
            {
                case 0:return new Vector3(1,-v,-u).normalized;
                case 1:return new Vector3(-1,-v,u).normalized;
                case 2:return new Vector3(u,1,v).normalized;
                case 3:return new Vector3(u,-1,-v).normalized;
                case 4:return new Vector3(u,-v,1).normalized;
                default:return new Vector3(-u,-v,-1).normalized;
            }
        }
        private void ProjectEnvironment(AsyncGPUReadbackRequest request)
        {
            if(this==null||probe==null)return;
            readbackPending=false;
            if(request.hasError||request.layerCount!=6)
            {DiffuseStatus="readback_failed_keep_existing_ambient";return;}
            var irradiance=new SphericalHarmonicsL2();
            var unit=new SphericalHarmonicsL2();
            int width=request.width,height=request.height;
            for(int face=0;face<6;face++)
            {
                var pixels=request.GetData<Color>(face);
                if(pixels.Length!=width*height){DiffuseStatus="unexpected_cube_layout";return;}
                for(int y=0;y<height;y++)for(int x=0;x<width;x++)
                {
                    float u=2f*(x+.5f)/width-1,v=2f*(y+.5f)/height-1;
                    float weight=1f/Mathf.Pow(1+u*u+v*v,1.5f);
                    var direction=CubeDirection(face,u,v);
                    var colour=pixels[y*width+x];
                    // Suppress tiny emissive spikes in low-order diffuse lighting;
                    // highlights remain in the full HDR reflection probe.
                    colour=new Color(Mathf.Clamp(colour.r,0,4),Mathf.Clamp(colour.g,0,4),Mathf.Clamp(colour.b,0,4));
                    irradiance.AddDirectionalLight(direction,colour,weight);
                    unit.AddDirectionalLight(direction,Color.white,weight);
                }
            }
            var directions=new[]{Vector3.right,Vector3.left,Vector3.up,Vector3.down,Vector3.forward,Vector3.back};
            var normalization=new Color[6];unit.Evaluate(directions,normalization);
            float mean=0;foreach(var c in normalization)mean+=c.r/6;
            if(!float.IsFinite(mean)||mean<=0){DiffuseStatus="invalid_irradiance";return;}
            // Calibrate a constant white cube to constant irradiance, then apply a
            // bounded .35 indirect gain. This is a local approximation, not path tracing.
            irradiance*=.35f/mean;
            var block=new MaterialPropertyBlock();var coefficients=new[]{irradiance};
            foreach(var pair in surfaces)
            {
                if(pair.Key==null)continue;
                pair.Key.GetPropertyBlock(block);block.CopySHCoefficientArraysFrom(coefficients);
                pair.Key.lightProbeUsage=LightProbeUsage.CustomProvided;pair.Key.SetPropertyBlock(block);
            }
            DiffuseUpdates++;DiffuseStatus="local_cube_irradiance_applied";
        }
        private void OnDestroy()
        {
            foreach(var pair in surfaces)if(pair.Key!=null)
            {pair.Key.lightProbeUsage=pair.Value.Usage;pair.Key.SetPropertyBlock(pair.Value.Original);}
            surfaces.Clear();
        }
    }
}
