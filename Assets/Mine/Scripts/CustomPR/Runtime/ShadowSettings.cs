using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public enum ShadowTextureSize
{
    _256 = 256,
    _512 = 512,
    _1024 = 1024,
    _2048 = 2048,
    _4096 = 4096,
    _8192 = 8192,
}

/// <summary>
/// 阴影采样插值方式
/// </summary>
public enum ShadowFilterMode
{
    PCF2x2,
    PCF3x3,
    PCF5x5,
    PCF7x7
}

[System.Serializable]
public class ShadowSettings
{
    [Min(0f)]
    public float maxDistance = 100f;
    [Range(0.001f, 1f)]
    [Tooltip("到最大阴影距离时 边缘淡出范围比例")]
    public float distanceFade = 0.1f;
    // 定向光
    [System.Serializable]
    public struct Directional
    {
        /// <summary>
        /// 阴影图集大小
        /// </summary>
        public ShadowTextureSize atlasSize;
        public ShadowFilterMode filterMode;
        /// <summary>
        /// 级联阴影 阴影贴图精细度分段
        /// </summary>
        [Range(1, 4)]
        public int cascadeCount;
        /// <summary>
        /// 分段占比
        /// </summary>
        [Range(0f, 1f)]
        public float cascadeRatio1, cascadeRatio2, cascadeRatio3;
        /// <summary>
        /// 最后一个级联的边缘淡化阴影
        /// </summary>
        [Range(0.001f, 1f)]
        public float cascadeFade;
        public Vector3 cascadeRatios { get { return new Vector3(cascadeRatio1, cascadeRatio2, cascadeRatio3); } }
    }

    public Directional directional = new Directional()
    {
        atlasSize = ShadowTextureSize._1024,
        filterMode = ShadowFilterMode.PCF2x2,
        cascadeCount = 4,
        cascadeRatio1 = 0.1f,
        cascadeRatio2 = 0.25f,
        cascadeRatio3 = 0.5f,
        cascadeFade = 0.1f
    };

}
