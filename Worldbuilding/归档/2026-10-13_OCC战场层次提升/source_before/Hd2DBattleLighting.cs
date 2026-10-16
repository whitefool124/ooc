using System;
using System.Reflection;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;

namespace OCC.Combat.Presentation
{
    // Battle-only renderer lease. Other cameras keep their original renderer data.
    // Numeric irradiance is projected from the existing scene reflection capture;
    // no albedo, pixel density, global ambient or authored pipeline assets change.
    internal sealed class Hd2DBattleLighting : MonoBehaviour
    {
        private static Hd2DBattleLighting active;
        private RenderPipelineAsset previous;
        private UniversalRenderPipelineAsset pipeline;
        private ScriptableRendererData renderer;
        private ScreenSpaceAmbientOcclusion contact;
        private bool pending;
        private bool released;
        private int leaseGeneration;
        private Camera ownerCamera;
        private Camera captureCamera;
        private RenderTexture captureCube,completedCube;
        private ReflectionProbe targetProbe;
        private int captureFace=-1;
        public bool CaptureInProgress=>captureFace>=0||pending;
        public float CapturedRadianceRange {get;private set;}
        private SphericalHarmonicsL2[] irradiance;
        private Transform stage;
        public int DiffuseUpdates {get;private set;}
        public int BoundSurfaces {get;private set;}
        public string DiffuseStatus {get;private set;}="waiting_for_scene_capture";
        public bool ContactEnabled {get=>contact!=null&&contact.isActive;set{if(contact!=null)contact.SetActive(value);}}
        public bool DiffuseEnabled {get;set;}=true;

        internal void Initialize(Camera camera,Transform root)
        {
            if(active==this)throw new InvalidOperationException("Battle lighting lease already initialized");
            stage=root;
            if(active!=null)active.Release();
            released=false;leaseGeneration++;ownerCamera=camera;
            var source=GraphicsSettings.currentRenderPipeline as UniversalRenderPipelineAsset;
            if(source==null)throw new InvalidOperationException("Battle lighting requires current URP");
            const BindingFlags flags=BindingFlags.Instance|BindingFlags.NonPublic|BindingFlags.Public;
            var listField=typeof(UniversalRenderPipelineAsset).GetField("m_RendererDataList",flags);
            var sourceList=listField?.GetValue(source) as ScriptableRendererData[];
            if(sourceList==null||sourceList.Length<2||sourceList[1]==null)
                throw new InvalidOperationException("Battle renderer1 is unavailable");
            previous=QualitySettings.renderPipeline;active=this;
            pipeline=Instantiate(source);pipeline.name="OCC battle local lighting candidate";
            renderer=Instantiate(sourceList[1]);renderer.name="OCC battle contact renderer candidate";
            contact=ScriptableObject.CreateInstance<ScreenSpaceAmbientOcclusion>();
            contact.name="Battle near contact indirect occlusion";
            var field=typeof(ScreenSpaceAmbientOcclusion).GetField("m_Settings",flags);
            if(field==null)throw new InvalidOperationException("Locked URP SSAO settings unavailable");
            var settings=field.GetValue(contact);
            void Set(string name,object value)
            {
                var setting=settings.GetType().GetField(name,flags);
                if(setting==null)throw new InvalidOperationException("Missing SSAO setting "+name);
                setting.SetValue(settings,setting.FieldType.IsEnum?Enum.ToObject(setting.FieldType,value):value);
            }
            // Depth reconstruction avoids adding a false card-normal depth pass.
            // Radius is a local 14cm contact scale, not the research town's 45cm.
            Set("Source",0);Set("NormalSamples",2);Set("AOMethod",1);Set("Samples",0);
            Set("Downsample",false);Set("AfterOpaque",false);Set("Intensity",.65f);
            Set("Radius",.14f);Set("DirectLightingStrength",0f);Set("Falloff",35f);
            contact.Create();renderer.rendererFeatures.Add(contact);
            var list=new ScriptableRendererData[sourceList.Length+1];Array.Copy(sourceList,list,sourceList.Length);
            list[sourceList.Length]=renderer;listField.SetValue(pipeline,list);
            QualitySettings.renderPipeline=pipeline;
            camera.GetUniversalAdditionalCameraData().SetRenderer(sourceList.Length);
        }

