// See https://aka.ms/new-console-template for more information
using CUE4Parse.FileProvider;
using CUE4Parse.UE4.Versions;
using Newtonsoft.Json;
using Serilog;
using Newtonsoft.Json.Linq;
using CUE4Parse.UE4.Assets.Exports.StaticMesh;
using CUE4Parse.UE4.Assets.Exports.Material;
using CUE4Parse_Conversion.Meshes;
using CUE4Parse_Conversion.Materials;
using CUE4Parse_Conversion;
using CUE4Parse_Conversion.Textures;
using CUE4Parse.UE4.Assets.Exports.Texture;
using SkiaSharp;
var zlibPath=Path.Combine(AppContext.BaseDirectory,"zlib-ng2.dll");
if(!File.Exists(zlibPath))CUE4Parse.Compression.ZlibHelper.DownloadDll(zlibPath);
CUE4Parse.Compression.ZlibHelper.Initialize(zlibPath);
Log.Logger = new LoggerConfiguration().WriteTo.Console().CreateLogger();
var provider = new DefaultFileProvider("E:/SteamLibrary/steamapps/common/OCTOPATH TRAVELER/Octopath_Traveler/Content/Paks", SearchOption.TopDirectoryOnly, true, new VersionContainer(EGame.GAME_UE4_18));
provider.Initialize();provider.Mount();
Console.WriteLine($"Mounted {provider.Files.Count} files");
if(args.Length==0) return;
if(args[0]=="functions") {
 var output=Path.GetFullPath(args[2]);Directory.CreateDirectory(output);
 var pending=new Queue<string>(File.ReadAllLines(args[1]));var visited=new HashSet<string>();var failures=new List<string>();
 while(pending.Count>0&&visited.Count<250) {
  var path=pending.Dequeue();if(!visited.Add(path))continue;
  try {
   var functionObjects=provider.LoadPackage(path).GetExports();
   var json=JArray.Parse(JsonConvert.SerializeObject(functionObjects));
   var target=Path.Combine(output,path+".json");Directory.CreateDirectory(Path.GetDirectoryName(target)!);File.WriteAllText(target,json.ToString());
   foreach(var texture in functionObjects.OfType<UTexture2D>()) {
    using var bitmap=texture.Decode(texture.GetFirstMip());using var png=bitmap!.Encode(SKEncodedImageFormat.Png,100);
    File.WriteAllBytes(Path.Combine(output,path+".png"),png.ToArray());
   }
   foreach(var reference in json.Descendants().OfType<JObject>()) {
    string? name=(string?)reference["ObjectName"],refPath=(string?)reference["ObjectPath"];
    if(refPath!=null&&name!=null&&(name.StartsWith("MaterialFunction'")||name.StartsWith("MaterialParameterCollection'")||name.StartsWith("Material'")||name.StartsWith("MaterialInstanceConstant'")||name.StartsWith("Texture2D'")))
     pending.Enqueue(refPath[..refPath.LastIndexOf('.')]);
   }
  }catch(Exception e){failures.Add(path+": "+e.Message);}
 }
 File.WriteAllText(Path.Combine(output,"inventory.json"),JsonConvert.SerializeObject(new{visited,failures},Formatting.Indented));
 Console.WriteLine($"Function research: {visited.Count} packages, {failures.Count} unavailable");return;
}
if(args[0]=="cube") {
 var texture=provider.LoadPackage(args[1]).GetExports().OfType<UTexture>().First();
 Directory.CreateDirectory(args[2]);
 File.WriteAllText(Path.Combine(args[2],"metadata.json"),JsonConvert.SerializeObject(texture,Formatting.Indented));
 {
  // UE4.18 stores these six cube faces in one 128 x 768 DXT1 strip.
  using var bitmap=texture.Decode(texture.GetFirstMip());
  using var data=bitmap!.Encode(SKEncodedImageFormat.Png,100);
  File.WriteAllBytes(Path.Combine(args[2],"cube_strip.png"),data.ToArray());
 }
 return;
}
if(args[0]=="stage") {
 var scene=JArray.Parse(File.ReadAllText(args[1]));var output=new DirectoryInfo(Path.GetFullPath(args[2]));output.Create();
 var refs=scene.Where(o=>(string?)o["Type"]=="StaticMeshComponent"&&o["Properties"]?["StaticMesh"]!=null).Select(o=>(string)o["Properties"]!["StaticMesh"]!["ObjectPath"]!).Distinct().ToArray();
 var meshes=new Dictionary<string,object>();var materials=new HashSet<string>();var failures=new List<string>();
 foreach(var path in refs)try {
  var mesh=provider.LoadPackage(path[..path.LastIndexOf('.')]).GetExports().OfType<UStaticMesh>().First();
  if(!mesh.TryConvert(out var converted)||converted.LODs.Count==0)throw new Exception("No mesh LOD");
  var lod=converted.LODs[0];var sections=lod.Sections.Value;
  foreach(var s in sections)if(s.Material!=null) materials.Add(s.Material.GetPathName());
  var ib=lod.Indices.Value;var indices=ib.Indices32.Length>0?ib.Indices32:ib.Indices16.Select(i=>(uint)i).ToArray();
  meshes[path]=new{vertices=lod.Verts.Select(v=>new[]{v.Position.X,v.Position.Y,v.Position.Z}),normals=lod.Verts.Select(v=>new[]{v.Normal.X,v.Normal.Y,v.Normal.Z}),uv=lod.Verts.Select(v=>new[]{v.UV.U,v.UV.V}),extraUV=lod.ExtraUV.Value.Select(channel=>channel.Select(v=>new[]{v.U,v.V})),colors=lod.VertexColors?.Select(c=>new[]{c.R/255f,c.G/255f,c.B/255f,c.A/255f}),indices,sections=sections.Select(s=>new{s.FirstIndex,s.NumFaces,material=s.Material?.GetPathName()})};
  Console.WriteLine("Mesh "+path);
 }catch(Exception e){failures.Add(path+": "+e.Message);Console.WriteLine("FAIL "+path+" "+e.Message);}
 foreach(var r in scene.SelectMany(o=>o["Properties"]?["OverrideMaterials"] as JArray??new JArray()))if(r?["ObjectPath"]!=null)materials.Add((string)r["ObjectPath"]!);
 File.WriteAllText(Path.Combine(output.FullName,"meshes.json"),JsonConvert.SerializeObject(meshes));
 if(args.Length>3&&args[3]=="mesh-only")return;
 foreach(var path in materials)try{
  var package=path[..path.LastIndexOf('.')];var mat=provider.LoadPackage(package).GetExports().OfType<UUnrealMaterial>().First();
  new MaterialExporter2(mat,new ExporterOptions()).TryWriteToDir(output,out _,out _);
  var rawPath=Path.Combine(output.FullName,package+".raw.json");Directory.CreateDirectory(Path.GetDirectoryName(rawPath)!);
  File.WriteAllText(rawPath,JsonConvert.SerializeObject(mat,Formatting.Indented));Console.WriteLine("Material "+path);
 }catch(Exception e){failures.Add(path+": "+e.Message);Console.WriteLine("FAIL material "+path+" "+e.Message);}
 File.WriteAllText(Path.Combine(output.FullName,"failures.json"),JsonConvert.SerializeObject(failures,Formatting.Indented));
 Console.WriteLine($"Finished: {meshes.Count} meshes, {materials.Count} material references, {failures.Count} failures");return;
}
if(args[0]=="list") { foreach(var f in provider.Files.Keys.Where(p=>p.Contains(args[1],StringComparison.OrdinalIgnoreCase)))Console.WriteLine(f);return; }
var objects=provider.LoadPackage(args[0]).GetExports();
var outPath=Path.GetFullPath(args[1]);Directory.CreateDirectory(Path.GetDirectoryName(outPath)!);
File.WriteAllText(outPath,JsonConvert.SerializeObject(objects,Formatting.Indented));
Console.WriteLine($"Exported {objects.Count()} objects to {outPath}");
