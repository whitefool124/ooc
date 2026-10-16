using UnityEngine;
using OCC.Combat.Presentation;
using Funplay.Editor.Tools.Scripting;
public sealed class WoodSpecularRestore : IFunplayCommand {
 public void Execute(ExecutionContext ctx){
  if(!Application.isPlaying || Application.dataPath.Replace('\\','/')!="E:/数据库/OCC_Codex/UnityProject/Assets")throw new System.InvalidOperationException("Main project Play required");
  var stage=Object.FindAnyObjectByType<MainMenuHd2DStage>();if(stage==null)throw new System.InvalidOperationException("Missing stage");
  int count=0;foreach(var renderer in stage.GetComponentsInChildren<MeshRenderer>()){
   var material=renderer.sharedMaterial;if(material==null||!material.name.StartsWith("QA_PENDING wood "))continue;
   ctx.RegisterObjectModification(material);material.SetFloat("_SpecularHighlights",1);material.DisableKeyword("_SPECULARHIGHLIGHTS_OFF");count++;
  }ctx.ReturnValue="Wood specular restored: "+count;
 }
}
