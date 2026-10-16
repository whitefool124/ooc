using System.Linq;
using UnityEngine;
using UnityEngine.UI;
using OCC.Combat.Presentation;
public static class InspectNativeTextures {public static string Run(){
 var textures=new[]{"Art/MainMenuE/Stage/vista","Art/MainMenuE/Stage/desk","Art/MainMenuE/Stage/room","Art/Hd2DContentFrames/简洁纸边"}.Select(p=>new{path=p,items=Resources.LoadAll(p).Select(o=>new{name=o.name,type=o.GetType().Name,details=o is Texture2D t?t.width+"x"+t.height:o is Sprite s?s.rect+" border="+s.border+" ppu="+s.pixelsPerUnit:""}).ToArray()}).ToArray();
 var images=UnityEngine.Object.FindObjectsByType<Image>(FindObjectsSortMode.None).Where(i=>i.sprite!=null&&i.sprite.texture.name=="简洁纸边").Select(i=>new{i.name,size=i.rectTransform.rect.ToString(),scale=i.transform.lossyScale.ToString("F5"),ppu=i.pixelsPerUnitMultiplier,sprite=i.sprite.rect.ToString(),border=i.sprite.border.ToString(),reference=i.canvas.referencePixelsPerUnit}).ToArray();
 return Newtonsoft.Json.JsonConvert.SerializeObject(new{textures,images});
}}
