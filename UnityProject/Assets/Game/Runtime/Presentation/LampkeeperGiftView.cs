using System;
using UnityEngine;
using UnityEngine.UI;

namespace OCC.Combat.Presentation
{
    public sealed class LampkeeperGiftView : MonoBehaviour
    {
        public const string ResourcePath = "UI/HD2D/lampkeeper";
        [SerializeField] private Text heading, introduction;
        [SerializeField] private Text[] titles, descriptions;
        [SerializeField] private Image keeper;
        [SerializeField] private Image[] illustrations;
        [SerializeField] private Button[] choices;
        [SerializeField] private Button map, equipment, home;
        public Button[] ChoiceButtons => choices;
        public void Bind(bool spellChoice, string[] ids, Func<string,string> title, Func<string,string> description,
            Action<string> choose, Action previewMap, Action previewEquipment, Action returnHome)
        {
            heading.text = spellChoice ? "空白批注 · 选择一项术式" : "守灯人的赠礼";
            introduction.text = spellChoice ? "这页留给你。选定后掌握术式，再到整备页配置。" :
                "路上的事，我不能替你决定。灯下这些，你可以带走一样。";
            heading.rectTransform.anchoredPosition=new Vector2(132,-104);heading.rectTransform.sizeDelta=new Vector2(1500,64);
            introduction.rectTransform.anchoredPosition=new Vector2(132,-176);introduction.rectTransform.sizeDelta=new Vector2(1600,48);
            var headingPaper=transform.Find("标题承载纸") as RectTransform;
            if(headingPaper!=null)headingPaper.gameObject.SetActive(false);
            if(transform.Find("赠礼主纸页")==null)
            {
                var sheet=FormalUiKit.AnchoredPanel("赠礼主纸页",transform,new Vector2(0,1),new Vector2(0,1),
                    new Vector2(84,-64),new Vector2(1740,910),ArchiveUiStyle.LightPaper);
                sheet.transform.SetAsFirstSibling();sheet.GetComponent<Image>().raycastTarget=false;
            }
            {
                var keeperSheet=keeper.transform.parent as RectTransform;
                keeperSheet.sizeDelta=new Vector2(keeperSheet.sizeDelta.x,650);
                keeperSheet.GetComponent<Image>().color=Color.clear;
                foreach(var note in keeperSheet.GetComponentsInChildren<Text>(true))
                {note.rectTransform.anchoredPosition=new Vector2(56,-548);note.rectTransform.sizeDelta=new Vector2(220,48);note.text="每轮一份";}
                foreach(var navigation in new[]{map,equipment,home})
                {var r=(RectTransform)navigation.transform;r.anchoredPosition=new Vector2(r.anchoredPosition.x,-900);AetherPaperChrome.Quiet(navigation);}
            }
            SetArt(keeper, "keeper");
            // Keep the character illustration above its paper carrier in the desk's lit geometry.
            var keeperPosition=keeper.rectTransform.localPosition;
            keeperPosition.z=-4;
            keeper.rectTransform.localPosition=keeperPosition;
            for (int i = 0; i < choices.Length; i++)
            {
                bool exists = i < ids.Length; choices[i].transform.parent.gameObject.SetActive(exists);
                if (!exists) continue;
                string id = ids[i]; titles[i].text = title(id); descriptions[i].text = description(id);
                titles[i].rectTransform.anchoredPosition=new Vector2(40,-40);
                titles[i].rectTransform.sizeDelta=new Vector2(352,56);
                titles[i].color=descriptions[i].color=ArchiveUiStyle.Ink;
                {
                    ((RectTransform)choices[i].transform.parent).sizeDelta=new Vector2(432,650);
                    choices[i].transform.parent.GetComponent<Image>().color=Color.clear;
                    ((RectTransform)choices[i].transform).anchoredPosition=new Vector2(40,-552);
                    ((RectTransform)choices[i].transform).sizeDelta=new Vector2(352,80);
                    illustrations[i].rectTransform.anchoredPosition=new Vector2(216,-198);
                }
                descriptions[i].rectTransform.anchoredPosition = new Vector2(40, spellChoice ? -132 : -376);
                descriptions[i].rectTransform.sizeDelta = new Vector2(352, spellChoice ? 376 : 164);
                if(!spellChoice)FormalUiKit.Label("收益摘要",GiftSummary(id),choices[i].transform.parent,new Vector2(40,-296),new Vector2(352,72),24,ArchiveUiStyle.Ink,TextAnchor.UpperLeft);
                illustrations[i].gameObject.SetActive(!spellChoice);
                SetArt(illustrations[i], spellChoice ? "LK-05" : id);
                choices[i].GetComponentInChildren<Text>().text = spellChoice ? "掌握此术式" : "接受赠礼";
                choices[i].onClick.RemoveAllListeners(); choices[i].onClick.AddListener(() => choose(id));
            }
            foreach(var label in GetComponentsInChildren<Text>(true)){label.fontStyle=FontStyle.Normal;label.resizeTextForBestFit=false;foreach(var shadow in label.GetComponents<Shadow>())shadow.enabled=false;}
            BindButton(map, previewMap); BindButton(equipment, previewEquipment); BindButton(home, returnHome);
        }
        private static string GiftSummary(string id)
        {
            switch(id)
            {
                case "LK-01": return "新生火场 → 魔力 +1";
                case "LK-02": return "主动移动累计 3 格 → 护盾 +3";
                case "LK-03": return "亲自摧毁耐久物块 → 魔力 +2";
                case "LK-04": return "每场开战 → 普通护盾 +6";
                case "LK-05": return "3 选 1 → 掌握一项新术式";
                default: return "立即获得 12 金币";
            }
        }
        private static void BindButton(Button button, Action action)
        { button.onClick.RemoveAllListeners(); button.onClick.AddListener(() => action()); }
        private static void SetArt(Image image, string id)
        {
            image.sprite = Resources.Load<Sprite>("Art/Lampkeeper/" + id);
            if (image.sprite == null) throw new InvalidOperationException("Missing lampkeeper illustration: " + id);
            image.rectTransform.sizeDelta = image.sprite.rect.size * 4;
            image.preserveAspect = true; image.raycastTarget = false;
        }
#if UNITY_EDITOR
        public static GameObject CreateTemplate()
        {
            var root = new GameObject("守灯人赠礼页", typeof(RectTransform), typeof(LampkeeperGiftView));
            FormalUiKit.Stretch(root.GetComponent<RectTransform>());
            var view = root.GetComponent<LampkeeperGiftView>();
            Panel(root.transform,"标题承载纸",new Vector2(84,-30),new Vector2(1740,172));
            view.heading = Label(root.transform,"标题",new Vector2(84,-42),new Vector2(1500,72),48);
            view.introduction = Label(root.transform,"对白",new Vector2(84,-130),new Vector2(1740,60),24);
            var left = Panel(root.transform,"守灯人陈列",new Vector2(84,-226),new Vector2(310,650));
            view.keeper = Picture(left.transform,"守灯人",new Vector2(155,-230));
            Label(left.transform,"一轮一份 · 出发前选定",new Vector2(20,-520),new Vector2(270,70),24);
            view.titles=new Text[3];view.descriptions=new Text[3];view.illustrations=new Image[3];view.choices=new Button[3];
            for(int i=0;i<3;i++)
            {
                var card=Panel(root.transform,"赠礼卡"+i,new Vector2(440+i*468,-226),new Vector2(432,650));
                view.titles[i]=Label(card.transform,"赠礼名称",new Vector2(28,-30),new Vector2(376,56),30);
                view.illustrations[i]=Picture(card.transform,"赠礼陈列图",new Vector2(216,-222));
                view.descriptions[i]=Label(card.transform,"完整效果",new Vector2(28,-362),new Vector2(376,170),24);
                view.choices[i]=FormalUiKit.Button("按钮_守灯人选择_"+i,"接受这份赠礼",card.transform,new Vector2(28,-552),new Vector2(376,64),ArchiveUiStyle.Paper);
                ArchiveUiStyle.TabButton(view.choices[i],false,()=>UiMotionProfile.FromIntensity(1));
            }
            view.map=FormalUiKit.Button("按钮_守灯人查看地图","查看路线",root.transform,new Vector2(440,-928),new Vector2(400,72),ArchiveUiStyle.Paper);
            view.equipment=FormalUiKit.Button("按钮_守灯人查看配置","查看基础配置",root.transform,new Vector2(908,-928),new Vector2(400,72),ArchiveUiStyle.Paper);
            view.home=FormalUiKit.Button("按钮_守灯人返回首页","暂回首页",root.transform,new Vector2(1376,-928),new Vector2(432,72),ArchiveUiStyle.Paper);
            return root;
        }
        private static GameObject Panel(Transform parent,string name,Vector2 at,Vector2 size)
        {
            var panel=FormalUiKit.AnchoredPanel(name,parent,new Vector2(0,1),new Vector2(0,1),at,size,ArchiveUiStyle.LightPaper);
            ArchiveUiStyle.PaperPanel(panel,ArchiveUiStyle.LightPaper,true);return panel;
        }
        private static Text Label(Transform parent,string name,Vector2 at,Vector2 size,int font)
            => FormalUiKit.Label(name,name,parent,at,size,font,ArchiveUiStyle.Ink,TextAnchor.UpperLeft);
        private static Image Picture(Transform parent,string name,Vector2 at)
        {
            var go=new GameObject(name,typeof(RectTransform),typeof(CanvasRenderer),typeof(Image));go.transform.SetParent(parent,false);
            var r=go.GetComponent<RectTransform>();r.anchorMin=r.anchorMax=new Vector2(0,1);r.pivot=new Vector2(.5f,.5f);r.anchoredPosition=at;
            var image=go.GetComponent<Image>();image.raycastTarget=false;return image;
        }
#endif
    }
}
