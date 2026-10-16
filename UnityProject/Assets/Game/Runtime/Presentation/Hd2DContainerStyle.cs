using UnityEngine;
using UnityEngine.UI;

namespace OCC.Combat.Presentation
{
    // A semantic container requests authored solid geometry instead of a paper skin.
    internal sealed class Hd2DContainerStyle : MonoBehaviour
    {
        internal static void Tray(Image image)
        {
            if(image==null)return;
            image.sprite=null;image.type=Image.Type.Simple;
            image.color=ArchiveUiStyle.Paper;
            if(image.GetComponent<Hd2DContainerStyle>()==null)image.gameObject.AddComponent<Hd2DContainerStyle>();
        }
        internal static void PrepareControls(GameObject page)
        {
            foreach(var button in page.GetComponentsInChildren<Button>(true))
            {
                var image=button.targetGraphic as Image;
                if(image==null)continue;
                Tray(image);
                foreach(var skin in button.GetComponentsInChildren<Image>(true))
                    if(skin.name=="正式皮肤")skin.color=Color.clear;
                var colors=button.colors;
                colors.normalColor=Color.white;
                colors.highlightedColor=new Color(1f,.97f,.9f);
                colors.selectedColor=colors.highlightedColor;
                colors.pressedColor=new Color(.82f,.74f,.61f);
                colors.disabledColor=new Color(.72f,.69f,.63f);
                colors.colorMultiplier=1;button.colors=colors;
                foreach(var label in button.GetComponentsInChildren<Text>(true))label.color=ArchiveUiStyle.Ink;
            }
        }
        internal static void PrepareLoadout(FormalRogueliteLoadoutShellView shell)
        {
            shell.Card.localScale=Vector3.one*.88f;
            shell.Card.anchoredPosition+=new Vector2(110,-70);
            // Keep the controller's anchors, remove the obsolete full-page sheet.
            foreach(var image in shell.Card.GetComponents<Image>())image.color=Color.clear;
            foreach(var image in shell.Card.GetComponentsInChildren<Image>(true))
            {
                var rect=image.rectTransform.rect;
                if(image.name=="正式皮肤"||image.name=="角色信息栏"||image.name=="装备工作区"||image.name=="战术栏工作区"||
                    image.name=="线"||rect.width>1700||rect.width<=4||rect.height<=4)
                    image.color=Color.clear;
                else if((image.sprite==null||image.type==Image.Type.Sliced)&&rect.width>=50&&rect.height>=50)
                    Tray(image);
            }
            foreach(var label in shell.GetComponentsInChildren<Text>(true))label.color=ArchiveUiStyle.Ink;
        }
    }
}
