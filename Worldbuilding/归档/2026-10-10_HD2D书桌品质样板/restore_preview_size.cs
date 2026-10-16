using System;
using System.Reflection;
using UnityEngine;
using UnityEditor;
using Funplay.Editor.Tools.Scripting;
public class RestoreDeskPreviewSize : IFunplayCommand {
 public void Execute(Funplay.Editor.Tools.Scripting.ExecutionContext ctx){
  if(Application.dataPath.Replace('\\','/')!="E:/数据库/OCC_Codex/UnityProject/Assets")throw new InvalidOperationException("Wrong project");
  var a=typeof(EditorWindow).Assembly;var f=BindingFlags.Public|BindingFlags.NonPublic|BindingFlags.Instance;
  var type=a.GetType("UnityEditor.GameView");var view=EditorWindow.GetWindow(type);
  var sizesType=a.GetType("UnityEditor.GameViewSizes");var sizes=sizesType.BaseType.GetProperty("instance",BindingFlags.Public|BindingFlags.Static).GetValue(null);
  var group=sizesType.GetProperty("currentGroup",f).GetValue(sizes);var gt=group.GetType();
  var st=a.GetType("UnityEditor.GameViewSize");var size=gt.GetMethod("GetGameViewSize",f).Invoke(group,new object[]{7});
  if((string)st.GetField("m_BaseText",f).GetValue(size)!="OCC QA temporary 960")throw new InvalidOperationException("Temporary size identity changed; do not remove");
  ctx.RegisterObjectModification(view);ctx.RegisterObjectModification((UnityEngine.Object)sizes);
  type.GetProperty("selectedSizeIndex",f).SetValue(view,3);
  int custom=(int)gt.GetMethod("TotalIndexToCustomIndex",f).Invoke(group,new object[]{7});
  gt.GetMethod("RemoveCustomSize",f).Invoke(group,new object[]{7});view.Repaint();
  ctx.ReturnValue="Original Game View selection 3 restored; only verified temporary size removed.";
 }
}
