Shader "CustomPR/UnlitShaderOnlySRP"
{
    Properties
    {
        // _MainTex ("Texture", 2D) = "white" {}
        _BaseColor("基础颜色", Color) = (1.0, 1.0, 1.0, 1.0)
        [Enum(UnityEngine.Rendering.BlendMode)] _SrcBlend ("Src Blend 源", Float) = 1
		[Enum(UnityEngine.Rendering.BlendMode)] _DstBlend ("Dst Blend 目标", Float) = 0
    }
    SubShader
    {
        // Tags { "RenderType"="Opaque" }
        // LOD 100
        Blend [_SrcBlend] [_DstBlend]

        Pass
        {
            HLSLPROGRAM
            #pragma vertex UnlitPassVertex
            #pragma fragment UnlitPassFragment
            #include "UnlitShaderOnlySRP.hlsl"
            ENDHLSL
        }
    }
}
