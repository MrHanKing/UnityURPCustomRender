Shader "CustomPR/UnlitShaderOnlySRP"
{
    Properties
    {
        // _MainTex ("Texture", 2D) = "white" {}
        _BaseColor("基础颜色", Color) = (1.0, 1.0, 1.0, 1.0)
    }
    SubShader
    {
        // Tags { "RenderType"="Opaque" }
        // LOD 100

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
