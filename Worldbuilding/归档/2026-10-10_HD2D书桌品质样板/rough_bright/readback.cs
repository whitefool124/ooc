using UnityEngine;
using UnityEditor;
using System.Linq;
using System.Reflection;
using OCC.Combat.Presentation;
using Funplay.Editor.Tools.Scripting;
public class LightMaterialReadback : IFunplayCommand {
 public void Execute(Funplay.Editor.Tools.Scripting.ExecutionContext ctx){
  if(!Application.isPlaying||Application.dataPath.Replace('\\','/')!="E:/数据库/OCC_Codex/UnityProject/Assets")throw new System.Exception("Main project Play required");
  var stage=Object.FindObjectsByType<MainMenuHd2DStage>(FindObjectsSortMode.None).Single(s=>s.transform.Find("场景空间整备陈列")!=null);
  var refresh=stage.GetComponentsInChildren<MonoBehaviour>().Single(c=>c!=null&&c.GetType().Name=="Hd2DDeskReflectionRefresh");
  var captureCount=(int)refresh.GetType().GetProperty("CaptureRequests").GetValue(refresh);
  var shader=Shader.Find("OCC/HD2D/Surface");
  ctx.ReturnValue=new{screen=new[]{Screen.width,Screen.height},brightness=new{hasKey=PlayerPrefs.HasKey("OCC.Visual.DeskLampBrightness.v1"),value=PlayerPrefs.GetFloat("OCC.Visual.DeskLampBrightness.v1",.4f)},captureCount,materials=stage.GetComponentsInChildren<Renderer>(true).Where(r=>r.name=="桌面实体"||r.name=="独立2D以太晶灯").Select(r=>new{r.name,texture=r.sharedMaterial.mainTexture.name,coat=r.sharedMaterial.GetFloat("_CoatMask"),coatSmoothness=r.sharedMaterial.GetFloat("_CoatSmoothness"),smoothness=r.sharedMaterial.GetFloat("_Smoothness"),relief=r.sharedMaterial.GetFloat("_ReliefStrength"),roughnessVariation=r.sharedMaterial.GetFloat("_RoughnessVariation"),coatKeyword=r.sharedMaterial.IsKeywordEnabled("_CLEARCOAT")}),lights=stage.GetComponentsInChildren<Light>(true).Select(l=>new{l.name,l.intensity,l.enabled,shadows=l.shadows.ToString(),l.shadowStrength}),probes=stage.GetComponentsInChildren<ReflectionProbe>().Select(p=>new{p.resolution,ready=p.texture!=null}),shaderMessages=ShaderUtil.GetShaderMessages(shader).Select(m=>new{m.message,m.line})};
 }
}

