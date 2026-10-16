using UnityEngine;
using UnityEditor;
using System.Linq;
using Funplay.Editor.Tools.Scripting;
public class MaterialShaderAudit : IFunplayCommand {
 public void Execute(Funplay.Editor.Tools.Scripting.ExecutionContext ctx){
  if(Application.dataPath.Replace('\\','/')!="E:/数据库/OCC_Codex/UnityProject/Assets")throw new System.Exception("Wrong project");
  var shader=AssetDatabase.LoadAssetAtPath<Shader>("Assets/Game/Resources/Art/MainMenuE/Hd2D/Hd2DSurface.shader");
  if(shader==null)throw new System.Exception("Surface shader missing");
  ctx.ReturnValue=new {shader=shader.name,shader.isSupported,errors=ShaderUtil.GetShaderMessages(shader).Select(m=>new {m.message,m.line,severity=m.severity.ToString()})};
 }
}
