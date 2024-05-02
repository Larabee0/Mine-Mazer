using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class AmbientController : MonoBehaviour
{
    private static AmbientController instance;
    public static AmbientController Instance
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

    [SerializeField] private AudioSource ambientNoiseMaker;
    [SerializeField] private AudioSource transitoryNoiseMaker;
    private AudioClip nextClip;
    private Coroutine audioProcess = null;

    [SerializeField] private bool torchAmbientBoost;
    [SerializeField] private float torchAmbientBoostIntensity = 0.5f;
    [SerializeField] private float targetAmbientIntensity;
    [SerializeField] private float ambientFromSection;
    [SerializeField] private float fadeSpeed = 1f;

    private Coroutine allProcess = null;

    private void Awake()
    {
        Instance = this;
        ambientNoiseMaker = FindAnyObjectByType<TutorialStarter>().GetComponent<AudioSource>();
        transitoryNoiseMaker = gameObject.AddComponent<AudioSource>();
        transitoryNoiseMaker.loop = true;
        transitoryNoiseMaker.volume = 0;
    }

    public void ChangeTune(AudioClip newClip)
    {
        if(newClip == null || nextClip == newClip)
        {
            return;
        }
        nextClip = newClip;
        audioProcess ??= StartCoroutine(FadeAmbience(newClip));
    }

    private IEnumerator FadeAmbience(AudioClip newClip)
    {
        transitoryNoiseMaker.clip = newClip;
        transitoryNoiseMaker.Play();
        for (float t = 0; t < 1; t += Time.deltaTime * fadeSpeed)
        {
            yield return null;
            float fader = Mathf.InverseLerp(0, 1, t);
            transitoryNoiseMaker.volume = fader;
            ambientNoiseMaker.volume = 1 - fader;
        }
        (ambientNoiseMaker, transitoryNoiseMaker) = (transitoryNoiseMaker, ambientNoiseMaker);
        ambientNoiseMaker.volume = 1;
        transitoryNoiseMaker.volume = 0;
        audioProcess = null;
        if (nextClip != null && newClip != nextClip)
        {
            ChangeTune(nextClip);
        }
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

    public void AmbientTorchLightBoost(bool boost)
    {
        if(torchAmbientBoost == boost) { return; }
        torchAmbientBoost = boost;
        FadeAmbientLight(targetAmbientIntensity, true);
    }

    public void FadeAmbientLight(float targetInensity, bool force = false)
    {
        if (!force && targetAmbientIntensity == targetInensity) { return; }
        if (!force && (targetInensity < 0 || targetInensity > 1.0f)) { return; }
        if (!force) { ambientFromSection = targetInensity; }
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
        if (torchAmbientBoost && targetAmbientIntensity == ambientFromSection)
        {
            targetAmbientIntensity = ambientFromSection + torchAmbientBoostIntensity;
        }
        else if (!torchAmbientBoost && targetAmbientIntensity != ambientFromSection)
        {
            targetAmbientIntensity = ambientFromSection;
        }
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
