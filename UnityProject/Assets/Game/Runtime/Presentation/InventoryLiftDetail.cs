using System;
using UnityEngine;
using UnityEngine.UI;
namespace OCC.Combat.Presentation
{
    public sealed class InventoryLiftDetail:MonoBehaviour
    {
        private Image icon,shadow;
        private Func<float> intensity;
        private float started;
        private bool settling;
        public static void Settle(Image actual,Func<float> animation)
        {
            if(actual==null || actual.sprite==null || animation()==0)return;
            var detail=actual.gameObject.AddComponent<InventoryLiftDetail>();detail.icon=actual;detail.intensity=animation;detail.started=Time.unscaledTime;detail.settling=true;
            detail.shadow=ImageAt(actual.transform.parent,"放下接触影",actual.sprite,new Color(.10f,.08f,.06f,.28f));
            detail.shadow.transform.SetSiblingIndex(actual.transform.GetSiblingIndex());
            detail.shadow.rectTransform.sizeDelta=actual.rectTransform.sizeDelta;
            detail.shadow.rectTransform.localEulerAngles=actual.rectTransform.localEulerAngles;
            detail.shadow.rectTransform.anchoredPosition=new Vector2(0,-2);
        }
        public static void Attach(Transform parent,Sprite sprite,Func<float> animation)
        {
            if(sprite==null)return;
            var go=new GameObject("提起物件",typeof(RectTransform));go.transform.SetParent(parent,false);
            var rect=(RectTransform)go.transform;rect.anchorMin=Vector2.zero;rect.anchorMax=Vector2.one;rect.offsetMin=rect.offsetMax=Vector2.zero;
            var detail=go.AddComponent<InventoryLiftDetail>();detail.intensity=animation;detail.started=Time.unscaledTime;
            detail.shadow=ImageAt(go.transform,"接触影",sprite,new Color(.10f,.08f,.06f,.18f));
            detail.icon=ImageAt(go.transform,"实际物件",sprite,Color.white);
        }
        private static Image ImageAt(Transform parent,string name,Sprite sprite,Color tint)
        {
            var go=new GameObject(name,typeof(RectTransform),typeof(CanvasRenderer),typeof(Image));go.transform.SetParent(parent,false);
            var image=go.GetComponent<Image>();image.sprite=sprite;image.color=tint;image.preserveAspect=true;image.raycastTarget=false;
            var rect=image.rectTransform;rect.anchorMin=rect.anchorMax=rect.pivot=new Vector2(.5f,.5f);
            return image;
        }
        public void Refresh(bool rotated)
        {
            var rect=(RectTransform)transform.parent;
            float available=Mathf.Min(rect.rect.width,rect.rect.height)-10;
            int size=FormalUiKit.IntegerSpriteSize(icon.sprite,available);
            icon.rectTransform.sizeDelta=shadow.rectTransform.sizeDelta=rect.name=="战外背包拖拽预览"
                ?icon.sprite.rect.size*2:Vector2.one*size;
            icon.rectTransform.localEulerAngles=shadow.rectTransform.localEulerAngles=rotated?new Vector3(0,0,-90):Vector3.zero;
            Sample();
        }
        private void Update(){if(icon!=null)Sample();}
        private void OnDisable(){if(settling && shadow!=null)Destroy(shadow.gameObject);}
        private void Sample()
        {
            float level=intensity==null?0:intensity();
            if(settling)
            {
                float age=(Time.unscaledTime-started)/.18f;
                if(level<=.01f || age>=1){Destroy(shadow.gameObject);Destroy(this);return;}
                shadow.color=new Color(.10f,.08f,.06f,.28f*(1-age));return;
            }
            if(level<=.01f){started=Time.unscaledTime-1;icon.rectTransform.anchoredPosition=Vector2.zero;shadow.enabled=false;return;}
            float rise=Mathf.Clamp01((Time.unscaledTime-started)/.18f);
            icon.rectTransform.anchoredPosition=new Vector2(0,Mathf.Round(4*rise));
            shadow.enabled=true;shadow.rectTransform.anchoredPosition=new Vector2(0,-2);
            shadow.color=new Color(.10f,.08f,.06f,Mathf.Lerp(.24f,.12f,rise));
        }
    }
}
