using UnityEngine;
using System.Linq;
using System.IO;
using OCC.Combat.Presentation;
using Funplay.Editor.Tools.Scripting;
public class WoodTexturePreview : IFunplayCommand {
 public void Execute(Funplay.Editor.Tools.Scripting.ExecutionContext ctx){
  if(!Application.isPlaying || Application.dataPath.Replace('\\','/')!="E:/数据库/OCC_Codex/UnityProject/Assets")throw new System.Exception("Main project Play required");
  var stages=Object.FindObjectsByType<MainMenuHd2DStage>(FindObjectsSortMode.None).Where(s=>s.transform.Find("场景空间整备陈列")!=null).ToArray();
  if(stages.Length!=1)throw new System.Exception("Expected one loadout stage");
  var owner=stages[0].GetComponent<Hd2DDeskArtCandidate>();
  if(owner==null)throw new System.Exception("Missing native desk material owner");
  var path="E:/数据库/OCC_Codex/Worldbuilding/归档/2026-10-10_HD2D书桌品质样板/wood_texture_v2/delivery_r2.png";
  if(!File.Exists(path))throw new System.Exception("Missing decoded wood delivery");
  var tex=new Texture2D(2,2,TextureFormat.RGBA32,false);ctx.RegisterObjectCreation(tex);
  if(!tex.LoadImage(File.ReadAllBytes(path)))throw new System.Exception("PNG load failed");
  tex.name="QA_PENDING native wood v2";tex.wrapMode=TextureWrapMode.Clamp;
  if(tex.GetPixels(15,1,98,98).Any(p=>p.a!=1f))throw new System.Exception("Native material patch contains transparent padding");
  ctx.RegisterObjectModification(owner);
  foreach(var r in stages[0].GetComponentsInChildren<MeshRenderer>())if(r.name=="桌面实体"||r.name=="桌前沿"||r.name=="桌腿"){
   ctx.RegisterObjectModification(r.sharedMaterial);ctx.RegisterObjectModification(r.GetComponent<MeshFilter>().sharedMesh);
  }
  owner.PreviewDeskWood(tex,new Rect(15,1,98,98));
  ctx.ReturnValue=new {stageId=stages[0].GetEntityId().ToString(),textureSize=new[]{tex.width,tex.height},patch=new[]{15,1,98,98},nativePPU=Hd2DDeskArtCandidate.ScenePixelsPerUnit,renderers=stages[0].GetComponentsInChildren<MeshRenderer>().Where(r=>r.name=="桌面实体"||r.name=="桌前沿"||r.name=="桌腿").Select(r=>new{r.name,id=r.GetEntityId().ToString(),texture=r.sharedMaterial.mainTexture.name,scale=r.transform.localScale.ToString(),bounds=r.sharedMaterial.GetVector("_HeightUvBounds").ToString()})};
 }
}


