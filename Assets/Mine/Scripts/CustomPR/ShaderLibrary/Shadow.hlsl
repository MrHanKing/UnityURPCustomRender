#ifndef CUSTOM_SHADOW_INCLUDED
#define CUSTOM_SHADOW_INCLUDED

#define MAX_SHADOW_DIRECTIONAL_LIGHT_COUNT 4

TEXTURE2D_SHADOW(_DirctionalShadowAtlas);
#define SHADOW_SAMPLER sampler_linear_clamp_compare // 显式定义采样器状态 因为只有一种合适的方式来采样阴影贴图
SAMPLER_CMP(SHADOW_SAMPLER);

// 系统场景光阴影
CBUFFER_START(_CustomShadow)
	float4x4 _DirShadowMatris[MAX_SHADOW_DIRECTIONAL_LIGHT_COUNT];
CBUFFER_END

// 阴影数据
struct DirectionalShadowData{
	float strength;
	int tileIndex; // 阴影的纹理区域Index
};

// 采样阴影贴图
// positionSTS 纹理空间坐标
float SampleDirectionalShadowAtlas(float3 positionSTS){
	return SAMPLE_TEXTURE2D_SHADOW(
		_DirctionalShadowAtlas, SHADOW_SAMPLER, positionSTS
	);
}

// 获取阴影衰减
float GetDirectionalShadowAttenuation(DirectionalShadowData shadowData, Surface surfaceWS){
	if(shadowData.strength <= 0.0){
		return 1.0;
	}

	float3 positionSTS = mul(_DirShadowMatris[shadowData.tileIndex], float4(surfaceWS.position, 1.0)).xyz;
	float shadow = SampleDirectionalShadowAtlas(positionSTS);
	// 1表示无阴影 0表示全阴影
	return lerp(1.0, shadow, shadowData.strength);
}

#endif