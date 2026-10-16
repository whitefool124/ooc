using System;
using System.Linq;
using System.Reflection;
using UnityEditor;
public static class InspectGameViewSize {
 public static string Run(){
  var assembly=typeof(EditorWindow).Assembly;
  var names=new[]{"UnityEditor.GameView","UnityEditor.GameViewSizes","UnityEditor.GameViewSizeGroup","UnityEditor.GameViewSize"};
  return Newtonsoft.Json.JsonConvert.SerializeObject(names.Select(n=>{var t=assembly.GetType(n);return new{name=n,properties=t?.GetProperties(BindingFlags.Public|BindingFlags.NonPublic|BindingFlags.Static|BindingFlags.Instance).Where(x=>x.Name.Contains("ize")||x.Name=="instance").Select(x=>new{x.Name,set=x.CanWrite}),methods=t?.GetMethods(BindingFlags.Public|BindingFlags.NonPublic|BindingFlags.Static|BindingFlags.Instance).Where(x=>x.Name.Contains("Size")||x.Name.Contains("Group")).Select(x=>x.ToString()),constructors=t?.GetConstructors(BindingFlags.Public|BindingFlags.NonPublic|BindingFlags.Instance).Select(x=>x.ToString())};}));
 }
}
