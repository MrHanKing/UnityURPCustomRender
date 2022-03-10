using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;

public class ColorEffectBlurFeature : ScriptableRendererFeature
{
    [System.Serializable]
    public class PassSettings
    {
        // Where/when the render pass should be injected during the rendering process.
        public RenderPassEvent renderPassEvent = RenderPassEvent.AfterRenderingTransparents;

        // Used for any potential down-sampling we will do in the pass.
        [Range(1, 4)] public int downsample = 1;

        // A variable that's specific to the use case of our pass.
        [Range(0, 20)] public int blurStrength = 5;

    }
    public PassSettings passSettings = new PassSettings();
    ColorEffectBlurPass m_ScriptablePass;

    public override void Create()
    {
        m_ScriptablePass = new ColorEffectBlurPass(passSettings);
        // Configures where the render pass should be injected.
        // m_ScriptablePass.renderPassEvent = RenderPassEvent.AfterRenderingPostProcessing;
    }

    // Here you can inject one or multiple render passes in the renderer.
    // This method is called when setting up the renderer once per-camera.
    public override void AddRenderPasses(ScriptableRenderer renderer, ref RenderingData renderingData)
    {
        var cameraColorTarget = renderer.cameraColorTarget;
        m_ScriptablePass.SetCameraColorBuffer(cameraColorTarget);
        renderer.EnqueuePass(m_ScriptablePass);
    }
}


