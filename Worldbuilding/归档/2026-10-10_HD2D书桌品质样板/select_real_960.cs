using System;
using System.Reflection;
using UnityEngine;
using UnityEditor;
using Funplay.Editor.Tools.Scripting;
public class SelectRealDesk960 : IFunplayCommand {
 public void Execute(Funplay.Editor.Tools.Scripting.ExecutionContext ctx){
  if(Application.dataPath.Replace('\\','/')!="E:/数据库/OCC_Codex/UnityProject/Assets"||!Application.isPlaying)throw new InvalidOperationException("Wrong context");
  var a=typeof(EditorWindow).Assembly;var flags=BindingFlags.Public|BindingFlags.NonPublic|BindingFlags.Instance;
  var type=a.GetType("UnityEditor.GameView");var view=EditorWindow.GetWindow(type);
  var index=type.GetProperty("selectedSizeIndex",flags);int previous=(int)index.GetValue(view);
  var sizesType=a.GetType("UnityEditor.GameViewSizes");var sizes=sizesType.BaseType.GetProperty("instance",BindingFlags.Public|BindingFlags.Static).GetValue(null);
  var group=sizesType.GetProperty("currentGroup",flags).GetValue(sizes);var gt=group.GetType();
  var st=a.GetType("UnityEditor.GameViewSize");int count=(int)gt.GetMethod("GetTotalCount",flags).Invoke(group,null);int selected=-1;
  for(int i=0;i<count;i++){var size=gt.GetMethod("GetGameViewSize",flags).Invoke(group,new object[]{i});if((int)st.GetField("m_Width",flags).GetValue(size)==960&&(int)st.GetField("m_Height",flags).GetValue(size)==540)selected=i;}
  bool created=selected<0;
  if(created){var enumType=a.GetType("UnityEditor.GameViewSizeType");var size=Activator.CreateInstance(st,BindingFlags.Public|BindingFlags.NonPublic|BindingFlags.Instance,null,new object[]{Enum.Parse(enumType,"FixedResolution"),960,540,"OCC QA temporary 960"},null);ctx.RegisterObjectModification((UnityEngine.Object)sizes);gt.GetMethod("AddCustomSize",flags).Invoke(group,new[]{size});selected=count;}
  ctx.RegisterObjectModification(view);index.SetValue(view,selected);view.Focus();view.Repaint();
  ctx.ReturnValue=new{previous_index=previous,selected_index=selected,created_temporary=created};
 }
}
