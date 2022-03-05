#ifndef CUSTOM_LIGHTING_INCLUDED
#define CUSTOM_LIGHTING_INCLUDED

// 物体高光强度 Todo(理解 Minimalist CookTorrance BRDF 的变体)
float SpecularStrength (Surface surface, BRDF brdf, Light light) {
	float3 h = SafeNormalize(light.direction + surface.viewDirection);
	float nh2 = Square(saturate(dot(surface.normal, h)));
	float lh2 = Square(saturate(dot(light.direction, h)));
	float r2 = Square(brdf.roughness);
	float d2 = Square(nh2 * (r2 - 1.0) + 1.00001);
	float normalization = brdf.roughness * 4.0 + 2.0;
	return r2 / (d2 * max(0.1, lh2) * normalization);
}

// 直接光照颜色 物体漫反射 + 物体高光
float3 DirectBRDF(Surface surface, BRDF brdf, Light light){
	return SpecularStrength(surface, brdf, light) * brdf.specular + brdf.diffuse;
}

// 入射光 (入射光能量) 考虑阴影
float3 IncomingLight (Surface surface, Light light) {
	return saturate(dot(surface.normal, light.direction)) * light.color * light.attenuationShadow;
}

float3 GetLighting (Surface surface, BRDF brdf, Light light) {
	return IncomingLight(surface, light) * DirectBRDF(surface, brdf, light);
}

float3 GetLighting (Surface surfaceWS, BRDF brdf) {
    float3 outColor = 0.0;
	for (int i = 0; i < GetDirectionalLightCount(); i++) {
		outColor += GetLighting(surfaceWS, brdf, GetDirectionalLight(i, surfaceWS));
	}

	return outColor;
}



#endif