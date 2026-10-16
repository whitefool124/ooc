using System;
using System.IO;
using System.Linq;
using System.Reflection;
using OCC.Combat.Presentation;
using UnityEngine;
using UnityEngine.Rendering.Universal;
using UnityEngine.UI;

namespace OCC.Combat.EditorTools
{
    // Render the current runtime Canvas and battlefield at the requested output
    // size. No GameView/window is created, resized, selected or focused. This is
    // full offscreen runtime UI evidence, not a Player screenshot or input test.
    internal static class Hd2DBattleCanvasCapture
    {
        internal static void Capture(Hd2DBattlefieldSample sample,int width,int height,string path,bool refreshBattle=true)
        {
            if(!Application.isPlaying)throw new InvalidOperationException("Live runtime canvas required");
            var roots=UnityEngine.Object.FindObjectsByType<Canvas>(FindObjectsSortMode.None)
                .Where(c=>c.enabled&&c.gameObject.activeInHierarchy&&c.isRootCanvas&&c.renderMode!=RenderMode.WorldSpace).ToArray();
            if(roots.Length==0)throw new InvalidOperationException("Runtime root canvases unavailable");
            var states=roots.Select(c=>new CanvasState(c)).ToArray();
            var transforms=roots.SelectMany(c=>c.GetComponentsInChildren<Transform>(true)).Distinct().ToArray();
            var layers=transforms.Select(t=>t.gameObject.layer).ToArray();
            var go=new GameObject("Owned offscreen battle UI capture",typeof(Camera),typeof(UniversalAdditionalCameraData)){hideFlags=HideFlags.HideAndDontSave};
            var camera=go.GetComponent<Camera>();camera.enabled=false;camera.cullingMask=1<<30;
            camera.clearFlags=CameraClearFlags.SolidColor;camera.backgroundColor=new Color(.08f,.075f,.065f);
            camera.nearClipPlane=.01f;camera.farClipPlane=10;camera.allowHDR=false;
            camera.GetUniversalAdditionalCameraData().SetRenderer(0);camera.GetUniversalAdditionalCameraData().renderPostProcessing=false;
            var output=new RenderTexture(width,height,24,RenderTextureFormat.ARGB32,RenderTextureReadWrite.sRGB){hideFlags=HideFlags.HideAndDontSave};
            var image=new Texture2D(width,height,TextureFormat.RGB24,false,false);
            var active=RenderTexture.active;
            try
            {
                output.Create();camera.targetTexture=output;
                for(int i=0;i<transforms.Length;i++)transforms[i].gameObject.layer=30;
                foreach(var state in states)
                {
                    state.Canvas.renderMode=RenderMode.ScreenSpaceCamera;state.Canvas.worldCamera=camera;state.Canvas.planeDistance=1;
                    if(state.Scaler!=null){state.Scaler.uiScaleMode=CanvasScaler.ScaleMode.ConstantPixelSize;state.Scaler.scaleFactor=width/1920f;}
                    else state.Canvas.scaleFactor=width/1920f;
                }
                Rebuild();if(refreshBattle)sample?.SendMessage("LateUpdate",SendMessageOptions.DontRequireReceiver);
                if(sample!=null)sample.RenderCamera.Render();camera.Render();RenderTexture.active=output;
                image.ReadPixels(new Rect(0,0,width,height),0,0);image.Apply();
                File.WriteAllBytes(path,image.EncodeToPNG());
            }
            finally
            {
                RenderTexture.active=active;foreach(var state in states)state.Restore();
                for(int i=0;i<transforms.Length;i++)if(transforms[i]!=null)transforms[i].gameObject.layer=layers[i];
                Rebuild();if(refreshBattle)sample?.SendMessage("LateUpdate",SendMessageOptions.DontRequireReceiver);
                camera.targetTexture=null;output.Release();UnityEngine.Object.DestroyImmediate(output);
                UnityEngine.Object.DestroyImmediate(image);UnityEngine.Object.DestroyImmediate(go);
            }
            void Rebuild()
            {
                Canvas.ForceUpdateCanvases();
                foreach(var state in states)if(state.Scaler!=null)
                    typeof(CanvasScaler).GetMethod("Handle",BindingFlags.Instance|BindingFlags.NonPublic)?.Invoke(state.Scaler,null);
                Canvas.ForceUpdateCanvases();
            }
        }
        private sealed class CanvasState
        {
            internal readonly Canvas Canvas;internal readonly CanvasScaler Scaler;
            private readonly RenderMode mode;private readonly Camera camera;private readonly float distance,canvasScale,scale;
            private readonly CanvasScaler.ScaleMode scaleMode;
            internal CanvasState(Canvas canvas)
            {
                Canvas=canvas;Scaler=canvas.GetComponent<CanvasScaler>();mode=canvas.renderMode;camera=canvas.worldCamera;
                distance=canvas.planeDistance;canvasScale=canvas.scaleFactor;
                if(Scaler!=null){scale=Scaler.scaleFactor;scaleMode=Scaler.uiScaleMode;}
            }
            internal void Restore()
            {
                if(Canvas==null)return;Canvas.renderMode=mode;Canvas.worldCamera=camera;Canvas.planeDistance=distance;Canvas.scaleFactor=canvasScale;
                if(Scaler!=null){Scaler.uiScaleMode=scaleMode;Scaler.scaleFactor=scale;}
            }
        }
    }
}
