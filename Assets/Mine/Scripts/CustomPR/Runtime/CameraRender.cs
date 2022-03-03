using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering;

public class CameraRender
{
    #region 常量
    const string bufferName = "Render Camera";
    static ShaderTagId unlitShaderTagId = new ShaderTagId("SRPDefaultUnlit");
    static ShaderTagId[] legacyShaderTagIds = {
        new ShaderTagId("Always"),
        new ShaderTagId("ForwardBase"),
        new ShaderTagId("PrepassBase"),
        new ShaderTagId("Vertex"),
        new ShaderTagId("VertexLMRGBM"),
        new ShaderTagId("VertexLM")
    };
    #endregion

    // 上下文
    private ScriptableRenderContext context;
    private Camera camera;
    private CommandBuffer buffer = new CommandBuffer()
    {
        name = bufferName
    };
    private CullingResults cullingResults;
    private Material errorMaterial;

    public void Render(ScriptableRenderContext context, Camera camera)
    {
        this.context = context;
        this.camera = camera;

        // check
        if (!Cull())
        {
            return;
        }

        Setup();

        DrawOpaque();
        DrawSkybox();
        DrawTransparent();
        DrawUnsupportedShaders();

        Submit();
    }
    /// <summary>
    /// 设置状态 设置属性
    /// </summary>
    private void Setup()
    {
        context.SetupCameraProperties(camera);
        // clear
        buffer.ClearRenderTarget(true, true, Color.clear);

        buffer.BeginSample(bufferName);
        ExecuteBuffer();

    }

    private void DrawOpaque()
    {
        var sortingSettings = new SortingSettings(camera)
        {
            criteria = SortingCriteria.CommonOpaque
        };
        var drawingSettings = new DrawingSettings(unlitShaderTagId, sortingSettings);
        var filteringSettings = new FilteringSettings(RenderQueueRange.opaque);
        context.DrawRenderers(cullingResults, ref drawingSettings, ref filteringSettings);
    }

    /// <summary>
    /// 天空球
    /// </summary>
    private void DrawSkybox()
    {
        context.DrawSkybox(camera);
    }

    private void DrawTransparent()
    {
        var sortingSettings = new SortingSettings(camera)
        {
            criteria = SortingCriteria.CommonTransparent
        };
        var drawingSettings = new DrawingSettings(unlitShaderTagId, sortingSettings);
        var filteringSettings = new FilteringSettings(RenderQueueRange.transparent);
        context.DrawRenderers(cullingResults, ref drawingSettings, ref filteringSettings);
    }

    private void DrawUnsupportedShaders()
    {
        if (errorMaterial == null)
        {
            errorMaterial = new Material(Shader.Find("Hidden/InternalErrorShader"));
        }

        var drawingSettings = new DrawingSettings(legacyShaderTagIds[0], new SortingSettings(camera))
        {
            overrideMaterial = errorMaterial
        };
        for (int i = 1; i < legacyShaderTagIds.Length; i++)
        {
            drawingSettings.SetShaderPassName(i, legacyShaderTagIds[i]);
        }
        var filteringSettings = FilteringSettings.defaultValue;
        context.DrawRenderers(cullingResults, ref drawingSettings, ref filteringSettings);
    }

    /// <summary>
    /// 提交排队内容并执行
    /// </summary>
    private void Submit()
    {
        buffer.EndSample(bufferName);
        ExecuteBuffer();
        context.Submit();
    }

    private void ExecuteBuffer()
    {
        context.ExecuteCommandBuffer(buffer);
        buffer.Clear();
    }

    // 获取视野内几何体
    private bool Cull()
    {
        ScriptableCullingParameters p;
        if (camera.TryGetCullingParameters(out p))
        {
            cullingResults = context.Cull(ref p);
            return true;
        }

        return false;
    }
}
