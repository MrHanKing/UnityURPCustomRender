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
