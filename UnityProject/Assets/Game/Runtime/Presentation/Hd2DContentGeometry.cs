using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.UI;

namespace OCC.Combat.Presentation
{
    // Authored layout is placed in world space once; camera motion does not
    // continuously reproject it back onto the screen.
    internal sealed class Hd2DContentGeometry : MonoBehaviour
    {
        private sealed class LayoutPose
        {
            internal RectTransform Root;internal Transform Parent;
            internal Vector2 Min,Max,Pivot,Size;internal Vector3 Position,Scale;internal Quaternion Rotation;
        }
        private readonly List<LayoutPose> layout=new List<LayoutPose>();
        private float readyAt;
        private sealed class Surface
        {
            internal Image Source;internal Mesh Mesh;internal MeshRenderer Renderer;internal Material Material;
            internal Rect Rect,ClipRect;internal Sprite Sprite;internal RectTransform Clip;
            internal Vector3 SourcePosition;
            internal MeshRenderer[] DepthLayers;
            internal Matrix4x4 ClipMatrix;internal CanvasGroup Group;
            internal Vector3[] Vertices;internal Vector2[] Uv;internal int[] Triangles;
            internal readonly List<Vector3> Output=new List<Vector3>();
            internal readonly List<Vector2> OutputUv=new List<Vector2>();
            internal readonly List<int> OutputTriangles=new List<int>();
        }
        private readonly List<Surface> surfaces=new List<Surface>();
        private readonly Dictionary<Texture,Material[]> depthMaterials=new Dictionary<Texture,Material[]>();
        private MainMenuHd2DStage stage;
        private GameObject worldRoot;
        private Hd2DServiceReadingText reading;
        private int frames;
        private bool prepared;
        internal static void Attach(GameObject page)
        {
            if(Application.isPlaying&&page.GetComponent<Hd2DContentGeometry>()==null)
                page.AddComponent<Hd2DContentGeometry>();
        }
        private void LateUpdate()
        {
            if(!prepared)
            {
                // Existing page entrance coroutines must finish before anchors move.
                if(Time.unscaledTime<readyAt)return;
                if(++frames<4)return;
                var backdrop=GetComponentInChildren<AcademyDeskBackdrop>();
                var menu=GetComponentInChildren<MainMenuDeskScene>();
                stage=backdrop!=null?backdrop.Stage:menu!=null?menu.Stage:null;
                if(stage==null)return;
                Build();prepared=true;
            }
            foreach(var s in surfaces)
            {
                var image=s.Source;if(image==null)continue;
                s.Renderer.enabled=image.isActiveAndEnabled&&(s.Group==null||s.Group.alpha>.01f);
                if(s.DepthLayers!=null)foreach(var layer in s.DepthLayers)layer.enabled=s.Renderer.enabled;
                if(!s.Renderer.enabled)continue;
                image.canvasRenderer.SetAlpha(0);
                if(s.Material.color!=image.color)s.Material.color=image.color;
                s.Material.mainTexture=image.mainTexture;
                bool changed=s.Rect!=image.rectTransform.rect||s.Sprite!=image.sprite;
                if(changed)ReadMesh(s);
                if(s.Clip!=null)
                {
                    var matrix=s.Clip.worldToLocalMatrix*image.rectTransform.localToWorldMatrix;
                    if(changed||matrix!=s.ClipMatrix||s.ClipRect!=s.Clip.rect)
                    {Clip(s,matrix);s.ClipMatrix=matrix;s.ClipRect=s.Clip.rect;}
                }
            }
        }
        private void Build()
        {
            Canvas.ForceUpdateCanvases();
            var roots=new List<RectTransform>();CollectRoots(transform,roots);
            var camera=stage.sceneCamera;
            worldRoot=new GameObject("场景阅读平面",typeof(RectTransform),typeof(Canvas),typeof(Hd2DSceneGraphicRaycaster));
            worldRoot.layer=stage.gameObject.layer;worldRoot.transform.SetParent(stage.transform,false);
            var rect=worldRoot.GetComponent<RectTransform>();var page=(RectTransform)transform;
            rect.sizeDelta=page.rect.size;
            var owner=GetComponentInParent<Canvas>();float scale=owner==null?1:owner.scaleFactor;
            var centre=RectTransformUtility.WorldToScreenPoint(null,page.TransformPoint(page.rect.center));
            const float depth=5.5f;
            rect.position=camera.ViewportToWorldPoint(new Vector3(centre.x/Screen.width,centre.y/Screen.height,depth));
            rect.rotation=camera.transform.rotation;
            float unit=2*depth*Mathf.Tan(camera.fieldOfView*Mathf.Deg2Rad*.5f)/Screen.height;
            rect.localScale=Vector3.one*(unit*scale*.95f);
            var canvas=worldRoot.GetComponent<Canvas>();canvas.renderMode=RenderMode.WorldSpace;
            canvas.worldCamera=camera;canvas.overrideSorting=true;canvas.sortingOrder=owner==null?0:owner.sortingOrder;
            foreach(var root in roots)
            {
                layout.Add(new LayoutPose{Root=root,Parent=root.parent,Min=root.anchorMin,Max=root.anchorMax,
                    Pivot=root.pivot,Position=root.anchoredPosition3D,Size=root.sizeDelta,Scale=root.localScale,Rotation=root.localRotation});
                var screen=RectTransformUtility.WorldToScreenPoint(null,root.TransformPoint(root.rect.center));
                Vector2 size=root.rect.size;Vector3 originalScale=root.lossyScale;
                root.SetParent(rect,false);root.anchorMin=root.anchorMax=root.pivot=new Vector2(.5f,.5f);
                root.sizeDelta=size;root.anchoredPosition=(screen-centre)/scale;
                root.localScale=originalScale/scale;root.localRotation=Quaternion.identity;
            }
            foreach(var t in worldRoot.GetComponentsInChildren<Transform>(true))t.gameObject.layer=stage.gameObject.layer;
            // Stencil masks cannot clip independent lit renderers. Rectangular
            // masks share the same viewport with both text and clipped meshes.
            foreach(var mask in worldRoot.GetComponentsInChildren<Mask>(true))
            {
                mask.enabled=false;
                if(mask.GetComponent<RectMask2D>()==null)mask.gameObject.AddComponent<RectMask2D>();
                if(mask.TryGetComponent<Image>(out var maskImage))maskImage.color=Color.clear;
            }
            Hd2DContainerStyle.PrepareControls(worldRoot);
            foreach(var image in worldRoot.GetComponentsInChildren<Image>(true))
            {
                var size=image.rectTransform.rect.size;
                if(image.color.a>=.85f&&size.x>=100&&size.y>=64&&(image.sprite==null||image.type==Image.Type.Sliced))
                    Hd2DContainerStyle.Tray(image);
            }
            Hd2DPaperSkin.Apply(worldRoot);
            foreach(var image in worldRoot.GetComponentsInChildren<Image>(true))
                foreach(var shadow in image.GetComponents<Shadow>())shadow.enabled=false;
            Canvas.ForceUpdateCanvases();
            var renderImages=worldRoot.GetComponentsInChildren<Image>(true);
            int surfaceOrder=0;
            foreach(var image in renderImages)
            {
                if(image.color.a<.85f||image.rectTransform.rect.width<32||image.rectTransform.rect.height<24||
                    image.GetComponent<Mask>()!=null)continue;
                var generated=image.canvasRenderer.GetMesh();if(generated==null||generated.vertexCount==0)continue;
                var sourcePosition=image.transform.localPosition;
                var node=new GameObject(image.name+" 受光面",typeof(MeshFilter),typeof(MeshRenderer));
                node.layer=stage.gameObject.layer;node.transform.SetParent(image.transform,false);
                node.transform.localPosition=new Vector3(0,0,.6f+(renderImages.Length-surfaceOrder++)*.02f);
                var mesh=Instantiate(generated);
                var material=new Material(stage.bookLitMaterial){mainTexture=image.mainTexture,color=image.color};
                material.SetFloat("_Smoothness",.08f);node.GetComponent<MeshFilter>().sharedMesh=mesh;
                var renderer=node.GetComponent<MeshRenderer>();renderer.sharedMaterial=material;
                renderer.shadowCastingMode=ShadowCastingMode.On;renderer.receiveShadows=true;
                var mask=image.GetComponentInParent<RectMask2D>();
                var s=new Surface{Source=image,SourcePosition=sourcePosition,Mesh=mesh,Material=material,Renderer=renderer,
                    Clip=mask==null?null:mask.rectTransform,Group=image.GetComponentInParent<CanvasGroup>()};
                ReadMesh(s);surfaces.Add(s);image.canvasRenderer.SetAlpha(0);
                if(s.Clip==null&&image.GetComponent<Hd2DContainerStyle>()!=null&&
                    image.rectTransform.rect.width*image.rectTransform.rect.height>6000)
                    AddDepthLayers(s);
                if(image.GetComponent<Button>()!=null)
                {
                    image.GetComponent<UiButtonFeedback>()?.UseSceneSpaceFeedback();
                    if(image.GetComponent<Hd2DSceneControl>()==null)image.gameObject.AddComponent<Hd2DSceneControl>();
                }
            }
            var go=new GameObject("中性阅读柔光",typeof(Light));go.transform.SetParent(worldRoot.transform,false);
            go.transform.rotation=camera.transform.rotation;var light=go.GetComponent<Light>();
            light.type=LightType.Directional;light.intensity=1.1f;light.color=new Color(.96f,.98f,1);
            light.shadows=LightShadows.None;light.cullingMask=1<<stage.gameObject.layer;
            var key=new GameObject("纸面左上柔光",typeof(Light));key.transform.SetParent(worldRoot.transform,false);
            key.transform.localPosition=new Vector3(-520,340,-650);
            var keyLight=key.GetComponent<Light>();keyLight.type=LightType.Point;keyLight.intensity=1.1f;
            keyLight.range=9;keyLight.color=new Color(.94f,.97f,1);keyLight.shadows=LightShadows.None;
            keyLight.cullingMask=1<<stage.gameObject.layer;
            reading=gameObject.AddComponent<Hd2DServiceReadingText>();reading.Configure(worldRoot,camera);
        }
        private void AddDepthLayers(Surface surface)
        {
            var texture=surface.Source.mainTexture;
            if(!depthMaterials.TryGetValue(texture,out var materials))
            {
                var edge=new Material(stage.bookLitMaterial){name="纸层侧沿",mainTexture=texture,color=new Color(.58f,.53f,.44f)};
                edge.enableInstancing=true;
                edge.SetFloat("_Smoothness",.04f);
                var underside=new Material(edge){name="容器下沿",color=new Color(.30f,.28f,.24f)};
                var shadowShader=Shader.Find("OCC/HD2D/ContactShadow");
                var shadow=shadowShader==null?null:new Material(shadowShader){name="容器接触投影",mainTexture=texture,color=new Color(.07f,.075f,.08f,.25f)};
                materials=new[]{edge,underside,shadow};depthMaterials.Add(texture,materials);
            }
            var layers=new List<MeshRenderer>(3);
            var offsets=new[]{new Vector3(0,-2,3),new Vector3(0,-4,6),new Vector3(6,-11,14)};
            for(int i=0;i<materials.Length;i++)
            {
                if(materials[i]==null)continue;
                var node=new GameObject(i==2?"承载层投影":"独立纸层侧沿"+i,typeof(MeshFilter),typeof(MeshRenderer));
                node.layer=stage.gameObject.layer;node.transform.SetParent(surface.Renderer.transform,false);
                node.transform.localPosition=offsets[i];node.GetComponent<MeshFilter>().sharedMesh=surface.Mesh;
                var renderer=node.GetComponent<MeshRenderer>();renderer.sharedMaterial=materials[i];
                renderer.shadowCastingMode=ShadowCastingMode.Off;renderer.receiveShadows=i!=2;layers.Add(renderer);
            }
            surface.DepthLayers=layers.ToArray();
        }
        private static void CollectRoots(Transform parent,List<RectTransform> roots)
        {
            foreach(Transform child in parent)
            {
                if(child.GetComponent<RawImage>()!=null||child.GetComponent<FormalHoverTooltip>()!=null||
                    child.GetComponent<MainMenuHd2DStage>()!=null)continue;
                if(child.GetComponent<MainMenuDeskScene>()!=null||child.GetComponentInChildren<RawImage>()!=null)
                {CollectRoots(child,roots);continue;}
                if(child is RectTransform rect)roots.Add(rect);
            }
        }
        private static void ReadMesh(Surface s)
        {
            var source=s.Source.canvasRenderer.GetMesh();if(source==null)return;
            s.Vertices=source.vertices;s.Uv=source.uv;s.Triangles=source.triangles;
            s.Mesh.Clear();s.Mesh.vertices=s.Vertices;s.Mesh.uv=s.Uv;s.Mesh.triangles=s.Triangles;
            var colors=new Color32[s.Vertices.Length];for(int i=0;i<colors.Length;i++)colors[i]=new Color32(255,255,255,255);
            s.Mesh.colors32=colors;s.Mesh.RecalculateNormals();s.Mesh.RecalculateBounds();
            s.Rect=s.Source.rectTransform.rect;s.Sprite=s.Source.sprite;
        }
        private struct Vertex {internal Vector3 Point;internal Vector2 Uv;}
        private readonly Vertex[] polygon=new Vertex[12],scratch=new Vertex[12];
        private void Clip(Surface s,Matrix4x4 matrix)
        {
            s.Output.Clear();s.OutputUv.Clear();s.OutputTriangles.Clear();var rect=s.Clip.rect;var inverse=matrix.inverse;
            for(int t=0;t<s.Triangles.Length;t+=3)
            {
                for(int i=0;i<3;i++){int index=s.Triangles[t+i];polygon[i]=new Vertex{Point=matrix.MultiplyPoint3x4(s.Vertices[index]),Uv=s.Uv[index]};}
                int count=3;
                for(int edge=0;edge<4&&count>0;edge++)
                {
                    int output=0;float boundary=edge==0?rect.xMin:edge==1?rect.xMax:edge==2?rect.yMin:rect.yMax;
                    for(int i=0;i<count;i++)
                    {
                        var a=polygon[i];var b=polygon[(i+1)%count];float av=edge<2?a.Point.x:a.Point.y,bv=edge<2?b.Point.x:b.Point.y;
                        bool ai=edge%2==0?av>=boundary:av<=boundary,bi=edge%2==0?bv>=boundary:bv<=boundary;
                        if(ai)scratch[output++]=a;
                        if(ai!=bi){float u=(boundary-av)/(bv-av);scratch[output++]=new Vertex{Point=Vector3.Lerp(a.Point,b.Point,u),Uv=Vector2.Lerp(a.Uv,b.Uv,u)};}
                    }
                    count=output;for(int i=0;i<count;i++)polygon[i]=scratch[i];
                }
                int start=s.Output.Count;
                for(int i=0;i<count;i++){s.Output.Add(inverse.MultiplyPoint3x4(polygon[i].Point));s.OutputUv.Add(polygon[i].Uv);}
                for(int i=1;i+1<count;i++){s.OutputTriangles.Add(start);s.OutputTriangles.Add(start+i);s.OutputTriangles.Add(start+i+1);}
            }
            s.Mesh.Clear();s.Mesh.SetVertices(s.Output);s.Mesh.SetUVs(0,s.OutputUv);s.Mesh.SetTriangles(s.OutputTriangles,0);
            s.Mesh.RecalculateNormals();s.Mesh.RecalculateBounds();
        }
        internal void RestoreLayout()=>Release();
        internal static void PrepareToHide(GameObject root)
        {
            if(root==null)return;
            foreach(var service in root.GetComponentsInChildren<Hd2DServiceScene>(true))service.RestoreLayout();
            foreach(var geometry in root.GetComponentsInChildren<Hd2DContentGeometry>(true))geometry.Release();
        }
        private void OnDisable(){}
        private void OnEnable(){readyAt=Time.unscaledTime+.42f;}
        private void OnDestroy()=>Release();
        private void Release()
        {
            if(reading!=null){reading.Restore();Destroy(reading);reading=null;}
            bool restore=true;
#if UNITY_EDITOR
            // Exiting Play Mode already destroys the complete transient scene.
            // Reparenting during that teardown is forbidden by Unity.
            restore=UnityEditor.EditorApplication.isPlayingOrWillChangePlaymode;
#endif
            foreach(var surface in surfaces)
                if(surface.Source!=null)surface.Source.transform.localPosition=surface.SourcePosition;
            foreach(var pose in layout)
            {
                if(!restore)continue;
                var r=pose.Root;if(r==null)continue;
                if(pose.Parent==null){Destroy(r.gameObject);continue;}
                r.SetParent(pose.Parent,false);r.anchorMin=pose.Min;r.anchorMax=pose.Max;r.pivot=pose.Pivot;
                r.anchoredPosition3D=pose.Position;r.sizeDelta=pose.Size;r.localScale=pose.Scale;r.localRotation=pose.Rotation;
            }
            foreach(var s in surfaces)
            {
                if(s.Source!=null)s.Source.canvasRenderer.SetAlpha(1);
                if(s.Renderer!=null)Destroy(s.Renderer.gameObject);
                if(s.Mesh!=null)Destroy(s.Mesh);if(s.Material!=null)Destroy(s.Material);
            }
            if(worldRoot!=null)Destroy(worldRoot);
            foreach(var materials in depthMaterials.Values)foreach(var material in materials)if(material!=null)Destroy(material);
            depthMaterials.Clear();
            worldRoot=null;surfaces.Clear();layout.Clear();prepared=false;frames=0;
        }
    }
}
