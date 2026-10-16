"""Replace the rejected presentation while retaining scene-owned playback helpers."""
from pathlib import Path
root=Path(__file__).resolve().parents[3]
path=root/'UnityProject/Assets/Game/Runtime/Presentation/SkyFurnaceDirector.cs'
s=path.read_text(encoding='utf-8-sig')
def method(name,text):
    global s
    start=s.index('        '+name)
    brace=s.index('{',start);depth=1;end=brace+1
    while depth:
        depth+=(s[end]=='{')-(s[end]=='}');end+=1
    s=s[:start]+text.rstrip()+s[end:]
s=s.replace('Duration = 22f, Impact = 8.35f','Duration = 16f, Impact = 7.4f')
s=s.replace('private Texture2D fireTexture, ashTexture, sealTexture, coreTexture;','private Texture2D fireTexture, ashTexture, sealTexture, coreTexture, bandTexture, chamberTexture;\n        private Material bandMaterial, chamberMaterial;\n        private Transform chamber, castSeal;\n        private readonly List<MeshRenderer> furnaceBands=new List<MeshRenderer>();\n        private Mesh flameMesh;\n        private MeshRenderer flameRenderer;\n        private Vector3 fixedCameraPosition;\n        private Quaternion fixedCameraRotation;\n        private float fixedFieldOfView;\n        public bool CameraLocked => Vector3.Distance(RenderCamera.transform.position,fixedCameraPosition)<.00001f && Quaternion.Angle(RenderCamera.transform.rotation,fixedCameraRotation)<.0001f && Mathf.Abs(RenderCamera.fieldOfView-fixedFieldOfView)<.00001f;\n        public string PhaseName => Age<1?"起手":Age<3?"地阵通电":Age<5?"天炉成形":Age<7?"炉芯压缩":Age<Impact?"贯穿接触":Age<9?"主爆发":Age<11.5f?"火冠回落":Age<14?"烟尘余波":"退场终态";')
s=s.replace('cold.SetColor("_Tint", new Color(.35f, 1.7f, 1.55f, 1))','cold.SetColor("_Tint", new Color(1.8f, .42f, .06f, 1))')
s=s.replace('coreTexture=RequireTexture("Art/SkyFurnace/furnace_core");','coreTexture=RequireTexture("Art/SkyFurnace/compressed_core");bandTexture=RequireTexture("Art/SkyFurnace/ring_band");chamberTexture=RequireTexture("Art/SkyFurnace/chamber_fire");')
s=s.replace('BuildArrays(); BuildDestruction(); BuildAtmosphere();','BuildArrays(); BuildAtmosphere();')
s=s.replace('coreMaterial.SetFloat("_Emission",2);','coreMaterial.SetFloat("_Emission",1.25f);\n            bandMaterial=Own(new Material(pixelShader));bandMaterial.SetTexture("_MainTex",Repeat(bandTexture));bandMaterial.SetFloat("_Emission",.45f);\n            chamberMaterial=Own(new Material(pixelShader));chamberMaterial.SetTexture("_MainTex",chamberTexture);chamberMaterial.SetFloat("_Emission",.7f);')
method('private void BuildEnvironment()',r'''
        private void BuildEnvironment()
        {
            // The seven metre spell field is embedded in an intact courtyard.
            Box("学院庭院承接地面",new Vector3(0,-.28f,1),new Vector3(18,.25f,18),floor);
            Box("7×7完整实训石坪",new Vector3(0,-.125f,0),new Vector3(7,.25f,7),floor);
            Box("永久后墙左",new Vector3(-4.4f,.65f,5.3f),new Vector3(5.5f,1.3f,.5f),masonry);
            Box("永久后墙右",new Vector3(4.4f,.65f,5.3f),new Vector3(5.5f,1.3f,.5f),masonry);
            for(int step=0;step<5;step++)
                Box("学院阶梯 "+step,new Vector3(0,(step+1)*.10f,4.05f+step*.25f),new Vector3(3.3f,.20f,.5f),masonry);
            for(int side=-1;side<=1;side+=2)
            {
                Box("阶梯门柱 "+side,new Vector3(side*1.75f,1.10f,5.0f),new Vector3(.375f,2.2f,.375f),masonry);
                Box("庭院侧墙 "+side,new Vector3(side*6.5f,.65f,1.7f),new Vector3(.5f,1.3f,7.2f),masonry);
                for(int i=0;i<3;i++)
                {
                    float z=-1.5f+i*2.4f;
                    EnvironmentCard("庭院完整花槽", "Art/FormalAcademyCombat32/academy_light_planter_intact",new Vector3(side*5,0,z));
                    EnvironmentCard("庭院晶灯", "Art/FormalAcademyCombat32/academy_decoy_lantern_active",new Vector3(side*4.4f,0,z));
                    var light=Lamp("庭院固定灯 "+side+"/"+i,LightType.Point,new Color(1,.62f,.28f),.5f,2.7f);light.transform.position=new Vector3(side*4.4f,.85f,z);
                }
            }
            // These remain whole for the complete film; no destruction nodes exist.
            foreach(var position in new[]{new Vector3(-1.85f,.35f,-.5f),new Vector3(1.85f,.35f,-.5f),new Vector3(0,.35f,2.25f)})
                Box("完整石障 · 不参与破坏",position,new Vector3(1.2f,.7f,.32f),masonry);
        }
''')
method('private void BuildCamera()',r'''
        private void BuildCamera()
        {
            var source=GraphicsSettings.currentRenderPipeline as UniversalRenderPipelineAsset;
            if(source==null)throw new MissingReferenceException("Sky Furnace requires URP");
            previousPipeline=QualitySettings.renderPipeline;localPipeline=Own(Instantiate(source));
            localPipeline.name="Sky Furnace owned full-resolution candidate";localPipeline.renderScale=1;QualitySettings.renderPipeline=localPipeline;
            var obj=Node("固定45度战场相机");obj.tag="MainCamera";
            RenderCamera=obj.AddComponent<Camera>();obj.AddComponent<AudioListener>();
            RenderCamera.allowHDR=true;RenderCamera.allowMSAA=false;RenderCamera.nearClipPlane=.1f;RenderCamera.farClipPlane=80;
            RenderCamera.cullingMask=1<<Layer;RenderCamera.clearFlags=CameraClearFlags.SolidColor;RenderCamera.backgroundColor=new Color(.17f,.20f,.27f);
            fixedCameraRotation=Quaternion.Euler(45,0,0);fixedCameraPosition=new Vector3(0,1.1f,.3f)-fixedCameraRotation*Vector3.forward*12.6f;fixedFieldOfView=32;
            RenderCamera.transform.SetPositionAndRotation(fixedCameraPosition,fixedCameraRotation);RenderCamera.fieldOfView=fixedFieldOfView;
            var data=RenderCamera.GetUniversalAdditionalCameraData();data.SetRenderer(1);data.renderPostProcessing=true;
            data.volumeLayerMask=1<<Layer;data.requiresColorTexture=true;data.requiresDepthTexture=true;
            var profile=Own(ScriptableObject.CreateInstance<VolumeProfile>());profile.Add<Tonemapping>(true).mode.value=TonemappingMode.ACES;
            var bloom=profile.Add<Bloom>(true);bloom.threshold.value=1.5f;bloom.intensity.value=.18f;bloom.scatter.value=.35f;
            var volume=Node("演出独立后期").AddComponent<Volume>();volume.isGlobal=true;volume.sharedProfile=profile;
            sun=Lamp("庭院夕光",LightType.Directional,new Color(1,.79f,.59f),1.25f,100);
            sun.transform.rotation=Quaternion.Euler(42,65,0);sun.shadows=LightShadows.Soft;sun.shadowBias=.025f;sun.shadowNormalBias=.04f;
            fill=Lamp("天空填充",LightType.Directional,new Color(.59f,.72f,1),.42f,100);fill.transform.rotation=Quaternion.Euler(35,-115,0);
            staffLight=Lamp("杖端火矢",LightType.Point,new Color(1,.43f,.10f),0,2.5f);
            coreLight=Lamp("炉腔琥珀受光",LightType.Point,new Color(1,.48f,.12f),0,5.5f);
            impactLight=Lamp("爆发真实照明",LightType.Point,new Color(1,.42f,.10f),0,6);impactLight.shadows=LightShadows.Soft;
        }
''')
method('private void BuildArrays()',r'''
        private void BuildArrays()
        {
            MakeRing("赤金地阵 · 顺序通电",2.85f,.04f,1,0,0);
            MakeRing("地阵内沿",2.35f,.05f,1.45f,0,0);MakeRing("目标收束口",.55f,.06f,2,0,0);
            for(int i=0;i<8;i++)
            {
                float angle=i*Mathf.PI/4;var obj=Node("地阵节点 "+i);
                obj.transform.position=centre+new Vector3(Mathf.Cos(angle)*2.45f,.06f,Mathf.Sin(angle)*2.45f);obj.transform.rotation=Quaternion.Euler(90,0,0);
                obj.transform.localScale=new Vector3(sealTexture.width/32f,sealTexture.height/32f,1);obj.AddComponent<MeshFilter>().sharedMesh=Own(Quad());
                var renderer=obj.AddComponent<MeshRenderer>();renderer.sharedMaterial=sealMaterial;seals.Add(renderer);
                var line=Line("地表向心导流 "+i,hot,2,.0625f);line.SetPosition(0,centre+new Vector3(Mathf.Cos(angle)*2.45f,.055f,Mathf.Sin(angle)*2.45f));
                line.SetPosition(1,centre+new Vector3(Mathf.Cos(angle)*.55f,.055f,Mathf.Sin(angle)*.55f));conduits.Add(line);
            }
            castSeal=Node("火矢生脚下起手阵").transform;castSeal.position=new Vector3(actorOrigin.x,.035f,actorOrigin.z);castSeal.rotation=Quaternion.Euler(90,0,0);
            castSeal.localScale=new Vector3(sealTexture.width/32f,sealTexture.height/32f,1);castSeal.gameObject.AddComponent<MeshFilter>().sharedMesh=Own(Quad());castSeal.gameObject.AddComponent<MeshRenderer>().sharedMaterial=sealMaterial;
            for(int i=0;i<3;i++)
            {
                float radius=new[]{1.65f,1.25f,.6f}[i],height=new[]{3.15f,2.20f,1.20f}[i];
                var obj=Node("同轴天炉阵带 "+i);obj.transform.position=centre+Vector3.up*height;
                var mesh=Own(CylinderSheet(radius,bandTexture.height/32f,bandTexture.width/32f));obj.AddComponent<MeshFilter>().sharedMesh=mesh;
                var renderer=obj.AddComponent<MeshRenderer>();renderer.sharedMaterial=bandMaterial;renderer.shadowCastingMode=ShadowCastingMode.Off;furnaceBands.Add(renderer);
            }
            chamber=PixelCard("天炉火流腔",chamberTexture,chamberMaterial,centre+Vector3.up*2.35f);
            core=PixelCard("压缩小炉芯",coreTexture,coreMaterial,centre+Vector3.up*2.15f);
            lance=Line("唯一垂直贯穿火柱",hot,2,.125f);shock=Line("水平压力波",hot,128,.09375f);shock.loop=true;
            blast=Node("连续地面火冠").transform;blast.position=centre;
            fireMaterial.SetTexture("_MainTex",Repeat(fireTexture));fireMaterial.SetFloat("_Emission",.65f);
            flameMesh=Own(CylinderSheet(.25f,fireTexture.height/32f,fireTexture.width/32f));
            // Cylinder vertices carry metre-based UVs; radius motion never scales sprite pixels.
            var vertices=flameMesh.vertices;for(int i=0;i<vertices.Length;i++)vertices[i]+=Vector3.up*(fireTexture.height/64f);flameMesh.vertices=vertices;flameMesh.RecalculateBounds();
            blast.gameObject.AddComponent<MeshFilter>().sharedMesh=flameMesh;flameRenderer=blast.gameObject.AddComponent<MeshRenderer>();flameRenderer.sharedMaterial=fireMaterial;
            heat=Node("局部热浪").transform;heat.gameObject.AddComponent<MeshFilter>().sharedMesh=Own(Quad());heat.gameObject.AddComponent<MeshRenderer>().sharedMaterial=Own(new Material(HeatShader));
        }
''')
method('private void BuildDestruction()',r'''
        // No terrain or object destruction is present in this revision.
        private void EvaluateFragments(float t) { BrokenPieces=0; }
''')
# Remove the old fragment trajectory method; retain the no-op count for external checks.
start=s.index('        private void EvaluateFragments(float t)',s.index('        private void EvaluateCamera'))
brace=s.index('{',start);depth=1;end=brace+1
while depth:
    depth+=(s[end]=='{')-(s[end]=='}');end+=1
