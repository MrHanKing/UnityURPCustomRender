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
    public void Setup(
        ScriptableRenderContext context, CullingResults cullingResults,
        ShadowSettings shadowSettings
    )
    {
        this.context = context;
        this.cullingResults = cullingResults;
        this.shadowSettings = shadowSettings;

        this.currentShadowDirectionalLightCount = 0;
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
    }

    /// <summary>
    /// 存储定向光阴影信息
    /// </summary>
    /// <param name="light"></param>
    /// <param name="visibleLightIndex"></param>
    public Vector2 ReserveDirectionalShadows(Light light, int visibleLightIndex)
    {
        if (currentShadowDirectionalLightCount < maxShadowDirectionalLightCount &&
            !IgnoreShadow(light) &&
            cullingResults.GetShadowCasterBounds(visibleLightIndex, out Bounds box)
        )
        {
            shadowDirectionalLights[currentShadowDirectionalLightCount] = new ShadowDirectionalLight()
            {
                visibleLightIndex = visibleLightIndex
            };

            var result = new Vector2(light.shadowStrength, currentShadowDirectionalLightCount * maxShadowCascades);

            currentShadowDirectionalLightCount += 1;
            return result;
        }

        return Vector2.zero;
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
        buffer.SetGlobalMatrixArray(CommonShaderPropertyID.dirShadowMatriId, dirShadowMatris);
        buffer.SetGlobalInt(CommonShaderPropertyID.shadowCascadeCountId, shadowSettings.directional.cascadeCount);
        buffer.SetGlobalVectorArray(CommonShaderPropertyID.shadowCascadeCullingSpheresId, cascadeCullingSpheres);
        buffer.SetGlobalFloat(CommonShaderPropertyID.shadowDistanceId, shadowSettings.maxDistance);

        buffer.EndSample(bufferName);
        ExecuteBuffer();
    }

    private void RenderDirectionalShadows(int index, int split, int tileSize)
    {
        ShadowDirectionalLight light = shadowDirectionalLights[index];
        var drawShadowSettings =
            new ShadowDrawingSettings(cullingResults, light.visibleLightIndex);
        int cascadeCount = shadowSettings.directional.cascadeCount;
        int tileOffset = index * cascadeCount;
        Vector3 ratios = shadowSettings.directional.cascadeRatios;

        for (int i = 0; i < cascadeCount; i++)
        {
            // 计算投影矩阵和裁剪空间立方体
            cullingResults.ComputeDirectionalShadowMatricesAndCullingPrimitives(
                light.visibleLightIndex, 0, 1, Vector3.zero, tileSize, 0f,
                out Matrix4x4 viewMatrix, out Matrix4x4 projectionMatrix,
                out ShadowSplitData shadowSplitData
            );

            if (index == 0)
            {
                // 只取第一个灯的剔除球距离 所有平行光定义一份
                // cullingSphere xyz:球心坐标  w:半径
                Vector4 cullingSphere = shadowSplitData.cullingSphere;
                // 距离要拿来判断片元是否在范围内 所以用平方来比较 减少计算量
                cullingSphere.w *= cullingSphere.w;
                cascadeCullingSpheres[i] = cullingSphere;
            }

            drawShadowSettings.splitData = shadowSplitData;
            int tileIndex = tileOffset + i;
            var offset = SetTileViewport(tileIndex, split, tileSize);

            dirShadowMatris[tileIndex] = ConvertToAtlasMatrix(projectionMatrix * viewMatrix, offset, split);
            buffer.SetViewProjectionMatrices(viewMatrix, projectionMatrix);
            ExecuteBuffer();
            context.DrawShadows(ref drawShadowSettings);
        }
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
