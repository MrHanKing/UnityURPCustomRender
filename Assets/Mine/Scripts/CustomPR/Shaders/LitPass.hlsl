#ifndef CUSTOM_LIT_PASS_INCLUDED
#define CUSTOM_LIT_PASS_INCLUDED

#include "../ShaderLibrary/Surface.hlsl"
#include "../ShaderLibrary/Shadow.hlsl"
#include "../ShaderLibrary/Light.hlsl"
#include "../ShaderLibrary/BRDF.hlsl"
#include "../ShaderLibrary/GI.hlsl"
#include "../ShaderLibrary/Lighting.hlsl"

// 顶点输入
struct Attributes{
    float3 positionOS : POSITION;
    float3 normalOS : NORMAL;
    float2 baseUV : TEXCOORD0;
    GI_ATTRIBUTE_DATA
    UNITY_VERTEX_INPUT_INSTANCE_ID 
};
// 顶点输出 片元输入
struct Varyings{
    float4 positionCS : SV_POSITION;
    float3 positionWS : VAR_POSITION;
    float3 normalWS : VAR_NORMAL; // 需要在片元中再次normalize 因为线性插值导致不是归一化向量
    float2 baseUV : VAR_BASE_UV;
    GI_VARYINGS_DATA
	UNITY_VERTEX_INPUT_INSTANCE_ID
};

Varyings LitPassVertex(Attributes input) {
    Varyings output;
    UNITY_SETUP_INSTANCE_ID(input);
    UNITY_TRANSFER_INSTANCE_ID(input, output);
    TRANSFORM_GI_DATA(input, output);
    
    output.positionWS = TransformObjectToWorld(input.positionOS);
    output.positionCS = TransformWorldToHClip(output.positionWS);

    output.normalWS = TransformObjectToWorldNormal(input.normalOS);

    float4 baseST = UNITY_ACCESS_INSTANCED_PROP(UnityPerMaterial, _BaseMap_ST);
    output.baseUV = TransformBaseUV(input.baseUV);
    return output;
}

float4 LitPassFragment(Varyings input) : SV_TARGET{
    UNITY_SETUP_INSTANCE_ID(input);
#if defined(LOD_FADE_CROSSFADE)
		ClipLOD(input.positionCS.xy, unity_LODFade.x);
#endif
    float4 resultColor = GetBase(input.baseUV);

#if defined(_CLIPPING)
    float4 cutoffAlpha = GetCutoff(input.baseUV);
    clip(resultColor.a - cutoffAlpha);
#endif

    Surface surface;
    surface.position = input.positionWS;
    surface.normal = normalize(input.normalWS);
    surface.depth = -TransformWorldToView(input.positionWS).z;
    surface.color = resultColor.rgb;
    surface.alpha = resultColor.a;
    surface.metallic = GetMetallic(input.baseUV);
    surface.smoothness = GetSmoothness(input.baseUV);
    surface.viewDirection = normalize(_WorldSpaceCameraPos - input.positionWS);
    surface.fresnelStrength = GetFresnel(input.baseUV);
    // 噪声采样抖动值
    surface.dither = InterleavedGradientNoise(input.positionCS.xy, 0);
#if defined(_PREMULTI_ALPHA)
    BRDF brdf = GetBRDF(surface, true);
#else
    BRDF brdf = GetBRDF(surface);
#endif
    GI gi = GetGI(GI_FRAGMENT_DATA(input), surface, brdf);
    //surface 表面数据 brdf 自身漫反射和高光 gi 环境漫反射和高光
    float3 color = GetLighting(surface, brdf, gi);

    color += GetEmission(input.baseUV);

    return float4(color, surface.alpha);
}

#endif