using UnityEngine;
using UnityEditor;
using OCC.Combat.Presentation;
using Funplay.Editor.Tools.Scripting;
public class WoodCompiled : IFunplayCommand {
 public void Execute(Funplay.Editor.Tools.Scripting.ExecutionContext ctx){
  ctx.ReturnValue=new {dataPath=Application.dataPath,compiledMethod=typeof(Hd2DDeskArtCandidate).GetMethod("PreviewDeskWood")!=null,compiling=EditorApplication.isCompiling,updating=EditorApplication.isUpdating};
 }
}
