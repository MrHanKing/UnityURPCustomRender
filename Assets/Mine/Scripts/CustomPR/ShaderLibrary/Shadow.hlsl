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

struct ShadowMask{
	bool alwaysMask; // shadowMask模式 静态物体优先使用烘焙阴影 不计算实时了
	bool distance; // 是否启用了distance shadowMask
	float4 shadows; // GI 环境阴影
};

// 阴影数据
struct ShadowData{
	int cascadeIndex; // 单个光阴影的级联索引
	float strength; // 级联阴影采样强度
	float cascadeBlend; // 级联阴影之间的混合系数 1.0为非边缘
	ShadowMask shadowMask;
};

struct DirectionalShadowData{
	float strength; // 阴影强度
	int tileIndex; // 阴影的纹理区域Index
	float normalBias; // 阴影法线偏移
	int shadowMaskChannel; // 阴影贴图在第几个通道
};

// 淡出阴影强度
float FadedShadowStrength(float distance, float scale, float fade){
	return saturate((1.0 - distance * scale) * fade);
}

// 阴影数据 这里计算的shadowData.strength 是级联阴影的强度
ShadowData GetShadowData(Surface surfaceWS){
	ShadowData shadowData;
	shadowData.shadowMask.alwaysMask = false;
	// 取值在GI里
	shadowData.shadowMask.distance = false;
	shadowData.shadowMask.shadows = 1.0;
	shadowData.strength = FadedShadowStrength(surfaceWS.depth, _ShadowDistanceFadeProp.x, _ShadowDistanceFadeProp.y);
	shadowData.cascadeBlend = 1.0;

	int i;
	for(i = 0; i < _ShadowCascadeCount; i++){
		float4 sphere = _ShadowCascadeCullingSpheres[i];
		float distanceSqr = DistanceSquare(sphere.xyz, surfaceWS.position);
		if(distanceSqr < sphere.w){
			// 阴影是否在边缘
			float fade = FadedShadowStrength(
					distanceSqr, _ShadowCascadeData[i].x, _ShadowDistanceFadeProp.z
				);
			if (i == _ShadowCascadeCount - 1) {
				// 最后一级过渡边缘
				shadowData.strength *= fade;
				// shadowData.strength = 0.0;
			}else{
				// 中间级别的过渡
				shadowData.cascadeBlend = fade;
			}
			break;
		}
	}

	// 超过级联等级
	if(i >= _ShadowCascadeCount){
		shadowData.strength = 0.0;
	}
	#if defined(_CASCADE_BLEND_DITHER)
	else if(shadowData.cascadeBlend < surfaceWS.dither){
		// 部分扰动到下一阶阴影
		i += 1;
	}
	#endif

	// 关闭级联混合
	#if !defined(_CASCADE_BLEND_SOFT)
		shadowData.cascadeBlend = 1.0;
	#endif

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

// 获取级联阴影 即实时阴影数据
float GetCascadeShadow(DirectionalShadowData dirShadowData, ShadowData globalShadow, Surface surfaceWS){
	// 偏移采样
	float3 normalBias = surfaceWS.normal * (dirShadowData.normalBias * _ShadowCascadeData[globalShadow.cascadeIndex].y);
	float4 getPos = float4(surfaceWS.position + normalBias, 1.0);
	float3 positionSTS = mul(_DirShadowMatris[dirShadowData.tileIndex], getPos).xyz;
	float shadow = FilterDirectionalShadow(positionSTS);
	// 每一级边缘区域采样混合
	if(globalShadow.cascadeBlend < 1.0){
		normalBias = surfaceWS.normal * (dirShadowData.normalBias * _ShadowCascadeData[globalShadow.cascadeIndex + 1].y);
		getPos = float4(surfaceWS.position + normalBias, 1.0);
		positionSTS = mul(_DirShadowMatris[dirShadowData.tileIndex + 1], getPos).xyz;
		shadow = lerp(FilterDirectionalShadow(positionSTS), shadow, globalShadow.cascadeBlend);
	}

	return shadow;
}
// 获得烘焙好的阴影
float GetBakedShadow (ShadowMask mask, int channel) {
	float shadow = 1.0;
	if (mask.alwaysMask || mask.distance) {
		if(channel >= 0){
			shadow = mask.shadows[channel];
		}
	}
	return shadow;
}
float GetBakedShadow (ShadowMask mask, float strength, int channel) {
	if (mask.alwaysMask || mask.distance) {
		return lerp(1.0, GetBakedShadow(mask, channel), strength);
	}
	return 1.0;
}
// 混合实时阴影和烘培阴影
float MixBakedAndRealtimeShadows (ShadowData globalShadow, float shadow, float strength, int shadowMaskChannel) {
	float baked = GetBakedShadow(globalShadow.shadowMask, shadowMaskChannel);
	if (globalShadow.shadowMask.alwaysMask){
		shadow = lerp(1.0, shadow, globalShadow.strength);
		shadow = min(baked, shadow);
		return lerp(1.0, shadow, strength);
	}

	if (globalShadow.shadowMask.distance) {
		// globalShadow.strength 0 说明超过级联阴影采样范围了
		shadow = lerp(baked, shadow, globalShadow.strength);
		return lerp(1.0, shadow, strength);
	}
	return lerp(1.0, shadow, strength * globalShadow.strength);
}
// 获取阴影衰减 1.0表示无阴影 0表示全阴影权重
float GetDirectionalShadowAttenuation(DirectionalShadowData dirShadowData, ShadowData globalShadow, Surface surfaceWS){
	#if !defined(_RECEIVE_SHADOWS)
		return 1.0;
	#endif

	float shadow;
	if(dirShadowData.strength * globalShadow.strength <= 0.0){
		shadow = GetBakedShadow(globalShadow.shadowMask, abs(dirShadowData.strength), dirShadowData.shadowMaskChannel);
	}
	else{
		shadow = GetCascadeShadow(dirShadowData, globalShadow, surfaceWS);
		shadow = MixBakedAndRealtimeShadows(globalShadow, shadow, dirShadowData.strength, dirShadowData.shadowMaskChannel);
	}

	return shadow;
}

#endif