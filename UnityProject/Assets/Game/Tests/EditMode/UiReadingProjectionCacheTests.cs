using System;
using System.Reflection;
using NUnit.Framework;
using OCC.Combat.Presentation;
using UnityEngine;
using UnityEngine.UI;

namespace OCC.Tests
{
    public sealed class UiReadingProjectionCacheTests
    {
        [Test]
        public void TextCameraViewportAndScrollMaskChangesInvalidateCachedProjection()
        {
            var root=new GameObject("reading cache test",typeof(RectTransform),typeof(RectMask2D));
            var node=new GameObject("source",typeof(RectTransform),typeof(CanvasRenderer),typeof(Text));
            node.transform.SetParent(root.transform,false);
            var source=node.GetComponent<Text>();source.text="50/50";
            var type=typeof(Hd2DServiceReadingText).Assembly.GetType("OCC.Combat.Presentation.Hd2DReadingProjectionCache");
            var cache=Activator.CreateInstance(type,true);
            var method=type.GetMethod("NeedsRefresh",BindingFlags.Instance|BindingFlags.NonPublic);
            Matrix4x4 projection=Matrix4x4.identity;
            var viewport=new Rect(0,0,1920,1080);
            var masks=new[]{root.GetComponent<RectMask2D>()};
            bool Refresh()=> (bool)method.Invoke(cache,new object[]{source,projection,viewport,viewport,1f,masks});
            void Changed(){Assert.That(Refresh(),Is.True);Assert.That(Refresh(),Is.False);}
            try
            {
                Changed();source.text="49/50";Changed();
                source.color=Color.red;Changed();
                source.rectTransform.anchoredPosition=new Vector2(12,24);Changed();
                projection=Matrix4x4.Translate(Vector3.right);Changed();
                viewport=new Rect(0,0,960,540);Changed();
                ((RectTransform)root.transform).sizeDelta=new Vector2(128,64);Changed();
                masks[0].enabled=false;Changed();
            }
            finally{UnityEngine.Object.DestroyImmediate(root);}
        }
    }
}
