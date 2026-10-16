using UnityEngine;
using OCC.Combat.Presentation;
public static class ShowServiceWorkshopLocal {
 public static string Run(){
  if(!Application.isPlaying||Application.dataPath.Replace('\\','/')!="E:/数据库/OCC_Codex/UnityProject/Assets")throw new System.InvalidOperationException("Main project Play required");
  var host=Object.FindAnyObjectByType<CombatPrototypeBootstrap>();if(host==null)throw new System.InvalidOperationException("Missing host");
  var error=host.PreviewUiCaptureRun(false,"W");if(error!=null)return error;
  return host.PreviewUiCapturePage("room","W")??"LOCAL IN-MEMORY WORKSHOP; not a normal journey verification";
 }
}
