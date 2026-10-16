using System;
using System.Reflection;
using UnityEngine;
using UnityEditor;
using Funplay.Editor.Tools.Scripting;
public class SetDeskPreview960 : IFunplayCommand {
 public void Execute(Funplay.Editor.Tools.Scripting.ExecutionContext ctx){
  if(Application.dataPath.Replace('\\','/')!="E:/数据库/OCC_Codex/UnityProject/Assets"||!Application.isPlaying)throw new InvalidOperationException("Wrong context");
  var type=typeof(EditorWindow).Assembly.GetType("UnityEditor.GameView");
  var view=EditorWindow.GetWindow(type);ctx.RegisterObjectModification(view);
  var property=type.GetProperty("targetSize",BindingFlags.Public|BindingFlags.NonPublic|BindingFlags.Instance);
  var previous=(Vector2)property.GetValue(view);
  property.SetValue(view,new Vector2(960,540));view.Focus();view.Repaint();
  ctx.ReturnValue=new{previous=new[]{previous.x,previous.y},requested="960x540",verify_next_frame=true};
 }
}
