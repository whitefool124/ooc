using UnityEngine;
using System.Linq;
using UnityEditor;
using OCC.Combat.Presentation;
using Funplay.Editor.Tools.Scripting;
public class MaterialLifetimeRead : IFunplayCommand {
 public void Execute(Funplay.Editor.Tools.Scripting.ExecutionContext ctx){
  if(Application.dataPath.Replace('\\','/')!="E:/数据库/OCC_Codex/UnityProject/Assets")throw new System.Exception("Wrong project");
  var shader=Shader.Find("OCC/HD2D/Surface");
  ctx.ReturnValue=new {loadoutStages=Object.FindObjectsByType<MainMenuHd2DStage>(FindObjectsSortMode.None).Count(x=>x.transform.Find("场景空间整备陈列")!=null),deskProbes=Object.FindObjectsByType<ReflectionProbe>(FindObjectsSortMode.None).Count(x=>x.name=="整备台局部环境反射"),readingRoots=Resources.FindObjectsOfTypeAll<Canvas>().Count(x=>x.name=="整备高清阅读层"),brightnessHasKey=PlayerPrefs.HasKey("OCC.Visual.DeskLampBrightness.v1"),shaderErrors=ShaderUtil.GetShaderMessages(shader).Select(m=>new{m.message,m.line,severity=m.severity.ToString()})};
 }
}
