using System;
using UnityEngine;
using UnityEngine.UI;

namespace OCC.Combat.Presentation
{
    internal sealed class AetherQuietNavigation : MonoBehaviour { }
    // Scoped sample: the gift page has one document edge. Inner comparisons
    // belong to that paper, rather than being four framed sheets on top of it.
    internal static class AetherPaperChrome
    {
        internal const string ResourcePath = "Art/AetherPaper/ThinPaper";
        internal const string ButtonPath = "Art/AetherPaper/SquareButton";
        internal static Sprite ButtonFrame => Resources.Load<Sprite>(ButtonPath);
        internal static void Quiet(Button button)
        {
            ArchiveUiStyle.HideOldChrome(button.transform);
            if(button.GetComponent<AetherQuietNavigation>()==null)button.gameObject.AddComponent<AetherQuietNavigation>();
            var image=button.targetGraphic as Image;if(image==null)return;
            var old=FormalUiKit.SkinOverlay(image);if(old!=null)old.gameObject.SetActive(false);
            image.sprite=null;image.type=Image.Type.Simple;image.color=Color.clear;
            var feedback=button.GetComponent<UiButtonFeedback>();
            if(feedback==null)feedback=button.gameObject.AddComponent<UiButtonFeedback>();
            feedback.UseQuietNavigation();
        }
        internal static void ApplyOverlay(Image image,bool button)
        {
            if(image==null)return;
            var old=FormalUiKit.SkinOverlay(image);if(old!=null)old.gameObject.SetActive(false);
            image.sprite=button?ButtonFrame:Resources.Load<Sprite>(ResourcePath);
            if(image.sprite==null)throw new InvalidOperationException("Missing thin overlay paper.");
            image.type=Image.Type.Sliced;image.fillCenter=true;image.color=Color.white;
            var canvas=image.GetComponentInParent<Canvas>();
            image.pixelsPerUnitMultiplier=(canvas==null?100:canvas.referencePixelsPerUnit)/(image.sprite.pixelsPerUnit*2f);
        }
        internal static void ApplyGift(GameObject root)
        {
            var sprite = Resources.Load<Sprite>(ResourcePath);
            if (sprite == null) throw new InvalidOperationException("Missing thin paper sample.");
            foreach (var image in root.GetComponentsInChildren<Image>(true))
            {
                if (image.name == "正式皮肤") { image.gameObject.SetActive(false); continue; }
                if(image.GetComponent<AetherQuietNavigation>()!=null){Quiet(image.GetComponent<Button>());continue;}
                if (image.name.StartsWith("赠礼卡", StringComparison.Ordinal) || image.name == "守灯人陈列")
                { image.color = Color.clear; continue; }
                if (image.name != "赠礼主纸页" && image.GetComponent<Button>() == null) continue;
                image.sprite = image.GetComponent<Button>()!=null?ButtonFrame:sprite;
                if(image.sprite==null)throw new InvalidOperationException("Missing square button sample.");
                image.type = Image.Type.Sliced; image.fillCenter = true;
                image.color = Color.white;
                var canvas = image.GetComponentInParent<Canvas>();
                image.pixelsPerUnitMultiplier = canvas.referencePixelsPerUnit * Mathf.Abs(image.transform.lossyScale.x) *
                    Hd2DDeskArtCandidate.ScenePixelsPerUnit / image.sprite.pixelsPerUnit;
            }
        }
    }
}
