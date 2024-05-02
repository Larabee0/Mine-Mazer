using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;

public class SetVolume : MonoBehaviour
{
    [SerializeField] private Volume volume;
    void Start()
    {
        VolumeComponent comp = volume.profile.components.Find(comp => comp.GetType() == typeof(ColorAdjustments));
        if (comp != null && comp is ColorAdjustments adjustments)
        {
            var settings = PlayerSettings.Instance.userSettings;
            adjustments.postExposure.value = settings.postExposureAdj;
            adjustments.contrast.value = settings.constrastAdj;
        }
    }
}
