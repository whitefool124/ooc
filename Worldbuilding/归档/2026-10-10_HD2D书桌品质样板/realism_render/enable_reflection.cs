using UnityEngine;
using UnityEditor;
using Funplay.Editor.Tools.Scripting;
public class EnableDeskReflectionQuality : IFunplayCommand {
 public void Execute(Funplay.Editor.Tools.Scripting.ExecutionContext ctx){
  if(Application.isPlaying||Application.dataPath.Replace('\\','/')!="E:/数据库/OCC_Codex/UnityProject/Assets")throw new System.Exception("Main project Edit required");
  var assets=AssetDatabase.LoadAllAssetsAtPath("ProjectSettings/QualitySettings.asset");
  if(assets.Length!=1)throw new System.Exception("Quality asset missing");
  var asset=assets[0];var so=new SerializedObject(asset);int index=QualitySettings.GetQualityLevel();
  var levels=so.FindProperty("m_QualitySettings");
  var level=levels.GetArrayElementAtIndex(index);
  if(level.FindPropertyRelative("name").stringValue!="Very Low")throw new System.Exception("Quality changed");
  ctx.RegisterObjectModification(asset);
  level.FindPropertyRelative("realtimeReflectionProbes").boolValue=true;so.ApplyModifiedProperties();
  AssetDatabase.SaveAssetIfDirty(asset);
  ctx.ReturnValue=new {level=index,realtime=QualitySettings.realtimeReflectionProbes};
 }
}
