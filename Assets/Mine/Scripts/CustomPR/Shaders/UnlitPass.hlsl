#ifndef CUSTOM_UNLIT_PASS_INCLUDED
#define CUSTOM_UNLIT_PASS_INCLUDED

#include "../ShaderLibrary/Common.hlsl"

// SRP 合批 (多个材质球 相同shader 不附带额外ObjectMaterialProperties脚本 才会合批)
// CBUFFER_START(UnityPerMaterial)
// float4 _BaseColor;
// CBUFFER_END

// GPU实例 + SRP合批 排序问题可能导致1次SRP合批被GPU实例拆成多次
// GPU 实例化仅适用于共享相同材质的对象 用ObjectMaterialProperties脚本修改属性
UNITY_INSTANCING_BUFFER_START(UnityPerMaterial)
	UNITY_DEFINE_INSTANCED_PROP(float4, _BaseColor)
UNITY_INSTANCING_BUFFER_END(UnityPerMaterial)

// 顶点输入
struct Attributes{
    float3 positionOS : POSITION;
    UNITY_VERTEX_INPUT_INSTANCE_ID 
};
// 顶点输出
struct Varyings{
    float4 positionCS : SV_POSITION;
	UNITY_VERTEX_INPUT_INSTANCE_ID
};

Varyings UnlitPassVertex(Attributes input) {
    Varyings output;
    UNITY_SETUP_INSTANCE_ID(input);
    UNITY_TRANSFER_INSTANCE_ID(input, output);
    output.positionCS = TransformObjectToHClip(input.positionOS.xyz);
    return output;
}

float4 UnlitPassFragment(Varyings input) : SV_TARGET{
    UNITY_SETUP_INSTANCE_ID(input);
    return UNITY_ACCESS_INSTANCED_PROP(UnityPerMaterial, _BaseColor);
}

#endif