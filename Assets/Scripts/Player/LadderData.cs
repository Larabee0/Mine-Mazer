using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class LadderData : MonoBehaviour, IInteractable
{
    [SerializeField] private BoxCollider ladderTop;
    [SerializeField] private BoxCollider ladderBottom;
    [SerializeField] private BoxCollider ladderMain;

    public Transform Top => ladderTop.transform;
    public Transform Bottom => ladderBottom.transform;
    public Transform Main => transform;

    public string GetToolTipText()
    {
        return "ladder";
    }

    public void Interact()
    {
        FindAnyObjectByType<AutoLadder>().ClimbLadder();  
    }

    public bool RequiresPickaxe()
    {
        return false;
    }

    private void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.green;
        Gizmos.DrawRay(Main.position, Main.up);
        Gizmos.DrawRay(Top.position, Top.up);
        Gizmos.DrawRay(Bottom.position, Bottom.up);
    }

}
