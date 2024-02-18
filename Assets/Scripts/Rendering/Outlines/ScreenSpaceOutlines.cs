using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;

public class ScreenSpaceOutlines : ScriptableRendererFeature
{
    [System.Serializable]
    private class ViewSpaceNormalTextureSettings {
        [Header("General Scene View Space Normal Texture Settings")]
        public RenderTextureFormat colourFormat;
        public int depthBufferBits = 16;
        public FilterMode filterMode;
        public Color backgroundColour = Color.black;

        [Header("View Space Normal Texture Object Draw Settings")]
        public PerObjectData perObjectData;
        public bool enableDynamicBatching;
        public bool enableInstancing;
    }

    [System.Serializable]
    private class ScreenSpaceOutlineSettings
    {
        [Header("General Outline Settings")]
        public Color outlineColour = Color.black;
        [Range(0.0f, 20.0f)]
        public float outlineScale = 1;
        [Header("Depth Settings")]
        [Range(0.0f, 100.0f)]
        public float depthThreshold = 1.5f;
        [Range(0.0f, 500.0f)]
        public float robertsCrossMultiplier = 100f;

        [Header("Normal Settings")]
        [Range(0.0f,1.0f)]
        public float normalThreshold = 0.4f;

        [Header("Depth Normal Related Settings")]
        [Range(0.0f, 2.0f)]
        public float SteepAngleThreshold = 0.2f;
        [Range(0.0f, 500.0f)]
        public float SteepAngleMultiplier = 25f;
    }

    private class ViewSpaceNormalsTexturePass : ScriptableRenderPass
    {
        private ViewSpaceNormalTextureSettings normalTextureSettings;
        private FilteringSettings filteringSettings;
        private FilteringSettings occulderFilteringSettings;

        private readonly List<ShaderTagId> shaderTgIdList;
        private readonly Material normalsMaterial;
        private readonly Material occluderMaterial;

        private readonly RTHandle normals;

        public ViewSpaceNormalsTexturePass(RenderPassEvent renderPassEvent, LayerMask layerMask,
            LayerMask occluderLayerMask, ViewSpaceNormalTextureSettings normalTextureSettings)
        {
            this.renderPassEvent = renderPassEvent;
            this.normalTextureSettings = normalTextureSettings;
            filteringSettings = new FilteringSettings(RenderQueueRange.opaque, layerMask);
            occulderFilteringSettings = new FilteringSettings(RenderQueueRange.opaque,occluderLayerMask);

            shaderTgIdList = new List<ShaderTagId>
            {
                new("UniversalForward"),
                new("UniversalForwardOnly"),
                new("LightweightForward"),
                new("SRPDefaultUnlit"),
            };

            normals = RTHandles.Alloc("_SceneViewSpaceNormals", name: "_SceneViewSpaceNormals");
            normalsMaterial = new Material(Shader.Find("Hidden/ViewSpaceNormals"));

            occluderMaterial = new Material(Shader.Find("Hidden/UnlitColor"));
            occluderMaterial.SetColor("_Color", Color.blue);
        }

        public override void Configure(CommandBuffer cmd, RenderTextureDescriptor cameraTextureDescriptor)
        {
            RenderTextureDescriptor normalTextureDescriptor = cameraTextureDescriptor;
            normalTextureDescriptor.colorFormat = normalTextureSettings.colourFormat;
            normalTextureDescriptor.depthBufferBits = normalTextureSettings.depthBufferBits;
            cmd.GetTemporaryRT(Shader.PropertyToID(normals.name), normalTextureDescriptor, normalTextureSettings.filterMode);

            ConfigureTarget(normals);
            ConfigureClear(ClearFlag.All, normalTextureSettings.backgroundColour);
        }

        public override void Execute(ScriptableRenderContext context, ref RenderingData renderingData)
        {
            if (!normalsMaterial||!occluderMaterial)
            {
                return;
            }

            CommandBuffer cmd = CommandBufferPool.Get();
            using(new ProfilingScope(cmd, new ProfilingSampler("SceneViewSpaceNormalsTextureCreation")))
            {
                context.ExecuteCommandBuffer(cmd);
                cmd.Clear();

                DrawingSettings drawingSettings = CreateDrawingSettings(shaderTgIdList, ref renderingData, renderingData.cameraData.defaultOpaqueSortFlags);
                drawingSettings.perObjectData = normalTextureSettings.perObjectData;
                drawingSettings.enableDynamicBatching = normalTextureSettings.enableDynamicBatching;
                drawingSettings.enableInstancing = normalTextureSettings.enableInstancing;
                drawingSettings.overrideMaterial = normalsMaterial;

                DrawingSettings occluderSettings = drawingSettings;
                occluderSettings.overrideMaterial = occluderMaterial;

                context.DrawRenderers(renderingData.cullResults,ref drawingSettings,ref filteringSettings);
                context.DrawRenderers(renderingData.cullResults, ref occluderSettings, ref occulderFilteringSettings);
            }
            context.ExecuteCommandBuffer(cmd);
            CommandBufferPool.Release(cmd);
        }

