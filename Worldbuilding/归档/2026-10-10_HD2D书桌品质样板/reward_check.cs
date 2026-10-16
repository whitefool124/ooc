using UnityEngine;
using UnityEngine.UI;
using System.Linq;
using OCC.Combat.Presentation;
public static class RewardCheck {
 public static string Run(){
  var h=Object.FindAnyObjectByType<CombatPrototypeBootstrap>();var r=h.CurrentMapRun;
  var icons=Object.FindObjectsByType<Image>(FindObjectsSortMode.None).Where(x=>x.name.StartsWith("奖励大图标_")).Select(x=>new {x.name,native=new[]{x.sprite.rect.width,x.sprite.rect.height},pitch=new[]{x.rectTransform.rect.width*Mathf.Abs(x.transform.lossyScale.x)/x.sprite.rect.width,x.rectTransform.rect.height*Mathf.Abs(x.transform.lossyScale.y)/x.sprite.rect.height},mesh=x.GetComponentInChildren<MeshFilter>()?.sharedMesh?.bounds.ToString()});
  return Newtonsoft.Json.JsonConvert.SerializeObject(new {r.CurrentNodeId,r.SelectedRewardId,r.RewardChoicesOpened,r.LootPendingExit,r.PendingRewardStepId,icons,localPreview=true});
 }
}
