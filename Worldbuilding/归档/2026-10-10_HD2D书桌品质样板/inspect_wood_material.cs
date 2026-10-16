using System.Linq;
using UnityEngine;
using OCC.Combat.Presentation;
public static class InspectWoodMaterial {
 public static string Run(){
  if(Application.dataPath.Replace('\\','/')!="E:/数据库/OCC_Codex/UnityProject/Assets")return "Wrong project";
  var stage=Object.FindAnyObjectByType<MainMenuHd2DStage>();
  if(stage==null)return "Missing stage";
  var models=stage.GetComponentsInChildren<MeshRenderer>(true).Where(r=>r.name=="桌面实体"||r.name=="桌前沿"||r.name=="桌腿").Select(r=>new{
   r.name, material=r.sharedMaterial.name, shader=r.sharedMaterial.shader.name,
   smoothness=r.sharedMaterial.GetFloat("_Smoothness"), metallic=r.sharedMaterial.GetFloat("_Metallic"),
   specular=r.sharedMaterial.GetColor("_SpecColor").ToString(),specularWorkflow=r.sharedMaterial.IsKeywordEnabled("_SPECULAR_SETUP"),
   source=r.sharedMaterial.mainTexture.name,sampling=r.sharedMaterial.mainTexture.filterMode.ToString(),
   scale=r.transform.lossyScale.ToString(),vertices=r.GetComponent<MeshFilter>().sharedMesh.vertexCount
  }).ToArray();
  return Newtonsoft.Json.JsonConvert.SerializeObject(new{screen=Screen.width+"x"+Screen.height,models});
 }
}
