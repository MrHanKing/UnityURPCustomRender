#ifndef CUSTOM_BRDF_INCLUDED
#define CUSTOM_BRDF_INCLUDED

#define MIN_REFLECT_VALUE 0.04

struct BRDF{
    float3 diffuse;
    // 高光颜色
    float3 specular;
    // 粗糙度 和smoothness相反
    float roughness;
};

// 非金属的反射率各不相同，但平均约为 0.04 所以映射漫反射强度 0 - 0.96
float OneMinusReflect(float metallic){
    float rangeMax = 1.0 - MIN_REFLECT_VALUE;
    return rangeMax - metallic * rangeMax;
}

// applyAlphaToDiffuse 将alpha应用于漫反射
BRDF GetBRDF(Surface surface, bool applyAlphaToDiffuse = false){
    BRDF brdf;
    // 金属度越大 镜面反射越强 漫反射越弱
	brdf.diffuse = surface.color * OneMinusReflect(surface.metallic);
    if(applyAlphaToDiffuse){
        brdf.diffuse *= surface.alpha;
    }
    // 金属度越高 镜面反射颜色越靠近金属颜色 非金属是白色
	brdf.specular = lerp(MIN_REFLECT_VALUE, surface.color, surface.metallic);
    // smoothness to perceptualRoughness 匹配迪士尼照明模型
    float perceptualRoughness = PerceptualSmoothnessToPerceptualRoughness(surface.smoothness);
	brdf.roughness = PerceptualRoughnessToRoughness(perceptualRoughness);
	return brdf;
}

#endif