using MazeGame.Input;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class LadderData : MonoBehaviour, IInteractable
{
    public BoxCollider ladderTop;
    public BoxCollider ladderBottom;
    public BoxCollider ladderMain;
    private bool forceLadderInteractable;
    public bool climbing = false;
    public BoxCollider otherCollider = null;
    private bool otherColliderHit = false;

    public Transform Top => ladderTop.transform;
    public Transform Bottom => ladderBottom.transform;
    public Transform Main => transform;

    public string GetToolTipText()
    {
        if (InputManager.GamePadPresent)
        {
            return string.Format("LT to climb ladder");
        }
        else
        {
            return string.Format("Right Click to climb ladder");
        }
    }

    public void Interact()
    {
        FindAnyObjectByType<AutoLadder>().ClimbLadder();  
    }

    public void OnTriggerEnterFromChild(Collider other, BoxCollider us)
    {
        if (climbing && !otherColliderHit && otherCollider == us)
        {
            otherColliderHit = true;
            NPC_Interact.Instance.ClearInteraction();
            return;
        }
        if(climbing && otherColliderHit && otherCollider == us)
        {
            climbing = false;
            otherColliderHit = false;
            otherCollider = null;
        }
        Debug.LogFormat("{0} entered ladder area", other.gameObject.name);
        forceLadderInteractable = true;
        NPC_Interact.Instance.ForceInteraction(this);
    }

    public void OnTriggerExitFromChild(Collider other)
    {
        Debug.LogFormat("{0} left ladder area", other.gameObject.name);
        forceLadderInteractable = false;

        NPC_Interact.Instance.ClearInteraction();
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
