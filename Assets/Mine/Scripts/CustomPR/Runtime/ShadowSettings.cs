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

[System.Serializable]
public class ShadowSettings
{
    [Min(0f)]
    public float maxDistance = 100f;

    // 定向光
    [System.Serializable]
    public struct Directional
    {
        /// <summary>
        /// 阴影图集大小
        /// </summary>
        public ShadowTextureSize atlasSize;
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

        public Vector3 cascadeRatios { get { return new Vector3(cascadeRatio1, cascadeRatio2, cascadeRatio3); } }
    }

    public Directional directional = new Directional()
    {
        atlasSize = ShadowTextureSize._1024,
        cascadeCount = 4,
        cascadeRatio1 = 0.1f,
        cascadeRatio2 = 0.25f,
        cascadeRatio3 = 0.5f,
    };

}
