using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.AI;

public class Follow_Player : MonoBehaviour
{
    private GameObject player;
    private NavMeshAgent agent;

    public Action OnHitPlayer;

    private bool enablePlayerHit;
    // Start is called before the first frame update
    void Start()
    {
        player = GameObject.FindGameObjectWithTag("Player");

        agent = GetComponent<NavMeshAgent>();
    }

    // Update is called once per frame
    void Update()
    {
        if (agent.gameObject.activeInHierarchy&&agent.isOnNavMesh)
        {
            agent.destination = player.transform.position;
        }
    }

    public void ZeroStoppingDistance()
    {
        agent.stoppingDistance = 0;
        enablePlayerHit = true;
    }

    private void OnTriggerEnter(Collider other)
    {
        if(enablePlayerHit && other.TryGetComponent<CharacterController>(out _))
        {
            OnHitPlayer?.Invoke();
            enablePlayerHit = false;
        }
    }
    private void OnTriggerStay(Collider other)
    {
        if (enablePlayerHit && other.TryGetComponent<CharacterController>(out _))
        {
            OnHitPlayer?.Invoke();
            enablePlayerHit =  false;
        }
    }
}
