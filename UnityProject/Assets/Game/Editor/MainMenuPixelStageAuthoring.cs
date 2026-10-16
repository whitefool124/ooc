using System;
using System.Linq;
using UnityEditor;
using UnityEngine;
using UnityEngine.Rendering.Universal;
using OCC.Combat.Presentation;

namespace OCC.EditorTools
{
    public static class MainMenuPixelStageAuthoring
    {
        const string Folder="Assets/Game/Resources/Art/MainMenuE/Stage/";
        [Serializable] class Layer{public string name;public int x,y,width,height,order;public bool lit;}
        [Serializable] class Composition{public Layer[] layers;}
        static Sprite Import(string name)
        {
            string path=Folder+name+".png";AssetDatabase.ImportAsset(path,ImportAssetOptions.ForceSynchronousImport);
            var importer=(TextureImporter)AssetImporter.GetAtPath(path);
            importer.textureType=TextureImporterType.Sprite;importer.spriteImportMode=SpriteImportMode.Single;
            importer.spritePixelsPerUnit=32;
            var settings=new TextureImporterSettings();importer.ReadTextureSettings(settings);
            settings.spriteMeshType=SpriteMeshType.FullRect;importer.SetTextureSettings(settings);
            importer.filterMode=FilterMode.Point;importer.mipmapEnabled=false;importer.textureCompression=TextureImporterCompression.Uncompressed;
            importer.npotScale=TextureImporterNPOTScale.None;importer.alphaSource=TextureImporterAlphaSource.FromInput;
            importer.alphaIsTransparency=false;importer.maxTextureSize=512;
            string normalPath=Folder+name+"_normal.png";
            if(System.IO.File.Exists(normalPath))
            {
                AssetDatabase.ImportAsset(normalPath,ImportAssetOptions.ForceSynchronousImport);
                var normal=(TextureImporter)AssetImporter.GetAtPath(normalPath);normal.textureType=TextureImporterType.Default;
                normal.sRGBTexture=false;normal.filterMode=FilterMode.Point;normal.mipmapEnabled=false;
                normal.textureCompression=TextureImporterCompression.Uncompressed;normal.npotScale=TextureImporterNPOTScale.None;normal.SaveAndReimport();
                importer.secondarySpriteTextures=new[]{new SecondarySpriteTexture{name="_NormalMap",texture=AssetDatabase.LoadAssetAtPath<Texture2D>(normalPath)}};
            }
            importer.SaveAndReimport();return AssetDatabase.LoadAssetAtPath<Sprite>(path);
        }
        static SpriteRenderer Prop(Transform root,string name,Sprite sprite,Vector2 centre,int layer,Material material)
        {
            var go=new GameObject(name){layer=31};go.transform.SetParent(root,false);go.transform.localPosition=new Vector3(centre.x/32,centre.y/32,0);
            var sr=go.AddComponent<SpriteRenderer>();sr.sprite=sprite;sr.sharedMaterial=material;sr.sortingLayerID=layer;return sr;
        }
        static Light2D Light(Transform root,string name,Vector2 position,float radius,float intensity,Color color,bool normals,int[] layers)
        {
            var go=new GameObject(name){layer=31};go.transform.SetParent(root,false);go.transform.localPosition=new Vector3(position.x/32,position.y/32,-1);
            var light=go.AddComponent<Light2D>();light.lightType=Light2D.LightType.Point;light.pointLightInnerRadius=radius*.75f;
            light.pointLightOuterRadius=radius;light.intensity=intensity;light.color=color;light.shadowIntensity=0;
            var so=new SerializedObject(light);so.FindProperty("m_NormalMapQuality").intValue=normals?1:2;
            so.FindProperty("m_NormalMapDistance").floatValue=1;
            var targets=so.FindProperty("m_ApplyToSortingLayers");targets.arraySize=layers.Length;
            for(int i=0;i<layers.Length;i++)targets.GetArrayElementAtIndex(i).intValue=layers[i];so.ApplyModifiedProperties();return light;
        }
        static void WindowLight(Transform root,string name,float left,float right,int[] layers)
        {
            var light=Light(root,name,Vector2.zero,1,.38f,new Color(1,.91f,.76f),false,layers);
            light.lightType=Light2D.LightType.Freeform;
            light.shapeLightFalloffSize=.75f/32;
            var points=new[]{new Vector3(left/32,86f/32,0),new Vector3((left-36)/32,4f/32,0),new Vector3((right-36)/32,4f/32,0),new Vector3(right/32,86f/32,0)};
            light.SetShapePath(points);
        }
        public static string Build()
        {
            var presentationShader=Shader.Find("OCC/UI/PixelStagePresentation");
            if(presentationShader==null)throw new InvalidOperationException("Pixel stage presentation shader missing");
            string presentationPath=Folder+"presentation.mat";
            var presentation=AssetDatabase.LoadAssetAtPath<Material>(presentationPath);
            if(presentation==null){presentation=new Material(presentationShader);AssetDatabase.CreateAsset(presentation,presentationPath);}
            // Neutral background materials share the same lighting pass as props.
            int backdrop=0,desk=SortingLayer.NameToID("OCCMenuDesk"),props=SortingLayer.NameToID("OCCMenuProps");
            if(desk==0 || props==0)throw new InvalidOperationException("Existing menu sorting layers missing");
            var composition=JsonUtility.FromJson<Composition>(System.IO.File.ReadAllText(Folder+"modular-composition.json"));
            if(composition==null || composition.layers==null)throw new InvalidOperationException("Modular composition missing");
            var sprites=composition.layers.Select(l=>l.name).Concat(new[]{"book_closed","book_open","cup","lamp"}).Distinct().ToDictionary(n=>n,Import);
            var shader=Shader.Find("Universal Render Pipeline/2D/Sprite-Lit-Default");
            if(shader==null)throw new InvalidOperationException("URP Sprite Lit shader missing");
            string materialPath=Folder+"shared_pixel_lit.mat";var material=AssetDatabase.LoadAssetAtPath<Material>(materialPath);
            if(material==null){material=new Material(shader);AssetDatabase.CreateAsset(material,materialPath);}else{material.shader=shader;EditorUtility.SetDirty(material);AssetDatabase.SaveAssetIfDirty(material);}
            var root=new GameObject("E原生像素光照场景"){layer=31};var stage=root.AddComponent<MainMenuPixelStage>();
            try
            {
                var cameraObject=new GameObject("Native320x180Camera"){layer=31};cameraObject.transform.SetParent(root.transform,false);
                var camera=cameraObject.AddComponent<Camera>();camera.orthographic=true;camera.orthographicSize=180f/64;
                camera.transform.localPosition=new Vector3(5,2.8125f,-10);camera.clearFlags=CameraClearFlags.SolidColor;
                camera.backgroundColor=new Color(39/255f,31/255f,26/255f,1);camera.cullingMask=1<<31;
                camera.allowHDR=false;camera.allowMSAA=false;camera.allowDynamicResolution=false;camera.depth=-100;
                var data=cameraObject.AddComponent<UniversalAdditionalCameraData>();data.renderPostProcessing=false;data.antialiasing=AntialiasingMode.None;
                stage.sceneCamera=camera;
                var pixel=cameraObject.AddComponent<PixelPerfectCamera>();pixel.assetsPPU=32;
                pixel.refResolutionX=320;pixel.refResolutionY=180;
                pixel.gridSnapping=PixelPerfectCamera.GridSnapping.UpscaleRenderTexture;
                pixel.cropFrame=PixelPerfectCamera.CropFrame.Windowbox;
                string unlitPath=Folder+"backdrop_unlit.mat";var unlit=AssetDatabase.LoadAssetAtPath<Material>(unlitPath);
                if(unlit==null){unlit=new Material(Shader.Find("Universal Render Pipeline/2D/Sprite-Unlit-Default"));AssetDatabase.CreateAsset(unlit,unlitPath);}
                foreach(var layer in composition.layers)
                    Prop(root.transform,layer.name,sprites[layer.name],new Vector2(layer.x+layer.width*.5f,180-layer.y-layer.height*.5f),layer.name=="desk"?desk:backdrop,layer.lit?material:unlit).sortingOrder=layer.order;
                // Canvas pivot stays centred; subject feet are fixed at Y=62 in 64px props.
                stage.closedBook=Prop(root.transform,"闭合书",sprites["book_closed"],new Vector2(169,47),props,material);
                stage.openBook=Prop(root.transform,"展开书",sprites["book_open"],new Vector2(169,47),props,material);stage.openBook.color=new Color(1,1,1,0);
                Prop(root.transform,"杯碟",sprites["cup"],new Vector2(244,56),props,material);
                Prop(root.transform,"以太灯",sprites["lamp"],new Vector2(283,50),props,material);
                foreach(var sr in root.GetComponentsInChildren<SpriteRenderer>())if(sr==stage.closedBook || sr==stage.openBook || sr.name=="杯碟" || sr.name=="以太灯")sr.sortingOrder=4;
                int[] all={backdrop,desk,props};Light(root.transform,"环境填充",new Vector2(160,90),30,.56f,new Color(.94f,.97f,1),false,all);
                var sun=Light(root.transform,"窗户主光",new Vector2(420,650),30,.60f,new Color(1,.94f,.82f),true,all);
                sun.pointLightInnerRadius=3;sun.shadowIntensity=.28f;
                var sunSettings=new SerializedObject(sun);sunSettings.FindProperty("m_NormalMapDistance").floatValue=12;sunSettings.ApplyModifiedProperties();
                // Ground illumination stays on the desk plane. Raised objects receive
                // the common directional light without window bars painted across them.
                int[] groundReceivers={desk};
                WindowLight(root.transform,"左窗受光",121,160,groundReceivers);
                WindowLight(root.transform,"中窗受光",170,211,groundReceivers);
                WindowLight(root.transform,"右窗受光",222,263,groundReceivers);
                stage.aetherBaseIntensity=.48f;
                stage.aetherLight=Light(root.transform,"局部以太光",new Vector2(283,37),.95f,stage.aetherBaseIntensity,new Color(.22f,.80f,1),false,all);
                stage.aetherLight.pointLightInnerRadius=3f/32;
                foreach(var sr in root.GetComponentsInChildren<SpriteRenderer>().Where(s=>s.sortingOrder==4))
                {
                    // Ground-plane casters, not vertical sprite rectangles. Their
                    // shape data is a technical footprint proxy, never replacement art.
                    var ground=new GameObject(sr.name+"_GroundCaster"){layer=31};ground.transform.SetParent(sr.transform,false);
                    ground.transform.localPosition=new Vector3(0,-30f/32,0);
                    var caster=ground.AddComponent<ShadowCaster2D>();caster.useRendererSilhouette=false;caster.selfShadows=false;caster.castsShadows=true;
                    float half=(sr==stage.openBook?58:sr==stage.closedBook?26:sr.name=="杯碟"?14:4)/32f;
                    var so=new SerializedObject(caster);so.FindProperty("m_ShadowCastingSource").intValue=1;
                    var shape=so.FindProperty("m_ShapePath");shape.arraySize=4;
                    var points=new[]{new Vector3(-half,0,0),new Vector3(half,0,0),new Vector3(half,1f/32,0),new Vector3(-half,1f/32,0)};
                    for(int i=0;i<4;i++)shape.GetArrayElementAtIndex(i).vector3Value=points[i];
                    so.FindProperty("m_ShapePathHash").intValue=Animator.StringToHash(ground.name);
                    var targets=so.FindProperty("m_ApplyToSortingLayers");targets.arraySize=1;targets.GetArrayElementAtIndex(0).intValue=desk;so.ApplyModifiedProperties();
                    if(sr==stage.closedBook)stage.closedShadow=caster;
                    if(sr==stage.openBook){stage.openShadow=caster;caster.enabled=false;}
                }
                PrefabUtility.SaveAsPrefabAsset(root,"Assets/Game/Resources/UI/MainMenuEPixelStage.prefab");
            }
            finally{UnityEngine.Object.DestroyImmediate(root);}
            return "Saved independently composed empty room, window vista, furniture and props; shared URP2D lighting and native320x180 output";
        }
    }
}
