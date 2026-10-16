using System;
using System.Linq;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;
using Funplay.Editor.Tools.Scripting;
public class LampPointerHigh : IFunplayCommand {
 public void Execute(Funplay.Editor.Tools.Scripting.ExecutionContext ctx){
  if(!Application.isPlaying||Application.dataPath.Replace('\\','/')!="E:/数据库/OCC_Codex/UnityProject/Assets")throw new InvalidOperationException("Wrong context");
  var slider=UnityEngine.Object.FindObjectsByType<Slider>(FindObjectsSortMode.None).Single(s=>s.isActiveAndEnabled&&s.name=="晶灯亮度滑条");
  var area=(RectTransform)slider.handleRect.parent;var camera=slider.GetComponentInParent<Canvas>().worldCamera;
  var p=RectTransformUtility.WorldToScreenPoint(camera,area.TransformPoint(new Vector3(area.rect.xMax+8,0,0)));
  var data=new PointerEventData(EventSystem.current){position=p,button=PointerEventData.InputButton.Left};
  var hits=new List<RaycastResult>();EventSystem.current.RaycastAll(data,hits);
  if(hits.Count==0||hits[0].gameObject.GetComponentInParent<Slider>()!=slider){ctx.ReturnValue=new{failed="slider raycast blocked",hits=hits.Select(h=>h.gameObject.name).ToArray()};return;}
  data.pointerCurrentRaycast=hits[0];data.pointerPressRaycast=hits[0];
  ctx.RegisterObjectModification(slider);
  ExecuteEvents.Execute(slider.gameObject,data,ExecuteEvents.pointerDownHandler);
  ExecuteEvents.Execute(slider.gameObject,data,ExecuteEvents.pointerUpHandler);
  ctx.ReturnValue=new{strategy="visible slider raycast plus EventSystem pointer down/up",value=slider.value,hit=hits[0].gameObject.name};
 }
}
