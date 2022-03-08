#ifndef CUSTOM_BRDF_INCLUDED
#define CUSTOM_BRDF_INCLUDED

#define MIN_REFLECT_VALUE 0.04

struct BRDF{
    float3 diffuse;
    // 高光颜色
    float3 specular;
    // 粗糙度 和smoothness相反
    float roughness;
    // 粗糙度会混乱表面的镜面反射
    float perceptualRoughness;
    // 菲涅尔效果强度
    float fresnel;
};

// 非金属的反射率各不相同，但平均约为 0.04 所以映射漫反射强度 0 - 0.96
float OneMinusReflect(float metallic){
    float rangeMax = 1.0 - MIN_REFLECT_VALUE;
    return rangeMax - metallic * rangeMax;
}

// 间接环境照明
float3 IndirectBRDF (
	Surface surface, BRDF brdf, float3 giDiffuse, float3 giSpecular
) {
    // 菲涅尔效果强度
    float fresnelStrength = surface.fresnelStrength * Pow4(1.0 - saturate(dot(surface.normal, surface.viewDirection)));
    // 镜面反射 
    float3 reflection = giSpecular * lerp(brdf.specular, brdf.fresnel, fresnelStrength);
    // 粗糙度分散了反射光线 减少看到的镜面反射强度
    reflection /= brdf.roughness * brdf.roughness + 1.0;

    return giDiffuse * brdf.diffuse + reflection;
    // return reflection;
}

// applyAlphaToDiffuse 将alpha应用于漫反射
BRDF GetBRDF(Surface surface, bool applyAlphaToDiffuse = false){
    BRDF brdf;
    float oneMinusReflectivity = OneMinusReflect(surface.metallic);
    // 金属度越大 镜面反射越强 漫反射越弱
	brdf.diffuse = surface.color * oneMinusReflectivity;
    if(applyAlphaToDiffuse){
        brdf.diffuse *= surface.alpha;
    }
    // 金属度越高 镜面反射颜色越靠近金属颜色 非金属是白色
	brdf.specular = lerp(MIN_REFLECT_VALUE, surface.color, surface.metallic);
    // smoothness to perceptualRoughness 匹配迪士尼照明模型
    brdf.perceptualRoughness = PerceptualSmoothnessToPerceptualRoughness(surface.smoothness);
	brdf.roughness = PerceptualRoughnessToRoughness(brdf.perceptualRoughness);
    // 越粗糙 效果越弱 漫反射越强 效果越弱
    brdf.fresnel = saturate(surface.smoothness + 1.0 - oneMinusReflectivity);
	return brdf;
}

#endif