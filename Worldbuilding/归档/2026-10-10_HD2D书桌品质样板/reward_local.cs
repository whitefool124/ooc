using UnityEngine;
using OCC.Combat.Presentation;
public static class RewardLocal {
 public static string Run(){
  if(!Application.isPlaying||Application.dataPath.Replace('\\','/')!="E:/数据库/OCC_Codex/UnityProject/Assets")throw new System.Exception("Main project Play required");
  var host=Object.FindAnyObjectByType<CombatPrototypeBootstrap>();if(host==null)throw new System.Exception("Missing host");
  var error=host.PreviewUiCaptureRun(false,"B1",false,"reward");
  return error??"LOCAL IN-MEMORY REWARD; not complete combat journey evidence";
 }
}

