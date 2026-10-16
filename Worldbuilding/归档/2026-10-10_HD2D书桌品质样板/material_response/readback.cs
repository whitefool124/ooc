using UnityEngine;
using System.Linq;
using UnityEngine.UI;
using UnityEditor;
using OCC.Combat.Presentation;
using Funplay.Editor.Tools.Scripting;
public class DeskMaterialReadback : IFunplayCommand {
 public void Execute(Funplay.Editor.Tools.Scripting.ExecutionContext ctx){
  if(!Application.isPlaying||Application.dataPath.Replace('\\','/')!="E:/数据库/OCC_Codex/UnityProject/Assets")throw new System.Exception("Main project Play required");
  var stage=Object.FindObjectsByType<MainMenuHd2DStage>(FindObjectsSortMode.None).Single(s=>s.transform.Find("场景空间整备陈列")!=null);
  var shader=Shader.Find("OCC/HD2D/Surface");
  ctx.ReturnValue=new {brightness=new{hasKey=PlayerPrefs.HasKey("OCC.Visual.DeskLampBrightness.v1"),value=PlayerPrefs.GetFloat("OCC.Visual.DeskLampBrightness.v1",.4f)},screen=new[]{Screen.width,Screen.height},shaderErrors=ShaderUtil.GetShaderMessages(shader).Select(x=>new{x.message,x.line,severity=x.severity.ToString()}),materials=stage.GetComponentsInChildren<Renderer>(true).Where(r=>r.name=="桌面实体"||r.name=="独立2D以太晶灯"||r.name=="独立像素杯碟候选"||r.name=="背包格_0_0 场景面").Select(r=>new {r.name,shader=r.sharedMaterial.shader.name,smoothness=r.sharedMaterial.GetFloat("_Smoothness"),relief=r.sharedMaterial.GetFloat("_ReliefStrength"),emission=r.sharedMaterial.GetFloat("_EmissionStrength"),crystalMask=r.sharedMaterial.GetFloat("_CrystalMask"),texture=r.sharedMaterial.mainTexture.name}),probes=stage.GetComponentsInChildren<ReflectionProbe>().Select(p=>new{p.name,ready=p.texture!=null,p.resolution})};
 }
}
