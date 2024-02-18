using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering.Universal;

public class MultiPassRendererFeature : ScriptableRendererFeature
{

    public List<string> lightModePasses;
    private MultiPassPass mainPass;

    public override void AddRenderPasses(ScriptableRenderer renderer, ref RenderingData renderingData)
    {

    }

    public override void Create()
    {

    }
}
