using System;using System.Linq;using System.Collections;using UnityEngine;using UnityEngine.UI;using UnityEngine.InputSystem;using UnityEngine.InputSystem.LowLevel;using OCC.Combat.Presentation;
public class WorldShopNativeProof {
static Mouse m,n;static bool enabled;static InputSettings.EditorInputBehaviorInPlayMode prev;
public static string Run(){if(!Application.isPlaying||Application.dataPath!="E:/数据库/OCC_Codex/UnityProject/Assets")return "guard";prev=InputSystem.settings.editorInputBehaviorInPlayMode;InputSystem.settings.editorInputBehaviorInPlayMode=InputSettings.EditorInputBehaviorInPlayMode.AllDeviceInputAlwaysGoesToGameView;n=Mouse.current;enabled=n!=null&&n.enabled;if(enabled)InputSystem.DisableDevice(n);m=InputSystem.AddDevice<Mouse>();m.MakeCurrent();UnityEditor.EditorWindow.GetWindow(Type.GetType("UnityEditor.GameView,UnityEditor")).Focus();UnityEngine.Object.FindAnyObjectByType<CombatPrototypeBootstrap>().StartCoroutine(Proof());return "started";}
static Vector2 Point(RectTransform r,Vector2 local){var c=r.GetComponentInParent<Canvas>();if(c.renderMode!=RenderMode.WorldSpace)return RectTransformUtility.WorldToScreenPoint(null,r.TransformPoint(local));var v=c.worldCamera.WorldToViewportPoint(r.TransformPoint(local));return new Vector2(v.x*Screen.width,v.y*Screen.height);}
static void Pointer(Vector2 p,bool down=false,float scroll=0){var state=new MouseState{position=p,scroll=new Vector2(0,scroll)};if(down)state=state.WithButton(MouseButton.Left);InputSystem.QueueStateEvent(m,state);}
static IEnumerator Click(Button b){var r=(RectTransform)b.transform;var p=Point(r,r.rect.center);Pointer(p);yield return null;yield return null;Pointer(p,true);yield return null;yield return null;Pointer(p);yield return new WaitForSecondsRealtime(.8f);}
static IEnumerator Proof(){try{
var host=UnityEngine.Object.FindAnyObjectByType<CombatPrototypeBootstrap>();var before=host.CurrentMapRun.Gold;
yield return Click(UnityEngine.Object.FindObjectsByType<Button>(FindObjectsSortMode.None).First(x=>x.name=="按钮_购买"&&x.IsInteractable()));
bool purchased=host.CurrentMapRun.Gold<before;Debug.Log("WORLD_UI_PROOF: native_shop_purchase="+purchased+" gold="+host.CurrentMapRun.Gold);if(!purchased)throw new Exception("Shop purchase failed");
}finally{InputSystem.RemoveDevice(m);if(n!=null&&n.added){if(enabled)InputSystem.EnableDevice(n);n.MakeCurrent();}InputSystem.settings.editorInputBehaviorInPlayMode=prev;}}
}
