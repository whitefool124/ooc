using UnityEngine;
using Funplay.Editor.Tools.Scripting;
using OCC.Combat.Presentation;
public class BagLightingPreview : IFunplayCommand {
 public void Execute(Funplay.Editor.Tools.Scripting.ExecutionContext ctx){
  if(!Application.isPlaying||Application.dataPath.Replace('\\','/')!="E:/数据库/OCC_Codex/UnityProject/Assets")throw new System.Exception("Main project Play required");
  MainMenuHd2DStage target=null;
  foreach(var s in Object.FindObjectsByType<MainMenuHd2DStage>(FindObjectsSortMode.None))if(s.transform.Find("场景空间整备陈列")!=null)target=s;
  if(target==null)throw new System.Exception("Loadout stage missing");
  foreach(var light in target.GetComponentsInChildren<Light>(true)){
   ctx.RegisterObjectModification(light);
   if(light.name=="窗外统一日光"){light.intensity=1.65f;light.color=new Color(1,.95f,.85f);ctx.RegisterObjectModification(light.transform);light.transform.localRotation=Quaternion.Euler(50,-25,0);}
   if(light.name=="窗边柔和补光")light.intensity=2.4f;
   if(light.name=="室内暖补光")light.intensity=1.2f;
   if(light.name=="整备陈列柔光")light.intensity=2.5f;
  }
  ctx.ReturnValue="Local loadout light preview; no asset/save change";
 }
}
