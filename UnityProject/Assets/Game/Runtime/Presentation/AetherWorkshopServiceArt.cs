using System;
using UnityEngine;
using UnityEngine.UI;

namespace OCC.Combat.Presentation
{
    // Reuse the inspected loadout materials on a small, explicit noncombat page family.
    internal static class AetherWorkshopServiceArt
    {
        internal static void Apply(GameObject root,bool gift)
        {
            var folio=Resources.Load<Sprite>("Art/AcademyWorkshop20261023/Folio");
            var paper=Resources.Load<Sprite>("Art/AcademyWorkshop20261023/Dossier");
            if(folio==null||paper==null)throw new InvalidOperationException("Workshop material family missing");
            foreach(var image in root.GetComponentsInChildren<Image>(true))
            {
                if(gift)
                {
                    if(image.name=="赠礼主纸页")Skin(image,folio);
                    else if(image.name.StartsWith("赠礼卡",StringComparison.Ordinal)||image.name=="守灯人陈列")
                    {Skin(image,paper);image.raycastTarget=false;}
                }
                if(image.name=="三选一窗口")Skin(image,folio);
                else if(image.name.StartsWith("揭晓卡牌_",StringComparison.Ordinal)||image.name=="待领取战利品"||image.name=="随身背包"||
                    image.name=="强化台"||image.name=="强化预览"||image.name=="服务_餐食选择")Skin(image,paper);
            }
            foreach(var label in root.GetComponentsInChildren<Text>(true))
            {
                if(!label.transform.parent.name.StartsWith("揭晓卡牌_",StringComparison.Ordinal))continue;
                if(label.name=="关键效果")
                    label.rectTransform.anchoredPosition=new Vector2(label.rectTransform.anchoredPosition.x,-388);
                if(label.name=="选择操作")
                {
                    label.rectTransform.anchoredPosition=new Vector2(label.rectTransform.anchoredPosition.x,-540);
                    label.rectTransform.sizeDelta=new Vector2(label.rectTransform.sizeDelta.x,32);
                }
            }
            if(gift)foreach(var label in root.GetComponentsInChildren<Text>(true))
            {
                if(label.name=="标题"||label.name=="对白"||label.GetComponentInParent<AetherQuietNavigation>()!=null)
                    label.color=new Color(.96f,.92f,.80f);
            }
        }
        private static void Skin(Image image,Sprite sprite)
        {
            Hd2DContainerStyle.Tray(image);
            image.sprite=sprite;image.type=Image.Type.Sliced;image.fillCenter=true;image.color=Color.white;
            var canvas=image.GetComponentInParent<Canvas>();
            if(canvas!=null)image.pixelsPerUnitMultiplier=canvas.referencePixelsPerUnit*Mathf.Abs(image.transform.lossyScale.x)*32/sprite.pixelsPerUnit;
        }
    }
}
