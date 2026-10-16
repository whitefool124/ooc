Shader "OCC/HD2D/VfxLitSmoke"
{
    Properties { [MainTexture] _MainTex("Existing prototype smoke",2D)="white"{} [MainColor] _Color("Tint",Color)=(1,1,1,1) }
    SubShader
    {
        Tags { "RenderPipeline"="UniversalPipeline" "Queue"="Transparent" "RenderType"="Transparent" }
        Pass
        {
            Tags { "LightMode"="UniversalForward" }
            Blend SrcAlpha OneMinusSrcAlpha
            ZWrite Off
            ZTest LEqual
            Cull Off
            HLSLPROGRAM
            #pragma vertex Vert
            #pragma fragment Frag
            #pragma multi_compile _ _ADDITIONAL_LIGHTS_VERTEX _ADDITIONAL_LIGHTS
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Lighting.hlsl"
            struct Attributes { float4 positionOS:POSITION;float2 uv:TEXCOORD0;half4 color:COLOR; };
            struct Varyings { float4 positionCS:SV_POSITION;float3 positionWS:TEXCOORD1;float2 uv:TEXCOORD0;half4 color:COLOR; };
            TEXTURE2D(_MainTex);SAMPLER(sampler_MainTex);
            CBUFFER_START(UnityPerMaterial)
            float4 _MainTex_ST;
            half4 _Color;
            CBUFFER_END
            Varyings Vert(Attributes v){Varyings o;o.positionWS=TransformObjectToWorld(v.positionOS.xyz);o.positionCS=TransformWorldToHClip(o.positionWS);o.uv=v.uv*_MainTex_ST.xy+_MainTex_ST.zw;o.color=v.color*_Color;return o;}
            half4 Frag(Varyings v):SV_Target
            {
                half4 tex=SAMPLE_TEXTURE2D(_MainTex,sampler_MainTex,v.uv)*v.color;
                float3 normalWS=GetWorldSpaceNormalizeViewDir(v.positionWS);
                Light key=GetMainLight();
                half3 lighting=half3(.12,.14,.18)+key.color*(.25+.5*saturate(dot(normalWS,key.direction)));
                // Billboard approximation, not volumetric scattering. Receives the real burst light.
                #if defined(_ADDITIONAL_LIGHTS) || defined(_ADDITIONAL_LIGHTS_VERTEX)
                uint count=GetAdditionalLightsCount();
                for(uint index=0;index<count;index++)
                {
                    Light light=GetAdditionalLight(index,v.positionWS);
                    lighting+=light.color*light.distanceAttenuation*(.3+.45*saturate(dot(normalWS,light.direction)));
                }
                #endif
                return half4(tex.rgb*lighting,tex.a);
            }
            ENDHLSL
        }
    }
}
