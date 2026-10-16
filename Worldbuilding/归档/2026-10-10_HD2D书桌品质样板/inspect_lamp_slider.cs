using System.Linq;
using UnityEngine;
using UnityEngine.UI;
public static class InspectLampSlider {
 public static string Run(){
  var values=Object.FindObjectsByType<Slider>(FindObjectsSortMode.None).Where(s=>s.isActiveAndEnabled).Select(s=>{
   var area=(RectTransform)s.handleRect.parent;var c=s.GetComponentInParent<Canvas>().worldCamera;
   var left=RectTransformUtility.WorldToScreenPoint(c,area.TransformPoint(new Vector3(area.rect.xMin,0,0)));
   var right=RectTransformUtility.WorldToScreenPoint(c,area.TransformPoint(new Vector3(area.rect.xMax,0,0)));
   var handle=RectTransformUtility.WorldToScreenPoint(c,s.handleRect.TransformPoint(s.handleRect.rect.center));
   return new{s.name,s.value,left=new[]{left.x,left.y},right=new[]{right.x,right.y},handle=new[]{handle.x,handle.y}};});
  return Newtonsoft.Json.JsonConvert.SerializeObject(new{key_exists=PlayerPrefs.HasKey("OCC.Visual.DeskLampBrightness.v1"),stored=PlayerPrefs.GetFloat("OCC.Visual.DeskLampBrightness.v1",.4f),sliders=values});
 }
}
