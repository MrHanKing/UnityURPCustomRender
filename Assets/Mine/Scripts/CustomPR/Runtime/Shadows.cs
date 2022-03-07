using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering;

public class Shadows
{
    #region struct
    struct ShadowDirectionalLight
    {
        // 光的索引映射
        public int visibleLightIndex;
        /// <summary>
        /// 斜率缩放偏移 微调阴影
        /// </summary>
        public float slopeScaleBias;
        /// <summary>
        /// 后拉近平面 缓解近裁面阴影问题
        /// </summary>
        public float nearPlaneOffset;
    }
    #endregion

    const string bufferName = "Shadows";
    // 支持的阴影光数量 最大级联阴影数
    const int maxShadowDirectionalLightCount = 4, maxShadowCascades = 4;
    private CommandBuffer buffer = new CommandBuffer()
    {
        name = bufferName
    };

    private ScriptableRenderContext context;
    private CullingResults cullingResults;
    private ShadowSettings shadowSettings;
    // 定向光的阴影数据
    private ShadowDirectionalLight[] shadowDirectionalLights = new ShadowDirectionalLight[maxShadowDirectionalLightCount];
    private int currentShadowDirectionalLightCount = 0;
    // 阴影的转换矩阵
    private static Matrix4x4[] dirShadowMatris = new Matrix4x4[maxShadowDirectionalLightCount * maxShadowCascades];
    // 级联阴影剔除球
    private static Vector4[] cascadeCullingSpheres = new Vector4[maxShadowCascades];
    private static Vector4[] cascadeDatas = new Vector4[maxShadowCascades];
    // 阴影采样模式
    private static string[] directionalFilterKeywords = {
        "_DIRECTIONAL_SHADOW_PCF3",
        "_DIRECTIONAL_SHADOW_PCF5",
        "_DIRECTIONAL_SHADOW_PCF7"
    };
    // 阴影混合模式
    private static string[] cascadeBlendKeywords = {
        "_CASCADE_BLEND_SOFT",
        "_CASCADE_BLEND_DITHER",
    };
    // 阴影遮罩shadow Mask 静态
    private static string[] shadowMaskKeywords = {
        "_SHADOW_MASK_DISTANCE",
    };
    // 是否使用阴影遮蔽 每帧都需要重新评估
    private bool useShadowMask;

    public void Setup(
        ScriptableRenderContext context, CullingResults cullingResults,
        ShadowSettings shadowSettings
    )
    {
        this.context = context;
        this.cullingResults = cullingResults;
        this.shadowSettings = shadowSettings;

        this.currentShadowDirectionalLightCount = 0;
        this.useShadowMask = false;
    }

    public void Render()
    {
        if (this.currentShadowDirectionalLightCount > 0)
        {
            RenderDirectionalShadows();
        }
        else
        {
            // 不声明纹理会导致 WebGL 2.0 出现问题，因为它将纹理和采样器绑定在一起 缺少纹理会导致shader出错
            buffer.GetTemporaryRT(
                CommonShaderPropertyID.dirShadowAtlasId, 1, 1,
                32, FilterMode.Bilinear, RenderTextureFormat.Shadowmap
            );
        }

        // shadow mask 设置
        buffer.BeginSample(bufferName);
        var modeEnumIndex = useShadowMask ? 0 : -1;
        SetKeywords(shadowMaskKeywords, modeEnumIndex);
        buffer.EndSample(bufferName);
        ExecuteBuffer();
    }

    /// <summary>
    /// 存储定向光阴影信息
    /// </summary>
    /// <param name="light"></param>
    /// <param name="visibleLightIndex"></param>
    public Vector3 ReserveDirectionalShadows(Light light, int visibleLightIndex)
    {
        if (currentShadowDirectionalLightCount < maxShadowDirectionalLightCount &&
            !IgnoreShadow(light) &&
            cullingResults.GetShadowCasterBounds(visibleLightIndex, out Bounds box)
        )
        {
            LightBakingOutput lightBaking = light.bakingOutput;
            useShadowMask = lightBaking.lightmapBakeType == LightmapBakeType.Mixed && lightBaking.mixedLightingMode == MixedLightingMode.Shadowmask;

            shadowDirectionalLights[currentShadowDirectionalLightCount] = new ShadowDirectionalLight()
            {
                visibleLightIndex = visibleLightIndex,
                slopeScaleBias = light.shadowBias,
                nearPlaneOffset = light.shadowNearPlane
            };

            var result = new Vector3(light.shadowStrength, currentShadowDirectionalLightCount * maxShadowCascades, light.shadowNormalBias);

            currentShadowDirectionalLightCount += 1;
            return result;
        }

        return Vector3.zero;
    }

