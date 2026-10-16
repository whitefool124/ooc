using System;
using System.Linq;
using System.Reflection;
using UnityEditor;
public static class DiscoverSizeGroups {
 public static string Run(){
  var a=typeof(EditorWindow).Assembly;var sizes=a.GetType("UnityEditor.GameViewSizes");var group=a.GetType("UnityEditor.GameViewSizeGroup");var size=a.GetType("UnityEditor.GameViewSize");
  return Newtonsoft.Json.JsonConvert.SerializeObject(new{baseProperties=sizes.BaseType.GetProperties(BindingFlags.Public|BindingFlags.NonPublic|BindingFlags.Static|BindingFlags.Instance).Select(x=>x.ToString()),groupMethods=group.GetMethods(BindingFlags.Public|BindingFlags.NonPublic|BindingFlags.Instance).Select(x=>x.ToString()),sizeFields=size.GetFields(BindingFlags.Public|BindingFlags.NonPublic|BindingFlags.Instance).Select(x=>x.ToString())});
 }
}
