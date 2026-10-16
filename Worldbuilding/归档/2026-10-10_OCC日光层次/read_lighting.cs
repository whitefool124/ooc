using System;
using System.Linq;
using System.Reflection;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;
public static class ReadLighting {
 public static object Run(){
  var stage=Resources.FindObjectsOfTypeAll<MonoBehaviour>().First(x=>x.GetType().Name=="MainMenuHd2DStage"&&x.gameObject.activeInHierarchy);
  var flags=BindingFlags.Instance|BindingFlags.NonPublic|BindingFlags.Public;
  var volume=(Volume)stage.GetType().GetField("sceneVolume",flags).GetValue(stage);
  volume.profile.TryGet<ColorAdjustments>(out var grade);
  var lights=stage.GetComponentsInChildren<Light>().Select(x=>new{name=x.name,type=x.type.ToString(),intensity=x.intensity,color=new[]{x.color.r,x.color.g,x.color.b},shadow=x.shadows.ToString()}).ToArray();
  return Newtonsoft.Json.JsonConvert.SerializeObject(new{path=Application.dataPath,stageLayer=stage.gameObject.layer,status=OCC.Combat.EditorTools.AcademyJourneyAudit.Status,output=new[]{Screen.width,Screen.height},exposure=grade.postExposure.value,contrast=grade.contrast.value,lights});
 }
}
