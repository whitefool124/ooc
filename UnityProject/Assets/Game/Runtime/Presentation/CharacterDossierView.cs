using UnityEngine;
using UnityEngine.UI;

namespace OCC.Combat.Presentation
{
    public sealed class CharacterDossierView : MonoBehaviour
    {
        public const string ResourcePath="UI/CharacterDossier";
        [SerializeField] private Button back,start;
        public Button[] Bind(FirstExperiencePrototypeController flow)
        {
            back.onClick.RemoveAllListeners();start.onClick.RemoveAllListeners();
            back.onClick.AddListener(flow.GoBack);start.onClick.AddListener(flow.ConfirmConfiguration);
            foreach(var button in new[]{back,start})
                FormalUiKit.ConfigureButtonFeedback(button,FormalUiButtonPalette.ForAccent(ArchiveUiStyle.Brass,FormalUiTheme.Cyan),()=>UiMotionProfile.FromIntensity(flow.AnimationIntensity),null);
            return new[]{start,back};
        }
#if UNITY_EDITOR
        public static GameObject CreateTemplate()
        {
            var root=new GameObject("展开的学生档案",typeof(RectTransform),typeof(CharacterDossierView));FormalUiKit.Stretch(root.GetComponent<RectTransform>());
            var view=root.GetComponent<CharacterDossierView>();
            var left=Leaf(root.transform,"立绘与角色队列",new Vector2(112,-170),new Vector2(820,780));
            var right=Leaf(root.transform,"学生档案与固定配置",new Vector2(948,-170),new Vector2(860,780));
            FormalUiKit.Line(root.transform,new Vector2(932,-170),new Vector2(16,780),new Color(.19f,.15f,.105f),"中央书脊");
            Label(left.transform,"选择主角",new Vector2(42,-30),new Vector2(730,66),48);
            var portrait=new GameObject("维克多像素立绘",typeof(RectTransform),typeof(CanvasRenderer),typeof(Image));portrait.transform.SetParent(left.transform,false);
            var rect=portrait.GetComponent<RectTransform>();rect.anchorMin=rect.anchorMax=new Vector2(0,1);rect.pivot=new Vector2(0,1);rect.anchoredPosition=new Vector2(154,-112);rect.sizeDelta=new Vector2(512,512);
            var image=portrait.GetComponent<Image>();image.sprite=Resources.Load<Sprite>(FormalArtRegistry.DisplayPortraitPath("hero"));image.preserveAspect=true;image.raycastTarget=false;
            for(int i=0;i<6;i++)
            {
                var slot=Leaf(left.transform,"角色槽"+i,new Vector2(38+i*124,-668),new Vector2(120,76));
                slot.GetComponent<Image>().color=i==0?new Color(.83f,.86f,.79f):new Color(.91f,.88f,.80f);
                Label(slot.transform,i==0?"维克多":"未开放",new Vector2(4,-16),new Vector2(112,44),24);
            }
            Label(right.transform,"维克多·维恩",new Vector2(42,-30),new Vector2(770,66),48);
            Label(right.transform,"雾桥村出身　学院工读学生\n火系　公共修缮与实地安全",new Vector2(44,-112),new Vector2(770,72),24);
            FormalUiKit.Line(right.transform,new Vector2(44,-208),new Vector2(772,2),ArchiveUiStyle.Rule,"档案分隔");
            Label(right.transform,"佃农家庭出身，完成教会基础教育后通过学院公开考核，\n以工读生身份入学。",new Vector2(44,-238),new Vector2(768,98),24);
            Label(right.transform,"希望把火系术式用于公共修缮，让危险的以太泄漏有可控、可复核的处置办法。",new Vector2(44,-356),new Vector2(768,116),24);
            Label(right.transform,"先登记风险，再处理最坏的泄漏点。",new Vector2(44,-492),new Vector2(768,48),24);
            FormalUiKit.Line(right.transform,new Vector2(44,-558),new Vector2(772,2),ArchiveUiStyle.Rule,"能力分隔");
            Label(right.transform,"初始能力",new Vector2(44,-576),new Vector2(768,42),24);
            for(int i=0;i<2;i++)
            {
                var ability=Leaf(right.transform,"固定配置卡"+i,new Vector2(44+i*394,-634),new Vector2(378,106));ability.GetComponent<Image>().color=new Color(.92f,.89f,.80f);
                Label(ability.transform,i==0?"就地接线":"维克多护幕",new Vector2(18,-12),new Vector2(342,38),24);
                Label(ability.transform,i==0?"邻接掩体后回盾回魔":"自身获得 8 点普通护盾",new Vector2(18,-54),new Vector2(342,38),24);
            }
            view.back=FormalUiKit.Button("按钮_返回角色选择","返回",root.transform,new Vector2(112,-974),new Vector2(240,72),ArchiveUiStyle.Paper);
            view.start=FormalUiKit.Button("按钮_以此角色开始","以此角色开始",root.transform,new Vector2(1332,-974),new Vector2(476,72),ArchiveUiStyle.Paper);
            ArchiveUiStyle.TabButton(view.back,false,()=>UiMotionProfile.FromIntensity(1));ArchiveUiStyle.ScrollButton(view.start,()=>UiMotionProfile.FromIntensity(1));
            return root;
        }
        private static GameObject Leaf(Transform parent,string name,Vector2 at,Vector2 size)=>FormalUiKit.AnchoredPanel(name,parent,new Vector2(0,1),new Vector2(0,1),at,size,ArchiveUiStyle.LightPaper);
        private static void Label(Transform parent,string text,Vector2 at,Vector2 size,int font)=>FormalUiKit.Label(text,text,parent,at,size,font,ArchiveUiStyle.Ink,TextAnchor.UpperLeft);
#endif
    }
}
