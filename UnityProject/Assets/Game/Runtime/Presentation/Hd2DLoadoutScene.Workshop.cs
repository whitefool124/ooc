using UnityEngine;
using UnityEngine.UI;

namespace OCC.Combat.Presentation
{
    internal sealed partial class Hd2DLoadoutScene
    {
        // Local art candidate. It reuses the production layout, input and data owners.
        // Source provenance and pending human review live beside the independent art.
        private const string WorkshopArt="Art/AcademyWorkshop20261023/";
        private static readonly Color WorkshopLightInk=new Color(.96f,.92f,.80f);
        private void ApplyWorkshopArt(RogueLoadoutEquipmentView view)
        {
            var folio=Resources.Load<Sprite>(WorkshopArt+"Folio");
            var canvas=Resources.Load<Sprite>(WorkshopArt+"Canvas");
            var paper=Resources.Load<Sprite>(WorkshopArt+"Dossier");
            var socket=Resources.Load<Sprite>(WorkshopArt+"Socket");
            var emptySocket=Resources.Load<Sprite>(WorkshopArt+"EmptySocket");
            if(folio==null||canvas==null||paper==null||socket==null||emptySocket==null)
                throw new System.InvalidOperationException("Academy workshop candidate assets are incomplete.");
            if(view!=null)
            {
                WorkshopFrame(view.CharacterContentRoot.parent.GetComponent<Image>(),paper);
                WorkshopClamp(view.CharacterContentRoot.parent as RectTransform);
                WorkshopFrame(Region("装备工作区")?.GetComponent<Image>(),folio);
                var roll=Resources.Load<Sprite>(WorkshopArt+"ToolRoll");
                if(roll==null)throw new System.InvalidOperationException("Workshop tool roll missing");
                WorkshopFrame(Region("战术栏工作区")?.GetComponent<Image>(),roll);
                WorkshopFrame(view.BackpackPanel.GetComponent<Image>(),canvas);
                foreach(var slot in view.EquipmentSlots)WorkshopFrame(slot.Root.GetComponent<Image>(),socket);
                foreach(var slot in view.TacticalSlots)WorkshopFrame(slot.Root.GetComponent<Image>(),socket);
                foreach(var cell in view.BackpackCells)
                    if(cell!=null&&cell.TryGetComponent<Image>(out var guide))guide.color=new Color(.94f,.91f,.82f,1);
                WorkshopHeading(Region("装备工作区"),"区域标题_1");
                var tactical=Region("战术栏工作区");
                if(tactical!=null)foreach(var label in tactical.GetComponentsInChildren<Text>(true))
                    if(label.name=="战术区域标题")
                    {label.color=ArchiveUiStyle.Ink;label.rectTransform.anchoredPosition=new Vector2(48,-50);label.rectTransform.sizeDelta=new Vector2(136,40);label.alignment=TextAnchor.MiddleCenter;}
                // The small entry retains the thin button rim: stacked paper
                // layers need a full writing surface and do not fit this control.
                foreach(var label in view.BackpackPanel.GetComponentsInChildren<Text>(true))
                {
                    if(label.name=="区域标题_2")label.rectTransform.anchoredPosition=new Vector2(88,-22);
                    if(label.name=="整备摘要")
                    {label.rectTransform.anchoredPosition=new Vector2(260,-22);label.rectTransform.sizeDelta=new Vector2(212,40);}
                }
            }
            else
            {
                var work=Region("术式工作区");
                WorkshopFrame(work?.GetComponent<Image>(),folio);
                WorkshopFrame(Region("术式详情")?.GetComponent<Image>(),paper);
                WorkshopClamp(Region("术式详情"));
                WorkshopHeading(work,"术式标题");
                WorkshopHeading(work,"术式说明");
                if(work!=null)foreach(var button in work.GetComponentsInChildren<Button>(true))
                {
                    var name=button.transform.Find("名称")?.GetComponent<Text>();
                    bool empty=name!=null&&name.text.Contains("空槽");
                    WorkshopFrame(button.targetGraphic as Image,empty?emptySocket:socket);
                    if(empty)
                    {
                        foreach(var label in button.GetComponentsInChildren<Text>(true))label.color=WorkshopLightInk;
                        var colors=button.colors;colors.normalColor=new Color(.76f,.79f,.83f);
                        colors.highlightedColor=colors.selectedColor=Color.white;button.colors=colors;
                    }
                    var cost=button.transform.Find("详情") as RectTransform;
                    if(cost!=null)cost.anchoredPosition=new Vector2(20,-240);
                }
            }
        }
        private static void WorkshopClamp(RectTransform parent)
        {
            if(parent==null)return;
            var sprite=Resources.Load<Sprite>(WorkshopArt+"Clamp");
            if(sprite==null)throw new System.InvalidOperationException("Workshop clamp source missing");
            var node=new GameObject("独立黄铜装订夹",typeof(RectTransform),typeof(CanvasRenderer),typeof(Image));
            node.layer=parent.gameObject.layer;node.transform.SetParent(parent,false);
            var image=node.GetComponent<Image>();image.sprite=sprite;image.raycastTarget=false;
            var rect=image.rectTransform;rect.anchorMin=rect.anchorMax=rect.pivot=new Vector2(.5f,1);
            rect.anchoredPosition=new Vector2(parent.rect.width*.5f-80,24);
            rect.sizeDelta=sprite.rect.size/(32*Mathf.Abs(rect.lossyScale.x));
        }
        private static void WorkshopHeading(RectTransform region,string name)
        {
            if(region==null)return;
            foreach(var label in region.GetComponentsInChildren<Text>(true))
                if(label.name==name)label.color=WorkshopLightInk;
        }
        private static void WorkshopFrame(Image image,Sprite sprite)
        {
            if(image==null)return;
            image.sprite=sprite;image.type=Image.Type.Sliced;image.fillCenter=true;image.color=Color.white;
            var canvas=image.GetComponentInParent<Canvas>();
            if(canvas!=null)image.pixelsPerUnitMultiplier=canvas.referencePixelsPerUnit*Mathf.Abs(image.transform.lossyScale.x)*
                Hd2DDeskArtCandidate.ScenePixelsPerUnit/sprite.pixelsPerUnit;
        }
    }
}