        public override void OnCameraCleanup(CommandBuffer cmd)
        {
            cmd.ReleaseTemporaryRT(Shader.PropertyToID(normals.name));
        }
    }

    private class ScreenSpaceOutlinePass : ScriptableRenderPass
    {
        private readonly Material screenSpaceOutlineMaterial;
        private RenderTargetIdentifier cameraColourTarget;

        private RTHandle cameraColourTargetHandle;
        private RTHandle temporaryTargetHandle;
        private bool rtHandles = false;

        private RenderTargetIdentifier temporaryBuffer;
        private int temporaryBufferID = Shader.PropertyToID("_TemporaryBuffer");

        public ScreenSpaceOutlinePass(RenderPassEvent renderPassEvent, ScreenSpaceOutlineSettings screenSpaceOutlineSettings)
        {
            this.renderPassEvent = renderPassEvent;
            screenSpaceOutlineMaterial = new Material(Shader.Find("Hidden/Outlines"));

            screenSpaceOutlineMaterial.SetColor("_Color", screenSpaceOutlineSettings.outlineColour);
            screenSpaceOutlineMaterial.SetFloat("_OutlineScale", screenSpaceOutlineSettings.outlineScale);

            screenSpaceOutlineMaterial.SetFloat("_DepthThreshold", screenSpaceOutlineSettings.depthThreshold);
            screenSpaceOutlineMaterial.SetFloat("_RobertsCrossMultiplier", screenSpaceOutlineSettings.robertsCrossMultiplier);

            screenSpaceOutlineMaterial.SetFloat("_NormalThreshold", screenSpaceOutlineSettings.normalThreshold);

            screenSpaceOutlineMaterial.SetFloat("_SteepAngleThreshold", screenSpaceOutlineSettings.SteepAngleThreshold);
            screenSpaceOutlineMaterial.SetFloat("_SteepAngleMultiplier", screenSpaceOutlineSettings.SteepAngleMultiplier);
            //temporaryBuffer = RTHandles.Alloc("_TemporaryBuffer", name: "_TemporaryBuffer");
        }

        public override void OnCameraSetup(CommandBuffer cmd, ref RenderingData renderingData)
        {
            RenderTextureDescriptor temporaryTargetDescriptor = renderingData.cameraData.cameraTargetDescriptor;
            temporaryTargetDescriptor.depthBufferBits = 0;
            if (rtHandles)
            {
                cameraColourTargetHandle = renderingData.cameraData.renderer.cameraColorTargetHandle;
                RenderingUtils.ReAllocateIfNeeded(ref temporaryTargetHandle, temporaryTargetDescriptor, name: "_TemporaryBuffer");
            }
            else
            {
                temporaryBuffer = new RenderTargetIdentifier(temporaryBufferID);
                cameraColourTarget = renderingData.cameraData.renderer.cameraColorTarget;
            }
        }

        public override void Execute(ScriptableRenderContext context, ref RenderingData renderingData)
        {
            if (!screenSpaceOutlineMaterial)
            {
                return;
            }
            CommandBuffer cmd = CommandBufferPool.Get();
            if (rtHandles)
            {
                NewRTHandles(cmd);
            }
            else
            {
                ObseleteRTHandles(cmd);
            }
            
            context.ExecuteCommandBuffer(cmd);
            CommandBufferPool.Release(cmd);
        }

        private void ObseleteRTHandles(CommandBuffer cmd)
        {
            using (new ProfilingScope(cmd, new ProfilingSampler("ScreenSpaceOutlines")))
            {
                Blit(cmd, cameraColourTarget, temporaryBuffer);
                Blit(cmd, temporaryBuffer, cameraColourTarget, screenSpaceOutlineMaterial);
            }
        }

        private void NewRTHandles(CommandBuffer cmd)
        {
            using (new ProfilingScope(cmd, new ProfilingSampler("ScreenSpaceOutlines")))
            {
                //Blit(cmd, cameraColourTargetHandle, temporaryTargetHandle);
                Blitter.BlitCameraTexture(cmd, cameraColourTargetHandle, cameraColourTargetHandle, screenSpaceOutlineMaterial,0);
                //Blit(cmd, cameraColourTargetHandle, cameraColourTargetHandle, screenSpaceOutlineMaterial);
            }
        }

        public override void OnCameraCleanup(CommandBuffer cmd)
        {
            if(!rtHandles)
                cmd.ReleaseTemporaryRT(temporaryBufferID);
        }

        public void Dispose()
        {
            temporaryTargetHandle?.Release();
        }
    }

