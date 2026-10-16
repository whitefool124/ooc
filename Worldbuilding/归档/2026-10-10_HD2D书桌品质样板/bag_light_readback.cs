using UnityEngine;
using System.Linq;
using Funplay.Editor.Tools.Scripting;
using OCC.Combat.Presentation;
public class BagLightReadback : IFunplayCommand {
 public void Execute(Funplay.Editor.Tools.Scripting.ExecutionContext ctx){
  if(!Application.isPlaying||Application.dataPath.Replace('\\','/')!="E:/数据库/OCC_Codex/UnityProject/Assets")throw new System.Exception("Main project Play required");
  var stage=Object.FindObjectsByType<MainMenuHd2DStage>(FindObjectsSortMode.None).FirstOrDefault(x=>x.transform.Find("场景空间整备陈列")!=null);
  if(stage==null)throw new System.Exception("Loadout stage missing");
  ctx.ReturnValue=new {screen=new[]{Screen.width,Screen.height},gpu=SystemInfo.graphicsDeviceName,cpu=SystemInfo.processorType,stageLayer=stage.gameObject.layer,lights=stage.GetComponentsInChildren<Light>(true).Select(x=>new {x.name,x.enabled,x.intensity,color=new[]{x.color.r,x.color.g,x.color.b},shadows=x.shadows.ToString(),x.cullingMask}),cellCasters=stage.GetComponentsInChildren<MeshRenderer>(true).Where(x=>x.name.StartsWith("背包格_")).Select(x=>new{x.name,casts=x.shadowCastingMode.ToString()}),materials=stage.GetComponentsInChildren<MeshRenderer>(true).Where(x=>x.name=="桌面实体").Select(x=>new{x.name,texture=x.sharedMaterial.mainTexture.name,smoothness=x.sharedMaterial.GetFloat("_Smoothness"),metallic=x.sharedMaterial.GetFloat("_Metallic")})};
 }
}
