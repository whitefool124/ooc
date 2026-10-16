using System;
using System.Collections.Generic;
using System.IO;
using Newtonsoft.Json;
using OCC.Combat.Presentation;
using UnityEditor;
using UnityEngine;
using UnityEngine.EventSystems;

namespace OCC.Combat.EditorTools
{
    // Timed live camera path using the runtime's legal scroll/drag handlers.
    // Records offscreen Canvas frames; does not focus/create an Editor window.
    public static class Hd2DBattleMotionCapture
    {
        private static Hd2DBattlefieldSample sample;
        private static string folder;
        private static Vector3 oldPosition;
        private static float oldFov;
        private static int resolutionIndex,index;
        private static double nextFrame;
        private static readonly List<object> frames=new List<object>();
        public static void Begin(Hd2DBattlefieldSample value,string destination)
        {
            sample=value;folder=destination;oldPosition=sample.RenderCamera.transform.position;oldFov=sample.RenderCamera.fieldOfView;
            resolutionIndex=0;index=0;frames.Clear();nextFrame=EditorApplication.timeSinceStartup;
        }
        public static bool Step()
        {
            if(sample==null||!Application.isPlaying)throw new InvalidOperationException("Live motion sample unavailable");
            if(EditorApplication.timeSinceStartup<nextFrame)return false;
            int width=resolutionIndex==0?1920:960,height=resolutionIndex==0?1080:540;
            if(index==0){sample.RenderCamera.transform.position=oldPosition;sample.RenderCamera.fieldOfView=oldFov;}
            else
            {
                var pointer=new PointerEventData(EventSystem.current){button=PointerEventData.InputButton.Left};
                float direction=index<=4?1:-1;
                if(index!=4){pointer.scrollDelta=new Vector2(0,direction*.5f);sample.OnScroll(pointer);}
                pointer.delta=new Vector2(direction*2,direction*.5f);sample.OnDrag(pointer);
            }
            string name="motion_"+width+"x"+height+"_"+index.ToString("00")+".png";
            double timestamp=EditorApplication.timeSinceStartup;
            Hd2DBattleCanvasCapture.Capture(sample,width,height,Path.Combine(folder,name));
            var camera=sample.RenderCamera;
            frames.Add(new{resolution=new[]{width,height},index,path=name,editorTimestamp=timestamp,fieldOfView=camera.fieldOfView,
                cameraPosition=new[]{camera.transform.position.x,camera.transform.position.y,camera.transform.position.z}});
            index++;nextFrame=EditorApplication.timeSinceStartup+.125;
            if(index<8)return false;
            sample.RenderCamera.transform.position=oldPosition;sample.RenderCamera.fieldOfView=oldFov;
            index=0;resolutionIndex++;
            if(resolutionIndex<2)return false;
            File.WriteAllText(Path.Combine(folder,"timed_motion_capture.json"),JsonConvert.SerializeObject(new{
                status="LOCAL_TIMED_CAMERA_PATH",scope="Timed live Editor runtime camera through legal public scroll/drag handlers; isolated preview, not normal input journey or FPS benchmark",
                requestedFrameIntervalSeconds=.125,frames,focusRequested=false,gameplayStateChanged=false,cameraRestored=true,
                visualStabilityReview="PENDING",playerPerformance="NOT_MEASURED"},Formatting.Indented));
            sample=null;return true;
        }
        public static void Restore()
        {
            if(sample!=null&&sample.RenderCamera!=null){sample.RenderCamera.transform.position=oldPosition;sample.RenderCamera.fieldOfView=oldFov;}
            sample=null;
        }
    }
}
