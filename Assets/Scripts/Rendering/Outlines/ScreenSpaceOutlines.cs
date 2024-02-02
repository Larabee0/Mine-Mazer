using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;

public class ScreenSpaceOutlines : ScriptableRendererFeature
{
    [System.Serializable]
    private class ViewSpaceNormalTextureSettings {
        public RenderTextureFormat colourFormat;
        public int depthBufferBits;
        public FilterMode filterMode = FilterMode.Point;
        public Color backgroundColour;
    }
    private class ViewSpaceNormalsTexturePass : ScriptableRenderPass
    {
        private ViewSpaceNormalTextureSettings normalTextureSettings;
        private readonly List<ShaderTagId> shaderTgIdList;
        private readonly RenderTargetHandle normals;
        private readonly Material normalsMaterial;

        public ViewSpaceNormalsTexturePass(RenderPassEvent renderPassEvent, ViewSpaceNormalTextureSettings normalTextureSettings)
        {
            normalsMaterial = new Material(Shader.Find("Hidden/ViewSpaceNormalsShader"));
            shaderTgIdList = new List<ShaderTagId>
            {
                new ShaderTagId("UniversalForward"),
                new ShaderTagId("UniversalForwardOnly"),
                new ShaderTagId("LightweightForward"),
                new ShaderTagId("SRPDefaultUnlit"),
            };

            this.renderPassEvent = renderPassEvent;
            this.normalTextureSettings = normalTextureSettings;
            normals.Init("_ScreenViewSpaceNormals");
        }

        public override void Configure(CommandBuffer cmd, RenderTextureDescriptor cameraTextureDescriptor)
        {
            RenderTextureDescriptor normalTextureDescriptor = cameraTextureDescriptor;
            normalTextureDescriptor.colorFormat = normalTextureSettings.colourFormat;
            normalTextureDescriptor.depthBufferBits = normalTextureSettings.depthBufferBits;
            cmd.GetTemporaryRT(normals.id, normalTextureDescriptor, normalTextureSettings.filterMode);

            ConfigureTarget(normals.Identifier());
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
                    FilteringSettings filteringSettings = FilteringSettings.defaultValue;

                context.DrawRenderers(renderingData.cullResults,ref drawingSettings,ref filteringSettings);
            }
            context.ExecuteCommandBuffer(cmd);
            CommandBufferPool.Release(cmd);
        }

        public override void OnCameraCleanup(CommandBuffer cmd)
        {
            cmd.ReleaseTemporaryRT(normals.id);
        }
    }

    private class ScreenSpaceOutlinePass : ScriptableRenderPass
    {
        public ScreenSpaceOutlinePass(RenderPassEvent renderPassEvent)
        {
            this.renderPassEvent = renderPassEvent;
        }

        public override void Execute(ScriptableRenderContext context, ref RenderingData renderingData)
        {

        }
    }

    [SerializeField] private RenderPassEvent renderPassEvent;
    [SerializeField] private ViewSpaceNormalTextureSettings viewSpaceNormalsTextureSettings;
    private ViewSpaceNormalsTexturePass viewSpaceNormalsTexturePass;
    private ScreenSpaceOutlinePass screenSpaceOutlinePass;


    public override void Create()
    {
        viewSpaceNormalsTexturePass = new ViewSpaceNormalsTexturePass(renderPassEvent, viewSpaceNormalsTextureSettings);
        screenSpaceOutlinePass = new ScreenSpaceOutlinePass(renderPassEvent);
    }

    public override void AddRenderPasses(ScriptableRenderer renderer, ref RenderingData renderingData)
    {
        renderer.EnqueuePass(viewSpaceNormalsTexturePass);
        renderer.EnqueuePass(screenSpaceOutlinePass);
    }
}
