using UnityEngine;
using System.Linq;
using UnityEngine.UI;
using OCC.Combat.Presentation;
using Funplay.Editor.Tools.Scripting;
public class MaterialNames : IFunplayCommand {
 public void Execute(Funplay.Editor.Tools.Scripting.ExecutionContext ctx){
 var s=Object.FindObjectsByType<MainMenuHd2DStage>(FindObjectsSortMode.None).Single(x=>x.transform.Find("场景空间整备陈列")!=null);
 ctx.ReturnValue=string.Join("\n",s.GetComponentsInChildren<Image>(true).Where(i=>i.sprite!=null&&i.gameObject.activeInHierarchy).Select(i=>i.name+" | "+i.mainTexture.name+" | "+i.transform.parent.name));
 }
}
