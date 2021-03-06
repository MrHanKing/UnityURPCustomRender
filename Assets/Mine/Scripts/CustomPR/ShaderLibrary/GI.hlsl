#ifndef CUSTOM_GI_INCLUDED
#define CUSTOM_GI_INCLUDED

#include "Packages/com.unity.render-pipelines.core/ShaderLibrary/EntityLighting.hlsl"
#include "Packages/com.unity.render-pipelines.core/ShaderLibrary/ImageBasedLighting.hlsl"

TEXTURE2D(unity_Lightmap);
SAMPLER(samplerunity_Lightmap);

TEXTURE2D(unity_ShadowMask);
SAMPLER(samplerunity_ShadowMask);

// 探针球协采样
TEXTURE3D_FLOAT(unity_ProbeVolumeSH);
SAMPLER(samplerunity_ProbeVolumeSH);
// 环境高光采样
TEXTURECUBE(unity_SpecCube0);
SAMPLER(samplerunity_SpecCube0);

#if defined(LIGHTMAP_ON)
	#define GI_ATTRIBUTE_DATA float2 lightMapUV : TEXCOORD1;
	#define GI_VARYINGS_DATA float2 lightMapUV : VAR_LIGHT_MAP_UV;
	#define TRANSFORM_GI_DATA(input, output) \
		 output.lightMapUV = input.lightMapUV * unity_LightmapST.xy + unity_LightmapST.zw;
	#define GI_FRAGMENT_DATA(input) input.lightMapUV
#else
	#define GI_ATTRIBUTE_DATA
	#define GI_VARYINGS_DATA
	#define TRANSFORM_GI_DATA(input, output)
	#define GI_FRAGMENT_DATA(input) 0.0
#endif

// 环境光
struct GI {
	// 环境漫反射颜色
	float3 diffuse;
	// 环境高光颜色
	float3 specular;
	// 在烘焙数据采样
	ShadowMask shadowMask;
};

// 采样环境 环境cubeMap
float3 SampleEnvironment (Surface surfaceWS, BRDF brdf) {
	float3 uvw = reflect(-surfaceWS.viewDirection, surfaceWS.normal);
	// 根据感知粗糙度获取LOD级别
	float mip = PerceptualRoughnessToMipmapLevel(brdf.perceptualRoughness);
	float4 environment = SAMPLE_TEXTURECUBE_LOD(
		unity_SpecCube0, samplerunity_SpecCube0, uvw, mip
	);
	return DecodeHDREnvironment(environment, unity_SpecCube0_HDR);
}

// 采样shadowMask或者探针里的烘焙阴影数据
float4 SampleBakedShadows(float2 lightMapUV, Surface surfaceWS){
	#if defined(LIGHTMAP_ON)
		// 光照贴图模式使用ShadowMask
		return SAMPLE_TEXTURE2D(unity_ShadowMask, samplerunity_ShadowMask, lightMapUV);
	#else
		// 探针遮挡数据
		if(unity_ProbeVolumeParams.x){
			// 使用了LPPV
			return SampleProbeOcclusion(
				TEXTURE3D_ARGS(unity_ProbeVolumeSH, samplerunity_ProbeVolumeSH),
				surfaceWS.position, unity_ProbeVolumeWorldToObject,
				unity_ProbeVolumeParams.y, unity_ProbeVolumeParams.z,
				unity_ProbeVolumeMin.xyz, unity_ProbeVolumeSizeInv.xyz
			);
		}
		else{
			return unity_ProbesOcclusion;
			// return 1.0;
		}
	#endif
}

float3 SampleLightProbe(Surface surfaceWS){
	#if defined(LIGHTMAP_ON)
		// 物体使用光照贴图的时候 不对探头反应
		return 0.0;
	#else
		if (unity_ProbeVolumeParams.x) {
			// 使用了LPPV
			return SampleProbeVolumeSH4(
				TEXTURE3D_ARGS(unity_ProbeVolumeSH, samplerunity_ProbeVolumeSH),
				surfaceWS.position, surfaceWS.normal,
				unity_ProbeVolumeWorldToObject,
				unity_ProbeVolumeParams.y, unity_ProbeVolumeParams.z,
				unity_ProbeVolumeMin.xyz, unity_ProbeVolumeSizeInv.xyz
			);
		}
		else {
			float4 coefficients[7];
			coefficients[0] = unity_SHAr;
			coefficients[1] = unity_SHAg;
			coefficients[2] = unity_SHAb;
			coefficients[3] = unity_SHBr;
			coefficients[4] = unity_SHBg;
			coefficients[5] = unity_SHBb;
			coefficients[6] = unity_SHC;
			// 球协函数求
			return max(0.0, SampleSH9(coefficients, surfaceWS.normal));
		}
	#endif
}

float3 SampleLightMap(float2 lightMapUV){
	#if defined(LIGHTMAP_ON)
		return SampleSingleLightmap(TEXTURE2D_ARGS(unity_Lightmap, samplerunity_Lightmap),
		lightMapUV, float4(1.0, 1.0, 0.0, 0.0),
		#if defined(UNITY_LIGHTMAP_FULL_HDR)
			false,
		#else
			true,
		#endif
		float4(LIGHTMAP_HDR_MULTIPLIER, LIGHTMAP_HDR_EXPONENT, 0.0, 0.0)
		);
	#else
		return 0.0;
	#endif
}

GI GetGI(float2 lightMapUV, Surface surfaceWS, BRDF brdf){
	GI gi;
	gi.diffuse = SampleLightMap(lightMapUV) + SampleLightProbe(surfaceWS);
	gi.specular = SampleEnvironment(surfaceWS, brdf);
	gi.shadowMask.alwaysMask = false;
	gi.shadowMask.distance = false;
	gi.shadowMask.shadows = 1.0;
	#if defined(_SHADOW_MASK_ALWAYS)
		gi.shadowMask.alwaysMask = true;
		gi.shadowMask.shadows = SampleBakedShadows(lightMapUV, surfaceWS);
	#elif defined(_SHADOW_MASK_DISTANCE)
		gi.shadowMask.distance = true;
		gi.shadowMask.shadows = SampleBakedShadows(lightMapUV, surfaceWS);
	#endif

	return gi;
}

#endif