using UnityEngine;
using UnityEngine.UI;
namespace OCC.Combat.Presentation
{
    internal static class Hd2DPaperSkin
    {
        internal const string ResourcePath="Art/Hd2DContentFrames/简洁纸边";
        internal static Sprite Load()
        {
            var sprites=Resources.LoadAll<Sprite>(ResourcePath);
            return sprites.Length==0?null:sprites[0];
        }
        internal static void Apply(GameObject root)
        {
            var sprite=Load();if(sprite==null)return;
            foreach(var image in root.GetComponentsInChildren<Image>(true))
            {
                if(image.color.a<.85f||image.GetComponent<Hd2DContainerStyle>()==null)continue;
                // Preserve authored input ownership. A plain paper can be a map
                // gesture surface or modal blocker without being a Selectable.
                // Decorative images are already non-raycast targets at creation.
                image.sprite=sprite;image.type=Image.Type.Tiled;image.fillCenter=true;image.color=Color.white;
                var canvas=image.GetComponentInParent<Canvas>();
                if(canvas!=null)
                    image.pixelsPerUnitMultiplier=canvas.renderMode==RenderMode.ScreenSpaceOverlay?3.125f:
                        canvas.referencePixelsPerUnit*Mathf.Abs(image.transform.lossyScale.x)*
                        Hd2DDeskArtCandidate.ScenePixelsPerUnit/sprite.pixelsPerUnit;
                // A small button cannot contain two native-size frame rims plus its label.
                // Use the parent document's rim rather than shrinking pixel art to fit.
                if(canvas!=null&&canvas.renderMode==RenderMode.WorldSpace&&image.GetComponent<Button>()!=null&&
                    image.rectTransform.rect.height*Mathf.Abs(image.transform.lossyScale.y)<
                    (sprite.border.y+sprite.border.w+4)/Hd2DDeskArtCandidate.ScenePixelsPerUnit)
                {image.sprite=null;image.type=Image.Type.Simple;image.color=ArchiveUiStyle.Paper;}
                foreach(var shadow in image.GetComponents<Shadow>())shadow.enabled=false;
            }
            foreach(var text in root.GetComponentsInChildren<Text>(true))
                for(var parent=text.transform.parent;parent!=null&&parent.IsChildOf(root.transform);parent=parent.parent)
                    if(parent.GetComponent<Hd2DContainerStyle>()!=null){text.color=ArchiveUiStyle.Ink;break;}
            foreach(var button in root.GetComponentsInChildren<Button>(true))
            {
                var labels=button.GetComponentsInChildren<Text>(true);
                if(labels.Length!=1)continue;
                var label=labels[0];var r=label.rectTransform;
                r.anchorMin=Vector2.zero;r.anchorMax=Vector2.one;r.offsetMin=new Vector2(12,4);r.offsetMax=new Vector2(-12,-4);
                label.alignment=TextAnchor.MiddleCenter;
                if(((RectTransform)button.transform).rect.height<50)
                {label.resizeTextForBestFit=true;label.resizeTextMinSize=12;label.resizeTextMaxSize=label.fontSize;}
            }
        }
    }
}
