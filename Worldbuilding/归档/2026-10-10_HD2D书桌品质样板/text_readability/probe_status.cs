using System;
using System.Linq;
using UnityEngine;
using UnityEngine.UI;
using Funplay.Editor.Tools.Scripting;
public class ProbeDeskStatus : IFunplayCommand {
 public void Execute(Funplay.Editor.Tools.Scripting.ExecutionContext ctx){
  if(!Application.isPlaying||Application.dataPath.Replace('\\','/')!="E:/数据库/OCC_Codex/UnityProject/Assets")throw new InvalidOperationException("Main project Play required");
  var source=UnityEngine.Object.FindObjectsByType<Text>(FindObjectsInactive.Include,FindObjectsSortMode.None).FirstOrDefault(t=>t.name=="背包交互状态");
  if(source==null)throw new InvalidOperationException("Missing backpack status source");
  ctx.RegisterObjectModification(source);
  var original=source.text;source.text="背包空间不足，请先整理或取消放置";
  ctx.ReturnValue=new{original,probe=source.text,scope="Temporary text-binding probe only; not a full-backpack gameplay test"};
 }
}
