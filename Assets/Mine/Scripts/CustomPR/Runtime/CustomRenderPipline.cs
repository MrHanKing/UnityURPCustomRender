using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering;

public class CustomRenderPipline : RenderPipeline
{
    private CameraRender render = new CameraRender();
    private bool useDynamicBatching, useGPUInstancing;
    public CustomRenderPipline(bool useDynamicBatching, bool useGPUInstancing, bool useSRPBatcher)
    {
        // 开启SRP的批处理器
        GraphicsSettings.useScriptableRenderPipelineBatching = useSRPBatcher;
        this.useDynamicBatching = useDynamicBatching;
        this.useGPUInstancing = useGPUInstancing;

        // 使用线性空间光照
        GraphicsSettings.lightsUseLinearIntensity = true;
    }
    protected override void Render(ScriptableRenderContext context, Camera[] cameras)
    {
        foreach (var camera in cameras)
        {
            render.Render(context, camera, useDynamicBatching, useGPUInstancing);
        }
    }
}
