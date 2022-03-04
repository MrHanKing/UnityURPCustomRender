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
    const int maxShadowDirectionalLightCount = 1;
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
    public void ReserveDirectionalShadows(Light light, int visibleLightIndex)
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

            currentShadowDirectionalLightCount += 1;
        }
    }

    public void Cleanup()
    {
        buffer.ReleaseTemporaryRT(CommonShaderPropertyID.dirShadowAtlasId);
        ExecuteBuffer();
    }

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
        for (int i = 0; i < currentShadowDirectionalLightCount; i++)
        {
            RenderDirectionalShadows(i, atlasSize);
        }
        buffer.EndSample(bufferName);
        ExecuteBuffer();
    }

    private void RenderDirectionalShadows(int index, int tileSize)
    {
        ShadowDirectionalLight light = shadowDirectionalLights[index];
        var shadowSettings =
            new ShadowDrawingSettings(cullingResults, light.visibleLightIndex);
        // 计算投影矩阵和裁剪空间立方体
        cullingResults.ComputeDirectionalShadowMatricesAndCullingPrimitives(
            light.visibleLightIndex, 0, 1, Vector3.zero, tileSize, 0f,
            out Matrix4x4 viewMatrix, out Matrix4x4 projectionMatrix,
            out ShadowSplitData shadowSplitData
        );
        shadowSettings.splitData = shadowSplitData;
        buffer.SetViewProjectionMatrices(viewMatrix, projectionMatrix);
        ExecuteBuffer();
        context.DrawShadows(ref shadowSettings);
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
