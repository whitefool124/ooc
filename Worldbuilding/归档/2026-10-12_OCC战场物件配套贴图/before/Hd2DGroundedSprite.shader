Shader "OCC/HD2D/GroundedSprite"
{
    Properties
    {
        [MainTexture] _BaseMap("Native sprite",2D)="white"{}
        [MainColor] _BaseColor("Tint",Color)=(1,1,1,1)
        _Cutoff("Alpha cutoff",Range(0,1))=0.5
        _Smoothness("Smoothness",Range(0,1))=0
        _Metallic("Metallic",Range(0,1))=0
        _SpecColor("Specular",Color)=(0.2,0.2,0.2,1)
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
            #pragma fragment LitPassFragment
            #pragma shader_feature_local _ALPHATEST_ON
            #pragma shader_feature_local _RECEIVE_SHADOWS_OFF
            #pragma multi_compile _ _ADDITIONAL_LIGHTS_VERTEX _ADDITIONAL_LIGHTS
            #pragma multi_compile _ _CLUSTER_LIGHT_LOOP
            #pragma multi_compile _ EVALUATE_SH_MIXED EVALUATE_SH_VERTEX
            #pragma multi_compile_fog
            #include "Packages/com.unity.render-pipelines.universal/Shaders/LitForwardPass.hlsl"
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
