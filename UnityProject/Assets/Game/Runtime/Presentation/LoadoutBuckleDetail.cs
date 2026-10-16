using System;
using UnityEngine;
using UnityEngine.UI;

namespace OCC.Combat.Presentation
{
    // Small runtime UI hardware; no generated art or inventory state is owned here.
    public sealed class LoadoutBuckleDetail : MaskableGraphic
    {
        public bool Closed { get; private set; }
        public bool IsAnimating => started >= 0;
        private Func<float> intensity;
        private float started = -1;
        public static LoadoutBuckleDetail Attach(RectTransform slot, bool closed, Func<float> motion)
        {
            var go = new GameObject("装备扣件", typeof(RectTransform), typeof(CanvasRenderer));
            go.transform.SetParent(slot, false);
            var rect = (RectTransform)go.transform;
            rect.anchorMin = rect.anchorMax = new Vector2(1, 0);
            rect.pivot = new Vector2(.5f, .5f); rect.anchoredPosition = new Vector2(-12, 10); rect.sizeDelta = new Vector2(18, 14);
            var detail = go.AddComponent<LoadoutBuckleDetail>();
            detail.Closed = closed; detail.intensity = motion; detail.color = FormalUiTheme.Amber; detail.raycastTarget = false;
            return detail;
        }
        public void PlayCommitted() { if ((intensity?.Invoke() ?? 0) > .01f) started = Time.unscaledTime; }
        private void Update()
        {
            if (started < 0) return;
            if ((intensity?.Invoke() ?? 0) <= .01f || Time.unscaledTime - started >= .28f) started = -1;
            SetVerticesDirty();
        }
        protected override void OnDisable() { started = -1; base.OnDisable(); }
        protected override void OnPopulateMesh(VertexHelper mesh)
        {
            mesh.Clear();
            float finalGap = Closed ? 0 : 4;
            float t = started < 0 ? 1 : Mathf.Clamp01((Time.unscaledTime - started) / .28f);
            float gap = Mathf.Round(Mathf.Lerp(Closed ? 4 : 0, finalGap, Mathf.SmoothStep(0, 1, t)));
            Quad(mesh, -6-gap/2, -3, 4, 6, FormalUiTheme.Ink);
            Quad(mesh, 2+gap/2, -3, 4, 6, FormalUiTheme.Ink);
            Quad(mesh, -5-gap/2, -2, 3, 4, color);
            Quad(mesh, 2+gap/2, -2, 3, 4, color);
            if (gap <= 1) Quad(mesh, -2, -1, 4, 2, color);
        }
        internal static void Quad(VertexHelper mesh, float x, float y, float w, float h, Color shade)
        {
            int start = mesh.currentVertCount;
            mesh.AddVert(new Vector3(x,y),shade,Vector2.zero); mesh.AddVert(new Vector3(x+w,y),shade,Vector2.zero);
            mesh.AddVert(new Vector3(x+w,y+h),shade,Vector2.zero); mesh.AddVert(new Vector3(x,y+h),shade,Vector2.zero);
            mesh.AddTriangle(start,start+1,start+2); mesh.AddTriangle(start,start+2,start+3);
        }
    }
}
