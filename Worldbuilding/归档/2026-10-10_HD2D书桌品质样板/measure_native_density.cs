using System;
using System.Linq;
using System.Collections.Generic;
using UnityEngine;
using OCC.Combat.Presentation;
public static class MeasureNativeDensity {public static string Run(){
 var stage=UnityEngine.Object.FindAnyObjectByType<MainMenuHd2DStage>();
 if(stage==null)throw new InvalidOperationException("Missing actual desk");
 var records=new List<object>();
 foreach(var renderer in stage.GetComponentsInChildren<MeshRenderer>(false)){
  var filter=renderer.GetComponent<MeshFilter>();var material=renderer.sharedMaterial;
  if(filter==null||filter.sharedMesh==null||material==null||material.mainTexture==null)continue;
  var texture=material.mainTexture;if(texture.width<=1||texture.height<=1)continue;
  var mesh=filter.sharedMesh;var verts=mesh.vertices;var uv=mesh.uv;var tris=mesh.triangles;var pitches=new List<float>();
  if(uv.Length!=verts.Length)continue;
  for(int t=0;t<tris.Length;t+=3)for(int e=0;e<3;e++){
   int a=tris[t+e],b=tris[t+(e+1)%3];var d=uv[b]-uv[a];float pixels=0;
   if(Mathf.Abs(d.x)>1e-7f&&Mathf.Abs(d.y)<1e-7f)pixels=Mathf.Abs(d.x*texture.width*material.mainTextureScale.x);
   else if(Mathf.Abs(d.y)>1e-7f&&Mathf.Abs(d.x)<1e-7f)pixels=Mathf.Abs(d.y*texture.height*material.mainTextureScale.y);
   if(pixels>0)pitches.Add(Vector3.Distance(renderer.transform.TransformPoint(verts[a]),renderer.transform.TransformPoint(verts[b]))/pixels);
  }
  if(pitches.Count>0)records.Add(new{name=renderer.name,texture=texture.name,min_world_pitch=pitches.Min(),max_world_pitch=pitches.Max(),target=1f/32,
   declared_texel_spacing_matches=pitches.All(x=>Mathf.Abs(x-1f/32)<.00001f)});
 }
 return Newtonsoft.Json.JsonConvert.SerializeObject(new{screen=Screen.width+"x"+Screen.height,
  method="World mesh edge length / UV texel span. Does not infer macro grid or replace aesthetic review.",records});
}}
