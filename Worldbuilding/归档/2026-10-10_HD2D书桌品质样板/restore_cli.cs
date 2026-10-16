using System;
using System.Linq;
using System.Reflection;
using UnityEngine;
using Funplay.Editor.Tools.Scripting;
public class RestoreOccCli : IFunplayCommand {
 public void Execute(Funplay.Editor.Tools.Scripting.ExecutionContext ctx) {
  if(Application.dataPath.Replace('\\','/')!="E:/数据库/OCC_Codex/UnityProject/Assets")throw new InvalidOperationException("Wrong project");
  var types=AppDomain.CurrentDomain.GetAssemblies().SelectMany(a=>{try{return a.GetTypes();}catch{return new Type[0];}}).ToArray();
  var startup=types.FirstOrDefault(t=>t.FullName=="Unity.Pipeline.Editor.PipelineServerStartup");
  if(startup==null)throw new InvalidOperationException("Missing Pipeline startup type");
  var field=startup.GetField("m_Server",BindingFlags.NonPublic|BindingFlags.Static);
  var server=field==null?null:field.GetValue(null);
  if(server==null)throw new InvalidOperationException("Missing existing Pipeline server");
  var type=server.GetType();
  type.GetMethod("Stop",BindingFlags.Public|BindingFlags.Instance).Invoke(server,null);
  type.GetMethod("Start",BindingFlags.Public|BindingFlags.Instance).Invoke(server,new object[]{7801});
  ctx.Log("Existing authenticated Pipeline server restarted on session port 7801; no settings or asset saved");
 }
}
