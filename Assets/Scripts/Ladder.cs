using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
//Octo Beard. ca. 2021. How to Make Simple Ladders in Unity | 5 Minute Unity Tutorials [YouTube user-generated content]. Available at: https://www.youtube.com/watch?v=138WGOIgUeI [accessed 7 November 2023].
public class LadderScript : MonoBehaviour
{
    public Transform chController;
    bool inside = false;
    public float speedUpDown = 3.2f;
    public Improved_Movement FPSInput;
    private Rigidbody rb;
    private Transform ladder;
    private Transform Ground;

    private bool newInputSystem = false;
    private Coroutine ladderProcess;

    // Start is called before the first frame update
    private void Start()
    {
        rb = GetComponent<Rigidbody>();
        FPSInput = GetComponent<Improved_Movement>();
        inside = false;
        newInputSystem = InputManager.Instance != null;
        Ground = gameObject.GetComponent<Improved_Movement>().groundCheck;

        if (newInputSystem)
        {
            InputManager.Instance.southButton.OnButtonHeld += OnJumpHeld;
        }
    }
    private void OnDestroy()
    {
        InputManager.Instance.southButton.OnButtonHeld -= OnJumpHeld;
    }
    private void OnJumpHeld()
    {
        if (inside)
        {
            ExitLadder();
        }
    }

    private void OnTriggerEnter(Collider col)
    {
       if(col.gameObject.CompareTag("Ladder"))
       {
            FPSInput.enabled = false;
            inside = true;
            if(ladderProcess != null)
            {
                StopCoroutine(ladderProcess);
            }
            
            ladder = col.transform;
            ladderProcess = StartCoroutine(UpdateLadder());
        }
    }

    private void OnTriggerExit(Collider col)
    {
        if (col.gameObject.CompareTag("Ladder"))
        {
            if (ladderProcess != null)
            {
                StopCoroutine(ladderProcess);
            }
            ExitLadder();
        }
    }

    private void ExitLadder()
    {
        FPSInput.enabled = true;
        inside = false;
        ladder = null;
    }

    private IEnumerator UpdateLadder()
    {
        while (true)
        {
            if (inside)
            {
                float speed = 0;
                if (newInputSystem)
                {
                    float axisValue = InputManager.Instance.MoveAxis.y;
                    speed = (axisValue > 0) ? speedUpDown : ((axisValue < 0) ? -speedUpDown : speed);
                }
                else
                {
                    if (Input.GetKey(KeyCode.W))
                    {
                        speed = speedUpDown;
                    }
                    else if (Input.GetKey(KeyCode.S))
                    {
                        speed = -speedUpDown;
                    }
                    if (Input.GetKey(KeyCode.Space))
                    {
                        ExitLadder();
                    }
                }
                chController.transform.position += speed * Time.deltaTime * ladder.up;

                if (gameObject.CompareTag("ground"))
                {
                    ExitLadder();
                }
            }
            yield return null;
        }
    }

    // private void Update()
    // {
    //     if (inside == true && Input.GetKey(KeyCode.Space))
    //     {
    //         FPSInput.enabled = true;
    //         inside = !inside;
    //         ladder = null;
    //     }
    // 
    //     if (inside == true && gameObject.CompareTag("ground"))
    //     {
    //         FPSInput.enabled = true;
    //         inside = !inside;
    //         ladder = null;
    //     }
    // }
}
