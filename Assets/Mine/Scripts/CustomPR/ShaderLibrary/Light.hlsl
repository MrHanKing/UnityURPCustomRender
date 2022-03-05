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

DirectionalShadowData GetDirectionalShadowData(int lightIndex){
	DirectionalShadowData resultData;
	resultData.strength = _DirLightShadowDatas[lightIndex].x;
	resultData.tileIndex = _DirLightShadowDatas[lightIndex].y;
	return resultData;
}

Light GetDirectionalLight(int index, Surface surfaceWS) {
	Light light;
	light.color = _DirectionalLightColors[index].rgb;
	light.direction = _DirectionalLightDirections[index].xyz;
	DirectionalShadowData shadowData = GetDirectionalShadowData(index);
	light.attenuationShadow = GetDirectionalShadowAttenuation(shadowData, surfaceWS);

	return light;
}

#endif