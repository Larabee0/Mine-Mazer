using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class ItemSoundEffect : MonoBehaviour
{
    [SerializeField]private AudioClip[] clips;
    [SerializeField]private AudioSource source;
    [SerializeField] private MapResource mapResource;

    private void OnEnable()
    {
        mapResource = GetComponent<MapResource>();
        mapResource.OnInventoryItemInteract += PlayRandomClip;
    }

    private void PlayRandomClip()
    {
        if (clips.Length ==0) return;
        if(source.isPlaying) return;
        source.PlayOneShot(clips[Random.Range(0, clips.Length)]);        
    }

    private void OnDisable()
    {
        mapResource.OnInventoryItemInteract -= PlayRandomClip;
    }
}
