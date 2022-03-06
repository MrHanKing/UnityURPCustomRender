#ifndef CUSTOM_SHADOW_INCLUDED
#define CUSTOM_SHADOW_INCLUDED

#include "Packages/com.unity.render-pipelines.core/ShaderLibrary/Shadow/ShadowSamplingTent.hlsl"

#define MAX_SHADOW_DIRECTIONAL_LIGHT_COUNT 4
#define MAX_SHADOW_CASCADE 4

TEXTURE2D_SHADOW(_DirctionalShadowAtlas);
#define SHADOW_SAMPLER sampler_linear_clamp_compare // 显式定义采样器状态 因为只有一种合适的方式来采样阴影贴图
SAMPLER_CMP(SHADOW_SAMPLER);

#if defined(_DIRECTIONAL_SHADOW_PCF3)
	#define DIRECTIONAL_FILTER_SAMPLES 4 //采样数量
	#define DIRECTIONAL_FILTER_SETUP SampleShadow_ComputeSamples_Tent_3x3 //采样函数
#elif defined(_DIRECTIONAL_SHADOW_PCF5)
	#define DIRECTIONAL_FILTER_SAMPLES 9
	#define DIRECTIONAL_FILTER_SETUP SampleShadow_ComputeSamples_Tent_5x5
#elif defined(_DIRECTIONAL_SHADOW_PCF)
	#define DIRECTIONAL_FILTER_SAMPLES 16
	#define DIRECTIONAL_FILTER_SETUP SampleShadow_ComputeSamples_Tent_7x7
#endif

// 系统场景光阴影
CBUFFER_START(_CustomShadow)
	int _ShadowCascadeCount;
	float4 _ShadowAtlasSize;
	float4 _ShadowDistanceFadeProp;
	float4 _ShadowCascadeCullingSpheres[MAX_SHADOW_CASCADE];
	float4 _ShadowCascadeData[MAX_SHADOW_CASCADE];
	float4x4 _DirShadowMatris[MAX_SHADOW_DIRECTIONAL_LIGHT_COUNT * MAX_SHADOW_CASCADE];
CBUFFER_END

// 阴影数据
struct ShadowData{
	int cascadeIndex; // 单个光阴影的级联索引
	float strength; // 阴影采样强度
};

struct DirectionalShadowData{
	float strength; // 阴影强度
	int tileIndex; // 阴影的纹理区域Index
	float normalBias; // 阴影法线偏移
};

// 淡出阴影强度
float FadedShadowStrength(float distance, float scale, float fade){
	return saturate((1.0 - distance * scale) * fade);
}

// 阴影数据
ShadowData GetShadowData(Surface surfaceWS){
	ShadowData shadowData;
	shadowData.strength = FadedShadowStrength(surfaceWS.depth, _ShadowDistanceFadeProp.x, _ShadowDistanceFadeProp.y);

	int i;
	for(i = 0; i < _ShadowCascadeCount; i++){
		float4 sphere = _ShadowCascadeCullingSpheres[i];
		float distanceSqr = DistanceSquare(sphere.xyz, surfaceWS.position);
		if(distanceSqr < sphere.w){
			if (i == _ShadowCascadeCount - 1) {
				// 最后一级过渡边缘
				shadowData.strength *= FadedShadowStrength(
					distanceSqr, _ShadowCascadeData[i].x, _ShadowDistanceFadeProp.z
				);
				// shadowData.strength = 0.0;
			}
			break;
		}
	}

	// 超过级联等级
	if(i >= _ShadowCascadeCount){
		shadowData.strength = 0.0;
	}

	shadowData.cascadeIndex = i;
	return shadowData;
}

// 采样阴影贴图
// positionSTS 纹理空间坐标
float SampleDirectionalShadowAtlas(float3 positionSTS){
	return SAMPLE_TEXTURE2D_SHADOW(
		_DirctionalShadowAtlas, SHADOW_SAMPLER, positionSTS
	);
}

// 根据线性函数采样阴影
float FilterDirectionalShadow(float3 positionSTS){
	#if defined(DIRECTIONAL_FILTER_SETUP)
		float weights[DIRECTIONAL_FILTER_SAMPLES];
		float2 positions[DIRECTIONAL_FILTER_SAMPLES];
		float4 size = _ShadowAtlasSize.yyxx; // x,y:纹素大小 z,w:总纹理大小
		DIRECTIONAL_FILTER_SETUP(size, positionSTS.xy, weights, positions);
		float shadow = 0;
		for(int i = 0; i < DIRECTIONAL_FILTER_SAMPLES; i++){
			shadow += weights[i] * SampleDirectionalShadowAtlas(float3(positions[i].xy, positionSTS.z));
		}
		return shadow;
	#else
		return SampleDirectionalShadowAtlas(positionSTS);
	#endif
}

// 获取阴影衰减
float GetDirectionalShadowAttenuation(DirectionalShadowData shadowData, ShadowData globalShadow, Surface surfaceWS){
	if(shadowData.strength <= 0.0){
		return 1.0;
	}
	// 偏移采样
	float3 normalBias = surfaceWS.normal * (shadowData.normalBias * _ShadowCascadeData[globalShadow.cascadeIndex].y);
	float4 getPos = float4(surfaceWS.position + normalBias, 1.0);
	float3 positionSTS = mul(_DirShadowMatris[shadowData.tileIndex], getPos).xyz;
	float shadow = FilterDirectionalShadow(positionSTS);
	// 1表示无阴影 0表示全阴影
	return lerp(1.0, shadow, shadowData.strength);
	// return shadowData.tileIndex / 4.0;
}

#endif