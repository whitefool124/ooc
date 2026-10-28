using System;
using UnityEngine;
using UnityEngine.UI;

namespace OCC.Combat.Presentation
{
    public enum UiChromeRole { Panel, Card, Button, Tab, Slot, Tooltip, Notice, Focus }

    // One existing, native paper border supplies the fixed corners of every role.
    // Semantic state belongs to colour/focus layers, never to a stretched bitmap.
    public static class FormalUiChrome
    {
        public const string ResourcePath="Art/Hd2DContentFrames/简洁纸边";
        private static Sprite frame;
        public static Sprite Frame
        {
            get
            {
                if(frame==null)frame=Resources.Load<Sprite>(ResourcePath);
                if(frame==null||frame.border.sqrMagnitude==0)throw new InvalidOperationException("Missing nine-sliced paper border.");
                return frame;
            }
        }
        public static int Inset(UiChromeRole role)=>role==UiChromeRole.Focus?0:role==UiChromeRole.Slot?4:
            role==UiChromeRole.Button||role==UiChromeRole.Tab?8:role==UiChromeRole.Panel||role==UiChromeRole.Notice?16:12;
        public static UiChromeRole Role(string id)
        {
            if(id=="focus")return UiChromeRole.Focus;
            if(id.StartsWith("button",StringComparison.Ordinal))return UiChromeRole.Button;
            if(id.StartsWith("tab",StringComparison.Ordinal))return UiChromeRole.Tab;
            if(id.StartsWith("slot",StringComparison.Ordinal)||id.StartsWith("group",StringComparison.Ordinal))return UiChromeRole.Slot;
            if(id=="danger"||id=="reward")return UiChromeRole.Notice;
            if(id=="tooltip")return UiChromeRole.Tooltip;
            return id=="panel_elevated"?UiChromeRole.Card:UiChromeRole.Panel;
        }
        public static void Apply(Image image,UiChromeRole role,bool fill=true)
        {
            if(image==null)return;
            image.sprite=Frame;image.type=Image.Type.Sliced;image.fillCenter=fill;image.preserveAspect=false;
            var canvas=image.GetComponentInParent<Canvas>();float reference=canvas==null?100:canvas.referencePixelsPerUnit;
            image.pixelsPerUnitMultiplier=canvas!=null&&canvas.renderMode==RenderMode.WorldSpace?
                reference*Mathf.Max(.00001f,Mathf.Abs(image.transform.lossyScale.x))*Hd2DDeskArtCandidate.ScenePixelsPerUnit/Frame.pixelsPerUnit:
                reference/(Frame.pixelsPerUnit*2f);
        }
    }
}
