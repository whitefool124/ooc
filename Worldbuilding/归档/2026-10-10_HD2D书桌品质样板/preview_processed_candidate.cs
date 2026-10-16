using System;
using System.IO;
using System.Security.Cryptography;
using UnityEngine;
using OCC.Combat.Presentation;
using Funplay.Editor.Tools.Scripting;
public class PreviewProcessedDesk : IFunplayCommand {
 public void Execute(Funplay.Editor.Tools.Scripting.ExecutionContext ctx) {
  if(!Application.isPlaying||Application.dataPath.Replace('\\','/')!="E:/数据库/OCC_Codex/UnityProject/Assets")throw new InvalidOperationException("Main-project Play required");
  string root="E:/数据库/OCC_Codex/ArtSource/desk_candidate_coarse_20261010";
  var textures=new Texture2D[3];int i=0;
  foreach(string key in new[]{"wood","plaster","cup"}) {
   var path=Path.Combine(root,key,"delivery",key+".png");
   var manifest=Newtonsoft.Json.Linq.JObject.Parse(File.ReadAllText(Path.Combine(root,key,"manifest.occ-art-manifest-v1.json")));
   byte[] bytes=File.ReadAllBytes(path);string hash;
   using(var sha=SHA256.Create())hash=BitConverter.ToString(sha.ComputeHash(bytes)).Replace("-","").ToLowerInvariant();
   if((string)manifest["delivery"]["output_sha256"]!=hash||(string)manifest["status"]!="QA_PENDING")throw new InvalidOperationException("Candidate binding failed");
   var tex=new Texture2D(2,2,TextureFormat.RGBA32,false){name="coarse_candidate_"+key,filterMode=FilterMode.Point,wrapMode=TextureWrapMode.Clamp};
   if(!ImageConversion.LoadImage(tex,bytes))throw new InvalidOperationException("Image decode failed");
   ctx.RegisterObjectCreation(tex);textures[i++]=tex;
  }
  foreach(var stage in UnityEngine.Object.FindObjectsByType<MainMenuHd2DStage>(FindObjectsSortMode.None)) {
   foreach(var t in stage.GetComponentsInChildren<Transform>(true))ctx.RegisterObjectModification(t.gameObject);
   foreach(var r in stage.GetComponentsInChildren<MeshRenderer>(true))ctx.RegisterObjectModification(r);
  }
  Hd2DDeskArtCandidate.BeginPreview(textures[0],textures[1],textures[2]);
  ctx.ReturnValue="Coarse QA_PENDING memory preview started; no filesystem writes or formal asset changes.";
 }
}
