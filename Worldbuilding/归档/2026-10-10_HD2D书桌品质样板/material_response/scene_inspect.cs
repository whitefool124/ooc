using UnityEngine;
using System.Linq;
using UnityEngine.UI;
using OCC.Combat.Presentation;
using Funplay.Editor.Tools.Scripting;
public class MaterialsSceneInspect : IFunplayCommand {
 public void Execute(Funplay.Editor.Tools.Scripting.ExecutionContext ctx){
  var s=Object.FindObjectsByType<MainMenuHd2DStage>(FindObjectsSortMode.None).Single(x=>x.transform.Find("场景空间整备陈列")!=null);
  ctx.ReturnValue=new {light=s.GetComponentsInChildren<Light>(true).Select(l=>new{l.name,position=l.transform.localPosition,rotation=l.transform.localEulerAngles}),images=s.GetComponentsInChildren<Image>(true).Where(i=>i.sprite!=null&&i.gameObject.activeInHierarchy).Select(i=>new {i.name,texture=i.mainTexture.name}).Take(35)};
 }
}
