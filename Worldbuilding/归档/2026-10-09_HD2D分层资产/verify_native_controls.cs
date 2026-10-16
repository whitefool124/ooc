using System;using System.Collections;using System.Linq;using UnityEngine;using UnityEngine.UI;using UnityEngine.InputSystem;using UnityEngine.InputSystem.LowLevel;using OCC.Combat.EditorTools;using OCC.Combat.Presentation;using OCC.Combat.Roguelite;
public static class NativeArtProof {
 static Mouse mouse,native;static bool nativeEnabled;static InputSettings.EditorInputBehaviorInPlayMode previous;
 public static string Run(){
 if(Application.dataPath!="E:/数据库/OCC_Codex/UnityProject/Assets"||!Application.isPlaying)throw new Exception("Wrong state");
 previous=InputSystem.settings.editorInputBehaviorInPlayMode;
 InputSystem.settings.editorInputBehaviorInPlayMode=InputSettings.EditorInputBehaviorInPlayMode.AllDeviceInputAlwaysGoesToGameView;
 native=Mouse.current;nativeEnabled=native!=null&&native.enabled;if(nativeEnabled)InputSystem.DisableDevice(native);mouse=InputSystem.AddDevice<Mouse>();mouse.MakeCurrent();
 UnityEditor.EditorWindow.GetWindow(Type.GetType("UnityEditor.GameView,UnityEditor")).Focus();
 UnityEngine.Object.FindAnyObjectByType<CombatPrototypeBootstrap>().StartCoroutine(Proof());return "Native mouse proof started";
 }
 static Vector2 ScreenPoint(RectTransform rect,Vector2 local){var camera=rect.GetComponentInParent<Canvas>().worldCamera;var v=camera.WorldToViewportPoint(rect.TransformPoint(local));return new Vector2(v.x*Screen.width,v.y*Screen.height);}
 static void Pointer(Vector2 p,bool down){InputSystem.QueueStateEvent(mouse,down?new MouseState{position=p}.WithButton(MouseButton.Left):new MouseState{position=p});}
 static IEnumerator Click(RectTransform rect){var p=ScreenPoint(rect,rect.rect.center);Pointer(p,false);yield return null;yield return null;Pointer(p,true);yield return null;yield return null;Pointer(p,false);for(int i=0;i<6;i++)yield return null;}
 static IEnumerator Proof(){
 try {
 var host=UnityEngine.Object.FindAnyObjectByType<CombatPrototypeBootstrap>();
 var view=UnityEngine.Object.FindAnyObjectByType<RogueLoadoutEquipmentView>();
 var from=view.EquipmentSlots.First(x=>x.Root.name.Contains("Chest")).Root;
 var start=ScreenPoint(from,from.rect.center);var end=ScreenPoint(view.BackpackGrid,new Vector2(30,-30));
 Pointer(start,false);yield return null;yield return null;Pointer(start,true);yield return null;yield return null;
 for(int i=1;i<=4;i++){Pointer(Vector2.Lerp(start,end,i/4f),true);yield return null;yield return null;}
 Pointer(end,false);for(int i=0;i<10;i++)yield return null;
 var runtime=RogueEquipmentRuntime.FromDto(host.CurrentMapRun.RogueRunState);
 bool dragged=!runtime.Equipped.TryGetValue(EquipmentSlot.Chest,out string id)||string.IsNullOrEmpty(id);
 Debug.Log("HD2D_ART_PROOF: native_drag="+dragged+" backpack="+RogueInventoryPresentation.Build(runtime).Count);
 if(!dragged)throw new Exception("Native drag failed");
 var book=GameObject.Find("实体档案册入口").GetComponent<RectTransform>();yield return Click(book);
 var ui=UnityEngine.Object.FindAnyObjectByType<FormalRogueliteUi>();Debug.Log("HD2D_ART_PROOF: book_archive="+ui.IsArchiveOpen);
 if(!ui.IsArchiveOpen)throw new Exception("Book native click failed");
 host.PreviewUiCapturePage("loadout");for(int i=0;i<15;i++)yield return null;
 var footer=UnityEngine.Object.FindObjectsByType<Button>(FindObjectsSortMode.None).First(x=>x.name=="按钮_返回地图");
 yield return Click((RectTransform)footer.transform);
 yield return new WaitForSecondsRealtime(.8f);bool returned=GameObject.Find("实体档案册入口")==null;
 Debug.Log("HD2D_ART_PROOF: plaque_return="+returned);
 if(!returned)throw new Exception("Plaque native click failed");
 } finally {InputSystem.RemoveDevice(mouse);if(native!=null&&native.added){if(nativeEnabled)InputSystem.EnableDevice(native);native.MakeCurrent();}InputSystem.settings.editorInputBehaviorInPlayMode=previous;}
 }
}