    private class ColorBlitPass : ScriptableRenderPass
    {
        ProfilingSampler m_ProfilingSampler = new ProfilingSampler("ColorBlit");
        Material m_Material;
        RTHandle m_CameraColorTarget;

        public ColorBlitPass(RenderPassEvent renderPassEvent, Material material,ScreenSpaceOutlineSettings screenSpaceOutlineSettings)
        {
            m_Material = material; //new Material(Shader.Find("Hidden/Outlines"));
            m_Material.SetColor("_Color", screenSpaceOutlineSettings.outlineColour);
            m_Material.SetFloat("_OutlineScale", screenSpaceOutlineSettings.outlineScale);

            m_Material.SetFloat("_DepthThreshold", screenSpaceOutlineSettings.depthThreshold);
            m_Material.SetFloat("_RobertsCrossMultiplier", screenSpaceOutlineSettings.robertsCrossMultiplier);

            m_Material.SetFloat("_NormalThreshold", screenSpaceOutlineSettings.normalThreshold);

            m_Material.SetFloat("_SteepAngleThreshold", screenSpaceOutlineSettings.SteepAngleThreshold);
            m_Material.SetFloat("_SteepAngleMultiplier", screenSpaceOutlineSettings.SteepAngleMultiplier);
            this.renderPassEvent = renderPassEvent;
        }

        public void SetTarget(RTHandle colorHandle)
        {
            m_CameraColorTarget = colorHandle;
        }

        public override void OnCameraSetup(CommandBuffer cmd, ref RenderingData renderingData)
        {
            ConfigureTarget(m_CameraColorTarget);
        }

        public override void Execute(ScriptableRenderContext context, ref RenderingData renderingData)
        {
            var cameraData = renderingData.cameraData;
            if (cameraData.camera.cameraType != CameraType.Game)
                return;

            if (m_Material == null)
                return;

            CommandBuffer cmd = CommandBufferPool.Get();
            using (new ProfilingScope(cmd, m_ProfilingSampler))
            {
                Blitter.BlitCameraTexture(cmd, m_CameraColorTarget, m_CameraColorTarget, m_Material, 0);
            }
            context.ExecuteCommandBuffer(cmd);
            cmd.Clear();

            CommandBufferPool.Release(cmd);
        }
    }

    [SerializeField] private RenderPassEvent renderPassEvent = RenderPassEvent.AfterRenderingOpaques;
    [SerializeField] private LayerMask outlinesLayerMask;
    [SerializeField] private LayerMask outlinesOccluderLayerMask;
    // private Material outlineMaterial;
    // [SerializeField] private Shader outlineShader;
    [SerializeField] private ScreenSpaceOutlineSettings screenSpaceOutlineSettings;
    [SerializeField] private ViewSpaceNormalTextureSettings viewSpaceNormalsTextureSettings;

    private ViewSpaceNormalsTexturePass viewSpaceNormalsTexturePass;
    private ScreenSpaceOutlinePass screenSpaceOutlinePass;
    private ColorBlitPass exampleBlitPass;

    public override void Create()
    {
        viewSpaceNormalsTexturePass = new ViewSpaceNormalsTexturePass(renderPassEvent, outlinesLayerMask, outlinesOccluderLayerMask, viewSpaceNormalsTextureSettings);
        screenSpaceOutlinePass = new ScreenSpaceOutlinePass(renderPassEvent,screenSpaceOutlineSettings);
        //outlineMaterial = CoreUtils.CreateEngineMaterial(outlineShader);
        //exampleBlitPass = new ColorBlitPass(renderPassEvent, outlineMaterial, screenSpaceOutlineSettings);
    }

    public override void AddRenderPasses(ScriptableRenderer renderer, ref RenderingData renderingData)
    {
        renderer.EnqueuePass(viewSpaceNormalsTexturePass);
        //if (renderingData.cameraData.cameraType == CameraType.Game)
        //    renderer.EnqueuePass(exampleBlitPass);
        renderer.EnqueuePass(screenSpaceOutlinePass);
    }

    //public override void SetupRenderPasses(ScriptableRenderer renderer, in RenderingData renderingData)
    //{
    //    if (renderingData.cameraData.cameraType == CameraType.Game)
    //    {
    //        // Calling ConfigureInput with the ScriptableRenderPassInput.Color argument
    //        // ensures that the opaque texture is available to the Render Pass.
    //        exampleBlitPass.ConfigureInput(ScriptableRenderPassInput.Color);
    //        exampleBlitPass.SetTarget(renderer.cameraColorTargetHandle);
    //    }
    //}

    protected override void Dispose(bool disposing)
    {
        base.Dispose(disposing);
        if (disposing)
        {
            screenSpaceOutlinePass.Dispose();
        }
        //CoreUtils.Destroy(outlineMaterial);
    }
}
