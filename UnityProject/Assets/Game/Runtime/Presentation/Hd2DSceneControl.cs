using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;
using System.Collections.Generic;
namespace OCC.Combat.Presentation
{
    public sealed class Hd2DSceneControl:MonoBehaviour,IPointerDownHandler,IPointerUpHandler,IPointerEnterHandler,IPointerExitHandler
    {
        private bool pressed,hovered;
        private float offset;
        private MeshRenderer shadow;
        private MaterialPropertyBlock shadowBlock;
        private Button button;
        private Material glow;
        private float glowStrength,bodyEmission;
        internal void BindGlow(Material material,float strength,float emission)
        {glow=material;glowStrength=strength;bodyEmission=emission;}
        private void Awake()=>button=GetComponent<Button>();
        internal void SetShadow(MeshRenderer value){shadow=value;shadowBlock=new MaterialPropertyBlock();}
        private void LateUpdate()
        {
            if(button!=null&&!button.IsInteractable())pressed=hovered=false;
            float next=Mathf.MoveTowards(offset,pressed?5:hovered?-3:0,Time.unscaledDeltaTime*60);
            transform.localPosition+=new Vector3(0,0,next-offset);offset=next;
            if(glow!=null)
            {
                float weight=button!=null&&!button.IsInteractable()?.25f:pressed?1.35f:hovered?1.2f:1f;
                glow.SetFloat("_DeskEdgeStrength",glowStrength*weight);
                glow.SetFloat("_SoftEmission",bodyEmission*(weight<1?.55f:1));
            }
            if(shadow!=null)
            {
                shadowBlock.SetColor("_BaseColor",new Color(.055f,.035f,.025f,Mathf.Lerp(.18f,.30f,(offset+3)/8)));
                shadow.SetPropertyBlock(shadowBlock);
            }
        }
        public void OnPointerDown(PointerEventData e){if(e.button==PointerEventData.InputButton.Left)pressed=true;}
        public void OnPointerUp(PointerEventData e)=>pressed=false;
        public void OnPointerEnter(PointerEventData e)=>hovered=true;
        public void OnPointerExit(PointerEventData e){pressed=hovered=false;}
    }
}
