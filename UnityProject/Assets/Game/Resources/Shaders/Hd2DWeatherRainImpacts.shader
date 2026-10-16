Shader "OCC/HD2D/WeatherRainImpacts"
{
    Properties { _WeatherTime("Rain time",Float)=0 _Roof("Rain height",Float)=5.5 _Floor("Rain wrap floor",Float)=-.5 _RainAmount("Rain amount",Range(0,1))=1 }
    SubShader
    {
        Tags {"RenderPipeline"="UniversalPipeline" "RenderType"="Transparent" "Queue"="Transparent"}
        Pass
        {
            Name "LitNativeRainContact"
            Tags {"LightMode"="UniversalForward"}
            Blend SrcAlpha OneMinusSrcAlpha
            Cull Off ZWrite Off ZTest LEqual
            HLSLPROGRAM
            #pragma target 3.0
            #pragma vertex ContactVertex
            #pragma fragment ContactFragment
            #pragma multi_compile _ _MAIN_LIGHT_SHADOWS _MAIN_LIGHT_SHADOWS_CASCADE _MAIN_LIGHT_SHADOWS_SCREEN
            #pragma multi_compile _ _SHADOWS_SOFT _SHADOWS_SOFT_LOW _SHADOWS_SOFT_MEDIUM _SHADOWS_SOFT_HIGH
            #pragma multi_compile _ _ADDITIONAL_LIGHTS_VERTEX _ADDITIONAL_LIGHTS
            #pragma multi_compile _ _CLUSTER_LIGHT_LOOP
            #pragma multi_compile_fragment _ _LIGHT_COOKIES
            #pragma multi_compile _ _REFLECTION_PROBE_BLENDING
            #pragma multi_compile _ _REFLECTION_PROBE_BOX_PROJECTION
            #define _SURFACE_TYPE_TRANSPARENT 1
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Lighting.hlsl"
            CBUFFER_START(UnityPerMaterial)
                float _WeatherTime,_Roof,_Floor,_RainAmount;
            CBUFFER_END
            struct Attributes {float4 positionOS:POSITION;float3 normalOS:NORMAL;float2 piece:TEXCOORD0;float4 contact:TEXCOORD1;float2 corner:TEXCOORD2;};
            struct Varyings {float4 positionCS:SV_POSITION;float3 world:TEXCOORD0;half3 normal:TEXCOORD1;float3 life:TEXCOORD2;};
            Varyings ContactVertex(Attributes input)
            {
                const float pitch=1.0/32.0;
                float span=_Roof-_Floor;
                // Exactly the same rain phase reaches exactly the sampled surface.
                float age=frac((_WeatherTime*6+input.contact.y-_Floor)/span-input.contact.x)*span/6;
                float water=input.piece.y;
                float life=age/(water>.5?.28:.16);
                float3 p=input.positionOS.xyz;
                p.y+=input.contact.y+.003;
                if(water>.5)
                {
                    float radius=1+min(1,floor(life*2));
                    if(input.piece.x<.5||input.piece.x>1.5&&input.piece.x<2.5)
                    {
                        p.x+=input.corner.x*(2*radius+1)*pitch;
                        p.z+=((input.piece.x<.5?1:-1)*radius+input.corner.y)*pitch;
                    }
                    else
                    {
                        p.x+=((input.piece.x<1.5?1:-1)*radius+input.corner.x)*pitch;
                        p.z+=input.corner.y*(2*radius-1)*pitch;
                    }
                }
                else
                {
                    float step=min(4,floor(life*5));
                    p.y+=(.5+(step<3?step:4-step))*pitch;
                    p.x+=input.piece.x*min(2,step)*pitch;
                }
                Varyings output;output.world=TransformObjectToWorld(p);output.positionCS=TransformWorldToHClip(output.world);
                output.normal=TransformObjectToWorldNormal(input.normalOS);
                output.life=float3(life,water,water>.5?input.contact.z:1-input.contact.z);return output;
            }
            half4 ContactFragment(Varyings input):SV_Target
            {
                clip(1-input.life.x);clip(input.life.z-.5);clip(_RainAmount-.001);
                InputData data=(InputData)0;data.positionWS=input.world;data.normalWS=normalize(input.normal);
                data.viewDirectionWS=GetWorldSpaceNormalizeViewDir(input.world);data.shadowCoord=TransformWorldToShadowCoord(input.world);
                data.bakedGI=SampleSH(data.normalWS);data.shadowMask=half4(1,1,1,1);
                #if defined(_ADDITIONAL_LIGHTS_VERTEX)
                data.vertexLighting=VertexLighting(input.world,data.normalWS);
                #endif
                data.normalizedScreenSpaceUV=GetNormalizedScreenSpaceUV(input.positionCS);
                SurfaceData surface=(SurfaceData)0;surface.albedo=half3(.42,.52,.62);surface.alpha=1;surface.occlusion=1;
                surface.smoothness=lerp(.08,.65,input.life.y);
                half4 colour=UniversalFragmentPBR(data,surface);
                return half4(colour.rgb,(1-input.life.x)*saturate(_RainAmount)*.42);
            }
            ENDHLSL
        }
    }
}