s=s[:start]+s[end:]
method('private void BuildAtmosphere()',r'''
        private void BuildAtmosphere()
        {
            for(int i=0;i<16;i++)
            {
                float angle=i*Mathf.PI*2/16;var direction=new Vector3(Mathf.Cos(angle),0,Mathf.Sin(angle));
                AddCloud("贴地尘浪 "+i,centre+direction*.6f+Vector3.up*.18f,direction*.6f,Impact+.35f+i%4*.04f,5.4f,1,i*.739f,false);
            }
            for(int i=0;i<14;i++)
            {
                float angle=i*2.399963f;var origin=centre+new Vector3(Mathf.Cos(angle)*.2f,.55f+i%3*.18f,Mathf.Sin(angle)*.2f);
                AddCloud("中央上升灰烟 "+i,origin,new Vector3(Mathf.Cos(angle)*.065f,.30f+i%3*.04f,Mathf.Sin(angle)*.065f),Impact+.9f+i*.12f,6.4f,1,i*.921f,false);
            }
            var emberMesh=Own(Octahedron());
            for(int i=0;i<40;i++){var obj=Node("稀疏余烬 "+i);obj.AddComponent<MeshFilter>().sharedMesh=emberMesh;obj.AddComponent<MeshRenderer>().sharedMaterial=hot;obj.transform.localScale=Vector3.one*PixelPitch;embers.Add(obj.transform);}
        }
''')
method('private void Evaluate()',r'''
        private void Evaluate()
        {
            if(!initialized)return;float t=Age,after=t-Impact;EvaluateActor(t);
            // Camera is configured once; evaluation never changes its transform or projection.
            float groundFade=1-Ramp(t,9,12),compression=Ramp(t,5,7);
            foreach(var ring in rings)
            {
                float reveal=Ramp(t,ring.start,ring.start+1.2f);ring.root.gameObject.SetActive(reveal>0&&groundFade>0);
                props.Clear();props.SetFloat("_Reveal",reveal);props.SetFloat("_Intensity",groundFade*(.68f-.24f*compression));ring.renderer.SetPropertyBlock(props);
            }
            for(int i=0;i<seals.Count;i++)
            {SetLayer(seals[i],Ramp(t,1.1f+i*.13f,1.7f+i*.13f)*groundFade,.30f);conduits[i].enabled=t>1.3f+i*.13f&&t<11;}
            castSeal.gameObject.SetActive(t>0&&t<10);SetLayer(castSeal.GetComponent<MeshRenderer>(),Ramp(t,0,.6f)*(1-Ramp(t,8,10)),.35f);
            float furnaceFade=1-Ramp(t,Impact,8.7f);
            for(int i=0;i<furnaceBands.Count;i++)
            {var renderer=furnaceBands[i];float reveal=Ramp(t,3+i*.22f,3.9f+i*.22f);renderer.gameObject.SetActive(reveal>0&&furnaceFade>0);SetLayer(renderer,reveal*furnaceFade,.55f-.4f*compression);}
            chamber.gameObject.SetActive(t>3.25f&&t<7.55f);chamber.rotation=fixedCameraRotation;
            SetLayer(chamber.GetComponent<MeshRenderer>(),Ramp(t,3.25f,4.6f)*(1-Ramp(t,5.1f,6.7f))*(1-Ramp(t,7.3f,7.55f)),.75f);
            core.gameObject.SetActive(t>5.05f&&t<7.55f);core.position=centre+Vector3.up*2.15f;core.rotation=fixedCameraRotation;
            SetLayer(core.GetComponent<MeshRenderer>(),Ramp(t,5.05f,6)*(1-Ramp(t,7.4f,7.55f)),1.0f+compression*.5f);
            lance.enabled=t>=7&&t<7.85f;lance.SetPosition(0,centre+Vector3.up*2.15f);
            lance.SetPosition(1,centre+Vector3.up*Mathf.Lerp(2.15f,.07f,Ramp(t,7,Impact)));lance.widthMultiplier=t<Impact?.09375f:.1875f*(1-Ramp(t,Impact,7.85f));
            shock.enabled=after>=0&&after<1.1f;shock.widthMultiplier=.09375f*(1-Ramp(after,.45f,1.1f));
            for(int i=0;i<128;i++){float a=i*Mathf.PI*2/128,r=Mathf.Min(3.25f,.15f+Mathf.Max(0,after)*4);shock.SetPosition(i,centre+new Vector3(Mathf.Cos(a)*r,.045f,Mathf.Sin(a)*r));}
            blast.gameObject.SetActive(after>=0&&t<10.6f);
            if(after>=0&&t<10.6f)
            {
                float radius=Mathf.Lerp(.2f,2.5f,Ramp(after,0,1.15f));var vertices=flameMesh.vertices;var uv=flameMesh.uv;
                for(int i=0;i<vertices.Length;i++){float angle=i/2*Mathf.PI*2/128;vertices[i].x=Mathf.Cos(angle)*radius;vertices[i].z=Mathf.Sin(angle)*radius;uv[i].x=angle*radius/(fireTexture.width/32f);}
                flameMesh.vertices=vertices;flameMesh.uv=uv;flameMesh.RecalculateBounds();SetLayer(flameRenderer,Ramp(after,0,.12f)*(1-Ramp(t,8.65f,10.6f)),.85f);
            }
            coreLight.transform.position=centre+Vector3.up*2;coreLight.intensity=Ramp(t,3,4.5f)*(1-Ramp(t,7.25f,7.6f))*(1.4f+compression*2.4f);
            impactLight.transform.position=centre+Vector3.up*.8f;impactLight.intensity=after>=0?7*Mathf.Exp(-after*2.3f)+1.1f*(1-Ramp(after,0,3)):0;
            sun.intensity=1.25f*(1-.22f*Ramp(t,3,6)*(1-Ramp(t,9,13)));EvaluateFragments(t);EvaluateClouds(t);
            for(int i=0;i<embers.Count;i++)
            {
                float a=after-i%5*.05f;bool visible=a>=0&&a<4.5f+i%3;embers[i].gameObject.SetActive(visible);if(!visible)continue;
                float angle=i*2.399963f,radius=Mathf.Min(a*.65f,2.55f);embers[i].position=centre+new Vector3(Mathf.Cos(angle)*radius,Mathf.Max(.07f,.5f+a*(.7f+i%4*.07f)-a*a*.16f),Mathf.Sin(angle)*radius);
            }
            float heatAge=t-7.6f;heat.gameObject.SetActive(heatAge>=0&&heatAge<3.5f);heat.position=centre+Vector3.up*1.15f;heat.rotation=fixedCameraRotation;heat.localScale=new Vector3(5.8f,2.6f,1);
            props.Clear();props.SetFloat("_Age",t);props.SetFloat("_Strength",heatAge>=0?.0035f*(1-Ramp(heatAge,.4f,3.5f)):0);heat.GetComponent<MeshRenderer>().SetPropertyBlock(props);
        }
''')
method('private void EvaluateActor(float t)',r'''
        private void EvaluateActor(float t)
        {
            actor.position=actorOrigin;actor.rotation=fixedCameraRotation;actorMat.SetTexture("_BaseMap",actorFrames[t>=.55f&&t<8.6f?1:0]);
            staffLight.transform.position=actorOrigin+Vector3.up*1.4f+Vector3.right*.3f;staffLight.intensity=1.15f*Ramp(t,0,.7f)*(1-Ramp(t,7,Impact));
        }
''')
method('private void EvaluateCamera(float t)',r'''
        private void EvaluateCamera(float t) { /* Permanently locked; no shot animation. */ }
''')
s=s.replace('22.00 秒　{(Paused?"暂停":"演出")}　{BrokenPieces} / {fragments.Count} 破坏构件','16.00 秒　{(Paused?"暂停":Age>=Duration?"结束":PhaseName)}')
s=s.replace('new[]{"music_22s","sfx_22s"}','new[]{"music_16s","sfx_16s"}').replace('name=="music_22s"?"22秒短篇配乐":"分层魔法／石块／空气音轨"','name=="music_16s"?"16秒短篇配乐":"分层魔法／空气音轨"')
s=s.replace('new[]{"music_22s","sfx_22s"}','new[]{"music_16s","sfx_16s"}')
extra=r'''
        private Texture2D Repeat(Texture2D source)
        {var copy=Own(new Texture2D(source.width,source.height,source.format,false));Graphics.CopyTexture(source,copy);copy.wrapMode=TextureWrapMode.Repeat;copy.filterMode=FilterMode.Point;return copy;}
        private void SetLayer(MeshRenderer renderer,float opacity,float emission)
        {props.Clear();props.SetFloat("_Opacity",Mathf.Clamp01(opacity));props.SetFloat("_Emission",emission);renderer.SetPropertyBlock(props);}
        private Transform PixelCard(string name,Texture2D texture,Material material,Vector3 position)
        {var obj=Node(name);obj.transform.position=position;obj.transform.localScale=new Vector3(texture.width/32f,texture.height/32f,1);obj.AddComponent<MeshFilter>().sharedMesh=Own(Quad());var renderer=obj.AddComponent<MeshRenderer>();renderer.sharedMaterial=material;renderer.shadowCastingMode=ShadowCastingMode.Off;return obj.transform;}
        private void EnvironmentCard(string name,string source,Vector3 foot)
        {
            var texture=RequireTexture(source);var material=Own(new Material(Shader.Find("OCC/HD2D/GroundedSprite")));material.SetTexture("_BaseMap",texture);material.SetFloat("_Cutoff",.5f);material.EnableKeyword("_ALPHATEST_ON");
            var obj=Node(name);obj.transform.position=foot;obj.transform.rotation=Quaternion.Euler(45,0,0);
            var mesh=Own(Quad());var vertices=mesh.vertices;for(int i=0;i<vertices.Length;i++)vertices[i]=new Vector3(vertices[i].x*texture.width/32f,(vertices[i].y+.5f)*texture.height/32f,0);mesh.vertices=vertices;mesh.triangles=new[]{0,2,1,0,3,2};mesh.RecalculateNormals();mesh.RecalculateBounds();
            obj.AddComponent<MeshFilter>().sharedMesh=mesh;var renderer=obj.AddComponent<MeshRenderer>();renderer.sharedMaterial=material;renderer.shadowCastingMode=ShadowCastingMode.Off;
        }
        private static Mesh CylinderSheet(float radius,float height,float textureWidthMetres)
        {
            var vertices=new Vector3[258];var uv=new Vector2[258];var triangles=new int[768];
            for(int i=0;i<=128;i++)
            {
                float angle=i*Mathf.PI*2/128;for(int row=0;row<2;row++){int k=i*2+row;vertices[k]=new Vector3(Mathf.Cos(angle)*radius,(row-.5f)*height,Mathf.Sin(angle)*radius);uv[k]=new Vector2(angle*radius/textureWidthMetres,row);}
                if(i==128)continue;int t=i*6,n=i*2;triangles[t]=n;triangles[t+1]=n+1;triangles[t+2]=n+3;triangles[t+3]=n;triangles[t+4]=n+3;triangles[t+5]=n+2;
            }
            var mesh=new Mesh{name="Native32 metre UV curved spell sheet"};mesh.vertices=vertices;mesh.uv=uv;mesh.triangles=triangles;mesh.RecalculateNormals();mesh.RecalculateBounds();return mesh;
        }
'''
s=s.replace('        private void OnGUI()',extra+'\n        private void OnGUI()')
path.write_text(s,encoding='utf-8')
print('Updated',path)
