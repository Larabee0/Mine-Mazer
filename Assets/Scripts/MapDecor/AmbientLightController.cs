using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class AmbientLightController : MonoBehaviour
{
    private static AmbientLightController instance;
    public static AmbientLightController Instance
    {
        get
        {
            if (instance == null)
            {
                Debug.LogWarning("Expected Ambient Light Controller instance not found. Order of operations issue? Or Ambient Light Controller is disabled/missing.");
            }
            return instance;
        }
        private set
        {
            if (value != null && instance == null)
            {
                instance = value;
            }
        }
    }

    [SerializeField] private float targetAmbientIntensity;
    [SerializeField] private float fadeSpeed = 1f;
    private Coroutine allProcess = null;
    private void Awake()
    {
        Instance = this;
    }

    public void FadeAmbientLight(float targetInensity, float fadeDuration)
    {
        if (targetAmbientIntensity == targetInensity) { return; }
        if (targetInensity < 0 || targetInensity > 1.0f) { return; }
        if(allProcess != null)
        {
            StopCoroutine(allProcess);
            allProcess = null;
        }
        allProcess = StartCoroutine(FadeAmbientLightUnsafe(targetInensity, fadeDuration));
    }

    public void FadeAmbientLight(float targetInensity)
    {
        if (targetAmbientIntensity == targetInensity) { return; }
        if (targetInensity < 0 || targetInensity > 1.0f) { return; }
        if (allProcess != null)
        {
            StopCoroutine(allProcess);
            allProcess = null;
        }
        allProcess = StartCoroutine(FadeAmbientLightUnsafe(targetInensity));
    }

    private IEnumerator FadeAmbientLightUnsafe(float target, float duration)
    {
        targetAmbientIntensity = target;
        float initialAmbientIntensity = RenderSettings.ambientIntensity;

        for (float t = 0; t < duration; t += Time.deltaTime)
        {
            yield return null;
            float fader = Mathf.InverseLerp(0, duration, t);
            RenderSettings.ambientIntensity = Mathf.Lerp(initialAmbientIntensity, targetAmbientIntensity, fader);
        }
        allProcess = null;
    }

    private IEnumerator FadeAmbientLightUnsafe(float target)
    {
        targetAmbientIntensity = target;
        float initialAmbientIntensity = RenderSettings.ambientIntensity;

        for (float t = 0; t < 1; t += Time.deltaTime * fadeSpeed)
        {
            yield return null;
            float fader = Mathf.InverseLerp(0, 1, t);
            RenderSettings.ambientIntensity = Mathf.Lerp(initialAmbientIntensity, targetAmbientIntensity, fader);
        }
        allProcess = null;
    }
}
