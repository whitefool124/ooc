using UnityEngine;
using UnityEditor;
using System.Linq;
using OCC.Combat.Presentation;
using Funplay.Editor.Tools.Scripting;
public class SoftGlowReadback : IFunplayCommand {
 public void Execute(Funplay.Editor.Tools.Scripting.ExecutionContext ctx) {
 if(!Application.isPlaying||Application.dataPath.Replace('\\','/')!="E:/数据库/OCC_Codex/UnityProject/Assets")throw new System.Exception("Main project Play required");
 var stages=Object.FindObjectsByType<MainMenuHd2DStage>(FindObjectsSortMode.None);
 var shader=Shader.Find("OCC/HD2D/Surface");
 ctx.ReturnValue=new{resolution=new[]{Screen.width,Screen.height},materials=stages.SelectMany(s=>s.GetComponentsInChildren<Renderer>(true)).Where(r=>r.sharedMaterial!=null&&r.sharedMaterial.shader==shader).Select(r=>new{r.name,softEmission=r.sharedMaterial.GetFloat("_SoftEmission"),crystal=r.sharedMaterial.GetFloat("_EmissionStrength"),texture=r.sharedMaterial.mainTexture.name}),shaderMessages=ShaderUtil.GetShaderMessages(shader).Select(m=>new{m.message,m.line})};
 }
}
