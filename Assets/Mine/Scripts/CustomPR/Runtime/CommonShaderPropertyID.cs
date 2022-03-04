using System.Collections;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// 统一存储shader的属性映射关系
/// </summary>
public static class CommonShaderPropertyID
{
    // 基础色
    public static int baseColorId = Shader.PropertyToID("_BaseColor");
    // 裁剪阈值
    public static int cutoffId = Shader.PropertyToID("_Cutoff");
    // 复数定向光
    public static int dirLightCountId = Shader.PropertyToID("_DirectionalLightCount");
    // 定向光颜色
    public static int dirLightColorsId = Shader.PropertyToID("_DirectionalLightColors");
    // 定向光方向 原点指向光
    public static int dirLightDirectionsId = Shader.PropertyToID("_DirectionalLightDirections");
    // 金属度
    public static int metallicId = Shader.PropertyToID("_Metallic");
    // 光滑度
    public static int smoothnessId = Shader.PropertyToID("_Smoothness");
}
