using System.Collections;
using System.Collections.Generic;
using UnityEditor.ShaderGraph.Drawing.Inspector.PropertyDrawers;
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

    // Start is called before the first frame update
    private void Start()
    {
        rb = GetComponent<Rigidbody>();
        FPSInput = GetComponent<Improved_Movement>();
        inside = false;
        Ground = gameObject.GetComponent<Improved_Movement>().groundCheck;
    }
    private void OnTriggerEnter(Collider col)
    {
       if(col.gameObject.CompareTag("Ladder"))
       {
            FPSInput.enabled = false;
            inside = !inside;
            ladder = col.transform;
       }
    }
    private void OnTriggerExit(Collider col)
    {
        if (col.gameObject.CompareTag("Ladder"))
        {
            FPSInput.enabled = true;
            inside = !inside;
            ladder = null;
        }
    }
    private void Update()
    {
        if (inside == true && Input.GetKey(KeyCode.W))
        {
            chController.transform.position += speedUpDown * Time.deltaTime * Vector3.up;
        }

        if (inside == true && Input.GetKey(KeyCode.S))
        {
            chController.transform.position += speedUpDown * Time.deltaTime * Vector3.down;
        }

        if (inside == true && Input.GetKey(KeyCode.Space))
        {
            FPSInput.enabled = true;
            inside = !inside;
            ladder = null;
        }

        if (inside == true && gameObject.CompareTag("ground"))
        {
            FPSInput.enabled = true;
            inside = !inside;
            ladder = null;
        }
    }
}
