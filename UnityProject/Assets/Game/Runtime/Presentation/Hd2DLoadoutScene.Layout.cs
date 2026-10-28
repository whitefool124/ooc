using UnityEngine;
using UnityEngine.UI;

namespace OCC.Combat.Presentation
{
    internal sealed partial class Hd2DLoadoutScene
    {
        // Keep the existing world pixel pitch and writing-plane position while
        // restoring the approved 24-degree lens. Distance, not a wider lens,
        // supplies room for the three columns in front of the desk lip.
        private static readonly float ReadingDepth=6f*Mathf.Tan(25f*Mathf.Deg2Rad)/Mathf.Tan(12f*Mathf.Deg2Rad);
        // One writing plane for the three columns. The papers remain lit world
        // surfaces; their content and input share the same reference coordinates.
        private void ComposeLoadout(MainMenuHd2DStage stage,RogueLoadoutEquipmentView view)
        {
            stage.sceneCamera.fieldOfView=24;
            backdrop.SetReadingCameraDistanceOffset(ReadingDepth-6f);
            if(view!=null)
            {
                var character=view.CharacterContentRoot.parent as RectTransform;
                Sheet(stage,character,new Rect(48,108,440,800));
                Sheet(stage,Region("装备工作区"),new Rect(520,108,760,636));
                Sheet(stage,Region("战术栏工作区"),new Rect(520,768,760,140));
                Sheet(stage,view.BackpackPanel,new Rect(1312,108,560,800));
                Box(view.CharacterContentRoot,0,0,440,800);
                Title(character,"区域标题_0","角色状态",32,22,376);
                Title(Region("装备工作区"),"区域标题_1","装备",32,22,696);
                Title(Region("战术栏工作区"),"战术区域标题","战术栏",24,12,220);
                Title(view.BackpackPanel,"区域标题_2","背包",32,22,190);
                foreach(var text in view.CharacterContentRoot.GetComponentsInChildren<Text>(true))
                {
                    if(text.name=="角色栏标题")text.gameObject.SetActive(false);
                    if(text.name=="被动标题")Box(text.rectTransform,32,630,376,40);
                    if(text.name=="被动名称")Box(text.rectTransform,32,674,280,40);
                    if(text.name=="被动说明")Box(text.rectTransform,32,720,280,64);
                }
                var portrait=view.CharacterContentRoot.Find("角色立绘框") as RectTransform;
                Box(portrait,40,84,360,380);
                if(portrait!=null){portrait.GetComponent<Image>().color=Color.clear;ArchiveUiStyle.HideOldChrome(portrait);}
                foreach(var rect in view.CharacterContentRoot.GetComponentsInChildren<RectTransform>(true))
                {
                    if(rect.name=="资源_生命")Box(rect,32,492,180,56);
                    if(rect.name=="资源_魔力")Box(rect,228,492,180,56);
                    if(rect.name=="资源_金币")Box(rect,32,560,180,56);
                    if(rect.name=="资源_贡献")Box(rect,228,560,180,56);
                }
                for(int i=0;i<view.EquipmentSlots.Length;i++)
                {
                    var slot=view.EquipmentSlots[i];
                    Box(slot.Root,32+(i%3)*236,74+(i/3)*184,224,176);
                    Centre(slot.Icon.rectTransform,0,16);
                }
                for(int i=0;i<view.TacticalSlots.Length;i++)
                {
                    var slot=view.TacticalSlots[i];
                    Box(slot.Root,200+i*134,20,122,100);
                    Centre(slot.Icon.rectTransform,0,0);
                    Box(slot.Quantity.rectTransform,46,40,64,48);
                    slot.Quantity.enabled=true;slot.Quantity.fontSize=24;
                    slot.Quantity.horizontalOverflow=HorizontalWrapMode.Overflow;
                    slot.Quantity.verticalOverflow=VerticalWrapMode.Overflow;
                    slot.Quantity.alignment=TextAnchor.MiddleRight;
                }
                // Grid dimensions/cell size are already bound to the real bag.
                // Keep their input coordinates intact; only move the whole grid.
                var grid=view.BackpackGrid;
                grid.anchoredPosition=new Vector2((560-grid.rect.width)*.5f,-112);
                foreach(var label in view.BackpackPanel.GetComponentsInChildren<Text>(true))
                {
                    if(label.name=="整备摘要")Box(label.rectTransform,244,22,284,40);
                    if(label.text.Contains("拖拽整理")){Box(label.rectTransform,88,68,384,36);label.fontSize=24;}
                }
                // Interaction errors have a dedicated footer, not the title line.
                Box(view.BackpackStatus.rectTransform,32,752,496,40);
                view.TacticalStatus.gameObject.SetActive(false);
                Sheet(stage,Region("实体档案册入口"),new Rect(48,940,360,64));
                var dossier=Region("实体档案册入口");
                if(dossier!=null)Hd2DContainerStyle.Tray(dossier.GetComponent<Image>());
            }
            else
            {
                var work=Region("术式工作区");var detail=Region("术式详情");
                Sheet(stage,work,new Rect(48,108,1120,800));
                Sheet(stage,detail,new Rect(1200,108,672,800));
                foreach(var label in work.GetComponentsInChildren<Text>(true))
                {
                    if(label.name=="术式标题"){Box(label.rectTransform,32,24,1056,64);label.text="术式编组";}
                    if(label.name=="术式说明"){Box(label.rectTransform,32,100,1056,40);label.text="选择槽位，在右侧浏览、替换或移除术式。数字对应战斗快捷键。";label.fontSize=24;}
                }
                int index=0;
                foreach(var button in work.GetComponentsInChildren<Button>(true))
                {
                    {
                        int slot=index++;
                        Box((RectTransform)button.transform,32+(slot%4)*272,168+(slot/4)*304,240,288);
                        var title=button.transform.Find("名称")?.GetComponent<Text>();
                        var cost=button.transform.Find("详情")?.GetComponent<Text>();
                        if(title!=null)Box(title.rectTransform,20,196,200,40);
                        if(cost!=null)Box(cost.rectTransform,20,240,96,32);
                        var mana=button.transform.Find("魔力消耗") as RectTransform;
                        if(mana!=null)Box(mana,124,240,96,32);
                        var art=button.transform.Find("操作图标") as RectTransform;
                        if(art!=null)Centre(art,0,48);
                    }
                }
                foreach(var label in detail.GetComponentsInChildren<Text>(true))
                {
                    if(label.name=="术式名称")Box(label.rectTransform,32,24,608,64);
                    if(label.name=="术式槽位")Box(label.rectTransform,32,92,608,40);
                    if(label.name=="目标标题")Box(label.rectTransform,32,276,608,40);
                    if(label.name=="目标规则")Box(label.rectTransform,32,322,608,96);
                    if(label.name=="规则标题")Box(label.rectTransform,32,434,608,40);
                    if(label.name=="术式规则")Box(label.rectTransform,32,480,608,176);
                }
                foreach(var rect in detail.GetComponentsInChildren<RectTransform>(true))
                    if(rect.name.StartsWith("指标_"))
                    {
                        rect.anchoredPosition=new Vector2(rect.anchoredPosition.x,-158);
                        ArchiveUiStyle.HideOldChrome(rect);
                        var image=rect.GetComponent<Image>();if(image!=null){image.sprite=null;image.color=Color.clear;}
                        var skin=rect.Find("正式皮肤");if(skin!=null)skin.gameObject.SetActive(false);
                    }
                int action=0;
                foreach(var button in detail.GetComponentsInChildren<Button>(true))
                {
                    Box((RectTransform)button.transform,32+action++*154,700,144,64);
                    foreach(var label in button.GetComponentsInChildren<Text>(true))
                        if(label.name=="名称"&&label.text=="装入或替换")label.text="装入术式";
                }
            }
            foreach(var carrier in sceneRoot.GetComponentsInChildren<Canvas>(true))
            {
                var rect=(RectTransform)carrier.transform;
                if(carrier.name=="按钮_装备、背包与战术栏")Sheet(stage,rect,new Rect(520,940,360,64),true);
                else if(carrier.name=="按钮_术式编组")Sheet(stage,rect,new Rect(904,940,360,64),true);
                else if(carrier.name.StartsWith("按钮_返回")||carrier.name=="按钮_确认出发")Sheet(stage,rect,new Rect(1512,940,360,64),true);
                if(carrier.name=="以太晶灯亮度调节")
                {
                    Sheet(stage,rect,new Rect(48,996,360,80),true);
                    var label=rect.Find("晶灯亮度") as RectTransform;Box(label,0,0,320,32);
                    var slider=rect.GetComponentInChildren<Slider>(true);
                    if(slider!=null)
                    {
                        Box((RectTransform)slider.transform,20,36,280,40);
                        var track=slider.transform.Find("亮度轨道") as RectTransform;Box(track,0,17,280,6);
                        var range=slider.handleRect.parent as RectTransform;
                        range.anchorMin=Vector2.zero;range.anchorMax=Vector2.one;
                        range.offsetMin=new Vector2(8,0);range.offsetMax=new Vector2(-8,0);
                        slider.handleRect.anchorMin=new Vector2(slider.normalizedValue,0);
                        slider.handleRect.anchorMax=new Vector2(slider.normalizedValue,1);
                        slider.handleRect.sizeDelta=new Vector2(16,0);slider.handleRect.anchoredPosition=Vector2.zero;
                    }
                }
            }
            foreach(var image in sceneRoot.GetComponentsInChildren<Image>(true))
            {
                if(image.GetComponent<Hd2DContainerStyle>()!=null)ArchiveUiStyle.HideOldChrome(image.transform);
                if(image.name.StartsWith("指标细框"))image.gameObject.SetActive(false);
            }
            var lamp=sceneRoot.transform.Find("独立2D以太晶灯");
            if(lamp!=null&&lamp.TryGetComponent<SpriteRenderer>(out var lampSprite)&&lampSprite.sprite!=null)
            {
                var previous=lamp.position;
                float x=view!=null?420:124,y=view!=null?824:936;
                lamp.rotation=stage.sceneCamera.transform.rotation;
                lamp.position=stage.sceneCamera.ViewportToWorldPoint(new Vector3(x/1920f,1-y/1080f,ReadingDepth-.1f));
                lamp.position-=lamp.TransformVector(lampSprite.sprite.bounds.center);
                var light=sceneRoot.transform.Find("整备台灯暖光");
                if(light!=null)light.position+=lamp.position-previous;
            }
        }
        private RectTransform Region(string name)
        {
            foreach(var rect in sceneRoot.GetComponentsInChildren<RectTransform>(true))
                if(rect.name==name&&rect.GetComponent<Canvas>()==null)return rect;
            return null;
        }
        private static void Box(RectTransform rect,float x,float y,float width,float height)
        {
            if(rect==null)return;
            rect.anchorMin=rect.anchorMax=rect.pivot=new Vector2(0,1);
            rect.anchoredPosition=new Vector2(x,-y);rect.sizeDelta=new Vector2(width,height);
        }
        private static void Centre(RectTransform rect,float x,float y)
        {rect.anchorMin=rect.anchorMax=rect.pivot=new Vector2(.5f,.5f);rect.anchoredPosition=new Vector2(x,y);}
        private static void Title(RectTransform parent,string name,string title,float x,float y,float width)
        {
            if(parent==null)return;
            var label=parent.Find(name)?.GetComponent<Text>();if(label==null)return;
            label.text=title;Box(label.rectTransform,x,y,width,40);label.fontSize=24;
        }
        private static void Sheet(MainMenuHd2DStage stage,RectTransform region,Rect frame,bool isCarrier=false)
        {
            if(region==null)return;
            var carrier=isCarrier?region:region.parent as RectTransform;
            var camera=stage.sceneCamera;
            float depth=ReadingDepth;
            var left=camera.ViewportToWorldPoint(new Vector3(0,.5f,depth));
            var right=camera.ViewportToWorldPoint(new Vector3(1,.5f,depth));
            float unit=Vector3.Distance(left,right)/1920f;
            carrier.position=camera.ViewportToWorldPoint(new Vector3(frame.center.x/1920f,1-frame.center.y/1080f,depth));
            carrier.rotation=camera.transform.rotation;
            float parentScale=carrier.parent.lossyScale.x;
            carrier.localScale=Vector3.one*unit/parentScale;carrier.sizeDelta=frame.size;
            if(!isCarrier)
            {region.anchorMin=region.anchorMax=region.pivot=new Vector2(.5f,.5f);region.anchoredPosition=Vector2.zero;region.sizeDelta=frame.size;}
            else
                foreach(var child in carrier.GetComponentsInChildren<Button>(true))
                {Box((RectTransform)child.transform,0,0,frame.width,frame.height);Centre((RectTransform)child.transform,0,0);}
        }
    }
}
