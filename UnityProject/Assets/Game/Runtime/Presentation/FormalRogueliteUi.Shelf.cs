using System;
using System.Linq;
using OCC.Combat.Roguelite;
using UnityEngine;
using UnityEngine.UI;

namespace OCC.Combat.Presentation
{
    public sealed partial class FormalRogueliteUi
    {
        private string selectedShopOffer;
        private void DrawShelfShop(Transform parent,RogueliteMapRun run)
        {
            var top=Panel("市集信息栏",parent,new Vector2(0,1),new Vector2(0,1),new Vector2(24,-16),new Vector2(1872,88),ArchiveUiStyle.Paper);
            Header("学院市集",string.Empty,top);
            ShopMetric(top.transform,new Vector2(1560,-12),"金币",run.Gold.ToString(),FormalArtRegistry.ResourceMetricPath("gold"),amber);
            OccDetailAudio.For(bootstrap)?.SetRoom("shop");
            var shop=run.CurrentShopService;
            var catalog=RogueContentCatalog.CreateAcademyV01();
            var selected=shop.Offers.FirstOrDefault(x=>x.OfferId==selectedShopOffer)??shop.Offers.FirstOrDefault();
            selectedShopOffer=selected?.OfferId;
            var root=FormalUiKit.AnchoredPanel("学院货架市集",parent,new Vector2(0,1),new Vector2(0,1),
                new Vector2(64,-146),new Vector2(1792,800),Color.clear);
            var outerSkin=root.transform.Find("正式皮肤");if(outerSkin!=null)outerSkin.gameObject.SetActive(false);
            // Reuse existing wood pixels. The concept is reference only, never a UI bitmap.
            var shelf=root.AddComponent<OccShopShelfSurface>();
            shelf.Build();
            for(int i=0;i<shop.Offers.Count;i++)
            {
                var offer=shop.Offers[i];bool chosen=offer.OfferId==selectedShopOffer;
                float x=52+i*336;
                var pick=ActionButton(string.Empty,string.Empty,root.transform,new Vector2(x,-124),new Vector2(312,330),
                    chosen?amber:cyan,true,()=>{selectedShopOffer=offer.OfferId;Invalidate(false);});
                pick.name="货架商品_"+offer.OfferId;
                pick.GetComponent<UiButtonFeedback>().enabled=false;
                pick.GetComponent<Button>().transition=Selectable.Transition.None;
                pick.GetComponent<Image>().color=Color.clear;
                var skin=pick.transform.Find("正式皮肤");if(skin!=null)skin.gameObject.SetActive(chosen);
                foreach(var label in pick.GetComponentsInChildren<Text>(true))label.gameObject.SetActive(false);
                if(!offer.Sold)
                {
                    var art=FormalUiKit.TopLeftIconSlot("货架实物",pick.transform,
                        Resources.Load<Sprite>(ShopContentIconPath(catalog,offer.DefinitionId)),new Vector2(156,-170));
                    art.rectTransform.anchorMin=art.rectTransform.anchorMax=new Vector2(0,1);
                    art.rectTransform.pivot=new Vector2(.5f,0);art.rectTransform.anchoredPosition=new Vector2(156,-316);
                    art.gameObject.AddComponent<OccShopShelfArtwork>();
                }
                var tag=Panel("货架价签_"+offer.OfferId,root.transform,new Vector2(0,1),new Vector2(0,1),
                    new Vector2(x,-472),new Vector2(312,96),ArchiveUiStyle.Paper);
                Label("商品名称",ContentName(catalog,offer.DefinitionId),tag.transform,new Vector2(20,-12),new Vector2(272,32),24,text,TextAnchor.MiddleCenter);
                Label("商品价格",offer.Sold?"已售罄":offer.Price+" 金币",tag.transform,new Vector2(20,-50),new Vector2(272,32),24,offer.Sold?muted:amber,TextAnchor.MiddleCenter);
                BindHover(pick,ContentName(catalog,offer.DefinitionId),offer.Sold?"已售罄":"查看效果与购买条件",amber);
            }
            var detail=Panel("货架商品详情",root.transform,new Vector2(0,1),new Vector2(0,1),new Vector2(1140,-12),new Vector2(600,688),ArchiveUiStyle.Paper);
            Label("货架详情标题",selected==null?"暂无商品":ContentName(catalog,selected.DefinitionId),detail.transform,new Vector2(48,-40),new Vector2(504,64),48,text,TextAnchor.MiddleLeft);
            if(selected!=null)
            {
                Label("商品类别",ContentCategory(catalog,selected.DefinitionId),detail.transform,new Vector2(48,-116),new Vector2(504,40),24,muted,TextAnchor.MiddleLeft);
                string productName=ContentName(catalog,selected.DefinitionId);
                FormalTooltipContent shared;
                string details=ShopContentDetail(catalog,selected.DefinitionId);
                if(TryGetSharedContentCard("法宝",productName,out shared)||TryGetSharedContentCard(ContentCategory(catalog,selected.DefinitionId),productName,out shared))
                    details=shared.Effect+"\n\n"+ShopContentDetail(catalog,selected.DefinitionId);
                var effect=Label("商品完整效果",details,detail.transform,new Vector2(48,-180),new Vector2(504,244),24,text,TextAnchor.UpperLeft);
                FormalUiKit.ConfigureParagraph(effect);
                bool affordable=run.Gold>=selected.Price,space=run.CanAcceptFirstRunOffer(selected.OfferId);
                bool canBuy=!selected.Sold&&affordable&&space;
                string reason=selected.Sold?"已售罄":!affordable?"金币不足，还差 "+(selected.Price-run.Gold):!space?"背包空间不足，先整理背包":"每件限购一件";
                Label("购买条件",reason,detail.transform,new Vector2(48,-440),new Vector2(504,64),24,canBuy?muted:danger,TextAnchor.UpperLeft);
                ActionButton(selected.Sold?"已售罄":"购买 · "+selected.Price+" 金币",string.Empty,detail.transform,
                    new Vector2(48,-544),new Vector2(504,88),canBuy?cyan:muted,canBuy,()=>bootstrap.PurchaseFirstRunOffer(selected.OfferId));
            }
            ActionButton("打开背包",string.Empty,root.transform,new Vector2(52,-716),new Vector2(400,76),cyan,true,()=>SetOverlay(UiOverlay.Loadout));
            ActionButton("离开市集",string.Empty,root.transform,new Vector2(492,-716),new Vector2(400,76),safe,true,
                ()=>{if(run.IsTutorialPhase)bootstrap.CompleteFirstRunExperience();else ReturnFromService(run);});
        }
    }

