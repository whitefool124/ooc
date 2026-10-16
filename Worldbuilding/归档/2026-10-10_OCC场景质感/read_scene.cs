using System;
using System.Linq;
using System.Reflection;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;
public static class ReadScene {
 public static object Run(){
  if(Application.dataPath.Replace('\\','/')!="E:/数据库/OCC_Codex/UnityProject/Assets")throw new InvalidOperationException("Wrong project");
  var f=BindingFlags.NonPublic|BindingFlags.Public|BindingFlags.Instance;
  var p=GraphicsSettings.currentRenderPipeline as UniversalRenderPipelineAsset;
  var ds=(ScriptableRendererData[])typeof(UniversalRenderPipelineAsset).GetField("m_RendererDataList",f).GetValue(p);
  var a=ds[1].rendererFeatures.OfType<ScreenSpaceAmbientOcclusion>().FirstOrDefault(x=>x.name=="Desk indirect contact occlusion");
  var m=UnityEngine.Object.FindObjectsByType<MeshRenderer>(FindObjectsSortMode.None).FirstOrDefault(x=>x.name=="桌面实体")?.sharedMaterial;
  var s=a==null?null:typeof(ScreenSpaceAmbientOcclusion).GetField("m_Settings",f).GetValue(a);
  object Field(string n)=>s?.GetType().GetField(n,f).GetValue(s);
  var result=new{path=Application.dataPath,status=OCC.Combat.EditorTools.AcademyJourneyAudit.Status,pipeline=p.name,
   ao=a!=null,aoActive=a!=null&&a.isActive,aoRadius=Field("Radius"),aoIntensity=Field("Intensity"),aoSource=Field("Source")?.ToString(),aoDirect=Field("DirectLightingStrength"),
   woodSmoothness=m?.GetFloat("_Smoothness"),woodFiber=m?.GetFloat("_TimberFiberStrength"),woodFinish=m?.GetFloat("_TimberFinishVariation"),
   output=new[]{Screen.width,Screen.height},ambient=RenderSettings.ambientMode.ToString(),
   deskRootCount=Resources.FindObjectsOfTypeAll<GameObject>().Count(x=>x.name=="场景空间整备陈列"),
   lampPrefPresent=PlayerPrefs.HasKey("OCC.Visual.DeskLampBrightness.v1"),lampPref=PlayerPrefs.GetFloat("OCC.Visual.DeskLampBrightness.v1",.4f)};
  return Newtonsoft.Json.JsonConvert.SerializeObject(result);
 }
}
