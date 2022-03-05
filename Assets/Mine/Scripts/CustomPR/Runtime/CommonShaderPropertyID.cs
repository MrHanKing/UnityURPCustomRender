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
    // 定向光阴影数据(如阴影强度等)
    public static int dirLightShadowDatasId = Shader.PropertyToID("_DirLightShadowDatas");

    // 金属度
    public static int metallicId = Shader.PropertyToID("_Metallic");
    // 光滑度
    public static int smoothnessId = Shader.PropertyToID("_Smoothness");
    // 定向光阴影贴图
    public static int dirShadowAtlasId = Shader.PropertyToID("_DirctionalShadowAtlas");
    // 定向光阴影变换矩阵 worldPos -> 阴影裁剪空间 -> 纹理空间
    public static int dirShadowMatriId = Shader.PropertyToID("_DirShadowMatris");
    // 级联阴影级别
    public static int shadowCascadeCountId = Shader.PropertyToID("_ShadowCascadeCount");
    // 级联阴影剔除球 以摄像机距离范围为半径的球体剔除
    public static int shadowCascadeCullingSpheresId = Shader.PropertyToID("_ShadowCascadeCullingSpheres");
    // x: 1f/阴影最大渲染距离 y: 1f/阴影淡出范围边缘
    public static int shadowDistanceFadePropId = Shader.PropertyToID("_ShadowDistanceFadeProp");
}
