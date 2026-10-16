Shader "OCC/HD2D/WeatherPreviewRain"
{
    Properties { _WeatherTime("Rain time",Float)=0 _Roof("Rain height",Float)=5.5 _Floor("Rain wrap floor",Float)=-.5 _RainAmount("Rain amount",Range(0,1))=1 }
    SubShader
    {
        Tags { "RenderPipeline"="UniversalPipeline" "RenderType"="Transparent" "Queue"="Transparent" }
        Pass
        {
            Tags { "LightMode"="SRPDefaultUnlit" }
            Blend SrcAlpha OneMinusSrcAlpha
            ZWrite Off ZTest LEqual Cull Off
            HLSLPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
            CBUFFER_START(UnityPerMaterial)
            float _WeatherTime; float _Roof; float _Floor; float _RainAmount;
            CBUFFER_END
            struct A {float4 positionOS:POSITION;float2 uv:TEXCOORD0;float4 contact:TEXCOORD1;};
            struct V {float4 positionCS:SV_POSITION;float2 uv:TEXCOORD0;float aboveSurface:TEXCOORD1;};
            V vert(A a)
            {
                V o;float3 p=a.positionOS.xyz;
                // Keep each quad together while wrapping its root, preserving streak length.
                float span=_Roof-_Floor;
                p.y=_Floor+frac(a.contact.x-_WeatherTime*6/span)*span+a.uv.y*.28;
                o.aboveSurface=p.y-a.contact.y;
                o.positionCS=TransformObjectToHClip(p);o.uv=a.uv;return o;
            }
            half4 frag(V i):SV_Target
            {clip(i.aboveSurface-.003);return half4(.47,.64,.86,.30*saturate(_RainAmount)*(1-abs(i.uv.x*2-1))*(.4+.6*i.uv.y));}
            ENDHLSL
        }
    }
}
