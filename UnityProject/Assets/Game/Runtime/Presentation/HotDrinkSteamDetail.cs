using System;
using UnityEngine;
using UnityEngine.UI;
namespace OCC.Combat.Presentation
{
    public sealed class HotDrinkSteamDetail : MaskableGraphic
    {
        public const string ArtPath="Art/FormalMedicalDetails/hot_drink";
#if UNITY_EDITOR
        // Review-only decoded sprite. Never present in a player build or loaded from raw art.
        public static Sprite ReviewSprite;
#endif
        private Sprite cup;
        private Func<float> intensity;
        private bool supplied;
        private float started;
        private int lastTick=-1;
        public bool EmitsSteam=>supplied && intensity!=null && intensity()>.01f && isActiveAndEnabled;
        public static HotDrinkSteamDetail Create(Transform parent,bool available,Func<float> animation)
        {
            Sprite sprite=Resources.Load<Sprite>(ArtPath);
#if UNITY_EDITOR
            if(sprite==null)sprite=ReviewSprite;
#endif
            if(sprite==null)return null;
            var go=new GameObject("热饮蒸汽",typeof(RectTransform),typeof(CanvasRenderer));go.transform.SetParent(parent,false);
            var detail=go.AddComponent<HotDrinkSteamDetail>();
            ConfigureRect(detail.rectTransform);detail.cup=sprite;detail.intensity=animation;detail.supplied=available;
            detail.started=Time.unscaledTime;detail.raycastTarget=false;
            var imageGo=new GameObject("热饮杯碟",typeof(RectTransform),typeof(CanvasRenderer),typeof(Image));imageGo.transform.SetParent(parent,false);
            var image=imageGo.GetComponent<Image>();ConfigureRect(image.rectTransform);image.sprite=sprite;image.raycastTarget=false;
            image.color=available?Color.white:new Color(1,1,1,.48f);
            return detail;
        }
        private static void ConfigureRect(RectTransform rect)
        {
            rect.anchorMin=rect.anchorMax=new Vector2(0,1);rect.pivot=new Vector2(0,1);
            rect.anchoredPosition=new Vector2(40,8);rect.sizeDelta=new Vector2(74,74);
        }
        private void Update()
        {
            int tick=EmitsSteam?Mathf.FloorToInt((Time.unscaledTime-started)*16f):-1;
            if(tick==lastTick)return;lastTick=tick;SetVerticesDirty();
        }
        protected override void OnPopulateMesh(VertexHelper vh)
        {
            vh.Clear();if(!EmitsSteam || cup==null || lastTick<0)return;
            for(int strand=0;strand<3;strand++)
            {
                float age=((lastTick+strand*9)%32)/32f;
                float alpha=Mathf.Sin(age*Mathf.PI)*.42f;
                int y=14-Mathf.FloorToInt(age*15),x=10+strand*4+Mathf.RoundToInt(Mathf.Sin(age*5+strand)*2);
                // Original opaque rim/spoon occludes the rising steam. No fake splitter geometry.
                for(int dy=0;dy<2;dy++)for(int dx=0;dx<2;dx++)
                {
                    int px=x+dx,py=y+dy;
                    if(py>=17 || px<0 || px>=37 || py<0 || py>=37)continue;
                    if(py<12 && cup.texture.GetPixel(px,36-py).a>.5f)continue;
                    Quad(vh,px,py,alpha);
                }
            }
        }
        private void Quad(VertexHelper vh,int x,int y,float alpha)
        {
            float l=x*2f,t=-y*2f;int start=vh.currentVertCount;
            Color32 tint=new Color(.53f,.51f,.46f,alpha);
            vh.AddVert(new Vector3(l,t),tint,Vector2.zero);vh.AddVert(new Vector3(l+2,t),tint,Vector2.zero);
            vh.AddVert(new Vector3(l+2,t-2),tint,Vector2.zero);vh.AddVert(new Vector3(l,t-2),tint,Vector2.zero);
            vh.AddTriangle(start,start+1,start+2);vh.AddTriangle(start,start+2,start+3);
        }
    }
}
