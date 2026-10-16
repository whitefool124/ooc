using UnityEngine;
using OCC.Combat.Presentation;
public static class InspectServiceState {
 public static string Run(){
  if(!Application.isPlaying||Application.dataPath.Replace('\\','/')!="E:/数据库/OCC_Codex/UnityProject/Assets")return "Main project Play required";
  var host=Object.FindAnyObjectByType<CombatPrototypeBootstrap>();if(host==null||host.CurrentMapRun==null)return "Missing run";
  var dto=host.CurrentMapRun.RogueRunState;
  return Newtonsoft.Json.JsonConvert.SerializeObject(new{host.CurrentMapRun.CurrentNodeId,dto.Gold,dto.StageContribution,dto.CurrentHealth,
    dto.CurrentMana,dto.EquipmentInstances,dto.TacticalItemInstances,
    inMemoryPreview=true,scope="service local UI route; not player save or normal journey evidence"});
 }
}