    public void Cleanup()
    {
        buffer.ReleaseTemporaryRT(CommonShaderPropertyID.dirShadowAtlasId);
        ExecuteBuffer();
    }

    // 渲染阴影贴图
    private void RenderDirectionalShadows()
    {
        int atlasSize = (int)shadowSettings.directional.atlasSize;
        // 临时RenderTexture
        buffer.GetTemporaryRT(CommonShaderPropertyID.dirShadowAtlasId, atlasSize,
        atlasSize, 32, FilterMode.Bilinear, RenderTextureFormat.Shadowmap);
        // 指定渲染纹理状态
        buffer.SetRenderTarget(CommonShaderPropertyID.dirShadowAtlasId,
        RenderBufferLoadAction.DontCare, RenderBufferStoreAction.Store);
        // 清理深度缓冲
        buffer.ClearRenderTarget(true, false, Color.clear);

        buffer.BeginSample(bufferName);
        ExecuteBuffer();

        // 阴影
        int tiles = currentShadowDirectionalLightCount * shadowSettings.directional.cascadeCount;
        int split = tiles <= 1 ? 1 : tiles <= 4 ? 2 : 4; // 最大支持4光影
        int tileSize = atlasSize / split; // 纹理正方型 求一次就行
        for (int i = 0; i < currentShadowDirectionalLightCount; i++)
        {
            RenderDirectionalShadows(i, split, tileSize);
        }
        #region shader参数设置
        buffer.SetGlobalMatrixArray(CommonShaderPropertyID.dirShadowMatriId, dirShadowMatris);
        buffer.SetGlobalInt(CommonShaderPropertyID.shadowCascadeCountId, shadowSettings.directional.cascadeCount);
        buffer.SetGlobalVectorArray(CommonShaderPropertyID.shadowCascadeCullingSpheresId, cascadeCullingSpheres);
        buffer.SetGlobalVectorArray(CommonShaderPropertyID.shadowCascadeDataId, cascadeDatas);

        float f = 1f - shadowSettings.directional.cascadeFade;
        buffer.SetGlobalVector(CommonShaderPropertyID.shadowDistanceFadePropId,
        new Vector4(1f / shadowSettings.maxDistance, 1f / shadowSettings.distanceFade, 1f / (1f - f * f)));

        SetKeywords(directionalFilterKeywords, (int)shadowSettings.directional.filterMode - 1);
        SetKeywords(cascadeBlendKeywords, (int)shadowSettings.directional.cascadeBlendMode - 1);
        buffer.SetGlobalVector(CommonShaderPropertyID.shadowAtlasSizeId, new Vector4(atlasSize, 1f / atlasSize));
        #endregion

        buffer.EndSample(bufferName);
        ExecuteBuffer();
    }

    /// <summary>
    /// 设置Shader关键字 shader变体
    /// </summary>
    private void SetKeywords(string[] keywords, int modeEnumIndex)
    {
        // 映射枚举 和 shader keyword
        // int enabledIndex = (int)shadowSettings.directional.filterMode - 1;
        // int enabledIndex = modeEnumIndex - 1;
        for (int i = 0; i < keywords.Length; i++)
        {
            if (i == modeEnumIndex)
            {
                buffer.EnableShaderKeyword(keywords[i]);
            }
            else
            {
                buffer.DisableShaderKeyword(keywords[i]);
            }
        }
    }

    private void RenderDirectionalShadows(int index, int split, int tileSize)
    {
        ShadowDirectionalLight light = shadowDirectionalLights[index];
        var drawShadowSettings =
            new ShadowDrawingSettings(cullingResults, light.visibleLightIndex);
        int cascadeCount = shadowSettings.directional.cascadeCount;
        int tileOffset = index * cascadeCount;
        Vector3 ratios = shadowSettings.directional.cascadeRatios;
        float cullingFactor = Mathf.Max(0f, 0.8f - shadowSettings.directional.cascadeFade);

        for (int i = 0; i < cascadeCount; i++)
        {
            // 计算投影矩阵和裁剪空间立方体
            cullingResults.ComputeDirectionalShadowMatricesAndCullingPrimitives(
                light.visibleLightIndex, i, cascadeCount, ratios, tileSize, light.nearPlaneOffset,
                out Matrix4x4 viewMatrix, out Matrix4x4 projectionMatrix,
                out ShadowSplitData shadowSplitData
            );

            if (index == 0)
            {
                // 只取第一个灯的剔除球距离 所有平行光定义一份
                SetCascadeData(i, shadowSplitData.cullingSphere, tileSize);
            }

            shadowSplitData.shadowCascadeBlendCullingFactor = cullingFactor;
            drawShadowSettings.splitData = shadowSplitData;
            int tileIndex = tileOffset + i;
            var offset = SetTileViewport(tileIndex, split, tileSize);

            dirShadowMatris[tileIndex] = ConvertToAtlasMatrix(projectionMatrix * viewMatrix, offset, split);
            buffer.SetViewProjectionMatrices(viewMatrix, projectionMatrix);

            buffer.SetGlobalDepthBias(0f, light.slopeScaleBias);
            ExecuteBuffer();
            context.DrawShadows(ref drawShadowSettings);
            buffer.SetGlobalDepthBias(0f, 0f);
        }
    }

