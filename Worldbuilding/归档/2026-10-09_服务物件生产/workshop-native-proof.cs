using System;using System.Linq;using System.Collections;using UnityEngine;using UnityEngine.UI;using UnityEngine.InputSystem;using UnityEngine.InputSystem.LowLevel;using OCC.Combat.Presentation;
public class ServiceNativeProof {
static Mouse m,n;static bool enabled;static InputSettings.EditorInputBehaviorInPlayMode prev;
public static string Run(){if(!Application.isPlaying||Application.dataPath!="E:/数据库/OCC_Codex/UnityProject/Assets")return "guard";prev=InputSystem.settings.editorInputBehaviorInPlayMode;InputSystem.settings.editorInputBehaviorInPlayMode=InputSettings.EditorInputBehaviorInPlayMode.AllDeviceInputAlwaysGoesToGameView;n=Mouse.current;enabled=n!=null&&n.enabled;if(enabled)InputSystem.DisableDevice(n);m=InputSystem.AddDevice<Mouse>();m.MakeCurrent();UnityEditor.EditorWindow.GetWindow(Type.GetType("UnityEditor.GameView,UnityEditor")).Focus();UnityEngine.Object.FindAnyObjectByType<CombatPrototypeBootstrap>().StartCoroutine(Proof());return "started";}
static Vector2 Point(RectTransform r,Vector2 local){var c=r.GetComponentInParent<Canvas>();if(c.renderMode!=RenderMode.WorldSpace)return RectTransformUtility.WorldToScreenPoint(null,r.TransformPoint(local));var v=c.worldCamera.WorldToViewportPoint(r.TransformPoint(local));return new Vector2(v.x*Screen.width,v.y*Screen.height);}
static void Pointer(Vector2 p,bool down=false,float scroll=0){var state=new MouseState{position=p,scroll=new Vector2(0,scroll)};if(down)state=state.WithButton(MouseButton.Left);InputSystem.QueueStateEvent(m,state);}
static IEnumerator Click(Button b){var r=(RectTransform)b.transform;var p=Point(r,r.rect.center);Pointer(p);yield return null;yield return null;Pointer(p,true);yield return null;yield return null;Pointer(p);yield return new WaitForSecondsRealtime(.8f);}
static IEnumerator Proof(){try{
yield return Click(UnityEngine.Object.FindObjectsByType<Button>(FindObjectsSortMode.None).First(x=>x.name=="页签_术式专精"));
var ui=UnityEngine.Object.FindAnyObjectByType<FormalRogueliteUi>();bool switched=!(bool)typeof(FormalRogueliteUi).GetField("workshopForgeTab",System.Reflection.BindingFlags.NonPublic|System.Reflection.BindingFlags.Instance).GetValue(ui);Debug.Log("SERVICE_DEPTH_PROOF: specialization_tab="+switched);if(!switched)throw new Exception("Tab failed");
yield return Click(UnityEngine.Object.FindObjectsByType<Button>(FindObjectsSortMode.None).First(x=>x.name=="页签_装备锻造"));
var card=UnityEngine.Object.FindObjectsByType<Button>(FindObjectsSortMode.None).First(x=>x.GetComponentsInChildren<Text>().Any(t=>t.text.Contains("夹棉练习衣")));
var r=(RectTransform)card.transform;Pointer(Point(r,r.rect.center));yield return new WaitForSecondsRealtime(.35f);
bool tip=UnityEngine.Object.FindObjectsByType<FormalHoverTooltip>(FindObjectsSortMode.None).Any(x=>x.IsVisible);Debug.Log("SERVICE_DEPTH_PROOF: equipment_hover="+tip);if(!tip)throw new Exception("Tooltip failed");
yield return Click(card);
Debug.Log("SERVICE_DEPTH_PROOF: equipment_card_click=True");
yield return Click(UnityEngine.Object.FindObjectsByType<Button>(FindObjectsSortMode.None).First(x=>x.name=="按钮_返回地图"));
bool map=UnityEngine.Object.FindAnyObjectByType<RogueMapViewportController>()!=null;Debug.Log("SERVICE_DEPTH_PROOF: return_map="+map);if(!map)throw new Exception("Return failed");
}finally{InputSystem.RemoveDevice(m);if(n!=null&&n.added){if(enabled)InputSystem.EnableDevice(n);n.MakeCurrent();}InputSystem.settings.editorInputBehaviorInPlayMode=prev;}}
}
