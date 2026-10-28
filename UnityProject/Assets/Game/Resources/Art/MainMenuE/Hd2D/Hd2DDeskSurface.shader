Shader "OCC/HD2D/DeskSurface"
{
    Properties
    {
        [MainTexture] _BaseMap("Native colour", 2D) = "white" {}
        [MainColor] _BaseColor("Tint", Color) = (1,1,1,1)
        _Smoothness("Smoothness", Range(0,1)) = .1
        _SpecColor("Dielectric reflection", Color) = (.04,.04,.04,1)
        _Metallic("Metallic", Range(0,1)) = 0
        _NormalMap("Independent surface normal", 2D) = "bump" {}
        _MaterialMap("Surface R smoothness G coat B occlusion", 2D) = "white" {}
        _UseNormalMap("Use independent normal", Float) = 0
        _UseMaterialMap("Use independent surface properties", Float) = 0
        _NormalStrength("Normal strength", Range(0,2)) = 1
        _TimberFiberStrength("Independent timber fibre slopes", Range(0,.3)) = 0
        _TimberFinishVariation("Independent timber finish variation", Range(0,.2)) = 0
        _CoatMask("Varnish layer", Range(0,1)) = 0
        _CoatSmoothness("Varnish smoothness", Range(0,1)) = .85
        _ReliefStrength("Source grain relief", Range(0,2)) = 0
        _RoughnessVariation("Source grain roughness", Range(0,.5)) = 0
        _HeightUvBounds("Grain patch bounds", Vector) = (0,0,1,1)
        _CrystalMask("Crystal and brass response", Float) = 0
        [HDR] _EmissionColor("Crystal emission", Color) = (.35,1,1.5,1)
        _EmissionStrength("Crystal output", Float) = 0
        _SoftEmission("Albedo readability emission", Range(0,.6)) = 0
        [HDR] _DeskEdgeColour("Desk edge radiance", Color) = (1,.78,.44,1)
        _DeskEdgeStrength("Desk edge output (opt-in)", Float) = 0
        _DeskEdgeWidth("Desk inner edge width in output pixels", Float) = 2
        _DeskUvBounds("Desk sprite bounds", Vector) = (0,0,1,1)
        _DeskRect("Desk plain surface bounds", Vector) = (0,0,1,1)
        _DeskPlain("Desk plain surface", Float) = 0
        _DeskPaperSurface("Paper material role", Float) = 0
        _DeskPaperAlbedo("Warm paper albedo", Color) = (.70,.61,.46,1)
        _DeskFrameBrass("Oxidised brass", Color) = (.46,.28,.095,1)
        _DeskFrameInk("Recessed edge", Color) = (.075,.045,.022,1)
        _DeskFrame("Inset material frame", Float) = 0
        _DeskPixelPitch("Native world pitch in carrier units", Vector) = (1,1,1.3,0)
        [Toggle(_ALPHATEST_ON)] _AlphaClip("Cutout", Float) = 0
        _Cutoff("Cutout threshold", Range(0,1)) = .4
        [Enum(UnityEngine.Rendering.CullMode)] _Cull("Cull", Float) = 0
    }
    SubShader
    {
        Tags { "RenderPipeline"="UniversalPipeline" "RenderType"="Opaque" "Queue"="Geometry" }
        HLSLINCLUDE
        #define _SPECULAR_SETUP 1
        #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
        #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Lighting.hlsl"
        #include "Packages/com.unity.render-pipelines.core/ShaderLibrary/Packing.hlsl"
        TEXTURE2D(_BaseMap); SAMPLER(sampler_BaseMap);
        // Linear sampling is confined to the lighting mask. Painted colour keeps
        // its importer sampling and native pixel clusters.
        SAMPLER(sampler_DeskLinearClamp);
        TEXTURE2D(_NormalMap); SAMPLER(sampler_NormalMap);
        TEXTURE2D(_MaterialMap); SAMPLER(sampler_MaterialMap);
        float4 _BaseMap_TexelSize;
        CBUFFER_START(UnityPerMaterial)
        float4 _BaseMap_ST, _HeightUvBounds;
        half4 _BaseColor, _SpecColor, _EmissionColor;
        half _Smoothness, _Metallic, _ReliefStrength, _RoughnessVariation;
        half _CrystalMask, _EmissionStrength, _AlphaClip, _Cutoff, _Cull;
        half _SoftEmission;
        half4 _DeskEdgeColour;
        float4 _DeskUvBounds, _DeskRect;
        half _DeskEdgeStrength, _DeskEdgeWidth, _DeskPlain;
        half _DeskPaperSurface, _DeskFrame;
        half4 _DeskPaperAlbedo, _DeskFrameBrass, _DeskFrameInk;
        float4 _DeskPixelPitch;
        half _UseNormalMap, _UseMaterialMap, _NormalStrength, _CoatMask, _CoatSmoothness;
        half _TimberFiberStrength, _TimberFinishVariation;
        CBUFFER_END
        struct Attributes {float4 positionOS:POSITION; float3 normalOS:NORMAL; float2 uv:TEXCOORD0;};
        struct Varyings {float4 positionCS:SV_POSITION; float3 positionWS:TEXCOORD0; half3 normalWS:TEXCOORD1; float2 uv:TEXCOORD2;};
        Varyings Vert(Attributes input)
        {
            Varyings o;
            o.positionWS=TransformObjectToWorld(input.positionOS.xyz);
            o.positionCS=TransformWorldToHClip(o.positionWS);
            o.normalWS=TransformObjectToWorldNormal(input.normalOS);
            o.uv=TRANSFORM_TEX(input.uv,_BaseMap);
            return o;
        }
        half Height(float2 uv)
        {
            // Neighbours stay inside the existing native material patch.
            uv=clamp(uv,_HeightUvBounds.xy+_BaseMap_TexelSize.xy*.5,
                _HeightUvBounds.zw-_BaseMap_TexelSize.xy*.5);
            return dot(SAMPLE_TEXTURE2D(_BaseMap,sampler_BaseMap,uv).rgb,half3(.2126,.7152,.0722));
        }
        ENDHLSL
        Pass
        {
            Name "ForwardLit"
            Tags {"LightMode"="UniversalForward"}
            Cull [_Cull] ZWrite On
            HLSLPROGRAM
            #pragma target 3.5
            #pragma vertex Vert
            #pragma fragment Frag
            #pragma shader_feature_local_fragment _ALPHATEST_ON
            #pragma shader_feature_local_fragment _CLEARCOAT
            #pragma shader_feature_local_fragment OCC_DESK_READABILITY
            #pragma multi_compile _ _MAIN_LIGHT_SHADOWS _MAIN_LIGHT_SHADOWS_CASCADE _MAIN_LIGHT_SHADOWS_SCREEN
            #pragma multi_compile _ _ADDITIONAL_LIGHTS_VERTEX _ADDITIONAL_LIGHTS
            #pragma multi_compile_fragment _ _ADDITIONAL_LIGHT_SHADOWS
            #pragma multi_compile_fragment _ _SHADOWS_SOFT _SHADOWS_SOFT_LOW _SHADOWS_SOFT_MEDIUM _SHADOWS_SOFT_HIGH
            #pragma multi_compile_fragment _ _REFLECTION_PROBE_BLENDING
            #pragma multi_compile_fragment _ _REFLECTION_PROBE_BOX_PROJECTION
            #pragma multi_compile_fragment _ _REFLECTION_PROBE_ATLAS
            #pragma multi_compile_fragment _ _SCREEN_SPACE_OCCLUSION
            #pragma multi_compile_fog
            #if defined(OCC_DESK_READABILITY)
            half DeskAlpha(float2 uv)
            {
                half inside=step(_DeskUvBounds.x,uv.x)*step(_DeskUvBounds.y,uv.y)
                    *step(uv.x,_DeskUvBounds.z)*step(uv.y,_DeskUvBounds.w);
                return SAMPLE_TEXTURE2D(_BaseMap,sampler_DeskLinearClamp,uv).a*inside;
            }
            #endif
            half4 Frag(Varyings input, FRONT_FACE_TYPE face:FRONT_FACE_SEMANTIC):SV_Target
            {
                half4 source=SAMPLE_TEXTURE2D(_BaseMap,sampler_BaseMap,input.uv);
                half4 colour=source*_BaseColor;
                #if defined(_ALPHATEST_ON)
                clip(colour.a-_Cutoff);
                #endif
                half3 n=normalize(input.normalWS)*IS_FRONT_VFACE(face,1,-1);
                // Analytic timber fibres are independent of the colour texture.
                // UV-to-native-texel mapping keeps their world density at 32 PPU.
                float2 timberCell=floor(input.uv*_BaseMap_TexelSize.zw);
                // Uneven fibre bands, not periodic sine stripes or sampled colour.
                float band=floor(timberCell.y+sin(timberCell.x*.08)*.6);
                half fibre=(frac(sin(band*127.1+17.7)*43758.5453)*2-1);
                if(_ReliefStrength>.001 || _UseNormalMap>.5 || _TimberFiberStrength>.001)
                {
                    float3 px=ddx(input.positionWS),py=ddy(input.positionWS);
                    float2 ux=ddx(input.uv),uy=ddy(input.uv);
                    float det=ux.x*uy.y-ux.y*uy.x;
                    float signDet=det<0?-1:1;
                    float3 t=normalize((px*uy.y-py*ux.y)*signDet);
                    float3 b=normalize((py*ux.x-px*uy.x)*signDet);
                    half hx=Height(input.uv+float2(_BaseMap_TexelSize.x,0))-Height(input.uv-float2(_BaseMap_TexelSize.x,0));
                    half hy=Height(input.uv+float2(0,_BaseMap_TexelSize.y))-Height(input.uv-float2(0,_BaseMap_TexelSize.y));
                    if(_UseNormalMap>.5)
                    {
                        half3 normalTS=UnpackNormalScale(SAMPLE_TEXTURE2D(_NormalMap,sampler_NormalMap,input.uv),_NormalStrength);
                        n=normalize(t*normalTS.x+b*normalTS.y+n*normalTS.z);
                    }
                    else n=normalize(n-(t*hx+b*hy)*_ReliefStrength);
                    n=normalize(n+b*fibre*_TimberFiberStrength+t*sin(timberCell.x*.41+timberCell.y*.23)*_TimberFiberStrength*.2);
                }
                half crystal=saturate((source.b-source.r-.025)*10)*step(.12,source.g)*_CrystalMask;
                half brass=step(source.b*1.2+.04,source.r)*step(.18,source.r)*_CrystalMask;
                half metal=max(_Metallic,brass*.85);
                SurfaceData surface=(SurfaceData)0;
                surface.albedo=colour.rgb;
                surface.specular=lerp(_SpecColor.rgb,colour.rgb,metal);
                surface.smoothness=saturate(_Smoothness+(dot(source.rgb,half3(.2126,.7152,.0722))-.35)*_RoughnessVariation);
                surface.smoothness=lerp(surface.smoothness,.72,max(brass,crystal));
                surface.smoothness=saturate(surface.smoothness+fibre*_TimberFinishVariation);
                surface.occlusion=1;surface.alpha=colour.a;
                surface.clearCoatMask=_CoatMask;
                surface.clearCoatSmoothness=_CoatSmoothness;
                if(_UseMaterialMap>.5)
                {
                    half3 properties=SAMPLE_TEXTURE2D(_MaterialMap,sampler_MaterialMap,input.uv).rgb;
                    surface.smoothness=properties.r;
                    surface.clearCoatMask*=properties.g;
                    surface.occlusion=properties.b;
                }
                surface.normalTS=half3(0,0,1);
                surface.emission=source.rgb*_EmissionColor.rgb*_EmissionStrength*crystal
                    +colour.rgb*_SoftEmission;
                #if defined(OCC_DESK_READABILITY)
                if(_DeskPaperSurface>.5)
                {
                    surface.albedo*=_DeskPaperAlbedo.rgb;
                    surface.emission*=_DeskPaperAlbedo.rgb;
                }
                if(_DeskFrame>.5)
                {
                    float2 local=TransformWorldToObject(input.positionWS).xy;
                    float2 corner=min(local-_DeskRect.xy,_DeskRect.zw-local)/max(_DeskPixelPitch.xy,float2(.001,.001));
                    float d=min(corner.x,corner.y),w=_DeskPixelPitch.z;
                    float aa=max(fwidth(d),.015);
                    half band=1-smoothstep(w-aa,w+aa,d);
                    half dark=1-smoothstep(.045-aa,.045+aa,d);
                    half inner=1-smoothstep(.025-aa,.025+aa,abs(d-(w+.17)));
                    half etch=(1-smoothstep(.025-aa,.025+aa,min(abs(corner.x-1.05),abs(corner.y-1.05))))
                        *(1-smoothstep(1.8-aa,1.8+aa,max(corner.x,corner.y)));
                    half plate=1-smoothstep(2-aa,2+aa,corner.x+corner.y);
                    float studDistance=length(corner-float2(.53,.53));
                    half stud=1-smoothstep(.16-aa,.16+aa,studDistance);
                    // Repeated inventory cells keep a single recessed rail;
                    // corner hardware belongs to the enclosing document.
                    half ornament=1-step(1.5,_DeskFrame);
                    half metalMask=saturate(band+ornament*(inner+etch+plate));
                    half3 metalColour=lerp(_DeskFrameBrass.rgb,_DeskFrameInk.rgb,dark);
                    metalColour=lerp(metalColour,_DeskFrameBrass.rgb*1.8,stud);
                    // A bright lip over a darker recessed seam gives the brass
                    // an actual material response under the shared room lights.
                    metalColour*=lerp(.65,1.25,saturate(d/max(w,.01)));
                    surface.albedo=lerp(surface.albedo,metalColour,metalMask);
                    surface.specular=lerp(surface.specular,metalColour*.65,metalMask);
                    surface.smoothness=lerp(surface.smoothness,.5,metalMask);
                    surface.emission=lerp(surface.emission,_DeskEdgeColour.rgb*.12,metalMask);
                }
                // Derivatives keep the soft inner band at output-pixel width,
                // including nine-sliced paper. HDR Bloom carries it past the
                // silhouette without enlarging geometry or painting hard rims.
                float2 dx=ddx(input.uv)*_DeskEdgeWidth,dy=ddy(input.uv)*_DeskEdgeWidth;
                half neighbours=DeskAlpha(input.uv+dx)+DeskAlpha(input.uv-dx)
                    +DeskAlpha(input.uv+dy)+DeskAlpha(input.uv-dy)
                    +DeskAlpha(input.uv+(dx+dy)*.7071)+DeskAlpha(input.uv-(dx+dy)*.7071)
                    +DeskAlpha(input.uv+(dx-dy)*.7071)+DeskAlpha(input.uv-(dx-dy)*.7071);
                half rim=saturate(source.a-neighbours*.125);
                if(_DeskPlain>.5)
                {
                    float2 p=TransformWorldToObject(input.positionWS).xy;
                    float2 unit=max(fwidth(p),float2(.00001,.00001));
                    float2 distanceToEdge=min(p-_DeskRect.xy,_DeskRect.zw-p)/unit;
                    rim=1-smoothstep(0,_DeskEdgeWidth,min(distanceToEdge.x,distanceToEdge.y));
                }
                surface.emission+=_DeskEdgeColour.rgb*_DeskEdgeStrength*rim;
                #endif
                InputData data=(InputData)0;
                data.positionWS=input.positionWS;data.positionCS=input.positionCS;
                data.normalWS=n;data.viewDirectionWS=GetWorldSpaceNormalizeViewDir(input.positionWS);
                data.shadowCoord=TransformWorldToShadowCoord(input.positionWS);
                data.bakedGI=SampleSH(n);data.shadowMask=half4(1,1,1,1);
                data.vertexLighting=VertexLighting(input.positionWS,n);
                data.normalizedScreenSpaceUV=GetNormalizedScreenSpaceUV(input.positionCS);
                half4 result=UniversalFragmentPBR(data,surface);
                result.rgb=MixFog(result.rgb,ComputeFogFactor(input.positionCS.z));
                return result;
            }
            ENDHLSL
        }
        Pass
        {
            Name "ShadowCaster"
            Tags {"LightMode"="ShadowCaster"}
            Cull [_Cull] ZWrite On ZTest LEqual ColorMask 0
            HLSLPROGRAM
            #pragma vertex ShadowVert
            #pragma fragment DepthFrag
            #pragma shader_feature_local_fragment _ALPHATEST_ON
            #pragma multi_compile_vertex _ _CASTING_PUNCTUAL_LIGHT_SHADOW
            float3 _LightDirection, _LightPosition;
            Varyings ShadowVert(Attributes input)
            {
                Varyings o=Vert(input);
                #if defined(_CASTING_PUNCTUAL_LIGHT_SHADOW)
                float3 direction=normalize(_LightPosition-o.positionWS);
                #else
                float3 direction=_LightDirection;
                #endif
                o.positionCS=TransformWorldToHClip(ApplyShadowBias(o.positionWS,o.normalWS,direction));
                #if UNITY_REVERSED_Z
                o.positionCS.z=min(o.positionCS.z,UNITY_NEAR_CLIP_VALUE*o.positionCS.w);
                #else
                o.positionCS.z=max(o.positionCS.z,UNITY_NEAR_CLIP_VALUE*o.positionCS.w);
                #endif
                return o;
            }
            half4 DepthFrag(Varyings input):SV_Target
            {
                #if defined(_ALPHATEST_ON)
                clip(SAMPLE_TEXTURE2D(_BaseMap,sampler_BaseMap,input.uv).a*_BaseColor.a-_Cutoff);
                #endif
                return 0;
            }
            ENDHLSL
        }
        Pass
        {
            Name "DepthOnly"
            Tags {"LightMode"="DepthOnly"}
            Cull [_Cull] ZWrite On ColorMask R
            HLSLPROGRAM
            #pragma vertex Vert
            #pragma fragment DepthFrag
            #pragma shader_feature_local_fragment _ALPHATEST_ON
            half4 DepthFrag(Varyings input):SV_Target
            {
                #if defined(_ALPHATEST_ON)
                clip(SAMPLE_TEXTURE2D(_BaseMap,sampler_BaseMap,input.uv).a*_BaseColor.a-_Cutoff);
                #endif
                return input.positionCS.z;
            }
            ENDHLSL
        }
    }
    Fallback Off
}

