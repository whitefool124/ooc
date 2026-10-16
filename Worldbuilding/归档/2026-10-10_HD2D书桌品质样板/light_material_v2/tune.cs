using UnityEngine;
using System.Linq;
using OCC.Combat.Presentation;
using Funplay.Editor.Tools.Scripting;
public class TuneDeskResponse : IFunplayCommand {
 public void Execute(Funplay.Editor.Tools.Scripting.ExecutionContext ctx){
  if(!Application.isPlaying||Application.dataPath.Replace('\\','/')!="E:/数据库/OCC_Codex/UnityProject/Assets")throw new System.Exception("Main project Play required");
  var stage=Object.FindObjectsByType<MainMenuHd2DStage>(FindObjectsSortMode.None).Single(s=>s.transform.Find("场景空间整备陈列")!=null);
  foreach(var r in stage.GetComponentsInChildren<Renderer>())if(r.sharedMaterial!=null&&r.sharedMaterial.HasProperty("_CoatMask")&&r.sharedMaterial.GetFloat("_CoatMask")>0){ctx.RegisterObjectModification(r.sharedMaterial);r.sharedMaterial.SetFloat("_CoatMask",r.name=="桌面实体"?.28f:.16f);r.sharedMaterial.SetFloat("_CoatSmoothness",.8f);r.sharedMaterial.SetFloat("_Smoothness",.18f);}
  foreach(var light in stage.GetComponentsInChildren<Light>()){
   ctx.RegisterObjectModification(light);
   if(light.name=="室内暖补光")light.intensity=.6f;
   if(light.name=="窗边柔和补光")light.intensity=1.3f;
   if(light.name=="整备陈列柔光")light.intensity=1.1f;
  }
  var refresh=stage.GetComponentsInChildren<MonoBehaviour>().Single(c=>c!=null&&c.GetType().Name=="Hd2DDeskReflectionRefresh");
  refresh.GetType().GetMethod("RequestRefresh",System.Reflection.BindingFlags.Instance|System.Reflection.BindingFlags.NonPublic).Invoke(refresh,null);
  ctx.ReturnValue="Candidate tuning applied; no texture/camera/UV changes";
 }
}

