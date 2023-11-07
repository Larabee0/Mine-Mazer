using System.Collections;
using System.Collections.Generic;
using UnityEngine;

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
