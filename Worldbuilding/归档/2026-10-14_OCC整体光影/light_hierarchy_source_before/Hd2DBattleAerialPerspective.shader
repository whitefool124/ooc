Shader "OCC/HD2D/BattleAerialPerspective"
{
    Properties
    {
        _BattleBounds("World centre XZ and half extents",Vector)=(0,0,0,0)
        _AirColour("Linear aerial radiance",Color)=(0,0,0,1)
        _AirSettings("Density, opacity cap, exterior dimming, safe margin",Vector)=(0,0,0,0)
        _AirLightSettings("Ray density, scattering strength, height falloff, ray length",Vector)=(0,0,0,0)
        _AirOriginY("Owned stage ground height",Float)=0
    }
    SubShader
    {
        Tags { "RenderPipeline"="UniversalPipeline" }
        Pass
        {
            Name "ExteriorAir"
            Cull Off ZWrite Off ZTest Always
            // Premultiplied scattering + background transmittance. Preserve alpha.
            Blend One SrcAlpha, Zero One
            ColorMask RGB
            HLSLPROGRAM
            #pragma target 3.5
            #pragma vertex Vert
            #pragma fragment ExteriorAir
            #pragma multi_compile _ _MAIN_LIGHT_SHADOWS _MAIN_LIGHT_SHADOWS_CASCADE _MAIN_LIGHT_SHADOWS_SCREEN
            #pragma multi_compile_fragment _ _LIGHT_COOKIES
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
            #include "Packages/com.unity.render-pipelines.core/Runtime/Utilities/Blit.hlsl"
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/DeclareDepthTexture.hlsl"
            // A participating medium samples the shadow atlas in world space,
            // including when opaque surfaces use screen-space shadows.
            #define _SURFACE_TYPE_TRANSPARENT 1
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/RealtimeLights.hlsl"
            CBUFFER_START(UnityPerMaterial)
                float4 _BattleBounds;
                half4 _AirColour;
                float4 _AirSettings;
                float4 _AirLightSettings;
                float _AirOriginY;
            CBUFFER_END
            half4 ExteriorAir(Varyings input):SV_Target
            {
                UNITY_SETUP_STEREO_EYE_INDEX_POST_VERTEX(input);
                float2 uv=input.texcoord;
                float depth=SampleSceneDepth(uv);
                #if UNITY_REVERSED_Z
                    if(depth<=.00001)return half4(0,0,0,1);
                #else
                    if(depth>=.99999)return half4(0,0,0,1);
                    depth=lerp(UNITY_NEAR_CLIP_VALUE,1,depth);
                #endif
                float3 position=ComputeWorldSpacePosition(uv,depth,UNITY_MATRIX_I_VP);
                // Depth follows the existing foot-anchored card proxy. Keep the
                // complete logical board plus a margin free of fog and dimming.
                float2 outside=max(abs(position.xz-_BattleBounds.xy)-_BattleBounds.zw-_AirSettings.w,0);
                float distanceOutside=length(outside);
                float exterior=smoothstep(0,3,distanceOutside);
                if(exterior<=.0001)return half4(0,0,0,1);
                float rear=max(position.z-(_BattleBounds.y+_BattleBounds.w+_AirSettings.w),0);
                half opacity=min(_AirSettings.y,1-exp2(-_AirSettings.x*(rear+distanceOutside*.18)))*exterior;
                half surroundingLight=1-_AirSettings.z*smoothstep(0,6,distanceOutside);
                half3 scatter=0;half transmittance=1;
                if(_AirLightSettings.y>.0001)
                {
                    float3 eye=GetCameraPositionWS();float3 ray=position-eye;
                    float rayLength=min(length(ray),_AirLightSettings.w);float3 direction=normalize(ray);
                    float3 start=position-direction*rayLength;float stride=rayLength/16;
                    Light key=GetMainLight();
                    // Henyey-Greenstein approximation, restrained forward bias.
                    half cosine=dot(direction,-key.direction);const half g=.35h;
                    half phase=.5h*(1-g*g)/pow(max(.05h,1+g*g-2*g*cosine),1.5h);
                    [loop] for(int i=0;i<16;i++)
                    {
                        float3 p=start+direction*((i+.5)*stride);
                        float density=_AirLightSettings.x*exp2(-max(0,p.y-_AirOriginY)*_AirLightSettings.z);
                        half segment=exp2(-density*stride);
                        Light sampleLight=GetMainLight(TransformWorldToShadowCoord(p),p,half4(1,1,1,1));
                        scatter+=transmittance*(1-segment)*sampleLight.color*
                            (sampleLight.shadowAttenuation*sampleLight.distanceAttenuation*phase*_AirLightSettings.y);
                        transmittance*=segment;
                    }
                    scatter*=exterior;transmittance=lerp(1.h,transmittance,exterior);
                }
                return half4(_AirColour.rgb*opacity*transmittance+scatter,(1-opacity)*surroundingLight*transmittance);
            }
            ENDHLSL
        }
    }
}
