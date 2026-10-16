using System;
using System.Linq;
using UnityEngine;
using UnityEngine.UI;
using Funplay.Editor.Tools.Scripting;
public class InspectDeskText : IFunplayCommand {
 public void Execute(Funplay.Editor.Tools.Scripting.ExecutionContext ctx){
  if(Application.dataPath.Replace('\\','/')!="E:/数据库/OCC_Codex/UnityProject/Assets"||!Application.isPlaying)throw new InvalidOperationException("Main project Play required");
  ctx.ReturnValue=UnityEngine.Object.FindObjectsByType<Text>(FindObjectsSortMode.None).Where(t=>t.isActiveAndEnabled&&!string.IsNullOrEmpty(t.text)).Select(t=>{
   var c=t.GetComponentInParent<Canvas>();var camera=c==null||c.renderMode==RenderMode.ScreenSpaceOverlay?null:c.worldCamera;
   var r=t.rectTransform;var p=RectTransformUtility.WorldToScreenPoint(camera,r.TransformPoint(r.rect.center));
   var a=RectTransformUtility.WorldToScreenPoint(camera,r.TransformPoint(Vector3.zero));var b=RectTransformUtility.WorldToScreenPoint(camera,r.TransformPoint(Vector3.up*t.fontSize));
   return new{t.name,t.text,font=t.font==null?null:t.font.name,t.fontSize,t.resizeTextForBestFit,t.horizontalOverflow,t.verticalOverflow,t.alignment,color=new[]{t.color.r,t.color.g,t.color.b,t.color.a},size=new[]{r.rect.width,r.rect.height},preferred=new[]{t.preferredWidth,t.preferredHeight},point=new[]{p.x,p.y},projectedFont=(b-a).magnitude,parent=t.transform.parent.name};
  }).ToArray();
 }
}
