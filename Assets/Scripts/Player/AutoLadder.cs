using MazeGame.Input;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

public class AutoLadder : MonoBehaviour
{
    public Transform chController;
    public float speedUpDown = 3.2f;
    public float speedToAlign = 3.2f;
    private float dir = 1;
    public Improved_Movement FPSInput;
    private LadderData ladder;
    [SerializeField] private LayerMask layerMask;
    private Vector3 startPos;
    private Vector3 endPos;
    private Vector3 forwards;

    private Coroutine ladderProcess;

    // Start is called before the first frame update
    private void Start()
    {
        FPSInput = GetComponent<Improved_Movement>();
    }

    public void ClimbLadder()
    {
        List<RaycastHit> hits = new(Physics.SphereCastAll(transform.position, 0.5f, transform.forward, 2f, layerMask.value, QueryTriggerInteraction.Collide));
        ladder = null;
        if (hits.Count > 0 )
        {
            ladder = hits.FirstOrDefault(hit => hit.collider.GetComponentInParent<LadderData>() != null).collider.GetComponentInParent<LadderData>();
            if(ladder != null )
            {
                HashSet<Transform> transforms = new();
                hits.ForEach(hit => transforms.Add(hit.collider.transform));


                if (transforms.Contains(ladder.Top))
                {
                    startPos = ladder.Top.transform.position;
                    endPos = ladder.Bottom.transform.position;
                    forwards = -ladder.Main.forward;
                    dir = -1;
                    ladderProcess ??= StartCoroutine(LadderProcess());
                }
                else if (transforms.Contains(ladder.Bottom))
                {
                    endPos = ladder.Top.transform.position;
                    startPos = ladder.Bottom.transform.position;
                    forwards = -ladder.Main.forward;
                    dir = 1;
                    ladderProcess ??= StartCoroutine(LadderProcess());
                }
                else
                {
                    Debug.LogError("Ladder fail to find top or bottom");
                }
            }
        }
    }

    private IEnumerator LadderProcess()
    {
        InputManager.Instance.PlayerActions.Disable();
        FPSInput.enabled = false;
        while (transform.position != startPos)
        {
            transform.position = Vector3.MoveTowards(transform.position, startPos, Time.deltaTime * speedToAlign);
            transform.forward = Vector3.MoveTowards(transform.forward, forwards, Time.deltaTime * speedToAlign);
            yield return null;
        }

        while(transform.forward != forwards)
        {
            transform.forward = Vector3.MoveTowards(transform.forward, forwards, Time.deltaTime * speedToAlign);
            yield return null;
        }

        //PlayerAnimationController.Instance.LadderClimb(dir);

        while(transform.position != endPos)
        {
            transform.position = Vector3.MoveTowards(transform.position, endPos, Time.deltaTime * speedUpDown);
            yield return null;
        }

        //PlayerAnimationController.Instance.LadderEnd();

        ladderProcess = null;
        FPSInput.enabled = true;
        InputManager.Instance.PlayerActions.Enable();
    }

}
