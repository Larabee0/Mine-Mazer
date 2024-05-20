using UnityEngine;

public class AutoLadderActivator : MonoBehaviour
{
    [SerializeField] private LadderData ladder;

    private void OnTriggerEnter(Collider other)
    {
        ladder.OnTriggerEnterFromChild(other);
    }

    private void OnTriggerExit(Collider other)
    {
        ladder.OnTriggerExitFromChild(other);
    }
}