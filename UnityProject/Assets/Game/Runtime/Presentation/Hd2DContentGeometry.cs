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
        private sealed class Surface
        {
            internal Image Source;internal Mesh Mesh;internal MeshRenderer Renderer;internal Material Material;
            internal Rect Rect,ClipRect;internal Sprite Sprite;internal RectTransform Clip;
            internal RectTransform[] Clips;
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
        private bool prepared;
        private Hd2DImageMeshBuilder meshBuilder;
        private Mesh BuildUnculledMesh(Image image)
        {
            if(meshBuilder==null)
            {
                var node=new GameObject("受光网格构建器",typeof(RectTransform),typeof(CanvasRenderer));
                node.transform.SetParent(worldRoot.transform,false);
                meshBuilder=node.AddComponent<Hd2DImageMeshBuilder>();meshBuilder.enabled=false;
            }
            return meshBuilder.Build(image);
        }
        internal static void Attach(GameObject page)
        {
            if(Application.isPlaying&&page.GetComponent<Hd2DContentGeometry>()==null)
            {
                Hd2DPageVisibility.Begin(page);
                page.AddComponent<Hd2DContentGeometry>();
            }
        }
        private void LateUpdate()
        {
            if(!prepared)
            {
                // Build before the first rendered frame. Screen-space entrance motion
                // must not reveal a second layout before the scene owns its controls.
                var backdrop=GetComponentInChildren<AcademyDeskBackdrop>();
                var menu=GetComponentInChildren<MainMenuDeskScene>();
                stage=backdrop!=null?backdrop.Stage:menu!=null?menu.Stage:null;
                if(stage==null)return;
                Build();prepared=true;
                Hd2DPageVisibility.Complete(gameObject);
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
                    var clipRect=EffectiveClip(s);
                    if(changed||matrix!=s.ClipMatrix||s.ClipRect!=clipRect)
                    {Clip(s,matrix,clipRect);s.ClipMatrix=matrix;s.ClipRect=clipRect;}
                }
            }
        }
        private void Build()
        {
            Canvas.ForceUpdateCanvases();
            var mapShell=GetComponentInChildren<FormalRogueliteMapShellView>();
            // Capture before reparenting: roots can leave the shell hierarchy.
            var chart=mapShell==null?null:mapShell.MapViewport;
            bool giftSample=GetComponentInChildren<LampkeeperGiftView>(true)!=null;
            var roots=new List<RectTransform>();CollectRoots(transform,roots);
            var camera=stage.sceneCamera;
            var template=Resources.Load<GameObject>(Hd2DPageStructure.ResourceRoot+"ReadingPlane");
            if(template==null)throw new System.InvalidOperationException("Missing HD2D ReadingPlane prefab");
            worldRoot=Instantiate(template);worldRoot.name="场景阅读平面";
            worldRoot.layer=stage.gameObject.layer;worldRoot.transform.SetParent(stage.transform,false);
            var rect=worldRoot.GetComponent<RectTransform>();var page=(RectTransform)transform;
            rect.sizeDelta=page.rect.size;
            var owner=GetComponentInParent<Canvas>();float scale=owner==null?1:owner.scaleFactor;
            var centre=RectTransformUtility.WorldToScreenPoint(null,page.TransformPoint(page.rect.center));
            float depth=GetComponentInChildren<FormalRogueliteAuxiliaryShellView>()!=null?7f:9.5f;
            var displayRect=stage.DisplayRect;
            rect.position=camera.ViewportToWorldPoint(new Vector3((centre.x-displayRect.x)/displayRect.width,(centre.y-displayRect.y)/displayRect.height,depth));
            rect.rotation=camera.transform.rotation;
            float unit=2*depth*Mathf.Tan(camera.fieldOfView*Mathf.Deg2Rad*.5f)/displayRect.height;
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
            OccShopShelfSurface.FitNative(worldRoot);
            foreach(var image in worldRoot.GetComponentsInChildren<Image>(true))
            {
                if(chart!=null&&image.transform.IsChildOf(chart))continue;
                if(image.name=="正式皮肤")continue;
                var size=image.rectTransform.rect.size;
                bool document=size.x>=100&&size.y>=64;
                bool headingStrip=size.x>=600&&size.y>=40;
                if(image.color.a>=.85f&&(document||headingStrip)&&(image.sprite==null||image.type==Image.Type.Sliced))
                    Hd2DContainerStyle.Tray(image);
            }
            Hd2DPaperSkin.Apply(worldRoot,true,chart);
            if(giftSample)AetherPaperChrome.ApplyGift(worldRoot);
            AetherWorkshopServiceArt.Apply(worldRoot,giftSample);
            foreach(var image in worldRoot.GetComponentsInChildren<Image>(true))
                foreach(var shadow in image.GetComponents<Shadow>())shadow.enabled=false;
            Canvas.ForceUpdateCanvases();
            var renderImages=worldRoot.GetComponentsInChildren<Image>(true);
            int surfaceOrder=0;
            foreach(var image in renderImages)
            {
                // The chart is one ordered UI layer: routes and translucent node
                // shadows must stay behind their markers. Splitting only opaque
                // images into meshes makes the remaining Canvas shadows cover them.
                if(chart!=null&&image.transform.IsChildOf(chart))continue;
                if(image.color.a<.85f||image.rectTransform.rect.width<32||image.rectTransform.rect.height<24||
                    image.GetComponent<Mask>()!=null)continue;
                var generated=image.canvasRenderer.GetMesh();
                var sourcePosition=image.transform.localPosition;
                var node=Hd2DPageStructure.CreateSurface(image.transform,image.name+" 受光面");
                node.layer=stage.gameObject.layer;node.transform.SetParent(image.transform,false);
                node.transform.localPosition=new Vector3(0,0,.6f+(renderImages.Length-surfaceOrder++)*.02f);
                var mesh=generated!=null&&generated.vertexCount>0?Instantiate(generated):BuildUnculledMesh(image);
                var material=Hd2DSurfaceProfile.Create(image.mainTexture,image.color,Hd2DSurfaceRole.Paper,true);
                material.SetFloat("_Smoothness",.08f);node.GetComponent<MeshFilter>().sharedMesh=mesh;
                Hd2DDeskReadability.Current.Apply(material,image);
                var renderer=node.GetComponent<MeshRenderer>();renderer.sharedMaterial=material;
                renderer.shadowCastingMode=ShadowCastingMode.On;renderer.receiveShadows=true;
                var mask=image.GetComponentInParent<RectMask2D>();
                var s=new Surface{Source=image,SourcePosition=sourcePosition,Mesh=mesh,Material=material,Renderer=renderer,
                    Clip=mask==null?null:mask.rectTransform,Clips=System.Array.ConvertAll(image.GetComponentsInParent<RectMask2D>(),m=>m.rectTransform),Group=image.GetComponentInParent<CanvasGroup>()};
                ReadMesh(s);surfaces.Add(s);image.canvasRenderer.SetAlpha(0);
                // The shared border already carries the paper edge. Repeating the
                // whole sliced sheet behind every control makes stacked corner steps.
                if(image.GetComponent<Button>()!=null)
                {
                    image.GetComponent<UiButtonFeedback>()?.UseSceneSpaceFeedback();
                    if(image.GetComponent<Hd2DSceneControl>()==null)image.gameObject.AddComponent<Hd2DSceneControl>();
                }
            }
            foreach(var light in worldRoot.GetComponentsInChildren<Light>(true))light.cullingMask=1<<stage.gameObject.layer;
            stage.Lighting.RefreshEnvironment(stage);
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
                var node=Hd2DPageStructure.CreateSurface(surface.Renderer.transform,i==2?"承载层投影":"独立纸层侧沿"+i);
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
        private void ReadMesh(Surface s)
        {
            var source=s.Source.canvasRenderer.GetMesh();
            bool temporary=source==null||source.vertexCount==0;
            if(temporary)source=BuildUnculledMesh(s.Source);
            s.Vertices=source.vertices;s.Uv=source.uv;s.Triangles=source.triangles;
            if(temporary)Destroy(source);
            s.Mesh.Clear();s.Mesh.vertices=s.Vertices;s.Mesh.uv=s.Uv;s.Mesh.triangles=s.Triangles;
            var colors=new Color32[s.Vertices.Length];for(int i=0;i<colors.Length;i++)colors[i]=new Color32(255,255,255,255);
            s.Mesh.colors32=colors;s.Mesh.RecalculateNormals();s.Mesh.RecalculateBounds();
            s.Rect=s.Source.rectTransform.rect;s.Sprite=s.Source.sprite;
        }
        private struct Vertex {internal Vector3 Point;internal Vector2 Uv;}
        private readonly Vertex[] polygon=new Vertex[12],scratch=new Vertex[12];
        private readonly Vector3[] clipCorners=new Vector3[4];
        private Rect EffectiveClip(Surface s)
        {
            var rect=s.Clip.rect;
            foreach(var mask in s.Clips)
            {
                if(mask==null||mask==s.Clip)continue;
                mask.GetWorldCorners(clipCorners);
                var min=Vector2.positiveInfinity;var max=Vector2.negativeInfinity;
                foreach(var corner in clipCorners)
                {var p=(Vector2)s.Clip.InverseTransformPoint(corner);min=Vector2.Min(min,p);max=Vector2.Max(max,p);}
                rect=Rect.MinMaxRect(Mathf.Max(rect.xMin,min.x),Mathf.Max(rect.yMin,min.y),Mathf.Min(rect.xMax,max.x),Mathf.Min(rect.yMax,max.y));
            }
            return rect;
        }
        private void Clip(Surface s,Matrix4x4 matrix,Rect rect)
        {
            s.Output.Clear();s.OutputUv.Clear();s.OutputTriangles.Clear();var inverse=matrix.inverse;
            if(rect.width<=0||rect.height<=0){s.Mesh.Clear();return;}
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
        internal void RebuildContents()
        {
            if(!prepared)return;
            Hd2DPageVisibility.Begin(gameObject);
            Release();
        }
        internal static void PrepareToHide(GameObject root)
        {
            if(root==null)return;
            foreach(var backdrop in root.GetComponentsInChildren<AcademyDeskBackdrop>(true))backdrop.SetStageVisible(false);
        }
        internal static bool OwnsSceneLayout(GameObject page) => page != null &&
            (page.GetComponent<Hd2DContentGeometry>() != null || page.GetComponent<Hd2DServiceScene>() != null ||
             page.GetComponent<Hd2DLoadoutScene>() != null);
        private void HideSourceGraphics()
        {
            foreach(var surface in surfaces)
                if(surface.Source!=null)surface.Source.canvasRenderer.SetAlpha(0);
        }
        private void OnDisable(){Canvas.willRenderCanvases-=HideSourceGraphics;}
        private void OnEnable(){Canvas.willRenderCanvases+=HideSourceGraphics;}
        private void OnDestroy()=>Release(false);
        private void Release(bool restoreLayout=true)
        {
            if(reading!=null){reading.Restore();Destroy(reading);reading=null;}
            bool restore=restoreLayout;
#if UNITY_EDITOR
            // Exiting Play Mode already destroys the complete transient scene.
            // Reparenting during that teardown is forbidden by Unity.
            restore=restore&&UnityEditor.EditorApplication.isPlayingOrWillChangePlaymode;
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
            worldRoot=null;surfaces.Clear();layout.Clear();prepared=false;
        }
    }
}
