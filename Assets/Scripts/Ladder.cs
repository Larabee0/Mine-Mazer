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

    private Transform ladder;

    private bool newInputSystem = false;
    private Coroutine ladderProcess;

    // Start is called before the first frame update
    private void Start()
    {
        FPSInput = GetComponent<Improved_Movement>();
        inside = false;
        newInputSystem = InputManager.Instance != null;
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
            FPSInput.enabled = true;
            inside = false;
            if (ladderProcess != null)
            {
                StopCoroutine(ladderProcess);
            }
            ladder = null;
        }
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
                }
                chController.transform.position += speed * Time.deltaTime * ladder.up;
            }
            yield return null;
        }
    }

    //private void Update()
    //{
    //    if(inside == true && Input.GetKey(KeyCode.W))
    //    {
    //        chController.transform.position += speedUpDown * Time.deltaTime * ladder.up;
    //    }
    //
    //    if(inside == true && Input.GetKey(KeyCode.S))
    //    {
    //        chController.transform.position += -speedUpDown * Time.deltaTime * ladder.up;
    //    }
    //}
}
