using System.Linq;
using UnityEngine;
using UnityEngine.UI;
using OCC.Combat.Presentation;
public static class DeskRuntimeInspect {
 public static string Run(){
  if(Application.dataPath.Replace('\\','/')!="E:/数据库/OCC_Codex/UnityProject/Assets")return "Wrong project";
  var host=Object.FindAnyObjectByType<CombatPrototypeBootstrap>();
  var buttons=Object.FindObjectsByType<Button>(FindObjectsSortMode.None).Where(b=>b.isActiveAndEnabled).Select(b=>{var canvas=b.GetComponentInParent<Canvas>();var camera=canvas==null||canvas.renderMode==RenderMode.ScreenSpaceOverlay?null:canvas.worldCamera;var p=RectTransformUtility.WorldToScreenPoint(camera,((RectTransform)b.transform).TransformPoint(((RectTransform)b.transform).rect.center));return new{name=b.name,text=b.GetComponentInChildren<Text>()?.text,enabled=b.IsInteractable(),point=new[]{p.x,p.y}};}).ToArray();
  var cameras=Object.FindObjectsByType<Camera>(FindObjectsSortMode.None).Select(c=>new{c.name,c.allowHDR,c.allowMSAA,c.fieldOfView,c.orthographic,c.cullingMask,output=c.targetTexture==null?"screen":c.targetTexture.width+"x"+c.targetTexture.height}).ToArray();
  return Newtonsoft.Json.JsonConvert.SerializeObject(new {screen=Screen.width+"x"+Screen.height,host=host==null?"missing":host.name,buttons,cameras});
 }
}
