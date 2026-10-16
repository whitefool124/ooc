Shader "OCC/HD2D/GroundedSprite"
{
    Properties
    {
        [MainTexture] _BaseMap("Native sprite",2D)="white"{}
        [MainColor] _BaseColor("Tint",Color)=(1,1,1,1)
        _Cutoff("Alpha cutoff",Range(0,1))=0.5
        _Smoothness("Smoothness",Range(0,1))=0
        _Metallic("Metallic",Range(0,1))=0
        _MetallicGlossMap("Registered metal (R), smoothness (A)",2D)="white"{}
        _SmoothnessTextureChannel("Smoothness texture channel",Float)=0
        _SpecColor("Specular",Color)=(0.2,0.2,0.2,1)
        _BumpMap("Registered normal",2D)="bump"{}
        _RoughnessMap("Registered roughness (R)",2D)="white"{}
        _MaterialMask("Metal (R), glass coat (G)",2D)="black"{}
        _FoliageData("Registered leaf scatter (R), indirect visibility (G)",2D)="black"{}
        _ClearCoatMask("Glass coat",Range(0,1))=0
        _ClearCoatSmoothness("Glass coat smoothness",Range(0,1))=.94
        _BumpScale("Normal scale",Float)=1
        _OcclusionStrength("Occlusion",Range(0,1))=1
        _EmissionColor("Emission",Color)=(0,0,0,0)
        _ReceiveShadows("Receive shadows",Float)=0
        _Cull("Cull",Float)=0
    }
    SubShader
    {
        Tags {"RenderPipeline"="UniversalPipeline" "RenderType"="TransparentCutout" "Queue"="AlphaTest"}
        HLSLINCLUDE
        #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
        // Keep the screen-facing silhouette, but use an upright foot-anchored proxy for depth.
        // This prevents a rear wall slicing through the middle of a leaning sprite.
        float4 GroundedDepth(float4 clipPosition,float3 positionOS)
        {
            float3 origin=TransformObjectToWorld(float3(0,0,0));
            float3 horizontal=mul((float3x3)unity_ObjectToWorld,float3(positionOS.x,0,0));
            float heightScale=length(mul((float3x3)unity_ObjectToWorld,float3(0,1,0)));
            float4 proxy=TransformWorldToHClip(origin+horizontal+float3(0,positionOS.y*heightScale,0));
            clipPosition.z=(proxy.z/proxy.w)*clipPosition.w;
            return clipPosition;
        }
        ENDHLSL
        Pass
        {
            Name "ForwardLit"
            Tags {"LightMode"="UniversalForward"}
            Cull Off ZWrite On
            HLSLPROGRAM
            #pragma target 3.0
            #pragma vertex GroundedVertex
            #pragma fragment GroundedFragment
            #pragma shader_feature_local _NORMALMAP
            #pragma shader_feature_local_fragment _OCC_PIXEL_EMISSION
            #pragma target 4.5 _OCC_PIXEL_EMISSION
            #pragma shader_feature_local _METALLICSPECGLOSSMAP
            #pragma shader_feature_local _OCC_PROP_DATA
            #pragma shader_feature_local _OCC_FOLIAGE_DATA
            #pragma shader_feature_local _OCC_UNIFORM_SURFACE
            #pragma shader_feature_local _OCC_CRYSTAL_REGIONS
            #pragma shader_feature_local _ALPHATEST_ON
            #pragma shader_feature_local _RECEIVE_SHADOWS_OFF
            #pragma multi_compile _ _ADDITIONAL_LIGHTS_VERTEX _ADDITIONAL_LIGHTS
            #pragma multi_compile _ _CLUSTER_LIGHT_LOOP
            #pragma multi_compile_fragment _ _LIGHT_COOKIES
            #pragma multi_compile_fragment _ _SCREEN_SPACE_OCCLUSION
            #pragma multi_compile _ EVALUATE_SH_MIXED EVALUATE_SH_VERTEX
            #pragma multi_compile _ _REFLECTION_PROBE_BLENDING
            #pragma multi_compile _ _REFLECTION_PROBE_BOX_PROJECTION
            #pragma multi_compile _ _REFLECTION_PROBE_ATLAS
            #pragma multi_compile_fog
            #if defined(_OCC_PROP_DATA)
            #define _CLEARCOAT 1
            #endif
            #if defined(_OCC_CRYSTAL_REGIONS)
            // The crystal's owned point light illuminates its surroundings. Its
            // near-zero distance is not a surface irradiance model; the crystal
            // itself uses key light, local environment and authored emission.
            #undef _ADDITIONAL_LIGHTS
            #undef _ADDITIONAL_LIGHTS_VERTEX
            #endif
            #include "Packages/com.unity.render-pipelines.universal/Shaders/LitForwardPass.hlsl"
            #include "Hd2DNativePixelEmission.hlsl"
            TEXTURE2D(_RoughnessMap);SAMPLER(sampler_RoughnessMap);
            TEXTURE2D(_MaterialMask);SAMPLER(sampler_MaterialMask);
            TEXTURE2D(_FoliageData);SAMPLER(sampler_FoliageData);
            half4 GroundedFragment(Varyings input):SV_Target
            {
                #if !defined(_OCC_PROP_DATA) && !defined(_OCC_FOLIAGE_DATA) && !defined(_OCC_PIXEL_EMISSION)
                half4 baseColour;
                #if defined(_WRITE_RENDERING_LAYERS)
                uint renderingLayers;LitPassFragment(input,baseColour,renderingLayers);
                #else
                LitPassFragment(input,baseColour);
                #endif
                return baseColour;
                #else
                SurfaceData surface;InitializeStandardLitSurfaceData(input.uv,surface);
                #if defined(_OCC_PIXEL_EMISSION)
                surface.emission+=NativePixelEmission(input.uv,surface.albedo);
                #endif
                #if defined(_OCC_PROP_DATA)
                half3 regions=SAMPLE_TEXTURE2D(_MaterialMask,sampler_MaterialMask,input.uv).rgb;
                #if !defined(_OCC_UNIFORM_SURFACE)
                surface.smoothness=1.h-saturate(SAMPLE_TEXTURE2D(_RoughnessMap,sampler_RoughnessMap,input.uv).r);
                #endif
                surface.metallic=max(surface.metallic,saturate(regions.r));
                surface.clearCoatMask=max(surface.clearCoatMask,saturate(regions.g));surface.clearCoatSmoothness=_ClearCoatSmoothness;
                #if defined(_OCC_CRYSTAL_REGIONS)
                // Crystal and its stone support are explicitly dielectric. The
                // unused R channel cannot turn model-generated mask noise metallic.
                surface.metallic=0.h;surface.smoothness*=saturate(regions.g);
                surface.emission+=saturate(regions.b)*_EmissionColor.rgb;
                #endif
                #endif
                #if defined(_OCC_FOLIAGE_DATA)
                half2 leaf=SAMPLE_TEXTURE2D(_FoliageData,sampler_FoliageData,input.uv).rg;
                surface.occlusion*=leaf.g;
                #endif
                InputData data;InitializeInputData(input,surface.normalTS,data);InitializeBakedGIData(input,data);
                half4 colour=UniversalFragmentPBR(data,surface);
                #if defined(_OCC_FOLIAGE_DATA)
                // Restrained wrapped diffuse for thin leaves. It depends on the
                // owned key light and its cookie; the wood region has zero scatter.
                Light key=GetMainLight(data.shadowCoord,data.positionWS,data.shadowMask);
                half ndl=dot(data.normalWS,key.direction);
                half wrap=max(0.h,saturate((ndl+.35h)/1.35h)-saturate(ndl));
                colour.rgb+=surface.albedo*key.color*(wrap*leaf.r*key.distanceAttenuation*key.shadowAttenuation);
                #endif
                colour.rgb=MixFog(colour.rgb,data.fogCoord);
                colour.a=OutputAlpha(colour.a,IsSurfaceTypeTransparent());return colour;
                #endif
            }
            Varyings GroundedVertex(Attributes input)
            {
                Varyings output=LitPassVertex(input);
                output.positionCS=GroundedDepth(output.positionCS,input.positionOS.xyz);
                return output;
            }
            ENDHLSL
        }
        Pass
        {
            Name "DepthOnly"
            Tags {"LightMode"="DepthOnly"}
            Cull Off ZWrite On ColorMask R
            HLSLPROGRAM
            #pragma target 3.0
            #pragma vertex GroundedDepthVertex
            #pragma fragment DepthOnlyFragment
            #pragma shader_feature_local _ALPHATEST_ON
            #include "Packages/com.unity.render-pipelines.universal/Shaders/LitInput.hlsl"
            #include "Packages/com.unity.render-pipelines.universal/Shaders/DepthOnlyPass.hlsl"
            Varyings GroundedDepthVertex(Attributes input)
            {
                Varyings output=DepthOnlyVertex(input);
                output.positionCS=GroundedDepth(output.positionCS,input.position.xyz);
                return output;
            }
            ENDHLSL
        }
    }
}
