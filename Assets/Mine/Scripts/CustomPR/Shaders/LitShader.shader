Shader "CustomPR/LitShader"
{
    Properties
    {
        _BaseMap("Texture", 2D) = "white" {}
        _BaseColor("基础颜色", Color) = (0.5, 0.5, 0.5, 1.0)
        [Enum(UnityEngine.Rendering.BlendMode)] _SrcBlend ("Src Blend 源", Float) = 1
		[Enum(UnityEngine.Rendering.BlendMode)] _DstBlend ("Dst Blend 目标", Float) = 0
        [Enum(Off, 0, On, 1)] _ZWrite ("深度写入", Float) = 1
        [Toggle(_CLIPPING)] _Clipping ("是否开启Alpha裁剪", Float) = 0
        _Cutoff ("裁剪阈值", Range(0.0, 1.0)) = 0.0

        _Metallic("金属度", Range(0.0, 1.0)) = 0
        _Smoothness("光滑度", Range(0.0, 1.0)) = 0
    }
    SubShader
    {
        Tags { 
            // "RenderType"="Opaque" 
            "LightMode" = "CustomLit"
        }
        // LOD 100
        Blend [_SrcBlend] [_DstBlend]
        ZWrite [_ZWrite]

        Pass
        {
            HLSLPROGRAM
            // 着色器通道的目标级别 避免为它们编译 OpenGL ES 2.0 着色器变体 因为Light里面有循环 老式硬件性能低
            #pragma target 3.5
            // 关键字分支
            #pragma shader_feature _CLIPPING
            #pragma multi_compile_instancing
            #pragma vertex LitPassVertex
            #pragma fragment LitPassFragment
            #include "LitPass.hlsl"
            ENDHLSL
        }
    }
}
