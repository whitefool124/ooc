// REJECTED: strict MCP filesystem guard refused File.Copy. Do not replay or
// disable checks. This archived script also targets the superseded fine art.
using System;
using System.IO;
using System.Linq;
using System.Security.Cryptography;
using UnityEngine;
using UnityEditor;
using UnityEngine.SceneManagement;
using Funplay.Editor.Tools.Scripting;
public sealed class ImportDeskCandidate : IFunplayCommand {
 public void Execute(Funplay.Editor.Tools.Scripting.ExecutionContext ctx) {
  if(Application.dataPath.Replace('\\','/')!="E:/数据库/OCC_Codex/UnityProject/Assets"||Application.isPlaying||EditorApplication.isCompiling)
   throw new InvalidOperationException("Correct idle main-project Edit Mode required");
  for(int i=0;i<SceneManager.sceneCount;i++) if(SceneManager.GetSceneAt(i).isDirty) throw new InvalidOperationException("Loaded scene is dirty; import only after inspection");
  var repo=Path.GetFullPath(Path.Combine(Application.dataPath,"../.."));
  var results=new System.Collections.Generic.List<object>();
  foreach(var key in new[]{"wood","plaster","cup"}) {
   var source=Path.Combine(repo,"ArtSource/desk_candidate_20261010",key,"delivery",key+".png");
   var manifest=Path.Combine(repo,"ArtSource/desk_candidate_20261010",key,"manifest.occ-art-manifest-v1.json");
   if(!File.Exists(source)||!File.Exists(manifest)) throw new InvalidOperationException("Missing processed delivery/manifest: "+key);
   var record=Newtonsoft.Json.Linq.JObject.Parse(File.ReadAllText(manifest));
   string hash;using(var sha=SHA256.Create())hash=BitConverter.ToString(sha.ComputeHash(File.ReadAllBytes(source))).Replace("-","").ToLowerInvariant();
   if((string)record["status"]!="QA_PENDING"||(string)record["delivery"]["output_sha256"]!=hash)throw new InvalidOperationException("Candidate hash/status mismatch");
   var relative="Assets/Game/Resources/Art/DeskCandidate20261010/"+key+".png";
   var dest=Path.Combine(Application.dataPath,relative.Substring(7));Directory.CreateDirectory(Path.GetDirectoryName(dest));
   if(File.Exists(dest)&&!File.ReadAllBytes(dest).SequenceEqual(File.ReadAllBytes(source)))throw new InvalidOperationException("Existing candidate differs; inspect before replace");
   if(!File.Exists(dest))File.Copy(source,dest);
   AssetDatabase.ImportAsset(relative,ImportAssetOptions.ForceSynchronousImport);
   var importer=AssetImporter.GetAtPath(relative) as TextureImporter;if(importer==null)throw new InvalidOperationException("Missing importer");
   ctx.RegisterObjectModification(importer);importer.textureType=TextureImporterType.Default;importer.filterMode=FilterMode.Point;
   importer.wrapMode=TextureWrapMode.Clamp;importer.mipmapEnabled=false;importer.textureCompression=TextureImporterCompression.Uncompressed;
   importer.alphaSource=TextureImporterAlphaSource.FromInput;importer.alphaIsTransparency=true;importer.npotScale=TextureImporterNPOTScale.None;importer.maxTextureSize=512;
   importer.SaveAndReimport();
   var tex=AssetDatabase.LoadAssetAtPath<Texture2D>(relative);
   results.Add(new{path=relative,guid=AssetDatabase.AssetPathToGUID(relative),width=tex.width,height=tex.height,filter=tex.filterMode.ToString(),mipmaps=tex.mipmapCount,compression=importer.textureCompression.ToString()});
  }
  ctx.ReturnValue=results;
 }
}
