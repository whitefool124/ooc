using System;
using System.Linq;
using System.IO;
using UnityEngine;
using UnityEngine.Rendering;
using OCC.Combat.EditorTools;
using Funplay.Editor.Tools.Scripting;
public class PlaceSprite : IFunplayCommand {
 public void Execute(Funplay.Editor.Tools.Scripting.ExecutionContext ctx){
  if(!Application.isPlaying||Application.dataPath.Replace('\\','/')!="E:/数据库/OCC_Codex/UnityProject/Assets"||OctopathTownStudy.Root==null)throw new InvalidOperationException("Main study Play required");
  var root=OctopathTownStudy.Root;
  if(root.transform.Find("Laughing yellow pixel guest")!=null)throw new InvalidOperationException("Guest exists");
  // Temporary collision surfaces only to establish actual walkable height.
  foreach(var r in root.GetComponentsInChildren<MeshRenderer>()){
   if(!r.name.Contains("T_Ground")&&!r.name.Contains("Floor"))continue;
   var mf=r.GetComponent<MeshFilter>();if(mf==null)continue;
   var collider=r.gameObject.AddComponent<MeshCollider>();collider.sharedMesh=mf.sharedMesh;ctx.RegisterObjectCreation(collider);
  }
  Physics.SyncTransforms();
  var point=new Vector3(-17,12,0);
  if(!Physics.Raycast(point,Vector3.down,out var hit,30,1<<30))throw new InvalidOperationException("No ground");
  var texture=new Texture2D(2,2,TextureFormat.RGBA32,false,false){name="Independent yellow guest native pixels",filterMode=FilterMode.Point,wrapMode=TextureWrapMode.Clamp};
  texture.LoadImage(File.ReadAllBytes("E:/数据库/OCC_Codex/Worldbuilding/归档/2026-10-10_城镇像素角色练习/sprite_r1.png"));
  var material=new Material(Shader.Find("OCC/Research/OctopathSurface")){name="QA_PENDING yellow guest lit cutout",mainTexture=texture,color=new Color(.72f,.72f,.72f,1)};
  material.SetFloat("_AlphaClip",1);material.SetFloat("_Cutoff",.5f);material.EnableKeyword("_ALPHATEST_ON");material.renderQueue=2450;
  material.SetFloat("_Smoothness",.06f);material.SetFloat("_Metallic",0);material.SetFloat("_Cull",0);material.SetFloat("_SoftEmission",0);
  material.SetColor("_StudyAmbient",new Color(.22f,.24f,.25f));
  var mesh=new Mesh{name="Guest 32PPU bottom anchored card"};float w=texture.width/32f,h=texture.height/32f;
  mesh.vertices=new[]{new Vector3(-w/2,-1/32f,0),new Vector3(w/2,-1/32f,0),new Vector3(w/2,h-1/32f,0),new Vector3(-w/2,h-1/32f,0)};
  mesh.uv=new[]{Vector2.zero,Vector2.right,Vector2.one,Vector2.up};mesh.triangles=new[]{0,2,1,0,3,2};mesh.RecalculateNormals();mesh.RecalculateBounds();
  var guest=new GameObject("Laughing yellow pixel guest",typeof(MeshFilter),typeof(MeshRenderer));guest.layer=30;guest.transform.SetParent(root.transform,false);guest.transform.position=hit.point+Vector3.up*.008f;
  guest.transform.rotation=Quaternion.Euler(0,90,0);
  guest.GetComponent<MeshFilter>().sharedMesh=mesh;var face=guest.GetComponent<MeshRenderer>();face.sharedMaterial=material;face.shadowCastingMode=ShadowCastingMode.Off;face.receiveShadows=true;
  // Upright silhouette proxy provides grounded directional shadows without the
  // tilted presentation card causing a hovering shadow or a billboard smear.
  var shadow=new GameObject("Guest upright shadow proxy",typeof(MeshFilter),typeof(MeshRenderer));shadow.layer=30;shadow.transform.SetParent(root.transform,false);shadow.transform.position=guest.transform.position;shadow.transform.rotation=Quaternion.Euler(0,90,0);
  shadow.GetComponent<MeshFilter>().sharedMesh=mesh;var sr=shadow.GetComponent<MeshRenderer>();sr.sharedMaterial=material;sr.shadowCastingMode=ShadowCastingMode.ShadowsOnly;
  var control=root.GetComponent<OctopathTownStudyControls>();control.Owned.AddRange(new UnityEngine.Object[]{texture,material,mesh});
  ctx.RegisterObjectCreation(guest);ctx.RegisterObjectCreation(shadow);
  ctx.ReturnValue=new{ground=hit.point.ToString(),surface=hit.collider.name,visiblePPU=32,visibleSubjectHeight=46/32f,screenAnchor=OctopathTownStudy.Camera.WorldToScreenPoint(guest.transform.position).ToString(),lighting="Shared research PBR, shadow reception, cloud cookie, SSAO, upright cutout cast shadow"};
 }
}
