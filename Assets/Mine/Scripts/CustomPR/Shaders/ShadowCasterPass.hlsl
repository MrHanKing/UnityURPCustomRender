#ifndef CUSTOM_SHADOW_CASTER_PASS_INCLUDED
#define CUSTOM_SHADOW_CASTER_PASS_INCLUDED

// 顶点输入
struct Attributes{
    float3 positionOS : POSITION;
    float2 baseUV : TEXCOORD0;
    UNITY_VERTEX_INPUT_INSTANCE_ID 
};
// 顶点输出 片元输入
struct Varyings{
    float4 positionCS : SV_POSITION;
    float2 baseUV : VAR_BASE_UV;
	UNITY_VERTEX_INPUT_INSTANCE_ID
};

Varyings ShadowCasterPassVertex(Attributes input) {
    Varyings output;
    UNITY_SETUP_INSTANCE_ID(input);
    UNITY_TRANSFER_INSTANCE_ID(input, output);
    
    float3 positionWS = TransformObjectToWorld(input.positionOS);
    output.positionCS = TransformWorldToHClip(positionWS);

    float4 baseST = UNITY_ACCESS_INSTANCED_PROP(UnityPerMaterial, _BaseMap_ST);
    output.baseUV = TransformBaseUV(input.baseUV);
    return output;
}

void ShadowCasterPassFragment(Varyings input){
    UNITY_SETUP_INSTANCE_ID(input);
    float4 resultColor = GetBase(input.baseUV);
#if defined(_SHADOWS_CLIP)
    float cutoffAlpha = GetCutoff(input.baseUV);
    clip(resultColor.a - cutoffAlpha);
#elif defined(_SHADOWS_DITHER)
    float cutoffAlpha = InterleavedGradientNoise(input.positionCS.xy, 0);
    clip(resultColor.a - cutoffAlpha);
#endif
}

#endif