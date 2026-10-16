using UnityEngine;
using UnityEditor;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;
using UnityEngine.SceneManagement;
using Funplay.Editor.Tools.Scripting;
public class RepairRealismRendering : IFunplayCommand {
 public void Execute(Funplay.Editor.Tools.Scripting.ExecutionContext ctx){
  if(Application.isPlaying || Application.dataPath.Replace('\\','/')!="E:/数据库/OCC_Codex/UnityProject/Assets")throw new System.Exception("Main project Edit required");
  for(int i=0;i<SceneManager.sceneCount;i++)if(SceneManager.GetSceneAt(i).isDirty)throw new System.Exception("Dirty scene");
  var pipeline=AssetDatabase.LoadAssetAtPath<UniversalRenderPipelineAsset>("Assets/Settings/UniversalRP.asset");
  var renderer=AssetDatabase.LoadAssetAtPath<UniversalRendererData>("Assets/Game/Resources/Art/MainMenuE/Hd2D/HD2DRenderer.asset");
  var post=AssetDatabase.LoadAssetAtPath<PostProcessData>("Packages/com.unity.render-pipelines.universal/Runtime/Data/PostProcessData.asset");
  if(pipeline==null||renderer==null||post==null||GraphicsSettings.currentRenderPipeline!=pipeline)throw new System.Exception("Asset/pipeline mismatch");
  ctx.RegisterObjectModification(pipeline);ctx.RegisterObjectModification(renderer);
  var so=new SerializedObject(pipeline);
  so.FindProperty("m_SoftShadowsSupported").boolValue=true;
  so.FindProperty("m_MainLightShadowmapResolution").intValue=4096;
  so.FindProperty("m_ShadowCascadeCount").intValue=2;
  so.FindProperty("m_RenderScale").floatValue=1f;
  so.ApplyModifiedProperties();
  renderer.postProcessData=post;renderer.SetDirty();EditorUtility.SetDirty(renderer);
  AssetDatabase.SaveAssetIfDirty(pipeline);AssetDatabase.SaveAssetIfDirty(renderer);
  ctx.ReturnValue=new {soft=pipeline.supportsSoftShadows,renderScale=pipeline.renderScale,shadowResolution=pipeline.mainLightShadowmapResolution,cascades=pipeline.shadowCascadeCount,postProcessData=AssetDatabase.GetAssetPath(renderer.postProcessData),sceneSaved=false};
 }
}
