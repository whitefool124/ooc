#if UNITY_EDITOR
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using Newtonsoft.Json.Linq;
using UnityEditor;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;

namespace OCC.Combat.EditorTools
{
    // Research conversions only. Cooked packages retain parameters but strip some
    // arithmetic expressions; these conversions must not be called exact UE shaders.
    public sealed class OctopathStudyRendering
    {
        public bool MaterialParameters = true, ContactOcclusion = true, CloudShadows = true;
        public bool SourceGrade;
        public int SourceMaterialCount { get; private set; }
        private readonly List<Material> sourceMaterials = new List<Material>();
        private ScreenSpaceAmbientOcclusion ao;
        private UniversalAdditionalLightData cloudLight;
        private Light sun;
        private Texture2D cookie;
        private ColorAdjustments grade;
        private LiftGammaGain liftGamma;
        private RenderPipelineAsset originalQuality;
        private UniversalRenderPipelineAsset pipeline;
        private ScriptableRendererData rendererData;
        private float sourceRoughness,sourceSpecular,sourceMetallic,sourceNormal;
        private float cloudTime;
        private bool[] keys = new bool[5];
        private AmbientMode ambientMode;
        private Color sky,equator,ground,fogColor;
        private float ambientIntensity,reflectionIntensity,fogStart,fogEnd;
        private SphericalHarmonicsL2 ambientProbe;
        private DefaultReflectionMode reflectionMode;
        private Texture reflectionTexture;
        private bool fog;
        private FogMode fogMode;
        private Light originalSun;

        public void Initialize()
        {
            ambientMode=RenderSettings.ambientMode;sky=RenderSettings.ambientSkyColor;
            equator=RenderSettings.ambientEquatorColor;ground=RenderSettings.ambientGroundColor;
            ambientIntensity=RenderSettings.ambientIntensity;ambientProbe=RenderSettings.ambientProbe;
            reflectionMode=RenderSettings.defaultReflectionMode;reflectionTexture=RenderSettings.customReflectionTexture;
            reflectionIntensity=RenderSettings.reflectionIntensity;originalSun=RenderSettings.sun;
            fog=RenderSettings.fog;fogMode=RenderSettings.fogMode;fogColor=RenderSettings.fogColor;
            fogStart=RenderSettings.fogStartDistance;fogEnd=RenderSettings.fogEndDistance;
            originalQuality=QualitySettings.renderPipeline;
            var original=GraphicsSettings.currentRenderPipeline as UniversalRenderPipelineAsset;
            if(original==null)throw new InvalidOperationException("Study requires URP");
            var listField=typeof(UniversalRenderPipelineAsset).GetField("m_RendererDataList",BindingFlags.Instance|BindingFlags.NonPublic);
            var list=((ScriptableRendererData[])listField.GetValue(original)).ToArray();
            if(list.Length<2||list[1]==null)throw new InvalidOperationException("Missing HD2D renderer");
            pipeline=UnityEngine.Object.Instantiate(original);pipeline.name="Octopath study transient pipeline";
            rendererData=UnityEngine.Object.Instantiate(list[1]);rendererData.name="Octopath study transient renderer";
            rendererData.rendererFeatures.Clear();
            ao=ScriptableObject.CreateInstance<ScreenSpaceAmbientOcclusion>();ao.name="Original 45cm contact AO conversion";
            var settings=new SerializedObject(ao);var s=settings.FindProperty("m_Settings");
            s.FindPropertyRelative("Source").enumValueIndex=0; // Reconstruct normals from depth; no missing DepthNormals pass.
            s.FindPropertyRelative("NormalSamples").enumValueIndex=2;
            s.FindPropertyRelative("AOMethod").enumValueIndex=1;
            s.FindPropertyRelative("Samples").enumValueIndex=0;
            s.FindPropertyRelative("Downsample").boolValue=false;
            s.FindPropertyRelative("Intensity").floatValue=1;
            s.FindPropertyRelative("Radius").floatValue=.45f; // UE 45cm -> Unity metres.
            s.FindPropertyRelative("DirectLightingStrength").floatValue=0;
            s.FindPropertyRelative("Falloff").floatValue=100;
            settings.ApplyModifiedPropertiesWithoutUndo();ao.Create();
            rendererData.rendererFeatures.Add(ao);list[1]=rendererData;listField.SetValue(pipeline,list);
            QualitySettings.renderPipeline=pipeline;
            var collection=JArray.Parse(File.ReadAllText(Path.Combine(OctopathTownStudy.Folder,"functions/collection.json")))[0]["Properties"]["ScalarParameters"];
            float Scalar(string name)=> (float)collection.First(p=>(string)p["ParameterName"]==name)["DefaultValue"];
            sourceRoughness=Scalar("RoughnessValue");sourceSpecular=Scalar("SpecularValue");
            sourceMetallic=Scalar("MetallicValue");sourceNormal=Scalar("NormalPower");
        }

