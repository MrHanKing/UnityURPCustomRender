using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;

class ColorEffectBlurPass : ScriptableRenderPass
{
    // The profiler tag that will show up in the frame debugger.
    const string ProfilerTag = "Template Pass";

    // We will store our pass settings in this variable.
    ColorEffectBlurFeature.PassSettings passSettings;

    // 从颜色缓冲区 -> 临时缓冲区 处理后 ->颜色缓冲区
    RenderTargetIdentifier colorBuffer, temporaryBuffer;
    int temporaryBufferID = Shader.PropertyToID("_TemporaryBuffer");

    Material material;

    // It is good to cache the shader property IDs here.
    static readonly int BlurStrengthProperty = Shader.PropertyToID("_BlurStrength");

    public ColorEffectBlurPass(ColorEffectBlurFeature.PassSettings passSettings)
    {
        this.passSettings = passSettings;

        // Set the render pass event.
        renderPassEvent = passSettings.renderPassEvent;

        // We create a material that will be used during our pass. You can do it like this using the 'CreateEngineMaterial' method, giving it
        // a shader path as an input or you can use a 'public Material material;' field in your pass settings and access it here through 'passSettings.material'.
        if (material == null) material = CoreUtils.CreateEngineMaterial("CustomURP/Blur");

        // Set any material properties based on our pass settings. 
        material.SetInt(BlurStrengthProperty, passSettings.blurStrength);
    }

    // This method is called before executing the render pass.
    // It can be used to configure render targets and their clear state. Also to create temporary render target textures.
    // When empty this render pass will render to the active camera render target.
    // You should never call CommandBuffer.SetRenderTarget. Instead call <c>ConfigureTarget</c> and <c>ConfigureClear</c>.
    // The render pipeline will ensure target setup and clearing happens in an performance manner.
    public override void Configure(CommandBuffer cmd, RenderTextureDescriptor cameraTextureDescriptor)
    {
        // Grab the camera target descriptor. We will use this when creating a temporary render texture.
        RenderTextureDescriptor descriptor = cameraTextureDescriptor;

        // Downsample the original camera target descriptor. 
        // You would do this for performance reasons or less commonly, for aesthetics.
        descriptor.width /= passSettings.downsample;
        descriptor.height /= passSettings.downsample;

        // Set the number of depth bits we need for our temporary render texture.
        descriptor.depthBufferBits = 0;

        // Enable these if your pass requires access to the CameraDepthTexture or the CameraNormalsTexture.
        // ConfigureInput(ScriptableRenderPassInput.Depth);
        // ConfigureInput(ScriptableRenderPassInput.Normal);

        // Grab the color buffer from the renderer camera color target.
        // colorBuffer = renderingData.cameraData.renderer.cameraColorTarget;

        // Create a temporary render texture using the descriptor from above.
        cmd.GetTemporaryRT(temporaryBufferID, descriptor, FilterMode.Bilinear);
        temporaryBuffer = new RenderTargetIdentifier(temporaryBufferID);
    }

    public void SetCameraColorBuffer(RenderTargetIdentifier cameraColorTarget)
    {
        colorBuffer = cameraColorTarget;
    }

    // Here you can implement the rendering logic.
    // Use <c>ScriptableRenderContext</c> to issue drawing commands or execute command buffers
    // https://docs.unity3d.com/ScriptReference/Rendering.ScriptableRenderContext.html
    // You don't have to call ScriptableRenderContext.submit, the render pipeline will call it at specific points in the pipeline.
    public override void Execute(ScriptableRenderContext context, ref RenderingData renderingData)
    {

        // Grab a command buffer. We put the actual execution of the pass inside of a profiling scope.
        CommandBuffer cmd = CommandBufferPool.Get();
        using (new ProfilingScope(cmd, new ProfilingSampler(ProfilerTag)))
        {
            // Blit from the color buffer to a temporary buffer and back. This is needed for a two-pass shader.
            Blit(cmd, colorBuffer, temporaryBuffer, material, 0); // shader pass 0
            Blit(cmd, temporaryBuffer, colorBuffer, material, 1); // shader pass 1
        }

        // Execute the command buffer and release it.
        context.ExecuteCommandBuffer(cmd);
        CommandBufferPool.Release(cmd);
    }

    /// Cleanup any allocated resources that were created during the execution of this render pass.
    public override void FrameCleanup(CommandBuffer cmd)
    {
        if (cmd == null) throw new ArgumentNullException("cmd");

        // Since we created a temporary render texture in OnCameraSetup, we need to release the memory here to avoid a leak.
        cmd.ReleaseTemporaryRT(temporaryBufferID);
    }
}