using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class PlayerExplorationStatistics : MonoBehaviour
{
    [SerializeField] private int uniqueSectionsVisited = 0;

    public int UniqueSectionsVisited => uniqueSectionsVisited;


    public void Increment()
    {
        uniqueSectionsVisited++;
    }
}
