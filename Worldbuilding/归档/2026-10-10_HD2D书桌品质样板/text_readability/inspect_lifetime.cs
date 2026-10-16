using System;
using System.Linq;
using UnityEngine;
using Funplay.Editor.Tools.Scripting;
public class InspectDeskTextLifetime : IFunplayCommand {
 public void Execute(Funplay.Editor.Tools.Scripting.ExecutionContext ctx){
  if(!Application.isPlaying||Application.dataPath.Replace('\\','/')!="E:/数据库/OCC_Codex/UnityProject/Assets")throw new InvalidOperationException("Main project Play required");
  var roots=UnityEngine.Object.FindObjectsByType<Canvas>(FindObjectsInactive.Include,FindObjectsSortMode.None).Where(c=>c.name=="整备页清晰文字").Select(c=>new{c.name,active=c.gameObject.activeInHierarchy,c.sortingOrder,root=c.rootCanvas.name}).ToArray();
  ctx.ReturnValue=new{roots};
 }
}
