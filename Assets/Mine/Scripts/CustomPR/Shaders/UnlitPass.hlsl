#ifndef CUSTOM_UNLIT_PASS_INCLUDED
#define CUSTOM_UNLIT_PASS_INCLUDED

// 顶点输入
struct Attributes{
    float3 positionOS : POSITION;
    float2 baseUV : TEXCOORD0;
    UNITY_VERTEX_INPUT_INSTANCE_ID 
};
// 顶点输出
struct Varyings{
    float4 positionCS : SV_POSITION;
    float2 baseUV : VAR_BASE_UV;
	UNITY_VERTEX_INPUT_INSTANCE_ID
};

Varyings UnlitPassVertex(Attributes input) {
    Varyings output;
    UNITY_SETUP_INSTANCE_ID(input);
    UNITY_TRANSFER_INSTANCE_ID(input, output);
    output.positionCS = TransformObjectToHClip(input.positionOS.xyz);

    float4 baseST = UNITY_ACCESS_INSTANCED_PROP(UnityPerMaterial, _BaseMap_ST);
    output.baseUV = TransformBaseUV(input.baseUV);
    return output;
}

float4 UnlitPassFragment(Varyings input) : SV_TARGET{
    UNITY_SETUP_INSTANCE_ID(input);
    float4 resultColor = GetBase(input.baseUV);
    
#if defined(_CLIPPING)
    float4 cutoffAlpha = GetCutoff(input.baseUV);
    clip(resultColor.a - cutoffAlpha);
#endif
    return resultColor;
}

#endif