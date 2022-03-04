#ifndef CUSTOM_LIGHTING_INCLUDED
#define CUSTOM_LIGHTING_INCLUDED

// 入射光
float3 IncomingLight (Surface surface, Light light) {
	return saturate(dot(surface.normal, light.direction)) * light.color;
}

float3 GetLighting (Surface surface, Light light) {
	return IncomingLight(surface, light) * surface.color;
}

float3 GetLighting (Surface surface) {
    float3 outColor = 0.0;
	for (int i = 0; i < GetDirectionalLightCount(); i++) {
		outColor += GetLighting(surface, GetDirectionalLight(i));
	}

	return outColor;
}

#endif