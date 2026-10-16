Shader "OCC/HD2D/ContactShadow"
{
    Properties
    {
        [MainTexture] _BaseMap ("Native silhouette", 2D) = "white" {}
        [MainColor] _BaseColor ("Contact tint", Color) = (0.055,0.035,0.025,0.24)
    }
    SubShader
    {
        Tags { "RenderPipeline"="UniversalPipeline" "Queue"="Transparent" "RenderType"="Transparent" }
        Pass
        {
            Blend SrcAlpha OneMinusSrcAlpha
            ZWrite Off
            Cull Off
            HLSLPROGRAM
            #pragma vertex Vert
            #pragma fragment Frag
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
            TEXTURE2D(_BaseMap); SAMPLER(sampler_BaseMap);
            CBUFFER_START(UnityPerMaterial)
            float4 _BaseMap_ST;
            half4 _BaseColor;
            CBUFFER_END
            struct Attributes { float4 positionOS:POSITION; float2 uv:TEXCOORD0; };
            struct Varyings { float4 positionCS:SV_POSITION; float2 uv:TEXCOORD0; };
            Varyings Vert(Attributes input)
            {
                Varyings output;
                output.positionCS=TransformObjectToHClip(input.positionOS.xyz);
                output.uv=TRANSFORM_TEX(input.uv,_BaseMap);
                return output;
            }
            half4 Frag(Varyings input):SV_Target
            {
                half alpha=SAMPLE_TEXTURE2D(_BaseMap,sampler_BaseMap,input.uv).a;
                return half4(_BaseColor.rgb,_BaseColor.a*alpha);
            }
            ENDHLSL
        }
    }
}
