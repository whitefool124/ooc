using System;
using System.Linq;
using System.Reflection;
using NUnit.Framework;
using OCC.Combat.Presentation;
using UnityEngine;
using UnityEngine.UI;
using Object=UnityEngine.Object;

namespace OCC.Combat.Tests
{
    public sealed class UiChromeSystemTests
    {
        [TestCase(UiChromeRole.Panel)] [TestCase(UiChromeRole.Card)]
        [TestCase(UiChromeRole.Button)] [TestCase(UiChromeRole.Tab)]
        [TestCase(UiChromeRole.Slot)] [TestCase(UiChromeRole.Tooltip)]
        [TestCase(UiChromeRole.Notice)] [TestCase(UiChromeRole.Focus)]
        public void EveryRoleUsesTheExistingBorderWithoutChangingInputOwnership(UiChromeRole role)
        {
            var root=new GameObject("frame-role",typeof(RectTransform),typeof(Image));
            try
            {
                var image=root.GetComponent<Image>();image.raycastTarget=true;
                FormalUiChrome.Apply(image,role);
                Assert.That(image.sprite,Is.SameAs(FormalUiChrome.Frame));
                Assert.That(image.type,Is.EqualTo(Image.Type.Sliced));
                Assert.That(image.sprite.border,Is.EqualTo(new Vector4(4,4,4,4)));
                Assert.That(image.raycastTarget,Is.True);
            }
            finally{Object.DestroyImmediate(root);}
        }
        [TestCase(64,48)] [TestCase(640,240)] [TestCase(1600,800)]
        public void NineSliceGeometryKeepsEightReferencePixelCornersAsPanelGrows(int width,int height)
        {
            var root=new GameObject("frame-canvas",typeof(RectTransform),typeof(Canvas));
            var item=new GameObject("frame",typeof(RectTransform),typeof(Image));item.transform.SetParent(root.transform,false);
            var mesh=new Mesh();
            try
            {
                root.GetComponent<Canvas>().renderMode=RenderMode.ScreenSpaceOverlay;
                var image=item.GetComponent<Image>();image.rectTransform.sizeDelta=new Vector2(width,height);
                FormalUiChrome.Apply(image,UiChromeRole.Panel);
                using(var vertices=new VertexHelper())
                {
                    typeof(Image).GetMethod("OnPopulateMesh",BindingFlags.Instance|BindingFlags.NonPublic,null,new[]{typeof(VertexHelper)},null).Invoke(image,new object[]{vertices});
                    vertices.FillMesh(mesh);
                }
                float left=image.rectTransform.rect.xMin;
                var xs=mesh.vertices.Select(v=>v.x-left).Distinct().ToArray();
                Assert.That(xs.Any(x=>Math.Abs(x-8)<.01f),Is.True,"left corner grew with panel");
                Assert.That(xs.Any(x=>Math.Abs(x-(width-8))<.01f),Is.True,"right corner grew with panel");
            }
            finally{Object.DestroyImmediate(mesh);Object.DestroyImmediate(root);}
        }
        [Test]
        public void DecorativeFrameDoesNotBlockTheUnderlyingButton()
        {
            var root=new GameObject("button",typeof(RectTransform),typeof(Image),typeof(Button));
            try
            {
                var image=root.GetComponent<Image>();image.rectTransform.sizeDelta=new Vector2(160,48);
                FormalUiKit.ApplySkin(image,"button_idle",Color.white);
                var frame=FormalUiKit.SkinOverlay(image);
                Assert.That(frame.raycastTarget,Is.False);Assert.That(image.raycastTarget,Is.True);
                Assert.That(frame.fillCenter,Is.False);
            }
            finally{Object.DestroyImmediate(root);}
        }
        [TestCase(32,true,1080f,1f,48)]
        [TestCase(16,false,1080f,1f,24)]
        [TestCase(32,true,540f,.5f,48)]
        [TestCase(24,false,540f,.5f,24)]
        public void ReadingLayerPreservesHeadingHierarchyAndNeverShrinksBodyType(int original,bool heading,float height,float scale,int expected)
        {
            var root=new GameObject("reading-test",typeof(RectTransform));
            var source=new GameObject("source",typeof(RectTransform),typeof(Text));source.transform.SetParent(root.transform);
            var display=new GameObject("display",typeof(RectTransform),typeof(Text));display.transform.SetParent(root.transform);
            try
            {
                var a=source.GetComponent<Text>();var b=display.GetComponent<Text>();
                a.font=b.font=FormalUiKit.Font;a.fontSize=original;a.text="标题与正文";
                a.fontStyle=FontStyle.Bold;a.resizeTextForBestFit=true;
                var type=typeof(FormalUiKit).Assembly.GetType("OCC.Combat.Presentation.Hd2DReadingLayout");
                type.GetMethod("Fit",BindingFlags.NonPublic|BindingFlags.Static).Invoke(null,new object[]{b,a,new Vector2(800,256),heading,height,scale});
                Assert.That(b.fontSize,Is.EqualTo(expected));Assert.That(b.resizeTextForBestFit,Is.False);
                Assert.That(b.fontStyle,Is.EqualTo(FontStyle.Normal));
            }
            finally{Object.DestroyImmediate(root);}
        }
    }
}
