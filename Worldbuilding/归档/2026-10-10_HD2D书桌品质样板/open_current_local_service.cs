using UnityEngine;
using OCC.Combat.Presentation;
public static class OpenCurrentLocalService {
 public static string Run(){
  if(!Application.isPlaying||Application.dataPath.Replace('\\','/')!="E:/数据库/OCC_Codex/UnityProject/Assets")throw new System.InvalidOperationException("Main project Play required");
  var host=Object.FindAnyObjectByType<CombatPrototypeBootstrap>();if(host==null||host.CurrentMapRun==null)throw new System.InvalidOperationException("Missing host/run");
  return host.PreviewUiCapturePage("room",host.CurrentMapRun.CurrentNodeId)??"Opened current disposable service page";
 }
}
