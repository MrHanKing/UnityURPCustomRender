using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Profiling;
using UnityEditor;

partial class CameraRender
{
    /// <summary>
    /// 绘制编辑器UI
    /// </summary>
    /// <returns></returns>
    partial void DrawGizmos();
    /// <summary>
    /// 绘制不支持（错误的）shader物体
    /// </summary>
    /// <returns></returns>
    partial void DrawUnsupportedShaders();
    /// <summary>
    /// 预处理UI的渲染
    /// </summary>
    /// <returns></returns>
    partial void PrepareForSceneWindow();
    /// <summary>
    /// 预处理缓冲buffer
    /// </summary>
    /// <returns></returns>
    partial void PrepareBuffer();

#if UNITY_EDITOR
    static ShaderTagId[] legacyShaderTagIds = {
        new ShaderTagId("Always"),
        new ShaderTagId("ForwardBase"),
        new ShaderTagId("PrepassBase"),
        new ShaderTagId("Vertex"),
        new ShaderTagId("VertexLMRGBM"),
        new ShaderTagId("VertexLM")
    };
    private static Material errorMaterial;
    private string SampleName { get; set; }

    partial void DrawUnsupportedShaders()
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

    // 摄像机内的操作面板
    partial void DrawGizmos()
    {
        if (Handles.ShouldRenderGizmos())
        {
            context.DrawGizmos(camera, GizmoSubset.PreImageEffects);
            context.DrawGizmos(camera, GizmoSubset.PreImageEffects);
        }
    }

    partial void PrepareForSceneWindow()
    {
        if (camera.cameraType == CameraType.SceneView)
        {
            ScriptableRenderContext.EmitWorldGeometryForSceneView(camera);
        }
    }

    partial void PrepareBuffer()
    {
        Profiler.BeginSample("Editor Only");
        // 区分frame调试器内多摄像头显示的作用区域
        buffer.name = SampleName = camera.name;
        Profiler.EndSample();
    }
#else
    const string SampleName = bufferName;
#endif
}