        public void BindMaterials(IEnumerable<Material> materials)
        {
            foreach(var m in materials)if(m.GetFloat("_UseSourceParameters")>.5f)sourceMaterials.Add(m);
            SourceMaterialCount=sourceMaterials.Count;ApplyMaterials();
        }
        private void ApplyMaterials()
        {
            foreach(var m in sourceMaterials)
            {
                m.SetFloat("_UseSourceParameters",MaterialParameters?1:0);
                m.SetFloat("_SourceRoughness",sourceRoughness);
                m.SetFloat("_SourceSpecular",sourceSpecular);
                m.SetFloat("_SourceMetallic",sourceMetallic);
                m.SetFloat("_SourceNormal",sourceNormal);
            }
        }

        public void BindCloud(Light light)
        {
            sun=light;
            string path=Path.Combine(OctopathTownStudy.Folder,"functions/packages/Octopath_Traveler/Content/Map/Resource/Environment/CloudShadow/envtx_cloudshadow.png");
            cookie=new Texture2D(2,2,TextureFormat.RGBA32,true,true){name="Original cloud mask power 6",filterMode=FilterMode.Trilinear,wrapMode=TextureWrapMode.Repeat};
            cookie.LoadImage(File.ReadAllBytes(path));
            var pixels=cookie.GetPixels();for(int i=0;i<pixels.Length;i++){float v=Mathf.Pow(pixels[i].r,6);pixels[i]=new Color(v,v,v,1);}
            cookie.SetPixels(pixels);cookie.Apply(true);
            cloudLight=light.GetUniversalAdditionalLightData();
            cloudLight.lightCookieSize=new Vector2(26.87f,38.367756f);
            light.cookie=cookie;
        }
        public void BindGrade(ColorAdjustments value,LiftGammaGain lift)
        {grade=value;liftGamma=lift;ApplyGrade();}
        private void ApplyGrade()
        {
            if(grade==null||liftGamma==null)return;
            // URP gamma W is an OFFSET, not UE's multiplier: .6 -> -.4.
            // Contrast's curve, shadow grading, exposure and film remain engine conversions.
            grade.contrast.Override(SourceGrade?-20:8);
            grade.saturation.Override(-10);
            liftGamma.gamma.Override(new Vector4(1,1,1,SourceGrade?-.4f:.2f));
            liftGamma.gain.Override(new Vector4(1,1,1,SourceGrade?.2f:0));
        }
        public void SetEffect(int index,bool enabled)
        {
            if(index==1){MaterialParameters=enabled;ApplyMaterials();}
            if(index==2){ContactOcclusion=enabled;ao.SetActive(enabled);}
            if(index==3){CloudShadows=enabled;sun.cookie=enabled?cookie:null;}
            if(index==4){SourceGrade=enabled;ApplyGrade();}
        }
        public string Readback()=> $"materials={SourceMaterialCount}, parameters={MaterialParameters}, AO={ContactOcclusion}, cloud={CloudShadows}, sourceGrade={SourceGrade}, cloudOffset={cloudLight.lightCookieOffset}, pipeline={QualitySettings.renderPipeline.name}";
        public void Tick()
        {
            // Panner speed components were stripped. This slow motion is a declared
            // approximation; the source Speed=1 does not reveal its missing XY rates.
            if(CloudShadows&&cloudLight!=null){cloudTime+=Time.unscaledDeltaTime;cloudLight.lightCookieOffset=new Vector2(cloudTime*.08f,cloudTime*.05f);}
            var k=UnityEngine.InputSystem.Keyboard.current;if(k==null)return;
            for(int i=1;i<=4;i++){
                bool down=k[(UnityEngine.InputSystem.Key)((int)UnityEngine.InputSystem.Key.Digit1+i-1)].isPressed;
                if(down&&!keys[i])SetEffect(i,i==1?!MaterialParameters:i==2?!ContactOcclusion:i==3?!CloudShadows:!SourceGrade);
                keys[i]=down;
            }
        }
        public void Cleanup()
        {
            if(pipeline!=null&&QualitySettings.renderPipeline==pipeline)QualitySettings.renderPipeline=originalQuality;
            RenderSettings.ambientMode=ambientMode;RenderSettings.ambientSkyColor=sky;RenderSettings.ambientEquatorColor=equator;RenderSettings.ambientGroundColor=ground;
            RenderSettings.ambientIntensity=ambientIntensity;RenderSettings.ambientProbe=ambientProbe;
            RenderSettings.defaultReflectionMode=reflectionMode;RenderSettings.customReflectionTexture=reflectionTexture;RenderSettings.reflectionIntensity=reflectionIntensity;RenderSettings.sun=originalSun;
            RenderSettings.fog=fog;RenderSettings.fogMode=fogMode;RenderSettings.fogColor=fogColor;RenderSettings.fogStartDistance=fogStart;RenderSettings.fogEndDistance=fogEnd;
            if(pipeline!=null)UnityEngine.Object.Destroy(pipeline);if(rendererData!=null)UnityEngine.Object.Destroy(rendererData);if(ao!=null)UnityEngine.Object.Destroy(ao);if(cookie!=null)UnityEngine.Object.Destroy(cookie);
        }
    }
}
#endif
