using UnityEngine;
using UnityEngine.Rendering;

[CreateAssetMenu(menuName = "MineCustomRender/Custom Render Pipeline")]
public class CustomRenderPiplineAsset : RenderPipelineAsset
{
    /// <summary>
    /// SRP 批处理程序有优先权 要启用动态批处理需要关闭SRP
    /// </summary>
    [SerializeField]
    bool useDynamicBatching = true, useGPUInstancing = true, useSRPBatcher = true;
    protected override RenderPipeline CreatePipeline()
    {
        return new CustomRenderPipline(useDynamicBatching, useGPUInstancing, useSRPBatcher);
    }
}
