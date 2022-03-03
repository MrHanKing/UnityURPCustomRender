#ifndef CUSTOM_UNLIT_PASS_INCLUDED
#define CUSTOM_UNLIT_PASS_INCLUDED

#include "../ShaderLibrary/Common.hlsl"

// SRP 合批 (多个材质球 相同shader 不附带额外ObjectMaterialProperties脚本 才会合批)
CBUFFER_START(UnityPerMaterial)
float4 _BaseColor;
CBUFFER_END

// 顶点输入
struct Attributes{
    float3 positionOS : POSITION;
};
// 顶点输出
struct Varyings{
    float4 positionCS : SV_POSITION;
};

Varyings UnlitPassVertex(Attributes input) {
    Varyings output;
    output.positionCS = TransformObjectToHClip(input.positionOS.xyz);
    return output;
}

float4 UnlitPassFragment(Varyings input) : SV_TARGET{
    return _BaseColor;
}

#endif