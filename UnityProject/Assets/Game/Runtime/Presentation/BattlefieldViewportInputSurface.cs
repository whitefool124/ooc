using UnityEngine;
using UnityEngine.EventSystems;

namespace OCC.Combat.Presentation
{
    [DisallowMultipleComponent]
    public sealed class BattlefieldViewportInputSurface : MonoBehaviour, IBeginDragHandler, IDragHandler,
        IEndDragHandler, IScrollHandler
    {
        private FormalBattlefieldView view;

        public void Initialize(FormalBattlefieldView value) => view = value;
        public void OnBeginDrag(PointerEventData eventData) => view?.BeginDrag(eventData);
        public void OnDrag(PointerEventData eventData) => view?.Drag(eventData);
        public void OnEndDrag(PointerEventData eventData) { }
        public void OnScroll(PointerEventData eventData) => view?.Scroll(eventData);
    }
}
