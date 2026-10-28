using UnityEngine;
using UnityEngine.UI;
namespace OCC.Combat.Presentation
{
    internal static class Hd2DPaperSkin
    {
        internal const string ResourcePath=AetherPaperChrome.ResourcePath;
        internal static Sprite Load()
        {
            var sprites=Resources.LoadAll<Sprite>(ResourcePath);
            return sprites.Length==0?null:sprites[0];
        }
        internal static void FitFrames(GameObject root)
        {
            var paper=Load();if(paper==null)return;
            foreach(var image in root.GetComponentsInChildren<Image>(true))
                if(image.sprite==paper||image.sprite==AetherPaperChrome.ButtonFrame)image.type=Image.Type.Sliced;
        }
        internal static void Apply(GameObject root,bool fitFrames=true,RectTransform exempt=null)
        {
            var sprite=Load();if(sprite==null)return;
            foreach(var image in root.GetComponentsInChildren<Image>(true))
            {
                if(exempt!=null&&image.transform.IsChildOf(exempt))continue;
                if(image.GetComponent<AetherQuietNavigation>()!=null)
                {AetherPaperChrome.Quiet(image.GetComponent<Button>());continue;}
                if(image.color.a<.85f||image.GetComponent<Hd2DContainerStyle>()==null)continue;
                // Preserve authored input ownership. A plain paper can be a map
                // gesture surface or modal blocker without being a Selectable.
                // Decorative images are already non-raycast targets at creation.
                bool button=image.GetComponent<Button>()!=null;
                if(!button&&InsidePaper(image.transform.parent,root.transform))
                {image.sprite=null;image.type=Image.Type.Simple;image.color=Color.clear;continue;}
                // Controls sit on the document. A quiet, solid fill leaves the
                // full label height readable instead of repeating its heavy rim.
                image.sprite=button?null:sprite;
                image.type=button?Image.Type.Simple:Image.Type.Sliced;image.fillCenter=true;image.preserveAspect=false;
                image.color=button?Color.Lerp(ArchiveUiStyle.Paper,FormalUiTheme.Cyan,.08f):Color.white;
                var canvas=image.GetComponentInParent<Canvas>();
                if(canvas!=null&&image.sprite!=null)
                    image.pixelsPerUnitMultiplier=canvas.renderMode==RenderMode.ScreenSpaceOverlay?canvas.referencePixelsPerUnit/(image.sprite.pixelsPerUnit*2f):
                        canvas.referencePixelsPerUnit*Mathf.Abs(image.transform.lossyScale.x)*
                        Hd2DDeskArtCandidate.ScenePixelsPerUnit/image.sprite.pixelsPerUnit;
                // A small button cannot contain two native-size frame rims plus its label.
                // Use the parent document's rim rather than shrinking pixel art to fit.
                foreach(var shadow in image.GetComponents<Shadow>())shadow.enabled=false;
            }
            foreach(var text in root.GetComponentsInChildren<Text>(true))
                for(var parent=text.transform.parent;parent!=null&&parent.IsChildOf(root.transform);parent=parent.parent)
                    if(parent.GetComponent<Hd2DContainerStyle>()!=null)
                    {
                        // Existing dark semantic ink keeps the hierarchy of
                        // categories, annotations and body copy on warm paper.
                        if(text.color.grayscale>.65f)text.color=ArchiveUiStyle.Ink;
                        break;
                    }
            foreach(var button in root.GetComponentsInChildren<Button>(true))
            {
                var labels=button.GetComponentsInChildren<Text>(true);
                if(labels.Length!=1)continue;
                var label=labels[0];var r=label.rectTransform;
                r.anchorMin=Vector2.zero;r.anchorMax=Vector2.one;r.offsetMin=new Vector2(24,12);r.offsetMax=new Vector2(-24,-12);
                label.alignment=TextAnchor.MiddleCenter;
                label.resizeTextForBestFit=false;label.fontStyle=FontStyle.Normal;
            }
            if(fitFrames)FitFrames(root);
        }
        private static bool InsidePaper(Transform parent,Transform root)
        {
            for(var node=parent;node!=null&&(node==root||node.IsChildOf(root));node=node.parent)
                if(node.TryGetComponent<Image>(out var image)&&image.color.a>=.85f&&node.GetComponent<Hd2DContainerStyle>()!=null)return true;
            return false;
        }
    }
}
