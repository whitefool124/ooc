using UnityEngine;
using System.Linq;
using OCC.Combat.Presentation;
using UnityEngine.Rendering.Universal;
public static class RealismReadback {
 public static string Run(){
 var stages=Object.FindObjectsByType<MainMenuHd2DStage>(FindObjectsSortMode.None);
 return Newtonsoft.Json.JsonConvert.SerializeObject(new {screen=new[]{Screen.width,Screen.height},stages=stages.Select(s=>new {s.name,layer=s.gameObject.layer,output=new[]{s.Output.width,s.Output.height},post=s.sceneCamera.GetUniversalAdditionalCameraData().renderPostProcessing,probes=s.GetComponentsInChildren<ReflectionProbe>().Select(p=>new {p.name,p.resolution,p.intensity,ready=p.texture!=null,texture=p.texture==null?null:p.texture.name}),exposure=s.sceneVolume.profile.components.OfType<ColorAdjustments>().Select(g=>g.postExposure.value),depth=s.sceneVolume.profile.components.OfType<DepthOfField>().Select(d=>d.active)})});
 }
}
