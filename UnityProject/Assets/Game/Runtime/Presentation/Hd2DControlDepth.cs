using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace OCC.Combat.Presentation
{
    internal sealed class Hd2DControlDepth : MonoBehaviour,IPointerDownHandler,IPointerUpHandler,
        IPointerEnterHandler,IPointerExitHandler,ISelectHandler,IDeselectHandler
    {
        private bool pressed,hovered,selected;
        internal float Offset {get;private set;}
        private Selectable selectable;
        private void Awake()=>selectable=GetComponent<Selectable>();
        private void Update()
        {
            if(selectable!=null&&!selectable.IsInteractable()){pressed=hovered=selected=false;}
            Offset=Mathf.MoveTowards(Offset,pressed?.012f:hovered||selected?-.009f:0,Time.unscaledDeltaTime*.5f);
        }
        public void OnPointerDown(PointerEventData e){if(e.button==PointerEventData.InputButton.Left)pressed=true;}
        public void OnPointerUp(PointerEventData e)=>pressed=false;
        public void OnPointerEnter(PointerEventData e)=>hovered=true;
        public void OnPointerExit(PointerEventData e){hovered=false;pressed=false;}
        public void OnSelect(BaseEventData e)=>selected=true;
        public void OnDeselect(BaseEventData e){selected=false;pressed=false;}
        private void OnDisable(){pressed=hovered=selected=false;Offset=0;}
    }
}
