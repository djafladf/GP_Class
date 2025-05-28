using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;

public class ScreenShader : ScriptableRendererFeature
{
    public class BlurPass : ScriptableRenderPass
    {
        const string ProfilerTag = "Blur Pass";

        Material Mat;
        RenderTextureDescriptor texDes;
        RTHandle texHand;

        public BlurPass(Material Mat, RenderPassEvent renderPassEvent) 
        { 
            this.Mat = Mat; this.renderPassEvent = renderPassEvent;
            texDes = new RenderTextureDescriptor(Screen.width,Screen.height, RenderTextureFormat.Default, 0);
        }

        public override void Configure(CommandBuffer cmd, RenderTextureDescriptor cameraTextureDescriptor)
        {
            texDes.width = cameraTextureDescriptor.width; texDes.height = cameraTextureDescriptor.height;
            RenderingUtils.ReAllocateIfNeeded(ref texHand, texDes);

            ConfigureInput(ScriptableRenderPassInput.Color);
        }
        
        public override void Execute(ScriptableRenderContext context, ref RenderingData renderingData)
        {
            if (Mat == null) return;
            CommandBuffer cmd = CommandBufferPool.Get();
            RTHandle cameraTargetHandle = renderingData.cameraData.renderer.cameraColorTargetHandle;
            if (!cameraTargetHandle.rt)
            {
                Debug.LogError("cameraTargetHandle is invalid!");
                return;
            }
            Blit(cmd, cameraTargetHandle,texHand,Mat);
            Blit(cmd, texHand,cameraTargetHandle,Mat);
            context.ExecuteCommandBuffer(cmd);
            CommandBufferPool.Release(cmd);
        }

        public override void OnCameraCleanup(CommandBuffer cmd)
        {
            texHand?.Release();
            texHand = null;
        }
    }

    [SerializeField] Shader shad;
    Material mat;
    BlurPass Pass;

    public override void Create()
    {
        if (shad == null) return;
        mat = CoreUtils.CreateEngineMaterial(shad);
        Pass = new BlurPass(mat, RenderPassEvent.AfterRendering);
    }

    public override void AddRenderPasses(ScriptableRenderer renderer, ref RenderingData renderingData)
    {
        renderer.EnqueuePass(Pass);
    }

    protected override void Dispose(bool disposing)
    {
        CoreUtils.Destroy(mat);
    }
}
