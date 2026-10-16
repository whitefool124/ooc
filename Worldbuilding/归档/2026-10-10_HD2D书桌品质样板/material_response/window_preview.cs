using UnityEngine;
using System.Linq;
using OCC.Combat.Presentation;
using Funplay.Editor.Tools.Scripting;
public class WindowLightPreview : IFunplayCommand {
 public void Execute(Funplay.Editor.Tools.Scripting.ExecutionContext ctx){
  if(!Application.isPlaying||Application.dataPath.Replace('\\','/')!="E:/数据库/OCC_Codex/UnityProject/Assets")throw new System.Exception("Main project Play required");
  var stage=Object.FindObjectsByType<MainMenuHd2DStage>(FindObjectsSortMode.None).Single(s=>s.transform.Find("场景空间整备陈列")!=null);
  var sun=stage.GetComponentsInChildren<Light>(true).Single(x=>x.name=="窗外统一日光");
  ctx.RegisterObjectModification(sun.transform);sun.transform.localRotation=Quaternion.Euler(50,155,0);
  var fill=stage.GetComponentsInChildren<Light>(true).Single(x=>x.name=="室内暖补光");
  ctx.RegisterObjectModification(fill);fill.intensity=4f;
  ctx.ReturnValue="Window-facing daylight preview; front fill 4; no asset/scene save.";
 }
}
