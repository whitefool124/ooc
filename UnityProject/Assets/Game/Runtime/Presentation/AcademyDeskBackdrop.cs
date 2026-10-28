using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

namespace OCC.Combat.Presentation
{
    // Shared HD2D room direction, isolated lights and volumes for simultaneous pages.
    public sealed class AcademyDeskBackdrop : MonoBehaviour
    {
        private MainMenuHd2DStage stage;
        private RawImage plate;
        private int isolatedLayer=-1;
        private bool book;
        private float frameOffset;
        private float frameFocus = -1f;
        private float readingCameraDistanceOffset;
        internal MainMenuHd2DStage Stage => stage;
        internal void SetReadingCameraDistanceOffset(float distance)
        {
            readingCameraDistanceOffset=Mathf.Max(0,distance);
            Update();
        }
        internal bool IsReleasingStage { get; private set; }
        public void SetDocumentFraming(float horizontalOffset, float focus)
        {
            frameOffset = horizontalOffset;
            frameFocus = Mathf.Clamp01(focus);
            // Projection must use the final camera pose in this same frame.
            Update();
        }
        private static readonly HashSet<int> occupiedLayers=new HashSet<int>();
        public static void Attach(Image image,bool openBook)
        {
            image.sprite=null;image.color=Color.clear;image.raycastTarget=false;
            var owner=image.gameObject.AddComponent<AcademyDeskBackdrop>();owner.book=openBook;
            var go=new GameObject("共用HD2D书桌场景",typeof(RectTransform),typeof(CanvasRenderer),typeof(RawImage));
            go.transform.SetParent(image.transform,false);go.transform.SetAsFirstSibling();
            FormalUiKit.Stretch(go.GetComponent<RectTransform>());owner.plate=go.GetComponent<RawImage>();owner.plate.raycastTarget=false;
            if(owner.isActiveAndEnabled)owner.EnsureStage();
        }
        private void EnsureStage()
        {
            if(!Application.isPlaying||stage!=null||plate==null)return;
            var prefab=Resources.Load<MainMenuHd2DStage>("UI/MainMenuEHD2DStage");
            if(prefab==null){Debug.LogError("Missing HD2D desk stage");return;}
            // Main menu owns 31; academy map owns 30. Visible pages own 16..29.
            for(int layer=29;layer>=16;layer--)
                if(occupiedLayers.Add(layer)){isolatedLayer=layer;break;}
            if(isolatedLayer<0){Debug.LogError("HD2D backdrop layer capacity exceeded");return;}
            // Reuse the isolated layer's bounded slot. A monotonic page counter
            // eventually put tiny UI transforms kilometres from the origin and
            // lost enough float precision to shift drag/drop coordinates.
            stage=Instantiate(prefab,new Vector3(500+64*(29-isolatedLayer),500,500),Quaternion.identity);
            stage.Initialize(isolatedLayer);stage.BindDisplay(plate);plate.texture=stage.Output;plate.material=null;
            plate.color=Color.white;Update();
        }
        private void Update()
        {
            if(stage!=null)
            {
                stage.SetState(book?1:0,frameFocus>=0 ? frameFocus : book?.35f:0,Time.unscaledTime,.25f);
                stage.sceneCamera.transform.localPosition += new Vector3(frameOffset, 0, 0);
                stage.sceneCamera.transform.position-=stage.sceneCamera.transform.forward*readingCameraDistanceOffset;
            }
        }
        internal void SetStageVisible(bool visible)
        {
            if(stage!=null)stage.gameObject.SetActive(visible);
        }
        private void OnEnable(){EnsureStage();SetStageVisible(true);}
        private void OnDisable()=>SetStageVisible(false);
        private void OnDestroy()=>ReleaseStage();
        private void ReleaseStage()
        {
            IsReleasingStage=true;
            if(stage!=null){stage.gameObject.SetActive(false);Destroy(stage.gameObject);stage=null;}
            if(plate!=null)plate.texture=null;
            if(isolatedLayer>=0){occupiedLayers.Remove(isolatedLayer);isolatedLayer=-1;}
        }
    }
}
