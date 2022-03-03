using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering;

public class CustomRenderPipline : RenderPipeline
{
    private CameraRender render = new CameraRender();

    public CustomRenderPipline()
    {
        // 开启SRP的批处理器
        GraphicsSettings.useScriptableRenderPipelineBatching = true;
    }
    protected override void Render(ScriptableRenderContext context, Camera[] cameras)
    {
        foreach (var camera in cameras)
        {
            render.Render(context, camera);
        }
    }
}
