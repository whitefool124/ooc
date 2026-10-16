using UnityEngine;
using System.Linq;
using OCC.Combat.Presentation;
public static class InspectVistaUv {public static string Run(){
 var stage=Object.FindAnyObjectByType<MainMenuHd2DStage>();var item=stage==null?null:stage.transform.Find("独立学院窗景");
 if(item==null)return "missing vista";
 var mesh=item.GetComponent<MeshFilter>().sharedMesh;var mat=item.GetComponent<MeshRenderer>().sharedMaterial;
 return Newtonsoft.Json.JsonConvert.SerializeObject(new{bounds=mesh.bounds.ToString(),uv=mesh.uv.Select(p=>p.ToString("F5")).ToArray(),verts=mesh.vertices.Select(p=>p.ToString("F5")).ToArray(),scale=mat.mainTextureScale.ToString(),offset=mat.mainTextureOffset.ToString()});
}}
