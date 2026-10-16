using System;
using System.Linq;
using UnityEngine;
using UnityEditor;
using UnityEngine.SceneManagement;
public static class DeskPreflight {
 public static string Run() {
  var scenes=Enumerable.Range(0,SceneManager.sceneCount).Select(i=>SceneManager.GetSceneAt(i)).Select(s=>new {s.name,s.path,s.isDirty,s.isLoaded}).ToArray();
  return Newtonsoft.Json.JsonConvert.SerializeObject(new {dataPath=Application.dataPath,unityVersion=Application.unityVersion,activeScene=SceneManager.GetActiveScene().path,scenes,play=Application.isPlaying,compiling=EditorApplication.isCompiling,updating=EditorApplication.isUpdating});
 }
}
