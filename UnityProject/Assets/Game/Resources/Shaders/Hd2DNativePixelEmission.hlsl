#ifndef OCC_NATIVE_PIXEL_EMISSION_INCLUDED
#define OCC_NATIVE_PIXEL_EMISSION_INCLUDED
#if defined(_OCC_PIXEL_EMISSION)
// The buffer contains per-original-texel gain, never replacement RGB artwork.
StructuredBuffer<float4> _PixelEmissionBuffer;
half3 NativePixelEmission(float2 uv,half3 originalPixel)
{
    uint2 size=max((uint2)_BaseMap_TexelSize.zw,uint2(1,1));
    uint2 texel=min((uint2)(frac(uv)*size),size-1);
    return originalPixel*_PixelEmissionBuffer[texel.y*size.x+texel.x].rgb;
}
#endif
#endif
