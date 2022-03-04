﻿using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public enum TextureSize
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
    public struct Directional
    {
        /// <summary>
        /// 阴影图集大小
        /// </summary>
        public TextureSize atlasSize;
    }

    public Directional directional = new Directional()
    {
        atlasSize = TextureSize._1024
    };
}
