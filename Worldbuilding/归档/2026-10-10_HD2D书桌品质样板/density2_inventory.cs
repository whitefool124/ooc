using System;
using System.Linq;
using UnityEngine;
using UnityEngine.UI;
using UnityEditor;
public static class ServiceDensityInventory {
 public static string Run(){
  if(Application.dataPath.Replace('\\','/')!="E:/数据库/OCC_Codex/UnityProject/Assets")throw new Exception("Wrong project");
  return Newtonsoft.Json.JsonConvert.SerializeObject(UnityEngine.Object.FindObjectsByType<Image>(FindObjectsSortMode.None)
   .Where(x=>x.sprite!=null && x.GetComponentInParent<Canvas>()!=null && x.GetComponentInParent<Canvas>().renderMode==RenderMode.WorldSpace)
   .Select(x=>new {name=x.name,path=AssetDatabase.GetAssetPath(x.sprite),source=new[]{x.sprite.rect.width,x.sprite.rect.height},ppu=x.sprite.pixelsPerUnit,
   size=new[]{x.rectTransform.rect.width,x.rectTransform.rect.height},world=new[]{x.transform.lossyScale.x,x.transform.lossyScale.y},preserve=x.preserveAspect,active=x.gameObject.activeInHierarchy,alpha=x.color.a,
   simple_texel_world_pitch=new[]{x.rectTransform.rect.width*Mathf.Abs(x.transform.lossyScale.x)/x.sprite.rect.width,x.rectTransform.rect.height*Mathf.Abs(x.transform.lossyScale.y)/x.sprite.rect.height},
   note=x.type==Image.Type.Simple?"sprite rectangle pitch; source lattice and projection still require review":"tiled/sliced mesh UV inspection required"}));
 }
}
