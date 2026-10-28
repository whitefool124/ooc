from pathlib import Path
src=Path('UnityProject/Assets/Game/Resources/Art/MainMenuE/Hd2D/Hd2DSurface.shader').read_text(encoding='utf8')
src=src.replace('Shader "OCC/HD2D/Surface"','Shader "OCC/Research/OctopathSurface"')
src=src.replace('        _Smoothness("Smoothness", Range(0,1)) = .1','''        _GradientBottom("Original bottom gradient", Color) = (1,1,1,1)
        _GradientTop("Original top gradient", Color) = (1,1,1,1)
        _GradientPos("Original gradient offset cm", Float) = 0
        _GradientScale("Original gradient height cm", Float) = 200
        _UseGradient("Use original gradient", Float) = 0
        _UseCombinedUV("Original combined colour UV", Float) = 0
        _Smoothness("Smoothness", Range(0,1)) = .1''')
src=src.replace('        CBUFFER_END','''        half4 _GradientBottom, _GradientTop;
        half _GradientPos, _GradientScale, _UseGradient, _UseCombinedUV;
        CBUFFER_END''')
src=src.replace('float2 uv:TEXCOORD0;};','float2 uv:TEXCOORD0; float2 uv1:TEXCOORD1;};',1)
src=src.replace('float2 uv:TEXCOORD2;};','float2 uv:TEXCOORD2; float2 normalUV:TEXCOORD3; float height:TEXCOORD4;};',1)
src=src.replace('            o.uv=TRANSFORM_TEX(input.uv,_BaseMap);','''            o.uv=TRANSFORM_TEX(lerp(input.uv,input.uv1,_UseCombinedUV),_BaseMap);
            o.normalUV=input.uv;
            o.height=input.positionOS.y*100;''')
src=src.replace('                surface.albedo=colour.rgb;','''                surface.albedo=colour.rgb;
                if(_UseGradient>.5)surface.albedo*=lerp(_GradientBottom.rgb,_GradientTop.rgb,saturate((input.height-_GradientPos)/max(_GradientScale,1)));''')
src=src.replace('float2 ux=ddx(input.uv),uy=ddy(input.uv);','float2 ux=ddx(input.normalUV),uy=ddy(input.normalUV);')
src=src.replace('half3 normalTS=UnpackNormalScale(SAMPLE_TEXTURE2D(_NormalMap,sampler_NormalMap,input.uv),_NormalStrength);','''half2 normalXY=(SAMPLE_TEXTURE2D(_NormalMap,sampler_NormalMap,input.normalUV).rg*2-1)*half2(1,-1)*_NormalStrength;
                        half3 normalTS=half3(normalXY,sqrt(saturate(1-dot(normalXY,normalXY))));''')
Path('UnityProject/Assets/Game/Editor/OctopathStudySurface.shader').write_text(src,encoding='utf8')
