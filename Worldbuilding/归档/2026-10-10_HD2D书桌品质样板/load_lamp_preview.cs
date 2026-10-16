using System;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEditor.SceneManagement;
public static class LoadLampPreview { public static string Run(){
 if(!Application.isPlaying || Application.dataPath.Replace('\\','/')!="E:/数据库/OCC_Codex/UnityProject/Assets")throw new InvalidOperationException("Main project Play required");
 EditorSceneManager.LoadSceneInPlayMode("Assets/Scenes/CombatPrototype.unity",new LoadSceneParameters(LoadSceneMode.Single));
 return "CombatPrototype loaded only in Play; edit scene unchanged";
}}
