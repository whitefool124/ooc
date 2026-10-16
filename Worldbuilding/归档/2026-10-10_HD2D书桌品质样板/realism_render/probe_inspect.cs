using UnityEngine;
using System.Linq;
using Funplay.Editor.Tools.Scripting;
public class ProbeInspect : IFunplayCommand {
 public void Execute(Funplay.Editor.Tools.Scripting.ExecutionContext ctx){
  var p=Object.FindObjectsByType<ReflectionProbe>(FindObjectsSortMode.None).Single(x=>x.name=="整备台局部环境反射");
  ctx.RegisterObjectModification(p);int id=p.RenderProbe();
  ctx.ReturnValue=new {id,finished=p.IsFinishedRendering(id),texture=p.texture==null?null:p.texture.name,realtime=p.realtimeTexture==null?null:p.realtimeTexture.name,enabled=p.enabled,mode=p.mode.ToString()};
 }
}
