#ifndef CUSTOM_UNLIT_INPUT_INCLUDED
#define CUSTOM_UNLIT_INPUT_INCLUDED

TEXTURE2D(_BaseMap);
// 采样器状态 控制如何采样 如clamp或repeat模式
SAMPLER(sampler_BaseMap);

// SRP 合批 (多个材质球 相同shader 不附带额外ObjectMaterialProperties脚本 才会合批)
// CBUFFER_START(UnityPerMaterial)
// float4 _BaseColor;
// CBUFFER_END

// GPU实例 + SRP合批 排序问题可能导致1次SRP合批被GPU实例拆成多次
// GPU 实例化仅适用于共享相同材质的对象 用ObjectMaterialProperties脚本修改属性
UNITY_INSTANCING_BUFFER_START(UnityPerMaterial)
    UNITY_DEFINE_INSTANCED_PROP(float4, _BaseMap_ST)
	UNITY_DEFINE_INSTANCED_PROP(float4, _BaseColor)
    UNITY_DEFINE_INSTANCED_PROP(float, _Cutoff)
UNITY_INSTANCING_BUFFER_END(UnityPerMaterial)

// UV偏移变化
float2 TransformBaseUV (float2 baseUV) {
	float4 baseST = UNITY_ACCESS_INSTANCED_PROP(UnityPerMaterial, _BaseMap_ST);
	return baseUV * baseST.xy + baseST.zw;
}

// 基础颜色
float4 GetBase (float2 baseUV) {
	float4 map = SAMPLE_TEXTURE2D(_BaseMap, sampler_BaseMap, baseUV);
	float4 color = UNITY_ACCESS_INSTANCED_PROP(UnityPerMaterial, _BaseColor);
	return map * color;
}

// 裁剪阈值
float GetCutoff (float2 baseUV) {
	return UNITY_ACCESS_INSTANCED_PROP(UnityPerMaterial, _Cutoff);
}

#endif