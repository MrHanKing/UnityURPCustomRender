using UnityEngine;
using UnityEngine.Rendering;

[CreateAssetMenu(menuName = "MineCustomRender/Custom Render Pipeline")]
public class CustomRenderPiplineAsset : RenderPipelineAsset
{
    protected override RenderPipeline CreatePipeline()
    {
        return new CustomRenderPipline();
    }
}
