using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering;
using Unity.Collections;

public class Lighting
{
    const string bufferName = "Lighting";
    // 最大支持的定向光数量
    const int maxDirLightCount = 4;

    private CommandBuffer buffer = new CommandBuffer()
    {
        name = bufferName
    };
    // culling信息里面有 影响摄像机可见空间内的 灯光信息
    private CullingResults cullingResults;

    private static Vector4[] dirLightColors = new Vector4[maxDirLightCount];
    private static Vector4[] dirLightDirections = new Vector4[maxDirLightCount];

    public void Setup(ScriptableRenderContext context, CullingResults cullingResults)
    {
        this.cullingResults = cullingResults;

        buffer.BeginSample(bufferName);
        SetupLights();
        buffer.EndSample(bufferName);
        context.ExecuteCommandBuffer(buffer);
        buffer.Clear();
    }

    private void SetupLights()
    {
        // 单定向光
        // Light light = RenderSettings.sun;
        // buffer.SetGlobalVector(CommonShaderPropertyID.dirLightColorId, light.color.linear * light.intensity);
        // buffer.SetGlobalVector(CommonShaderPropertyID.dirLightDirectionId, -light.transform.forward);
        NativeArray<VisibleLight> visibleLights = cullingResults.visibleLights;
        // 已注册定向光数量
        int dirLightCount = 0;
        for (int i = 0; i < visibleLights.Length; i++)
        {
            VisibleLight light = visibleLights[i];
            if (light.lightType == LightType.Directional)
            {
                SetupDirectionalLight(dirLightCount, ref light);
                dirLightCount += 1;
            }

            if (dirLightCount >= maxDirLightCount)
            {
                break;
            }
        }

        buffer.SetGlobalInt(CommonShaderPropertyID.dirLightCountId, visibleLights.Length);
        buffer.SetGlobalVectorArray(CommonShaderPropertyID.dirLightColorsId, dirLightColors);
        buffer.SetGlobalVectorArray(CommonShaderPropertyID.dirLightDirectionsId, dirLightDirections);
    }

    /// <summary>
    /// 
    /// </summary>
    /// <param name="index"></param>
    /// <param name="light">VisibleLight结构相当大 用ref避免复制</param>
    private void SetupDirectionalLight(int index, ref VisibleLight light)
    {
        dirLightColors[index] = light.finalColor;
        // 转换矩阵的第三列是前向向量
        dirLightDirections[index] = -light.localToWorldMatrix.GetColumn(2);
    }
}
