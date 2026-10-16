using UnityEngine;
using OCC.Combat.Presentation;
public static class ShowServiceShopLocal {
 public static string Run(){
  if(!Application.isPlaying||Application.dataPath.Replace('\\','/')!="E:/数据库/OCC_Codex/UnityProject/Assets")throw new System.InvalidOperationException("Main project Play required");
  var host=Object.FindAnyObjectByType<CombatPrototypeBootstrap>();if(host==null)throw new System.InvalidOperationException("Missing host");
  var error=host.PreviewUiCaptureRun(false,"S");if(error!=null)return error;
  return host.PreviewUiCapturePage("room","S")??"LOCAL IN-MEMORY SHOP; not a normal journey verification";
 }
}
