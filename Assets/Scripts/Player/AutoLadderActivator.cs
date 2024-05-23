using UnityEngine;

public class AutoLadderActivator : MonoBehaviour
{
    [SerializeField] private LadderData ladder;

    private void OnTriggerEnter(Collider other)
    {
        ladder.OnTriggerEnterFromChild(other,GetComponent<BoxCollider>());
    }

    private void OnTriggerExit(Collider other)
    {
        ladder.OnTriggerExitFromChild(other);
    }
}