using System;
using System.Linq;
using UnityEngine;
using OCC.Combat.Presentation;
public static class InspectNativeDensity {
 public static string Run(){
  if(Application.dataPath.Replace('\\','/')!="E:/数据库/OCC_Codex/UnityProject/Assets")throw new InvalidOperationException("Wrong project");
  var stage=UnityEngine.Object.FindAnyObjectByType<MainMenuHd2DStage>();
  if(stage==null)throw new InvalidOperationException("Missing actual desk stage");
  var renderers=stage.GetComponentsInChildren<MeshRenderer>(true).Where(r=>r.enabled&&r.gameObject.activeInHierarchy).Select(r=>{
   var filter=r.GetComponent<MeshFilter>();var material=r.sharedMaterial;var texture=material==null?null:material.mainTexture;
   return new {name=r.name,scale=r.transform.lossyScale.ToString("F5"),texture=texture==null?"none":texture.name,
    filter=texture==null?"none":texture.filterMode.ToString(),vertices=filter==null||filter.sharedMesh==null?0:filter.sharedMesh.vertexCount};
  }).ToArray();
  return Newtonsoft.Json.JsonConvert.SerializeObject(new{screen=Screen.width+"x"+Screen.height,
   hasPersistentArt=stage.GetComponent<Hd2DDeskArtCandidate>()!=null,scenePPU=Hd2DDeskArtCandidate.ScenePixelsPerUnit,
   stageScale=stage.transform.lossyScale.ToString("F5"),renderers});
 }
}
