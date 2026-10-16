Shader "OCC/HD2D/VfxGroundRing"
{
    Properties
    {
        [HDR] _Tint("HDR Tint",Color)=(5,1,.04,1)
        _Width("Ring Width",Range(.001,.2))=.025
    }
    SubShader
    {
        Tags { "RenderPipeline"="UniversalPipeline" "RenderType"="Transparent" "Queue"="Transparent" }
        Pass
        {
            Blend SrcAlpha One
            ZWrite Off
            ZTest LEqual
            Cull Off
            HLSLPROGRAM
            #pragma vertex Vert
            #pragma fragment Frag
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
            struct Attributes { float4 positionOS:POSITION; float2 uv:TEXCOORD0; };
            struct Varyings { float4 positionCS:SV_POSITION;float2 uv:TEXCOORD0; };
            CBUFFER_START(UnityPerMaterial)
            float4 _Tint;
            float _Width;
            CBUFFER_END
            Varyings Vert(Attributes v){Varyings o;o.positionCS=TransformObjectToHClip(v.positionOS.xyz);o.uv=v.uv;return o;}
            half4 Frag(Varyings v):SV_Target
            {
                float2 p=v.uv-.5;
                float distance=abs(length(p)-.43);
                float edge=max(fwidth(distance),.001);
                float alpha=1-smoothstep(_Width,_Width+edge,distance);
                float angle=atan2(p.y,p.x);
                float segments=.7+.3*step(.05,sin(angle*16));
                return half4(_Tint.rgb,alpha*_Tint.a*segments);
            }
            ENDHLSL
        }
    }
}
