using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;

public class ScreenSpaceOutlines : ScriptableRendererFeature
{
    [System.Serializable]
    private class ViewSpaceNormalTextureSettings {
        public RenderTextureFormat colourFormat = RenderTextureFormat.ARGB32;
        public int depthBufferBits = 4;
        public FilterMode filterMode = FilterMode.Point;
        public Color backgroundColour = Color.red;
    }

    private class ViewSpaceNormalsTexturePass : ScriptableRenderPass
    {
        private ViewSpaceNormalTextureSettings normalTextureSettings;
        private FilteringSettings filteringSettings;
        private readonly List<ShaderTagId> shaderTgIdList;
        //private readonly RenderTargetHandle normals;
        private readonly RTHandle normals;
        private readonly Material normalsMaterial;
        private DrawingSettings occulderSettings;
        private FilteringSettings occulderFilteringSettings;

        public ViewSpaceNormalsTexturePass(RenderPassEvent renderPassEvent, DrawingSettings occulderSettings,
            FilteringSettings occulderFilteringSettings,
            LayerMask outlinesLayerMask, ViewSpaceNormalTextureSettings normalTextureSettings)
        {
            normalsMaterial = new Material(Shader.Find("Shader Graphs/ViewSpaceNormalsShader"));
            shaderTgIdList = new List<ShaderTagId>
            {
                new("UniversalForward"),
                new("UniversalForwardOnly"),
                new("LightweightForward"),
                new("SRPDefaultUnlit"),
            };

            this.renderPassEvent = renderPassEvent;
            this.normalTextureSettings = normalTextureSettings;
            this.occulderSettings = occulderSettings;
            this.occulderFilteringSettings = occulderFilteringSettings;
            //normals.Init("_SceneViewSpaceNormals");
            normals = RTHandles.Alloc("_SceneViewSpaceNormals", name: "_SceneViewSpaceNormals");
            this.occulderFilteringSettings = filteringSettings = new FilteringSettings(RenderQueueRange.opaque, outlinesLayerMask);
        }

        public override void Configure(CommandBuffer cmd, RenderTextureDescriptor cameraTextureDescriptor)
        {
            RenderTextureDescriptor normalTextureDescriptor = cameraTextureDescriptor;
            normalTextureDescriptor.colorFormat = normalTextureSettings.colourFormat;
            normalTextureDescriptor.depthBufferBits = normalTextureSettings.depthBufferBits;
            //cmd.GetTemporaryRT(normals.id, normalTextureDescriptor, normalTextureSettings.filterMode);
            cmd.GetTemporaryRT(Shader.PropertyToID(normals.name), normalTextureDescriptor, normalTextureSettings.filterMode);

            //ConfigureTarget(normals.Identifier());
            ConfigureTarget(normals);
            ConfigureClear(ClearFlag.All, normalTextureSettings.backgroundColour);
        }

        public override void Execute(ScriptableRenderContext context, ref RenderingData renderingData)
        {
            if (!normalsMaterial)
            {
                return;
            }
            CommandBuffer cmd = CommandBufferPool.Get();
            using(new ProfilingScope(cmd, new ProfilingSampler("SceneViewSpaceNormalsTextureCreation")))
            {
                context.ExecuteCommandBuffer(cmd);
                cmd.Clear();
                DrawingSettings drawingSettings = CreateDrawingSettings(shaderTgIdList, ref renderingData, renderingData.cameraData.defaultOpaqueSortFlags);
                drawingSettings.overrideMaterial =normalsMaterial;

                context.DrawRenderers(renderingData.cullResults,ref drawingSettings,ref filteringSettings);
                context.DrawRenderers(renderingData.cullResults, ref occulderSettings, ref occulderFilteringSettings);
            }
            context.ExecuteCommandBuffer(cmd);
            CommandBufferPool.Release(cmd);
        }

        public override void OnCameraCleanup(CommandBuffer cmd)
        {
            //cmd.ReleaseTemporaryRT(normals.id);
            cmd.ReleaseTemporaryRT(Shader.PropertyToID(normals.name));
        }
    }

    private class ScreenSpaceOutlinePass : ScriptableRenderPass
    {
        private readonly Material screenSpaceOutlineMaterial;
        private RTHandle cameraColourTarget;
        private RTHandle temporaryBuffer;
        private int temporaryBufferID = Shader.PropertyToID("_TemporaryBuffer");
        public ScreenSpaceOutlinePass(RenderPassEvent renderPassEvent)
        {
            this.renderPassEvent = renderPassEvent;
            screenSpaceOutlineMaterial = new Material(Shader.Find("Shader Graphs/OutlineShader"));
            temporaryBuffer = RTHandles.Alloc("_TemporaryBuffer", name: "_TemporaryBuffer");
        }

        public override void OnCameraSetup(CommandBuffer cmd, ref RenderingData renderingData)
        {
            cameraColourTarget = renderingData.cameraData.renderer.cameraColorTargetHandle;
            cmd.GetTemporaryRT(temporaryBufferID, renderingData.cameraData.cameraTargetDescriptor);
        }
        public override void Configure(CommandBuffer cmd, RenderTextureDescriptor cameraTextureDescriptor)
        {
            RenderingUtils.ReAllocateIfNeeded(ref temporaryBuffer, cameraTextureDescriptor, name: "_TemporaryBuffer");
        }
        public override void Execute(ScriptableRenderContext context, ref RenderingData renderingData)
        {
            if (!screenSpaceOutlineMaterial)
            {
                return;
            }
            CommandBuffer cmd =CommandBufferPool.Get();
            using(new ProfilingScope(cmd,new ProfilingSampler("ScreenSpaceOutlines")))
            {
                Blit(cmd, cameraColourTarget, temporaryBuffer);
                Blit(cmd, temporaryBuffer, cameraColourTarget, screenSpaceOutlineMaterial);
            }
            context.ExecuteCommandBuffer(cmd);
            CommandBufferPool.Release(cmd);
        }
        public override void OnCameraCleanup(CommandBuffer cmd)
        {
            cmd.ReleaseTemporaryRT(temporaryBufferID);
        }

        public void Dispose()
        {
            temporaryBuffer?.Release();
        }
    }

    [SerializeField] private RenderPassEvent renderPassEvent;
    [SerializeField] private ViewSpaceNormalTextureSettings viewSpaceNormalsTextureSettings;
    [SerializeField] private LayerMask outlinesLayerMask;

    [SerializeField] private DrawingSettings occulderSettings;
    [SerializeField] private FilteringSettings occulderFilteringSettings;
    private ViewSpaceNormalsTexturePass viewSpaceNormalsTexturePass;
    private ScreenSpaceOutlinePass screenSpaceOutlinePass;


    public override void Create()
    {
        viewSpaceNormalsTexturePass = new ViewSpaceNormalsTexturePass(renderPassEvent, occulderSettings, occulderFilteringSettings, outlinesLayerMask, viewSpaceNormalsTextureSettings);
        screenSpaceOutlinePass = new ScreenSpaceOutlinePass(renderPassEvent);
    }

    public override void AddRenderPasses(ScriptableRenderer renderer, ref RenderingData renderingData)
    {
        renderer.EnqueuePass(viewSpaceNormalsTexturePass);
        renderer.EnqueuePass(screenSpaceOutlinePass);
    }

    protected override void Dispose(bool disposing)
    {
        base.Dispose(disposing);
        if (disposing)
        {
            screenSpaceOutlinePass.Dispose();
        }
    }
}
