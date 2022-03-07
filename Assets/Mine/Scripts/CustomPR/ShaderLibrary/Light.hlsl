#ifndef CUSTOM_LIGHT_INCLUDED
#define CUSTOM_LIGHT_INCLUDED

#define MAX_DIRECTIONAL_LIGHT_COUNT 4
// 系统场景光数据
CBUFFER_START(_CustomLight)
	int _DirectionalLightCount;
	float4 _DirectionalLightColors[MAX_DIRECTIONAL_LIGHT_COUNT];
	float4 _DirectionalLightDirections[MAX_DIRECTIONAL_LIGHT_COUNT];
	float4 _DirLightShadowDatas[MAX_DIRECTIONAL_LIGHT_COUNT];
CBUFFER_END

struct Light{
	float3 color;
	float3 direction;
	float attenuationShadow; // 阴影导致的光衰减
};

int GetDirectionalLightCount () {
	return _DirectionalLightCount;
}

DirectionalShadowData GetDirectionalShadowData(int lightIndex, ShadowData shadowdata){
	DirectionalShadowData dirShadowData;
	dirShadowData.strength = _DirLightShadowDatas[lightIndex].x;
	dirShadowData.tileIndex = _DirLightShadowDatas[lightIndex].y + shadowdata.cascadeIndex;
	dirShadowData.normalBias = _DirLightShadowDatas[lightIndex].z;
	return dirShadowData;
}

Light GetDirectionalLight(int index, Surface surfaceWS, ShadowData shadowdata) {
	Light light;
	light.color = _DirectionalLightColors[index].rgb;
	light.direction = _DirectionalLightDirections[index].xyz;
	DirectionalShadowData dirShadowData = GetDirectionalShadowData(index, shadowdata);
	light.attenuationShadow = GetDirectionalShadowAttenuation(dirShadowData, shadowdata, surfaceWS);
	return light;
}

#endif