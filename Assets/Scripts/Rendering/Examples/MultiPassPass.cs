using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;

public class MultiPassPass : ScriptableRenderPass
{
    private List<ShaderTagId> m_Tags;

    public MultiPassPass(List<string> tags)
    {
        m_Tags = new List<ShaderTagId>(tags.Count);
        tags.ForEach(tag => m_Tags.Add(new ShaderTagId(tag)));

        renderPassEvent = RenderPassEvent.AfterRenderingOpaques;
    }

    public override void Execute(ScriptableRenderContext context, ref RenderingData renderingData)
    {
        FilteringSettings filteringSettings = FilteringSettings.defaultValue;

        for (int i = 0; i < m_Tags.Count; i++)
        {
            ShaderTagId pass = m_Tags[i];
            DrawingSettings drawingSettings = CreateDrawingSettings(pass, ref renderingData, SortingCriteria.CommonOpaque);
            context.DrawRenderers(renderingData.cullResults, ref drawingSettings, ref filteringSettings);
        }
        context.Submit();
    }
}