    // 级联阴影数据
    private void SetCascadeData(int levelIndex, Vector4 cullingSphere, float tileSize)
    {
        // 伪阴影由于 一个纹素被多个点使用导致 通过沿法线偏移采样解决 1.4142是√2
        float textSize = 2f * cullingSphere.w / tileSize;
        // 采样偏移
        float filterSize = textSize * ((float)shadowSettings.directional.filterMode + 1f) * 1.4142f;

        // cullingSphere xyz:球心坐标  w:半径
        // 距离要拿来判断片元是否在范围内 所以用平方来比较 减少计算量
        cullingSphere.w -= filterSize; // 上面扩大了采样范围 防止在剔除外采样 缩小范围
        cullingSphere.w *= cullingSphere.w;
        cascadeCullingSpheres[levelIndex] = cullingSphere;

        cascadeDatas[levelIndex] = new Vector4(1f / cullingSphere.w, filterSize);
    }

    /// <summary>
    /// 转换VP矩阵 -> VP + 阴影贴图纹理空间 矩阵
    /// </summary>
    /// <param name="m"></param>
    /// <param name="offset"></param>
    /// <param name="split"></param>
    /// <returns></returns>
    private Matrix4x4 ConvertToAtlasMatrix(Matrix4x4 m, Vector2 offset, int split)
    {
        // 反转Z OpenGL 1表示最大深度 而其他图像API正好相反
        if (SystemInfo.usesReversedZBuffer)
        {
            m.m20 = -m.m20;
            m.m21 = -m.m21;
            m.m22 = -m.m22;
            m.m23 = -m.m23;
        }

        // 裁剪空间[-1, 1] 映射为纹理空间[0, 1]  再根据分区进行偏移和缩放
        float scale = 1f / split;
        m.m00 = (0.5f * (m.m00 + m.m30) + offset.x * m.m30) * scale; // m30 即 w分量 来矫正倍数
        m.m01 = (0.5f * (m.m01 + m.m31) + offset.x * m.m31) * scale;
        m.m02 = (0.5f * (m.m02 + m.m32) + offset.x * m.m32) * scale;
        m.m03 = (0.5f * (m.m03 + m.m33) + offset.x * m.m33) * scale;
        m.m10 = (0.5f * (m.m10 + m.m30) + offset.y * m.m30) * scale;
        m.m11 = (0.5f * (m.m11 + m.m31) + offset.y * m.m31) * scale;
        m.m12 = (0.5f * (m.m12 + m.m32) + offset.y * m.m32) * scale;
        m.m13 = (0.5f * (m.m13 + m.m33) + offset.y * m.m33) * scale;
        m.m20 = 0.5f * (m.m20 + m.m30);
        m.m21 = 0.5f * (m.m21 + m.m31);
        m.m22 = 0.5f * (m.m22 + m.m32);
        m.m23 = 0.5f * (m.m23 + m.m33);

        return m;
    }

    /// <summary>
    /// 设置渲染视野所在纹理的位置
    /// </summary>
    /// <param name="tileIndex">阴影纹理的index</param>
    /// <param name="split">切分量</param>
    /// <param name="tileSize">纹理单位大小</param>
    private Vector2 SetTileViewport(int tileIndex, int split, float tileSize)
    {
        Vector2 offset = new Vector2(tileIndex % split, tileIndex / split);
        buffer.SetViewport(new Rect(offset.x * tileSize, offset.y * tileSize, tileSize, tileSize));
        return offset;
    }

    private bool IgnoreShadow(Light light)
    {
        return light.shadows == LightShadows.None || light.shadowStrength <= 0;
    }

    private void ExecuteBuffer()
    {
        context.ExecuteCommandBuffer(buffer);
        buffer.Clear();
    }


}
