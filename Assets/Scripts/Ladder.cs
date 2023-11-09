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

    // Start is called before the first frame update
    private void Start()
    {
        FPSInput = GetComponent<Improved_Movement>();
        inside = false;
    }
    private void OnTriggerEnter(Collider col)
    {
       if(col.gameObject.tag == "Ladder")
        {
            FPSInput.enabled = false;
            inside = !inside;
        }
    }
    private void OnTriggerExit(Collider col)
    {
        if(col.gameObject.tag == "Ladder")
        {
            FPSInput.enabled = true;
            inside = !inside;
        }
    }
    private void Update()
    {
        if(inside == true && Input.GetKey("w"))
        {
            chController.transform.position += Vector3.up * speedUpDown * Time.deltaTime;
        }

        if(inside == true && Input.GetKey("s"))
        {
            chController.transform.position += Vector3.down * speedUpDown * Time.deltaTime;
        }
    }
}
