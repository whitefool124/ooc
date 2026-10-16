using System;using System.Collections;using System.Linq;using UnityEngine;using UnityEngine.UI;using UnityEngine.InputSystem;using UnityEngine.InputSystem.LowLevel;using OCC.Combat.EditorTools;using OCC.Combat.Presentation;using OCC.Combat.Roguelite;
public static class CrystalNativeProof {
 static bool hadBrightness;static float savedBrightness;static Mouse mouse,native;static bool nativeEnabled;static InputSettings.EditorInputBehaviorInPlayMode previous;
 public static string Run(){
 if(Application.dataPath!="E:/数据库/OCC_Codex/UnityProject/Assets"||!Application.isPlaying)throw new Exception("Wrong state");
 hadBrightness=PlayerPrefs.HasKey("OCC.Visual.DeskLampBrightness.v1");savedBrightness=PlayerPrefs.GetFloat("OCC.Visual.DeskLampBrightness.v1",.4f);previous=InputSystem.settings.editorInputBehaviorInPlayMode;
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
 
 var slider=UnityEngine.Object.FindObjectsByType<Slider>(FindObjectsSortMode.None).First(x=>x.name=="晶灯亮度滑条");
 var sliderRect=(RectTransform)slider.transform;
 var dim=ScreenPoint(sliderRect,new Vector2(-135,0));var bright=ScreenPoint(sliderRect,new Vector2(135,0));
 Pointer(dim,false);yield return null;yield return null;Pointer(dim,true);yield return null;yield return null;
 Pointer(bright,true);yield return null;yield return null;Pointer(bright,false);yield return new WaitForSecondsRealtime(.25f);
 var lamp=GameObject.Find("整备台灯暖光").GetComponent<Light>();
 bool brightOk=slider.value>.95f && lamp.intensity>5.7f && PlayerPrefs.GetFloat("OCC.Visual.DeskLampBrightness.v1",0)>.95f;
 Debug.Log("CRYSTAL_PROOF: drag_brightness_and_save="+brightOk);if(!brightOk)throw new Exception("Lamp brightness drag failed");
 Pointer(dim,false);yield return null;Pointer(dim,true);yield return null;yield return null;Pointer(dim,false);yield return new WaitForSecondsRealtime(.25f);
 bool off=slider.value<.01f&&!lamp.enabled;Debug.Log("CRYSTAL_PROOF: zero_turns_off="+off);if(!off)throw new Exception("Lamp off failed");

 var spellTab=UnityEngine.Object.FindObjectsByType<Button>(FindObjectsSortMode.None).First(x=>x.name=="按钮_术式编组");yield return Click((RectTransform)spellTab.transform);yield return new WaitForSecondsRealtime(.6f);
 var spellUi=UnityEngine.Object.FindAnyObjectByType<FormalRogueliteUi>();
 var spellSlot=UnityEngine.Object.FindObjectsByType<Button>(FindObjectsSortMode.None).First(x=>x.GetComponentsInChildren<Text>().Any(t=>t.text.StartsWith("2  ")));yield return Click((RectTransform)spellSlot.transform);yield return new WaitForSecondsRealtime(.6f);
 int index=(int)typeof(FormalRogueliteUi).GetField("selectedLoadoutSpellIndex",System.Reflection.BindingFlags.Instance|System.Reflection.BindingFlags.NonPublic).GetValue(spellUi);
 Debug.Log("HD2D_ART_PROOF: native_spell_selection="+(index==1));if(index!=1)throw new Exception("Native spell selection failed");
 bool remembered=!GameObject.Find("整备台灯暖光").GetComponent<Light>().enabled;Debug.Log("CRYSTAL_PROOF: page_remembers="+remembered);if(!remembered)throw new Exception("Brightness not remembered");
 var equipmentTab=UnityEngine.Object.FindObjectsByType<Button>(FindObjectsSortMode.None).First(x=>x.name=="按钮_装备、背包与战术栏");yield return Click((RectTransform)equipmentTab.transform);yield return new WaitForSecondsRealtime(.6f);
 var book=GameObject.Find("实体档案册入口").GetComponent<RectTransform>();yield return Click(book);
 var ui=UnityEngine.Object.FindAnyObjectByType<FormalRogueliteUi>();Debug.Log("HD2D_ART_PROOF: book_archive="+ui.IsArchiveOpen);
 if(!ui.IsArchiveOpen)throw new Exception("Book native click failed");
 host.PreviewUiCapturePage("loadout");for(int i=0;i<15;i++)yield return null;
 var footer=UnityEngine.Object.FindObjectsByType<Button>(FindObjectsSortMode.None).First(x=>x.name=="按钮_返回地图");
 yield return Click((RectTransform)footer.transform);
 yield return new WaitForSecondsRealtime(.8f);bool returned=GameObject.Find("实体档案册入口")==null;
 Debug.Log("HD2D_ART_PROOF: plaque_return="+returned);
 if(!returned)throw new Exception("Plaque native click failed");
 } finally {if(hadBrightness)PlayerPrefs.SetFloat("OCC.Visual.DeskLampBrightness.v1",savedBrightness);else PlayerPrefs.DeleteKey("OCC.Visual.DeskLampBrightness.v1");PlayerPrefs.Save();InputSystem.RemoveDevice(mouse);if(native!=null&&native.added){if(nativeEnabled)InputSystem.EnableDevice(native);native.MakeCurrent();}InputSystem.settings.editorInputBehaviorInPlayMode=previous;}
 }
}