    internal sealed class OccShopShelfArtwork:MonoBehaviour {}
    internal sealed class OccShopShelfSurface:MonoBehaviour
    {
        private Sprite wood;
        internal void Build()
        {
            var source=Resources.Load<Sprite>("Art/MainMenuE/Stage/desk");
            if(source==null)throw new InvalidOperationException("Existing desk wood is missing");
            var r=source.rect;
            // Technical crop of an existing texture; no new image or colour is synthesized.
            wood=Sprite.Create(source.texture,new Rect(r.x+r.width*.25f,r.y+r.height*.38f,r.width*.5f,Mathf.Max(8,r.height*.28f)),new Vector2(.5f,.5f),32);
            Board("货架背板",new Vector2(28,-16),new Vector2(1072,666));
            Board("货架顶板",new Vector2(28,-16),new Vector2(1072,80));
            Board("货架承重板",new Vector2(28,-440),new Vector2(1072,52));
            Board("货架下柜",new Vector2(28,-614),new Vector2(1072,68));
            Board("货架左立柱",new Vector2(28,-16),new Vector2(36,666));
            Board("货架右立柱",new Vector2(1064,-16),new Vector2(36,666));
        }
        private void Board(string name,Vector2 at,Vector2 size)
        {
            var image=ShopCounterDetail.ImageAt(name,transform,wood,at,size);image.type=Image.Type.Tiled;
        }
        internal static void FitNative(GameObject root)
        {
            foreach(var shelf in root.GetComponentsInChildren<OccShopShelfSurface>(true))
                foreach(var image in shelf.GetComponentsInChildren<Image>(true))
                {
                    var canvas=image.GetComponentInParent<Canvas>();float scale=Mathf.Abs(image.transform.lossyScale.x);
                    if(image.sprite==shelf.wood)image.pixelsPerUnitMultiplier=canvas.referencePixelsPerUnit*scale;
                    if(image.GetComponent<OccShopShelfArtwork>()!=null&&image.sprite!=null)
                        image.rectTransform.sizeDelta=image.sprite.rect.size/(32*scale);
                }
        }
        private void OnDestroy(){if(wood!=null)Destroy(wood);}
    }
}
