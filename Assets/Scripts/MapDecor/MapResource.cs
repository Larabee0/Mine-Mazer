using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public enum ItemCatagory
{
    Crystal,
    Mushroom,
    Metal,
    Equipment
}

public class MapResource : MonoBehaviour, IInteractable
{
    // Start is called before the first frame update
    void Start()
    {

    }

    // Update is called once per frame
    void Update()
    {

    }

    public string GetToolTipText()
    {
        throw new System.NotImplementedException();
    }

    public void Interact()
    {
        throw new System.NotImplementedException();
    }
}
