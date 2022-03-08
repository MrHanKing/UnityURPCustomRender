Shader "CustomPR/LitShader"
{
    Properties
    {
        // Unity 硬编码属性 用于烘培透明度等
        [HideInInspector] _MainTex("Texture for Lightmap", 2D) = "white" {}
		[HideInInspector] _Color("Color for Lightmap", Color) = (0.5, 0.5, 0.5, 1.0)

        _BaseMap("Texture", 2D) = "white" {}
        _BaseColor("基础颜色", Color) = (0.5, 0.5, 0.5, 1.0)
        [Enum(UnityEngine.Rendering.BlendMode)] _SrcBlend ("Src Blend 源", Float) = 1
		[Enum(UnityEngine.Rendering.BlendMode)] _DstBlend ("Dst Blend 目标", Float) = 0
        [Enum(Off, 0, On, 1)] _ZWrite ("深度写入", Float) = 1
        [Toggle(_CLIPPING)] _Clipping ("是否开启Alpha裁剪", Float) = 0
        _Cutoff ("裁剪阈值", Range(0.0, 1.0)) = 0.0

        _Metallic("金属度", Range(0.0, 1.0)) = 0
        _Smoothness("光滑度", Range(0.0, 1.0)) = 0
        [Toggle(_PREMULTI_ALPHA)] _PremulAlpha ("是否预乘alpha", Float) = 0

        [KeywordEnum(On, Clip, Dither, Off)] _Shadows ("阴影模式", Float) = 0
        [Toggle(_RECEIVE_SHADOWS)] _ReceiveShadows ("表面受阴影影响", Float) = 1

        [NoScaleOffset] _EmissionMap("自发光贴图", 2D) = "white" {}
        [HDR] _EmissionColor("自发光颜色", Color) = (0.0, 0.0, 0.0, 0.0)
    }
    SubShader
    {
        HLSLINCLUDE
		#include "../ShaderLibrary/Common.hlsl"
		#include "LitInput.hlsl"
		ENDHLSL

        Pass
        {
            Tags { 
                // "RenderType"="Opaque" 
                "LightMode" = "CustomLit"
            }
            // LOD 100
            Blend [_SrcBlend] [_DstBlend]
            ZWrite [_ZWrite]

            HLSLPROGRAM
            // 着色器通道的目标级别 避免为它们编译 OpenGL ES 2.0 着色器变体 因为Light里面有循环 老式硬件性能低
            #pragma target 3.5
            // 关键字分支
            #pragma shader_feature _CLIPPING
            #pragma shader_feature _PREMULTI_ALPHA
            #pragma shader_feature _RECEIVE_SHADOWS
            #pragma multi_compile _ _DIRECTIONAL_SHADOW_PCF3 _DIRECTIONAL_SHADOW_PCF5 _DIRECTIONAL_SHADOW_PCF7
            #pragma multi_compile _ _CASCADE_BLEND_SOFT _CASCADE_BLEND_DITHER
            #pragma multi_compile _ _SHADOW_MASK_ALWAYS _SHADOW_MASK_DISTANCE
            // LOD
            #pragma multi_compile _ LOD_FADE_CROSSFADE
            // 光照贴图
            #pragma multi_compile _ LIGHTMAP_ON
            #pragma multi_compile_instancing
            #pragma vertex LitPassVertex
            #pragma fragment LitPassFragment
            #include "LitPass.hlsl"
            ENDHLSL
        }
        
        Pass
        {
            Tags {
                "LightMode" = "ShadowCaster"
            }

            // 只写深度 不写颜色 一个通道就够了
            ColorMask 0

            HLSLPROGRAM
            #pragma target 3.5
            #pragma shader_feature _ _SHADOWS_CLIP _SHADOWS_DITHER
            #pragma shader_feature _PREMULTI_ALPHA
            // LOD
            #pragma multi_compile _ LOD_FADE_CROSSFADE
            #pragma vertex ShadowCasterPassVertex
            #pragma fragment ShadowCasterPassFragment
            #include "ShadowCasterPass.hlsl"
            ENDHLSL
        }

        // meta通道确定间接漫反射颜色
        Pass {
			Tags {
				"LightMode" = "Meta"
			}

			Cull Off

			HLSLPROGRAM
			#pragma target 3.5
			#pragma vertex MetaPassVertex
			#pragma fragment MetaPassFragment
			#include "MetaPass.hlsl"
			ENDHLSL
		}
    }

    CustomEditor "CustomLitShaderGUI"
}
