using UnityEngine;
using System.Linq;
using OCC.Combat.Presentation;
using Funplay.Editor.Tools.Scripting;
public class WoodLifetime : IFunplayCommand {
 public void Execute(Funplay.Editor.Tools.Scripting.ExecutionContext ctx){
  if(!Application.isPlaying||Application.dataPath.Replace('\\','/')!="E:/数据库/OCC_Codex/UnityProject/Assets")throw new System.Exception("Main project Play required");
  ctx.ReturnValue=new {candidateTextures=Resources.FindObjectsOfTypeAll<Texture2D>().Count(t=>t.name=="QA_PENDING native wood v2"),loadoutStages=Object.FindObjectsByType<MainMenuHd2DStage>(FindObjectsSortMode.None).Count(s=>s.transform.Find("场景空间整备陈列")!=null),brightnessHasKey=PlayerPrefs.HasKey("OCC.Visual.DeskLampBrightness.v1")};
 }
}