        internal void RequestReflectionCapture(ReflectionProbe probe)
        {
            if(released||probe==null||CaptureInProgress)return;
            targetProbe=probe;
            if(captureCamera==null)
            {
                var go=new GameObject("Owned battle 3D reflection capture",typeof(Camera),typeof(UniversalAdditionalCameraData));
                go.transform.SetParent(stage,false);captureCamera=go.GetComponent<Camera>();captureCamera.enabled=false;
                // ReflectionProbe.RenderProbe uses the pipeline default, which is
                // this project's 2D renderer. Explicitly render the owned cube with
                // the existing 3D renderer1, without changing any default camera.
                captureCamera.GetUniversalAdditionalCameraData().SetRenderer(1);
                captureCamera.GetUniversalAdditionalCameraData().renderPostProcessing=false;
                captureCamera.allowHDR=true;
            }
            captureCamera.transform.position=probe.transform.position;
            captureCamera.cullingMask=probe.cullingMask;captureCamera.nearClipPlane=probe.nearClipPlane;
            captureCamera.farClipPlane=probe.farClipPlane;captureCamera.clearFlags=CameraClearFlags.SolidColor;
            captureCamera.backgroundColor=probe.backgroundColor;
            if(captureCube==null)
            {
                captureCube=new RenderTexture(probe.resolution,probe.resolution,24,RenderTextureFormat.ARGBHalf,RenderTextureReadWrite.Linear)
                {name="Battle sliced reflection cube",dimension=TextureDimension.Cube,useMipMap=true,autoGenerateMips=false};
                captureCube.Create();
            }
            captureFace=0;DiffuseStatus="capturing_owned_3d_cube";
        }
        internal void TickReflectionCapture()
        {
            if(released||captureFace<0||captureCamera==null||targetProbe==null)return;
            // One face per presentation tick; two cubes prevent sampling an
            // unfinished face while reflection materials render the next capture.
            if(!captureCamera.RenderToCubemap(captureCube,1<<captureFace))
            {captureFace=-1;DiffuseStatus="cube_render_failed_keep_existing_ambient";return;}
            if(++captureFace<6)return;
            captureFace=-1;captureCube.GenerateMips();
            var previousCube=completedCube;completedCube=captureCube;captureCube=previousCube;
            targetProbe.mode=ReflectionProbeMode.Custom;targetProbe.customBakedTexture=completedCube;
            if(!SystemInfo.supportsAsyncGPUReadback){DiffuseStatus="unavailable_keep_existing_ambient";return;}
            pending=true;
            int mip=Mathf.Clamp(Mathf.FloorToInt(Mathf.Log(completedCube.width/16f,2)),0,completedCube.mipmapCount-1);
            int generation=leaseGeneration;
            AsyncGPUReadback.Request(completedCube,mip,TextureFormat.RGBAFloat,request=>Project(request,generation));
        }
        private static Vector3 Direction(int face,float u,float v)
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
        private void Project(AsyncGPUReadbackRequest request,int generation)
        {
            if(this==null||released||generation!=leaseGeneration||stage==null)return;
            pending=false;
            if(request.hasError||request.layerCount!=6){DiffuseStatus="readback_failed_keep_existing_ambient";return;}
            var sh=new SphericalHarmonicsL2();var white=new SphericalHarmonicsL2();float minimum=float.MaxValue,maximum=0;
            for(int face=0;face<6;face++)
            {
                var pixels=request.GetData<Color>(face);
                if(pixels.Length!=request.width*request.height){DiffuseStatus="unexpected_cube_layout";return;}
                for(int y=0;y<request.height;y++)for(int x=0;x<request.width;x++)
                {
                    float u=2f*(x+.5f)/request.width-1,v=2f*(y+.5f)/request.height-1;
                    float weight=1f/Mathf.Pow(1+u*u+v*v,1.5f);var direction=Direction(face,u,v);
                    var c=pixels[y*request.width+x];
                    float luminance=c.r*.2126f+c.g*.7152f+c.b*.0722f;minimum=Mathf.Min(minimum,luminance);maximum=Mathf.Max(maximum,luminance);
                    c=new Color(Mathf.Clamp(c.r,0,4),Mathf.Clamp(c.g,0,4),Mathf.Clamp(c.b,0,4));
                    sh.AddDirectionalLight(direction,c,weight);white.AddDirectionalLight(direction,Color.white,weight);
                }
            }
            var values=new Color[6];white.Evaluate(new[]{Vector3.right,Vector3.left,Vector3.up,Vector3.down,Vector3.forward,Vector3.back},values);
            float mean=0;foreach(var c in values)mean+=c.r/6;
            if(!float.IsFinite(mean)||mean<=0){DiffuseStatus="invalid_irradiance";return;}
            CapturedRadianceRange=maximum-minimum;
            sh*=.2f/mean;sh+=RenderSettings.ambientProbe;
            irradiance=new[]{sh};DiffuseUpdates++;DiffuseStatus="local_scene_irradiance_applied";
            ApplyToSurfaces();
        }
        internal void ApplyToSurfaces()
        {
            if(released||stage==null||irradiance==null)return;
            var block=new MaterialPropertyBlock();BoundSurfaces=0;
            // Preserve current combat tints and water fields. Reapply only after the
            // existing scene presentation updates, never recapture each frame.
            var coefficient=DiffuseEnabled?irradiance:new[]{RenderSettings.ambientProbe};
            foreach(var surface in stage.GetComponentsInChildren<MeshRenderer>(true))
            {
                if(!surface.gameObject.activeInHierarchy||!surface.enabled||surface.shadowCastingMode==ShadowCastingMode.ShadowsOnly)continue;
                var material=surface.sharedMaterial;
                if(material==null||material.shader==null||
                    !(material.shader.name.StartsWith("OCC/HD2D/")||material.shader.name=="Universal Render Pipeline/Lit"))continue;
                block.Clear();surface.GetPropertyBlock(block);block.CopySHCoefficientArraysFrom(coefficient);
                surface.lightProbeUsage=LightProbeUsage.CustomProvided;surface.SetPropertyBlock(block);BoundSurfaces++;
            }
        }
        private void OnDestroy()=>Release();
        private void Release()
        {
            // An old asynchronous cube readback can arrive after another scene
            // has acquired the renderer lease. It must not write to released data.
            released=true;leaseGeneration++;pending=false;captureFace=-1;
            if(ownerCamera!=null)
            {
                var data=ownerCamera.GetComponent<UniversalAdditionalCameraData>();
                if(data!=null)data.SetRenderer(1);
            }
            if(pipeline!=null&&QualitySettings.renderPipeline==pipeline)QualitySettings.renderPipeline=previous;
            if(pipeline!=null)Destroy(pipeline);if(renderer!=null)Destroy(renderer);if(contact!=null)Destroy(contact);
            if(captureCamera!=null)Destroy(captureCamera.gameObject);
            if(captureCube!=null){captureCube.Release();Destroy(captureCube);}
            if(completedCube!=null){completedCube.Release();Destroy(completedCube);}
            pipeline=null;renderer=null;contact=null;captureCamera=null;captureCube=null;completedCube=null;
            ownerCamera=null;targetProbe=null;stage=null;irradiance=null;BoundSurfaces=0;DiffuseStatus="released";
            if(active==this)active=null;
        }
    }
}
