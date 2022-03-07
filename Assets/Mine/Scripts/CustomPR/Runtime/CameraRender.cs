using UnityEngine;
using UnityEngine.Rendering;

public partial class CameraRender
{
    #region 常量
    const string bufferName = "Render Camera";
    static ShaderTagId unlitShaderTagId = new ShaderTagId("SRPDefaultUnlit");
    // 跟shader中的tag匹配
    static ShaderTagId litShaderTagId = new ShaderTagId("CustomLit");

    #endregion

    // 上下文
    private ScriptableRenderContext context;
    private Camera camera;
    private Lighting lighting = new Lighting();
    private CommandBuffer buffer = new CommandBuffer()
    {
        name = bufferName
    };
    private CullingResults cullingResults;

    public void Render(ScriptableRenderContext context, Camera camera, bool useDynamicBatching, bool useGPUInstancing, ShadowSettings shadowSettings)
    {
        this.context = context;
        this.camera = camera;

        PrepareBuffer();
        // 显式添加UI渲染 必须在剔除之前
        PrepareForSceneWindow();
        // check
        if (!Cull(shadowSettings.maxDistance))
        {
            return;
        }
        // 先阴影 再相机
        buffer.BeginSample(SampleName);
        ExecuteBuffer();
        lighting.Setup(context, cullingResults, shadowSettings);
        buffer.EndSample(SampleName);

        Setup();

        DrawOpaque(useDynamicBatching, useGPUInstancing);
        DrawSkybox();
        DrawTransparent(useDynamicBatching, useGPUInstancing);
        DrawUnsupportedShaders();
        DrawGizmos();

        lighting.Cleanup();
        Submit();
    }
    /// <summary>
    /// 设置状态 设置属性
    /// </summary>
    private void Setup()
    {
        context.SetupCameraProperties(camera);
        // clear
        // flags 标示清除量的减少 有前后关系
        CameraClearFlags flags = camera.clearFlags;
        bool clearDepth = flags <= CameraClearFlags.Depth;
        bool clearColor = flags == CameraClearFlags.Color;
        Color baseColor = flags == CameraClearFlags.Color ? camera.backgroundColor.linear : Color.clear;
        buffer.ClearRenderTarget(clearDepth, clearColor, baseColor);

        buffer.BeginSample(SampleName);
        ExecuteBuffer();

    }

    private void DrawOpaque(bool useDynamicBatching, bool useGPUInstancing)
    {
        var sortingSettings = new SortingSettings(camera)
        {
            criteria = SortingCriteria.CommonOpaque
        };
        var drawingSettings = new DrawingSettings(unlitShaderTagId, sortingSettings);
        PrepareDrawingSetting(useDynamicBatching, useGPUInstancing, ref drawingSettings);

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

    private void DrawTransparent(bool useDynamicBatching, bool useGPUInstancing)
    {
        var sortingSettings = new SortingSettings(camera)
        {
            criteria = SortingCriteria.CommonTransparent
        };

        var drawingSettings = new DrawingSettings(unlitShaderTagId, sortingSettings);
        PrepareDrawingSetting(useDynamicBatching, useGPUInstancing, ref drawingSettings);

        var filteringSettings = new FilteringSettings(RenderQueueRange.transparent);
        context.DrawRenderers(cullingResults, ref drawingSettings, ref filteringSettings);
    }

    // 绘制设置的公共处理
    private void PrepareDrawingSetting(bool useDynamicBatching, bool useGPUInstancing, ref DrawingSettings drawingSettings)
    {
        drawingSettings.enableDynamicBatching = useDynamicBatching;
        drawingSettings.enableInstancing = useGPUInstancing;
        drawingSettings.perObjectData = PerObjectData.Lightmaps | PerObjectData.LightProbe |
        PerObjectData.LightProbeProxyVolume | PerObjectData.ShadowMask |
        PerObjectData.OcclusionProbe | PerObjectData.OcclusionProbeProxyVolume;

        drawingSettings.SetShaderPassName(1, litShaderTagId);
    }

    /// <summary>
    /// 提交排队内容并执行
    /// </summary>
    private void Submit()
    {
        buffer.EndSample(SampleName);
        ExecuteBuffer();
        context.Submit();
    }

    private void ExecuteBuffer()
    {
        context.ExecuteCommandBuffer(buffer);
        buffer.Clear();
    }

    // 获取视野内几何体
    private bool Cull(float maxShadowDistance)
    {
        ScriptableCullingParameters p;
        if (camera.TryGetCullingParameters(out p))
        {
            p.shadowDistance = Mathf.Min(maxShadowDistance, camera.farClipPlane);
            cullingResults = context.Cull(ref p);
            return true;
        }

        return false;
    }
}

