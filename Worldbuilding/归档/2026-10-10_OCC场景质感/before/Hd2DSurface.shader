Shader "OCC/HD2D/Surface"
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
        _CoatMask("Varnish layer", Range(0,1)) = 0
        _CoatSmoothness("Varnish smoothness", Range(0,1)) = .85
        _ReliefStrength("Source grain relief", Range(0,2)) = 0
        _RoughnessVariation("Source grain roughness", Range(0,.5)) = 0
        _HeightUvBounds("Grain patch bounds", Vector) = (0,0,1,1)
        _CrystalMask("Crystal and brass response", Float) = 0
        [HDR] _EmissionColor("Crystal emission", Color) = (.35,1,1.5,1)
        _EmissionStrength("Crystal output", Float) = 0
        _SoftEmission("Albedo readability emission", Range(0,.3)) = 0
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
        TEXTURE2D(_NormalMap); SAMPLER(sampler_NormalMap);
        TEXTURE2D(_MaterialMap); SAMPLER(sampler_MaterialMap);
        float4 _BaseMap_TexelSize;
        CBUFFER_START(UnityPerMaterial)
        float4 _BaseMap_ST, _HeightUvBounds;
        half4 _BaseColor, _SpecColor, _EmissionColor;
        half _Smoothness, _Metallic, _ReliefStrength, _RoughnessVariation;
        half _CrystalMask, _EmissionStrength, _AlphaClip, _Cutoff, _Cull;
        half _SoftEmission;
        half _UseNormalMap, _UseMaterialMap, _NormalStrength, _CoatMask, _CoatSmoothness;
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
            #pragma multi_compile _ _MAIN_LIGHT_SHADOWS _MAIN_LIGHT_SHADOWS_CASCADE _MAIN_LIGHT_SHADOWS_SCREEN
            #pragma multi_compile _ _ADDITIONAL_LIGHTS_VERTEX _ADDITIONAL_LIGHTS
            #pragma multi_compile_fragment _ _ADDITIONAL_LIGHT_SHADOWS
            #pragma multi_compile_fragment _ _SHADOWS_SOFT _SHADOWS_SOFT_LOW _SHADOWS_SOFT_MEDIUM _SHADOWS_SOFT_HIGH
            #pragma multi_compile_fragment _ _REFLECTION_PROBE_BLENDING
            #pragma multi_compile_fragment _ _REFLECTION_PROBE_BOX_PROJECTION
            #pragma multi_compile_fragment _ _REFLECTION_PROBE_ATLAS
            #pragma multi_compile_fog
            half4 Frag(Varyings input, FRONT_FACE_TYPE face:FRONT_FACE_SEMANTIC):SV_Target
            {
                half4 source=SAMPLE_TEXTURE2D(_BaseMap,sampler_BaseMap,input.uv);
                half4 colour=source*_BaseColor;
                #if defined(_ALPHATEST_ON)
                clip(colour.a-_Cutoff);
                #endif
                half3 n=normalize(input.normalWS)*IS_FRONT_VFACE(face,1,-1);
                if(_ReliefStrength>.001 || _UseNormalMap>.5)
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
                }
                half crystal=saturate((source.b-source.r-.025)*10)*step(.12,source.g)*_CrystalMask;
                half brass=step(source.b*1.2+.04,source.r)*step(.18,source.r)*_CrystalMask;
                half metal=max(_Metallic,brass*.85);
                SurfaceData surface=(SurfaceData)0;
                surface.albedo=colour.rgb;
                surface.specular=lerp(_SpecColor.rgb,colour.rgb,metal);
                surface.smoothness=saturate(_Smoothness+(dot(source.rgb,half3(.2126,.7152,.0722))-.35)*_RoughnessVariation);
                surface.smoothness=lerp(surface.smoothness,.72,max(brass,crystal));
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
