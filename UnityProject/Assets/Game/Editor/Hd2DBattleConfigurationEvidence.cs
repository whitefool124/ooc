using System.IO;
using System.Linq;
using Newtonsoft.Json;
using OCC.Combat.Presentation;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;

namespace OCC.Combat.EditorTools
{
    public static class Hd2DBattleConfigurationEvidence
    {
        private static float[] Colour(Color c)=>new[]{c.r,c.g,c.b,c.a};
        private static float[] Vector(Vector3 v)=>new[]{v.x,v.y,v.z};
        public static void Write(Hd2DBattlefieldSample sample,string folder)
        {
            var camera=sample.RenderCamera;var root=camera.transform.parent;
            var pipeline=GraphicsSettings.currentRenderPipeline as UniversalRenderPipelineAsset;
            var profile=root.GetComponentInChildren<Volume>().sharedProfile;
            profile.TryGet<Tonemapping>(out var tone);profile.TryGet<ColorAdjustments>(out var grade);
            profile.TryGet<Bloom>(out var bloom);profile.TryGet<DepthOfField>(out var focus);
            var renderers=root.GetComponentsInChildren<MeshRenderer>(false).Where(r=>r.enabled&&r.shadowCastingMode!=ShadowCastingMode.ShadowsOnly).ToArray();
            var data=camera.GetUniversalAdditionalCameraData();
            File.WriteAllText(Path.Combine(folder,"actual_render_configuration.json"),JsonConvert.SerializeObject(new{
                schema="occ-hd2d-actual-render-configuration-v1",status="QA_PENDING",source="Current live camera, owned URP lease, Volume and material readback",
                hdr=new{camera=camera.allowHDR,pipeline=pipeline.supportsHDR,targetFormat=camera.targetTexture.format.ToString(),colourSpace=QualitySettings.activeColorSpace.ToString()},
                msaa=new{pipeline=pipeline.msaaSampleCount,camera=camera.allowMSAA,target=camera.targetTexture.antiAliasing},
                output_policy=new{pipeline.renderScale,target=new[]{camera.targetTexture.width,camera.targetTexture.height},supportedCanvasResolutions=new[]{new[]{1920,1080},new[]{960,540}},sourcePitch=1f/32},
                tone_mapping=tone?.mode.value.ToString(),exposure=grade?.postExposure.value,
                bloom=bloom==null?null:new{bloom.active,intensity=bloom.intensity.value,threshold=bloom.threshold.value,scatter=bloom.scatter.value,tint=Colour(bloom.tint.value)},
                depth_of_field=focus==null?null:new{focus.active,mode=focus.mode.value.ToString(),start=focus.gaussianStart.value,end=focus.gaussianEnd.value,radius=focus.gaussianMaxRadius.value},
                shadows=new{pipeline.shadowDistance,pipeline.shadowCascadeCount,pipeline.mainLightShadowmapResolution,pipeline.supportsMainLightShadows,pipeline.supportsAdditionalLightShadows},
                lights=root.GetComponentsInChildren<Light>(false).Select(l=>new{l.name,type=l.type.ToString(),colour=Colour(l.color),l.intensity,l.range,l.enabled,rotation=Vector(l.transform.localEulerAngles),shadows=l.shadows.ToString(),l.shadowStrength,l.shadowBias,l.shadowNormalBias,l.cullingMask}).ToArray(),
                material_profile=new{shaders=renderers.Select(r=>r.sharedMaterial?.shader.name).Where(n=>n!=null).Distinct().ToArray(),reflectionCaptures=sample.ReflectionCaptures,localDiffuseUpdates=sample.LocalDiffuseUpdates,localDiffuseStatus=sample.LocalDiffuseStatus},
                source_sampling=new{scope="Actual visible texture sampling, with runtime source-copy tracing in local_density_configuration.json",sourceTexelWorldPitch=1f/32,macroPixelVerdict="PENDING"},
                camera=new{camera.fieldOfView,position=Vector(camera.transform.localPosition),rotation=Vector(camera.transform.localEulerAngles),renderer=data.scriptableRenderer.GetType().Name,data.renderPostProcessing},
                models=renderers.Where(r=>r.GetComponent<MeshFilter>()?.sharedMesh.vertexCount>4).Select(r=>new{r.name,vertices=r.GetComponent<MeshFilter>().sharedMesh.vertexCount,shader=r.sharedMaterial?.shader.name}).ToArray(),
                genuineHumanReview="PENDING",normalEntry=false,playerPerformance="NOT_MEASURED"
            },Formatting.Indented));
        }
    }
}
