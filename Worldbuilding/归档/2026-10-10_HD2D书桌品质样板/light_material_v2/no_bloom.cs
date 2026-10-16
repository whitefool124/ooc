using UnityEngine;
using System.Linq;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;
using Funplay.Editor.Tools.Scripting;
public class DeskBloomOff : IFunplayCommand {
 public void Execute(Funplay.Editor.Tools.Scripting.ExecutionContext ctx){
  if(!Application.isPlaying||Application.dataPath.Replace('\\','/')!="E:/数据库/OCC_Codex/UnityProject/Assets")throw new System.Exception("Main project Play required");
  var volume=Object.FindObjectsByType<Volume>(FindObjectsSortMode.None).Single(v=>v.gameObject.name=="主菜单独立景深与色调");
  if(!volume.profile.TryGet<Bloom>(out var bloom))throw new System.Exception("Missing desk bloom");
  ctx.RegisterObjectModification(bloom);bloom.active=false;ctx.ReturnValue=new{bloom.active};
 }
}

